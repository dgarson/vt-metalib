using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VTMetaLib.afy.yaml;
using VTMetaLib.VTank;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace VTMetaLib.afy
{
    public class AfyContext
    {
        #region Metadata
        public string Name { get => MetaDefinition.Name; }

        public Dictionary<string, string> Metadata { get; internal set; } = new Dictionary<string, string>();

        #endregion Metadata

        #region Yaml Parsing

        public IDeserializer YamlDeserializer { get; internal set; }

        #endregion Yaml Parsing

        #region Source YAML Definitions

        public MetaDefinition MetaDefinition { get; internal set; }

        public Dictionary<string, MetaFragmentDefinition> MetaFragments { get; } = new Dictionary<string, MetaFragmentDefinition>();

        public Dictionary<string, ImportMetaFragmentDefinition> MetaFragmentImports { get; } = new Dictionary<string, ImportMetaFragmentDefinition>();

        public Dictionary<string, StateFragmentDefinition> StateFragments { get; } = new Dictionary<string, StateFragmentDefinition>();

        public Dictionary<string, ImportStateFragmentDefinition> StateFragmentImports { get; } = new Dictionary<string, ImportStateFragmentDefinition>();

        public Dictionary<string, AfyNavRouteDefinition> NavRoutes { get; } = new Dictionary<string, AfyNavRouteDefinition>();

        public bool HasFragment(string fragDefName)
        {
            return MetaFragments.ContainsKey(fragDefName) || StateFragments.ContainsKey(fragDefName);
        }

        #endregion Source YAML Definitions

        #region States 
        public Dictionary<string, AfyState> States { get; internal set; } = new Dictionary<string, AfyState>();

        public HashSet<string> StateNames { get => States.Keys.ToHashSet(); }

        public AfyState InitState { get; internal set; } // = null;

        public HashSet<string> InitStateNames { get; } = new HashSet<string>();
        #endregion States 

        #region VTank Native

        public Dictionary<string, VTNavRoute> NativeNavRoutes { get; } = new Dictionary<string, VTNavRoute>();

        #endregion VTank Native

        #region Rendering State

        public Stack<string> RenderingStateNames { get; } = new Stack<string>();

        public string RenderingStateName { get => RenderingStateNames.Count > 0 ? RenderingStateNames.Peek() : null; }

        public Stack<string> RenderingFragmentNames { get; } = new Stack<string>();

        public string CurrentFragmentName { get => RenderingFragmentNames.Count > 0 ? RenderingFragmentNames.Peek() : null; }

        public Stack<string> RenderingSectionNames { get; } = new Stack<string>();

        public Stack<bool> RenderingSectionBeganFragment { get; } = new Stack<bool>();

        public string CurrentFragmentSection { get => RenderingSectionNames.Count > 0 ? RenderingSectionNames.Peek() : null; }

        #endregion Rendering State

        public void RegisterInitState(string auxInitStateName)
        {
            if (InitState == null)
                throw new YamlException($"Unable to register InitState '{auxInitStateName}' because InitState not defined for Meta: {Name}");
            if (!InitStateNames.Add(auxInitStateName))
                Loggers.Log.Warn($"Registered InitState that is already in InitStateNames list: {auxInitStateName}");
        }

        public void AddState(AfyState state)
        {
            if (!StateNames.Add(state.Name))
                throw new ArgumentException($"Unable to add state with duplicate name: {state.Name}");
            States.Add(state.Name, state);
        }

        #region Meta Rendering Operations

        public void BeginRenderingState(string stateName)
        {
            RenderingStateNames.Push(stateName);
        }

        public string FinishRenderingState(string stateName)
        {
            string renderingState = RenderingStateName;
            if (renderingState != stateName)
                throw new YamlException($"Unable to finish rendering state '{stateName}' because the state being rendered is currently: {renderingState}");

            // pop from stack
            RenderingStateNames.Pop();

            // return the now-current state being rendered, after finishing the provided 'stateName'
            return RenderingStateName;
        }

        public void BeginRenderingFragment(string fragmentDefName, bool autoBeganFrag = false)
        {
            if (!HasFragment(fragmentDefName))
                throw new YamlException($"Unable to render unregistered fragment: {fragmentDefName}");

            RenderingFragmentNames.Push(fragmentDefName);
            RenderingSectionBeganFragment.Push(autoBeganFrag);
        }

        public string FinishRenderingFragment(string fragmentDefName)
        {
            string renderingFrag = CurrentFragmentName;
            if (renderingFrag != fragmentDefName)
                throw new YamlException($"Unable to finish rendering fragment '{fragmentDefName}' because the state being rendered is currently: {renderingFrag}");

            // pop from stack
            RenderingFragmentNames.Pop();

            // return the now-current fragment being rendered, after finishing the provided 'fragmentDefName'
            return CurrentFragmentName;
        }

        public void BeginRenderingFragmentSection(string fragmentDefName, string fragSection)
        {
            string renderingFrag = CurrentFragmentName;
            string renderingSection = CurrentFragmentSection;

            // automatically begin rendering this fragment if not already at top of stack
            if (renderingFrag != fragmentDefName)
                BeginRenderingFragment(fragmentDefName, true);

            // 
            RenderingSectionNames.Push(fragSection);
        }

        public string FinishRenderingFragmentSection(string fragSection)
        {
            string renderingSection = CurrentFragmentSection;
            if (renderingSection != null && renderingSection != fragSection)
                throw new YamlException($"Unable to finish rendering fragment section '{fragSection}' because current section is '{CurrentFragmentName}' is '{renderingSection}'");
            else if (renderingSection == null)
                throw new YamlException($"Unable to finish rendering fragment section '{fragSection}' when it is not already begin rendered");

            // pop from stack
            RenderingSectionNames.Pop();
            bool autoBeganFrag = RenderingSectionBeganFragment.Pop();

            // automatically wrap up rendering of this fragment if we automatically began it to render this section!
            if (autoBeganFrag)
                FinishRenderingFragment(CurrentFragmentName);
                
            // return the now-current fragment being rendered, after finishing the provided 'fragmentDefName'
            return CurrentFragmentSection;
        }

        #endregion Meta Rendering Operations
    }
}
