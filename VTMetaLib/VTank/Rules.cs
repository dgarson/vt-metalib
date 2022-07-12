using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VTMetaLib.IO;

namespace VTMetaLib.VTank
{
	public class TableTypeConstants
	{
		public static readonly TableSchema SCHEMA_KV = new TableSchema("").AddColumn("K", false).AddColumn("V", false);
		public static readonly TableSchema SCHEMA_kv = new TableSchema("").AddColumn("k", false).AddColumn("v", false);

		public static readonly List<string> COLUMNS_KV = SCHEMA_KV.ColumnNames;
		public static readonly List<string> COLUMNS_kv = SCHEMA_kv.ColumnNames;
	}

    public class VTRule
    {
		public string StateName { get; internal set; }

        public VTCondition Condition { get; internal set; }  

		public VTAction Action { get; internal set; }

		public VTRule(string stateName, VTCondition condition, VTAction action)
        {
			StateName = stateName;
			Condition = condition;
			Action = action;
        }
    }

	public static class MetaReaderExtensions
    {
		
		public static VTRule ReadRule(this MetaFile file)
        {
			SeekableCharStream reader = file.Reader;

			// TODO validate ID values exist ...
			VTConditionType condType = (VTConditionType)reader.ReadVTInteger().Value;
			VTActionType actionType = (VTActionType)reader.ReadVTInteger().Value;

			VTCondition condition = condType.NewCondition(reader);
			condition.ReadDataFrom(reader: reader);

			VTAction action = actionType.NewAction(reader);
			action.ReadDataFrom(reader: reader);

			VTString stateName = reader.ReadVTString();
			return new VTRule(stateName.Value, condition, action);
        }
	
		public static void WriteRule(this MetaFileBuilder writer, VTRule rule)
        {
			writer.WriteLine(rule.Condition.TypeId.ToString());
			writer.WriteLine(rule.Action.TypeId.ToString());
			rule.Condition.WriteTo(writer);
			rule.Action.WriteTo(writer);
			writer.WriteData(new VTString(rule.StateName));
        }
	}
}
