using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VTMetaLib.afy
{
    public class AfyContext
    {

        // TODO

        public string CurrentStateName { get; set; }

        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }
}
