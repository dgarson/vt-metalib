using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VTMetaLib.IO;

namespace VTMetaLib.VTank
{
	public enum VTConditionType
	{
		Unassigned = -1,
		Never = 0,
		Always = 1,
		All = 2,
		Any = 3,
		ChatMatch = 4,
		MainSlotsLE = 5,
		SecsInStateGE = 6,
		NavEmpty = 7,
		Death = 8,
		VendorOpen = 9,
		VendorClosed = 10,
		ItemCountLE = 11,
		ItemCountGE = 12,
		MobsInDist_Name = 13,
		MobsInDist_Priority = 14,
		NeedToBuff = 15,
		NoMobsInDist = 16,
		BlockE = 17,
		CellE = 18,
		PortalEnter = 19,
		PortalExit = 20,
		Not = 21,
		PSecsInStateGE = 22,
		SecsOnSpellGE = 23,
		BurdenPercentGE = 24,
		DistanceToRouteGE = 25,
		Expr = 26,
		//ClientDialogPopup = 27, // some type from the past? it's not in vt now.
		ChatCapture = 28
	}

	public interface VTCondition : VTEncodable
	{
		public VTConditionType ConditionType { get; }
	}

	public abstract class VTConditionWithTableData : VTTableEncodable, VTCondition
	{
		public VTConditionType ConditionType { get; internal set; }

        protected VTConditionWithTableData(VTConditionType condType, List<ColumnSpec> columnSpecs, string tableName = "") : base((int)condType, columnSpecs, tableName)
		{
			ConditionType = condType;
		}

		protected VTConditionWithTableData(VTConditionType condType, TableSchema tableSchema) : base((int)condType, tableSchema)
		{
			ConditionType = condType;
		}
	}

	public abstract class VTConditionWithScalarData : VTEncodable, VTCondition
	{
		public VTConditionType ConditionType { get; internal set; }
		public int TypeId
		{
			get
			{
				return (int)ConditionType;
			}
		}

		protected VTConditionWithScalarData(VTConditionType condType)
        {
			ConditionType = condType;
        }

		public abstract VTDataType AsVTData();
		public abstract void ReadDataFrom(LineReadable file);
		public abstract void ReadFromData(LineReadable file, VTDataType data);

        public void WriteTo(MetaFileBuilder writer)
        {
			writer.WriteData(new VTInteger(TypeId));
			writer.WriteData(AsVTData());
        }
    }

	public abstract class VTConditionWithZeroData : VTZeroIntEncodable, VTCondition
	{
		public VTConditionType ConditionType { get; internal set; }

		protected VTConditionWithZeroData(VTConditionType condType) : base((int)condType)
        {
			ConditionType = condType;
        }
    }

	public class CUnassigned : VTConditionWithZeroData
	{

        public CUnassigned() : base(VTConditionType.Unassigned) { }
    }

	/// <summary>
	/// CondId = 0
	/// 
	/// Expected data is:
	///			i
	///			0
	/// </summary>
	public class CNever : VTConditionWithZeroData
	{
		public CNever() : base(VTConditionType.Never) { }
    }

	/// <summary>
	/// CondId = 1
	/// 
	/// Expected data is:
	///			i
	///			0
	/// </summary>
	public class CAlways : VTConditionWithZeroData
	{
		public CAlways() : base(VTConditionType.Always) { }
    }

	/// <summary>
	/// CondId = 2
	/// 
	/// Data is:
	/// 
	/// TABLE
	/// 2			(column count)
	/// K			('K' col name)
	/// V			('V' col name)
	/// n			(col 1 not indexed)
	/// n			(col 2 not indexed)
	/// integer		(condition count for iterating nested conditions)
	/// [ConditionType]		(pair-field for 'K' column)
	/// [ConditionData]		(pair-field, simple or compound via All/Any/Not, for 'V' column)
	/// 
	public class CAll : VTConditionWithTableData
	{
		public List<VTCondition> Children { get; } = new List<VTCondition>();

		public CAll(List<VTCondition> children) : base(VTConditionType.All, TableTypeConstants.SCHEMA_KV)
		{
			Children.AddRange(children);
		}

