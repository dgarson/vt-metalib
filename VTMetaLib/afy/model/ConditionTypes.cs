using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VTMetaLib.afy.Model
{
    public interface AfyCondition : AfyEntity
    {
        public AfyConditionType ConditionType { get; }
    }

    public abstract class AfyConditionWithChildren : AfyCondition, AfyEntityWithChildren<AfyCondition>
    {
        public abstract AfyConditionType ConditionType { get; }

        public List<AfyCondition> Conditions { get; internal set; } = new List<AfyCondition>();
        public abstract AfyEntity Parent { get; }
        public abstract Dictionary<string, string> Metadata { get; }
        public List<AfyCondition> Children => Conditions;
    }

    public enum AfyConditionType
    {
        #region VTank Native Condition Types

        Never,
        Always,
        Not,
        All,
        Any,
        Expr,
        ChatMatch,
        ChatCapture,
        MainSlotsLE,
        SecsInStateGE,
        PSecsInStateGE,
        NavEmpty,
        Death,
        VendorOpen,
        VendorClosed,
        ItemCountLE,
        ItemCountGE,
        MobsInDistanceByName,
        MobsInDistanceByPriority,
        NeedToBuff,
        NoMobsInRange,
        LandblockE,
        LandcellE,
        PortalEnter,
        PortalExit,
        SecsOnSpellGE,
        BurdenPercentGE,
        DistanceToRouteGE,

        #endregion VTank Native Condition Types

        #region afy-exclusive Condition Types

        MetaVersionChanged,

        #endregion afy-exclusive Condition Types
    }

}
