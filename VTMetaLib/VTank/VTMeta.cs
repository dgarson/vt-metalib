using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VTMetaLib.VTank
{
    public class VTMeta : VTEncodableWithSerializer
    {
        public int TypeId => 0;

        public List<VTRule> Rules { get; } = new List<VTRule>();

        public HashSet<string> StateNames { get; } = new HashSet<string>();

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

        public void ReadDataFrom(MetaFile file)
        {
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

        public void ReadFromData(MetaFile file, VTDataType data)
        {
            VTTable table = data as VTTable;
            if (table.ColumnCount != 5)
                throw file.MalformedFor($"Expected 5 columns for top-level Meta CondAct table but got {table.ColumnCount}");
            // TODO optional validation of column names?
            file.Info($"Reading {table.RowCount} Rules from top-level Meta CondAct table");
            foreach (var row in table.Rows)
            {
                VTConditionType condType = (VTConditionType)row[0].GetValue();
                VTActionType actionType = (VTActionType)row[1].GetValue();

                VTCondition cond = condType.NewCondition(file);
                cond.ReadFromData(file, row[2]);

                VTAction action = actionType.NewAction(file);
                action.ReadFromData(file, row[3]);

                string stateName = row[4].GetValueAsString();

                Rules.Add(new VTRule(stateName, cond, action));
                StateNames.Add(stateName);
                file.Info($"Added Rule #{Rules.Count} in State {stateName}");
            }
        }

        public void WriteTo(MetaFileBuilder writer)
        {
            writer.WriteLine("1");
            writer.WriteData(AsVTData());
        }
    }
}