		internal CAll() : base(VTConditionType.All, TableTypeConstants.SCHEMA_KV) { }

        protected override void PopulateTable(VTTable table)
        {
			foreach (var cond in Children)
				table.AddTwoColRow(new VTInteger(cond.TypeId), cond.AsVTData());
		}

        protected override void ReadFromTable(LineReadable file, VTTable table)
        {
			foreach (VTTableRow row in table.Rows)
			{
				VTInteger ctypeVal = row["K"] as VTInteger;
				VTConditionType ctype = (VTConditionType)ctypeVal.Value;
				VTCondition cond = ctype.NewCondition(file);
				cond.ReadFromData(file, row["V"]);

				Children.Add(cond);
			}
		}
    }

	/// <summary>
	/// CondId = 3
	/// 
	/// Data is:
	/// 
	/// TABLE
	/// 2			(column count)
	/// K			('K' col name)
	/// V			('V' col name)
	/// n			(col 1 not indexed)
	/// n			(col 2 not indexed)
	/// integer		(condition count for iterating nested conditions)
	/// [ConditionType]		(pair-field for 'K' column)
	/// [ConditionData]		(pair-field, simple or compound via All/Any/Not, for 'V' column)
	/// 
	public class CAny : VTConditionWithTableData
	{
		public List<VTCondition> Children { get; } = new List<VTCondition>();

		public CAny(List<VTCondition> children) : base(VTConditionType.Any, TableTypeConstants.SCHEMA_KV)
		{
			Children.AddRange(children);
		}

		internal CAny() : base(VTConditionType.Any, TableTypeConstants.SCHEMA_KV) { }

		protected override void PopulateTable(VTTable table)
		{
			foreach (var cond in Children)
				table.AddTwoColRow(new VTInteger(cond.TypeId), cond.AsVTData());
		}

		protected override void ReadFromTable(LineReadable file, VTTable table)
		{
			foreach (VTTableRow row in table.Rows)
			{
				VTInteger ctypeVal = row["K"] as VTInteger;
				VTConditionType ctype = (VTConditionType)ctypeVal.Value;
				VTCondition cond = ctype.NewCondition(file);
				cond.ReadFromData(file, row["V"]);

				Children.Add(cond);
			}
		}
    }

	public class CChatMatch : VTConditionWithScalarData
	{
		public string MatchText { get; internal set; }

		internal CChatMatch() : base(VTConditionType.ChatMatch)
		{
		}

		public CChatMatch(string text) : this()
		{
			MatchText = text;
		}

		public override void ReadDataFrom(LineReadable file)
		{
			ReadFromData(file,
				file.ReadExpectedData(typeof(CChatMatch), typeof(VTString)) as VTString);
		}

		public override VTDataType AsVTData()
		{
			return new VTString(MatchText);
		}

        public override void ReadFromData(LineReadable context, VTDataType data)
        {
			MatchText = data.GetValueAsString();
        }
    }

	public class CMainSlotsLE : VTConditionWithScalarData
	{
		public int Slots { get; internal set; }

		internal CMainSlotsLE() : base(VTConditionType.MainSlotsLE) { }

		public CMainSlotsLE(int slots) : this()
		{
			Slots = slots;
		}

		public override void ReadDataFrom(LineReadable file)
		{
			ReadFromData(file, 
				file.ReadExpectedData(typeof(CMainSlotsLE), typeof(VTInteger)) as VTInteger);
		}

		public override VTDataType AsVTData()
		{
			return new VTInteger(Slots);
		}

        public override void ReadFromData(LineReadable context, VTDataType data)
        {
			var slots = data as VTInteger;
            Slots = slots.Value;
        }
    }

	public class CSecsInStateGE : VTConditionWithScalarData
	{
		public int Seconds { get; internal set; }

		internal CSecsInStateGE() : base(VTConditionType.SecsInStateGE) { }

		public CSecsInStateGE(int secs) : this()
		{
			Seconds = secs;
		}

		public override void ReadDataFrom(LineReadable file)
		{
			ReadFromData(file, 
				file.ReadExpectedData(typeof(CSecsInStateGE), typeof(VTInteger)) as VTInteger);
		}

