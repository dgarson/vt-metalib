using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VTMetaLib.afy.Model
{
    public interface AfyEntity
    {
        public AfyEntity Parent { get; }

        // TODO : ADD MORE??

        public Dictionary<string, string> Metadata { get; }

    }

    public partial class AfyEntityMetadata
    {
        public Dictionary<string, string> Metadata { get; private set; }

        public bool HasMetadata => Metadata != null && Metadata.Count > 0;

        public string this[string key]
        {
            get
            {
                if (Metadata == null)
                    return null;
                return Metadata[key];
            }
            set
            {
                if (Metadata == null)
                    Metadata = new Dictionary<string, string>();
                Metadata[key] = value;
            }
        }

        public bool ContainsMetadata(string key)
        {
            return Metadata != null && Metadata.ContainsKey(key);
        }
    }

    public interface AfyEntityWithChildren<C> : AfyEntity where C : AfyEntity
    {
        /// <summary>
        /// List of child entities for this one
        /// </summary>
        public List<C> Children { get; }
    }
}
