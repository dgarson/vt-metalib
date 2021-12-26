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

namespace VTMetaLib.afy.yaml
{

    public abstract class AfyYamlCondition : AfyCondition
    {
        internal readonly AfyConditionType condType;

        protected AfyYamlCondition(AfyConditionType condType)
        {
            this.condType = condType;
        }

        public AfyConditionType ConditionType => condType;

        public AfyEntity Parent { get; internal set; }

        // lazy initialize to avoid a shit ton of empty Dictionaries
        public Dictionary<string, string> Metadata { get; internal set; } // = new Dictionary<string, string>();
    }

    public class AfyZeroArgCondition : AfyYamlCondition
    {

        protected AfyZeroArgCondition(AfyConditionType condType) : base(condType) { }
    }

    public class AlwaysCondition : AfyZeroArgCondition
    {
        public static readonly AlwaysCondition Instance = new AlwaysCondition();

        public AlwaysCondition() : base(AfyConditionType.Always) { }
    }

    public class NeverCondition : AfyZeroArgCondition
    {
        public static readonly NeverCondition Instance = new NeverCondition();

        public NeverCondition() : base(AfyConditionType.Never) { }
    }

    public class NavEmptyCondition : AfyZeroArgCondition
    {
        public static readonly NavEmptyCondition Instance = new NavEmptyCondition();

        public NavEmptyCondition() : base(AfyConditionType.NavEmpty) { }
    }

    public class DeathCondition : AfyZeroArgCondition
    {
        public static readonly DeathCondition Instance = new DeathCondition();

        public DeathCondition() : base(AfyConditionType.Death) { }
    }

    public class PortalEnter : AfyZeroArgCondition
    {
        public static readonly PortalEnter Instance = new PortalEnter();

        public PortalEnter() : base(AfyConditionType.PortalEnter) { }
    }
    public class PortalExit : AfyZeroArgCondition
    {
        public static readonly PortalExit Instance = new PortalExit();

        public PortalExit() : base(AfyConditionType.PortalExit) { }
    }

    public class VendorOpen : AfyZeroArgCondition
    {
        public static readonly VendorOpen Instance = new VendorOpen();

        public VendorOpen() : base(AfyConditionType.VendorOpen) { }
    }

    public class VendorClosed : AfyZeroArgCondition
    {
        public static readonly VendorClosed Instance = new VendorClosed();

        public VendorClosed() : base(AfyConditionType.VendorClosed) { }
    }

    public class AnyCondition : AfyConditionWithChildren
    {

    }

    public class AllCondition : AfyConditionWithChildren
    {

    }


    
    public static class AfyConditionsExtensions
    {
        public static AfyCondition NewCondition(this AfyConditionType condType)
        {
            switch (condType)
            {

            }
            throw new ArgumentException($"Unhandled AFY condition type '{condType}' with int value: {(int)condType}");
        }
    }


}
