using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using VTMetaLib.IO;

using VTMetaLib.afy.Model;
using VTMetaLib.afy.yaml;

using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Core;

/**
 * https://github.com/aaubry/YamlDotNet/wiki/Serialization.Deserializer
 * https://stackoverflow.com/questions/64242023/yamldotnet-custom-serialization
 * https://github.com/aaubry/YamlDotNet/tree/master/YamlDotNet/Core/Events
 */
namespace VTMetaLib.afy.IO
{
    public class AfyReader : IDisposable
    {
        private readonly LineReadable lines;
        private readonly TextReader textReader;

        private readonly IDeserializer yamlDeserializer;
        private bool disposed;

        public AfyReader(TextReader textReader)
        {
            this.textReader = textReader;
            this.lines = InMemoryLines.ReadAllFrom(textReader);

            yamlDeserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
        }

        public AfyReader(string contents) : this(new StringReader(contents)) { }

        public string GetContextLines()
        {
            // TODO!!
            return "";
        }

        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                if (textReader != null)
                    textReader.Dispose();
            }
        }
    }

    internal abstract class AfyYamlTypeConverter : IYamlTypeConverter
    {
        // Unfortunately the API does not provide those in the ReadYaml and WriteYaml
        // methods, so we are forced to set them after creation.
        public IValueSerializer ValueSerializer { get; internal set; }
        public IValueDeserializer ValueDeserializer { get; internal set; }

        public Type ModelType { get; internal set; }

        protected AfyYamlTypeConverter(Type modelType)
        {
            ModelType = modelType;
        }

        public bool Accepts(Type type) => ModelType.IsAssignableFrom(type);

        public abstract object ReadYaml(IParser parser);
        public abstract void WriteYaml(IEmitter emitter, object value);

        public object ReadYaml(IParser parser, Type type)
        {
            if (type != ModelType)
                throw new ArgumentException($"Only supports {ModelType.Name} but attempted to use {GetType().Name} to deserialize {type.Name}");
            return ReadYaml(parser);
        }

        public void WriteYaml(IEmitter emitter, object value, Type type)
        {
            if (type != ModelType)
                throw new ArgumentException($"Only supports {ModelType.Name} but attempted to use {GetType().Name} to serialize {type.Name}");
            WriteYaml(emitter, value);
        }
    }

}
