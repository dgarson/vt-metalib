using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using VTMetaLib.afy;
using VTMetaLib.afy.IO;

namespace VTMetaLib.afy.yaml
{
    public interface AfyYamlNode
    {

        public void ReadFrom(AfyReader reader, AfyYamlContext context);

        public void WriteTo(AfyWriter writer, AfyYamlContext context);
    }

    public class AfyYamlContext : AfyContext
    {
        public string FilePath { get; set; }

        // TODO more stuffzzzz
    }
}
