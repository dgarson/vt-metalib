using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VTMetaLib.afy.yaml;
using VTMetaLib.VTank;
using YamlDotNet.Serialization;

namespace VTMetaLib.afy.Model
{
    public abstract class AfyAction : AfyEntityMetadata, AfyEntity
    {
        [YamlIgnore]
        public abstract AfyActionType ActionType { get; }

        [YamlIgnore]
        public AfyEntity Parent { get; internal set; }

        public abstract VTAction AsVTAction(AfyYamlContext context);

    }

    public class AfyActionTypes
    {
        public static readonly Dictionary<AfyActionType, Type> ActionTypeToModelClass = new Dictionary<AfyActionType, Type>();

        static AfyActionTypes()
        {
            ActionTypeToModelClass.Add(AfyActionType.None, typeof(NoneAction));
            ActionTypeToModelClass.Add(AfyActionType.All, typeof(AllAction));

            ActionTypeToModelClass.Add(AfyActionType.SetState, typeof(SetState));
            ActionTypeToModelClass.Add(AfyActionType.CallState, typeof(CallState));
            ActionTypeToModelClass.Add(AfyActionType.Return, typeof(ReturnAction));

            ActionTypeToModelClass.Add(AfyActionType.Chat, typeof(ChatCommand));
            ActionTypeToModelClass.Add(AfyActionType.ChatExpr, typeof(ChatExpr));
            ActionTypeToModelClass.Add(AfyActionType.EmbedNav, typeof(EmbedNav));
            ActionTypeToModelClass.Add(AfyActionType.Expr, typeof(ExprAct));

            ActionTypeToModelClass.Add(AfyActionType.GetOpt, typeof(GetOpt));
            ActionTypeToModelClass.Add(AfyActionType.SetOpt, typeof(SetOpt));

            ActionTypeToModelClass.Add(AfyActionType.SetWatchdog, typeof(SetWatchdog));
            ActionTypeToModelClass.Add(AfyActionType.ClearWatchdog, typeof(ClearWatchdog));

            ActionTypeToModelClass.Add(AfyActionType.CreateView, typeof(CreateView));
            ActionTypeToModelClass.Add(AfyActionType.DestroyView, typeof(DestroyView));
            ActionTypeToModelClass.Add(AfyActionType.DestroyAllViews, typeof(DestroyAllViews));

            ActionTypeToModelClass.Add(AfyActionType.ImportFragment, typeof(ImportFragment));
            ActionTypeToModelClass.Add(AfyActionType.ClearFragmentVars, typeof(ClearFragmentVars));

            ActionTypeToModelClass.Add(AfyActionType.SetManagedVars, typeof(SetManagedVars));
            ActionTypeToModelClass.Add(AfyActionType.ClearManagedVars, typeof(ClearManagedVars));
        }
    }

    public static class AfyActionTypeExtensions
    {

        public static Type GetActionTypeModelClass(this AfyActionType actionType)
        {
            Type type;
            if (AfyActionTypes.ActionTypeToModelClass.TryGetValue(actionType, out type))
                return type;
            throw new ArgumentException($"Unrecognized ActionType: {actionType}");
        }
    }

    public enum AfyActionType
    {
        #region VTank Native Action Types

        [Description("No Action")]
        None,

        [Description("Multiple")]
        All,

        [Description("Execute Expression")]
        Expr,

        [Description("Send Text to Chatbox")]
        Chat,

        [Description("Send Expression to Chatbox")]
        ChatExpr,

        [Description("Embed Nav Route")]
        EmbedNav,

        [Description("Set Meta State")]
        SetState,

        [Description("Call Meta State")]
        CallState,

        [Description("Return from Call")]
        Return,

        [Description("Get VTank Option")]
        GetOpt,

        [Description("Set VTank Option")]
        SetOpt,

        [Description("Set Watchdog")]
        SetWatchdog,

        [Description("Clear Watchdog")]
        ClearWatchdog,

        [Description("Create View")]
        CreateView,

        [Description("Destroy View")]
        DestroyView,

        [Description("Destroy All Views")]
        DestroyAllViews,

        #endregion VTank Native Action Types

        #region afy-exclusive Action Types

        [Description("Import State Fragment")]
        ImportFragment,

        [Description("Clear Vars for a State Transition")]
        ClearFragmentVars,

        [Description("Set Managed State Vars")]
        SetManagedVars,

        [Description("Registers Managed State Vars by name")]
        RegisterManagedVars,

        [Description("Clear Managed State Vars")]
        ClearManagedVars,

        #endregion

        #region Shorthand Action Types

        #endregion Shorthand Action Types
    }
}
