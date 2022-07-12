using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using YamlDotNet;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using VTMetaLib.VTank;

namespace VTMetaLib.afy.IO
{
    public class AfyMetaConverter
    {
        
    }

    public class AfyWriter : IDisposable
    {
        private readonly StreamWriter textWriter;
        private readonly ISerializer serializer;

        public AfyWriter(StreamWriter streamWriter)
        {
            textWriter = streamWriter;
            serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
        }

        public void WriteComments(IEnumerable<string> comments)
        {
            foreach (var comment in comments)
                WriteComment(comment);
        }

        public void WriteComment(string comment)
        {
            textWriter.WriteLine($"# {comment}");
        }

        /*
        public void WriteFile(AfyFile file)
        {
            // TODO :
            // serializer.Serialize()
        }
        */

        public void Dispose()
        {

        }
    }
}
