using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using VTMetaLib.afy;
using VTMetaLib.afy.IO;
using VTMetaLib.afy.Model;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace VTMetaLib.afy.yaml
{
    public static class Yaml
    {
        public static TextReader ReaderFrom(string name)
        {
            return new StreamReader(StreamFrom(name));
        }

        public static Stream StreamFrom(string name)
        {
            var fromType = typeof(Yaml);
            var assembly = fromType.Assembly;
            var stream = assembly.GetManifestResourceStream(name) ??
                         assembly.GetManifestResourceStream(fromType.Namespace + ".files." + name);
            return stream;
        }

        public static string TemplatedOn<T>(this TextReader reader)
        {
            var text = reader.ReadToEnd();
            return text.TemplatedOn<T>();
        }

        public static string TemplatedOn<T>(this string text)
        {
            return Regex.Replace(text, @"{type}", match =>
                Uri.EscapeDataString(typeof(T).Name));
        }

        public static IParser ParserForEmptyContent()
        {
            return new Parser(new StringReader(string.Empty));
        }

        public static IParser ParserForResource(string name)
        {
            return new Parser(Yaml.ReaderFrom(name));
        }

        public static IParser ParserForText(string yamlText)
        {
            return new Parser(ReaderForText(yamlText));
        }

        public static Scanner ScannerForResource(string name)
        {
            return new Scanner(Yaml.ReaderFrom(name));
        }

        public static Scanner ScannerForText(string yamlText)
        {
            return new Scanner(ReaderForText(yamlText));
        }

        public static StringReader ReaderForText(string yamlText)
        {
            return new StringReader(Text(yamlText));
        }

        public static string Text(string yamlText)
        {
            var lines = yamlText
                .Split('\n')
                .Select(l => l.TrimEnd('\r', '\n'))
                .SkipWhile(l => l.Trim(' ', '\t').Length == 0)
                .ToList();

            while (lines.Count > 0 && lines[lines.Count - 1].Trim(' ', '\t').Length == 0)
            {
                lines.RemoveAt(lines.Count - 1);
            }

            if (lines.Count > 0)
            {
                var indent = Regex.Match(lines[0], @"^(\s*)");
                if (!indent.Success)
                {
                    throw new ArgumentException("Invalid indentation");
                }

                lines = lines
                    .Select(l => l.Substring(indent.Groups[1].Length))
                    .ToList();
            }

            return string.Join("\n", lines.ToArray());
        }
    }

    public class AfyStateEntryTypeResolver : ITypeDiscriminator
    {
        public const string ActionFieldName = "Action";
        public const string ConditionFieldName = "Condition";
        public const string DoFieldName = "Do";

        public Type BaseType => typeof(AfyStateEntry);

        private readonly string actionFieldName;
        private readonly string conditionFieldName;
        private readonly string doFieldName;

        public AfyStateEntryTypeResolver(INamingConvention namingConvention)
        {
            actionFieldName = namingConvention.Apply(ActionFieldName);
            conditionFieldName = namingConvention.Apply(ConditionFieldName);
            doFieldName = namingConvention.Apply(DoFieldName);
        }

        public AfyStateEntryTypeResolver() : this(CamelCaseNamingConvention.Instance) { }

        public bool TryResolve(ParsingEventBuffer buffer, out Type suggestedType)
        {
            if (buffer.TryFindMappingEntry(
                scalar => doFieldName == scalar.Value,
                out Scalar doKey,
                out ParsingEvent doValue))
            {
                string doType = doValue.AsScalar().Value;
                switch (doType)
                {
                    case "ImportFragment":
                    case "ClearFragmentVars":
                    case "ClearManagedVars":
                    case "SetManagedVars":
                        suggestedType = typeof(AfyRule);
                        return true;
                    default:
                        throw new ArgumentException($"Unrecognized afy directive: {doType}, supported: ImportFragment, ClearFragmentVars, SetManagedVars, ClearManagedVars");
                }
            }
            // we have to reset to do the full scan again
            buffer.Reset();

            if (buffer.TryFindMappingEntry(
                scalar => conditionFieldName == scalar.Value,
                out Scalar key,
                out ParsingEvent value))
            {
                // buffer.DeleteCurrentAndPrevious();
                suggestedType = typeof(AfyRule);
                return true;
            }
            throw new ArgumentException($"Found neither a 'Do' or 'Condition' field which are required for a state/rule entry");
        }
    }

    public static class ParserEventBufferExtensions
    {
        public static Scalar AsScalar(this ParsingEvent value)
        {
            if (value is Scalar scalar)
                return scalar;
            throw new ArgumentException($"Expected Scalar value but got {value.GetType().Name}: {value}");
        }
    }

    public class AfyActionTypeResolver : ITypeDiscriminator
    {
        public const string FieldName = "Action";

        private readonly string actionFieldKey;

        public Type BaseType => typeof(AfyAction);

        public AfyActionTypeResolver(INamingConvention namingConvention)
        {
            actionFieldKey = namingConvention.Apply(FieldName);
        }

        public AfyActionTypeResolver() : this(CamelCaseNamingConvention.Instance) { }

        public bool TryResolve(ParsingEventBuffer buffer, out Type suggestedType)
        {
            if (buffer.TryFindMappingEntry(
                scalar => actionFieldKey == scalar.Value,
                out Scalar key,
                out ParsingEvent value))
            {
                // read the value of the kind key
                if (value is Scalar valueScalar)
                {
                    suggestedType = ResolveActionModelClass(valueScalar.Value);

                    // 
                    buffer.DeleteCurrentAndPrevious();

                    return true;
                }
                throw new ArgumentException($"Unexpected non-scalar value for {FieldName}");
            }

            // we could not find our key, thus we could not determine correct child type
            suggestedType = null;
            return false;
        }

        private Type ResolveActionModelClass(string val)
        {
            AfyActionType actionType;
            if (!Enum.TryParse(val, out actionType))
            {
                var known = string.Join(", ", Enum.GetNames(typeof(AfyActionType)));
                throw new ArgumentException($"Unrecognized action type ('{val}'), supported values are: {known}");
            }
            return actionType.GetActionTypeModelClass();
        }
    }

    public class AfyConditionTypeResolver : ITypeDiscriminator
    {
        public const string FieldName = "Condition";

        private readonly string conditionFieldKey;

        public Type BaseType => typeof(AfyCondition);

        public AfyConditionTypeResolver(INamingConvention namingConvention)
        {
            conditionFieldKey = namingConvention.Apply(FieldName);
        }

        public AfyConditionTypeResolver() : this(CamelCaseNamingConvention.Instance) { }

        public bool TryResolve(ParsingEventBuffer buffer, out Type suggestedType)
        {
            if (buffer.TryFindMappingEntry(
                scalar => conditionFieldKey == scalar.Value,
                out Scalar key,
                out ParsingEvent value))
            {
                // read the value of the kind key
                if (value is Scalar valueScalar)
                {
                    suggestedType = ResolveConditionModelClass(valueScalar.Value);
                    // 
                    buffer.DeleteCurrentAndPrevious();

                    return true;
                }
                throw new ArgumentException($"Unexpected non-scalar value for {FieldName}");
            }

            // we could not find our key, thus we could not determine correct child type
            suggestedType = null;
            return false;
        }

        private Type ResolveConditionModelClass(string val)
        {
            AfyConditionType condType;
            if (!Enum.TryParse(val, out condType))
            {
                var known = string.Join(", ", Enum.GetNames(typeof(AfyConditionType)));
                throw new ArgumentException($"Unrecognized condition type ('{val}'), supported values are: {known}");
            }
            return condType.GetConditionTypeModelClass();
        }
    }


    public interface AfyYamlNode
    {

        public void ReadFrom(AfyReader reader, AfyYamlContext context);

        public void WriteTo(AfyWriter writer, AfyYamlContext context);
    }

    public class AfyYamlContext : AfyContext
    {
        public string FilePath { get; set; }

        /// <summary>
        /// If we are in the midst of exporting rules or states that are being imported from a fragment of state fragment, this
        /// indicates the state name for which rendered rules will be imported.
        /// </summary>
        public string ImportingStateName { get; set; }

        // TODO more stuffzzzz
    }

    public class StateEntryYamlTypeConverter : IYamlTypeConverter
    {
        public bool Accepts(Type type)
        {
            return type == typeof(AfyStateEntry) || type == typeof(AfyRule);
        }

        public object ReadYaml(IParser parser, Type type)
        {
            // read the '-' prefixing the first property in a rule entry
            parser.Require<MappingStart>();
            parser.MoveNext();

            // keep reading arbitrary properties in the rule
            // parser.

            DeserializerBuilder builder = new DeserializerBuilder();

            return null;
        }

        public void WriteYaml(IEmitter emitter, object value, Type type)
        {
            throw new NotImplementedException();
        }
    }

    
}
