using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Xml;
using VTMetaLib.VTank;
using VTMetaLib.IO;

namespace VTMetaLib.MIMB
{
    internal class MimbConstants
    {
        public static readonly string ConditionTypeElement = "Condition_x0020_Type";
        public static readonly string ActionTypeElement = "Action_x0020_Type";
        public static readonly string ConditionDataElement = "Condition_x0020_Data";
        public static readonly string ActionDataElement = "Action_x0020_Data";
        public static readonly string StateNameElement = "State";

        public static readonly Dictionary<MimbConditionType, VTConditionType> MimbToVTankConditionTypes = new Dictionary<MimbConditionType, VTConditionType>();
        public static readonly Dictionary<MimbActionType, VTActionType> MimbToVTankActionTypes = new Dictionary<MimbActionType, VTActionType>();

        static MimbConstants()
        {
            foreach (var condType in Enum.GetValues(typeof(MimbConditionType)))
                MimbToVTankConditionTypes.Add((MimbConditionType)condType, (VTConditionType)((int)condType));
            foreach (var actionType in Enum.GetValues(typeof(MimbActionType)))
                MimbToVTankActionTypes.Add((MimbActionType)actionType, (VTActionType)((int)actionType));
        }

    }

    internal class Logging
    {
        public static readonly ILog MimbLogger = LogManager.GetLogger("MIMB");
    }

    public enum MimbConditionType
    {
        Unassigned = -1,
        Never = 0,
        Always = 1,
        All = 2,
        Any = 3,
        ChatMessage = 4,
        MainPackSlotsLE = 5,
        SecondsInStateGE = 6,
        NavrouteEmpty = 7,
        Died = 8,
        VendorOpen = 9,
        VendorClosed = 10,
        ItemCountLE = 11,
        ItemCountGE = 12,
        MonsterCountWithinDistance = 13,
        MonstersWithPriorityWithinDistance = 14,
        NeedToBuff = 15,
        NoMonstersWithinDistance = 16,
        LandBlockE = 17,
        LandCellE = 18,
        PortalspaceEnter = 19,
        PortalspaceExit = 20,
        Not = 21,
        SecondsInStatePersistGE = 22,
        TimeLeftOnSpellGE = 23,
        BurdenPercentGE = 24,
        DistanceToAnyRoutePointGE = 25,
        Expression = 26,
        ClientDialogPopup = 27,
        ChatMessageCapture = 28,
    }

    public enum MimbActionType
    {
        Unassigned = -1,
        None = 0,
        SetState = 1,
        ChatCommand = 2,
        Multiple = 3,
        EmbeddedNavRoute = 4,
        CallState = 5,
        ReturnFromCall = 6,
        ExpressionAct = 7,
        ChatWithExpression = 8,
        WatchdogSet = 9,
        WatchdogClear = 10,
        GetVTOption = 11,
        SetVTOption = 12,
        CreateView = 13,
        DestroyView = 14,
        DestroyAllViews = 15,
    }

    [XmlRoot(ElementName = "NewDataSet")]
    public class MimbXmlFile
    {
        [XmlAnyElement]
        public List<XmlNode> ruleNodes = new List<XmlNode>();
    }

    public class MimbFile
    {
        public List<MimbRule> rules = new List<MimbRule>();

        public MimbFile(List<XmlNode> ruleNodes)
        {
            foreach (var topLevelNode in ruleNodes)
            {
                if (topLevelNode.Name != "table")
                    continue;

                MimbRule rule = new MimbRule();
                rule.CondType = GetChildText(topLevelNode, MimbConstants.ConditionTypeElement);
                rule.CondData = GetChildText(topLevelNode, MimbConstants.ConditionDataElement);
                rule.ActType = GetChildText(topLevelNode, MimbConstants.ActionTypeElement);
                rule.ActData = GetChildText(topLevelNode, MimbConstants.ActionDataElement);
                rule.StateName = GetChildText(topLevelNode, MimbConstants.StateNameElement);
                this.rules.Add(rule);
            }            
        }

        private static string GetChildText(XmlNode node, string childName)
        {
            for (int i = 0; i < node.ChildNodes.Count; i++)
            {
                if (node.ChildNodes[i].Name == childName)
                    return node.ChildNodes[i].InnerText;
            }
            throw new ArgumentException($"Unable to find child element '{childName}' within element <{node.Name}>");
        }
    }

