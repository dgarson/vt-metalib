using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using VTMetaLib.afy;
using VTMetaLib.afy.Model;
using VTMetaLib.afy.IO;
using VTMetaLib.VTank;
using VTMetaLib.Data;

using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Core;

namespace VTMetaLib.afy.yaml
{
    

    internal class AllActionConverter : AfyYamlTypeConverter
    {
        internal AllActionConverter() : base(typeof(AfyAllAction)) { }

        public override object ReadYaml(IParser parser)
        {
            
            ValueDeserializer.DeserializeValue()
            throw new NotImplementedException();
        }

        public override void WriteYaml(IEmitter emitter, object value)
        {
            throw new NotImplementedException();
        }
    }
    
}