		public override VTDataType AsVTData()
		{
			return new VTInteger(Seconds);
		}

        public override void ReadFromData(LineReadable context, VTDataType data)
        {
			VTInteger secsVal = data as VTInteger;
			Seconds = secsVal.Value;
		}
    }

	public class CNavEmpty : VTConditionWithZeroData
	{
		public CNavEmpty() : base(VTConditionType.NavEmpty) { }
    }

	public class CDeath : VTConditionWithZeroData
	{
		public CDeath() : base(VTConditionType.Death) { }
    }

	public class CVendorOpen : VTConditionWithZeroData
	{
		public CVendorOpen() : base(VTConditionType.VendorOpen) { }
	}

	public class CVendorClosed : VTConditionWithZeroData
	{
		public CVendorClosed() : base(VTConditionType.VendorClosed) { }
	}

	public class CItemCountLE : VTConditionWithTableData
	{
		public string ItemName { get; internal set; }

		public int Count { get; internal set; }

		internal CItemCountLE() : base(VTConditionType.ItemCountLE, TableTypeConstants.SCHEMA_kv) { }

		public CItemCountLE(string itemName, int count) : this()
		{
			ItemName = itemName;
			Count = count;
		}

        protected override void PopulateTable(VTTable table)
        {
			table.AddTwoColRow(new VTString("n"), new VTString(ItemName));
			table.AddTwoColRow(new VTString("c"), new VTInteger(Count));
		}

        protected override void ReadFromTable(LineReadable file, VTTable table)
        {
			if (table.RowCount != 2)
				throw file.MalformedFor($"Expected 2 rows but got {table.RowCount} for CItemCountLE");
			// TODO OPTIONAL VALIDATION
			ItemName = table.Rows[0][1].GetValueAsString();
			var countCol = table.Rows[1][1] as VTInteger;
			Count = countCol.Value;
		}
    }

	public class CItemCountGE : VTConditionWithTableData
	{
		public string ItemName { get; internal set; }

		public int Count { get; internal set; }

		internal CItemCountGE() : base(VTConditionType.ItemCountGE, TableTypeConstants.SCHEMA_kv) { }

		public CItemCountGE(string itemName, int count) : this()
		{
			ItemName = itemName;
			Count = count;
		}

        protected override void PopulateTable(VTTable table)
        {
			table.AddTwoColRow(new VTString("n"), new VTString(ItemName));
			table.AddTwoColRow(new VTString("c"), new VTInteger(Count));
		}

        protected override void ReadFromTable(LineReadable file, VTTable table)
        {
			if (table.RowCount != 2)
				throw file.MalformedFor($"Expected 2 rows but got {table.RowCount} for CItemCountGE");
			// TODO OPTIONAL VALIDATION
			ItemName = table.Rows[0][1].GetValueAsString();
			var countCol = table.Rows[1][1] as VTInteger;
			Count = countCol.Value;
		}
    }

	public class CMobsInDistanceName : VTConditionWithTableData
	{
		public string MonsterName { get; internal set; }

		public int Count { get; internal set; }

		public double Distance { get; internal set; }

		internal CMobsInDistanceName() : base(VTConditionType.MobsInDist_Name, TableTypeConstants.SCHEMA_kv) { }

		public CMobsInDistanceName(string monsterName, int count, double dist) : this()
		{
			MonsterName = monsterName;
			Count = count;
			Distance = dist;
		}

        protected override void PopulateTable(VTTable table)
        {
			table.AddTwoColRow(new VTString("n"), new VTString(MonsterName));
			table.AddTwoColRow(new VTString("c"), new VTInteger(Count));
			table.AddTwoColRow(new VTString("r"), new VTDouble(Distance));
		}

        protected override void ReadFromTable(LineReadable file, VTTable table)
        {
			if (table.RowCount != 3)
				throw file.MalformedFor($"Expected 3 rows but got {table.RowCount} for CMobsInDistanceName");
			// TODO OPTIONAL VALIDATION
			MonsterName = table.Rows[0][1].GetValueAsString();
			Count = (int)table.Rows[1][1].GetValue();
			Distance = (double)table.Rows[2][1].GetValue();
		}
    }

