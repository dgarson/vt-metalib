using log4net;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using VTMetaLib.afy;
using VTMetaLib.afy.Model;
using VTMetaLib.VTank;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace VTMetaLib.afy.yaml
{
    public static class AfyConstants
    {
        public const string SetVarsStateName = "**SetVars**";
        public const string InitFinishedStateName = "A_InitFinished";
        public const string InitNextStateName = "A_InitNext";
        public const string DefaultStateName = "Default";

        public const string DebugFeaturesVarName = "DebugFeatures";
        public const string DebugLevelVarName = "Debug";

        public static readonly IList<AfyRule> EmptyRuleList = new List<AfyRule>().AsReadOnly();
        public static readonly IList<AfyState> EmptyStateList = new List<AfyState>().AsReadOnly();
        public static readonly IList<AfyYamlAction> EmptyActionList = new List<AfyYamlAction>().AsReadOnly();
    }

    public static class AfyRules
    {
        public static AfyRule DebugExpr(AfyYamlAction action, int minDebugLevel, string category = null)
        {
            AfyYamlCondition cond = string.IsNullOrEmpty(category) ? new ExprCondition($"${AfyConstants.DebugLevelVarName}>={minDebugLevel}") :
                new AllCondition(new List<AfyYamlCondition> {
                        new ExprCondition($"listcontains[${AfyConstants.DebugFeaturesVarName},`{category}`]"),
                        new ExprCondition($"${AfyConstants.DebugLevelVarName}>={minDebugLevel}")
                    });
            return new AfyRule(cond, action);
        }

        public static AfyRule DebugChat(string rawMessage, int minDebugLevel, string category = null)
        {
            return DebugExpr(new ExprAct($"chatbox[{rawMessage}]"), minDebugLevel, category);
        }

        public static AfyRule DebugThink(string message, int minDebugLevel, string category = null)
        {
            return DebugChat($"`/w `+getcharstringprop[1]+`, {message}`", minDebugLevel, category);
        }

        public static AfyRule Think(string message, AfyYamlCondition condition = null)
        {
            return ChatBox($"`/w `+getcharstringprop[1]+`, {message}`", condition);
        }

        public static AfyRule ChatBox(string message, AfyYamlCondition condition = null)
        {
            return new AfyRule(condition ?? AlwaysCondition.Instance, new ExprAct($"chatbox[{message}]"));
        }
    }

    public class AfyChatMessageBuilder
    {
        private readonly StringBuilder buf = new StringBuilder();
        private readonly bool thinkSelf;

        public AfyChatMessageBuilder(bool thinkSelf = false, string initial = null)
        {
            this.thinkSelf = thinkSelf;
            if (initial != null)
                buf.Append(initial);
        }

        public AfyChatMessageBuilder Append(string txt)
        {
            buf.Append(txt);
            return this;
        }

        public AfyRule Build(AfyCondition condition)
        {
            String message = buf.ToString();
            return new AfyRule(condition, new ExprAct(
                thinkSelf ? $"chatbox[`/w `+getcharstringprop[1]+`, {message}`]" : 
                            $"chatbox[`{message}`]"));
        }
    }

    public static class AfyMetaExtensions
    {
        public static readonly ILog Log = LogManager.GetLogger(typeof(MetaDefinition));

        private static readonly IDeserializer YamlDeserializer = YamlSerialization.CreateDeserializer();

        public static VTMeta AsVTMeta(this MetaDefinition def, AfyYamlContext context = null)
        {
            if (context == null)
                context = new AfyYamlContext();

            // populate context, load up any navs/fragments
            InitializeContext(context, def);

            // Phase 1 : Go through everything and collect things like:
            //              i. init states from state/meta fragments
            //             ii. FUTURE: collect all names of managedvars and map in a Dictionary[StateName=>List[ManagedVarName]]
            // Phase 2 : Validate and expand States into AfyRule list
            //              i. Validate anything in the deserialized YAML model before proceeding to use it
            //             ii. Expand to Dictionary<string, List<AfyRule>> for the states
            //            iii. Resolve to Dictionary<string, List<AfyRule>> from the StateEntry list (which could have imports)
            AfyMeta meta = context.LoadAfyMeta(def);
            Log.Info($"Resolved AfyMeta '{def.Name}' with {meta.States.Count} states and {meta.NavRoutes.Count} nav routes");

            // Phase 3 : Convert to VT MetaLib Models
            VTMeta vtMeta = new VTMeta();
            foreach (var state in meta.States)
            {
                List<VTRule> stateRules = state.Value.AsVTRules(context);
                foreach (VTRule rule in stateRules)
                    vtMeta.AddRuleToState(rule);
            }
            return vtMeta;
        }

        internal static List<VTRule> AsVTRules(this AfyState state, AfyYamlContext context)
        {
            return state.Rules.Select(r => r as AfyRule).Select(r => r.AsVTRule(context)).ToList();
        }

        internal static VTRule AsVTRule(this AfyRule rule, AfyYamlContext context)
        {
            VTCondition cond = rule.Condition.AsVTCondition(context);
            VTAction action = rule.Action.AsVTAction(context);
            VTRule vtRule = new VTRule(rule.State.Name, cond, action);
            // TODO: logging, metrics/counters?
            return vtRule;
        }

        internal static AfyMeta LoadAfyMeta(this AfyYamlContext context, MetaDefinition def)
        {
            AfyMeta meta = new AfyMeta(def.Name);
            if (def.Metadata != null)
            {
                foreach (var kvp in def.Metadata)
                    meta.Metadata.Add(kvp.Key, kvp.Value);
            }
            
            if (context.InitState != null)
            {
                Log.Info($"Using InitState: {context.InitState.Name}");

                // Register explicitly declared InitState names
                if (def.InitStates != null && def.InitStates.Count > 0)
                {
                    foreach (string initStateName in def.InitStates)
                    {
                        context.RegisterInitState(initStateName);
                        // these states are user-defined in their meta/afy, so they are not registered here, as they are parsed
                        // out of the afy file itself
                    }
                }

                // Generate and register support InitStates for fragments/imports
                IEnumerable<AfyState> initStates = context.GenerateInitStates();
                foreach (AfyState initState in initStates)
                {
                    context.RegisterInitState(initState.Name);
                    context.AddState(initState);
                }
            }

            // Register all the fragments from the MetaDefinition
            Log.Debug($"Processing {context.StateFragments.Count} State Fragments and {context.MetaFragments.Count} Meta Fragments");
            context.ProcessFragments();

            // Dictionary<string, List<AfyRule>> stateRules = new Dictionary<string, List<AfyRule>>();
            Dictionary<string, AfyState> renderedStates = new Dictionary<string, AfyState>();
            foreach (AfyState stateDef in def.States)
            {
                AfyState renderedState = context.RenderMetaState(meta, stateDef);
                if (!renderedStates.TryAdd(renderedState.Name, renderedState))
                {
                    Log.Error($"Attempted to register duplicate state with name '{renderedState.Name}'");
                    throw new YamlException($"Attempted to register duplicate state with name '{renderedState.Name}'");
                }
                Log.Info($"Rendered Meta State: {renderedState.Name}");
                context.AddState(renderedState);
            }

            // Validate existence of all registered InitStates
            foreach (string initStateName in context.InitStateNames)
            {
                if (!context.StateNames.Contains(initStateName))
                    throw new YamlException($"Missing InitState '{initStateName}' after rendering all States in the afy file and the imported fragments");
            }

            // copy over part of the AfyYamlContext to the AfyMeta model
            foreach (var state in context.States.Values)
                meta.States.Add(state.Name, state);

            // post-process all meta states (to fill in the 'State' backreference in AfyRule)
            context.PostProcessStates(meta.States.Values);

            // copy over nav routes
            foreach (var navRoute in context.NavRoutes)
                meta.NavRoutes.Add(navRoute.Key, navRoute.Value);
            
            // return constructed/processed meta model
            return meta;
        }

        internal static void PostProcessStates(this AfyYamlContext context, IEnumerable<AfyState> states)
        {
            foreach (var state in states)
            {
                foreach (AfyRule entry in state.Rules)
                {
                    AfyRule rule = entry as AfyRule;
                    rule.State = state;
                }
            }
        }

        /// <summary>
        /// Process the FragmentDefinitions popoulate in the AfyYamlContext from the InitializeContext method.
        /// </summary>
        internal static void ProcessFragments(this AfyYamlContext context)
        {
            foreach (StateFragmentDefinition stateFrag in context.StateFragments.Values)
            {
                string fragName = stateFrag.Name;
                if (!string.IsNullOrEmpty(stateFrag.FragmentInitState))
                {
                    ImportStateFragmentDefinition importDef = context.StateFragmentImports[fragName];
                    if (context.InitState == null)
                        throw new YamlException($"No top-level Meta 'InitState' property defined. Unable to register InitState '{stateFrag.FragmentInitState}` for State Fragment '{stateFrag.Name}' loaded from {importDef.Path}");

                    Log.Info($"Registering InitState '{stateFrag.FragmentInitState}' for State Fragment '{stateFrag.Name}' loaded from {importDef.Path}");
                    context.RegisterInitState(stateFrag.FragmentInitState);
                }
                foreach (AfyNavRouteDefinition routeDef in stateFrag.NavRoutes)
                {
                    context.LoadNavRoute(routeDef);
                }
            }

            foreach (MetaFragmentDefinition metaFrag in context.MetaFragments.Values)
            {
                string fragName = metaFrag.Name;
                if (!string.IsNullOrEmpty(metaFrag.FragmentInitState))
                {
                    ImportMetaFragmentDefinition importDef = context.MetaFragmentImports[fragName];
                    if (context.InitState == null)
                        throw new YamlException($"No top-level Meta 'InitState' property defined. Unable to register InitState '{metaFrag.FragmentInitState}` for Meta Fragment '{metaFrag.Name}' loaded from {importDef.Path}");

                    Log.Info($"Registering InitState '{metaFrag.FragmentInitState}' for Meta Fragment '{metaFrag.Name}' loaded from {importDef.Path}");
                    context.RegisterInitState(metaFrag.FragmentInitState);
                }
            }
        }

        internal static AfyState RenderMetaState(this AfyYamlContext context, AfyMeta meta, AfyState stateDef)
        {
            AfyState state = new AfyState(stateDef.Name);
            foreach (AfyRule ruleDef in stateDef.Rules)
            {
                if (ruleDef is AfyRule rd)
                {
                    AfyRule resolvedRule = context.ResolveRule(meta, state, stateDef.Name, rd, context.StateNames);
                    state.Rules.Add(resolvedRule);
                }
                else
                {
                    IList<AfyRule> childRuleDefs = ruleDef.GetRules(context);
                    foreach (var childRuleDef in childRuleDefs)
                    {
                        AfyRule resolvedChildRule = context.ResolveRule(meta, state, stateDef.Name, childRuleDef, context.StateNames);
                        state.Rules.Add(resolvedChildRule);
                    }
                    // log state entries that do not resolve one-to-one with an actual VT Meta Rule
                    if (childRuleDefs.Count != 1)
                        Log.Info($"Resolved {childRuleDefs.Count} rules for state entry of type {ruleDef.GetType().Name}: {ruleDef}");
                }
                Log.Debug($"Processed rule #{state.Rules.Count} of type {ruleDef.GetType().Name} in State '{stateDef.Name}'");
            }
            return state;
        }
        
        internal static void RenderStatesFor(this AfyYamlContext context, AfyStateContainer stateContainer)
        {
            // TODO render states w/Go Templating
            var states = stateContainer.GetStates(context);
            string prefix = stateContainer.StateNamePrefix ?? "";
            HashSet<string> stateContainerStateNames = states.Select(s => s.Name).ToHashSet();
            List<AfyState> realStateList = new List<AfyState>();
            foreach (AfyState stateDef in states)
            {
                string origStateName = stateDef.Name;
                AfyState state = new AfyState(prefix + origStateName);

                // first pass to preprocess all rule declarations to collect information for expansion of AFY special action types (e.g. ClearManagedVars)
                context.PreProcessRules(state, origStateName, stateDef.Rules);

                // second pass to iterate over each rule definition and add its transformed/resolved Rule(s) to the rendered state
                foreach (AfyStateEntry ruleDef in stateDef.Rules)
                {
                    if (ruleDef is AfyRule rd)
                    {
                        AfyRule resolvedRule = context.ResolveRule(stateContainer, state, origStateName, rd, stateContainerStateNames);
                        state.Rules.Add(resolvedRule);
                    }
                    else
                    {
                        foreach (var childRuleDef in ruleDef.GetRules(context))
                        {
                            AfyRule resolvedChildRule = context.ResolveRule(stateContainer, state, origStateName, childRuleDef, stateContainerStateNames);
                            state.Rules.Add(resolvedChildRule);
                        }
                    }
                }

                context.AppendState(realStateList, state);
            }
        }

        private static void PreProcessRules(this AfyYamlContext context, AfyState state, string origStateName, IList<AfyRule> entries)
        {
            foreach (AfyRule entry in entries)
            {
                if (entry is AfyRule)
                    context.PreProcessRule(state, origStateName, entry as AfyRule);
                else
                {
                    IList<AfyRule> ruleEntries = new List<AfyRule>();
                    foreach (AfyRule r in entry.GetRules(context))
                        ruleEntries.Add(r);
                    context.PreProcessRules(state, origStateName, ruleEntries);
                }
            }
        }

        private static void PreProcessRule(this AfyYamlContext context, AfyState state, string origStateName, AfyRule unrenderedEntry)
        {
            var action = unrenderedEntry.Action;
            if (action is EmbedNav embedNav)
            {
                if (!context.NavRoutes.ContainsKey(embedNav.Name))
                    throw new YamlException($"Referenced undefined nav route with name: {embedNav.Name}");
            }
            else if (action is SetManagedVars setVars)
            {
                StringBuilder sb = new StringBuilder();
                foreach (var varDef in setVars.Vars)
                    state.ManagedVars.Add(varDef.Name);
                Log.Info($"Registered ManagedVars: {string.Join(", ", setVars.Vars.Select(x => x.Name).ToArray())}");
            }
            else if (action is RegisterManagedVars regVars)
            {
                foreach (var name in regVars.Names)
                    state.ManagedVars.Add(name);
                Log.Info($"Registered ManagedVars: {string.Join(", ", regVars.Names.ToArray())}");
            }
        }

        private static AfyRule ResolveRule(this AfyYamlContext context, AfyStateContainer parent, AfyState state, string origStateName, AfyRule unrenderedEntry, ICollection<string> parentDefinedStateNames)
        {
            var renderedRule = (AfyRule)unrenderedEntry.Clone();
            var action = renderedRule.Action;

            if (action is SetState setState)
            {
                string origName = setState.State;
                setState.State = context.ResolveStateName(parent, origName, parentDefinedStateNames);
                if (origName != setState.State)
                    Log.Info($"Resolved State[{origName} => {setState.State}] for rule in State '{state.Name}'");

            }
            else if (action is CallState callState)
            {
                string origState = callState.State;
                string origReturnTo = callState.ReturnTo;
                callState.State = context.ResolveStateName(parent, origState, parentDefinedStateNames);
                callState.ReturnTo = context.ResolveStateName(parent, origReturnTo, parentDefinedStateNames);
                if (origState != callState.State || origReturnTo != callState.ReturnTo)
                    Log.Info($"Resolved State[{origState} => {callState.State}] and ReturnToState[{origReturnTo} => {callState.ReturnTo}] for rule in State '{state.Name}'");
            }
            else if (action is ClearFragmentVars)
            {
                throw new InvalidOperationException($"ClearFragmentVars used in state '{state.Name}' is not yet supported");
            }
            else if (action is SetManagedVars setVars)
            {
                string exprStr = string.Join(';', setVars.Vars.Select(v => $"setvar[{v.Name}, {context.ResolveVarDefValue(state, v)}]"));
                Log.Info($"Expanded SetManagedVars[{string.Join(", ", setVars.Vars.Select(x => x.Name).ToArray())}] into an ExprAct:\n\t{exprStr}");
                renderedRule.Action = new ExprAct(exprStr);
            }
            else if (action is RegisterManagedVars regVars)
            {
                // no-op: fully handled in preprocessing
            }
            else if (action is ClearManagedVars)
            {
                string exprStr = string.Join(';', state.ManagedVars.Select(v => $"clearvar[{v}]"));
                renderedRule.Action = new ExprAct(exprStr);
                Log.Info($"Expanded ClearManagedVars for State '{state.Name}' into ExprAct:\n\t{exprStr}");
            }
            else
            {
                Log.Debug($"No Action transformations needed for action type: {action.ActionType}");
            }

            // return the cloned rule with a possibly-transformed AfyAction element
            return renderedRule;
        }

        private static string ResolveVarDefValue(this AfyYamlContext context, AfyState state, FragmentVarDefinition varDef)
        {
            if (varDef.ValueList != null)
            {
                StringBuilder buf = new StringBuilder();
                foreach (var listEl in varDef.ValueList)
                {
                    if (buf.Length > 0)
                        buf.Append(',');
                    // must already be quoted if string is desired
                    buf.Append(listEl);
                }
                return $"listcreate[{buf.ToString()}]";
            }
            else if (varDef.ValueRef != null)
            {
                throw new InvalidOperationException($"ValueRef not yet supported for var: {varDef.Name}");
            }
            else if (varDef.Value != null)
            {
                return varDef.Value;
            }
            else
                throw new ArgumentException($"Unable to use var '{varDef.Name}' without any Value, ValueList, or ValueRef properties!");
        }

        private static string ResolveStateName(this AfyYamlContext context, AfyStateContainer parent, string origStateName, ICollection<string> parentDefinedStateNames = null)
        {
            if (parent == null || string.IsNullOrEmpty(parent.StateNamePrefix))
                return origStateName;

            // resolve the overall set of states defined in parent, if not provided (optimizes for multiple calls to this method)
            if (parentDefinedStateNames == null)
                parentDefinedStateNames = parent.GetStates(context).Select(s => s.Name).ToHashSet();

            // if no states in parent, then don't bother checking for names
            if (parentDefinedStateNames == null || parentDefinedStateNames.Count == 0)
                return origStateName;

            // if the state is defined inside the parent container, prefix it
            return parent.StateNamePrefix + origStateName;
        }


        private static void AppendState(this AfyYamlContext context, List<AfyState> states, AfyState state)
        {
            if (context.States.ContainsKey(state.Name))
            {
                Log.Error($"Attempted to append state '{state.Name}' but it is already in use!");
                throw new ArgumentException($"Attempted to append state '{state.Name}' but it is already in use!");
            }
            context.States[state.Name] = state;
        }

        public static MetaFragmentDefinition LoadMetaFragment(this AfyYamlContext context, string fragmentPath)
        {
            string contents = ReadFileFromPath("MetaFragment", fragmentPath);
            try
            {
                return context.ParseMetaFragment(contents);
            }
            catch (Exception e)
            {
                throw new IOException($"Unable to parse MetaFragmentDefinition from file: {fragmentPath}", e);
            }
        }

        public static MetaFragmentDefinition ParseMetaFragment(this AfyYamlContext context, string fragmentYaml)
        {
            using (var reader = Yaml.ReaderForText(fragmentYaml))
            {
                IParser parser = new Parser(reader);
                try
                {
                    return YamlDeserializer.Deserialize<MetaFragmentDefinition>(parser);
                }
                catch (Exception e)
                {
                    throw new YamlException($"Failed to import MetaFragmentDefinition from path YAML:\n{fragmentYaml}", e);
                }
            }
        }

        public static StateFragmentDefinition LoadStateFragment(this AfyYamlContext context, string fragmentPath)
        {
            string contents = ReadFileFromPath("StateFragment", fragmentPath);
            try
            {
                return context.ParseStateFragment(contents);
            }
            catch (Exception e)
            {
                throw new IOException($"Unable to parse StateFragmentDefinition from file: {fragmentPath}", e);
            }
        }

        public static StateFragmentDefinition ParseStateFragment(this AfyYamlContext context, string fragmentYaml)
        {
            using (var reader = Yaml.ReaderForText(fragmentYaml))
            {
                IParser parser = new Parser(reader);
                try
                {
                    return YamlDeserializer.Deserialize<StateFragmentDefinition>(parser);
                }
                catch (Exception e)
                {
                    throw new YamlException($"Failed to import StateFragmentDefinition from path YAML:\n{fragmentYaml}", e);
                }
            }
        }

        internal static void InitializeContext(this AfyYamlContext context, MetaDefinition def)
        {
            if (context.MetaDefinition == null)
                context.MetaDefinition = def;

            var deserializer = context.YamlDeserializer;
            if (deserializer == null)
                deserializer = context.YamlDeserializer = YamlSerialization.CreateDeserializer();

            if (!string.IsNullOrEmpty(def.InitState))
            {
                Log.Info($"Generated InitStates for {def.InitState}");
                context.InitState = new AfyState(def.InitState);
                Log.Debug($"Using InitState with name {def.InitState}");
            }

            if (def.Metadata != null)
            {
                foreach (var entry in def.Metadata)
                    context.Metadata[entry.Key] = entry.Value;
            }

            if (def.Imports != null)
            {
                if (def.Imports.MetaFragments != null && def.Imports.MetaFragments.Count > 0)
                {
                    foreach (ImportMetaFragmentDefinition importMetaFragDef in def.Imports.MetaFragments)
                    {
                        MetaFragmentDefinition metaFrag = context.LoadMetaFragment(importMetaFragDef.Path);
                        if (!context.MetaFragments.TryAdd(metaFrag.Name, metaFrag))
                            throw new YamlException($"Duplicate MetaFragment imported: {metaFrag.Name}");
                        context.MetaFragmentImports.Add(metaFrag.Name, importMetaFragDef);
                        Log.Debug($"Loaded meta fragment #{context.MetaFragments.Count} with {metaFrag.States.Count} States and {metaFrag.StateSections.Count} State Fragment Sections from file: {importMetaFragDef.Path}");
                    }
                    Log.Info($"Loaded Meta Fragments: {string.Join(", ", context.MetaFragments.Select(kv => kv.Value.Name).ToArray())}");
                }
                if (def.Imports.StateFragments != null && def.Imports.StateFragments.Count > 0)
                {
                    foreach (ImportStateFragmentDefinition importStateFragDef in def.Imports.StateFragments)
                    {
                        StateFragmentDefinition stateFrag = context.LoadStateFragment(importStateFragDef.Path);
                        if (!context.StateFragments.TryAdd(stateFrag.Name, stateFrag))
                            throw new YamlException($"Duplicate StateFragment imported: {stateFrag.Name}");
                        context.StateFragmentImports.Add(stateFrag.Name, importStateFragDef);
                        Log.Debug($"Loaded state fragment #{context.StateFragments.Count} with {stateFrag.Sections.Count} sections from file: {importStateFragDef.Path}");
                    }
                    Log.Info($"Loaded State Fragments: {string.Join(", ", context.StateFragments.Select(kv => kv.Value.Name).ToArray())}");
                }
            }

            Log.Debug($"Loading {def.NavRoutes.Count} defined Nav Routes");
            foreach (AfyNavRouteDefinition navRt in def.NavRoutes)
            {
                Log.Debug($"Loading Nav Route '{navRt.Name}' from Definition: {navRt}");
                if (!context.NavRoutes.TryAdd(navRt.Name, navRt))
                {
                    Log.Error($"Duplicate Nav Route defined: {navRt.Name}");
                    if (navRt.Path != null)
                        throw new YamlException($"Imported Nav Route is a duplicate of another Nav Route with name '{navRt.Name}', defined in file: {navRt.Path}");
                    else
                        throw new YamlException($"Inlined Nav Route is a duplicate of another Nav Route with name '{navRt.Name}'");
                }

                VTNavRoute vtNavRoute = LoadNavRoute(context, navRt);
                Log.Info($"Loaded nav route with name '{navRt.Name}` of type {navRt.Type}");
                context.NativeNavRoutes.Add(navRt.Name, vtNavRoute);
            }
        }

        /*
        private static AfyState GenerateDefaultState(AfyContext context)
        {
            AfyState defState = new AfyState(AfyConstants.DefaultStateName);
            // TODO config default enabled to include this?
            // defState.Rules.Add(new AfyRule(AlwaysCondition.Instance, new ExprAct("clearallvars[]")));
            defState.Rules.Add(new AfyRule(AlwaysCondition.Instance, new SetState(AfyConstants.SetVarsStateName)));
            return defState;
        }
        */

        private static IEnumerable<AfyState> GenerateInitStates(this AfyYamlContext context)
        {
            if (string.IsNullOrEmpty(context.MetaDefinition.InitState))
                return null;

            AfyState initState = new AfyState(context.MetaDefinition.InitState);
            initState.Rules.Add(new AfyRule(new ExprCondition("$init_counter>=listcount[$InitStates]"), new SetState(AfyConstants.InitFinishedStateName)));
            initState.Rules.Add(new AfyRule(AlwaysCondition.Instance, new ExprAct("vtsetmetastate[listgetitem[$InitStates,$init_counter]]")));

            AfyState initNextState = new AfyState(AfyConstants.InitNextStateName);
            initNextState.Rules.Add(new AfyRule(AlwaysCondition.Instance, new ExprAct("setvar[init_counter, getvar[init_counter]+1]")));
            initNextState.Rules.Add(new AfyRule(AlwaysCondition.Instance, new SetState(context.MetaDefinition.InitState)));

            AfyState initFinishedState = new AfyState(AfyConstants.InitFinishedStateName);
            initFinishedState.Rules.Add(new AfyRule(AlwaysCondition.Instance, new ExprAct("clearvar[init_counter]")));
            initFinishedState.Rules.Add(new AfyRule(AlwaysCondition.Instance, new SetState(AfyConstants.SetVarsStateName)));

            return new List<AfyState> { initState, initNextState, initFinishedState };
        }

        internal static VTNavRoute Transform(this VTNavRoute baseRoute, AfyNavRouteDefinition routeDef, RouteTransform transform, bool reversePoints)
        {
            // TODO implement route transformation!!
            // NavRoutes.TransformRoute(
            // ....
            if (reversePoints || transform != null)
                Log.Warn($"Nav Route '{routeDef.Name}' uses ReversePoints or Transform but those are not yet supported. They will be ignored.");

            return baseRoute;
        }

        internal static VTNavRoute FindNavRoute(this AfyYamlContext context, string routeName)
        {
            if (!context.NativeNavRoutes.TryGetValue(routeName, out var vtRoute))
            {
                Log.Warn($"Caught undefined NavRoute[{routeName}] but not until Resolution time - should have been caught before!");
                throw new YamlException($"Referenced undefined nav route: {routeName}");
            }
            return vtRoute;
        }

        private static VTNavRoute LoadNavRoute(this AfyYamlContext context, AfyNavRouteDefinition routeDef)
        {
            // TODO handle duplicate nav route definitions in different part of meta (e.g. state fragment vs root meta definition)
            if (!context.NativeNavRoutes.TryGetValue(routeDef.Name, out var vtRoute))
            {
                switch (routeDef.Type)
                {
                    case AfyNavRouteType.File:
                        {
                            vtRoute = NavRoutes.LoadNavRoute(routeDef.Path);
                            break;
                        }
                    case AfyNavRouteType.Data:
                        {
                            vtRoute = NavRoutes.ParseEmbeddedNavRoute(routeDef.Data);
                            break;
                        }
                    default:
                        throw new ArgumentException($"Unsupported NavRoute type: {routeDef.Type}");
                }

                // register both the native and afy definition of nav route
                context.NavRoutes.Add(routeDef.Name, routeDef);
                context.NativeNavRoutes.Add(routeDef.Name, vtRoute);
            }
            return vtRoute;
        }
        
        private static string ReadFileFromPath(string desc, string filePath)
        {
            // TODO possible directory prepending etc
            try
            {
                return File.ReadAllText(filePath);
            }
            catch (IOException e)
            {
                throw new ArgumentException($"Unable to load {desc} from path: {filePath}", e);
            }
        }
    }

    

    /// <summary>
    /// YAML model for the top-level Meta entity
    /// </summary>
    public class MetaDefinition
    {
        /// <summary>
        /// required string name for this meta
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// optional map of key-value pairs used exclusively for metadata and automatically included in the templateable variables, under the
        /// `.Meta.Metadata` property.
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// State name that will be used whenever there is a need to 'reset' the entire meta. If a state other than `Default` should be used, 
        /// then this property must be provided.
        /// 
        /// This can be referenced from any template using `.Meta.ResetState`
        /// </summary>
        public string ResetState { get; set; } = "Default";

        /// <summary>
        /// optional initialization state name that, if provided, will enable InitState management for use in Fragments, and will generate a synthetic
        /// state with this name, which will be used to iterate over decorated `InitStates` that are used in the meta.
        /// </summary>
        public string InitState { get; set; } = "";

        /// <summary>
        /// optional list of `InitStates` that will be called during the execution of the declared `InitState`, which is REQUIRED if this list has any
        /// elements at all. 
        /// 
        /// NOTE: this list *MAY* be empty even if `InitState` is defined, for exclusive use of `InitStates` for imported fragments, which auto-register
        /// their own initialization state in the `InitStates` list, during parsing/generation time
        /// </summary>
        public List<string> InitStates { get; set; } = new List<string>();

        /// <summary>
        /// Imported non-nav files, specifically State and Meta Fragments
        /// </summary>
        public MetaImports Imports { get; set; }

        /// <summary>
        /// optional list of named nav routes that can be referenced in any `EmbedNav` Action.
        /// </summary>
        public List<AfyNavRouteDefinition> NavRoutes { get; set; } = new List<AfyNavRouteDefinition>();

        /// <summary>
        /// List of key-value pairs that each describe a State within the meta. Each state will be a mapping entry that includes a `Name`
        /// key as well as a `Rules` key with list items.
        /// </summary>
        public List<AfyState> States { get; set; } = new List<AfyState>();

    }

    public class MetaImports
    {
        /// <summary>
        /// optional list of state fragments being used in this meta
        /// </summary>
        public List<ImportStateFragmentDefinition> StateFragments { get; set; } = new List<ImportStateFragmentDefinition>();

        /// <summary>
        /// optional list of meta fragments that will be imported into the meta at generation time
        /// </summary>
        public List<ImportMetaFragmentDefinition> MetaFragments { get; set; } = new List<ImportMetaFragmentDefinition>();
    }

    public class AfyMeta : AfyStateContainer
    {
        public string Name { get; private set; }

        public Dictionary<string, string> Metadata { get; internal set; } = new Dictionary<string, string>();

        public Dictionary<string, AfyState> States { get; } = new Dictionary<string, AfyState>();

        public Dictionary<string, AfyNavRouteDefinition> NavRoutes { get; } = new Dictionary<string, AfyNavRouteDefinition>();

        public Dictionary<string, VTNavRoute> NativeNavRoutes { get; } = new Dictionary<string, VTNavRoute>();

        public AfyMeta(string name) { Name = name; }

        public string StateNamePrefix => null;

        public IList<AfyState> GetStates(AfyYamlContext context)
        {
            return States.Values.ToList();
        }

        public IList<AfyYamlAction> GetStateTransitionActions(AfyYamlContext context, object sourceYamlDef)
        {
            return AfyConstants.EmptyActionList;
        }
    }

    public class AfyState
    {
        public string Name { get; set; }

        public List<AfyRule> Rules { get; set; } = new List<AfyRule>();

        [YamlIgnore]
        internal HashSet<string> ManagedVars { get; } = new HashSet<string>();

        public AfyState(string name) { Name = name; }

        public AfyState() { }
    }

    public class AfyRule : AfyStateEntry
    {
        public AfyCondition Condition { get; set; }

        public AfyAction Action { get; set; }

        // set later
        public AfyState State { get; set; }

        private readonly ReadOnlyCollection<AfyRule> thisList;

        internal AfyRule(AfyCondition condition, AfyAction action) : this()
        {
            Condition = condition;
            Action = action;
        }

        internal AfyRule(AfyAction directive, AfyState state) : this()
        {
            Condition = AlwaysCondition.Instance;
            Action = directive;
            State = state;
        }

        public AfyRule()
        {
            thisList = new List<AfyRule> { this }.AsReadOnly();
        }

        public IList<AfyRule> GetRules(AfyYamlContext context)
        {
            return thisList;
        }

        public AfyRule Clone()
        {
            return (AfyRule)MemberwiseClone();
        }
    }

    /// <summary>
    /// Container interface implemented by both the standard Meta and by Fragments. It returns one or more entries that each expand into
    /// one or more rules, composing the resulting state that will be exported to the VT Native Meta format.
    /// </summary>
    public interface AfyStateContainer
    {
        public string StateNamePrefix { get; }

        /// <summary>
        /// Returns a list of the States inside this container, which will container unqualified names and will not include any actions/rules to
        /// handle state transitions/events, etc.
        /// </summary>
        public IList<AfyState> GetStates(AfyYamlContext context);

        /// <summary>
        /// Returns zero or more actions that should be executed when a state transition will occur in any states rendered for this container
        /// </summary>
        public IList<AfyYamlAction> GetStateTransitionActions(AfyYamlContext context, object sourceYamlDef);
    }

    /// <summary>
    /// Interface for the entry in a Meta State, so that it can be expanded into many rules, if necessary, when importing fragments or
    /// using an afy-specific action type that expands to one or more transformed rules.
    /// </summary>
    public interface AfyStateEntry
    {
        public IList<AfyRule> GetRules(AfyYamlContext context);
    }

    internal class AfyRuleListEntry : AfyStateEntry
    {
        private readonly ReadOnlyCollection<AfyRule> ruleList;

        public AfyRuleListEntry(IEnumerable<AfyRule> rules)
        {
            ruleList = new List<AfyRule>(rules).AsReadOnly();
        }

        public IList<AfyRule> GetRules(AfyYamlContext context)
        {
            return ruleList;
        }
    }
}
