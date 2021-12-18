using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaLib.VTank
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
		public string StateName { get; set; }

        public VTConditionData Condition { get; set; }  

		public VTAction Action { get; set; }
    }

	

	public class VTConditionData
	{
		public VTConditionType Type { get; set; } = VTConditionType.Unassigned;

		/// <summary>
		/// If this is a condition that has sub-elements, such as Any, Not or All conditions, then this will contain each "inner" condition
		/// </summary>
		public List<VTConditionData> Children { get; private set; } = new List<VTConditionData>();

		/// <summary>
		/// Contains a singular text value if this Condition acts on String values
		/// </summary>
		public string TextData { get; set; } = "";

		public VTDataType Data { get; set; }
	}
}