	public class CMobsInDistancePriority : VTConditionWithTableData
	{
		public int Priority { get; internal set; }

		public int Count { get; internal set; }

		public double Distance { get; internal set; }

		internal CMobsInDistancePriority() : base(VTConditionType.MobsInDist_Priority, TableTypeConstants.SCHEMA_kv) { }

		public CMobsInDistancePriority(int priority, int count, double dist) : this()
		{
			Priority = priority;
			Count = count;
			Distance = dist;
		}

        protected override void PopulateTable(VTTable table)
        {
			table.AddTwoColRow(new VTString("p"), new VTInteger(Priority));
			table.AddTwoColRow(new VTString("c"), new VTInteger(Count));
			table.AddTwoColRow(new VTString("r"), new VTDouble(Distance));
		}

        protected override void ReadFromTable(LineReadable file, VTTable table)
        {
			if (table.RowCount != 3)
				throw file.MalformedFor($"Expected 3 rows but got {table.RowCount} for CMobsInDistancePriority");
			// TODO OPTIONAL VALIDATION
			Priority = (int)table.Rows[0][1].GetValue();
			Count = (int)table.Rows[1][1].GetValue();
			Distance = (double)table.Rows[2][1].GetValue();
		}
    }

	public class CNeedToBuff : VTConditionWithZeroData
	{
		public CNeedToBuff() : base(VTConditionType.NeedToBuff) { }
	}

	public class CNoMobsInRange : VTConditionWithTableData
	{
		public double Distance { get; internal set; }

		internal CNoMobsInRange() : base(VTConditionType.NoMobsInDist, TableTypeConstants.SCHEMA_kv) { }

		public CNoMobsInRange(double distance) : this()
		{
			Distance = distance;
		}

        protected override void PopulateTable(VTTable table)
        {
			table.AddTwoColRow(new VTString("r"), new VTDouble(Distance));
		}

        protected override void ReadFromTable(LineReadable file, VTTable table)
        {
			if (table.RowCount != 1)
				throw file.MalformedFor($"Expected only 1 row but got {table.RowCount} for CNoMobsInRange");
			// TODO OPTIONAL VALIDATION
			Distance = (double)table.Rows[0][1].GetValue();
		}
    }

	public class CLandblockE : VTConditionWithScalarData
	{
		public int Landblock { get; internal set; }

		internal CLandblockE() : base(VTConditionType.BlockE) { }

		public CLandblockE(int landblock) : this()
		{
			Landblock = landblock;
		}

		public override void ReadDataFrom(LineReadable file)
		{
			var lb = file.ReadExpectedData(typeof(CLandblockE), typeof(VTInteger)) as VTInteger;
			ReadFromData(file, lb);
		}

		public override VTDataType AsVTData()
		{
			return new VTInteger(Landblock);
		}

        public override void ReadFromData(LineReadable context, VTDataType data)
        {
			var lb = data as VTInteger;
			Landblock = lb.Value;
		}
    }

	public class CLandcellE : VTConditionWithScalarData
	{
		public int Landcell { get; internal set; }

		internal CLandcellE() : base(VTConditionType.CellE) { }

		public CLandcellE(int landcell) : this()
		{
			Landcell = landcell;
		}

		public override void ReadDataFrom(LineReadable file)
		{
			var lc = file.ReadExpectedData(typeof(CLandcellE), typeof(VTInteger)) as VTInteger;
			ReadFromData(file, lc);
		}

		public override VTDataType AsVTData()
		{
			return new VTInteger(Landcell);
		}

        public override void ReadFromData(LineReadable context, VTDataType data)
        {
			var lc = data as VTInteger;
			Landcell = lc.Value;
        }
    }

	public class CPortalEnter : VTConditionWithZeroData
	{
		public CPortalEnter() : base(VTConditionType.PortalEnter) { }
    }

	public class CPortalExit : VTConditionWithZeroData
	{
		public CPortalExit() : base(VTConditionType.PortalExit) { }
	}