    public class MimbRule
    {
        [XmlElement(ElementName = "Condition_x0020_Type", Namespace = "")]
        public string CondType { get; set; }

        [XmlElement(ElementName = "Action_x0020_Type", Namespace = "")]
        public string ActType { get; set; }

        [XmlElement(ElementName = "Condition_x0020_Data", Namespace = "")]
        public string CondData { get; set; }

        [XmlElement(ElementName = "Action_x0020_Data", Namespace = "")]
        public string ActData { get; set; }

        [XmlElement(ElementName = "State", Namespace = "")]
        public string StateName { get; set; }
    }

    public class Mimb
    {

        public static MimbFile LoadMimbXml(string path)
        {
            
            Console.WriteLine($"Loading Mimb XML file from {path}...");
            using (StreamReader reader = new StreamReader(new FileStream(path, FileMode.Open)))
            {
                return LoadMimbXml(reader);
            }
        }

        public static MimbFile LoadMimbXml(StreamReader reader)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(MimbXmlFile));
            MimbXmlFile mimbXml = (MimbXmlFile)serializer.Deserialize(reader);
            Console.WriteLine($"Deserialized MiMB xml file with {mimbXml.ruleNodes.Count} rules from stream");
            return new MimbFile(mimbXml.ruleNodes);
        }

        public static VTMeta LoadMetaFromMimbXml(string path)
        {
            Console.WriteLine($"Loading Mimb XML file as VTMeta from {path}...");
            using (StreamReader reader = new StreamReader(new FileStream(path, FileMode.Open)))
            {
                return LoadMetaFromMimbXml(reader);
            }
        }

        public static VTMeta LoadMetaFromMimbXml(StreamReader reader)
        {
            List<string> lines = new List<string>();
            string line;
            while ((line = reader.ReadLine()) != null)
                lines.Add(line);
            InMemoryLines readable = new InMemoryLines(lines);

            XmlSerializer serializer = new XmlSerializer(typeof(MimbXmlFile));
            StringReader strReader = new StringReader(string.Join("\n", lines));
            MimbXmlFile mimbXml = (MimbXmlFile)serializer.Deserialize(strReader);
            MimbFile mimbFile = new MimbFile(mimbXml.ruleNodes);
            Console.WriteLine($"Deserialized MiMB xml file with {mimbFile.rules.Count} rules (total of {lines.Count} lines in source)");
            return readable.ReadVTankMetaFromMimbXml(mimbFile);
        }
    }

    public static class MimbXmlFileExtensions
    {
        public static VTMeta ReadVTankMetaFromMimbXml(this LineReadable readable, MimbFile mimbXml)
        {
            VTMeta meta = new VTMeta();
            foreach (MimbRule mimbRule in mimbXml.rules)
            {
                String state = mimbRule.StateName;
                VTRule vtRule = readable.ReadVTRuleFromMimb(mimbRule);
                meta.AddRuleToState(vtRule);
            }

            return meta;
        }

        public static VTRule ReadVTRuleFromMimb(this LineReadable readable, MimbRule mimbRule)
        {
            VTCondition condition = readable.ParseMimbCondition(mimbRule.CondData, (MimbConditionType)Enum.Parse(typeof(MimbConditionType), mimbRule.CondType));
            VTAction action = readable.ParseMimbAction(mimbRule.ActData, (MimbActionType)Enum.Parse(typeof(MimbActionType), mimbRule.ActType));
            VTRule rule = new VTRule(mimbRule.StateName, condition, action);
            return rule;
        }

        public static VTCondition ParseMimbCondition(this LineReadable readable, string data, MimbConditionType condType = MimbConditionType.Unassigned)
        {
            string condData = data;
            if (condType == MimbConditionType.Unassigned) { 
                int openBrace = data.IndexOf('{');
                string condTypeStr = data.Substring(0, openBrace).TrimEnd();
                if (condTypeStr.EndsWith(':'))
                    condTypeStr = condTypeStr.Substring(0, condTypeStr.Length - 1);
                condType = (MimbConditionType)Enum.Parse(typeof(MimbConditionType), condTypeStr);
                condData = data.Substring(openBrace + 1);
                int closingBrace = condData.LastIndexOf('}');
                condData = condData.Substring(0, closingBrace);
            }

            if (condType == MimbConditionType.Any || condType == MimbConditionType.All)
            {
                List<string> childCondData = MimbDataTokenizer.GetAllTokens(condData);
                List<VTCondition> conditions = new List<VTCondition>();
                foreach (var childData in childCondData)
                {
                    VTCondition childCond = ParseMimbCondition(readable, childData);
                    Logging.MimbLogger.Info($"Parsed child Condition of type {childCond.ConditionType} ({(int)childCond.ConditionType})");
                    conditions.Add(childCond);
                }
                if (condType == MimbConditionType.All)
                    return new CAll(conditions);
                else
                    return new CAny(conditions);
            }

            VTConditionType? vtCondType = MimbConstants.MimbToVTankConditionTypes[condType];
            if (!vtCondType.HasValue)
            {
                Logging.MimbLogger.Error($"Unmapped MimbConditionType: {condType} ({(int)condType})");
                throw new ArgumentException($"Unmapped MimbConditionType: {condType} ({(int)condType})");
            }
            VTCondition cond = vtCondType.Value.NewCondition(readable);
            switch (condType)
            {
                // no data that needs to be read
                case MimbConditionType.Never:
                case MimbConditionType.Always:
                case MimbConditionType.NavrouteEmpty:
                case MimbConditionType.Died:
                case MimbConditionType.NeedToBuff:
                case MimbConditionType.PortalspaceEnter:
                case MimbConditionType.PortalspaceExit:
                case MimbConditionType.VendorClosed:
                case MimbConditionType.VendorOpen:
                    break;

                case MimbConditionType.All:
                    ((CAll)cond).Children.AddRange(readable.SplitAndParseConditions(condData));
                    break;

                case MimbConditionType.Not:
                    ((CNot)cond).Condition = readable.ParseMimbCondition(condData);
                    break;

                case MimbConditionType.Any:
                    ((CAny)cond).Children.AddRange(readable.SplitAndParseConditions(condData));
                    break;

                case MimbConditionType.ChatMessage:
                    ((CChatMatch)cond).MatchText = condData;
                    break;

                case MimbConditionType.Expression:
                    ((CExpr)cond).Expr = condData;
                    break;

                case MimbConditionType.LandBlockE:
                    ((CLandblockE)cond).Landblock = readable.ParseInt(condData);
                    break;

                case MimbConditionType.LandCellE:
                    ((CLandcellE)cond).Landcell = readable.ParseInt(condData);
                    break;

                case MimbConditionType.BurdenPercentGE:
                    ((CBurdenPercentGE)cond).Burden = readable.ParseInt(condData);
                    break;

                case MimbConditionType.ChatMessageCapture:
                { 
                    string[] tokens = condData.Split(';');
                    ((CChatCapture)cond).Pattern = tokens[0];
                    ((CChatCapture)cond).ColorIdList = tokens[1];
                    break;
                }

                case MimbConditionType.ItemCountLE:
                {
                        string[] tokens = condData.Split(';');
                        ((CItemCountLE)cond).ItemName = tokens[0];
                        ((CItemCountLE)cond).Count = readable.ParseInt(tokens[1]);
                        break;
                }

                case MimbConditionType.ItemCountGE:
                { 
                    string[] tokens = condData.Split(';');
                    ((CItemCountGE)cond).ItemName = tokens[0];
                    ((CItemCountGE)cond).Count = readable.ParseInt(tokens[1]);
                    break;
                }

                case MimbConditionType.MainPackSlotsLE:
                    ((CMainSlotsLE)cond).Slots = readable.ParseInt(condData);
                    break;

                case MimbConditionType.NoMonstersWithinDistance:
                    ((CNoMobsInRange)cond).Distance = readable.ParseDouble(condData);
                    break;

                case MimbConditionType.MonsterCountWithinDistance:
                {
                    string[] tokens = condData.Split(';');
                    ((CMobsInDistanceName)cond).MonsterName = tokens[0];
                    ((CMobsInDistanceName)cond).Count = readable.ParseInt(tokens[1]);
                    ((CMobsInDistanceName)cond).Distance = readable.ParseDouble(tokens[2]);
                    break;
                }

                case MimbConditionType.MonstersWithPriorityWithinDistance:
                {
                    string[] tokens = condData.Split(';');
                    ((CMobsInDistancePriority)cond).Priority = readable.ParseInt(tokens[0]);
                    ((CMobsInDistancePriority)cond).Count = readable.ParseInt(tokens[1]);
                    ((CMobsInDistancePriority)cond).Distance = readable.ParseDouble(tokens[2]);
                    break;
                }

                case MimbConditionType.SecondsInStateGE:
                    ((CSecsInStateGE)cond).Seconds = readable.ParseInt(condData);
                    break;

                case MimbConditionType.SecondsInStatePersistGE:
                    ((CPSecsInStateGE)cond).Seconds = readable.ParseInt(condData);
                    break;

                case MimbConditionType.TimeLeftOnSpellGE:
                {
                    string[] tokens = condData.Split(';');
                    ((CSecsOnSpellGE)cond).SpellId = readable.ParseInt(tokens[0]);
                    ((CSecsOnSpellGE)cond).Seconds = readable.ParseInt(tokens[1]);
                    break;
                }

                case MimbConditionType.DistanceToAnyRoutePointGE:
                    ((CDistanceToRouteGE)cond).Distance = readable.ParseDouble(condData);
                    break;

                default:
                    throw readable.MalformedFor($"Unrecognized MimbConditionType: {condType} ({(int)condType})");
            }
            return cond;
        }

        public static VTAction ParseMimbAction(this LineReadable readable, string data, MimbActionType actionType = MimbActionType.Unassigned)
        {
            string actionData = data;
            if (actionType == MimbActionType.Unassigned)
            {
                int openBrace = data.IndexOf('{');
                string actionTypeStr = data.Substring(0, openBrace).TrimEnd();
                if (actionTypeStr.EndsWith(":"))
                    actionTypeStr = actionTypeStr.Substring(0, actionTypeStr.Length - 1);
                actionType = (MimbActionType)Enum.Parse(typeof(MimbActionType), actionTypeStr);

                actionData = data.Substring(openBrace + 1);
                int closingBrace = actionData.LastIndexOf('}');
                actionData = actionData.Substring(0, closingBrace);
            }

            if (actionType == MimbActionType.Multiple)
            {
                List<string> childActionData = MimbDataTokenizer.GetAllTokens(actionData);
                List<VTAction> actions = new List<VTAction>();
                foreach (var childData in childActionData)
                {
                    VTAction childAction = readable.ParseMimbAction(childData);
                    Logging.MimbLogger.Info($"Parsed child Action of type {childAction.ActionType} ({(int)childAction.ActionType})");
                    actions.Add(childAction);
                }
                return new AAll(actions);
            }

            VTActionType vtActionType = MimbConstants.MimbToVTankActionTypes[actionType];
            VTAction action = vtActionType.NewAction(readable);
            switch (actionType)
            {
                // nothing to populate from XML
                case MimbActionType.None:
                case MimbActionType.WatchdogClear:
                case MimbActionType.ReturnFromCall:
                case MimbActionType.DestroyAllViews:
                    break;

                case MimbActionType.SetState:
                    ((ASetState)action).State = actionData;
                    break;

                case MimbActionType.ChatCommand:
                    ((AChatCommand)action).Message = actionData;
                    break;

                case MimbActionType.ChatWithExpression:
                    ((AChatExpr)action).ChatExpression = actionData;
                    break;

                case MimbActionType.EmbeddedNavRoute:
                    ((AEmbedNav)action).SetData(actionData);
                    break;

                case MimbActionType.CallState:
                {
                    string[] tokens = actionData.Split(';');
                    ((ACallState)action).CallStateName = tokens[0];
                    ((ACallState)action).ReturnToStateName = tokens[1];
                    break;
                }

                case MimbActionType.ExpressionAct:
                    ((AExprAction)action).Expression = actionData;
                    break;

                case MimbActionType.WatchdogSet:
                {
                    string[] tokens = actionData.Split(';');
                    ((ASetWatchdog)action).StateName = tokens[0];
                    ((ASetWatchdog)action).Distance = readable.ParseDouble(tokens[1]);
                    ((ASetWatchdog)action).Seconds = readable.ParseDouble(tokens[2]);
                    break;
                }

                case MimbActionType.GetVTOption:
                {
                    string[] tokens = actionData.Split(';');
                    ((AGetOpt)action).OptionName = tokens[0];
                    ((AGetOpt)action).VarName = tokens[1];
                    break;
                }

                case MimbActionType.SetVTOption:
                {
                    int firstSep = actionData.IndexOf(';');
                    ((ASetOpt)action).OptionName = actionData.Substring(0, firstSep - 1);
                    ((ASetOpt)action).Expression = actionData.Substring(firstSep + 1);
                    break;
                }

                case MimbActionType.CreateView:
                {
                    int firstSep = actionData.IndexOf(';');
                    string viewName = actionData.Substring(0, firstSep - 1);
                    string viewXml = actionData.Substring(firstSep + 1);
                    ((ACreateView)action).ViewName = viewName;
                    ((ACreateView)action).XmlBytes = viewXml;
                    break;
                }

                case MimbActionType.DestroyView:
                    ((ADestroyView)action).ViewName = actionData;
                    break;

                case MimbActionType.Multiple:
                    throw readable.MalformedFor("Expected ActionType[Multiple] to be handled already in ParseMimbAction!");
                default:
                    throw readable.MalformedFor($"Unrecognized MimbActionType: {actionType} ({(int)actionType})");
            }
            return action;
        }

        public static int ParseInt(this LineReadable readable, string str)
        {
            int val;
            if (int.TryParse(str, out val))
                return val;
            try
            {
                return Convert.ToInt32(str, 16);
            }
            catch (FormatException e)
            {
                throw readable.MalformedFor($"Invalid integer value: {str}: {e.Message}");
            }
        }

        public static double ParseDouble(this LineReadable readable, string str)
        {
            double val;
            if (!double.TryParse(str, out val))
                throw readable.MalformedFor($"Invalid double value: {str}");
            return val;
        }

        private static List<VTCondition> SplitAndParseConditions(this LineReadable readable, string data)
        {
            List<string> tokens = MimbDataTokenizer.GetAllTokens(data);
            List<VTCondition> conds = new List<VTCondition>();
            foreach (var token in tokens)
                conds.Add(readable.ParseMimbCondition(token));
            return conds;
        }
    }

    internal class MimbDataTokenizer
    {
        private readonly string data;
        private int pos;
        private bool finished;

        public MimbDataTokenizer(string data)
        {
            this.data = data;
        }

        void Finish()
        {
            finished = true;
            pos = data.Length;
        }

        public string NextToken() {
            if (finished)
                return null;

            StringBuilder token = new StringBuilder();
            int braceDepth = 0;

            while (pos < data.Length)
            {
                char ch = data[pos++];

                token.Append(ch);
                // THIS MAY BE INSIDE A LIST ELEMENT!!
                if (ch == '{') {
                    braceDepth++;
                } else if (ch == '}') { 
                    braceDepth--;

                    if (braceDepth == 0)
                        return token.ToString();
                }
            }

            Finish();
            return token.Length > 0 ? token.ToString() : null;
        }

        public static List<string> GetAllTokens(string data)
        {
            List<string> results = new List<string>();
            MimbDataTokenizer tokenizer = new MimbDataTokenizer(data);
            string token;
            while ((token = tokenizer.NextToken()) != null)
                results.Add(token);
            return results;
        }
    }

    public static class VTMetaExtensions
    {
        public static void WriteAsMimbXml(this VTMeta meta, string filePath)
        {
            using (StreamWriter writer = new StreamWriter(new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write)))
            {
                meta.WriteAsMimbXml(writer);
            }
        }

        // TODO NOT DONE
        public static void WriteAsMimbXml(this VTMeta meta, StreamWriter writer)
        {
            // NOTE: This will NOT export the schema at the top, but this is NOT required for MiMB UI loading an XML file, so it has no impact outside of byte-for-byte output
            MimbXmlFile mimbXml = new MimbXmlFile();
            List<string> mimbRules = new List<string>();
            foreach (var rule in meta.Rules)
            {
            }

            throw new NotImplementedException("not yet implemented, read-only");
        }
    }
}
