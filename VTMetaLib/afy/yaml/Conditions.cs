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

        public AfyConditionType Condition { get => ConditionType;
            set {
                // no-op
            }
        }


        public AfyEntity Parent { get; internal set; }

        // lazy initialize to avoid a shit ton of empty Dictionaries
        public Dictionary<string, string> Metadata { get; internal set; } // = new Dictionary<string, string>();

        public abstract VTCondition AsVTCondition(AfyYamlContext context);
    }

    public abstract class AfyConditionWithChildren : AfyYamlCondition, AfyEntityWithChildren<AfyCondition>
    {
        public List<AfyCondition> Conditions { get; internal set; } = new List<AfyCondition>();
        
        public List<AfyCondition> Children => Conditions;

        protected AfyConditionWithChildren(AfyConditionType condType) : base(condType) { }

        protected AfyConditionWithChildren(AfyConditionType condType, IEnumerable<AfyCondition> conditions) : this(condType)
        {
            Conditions = new List<AfyCondition>(conditions);
        }
    }

    public abstract class AfyZeroArgCondition : AfyYamlCondition
    {

        protected AfyZeroArgCondition(AfyConditionType condType) : base(condType) { }
    }

    public class AlwaysCondition : AfyZeroArgCondition
    {
        public static readonly AlwaysCondition Instance = new AlwaysCondition();

        public AlwaysCondition() : base(AfyConditionType.Always) { }

        public override VTCondition AsVTCondition(AfyYamlContext context)
        {
            return new CAlways();
        }
    }

    public class NeverCondition : AfyZeroArgCondition
    {
        public static readonly NeverCondition Instance = new NeverCondition();

        public NeverCondition() : base(AfyConditionType.Never) { }

        public override VTCondition AsVTCondition(AfyYamlContext context)
        {
            return new CNever();
        }
    }

    public class NotCondition : AfyYamlCondition
    {
        public AfyCondition Child { get; set; }

        public NotCondition() : base(AfyConditionType.Not) { }

        public NotCondition(AfyCondition cond) : this()
        {
            Child = cond;
        }

        public override VTCondition AsVTCondition(AfyYamlContext context)
        {
            return new CNot(Child.AsVTCondition(context: context));
        }
    }

    public class NavEmpty : AfyZeroArgCondition
    {
        public static readonly NavEmpty Instance = new NavEmpty();

        public NavEmpty() : base(AfyConditionType.NavEmpty) { }

        public override VTCondition AsVTCondition(AfyYamlContext context)
        {
            return new CNavEmpty();
        }
    }

    public class DeathCondition : AfyZeroArgCondition
    {
        public static readonly DeathCondition Instance = new DeathCondition();

        public DeathCondition() : base(AfyConditionType.Death) { }

        public override VTCondition AsVTCondition(AfyYamlContext context)
        {
            return new CDeath();
        }
    }

    public class PortalEnter : AfyZeroArgCondition
    {
        public static readonly PortalEnter Instance = new PortalEnter();

        public PortalEnter() : base(AfyConditionType.PortalEnter) { }

        public override VTCondition AsVTCondition(AfyYamlContext context)
        {
            return new CPortalEnter();
        }
    }
    public class PortalExit : AfyZeroArgCondition
    {
        public static readonly PortalExit Instance = new PortalExit();

        public PortalExit() : base(AfyConditionType.PortalExit) { }

        public override VTCondition AsVTCondition(AfyYamlContext context)
        {
            return new CPortalExit();
        }
    }

    public class VendorOpen : AfyZeroArgCondition
    {
        public static readonly VendorOpen Instance = new VendorOpen();

        public VendorOpen() : base(AfyConditionType.VendorOpen) { }

        public override VTCondition AsVTCondition(AfyYamlContext context)
        {
            return new CVendorOpen();
        }
    }

    public class VendorClosed : AfyZeroArgCondition
    {
        public static readonly VendorClosed Instance = new VendorClosed();

        public VendorClosed() : base(AfyConditionType.VendorClosed) { }

        public override VTCondition AsVTCondition(AfyYamlContext context)
        {
            return new CVendorClosed();
        }
    }

    public class AnyCondition : AfyConditionWithChildren
    {
        public AnyCondition() : base(AfyConditionType.Any) { }

        public AnyCondition(IEnumerable<AfyCondition> conditions) : base(AfyConditionType.Any, conditions) { }

        public override VTCondition AsVTCondition(AfyYamlContext context)
        {
            return new CAny(Conditions.Select(c => c.AsVTCondition(context: context)).ToList());
        }
    }

    public class AllCondition : AfyConditionWithChildren
    {
        public AllCondition() : base(AfyConditionType.All) { }
        public AllCondition(IEnumerable<AfyCondition> conditions) : base(AfyConditionType.All, conditions) { }

        public override VTCondition AsVTCondition(AfyYamlContext context)
        {
            return new CAll(Conditions.Select(c => c.AsVTCondition(context: context)).ToList());
        }
    }

    public class ExprCondition : AfyYamlCondition
    {
        public string Expr { get; set; }

        public ExprCondition() : base(AfyConditionType.Expr) { }

        public ExprCondition(string expr) : this() { Expr = expr; }

        public override VTCondition AsVTCondition(AfyYamlContext context)
        {
            return new CExpr(Expr);
        }
    }
      
    public class ChatMatch : AfyYamlCondition
    {
        public string Pattern { get; set; }

        public ChatMatch() : base(AfyConditionType.ChatMatch) { }

        public ChatMatch(string pattern) : this() { Pattern = pattern; }

        public override VTCondition AsVTCondition(AfyYamlContext context)
        {
            return new CChatMatch(Pattern);
        }
    }

    public class ChatCapture : AfyYamlCondition
    {
        public string Pattern { get; set; }

        public List<string> ColorIdList { get; set; } = new List<string>();

        public ChatCapture() : base(AfyConditionType.ChatCapture) { }

        public ChatCapture(string pattern, List<string> colorIdList = null) : this()
        {
            Pattern = pattern;
            if (colorIdList != null)
                ColorIdList.AddRange(colorIdList);
        }

        public override VTCondition AsVTCondition(AfyYamlContext context)
        {
            return new CChatCapture(Pattern, string.Join(",", ColorIdList));
        }
    }

    public class MainSlotsLE : AfyYamlCondition
    {
        public int Slots { get; set; }

        public MainSlotsLE() : base(AfyConditionType.MainSlotsLE) { }

        public MainSlotsLE(int slots) : this() { Slots = slots;  }

        public override VTCondition AsVTCondition(AfyYamlContext context)
        {
            return new CMainSlotsLE(Slots);
        }
    }

    public class SecsInStateGE : AfyYamlCondition
    {
        public int Seconds { get; set; }

        public SecsInStateGE() : base(AfyConditionType.SecsInStateGE) { }

        public SecsInStateGE(int seconds) : this() { Seconds = seconds; }

        public override VTCondition AsVTCondition(AfyYamlContext context)
        {
            return new CSecsInStateGE(Seconds);
        }
    }

    public class PSecsInStateGE : AfyYamlCondition
    {
        public int Seconds { get; set; }

        public PSecsInStateGE() : base(AfyConditionType.PSecsInStateGE) { }

        public PSecsInStateGE(int seconds) : this() { Seconds = seconds; }

        public override VTCondition AsVTCondition(AfyYamlContext context)
        {
            return new CPSecsInStateGE(Seconds);
        }
    }

    public class ItemCountLE : AfyYamlCondition
    {
        public string Name { get; set; }

        public int Count { get; set; }

        public ItemCountLE() : base(AfyConditionType.ItemCountLE) { }

        public ItemCountLE(string name, int count) : this()
        {
            Name = name;
            Count = count;
        }

        public override VTCondition AsVTCondition(AfyYamlContext context)
        {
            return new CItemCountLE(Name, Count);
        }
    }

    public class ItemCountGE : AfyYamlCondition
    {
        public string Name { get; set; }

        public int Count { get; set; }

        public ItemCountGE() : base(AfyConditionType.ItemCountGE) { }

        public ItemCountGE(string name, int count) : this()
        {
            Name = name;
            Count = count;
        }

        public override VTCondition AsVTCondition(AfyYamlContext context)
        {
            return new CItemCountGE(Name, Count);
        }
    }

    public class MobsInDistanceByName : AfyYamlCondition
    {
        public string Name { get; set; }

        public int Count { get; set; }

        public float Distance { get; set; }

        public MobsInDistanceByName() : base(AfyConditionType.MobsInDistanceByName) { }

        public MobsInDistanceByName(string name, int count, float distance) : this()
        {
            Name = name;
            Count = count;
            Distance = distance;
        }

        public override VTCondition AsVTCondition(AfyYamlContext context)
        {
            return new CMobsInDistanceName(Name, Count, Distance);
        }
    }

    public class MobsInDistanceByPriority : AfyYamlCondition
    {
        public int Priority { get; set; }

        public int Count { get; set; }

        public float Distance { get; set; }

        public MobsInDistanceByPriority() : base(AfyConditionType.MobsInDistanceByPriority) { }

        public MobsInDistanceByPriority(int priority, int count, float distance) : this()
        {
            Priority = priority;
            Count = count;
            Distance = distance;
        }

        public override VTCondition AsVTCondition(AfyYamlContext context)
        {
            return new CMobsInDistancePriority(Priority, Count, Distance);
        }
    }

    public class NeedToBuff : AfyZeroArgCondition
    {
        public NeedToBuff() : base(AfyConditionType.NeedToBuff) { }

        public override VTCondition AsVTCondition(AfyYamlContext context)
        {
            return new CNeedToBuff();
        }
    }

    public class NoMobsInRange : AfyYamlCondition
    {
        public float Distance { get; set; }

        public NoMobsInRange() : base(AfyConditionType.NoMobsInRange) { }

        public NoMobsInRange(float distance) : this() { Distance = distance; }

        public override VTCondition AsVTCondition(AfyYamlContext context)
        {
            return new CNoMobsInRange(Distance);
        }
    }

    public class LandblockE : AfyYamlCondition
    {
        public string Value { get; set; }

        public LandblockE() : base(AfyConditionType.LandblockE) { }

        public LandblockE(string value) : this() { Value = value; }

        public override VTCondition AsVTCondition(AfyYamlContext context)
        {
            // FIXME is this right??
            int landblock = Convert.ToInt32(Value, 16);
            return new CLandblockE(landblock);
        }
    }

    public class LandcellE : AfyYamlCondition
    {
        public string Value { get; set; }

        public LandcellE() : base(AfyConditionType.LandcellE) { }

        public LandcellE(string value) : this() { Value = value; }

        public override VTCondition AsVTCondition(AfyYamlContext context)
        {
            // FIXME is this right??
            int landcell = Convert.ToInt32(Value, 16);
            return new CLandcellE(landcell);
        }
    }

    public class SecsOnSpellGE : AfyYamlCondition
    {
        public int SpellId { get; set; }

        public string SpellName { get; set; }

        public int Seconds { get; set; }

        public SecsOnSpellGE() : base(AfyConditionType.SecsOnSpellGE) { }

        public SecsOnSpellGE(int spellId, int seconds) : this()
        {
            SpellId = spellId;
            Seconds = seconds;
        }

        public SecsOnSpellGE(string spellName, int seconds) : this()
        {
            SpellName = spellName;
            Seconds = seconds;
        }

        public override VTCondition AsVTCondition(AfyYamlContext context)
        {
            int sid = GetSpellId();
            return new CSecsOnSpellGE(sid, Seconds);
        }

        private int GetSpellId()
        {
            if (string.IsNullOrEmpty(SpellName))
                return -1;
            else
            {

            }
            return -1;
        }
    }

    public class BurdenPercentGE : AfyYamlCondition
    {
        public int Value { get; set; }

        public BurdenPercentGE() : base(AfyConditionType.BurdenPercentGE) { }

        public BurdenPercentGE(int value) : this() { Value = value; }

        public override VTCondition AsVTCondition(AfyYamlContext context)
        {
            return new CBurdenPercentGE(Value);
        }
    }

    public class DistanceToRouteGE : AfyYamlCondition
    {
        public float Distance { get; set; }

        public DistanceToRouteGE() : base(AfyConditionType.DistanceToRouteGE) { }

        public DistanceToRouteGE(float distance) : this() { Distance = distance; }

        public override VTCondition AsVTCondition(AfyYamlContext context)
        {
            return new CDistanceToRouteGE(Distance);
        }
    }

    public class MetaVersionChanged : AfyZeroArgCondition
    {
        public MetaVersionChanged() : base(AfyConditionType.MetaVersionChanged) { }

        public override VTCondition AsVTCondition(AfyYamlContext context)
        {
            ExprCondition cond = new ExprCondition("getvar[MetaVersion]!=getvar[LastActiveVersion]");
            return cond.AsVTCondition(context: context);
        }
    }

}
