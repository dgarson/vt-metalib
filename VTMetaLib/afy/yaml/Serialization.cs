using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization.NodeDeserializers;

namespace VTMetaLib.afy.yaml
{
    public class YamlSerialization
    {
        public static IDeserializer CreateDeserializer()
        {
            INamingConvention namingConvention = PascalCaseNamingConvention.Instance;
            return new DeserializerBuilder()
                .WithNamingConvention(namingConvention)
                .WithNodeDeserializer(
                    inner => new AbstractNodeNodeTypeResolver(inner, new AfyStateEntryTypeResolver(namingConvention),
                                new AfyConditionTypeResolver(namingConvention), new AfyActionTypeResolver(namingConvention)),
                    s => s.InsteadOf<ObjectNodeDeserializer>())
                .Build();
        }

        public static ISerializer CreateSerializer()
        {
            return new SerializerBuilder()
                // ... TODO
                .Build();
        }
    }

    public interface ITypeDiscriminator
    {
        Type BaseType { get; }

        bool TryResolve(ParsingEventBuffer buffer, out Type suggestedType);
    }

    public class AbstractNodeNodeTypeResolver : INodeDeserializer
    {
        private readonly INodeDeserializer original;
        private readonly ITypeDiscriminator[] typeDiscriminators;

        public AbstractNodeNodeTypeResolver(INodeDeserializer original, params ITypeDiscriminator[] discriminators)
        {
            if (original is not ObjectNodeDeserializer)
            {
                throw new ArgumentException($"{nameof(AbstractNodeNodeTypeResolver)} requires the original resolver to be a {nameof(ObjectNodeDeserializer)}");
            }

            this.original = original;
            typeDiscriminators = discriminators;
        }

        public bool Deserialize(IParser reader, Type expectedType, Func<IParser, Type, object> nestedObjectDeserializer, out object value)
        {
            // we're essentially "in front of" the normal ObjectNodeDeserializer.
            // We could let it check if the current event is a mapping, but we also need to know.
            if (!reader.Accept<MappingStart>(out var mapping))
            {
                value = null;
                return false;
            }

            // can any of the registered discrimaintors deal with the abstract type?
            var supportedTypes = typeDiscriminators.Where(t => t.BaseType.IsAssignableFrom(expectedType));
            if (!supportedTypes.Any())
            {
                // no? then not a node/type we want to deal with
                return original.Deserialize(reader, expectedType, nestedObjectDeserializer, out value);
            }

            // now buffer all the nodes in this mapping.
            // it'd be better if we did not have to do this, but YamlDotNet does not support non-streaming access.
            // See:  https://github.com/aaubry/YamlDotNet/issues/343
            // WARNING: This has the potential to be quite slow and add a lot of memory usage, especially for large documents.
            // It's better, if you use this at all, to use it on leaf mappings
            var start = reader.Current.Start;
            Type actualType;
            ParsingEventBuffer buffer;
            LinkedList<ParsingEvent> events;
            try
            {
                events = ReadNestedMapping(reader);
                buffer = new ParsingEventBuffer(events);

                // use the discriminators to tell us what type it is really expecting by letting it inspect the parsing events
                actualType = CheckWithDiscriminators(expectedType, supportedTypes, buffer);
            }
            catch (Exception exception)
            {
                throw new YamlException(start, reader.Current.End, "Failed when resolving abstract type", exception);
            }

            // now continue by re-emitting parsing events
            buffer.Reset();
            return original.Deserialize(buffer, actualType, nestedObjectDeserializer, out value);
        }

        private static Type CheckWithDiscriminators(Type expectedType, IEnumerable<ITypeDiscriminator> supportedTypes, ParsingEventBuffer buffer)
        {
            foreach (var discriminator in supportedTypes)
            {
                buffer.Reset();
                if (discriminator.TryResolve(buffer, out var actualType))
                {
                    CheckReturnedType(discriminator.BaseType, actualType);
                    return actualType;
                }
            }

            throw new Exception($"None of the registered type discriminators could supply a child class for {expectedType}");
        }

        private static LinkedList<ParsingEvent> ReadNestedMapping(IParser reader)
        {
            var result = new LinkedList<ParsingEvent>();
            result.AddLast(reader.Consume<MappingStart>());
            var depth = 0;
            do
            {
                var next = reader.Consume<ParsingEvent>();
                depth += next.NestingIncrease;
                result.AddLast(next);
            } while (depth >= 0);

            return result;
        }

        private static void CheckReturnedType(Type baseType, Type candidateType)
        {
            if (candidateType is null)
            {
                throw new NullReferenceException($"The type resolver for AbstractNodeNodeTypeResolver returned null. It must return a valid sub-type of {baseType}.");
            }
            else if (candidateType.GetType() == baseType)
            {
                throw new InvalidOperationException($"The type resolver for AbstractNodeNodeTypeResolver returned the abstract type. It must return a valid sub-type of {baseType}.");
            }
            else if (!baseType.IsAssignableFrom(candidateType))
            {
                throw new InvalidOperationException($"The type resolver for AbstractNodeNodeTypeResolver returned a type ({candidateType}) that is not a valid sub type of {baseType}");
            }
        }
    }

    public class ParsingEventBuffer : IParser
    {
        private readonly LinkedList<ParsingEvent> buffer;

        private LinkedListNode<ParsingEvent> current;

        public ParsingEventBuffer(LinkedList<ParsingEvent> events)
        {
            buffer = events;
            current = events.First;
        }

        public ParsingEvent Current => current?.Value;

        public bool DeleteCurrentAndPrevious()
        {
            ParsingEvent prev = current?.Previous?.Value;
            ParsingEvent cur = Current;
            MoveNext();
            ParsingEvent newCur = Current;

            if (prev != null)
                buffer.Remove(prev);
            buffer.Remove(cur);

            current = buffer.First;
            if (newCur != null)
            {
                while (Current != newCur && Current != null)
                    MoveNext();
            }
            return true;
        }

        public bool MoveNext()
        {
            if (current == null)
                return false;
            current = current.Next;
            return current is not null;
        }

        public void Reset()
        {
            current = buffer.First;
        }
    }

    public static class IParserExtensions
    {
        public static bool TryFindMappingEntry(this ParsingEventBuffer parser, Func<Scalar, bool> selector, out Scalar key, out ParsingEvent value)
        {
            parser.Consume<MappingStart>();
            do
            {
                // so we only want to check keys in this mapping, don't descend
                switch (parser.Current)
                {
                    case Scalar scalar:
                        // we've found a scalar, check if it's value matches one
                        // of our  predicate
                        var keyMatched = selector(scalar);

                        // move head so we can read or skip value
                        parser.MoveNext();

                        // read the value of the mapping key
                        if (keyMatched)
                        {
                            // success
                            value = parser.Current;
                            key = scalar;
                            return true;
                        }

                        // skip the value
                        parser.SkipThisAndNestedEvents();

                        break;
                    case MappingStart or SequenceStart:
                        parser.SkipThisAndNestedEvents();
                        break;
                    default:
                        // do nothing, skip to next node
                        parser.MoveNext();
                        break;
                }
            } while (parser.Current is not null);

            key = null;
            value = null;
            return false;
        }
    }
}
