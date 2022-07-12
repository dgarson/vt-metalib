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
using YamlDotNet.Core.Events;
using System.IO;

namespace VTMetaLib.afy.yaml
{

    public abstract class AfyYamlAction : AfyAction
    {
        private AfyActionType actionType;

        protected AfyYamlAction(AfyActionType actionType)
        {
            this.actionType = actionType;
        }

        [YamlIgnore]
        public override AfyActionType ActionType => actionType;

        public AfyActionType Action { get => ActionType; set { /* do nothing, already set in constructor */ } }
    }

    public abstract class AfyActionWithChildren : AfyYamlAction, AfyEntityWithChildren<AfyAction>
    {
        public List<AfyAction> Actions { get; set; } = new List<AfyAction>();

        public List<AfyAction> Children => Actions;

        protected AfyActionWithChildren(AfyActionType actionType) : base(actionType) { }

        protected AfyActionWithChildren(AfyActionType actionType, IEnumerable<AfyAction> actions) : this(actionType)
        {
            Actions = new List<AfyAction>(actions);
        }
    }

    public abstract class AfyZeroArgAction : AfyYamlAction
    {
        protected AfyZeroArgAction(AfyActionType actionType) : base(actionType) { }
    }

    public class NoneAction : AfyZeroArgAction
    {
        public NoneAction() : base(AfyActionType.None) { }

        public override VTAction AsVTAction(AfyYamlContext context)
        {
            return ANone.Instance;
        }
    }

    public class SetState : AfyYamlAction
    {
        public string State { get; set; }

        public SetState() : base(AfyActionType.SetState) { }

        public SetState(string state) : this()
        {
            State = state;
        }

        public override VTAction AsVTAction(AfyYamlContext context)
        {
            return new ASetState(State);
        }
    }

    public class ChatCommand : AfyYamlAction
    {
        public string Message { get; set; }

        public ChatCommand() : base(AfyActionType.Chat) { }

        public ChatCommand(string message) : this()
        {
            Message = message;
        }

        public override VTAction AsVTAction(AfyYamlContext context)
        {
            return new AChatCommand(Message);
        }
    }

    public class AllAction : AfyActionWithChildren
    {
        public AllAction() : base(AfyActionType.All) { }

        public AllAction(IEnumerable<AfyAction> actions) : base(AfyActionType.All, actions) { }

        public override VTAction AsVTAction(AfyYamlContext context)
        {
            return new AAll(Actions.Select(a => a.AsVTAction(context)).ToList());
        }
    }

    public class EmbedNav : AfyYamlAction
    {
        public string Name { get; set; }

        public string Label { get; set; }

        public RouteTransform Transform { get; set; }

        public Boolean Reverse { get; set; }

        public EmbedNav() : base(AfyActionType.EmbedNav) { }

        public EmbedNav(string name, string label = "[None]", bool reverseRoute = false, RouteTransform transform = null) : this()
        {
            Name = name;
            Label = label;
            Reverse = reverseRoute;
            Transform = transform;
        }

        public override VTAction AsVTAction(AfyYamlContext context)
        {
            VTNavRoute baseRoute = context.FindNavRoute(Name);
            VTNavRoute embedRoute = baseRoute.Transform(context.NavRoutes[Name], Transform, Reverse);
            // TODO add the Label usage ?? ... gotta investigate that

            return new AEmbedNav(embedRoute);
        }
    }

    public class ExprAct : AfyYamlAction
    {
        public string Expr { get; set; }

        public ExprAct() : base(AfyActionType.Expr) { }

        public ExprAct(string expr) : this()
        {
            Expr = expr;
        }

        public override VTAction AsVTAction(AfyYamlContext context)
        {
            return new AExprAction(Expr);
        }
    }
    
    public class CallState : AfyYamlAction
    {
        public string State { get; set; }

        public string ReturnTo { get; set; }

        public CallState() : base(AfyActionType.CallState) { }

        public CallState(string state, string returnTo) : this()
        {
            State = state;
            ReturnTo = returnTo;
        }

        public override VTAction AsVTAction(AfyYamlContext context)
        {
            return new ACallState(State, ReturnTo);
        }
    }

    public class ChatExpr : AfyYamlAction
    {
        public string Expr { get; set; }

        public ChatExpr() : base(AfyActionType.ChatExpr) { }

        public ChatExpr(string expr) : this()
        {
            Expr = expr;
        }

        public override VTAction AsVTAction(AfyYamlContext context)
        {
            return new AChatExpr(Expr);
        }
    }

    public class ReturnAction : AfyZeroArgAction
    {
        public ReturnAction() : base(AfyActionType.Return) { }

        public override VTAction AsVTAction(AfyYamlContext context)
        {
            return AReturn.Instance;
        }
    }

    public class SetWatchdog : AfyYamlAction
    {
        public string State { get; set; }
        public double Distance { get; set; }
        public int Seconds { get; set; }

        public SetWatchdog() : base(AfyActionType.SetWatchdog) { }

        public SetWatchdog(string state, double distance, int seconds) : this()
        {
            State = state;
            Distance = distance;
            Seconds = seconds;
        }

        public override VTAction AsVTAction(AfyYamlContext context)
        {
            return new ASetWatchdog(State, Distance, Seconds);
        }
    }

