using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VTMetaLib.IO;

namespace VTMetaLib.VTank
{
    public class VTMeta : VTEncodable
    {
        public int TypeId => 0;

        public List<VTRule> Rules { get; } = new List<VTRule>();

        public Dictionary<string, List<VTRule>> States { get; } = new Dictionary<string, List<VTRule>>();

        public ICollection<string> StateNames => States.Keys;

        public int LineCount { get; private set; }

        public VTDataType AsVTData()
        {
            VTTable table = new VTTable("CondAct");
            table.AddColumn("CType", false);
            table.AddColumn("AType", false);
            table.AddColumn("CData", false);
            table.AddColumn("AData", false);
            table.AddColumn("State", false);

            foreach (VTRule rule in Rules)
            {
                VTTableRow row = new VTTableRow(table);
                row.Data["CType"] = new VTInteger((int)rule.Condition.TypeId);
                row.Data["AType"] = new VTInteger((int)rule.Action.TypeId);
                row.Data["CData"] = rule.Condition.AsVTData();
                row.Data["AData"] = rule.Action.AsVTData();
                row.Data["State"] = new VTString(rule.StateName);
                table.AppendRow(row);
            }

            return table;
        }

        public void ReadDataFrom(LineReadable file)
        {
            LineCount = file.Lines.Count;

            int tableCount = file.ReadAndParseInt(typeof(VTMeta), "meta tableCount");
            if (tableCount != 1)
                throw file.MalformedFor($"Expected top-level tableCount to be 1 (one meta) but got {tableCount}");

            string tableName = file.ReadNextLine(typeof(VTMeta), "meta tableName");
            VTTable table = file.ReadVTTable("meta", tableName);
            ReadFromData(file, table);
        }

        public void ReadFrom(MetaFile metaFile)
        {
            ReadDataFrom(metaFile);
        }

        public void ReadFromData(LineReadable file, VTDataType data)
        {
            VTTable table = data as VTTable;
            if (table.ColumnCount != 5)
                throw file.MalformedFor($"Expected 5 columns for top-level Meta CondAct table but got {table.ColumnCount}");
            // TODO optional validation of column names?
            file.Info($"Reading {table.RowCount} Rules from top-level Meta CondAct table");

            Console.WriteLine($"Processing {table.Rows} records in main CondAct table");
            foreach (var row in table.Rows)
            {
                VTConditionType condType = (VTConditionType)row[0].GetValue();
                VTActionType actionType = (VTActionType)row[1].GetValue();

                VTCondition cond = condType.NewCondition(file);
                cond.ReadFromData(file, row[2]);

                VTAction action = actionType.NewAction(file);
                action.ReadFromData(file, row[3]);

                string stateName = row[4].GetValueAsString();

                VTRule rule = new VTRule(stateName, cond, action);
                int stateRuleCount = AddRuleToState(rule).Count;

                file.Info($"State[{stateName}]: Added Rule #{stateRuleCount}");
                Console.WriteLine($"State[{stateName}]: Added Rule #{stateRuleCount} with Condition {condType} and Action {actionType}");
            }
        }

        /// <summary>
        /// Shorthand to both construct a new VTRule and call AddRuleToState(..)
        /// </summary>
        /// <returns>the list of rules in the state</returns>
        public List<VTRule> AddRuleToState(string stateName, VTCondition cond, VTAction action)
        {
            return AddRuleToState(new VTRule(stateName, cond, action));
        }

        /// <summary>
        /// Adds a rule to this meta, mapping it to its state and returning the full list of rules that are in the
        /// same meta state as the rule being added.
        /// </summary>
        /// <returns>full list of rules in the same meta state as the given rule</returns>
        public List<VTRule> AddRuleToState(VTRule rule)
        {
            List<VTRule> stateRules;
            if (!States.TryGetValue(rule.StateName, out stateRules))
            {
                stateRules = new List<VTRule>();
                States.Add(rule.StateName, stateRules);
            }
            Rules.Add(rule);
            stateRules.Add(rule);
            return stateRules;
        }

        public void WriteTo(MetaFileBuilder writer)
        {
            writer.WriteLine("1");
            writer.WriteData(AsVTData());
        }
    }
}