	public class CNot : VTConditionWithTableData
	{
		public VTCondition Condition { get; internal set; }

		internal CNot() : base(VTConditionType.Not, TableTypeConstants.SCHEMA_KV) { }

		public CNot(VTCondition notCond) : this()
		{
			Condition = notCond;
		}

        protected override void PopulateTable(VTTable table)
        {
			table.AddTwoColRow(new VTInteger(Condition.TypeId), Condition.AsVTData());
		}

        protected override void ReadFromTable(LineReadable file, VTTable table)
        {
            VTConditionType ctype = (VTConditionType)table[0][0].GetValue();
			VTCondition cond = ctype.NewCondition(file);
			cond.ReadFromData(file, table[0][1]);
			Condition = cond;
        }
    }

	public class CPSecsInStateGE : VTConditionWithScalarData
	{
		public int Seconds { get; internal set; }

		internal CPSecsInStateGE() : base(VTConditionType.PSecsInStateGE) { }

		public CPSecsInStateGE(int seconds) : this()
		{
			Seconds = seconds;
		}

		public override void ReadDataFrom(LineReadable file)
		{
			ReadFromData(file,
				file.ReadExpectedData(typeof(CPSecsInStateGE), typeof(VTInteger)) as VTInteger);
		}

		public override VTDataType AsVTData()
		{
			return new VTInteger(Seconds);
		}

        public override void ReadFromData(LineReadable context, VTDataType data)
        {
			VTInteger secs = data as VTInteger;
			Seconds = secs.Value;
		}
    }

	public class CSecsOnSpellGE : VTConditionWithTableData
	{
		public int SpellId { get; internal set; }

		public int Seconds { get; internal set; }

		internal CSecsOnSpellGE() : base(VTConditionType.SecsOnSpellGE, TableTypeConstants.SCHEMA_kv) { }

		public CSecsOnSpellGE(int spellId, int seconds) : this()
		{
			SpellId = spellId;
			Seconds = seconds;
		}

        protected override void PopulateTable(VTTable table)
        {
			table.AddTwoColRow(new VTString("sid"), new VTInteger(SpellId));
			table.AddTwoColRow(new VTString("sec"), new VTInteger(Seconds));
		}

        protected override void ReadFromTable(LineReadable file, VTTable table)
        {
			if (table.RowCount != 2)
				throw file.MalformedFor($"Expected 2 rows but got {table.RowCount} for CSecsOnSpellGE");
			// TODO OPTIONAL VALIDATION
			SpellId = (int)table.Rows[0][1].GetValue();
			Seconds = (int)table.Rows[1][1].GetValue();
		}
    }

	public class CBurdenPercentGE : VTConditionWithScalarData
	{
		public int Burden { get; internal set; }

		internal CBurdenPercentGE() : base(VTConditionType.BurdenPercentGE) { }

		public CBurdenPercentGE(int burden) : this()
		{
			Burden = burden;
		}

		public override void ReadDataFrom(LineReadable file)
		{
			ReadFromData(file, 
				file.ReadExpectedData(typeof(CBurdenPercentGE), typeof(VTInteger)) as VTInteger);
		}

		public override VTDataType AsVTData()
		{
			return new VTInteger(Burden);
		}

        public override void ReadFromData(LineReadable context, VTDataType data)
        {
			VTInteger burden = data as VTInteger;
			Burden = burden.Value;
        }
    }

	public class CDistanceToRouteGE : VTConditionWithTableData
	{
		public double Distance { get; internal set; }

		internal CDistanceToRouteGE() : base(VTConditionType.DistanceToRouteGE, TableTypeConstants.SCHEMA_kv) { }

		public CDistanceToRouteGE(double distance) : this()
		{
			Distance = distance;
		}

        protected override void PopulateTable(VTTable table)
        {
			table.AddTwoColRow(new VTString("d"), new VTDouble(Distance));
		}

        protected override void ReadFromTable(LineReadable file, VTTable table)
        {
			if (table.RowCount != 1)
				throw file.MalformedFor($"Expected only one row but got {table.RowCount} for CDistanceToRouteGE");
			// TODO optional additional validation
			var distance = (double)table[0][1].GetValue();
			Distance = distance;
		}
    }

