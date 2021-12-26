using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VTMetaLib.afy.Model
{
    public abstract class AfyAction : AfyEntity
    {
        public abstract AfyActionType ActionType { get; }

        public AfyEntity Parent { get; internal set; }
        public Dictionary<string, string> Metadata { get; internal set; }

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

    public class AfyAllAction : AfyAction, AfyEntityWithChildren<AfyAction>
    {
        public List<AfyAction> Actions { get; internal set; } = new List<AfyAction>();

        public override AfyActionType ActionType => AfyActionType.All;

        public List<AfyAction> Children => Actions;
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
        ClearStateVars,

        #endregion

        #region Shorthand Action Types

        #endregion Shorthand Action Types
    }
}
