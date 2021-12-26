using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using VTMetaLib.afy;
using VTMetaLib.afy.Model;
using YamlDotNet.Core.Events;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace VTMetaLib.afy.yaml
{
    public class Meta
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
        /// optional list of state fragments being used in this meta
        /// </summary>
        public List<StateFragmentDefinition> StateFragments { get; set; } = new List<StateFragmentDefinition>();

        /// <summary>
        /// optional list of meta fragments that will be imported into the meta at generation time
        /// </summary>
        public List<MetaFragmentDefinition> MetaFragments { get; set; } = new List<MetaFragmentDefinition>();

        /// <summary>
        /// optional list of named nav routes that can be referenced in any `EmbedNav` Action.
        /// </summary>
        // TODO NodeTypeResolver
        public List<AfyNavRouteDefinition> NavRoutes { get; set; } = new List<AfyNavRouteDefinition>();

        /// <summary>
        /// List of key-value pairs that each describe a State within the meta. Each state will be a mapping entry that includes a `Name`
        /// key as well as a `Rules` key with list items.
        /// </summary>
        public List<AfyState> States { get; set; } = new List<AfyState>();

    }

    public class AfyState
    {
        public string Name { get; set; }

        // TODO NodeTypeResolver
        public List<AfyStateEntry> Rules { get; internal set; } = new List<AfyStateEntry>();
    }

    public class AfyRule 
    {
        public AfyCondition Condition { get; private set; }

        public AfyAction Action { get; private set; }

        // set later
        public AfyState State { get; internal set; }

        internal AfyRule(AfyCondition condition, AfyAction action, AfyState state = null)
        {
            Condition = condition;
            Action = action;
            State = state;
        }

        internal AfyRule(AfyAction directive, AfyState state)
        {
            Condition = AlwaysCondition.Instance;
            Action = directive;
            State = state;
        }
    }

    public interface AfyStateEntry
    {
        public IList<AfyRule> GetRules(Meta meta, AfyYamlContext context);
    }

    internal class AfyStateEntryNodeTypeResolver : INodeTypeResolver
    {
        public bool Resolve(NodeEvent nodeEvent, ref Type currentType)
        {
            throw new NotImplementedException();
        }
    }

    internal abstract class DirectiveStateEntry : AfyStateEntry
    {
        public AfyActionType Directive { get; private set; }

        protected DirectiveStateEntry(AfyActionType directive)
        {
            Directive = directive;
        }

        public abstract IList<AfyRule> GetRules(Meta meta, AfyYamlContext context);
    }

    internal class OneToOneRuleEntry : AfyStateEntry
    {
        private readonly ReadOnlyCollection<AfyRule> ruleList;

        public AfyRule Rule { get; private set; }

        public OneToOneRuleEntry(AfyRule rule)
        {
            Rule = rule;
            ruleList = new List<AfyRule> { rule }.AsReadOnly();
        }

        public IList<AfyRule> GetRules(Meta meta, AfyYamlContext context)
        {
            return ruleList;
        }
    }
}
