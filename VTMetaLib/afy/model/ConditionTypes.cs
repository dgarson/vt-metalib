using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VTMetaLib.afy.yaml;
using VTMetaLib.VTank;

namespace VTMetaLib.afy.Model
{
    public interface AfyCondition : AfyEntity
    {
        public AfyConditionType ConditionType { get; }

        public VTCondition AsVTCondition(AfyYamlContext context);
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

    public class AfyConditionTypes
    {
        public static readonly Dictionary<AfyConditionType, Type> ConditionTypeToModelClass = new Dictionary<AfyConditionType, Type>();

        static AfyConditionTypes()
        {
            ConditionTypeToModelClass.Add(AfyConditionType.Never, typeof(NeverCondition));
            ConditionTypeToModelClass.Add(AfyConditionType.Always, typeof(AlwaysCondition));
            ConditionTypeToModelClass.Add(AfyConditionType.Not, typeof(NotCondition));
            ConditionTypeToModelClass.Add(AfyConditionType.All, typeof(AllCondition));
            ConditionTypeToModelClass.Add(AfyConditionType.Any, typeof(AnyCondition));
            ConditionTypeToModelClass.Add(AfyConditionType.BurdenPercentGE, typeof(BurdenPercentGE));
            ConditionTypeToModelClass.Add(AfyConditionType.ChatMatch, typeof(ChatMatch));
            ConditionTypeToModelClass.Add(AfyConditionType.ChatCapture, typeof(ChatCapture));
            ConditionTypeToModelClass.Add(AfyConditionType.Death, typeof(DeathCondition));
            ConditionTypeToModelClass.Add(AfyConditionType.DistanceToRouteGE, typeof(DistanceToRouteGE));
            ConditionTypeToModelClass.Add(AfyConditionType.Expr, typeof(ExprCondition));
            ConditionTypeToModelClass.Add(AfyConditionType.ItemCountGE, typeof(ItemCountGE));
            ConditionTypeToModelClass.Add(AfyConditionType.ItemCountLE, typeof(ItemCountLE));
            ConditionTypeToModelClass.Add(AfyConditionType.LandblockE, typeof(LandblockE));
            ConditionTypeToModelClass.Add(AfyConditionType.LandcellE, typeof(LandcellE));
            ConditionTypeToModelClass.Add(AfyConditionType.MainSlotsLE, typeof(MainSlotsLE));
            ConditionTypeToModelClass.Add(AfyConditionType.MetaVersionChanged, typeof(MetaVersionChanged));
            ConditionTypeToModelClass.Add(AfyConditionType.MobsInDistanceByName, typeof(MobsInDistanceByName));
            ConditionTypeToModelClass.Add(AfyConditionType.MobsInDistanceByPriority, typeof(MobsInDistanceByPriority));
            ConditionTypeToModelClass.Add(AfyConditionType.NavEmpty, typeof(NavEmpty));
            ConditionTypeToModelClass.Add(AfyConditionType.NeedToBuff, typeof(NeedToBuff));
            ConditionTypeToModelClass.Add(AfyConditionType.NoMobsInRange, typeof(NoMobsInRange));
            ConditionTypeToModelClass.Add(AfyConditionType.PortalEnter, typeof(PortalEnter));
            ConditionTypeToModelClass.Add(AfyConditionType.PortalExit, typeof(PortalExit));
            ConditionTypeToModelClass.Add(AfyConditionType.PSecsInStateGE, typeof(PSecsInStateGE));
            ConditionTypeToModelClass.Add(AfyConditionType.SecsInStateGE, typeof(SecsInStateGE));
            ConditionTypeToModelClass.Add(AfyConditionType.SecsOnSpellGE, typeof(SecsOnSpellGE));
            ConditionTypeToModelClass.Add(AfyConditionType.VendorOpen, typeof(VendorOpen));
            ConditionTypeToModelClass.Add(AfyConditionType.VendorClosed, typeof(VendorClosed));
        }
    }

    public static class AfyConditionTypeExtensions
    {
        public static Type GetConditionTypeModelClass(this AfyConditionType condType)
        {
            Type type;
            if (AfyConditionTypes.ConditionTypeToModelClass.TryGetValue(condType, out type))
                return type;
            throw new ArgumentException($"Unrecognized ConditionType {condType}");
        }
    }
}