    public class ClearWatchdog : AfyZeroArgAction
    {
        public ClearWatchdog() : base(AfyActionType.ClearWatchdog) { }

        public override VTAction AsVTAction(AfyYamlContext context)
        {
            return AClearWatchdog.Instance;
        }
    }

    public class GetOpt : AfyYamlAction
    {
        public string Name { get; set; }
        public string Var { get; set; }

        public GetOpt() : base(AfyActionType.GetOpt) { }

        public GetOpt(string name, string varName) : this()
        {
            Name = name;
            Var = varName;
        }

        public override VTAction AsVTAction(AfyYamlContext context)
        {
            return new AGetOpt(Name, Var);
        }
    }

    public class SetOpt : AfyYamlAction
    {
        public string Name { get; set; }

        public string Value { get; set; }

        public SetOpt() : base(AfyActionType.SetOpt) { }

        public SetOpt(string name, string val) : this()
        {
            Name = name;
            Value = val;
        }

        public override VTAction AsVTAction(AfyYamlContext context)
        {
            return new ASetOpt(Name, Value);
        }
    }

    public class CreateView : AfyYamlAction
    {
        public string Name { get; set; }

        /// <summary>
        /// External path containing a view XML file, mutually exclusive with the Data property
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Literal string block containing the view XML in the afy file. Mutually exclusive with the 
        /// Path property.
        /// </summary>
        public string Data { get; set; }

        public CreateView() : base(AfyActionType.CreateView) { }

        public CreateView(string name, bool isFile, string val) : this()
        {
            Name = name;
            if (isFile)
            {
                Path = val;
                Data = File.ReadAllText(Path);
            }
            else
                Data = val;
        }

        public override VTAction AsVTAction(AfyYamlContext context)
        {
            return new ACreateView(Name, Data);
        }
    }

    public class DestroyView : AfyYamlAction
    {
        public string Name { get; set; }

        public DestroyView() : base(AfyActionType.DestroyView) { }

        public DestroyView(string name) : this()
        {
            Name = name;
        }

        public override VTAction AsVTAction(AfyYamlContext context)
        {
            return new ADestroyView(Name);
        }
    }

    public class ImportFragment : AfyYamlAction
    {
        public string Name { get; set; }

        public string Section { get; set; }

        public List<FragmentVarDefinition> Vars { get; set; } = new List<FragmentVarDefinition>();

        public ImportFragment() : base(AfyActionType.ImportFragment) { }

        public ImportFragment(string name, string section, IEnumerable<FragmentVarDefinition> vars) : this()
        {
            Name = name;
            Section = section;
            Vars.AddRange(vars);
        }

        public override VTAction AsVTAction(AfyYamlContext context)
        {
            throw new NotImplementedException("ImportFragment should be replaced at pre-processing time, prior to VT met export");
        }
    }

    public class ClearFragmentVars : AfyZeroArgAction
    {
        public ClearFragmentVars() : base(AfyActionType.ClearFragmentVars) { }

        public override VTAction AsVTAction(AfyYamlContext context)
        {
            throw new NotImplementedException("ClearFragmentVars should be replaced at pre-processing time, prior to VT met export");
        }
    }

    public class SetManagedVars : AfyYamlAction
    {
        public List<FragmentVarDefinition> Vars { get; set; } = new List<FragmentVarDefinition>();

        public SetManagedVars() : base(AfyActionType.SetManagedVars) { }

        public SetManagedVars(IEnumerable<FragmentVarDefinition> vars) : this() { Vars.AddRange(vars); }

        public override VTAction AsVTAction(AfyYamlContext context)
        {
            throw new NotImplementedException("SetManagedVars should be replaced at pre-processing time, prior to VT met export");
        }
    }

    public class RegisterManagedVars : AfyYamlAction
    {
        /// <summary>
        /// List value for multiple var names
        /// </summary>
        public List<string> Names { get; set; } = new List<string>();

        /// <summary>
        /// Shorthand for having a singleton list for 'Names'
        /// </summary>
        public string Name
        {
            get
            {
                if (Names.Count != 1)
                    throw new ArgumentException($"Unable to retrieve unique Var Name when there is not exactly one var name registered in action: {string.Join(", ", Names.ToArray())}");
                return Names[0];
            }
        }

        public RegisterManagedVars() : base(AfyActionType.RegisterManagedVars) { }

        public RegisterManagedVars(string name) : this() { Names.Add(name); }

        public RegisterManagedVars(IEnumerable<string> names) : this() { Names.AddRange(names); }

        public override VTAction AsVTAction(AfyYamlContext context)
        {
            throw new NotImplementedException("RegisterManagedVars should be replaced at pre-processing time, prior to VT met export");
        }
    }

    public class ClearManagedVars : AfyZeroArgAction
    {
        public ClearManagedVars() : base(AfyActionType.ClearManagedVars) { }

        public override VTAction AsVTAction(AfyYamlContext context)
        {
            throw new NotImplementedException("ClearManagedVars should be replaced at pre-processing time, prior to VT met export");
        }
    }

    public class DestroyAllViews : AfyZeroArgAction
    {
        public DestroyAllViews() : base(AfyActionType.DestroyAllViews) { }

        public override VTAction AsVTAction(AfyYamlContext context)
        {
            return ADestroyAllViews.Instance;
        }
    }
}