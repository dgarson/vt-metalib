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

    public interface AfyEntityWithChildren<C> : AfyEntity where C : AfyEntity
    {
        /// <summary>
        /// List of child entities for this one
        /// </summary>
        public List<C> Children { get; }
    }
}