	public class CExpr : VTConditionWithTableData
	{
		public string Expr { get; internal set; }

		internal CExpr() : base(VTConditionType.Expr, TableTypeConstants.SCHEMA_kv) { }

		public CExpr(string expr) : this()
		{
			Expr = expr;
		}

        protected override void PopulateTable(VTTable table)
        {
			table.AddTwoColRow(new VTString("e"), new VTString(Expr));
		}

        protected override void ReadFromTable(LineReadable file, VTTable table)
        {
			if (table.RowCount != 1)
				throw file.MalformedFor($"Expected only one row but got {table.RowCount} for CExpr");

			Expr = table.Rows[0][1].GetValueAsString();
		}
    }

	public class CChatCapture : VTConditionWithTableData
	{
		public string Pattern { get; internal set; }

		public string ColorIdList { get; internal set; }

		internal CChatCapture() : base(VTConditionType.ChatCapture, TableTypeConstants.SCHEMA_kv) { }

		public CChatCapture(string regex, String colorIds) : this()
		{
			Pattern = regex;
			ColorIdList = colorIds;
		}

        protected override void PopulateTable(VTTable table)
        {
			table.AddTwoColRow(new VTString("p"), new VTString(Pattern));
			table.AddTwoColRow(new VTString("c"), new VTString(ColorIdList));
		}

        protected override void ReadFromTable(LineReadable file, VTTable table)
        {
			if (table.RowCount != 2)
				throw file.MalformedFor($"Expected only one row but got {table.RowCount} for CDistanceToRouteGE");
			// TODO additional validation

			Pattern = table.Rows[0][1].GetValueAsString();
			ColorIdList = table.Rows[1][1].GetValueAsString();
		}
    }

	public static class VTConditionHelpers
    {
		internal static VTCondition NewCondition(this VTConditionType type, LineReadable file)
		{
			switch (type)
			{
				case VTConditionType.Unassigned: return new CUnassigned();
				case VTConditionType.Never: return new CNever();
				case VTConditionType.Always: return new CAlways();
				case VTConditionType.All: return new CAll();
				case VTConditionType.Any: return new CAny();
				case VTConditionType.Expr: return new CExpr();
				case VTConditionType.ChatMatch: return new CChatMatch();
				case VTConditionType.MainSlotsLE: return new CMainSlotsLE();
				case VTConditionType.SecsInStateGE: return new CSecsInStateGE();
				case VTConditionType.Death: return new CDeath();
				case VTConditionType.NavEmpty: return new CNavEmpty();
				case VTConditionType.VendorOpen: return new CVendorOpen();
				case VTConditionType.VendorClosed: return new CVendorClosed();
				case VTConditionType.ItemCountLE: return new CItemCountLE();
				case VTConditionType.ItemCountGE: return new CItemCountGE();
				case VTConditionType.MobsInDist_Name: return new CMobsInDistanceName();
				case VTConditionType.MobsInDist_Priority: return new CMobsInDistancePriority();
				case VTConditionType.NeedToBuff: return new CNeedToBuff();
				case VTConditionType.NoMobsInDist: return new CNoMobsInRange();
				case VTConditionType.BlockE: return new CLandblockE();
				case VTConditionType.CellE: return new CLandcellE();
				case VTConditionType.PortalEnter: return new CPortalEnter();
				case VTConditionType.PortalExit: return new CPortalExit();
				case VTConditionType.Not: return new CNot();
				case VTConditionType.PSecsInStateGE: return new CPSecsInStateGE();
				case VTConditionType.SecsOnSpellGE: return new CSecsOnSpellGE();
				case VTConditionType.BurdenPercentGE: return new CBurdenPercentGE();
				case VTConditionType.DistanceToRouteGE: return new CDistanceToRouteGE();
				case VTConditionType.ChatCapture: return new CChatCapture();
				default:
					throw file.MalformedFor($"No such CTypeID = {type} ({(int)type})");
			}
		}
	}
}
