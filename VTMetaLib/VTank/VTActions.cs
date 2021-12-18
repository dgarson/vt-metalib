﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaLib.VTank
{
	public enum VTActionType
	{
		Unassigned = -1,
		None = 0,
		SetState = 1,
		ChatCommand = 2,
		All = 3,
		EmbedNav = 4,
		CallState = 5,
		Return = 6,
		Expr = 7,
		ChatExpr = 8,
		SetWatchdog = 9,
		ClearWatchdog = 10,
		GetOpt = 11,
		SetOpt = 12,
		CreateView = 13,
		DestroyView = 14,
		DestroyAllViews = 15
	}

	public interface VTAction : VTEncodable
	{
		public VTActionType ActionType { get; }
	}

	public abstract class VTActionWithTableData : VTTableEncodable, VTAction
	{
		public VTActionType ActionType { get; private set; }

		protected VTActionWithTableData(VTActionType actionType, List<ColumnSpec> columnSpecs, string tableName = "") : base((int)actionType, columnSpecs, tableName)
		{
			ActionType = actionType;
		}

		protected VTActionWithTableData(VTActionType actionType, TableSchema tableSchema) : base((int)actionType, tableSchema)
		{
			ActionType = actionType;
		}
	}

	public abstract class VTActionWithScalarData : VTAction
	{
		public VTActionType ActionType { get; private set; }
		public int TypeId => (int)ActionType;

		protected VTActionWithScalarData(VTActionType actionType)
		{
			ActionType = actionType;
		}

		public abstract VTDataType AsVTData();
		public abstract void ReadDataFrom(MetaFileReader reader);
		public abstract void ReadFromData(MetaContext context, VTDataType data);
	}

	public abstract class VTActionWithZeroData : VTZeroIntEncodable, VTAction
    {
		public VTActionType ActionType { get; private set; }
		protected VTActionWithZeroData(VTActionType actionType) : base((int)actionType)
        {
			ActionType = actionType;
		}
    }

	public class AUnassigned : VTActionWithScalarData
	{
		public AUnassigned() : base(VTActionType.Unassigned) { }

		public override void ReadDataFrom(MetaFileReader reader)
		{
			throw reader.MalformedFor("AUnassigned not supported");
		}

		public override VTDataType AsVTData()
		{
			return new VTInteger(0);
		}

		public override void ReadFromData(MetaContext context, VTDataType data)
		{
			throw context.MalformedFor("AUnassigned not supported");
		}
	}

	public class ANone : VTActionWithZeroData
	{
		public ANone() : base(VTActionType.Unassigned) { }
	}

	public class ASetState : VTActionWithScalarData
	{
		public string State { get; private set; }

		public ASetState() : base(VTActionType.SetState) { }

		public ASetState(string state) : this()
		{
			State = state;
		}

		public override void ReadDataFrom(MetaFileReader reader)
		{
			ReadFromData(reader.MetaContext,
				reader.ReadExpectedData(typeof(ASetState), typeof(VTString)) as VTString);
		}

		public override VTDataType AsVTData()
		{
			return new VTString(State);
		}

		public override void ReadFromData(MetaContext context, VTDataType data)
		{
			VTString val = data as VTString;
			State = val.Value;
		}
	}

	public class AChatCommand : VTActionWithScalarData
	{
		public string Message { get; private set; }

		public AChatCommand() : base(VTActionType.ChatCommand) { }

		public AChatCommand(string message) : this()
		{
			Message = message;
		}

		public override void ReadDataFrom(MetaFileReader reader)
		{
			ReadFromData(reader.MetaContext,
				reader.ReadExpectedData(typeof(AChatCommand), typeof(VTString)) as VTString);
		}

		public override VTDataType AsVTData()
		{
			return new VTString(Message);
		}

		public override void ReadFromData(MetaContext context, VTDataType data)
		{
			VTString val = data as VTString;
			Message = val.Value;
		}
	}

	public class AAll : VTActionWithTableData
	{
		public List<VTAction> Actions { get; } = new List<VTAction>();

		public AAll() : base(VTActionType.All, TableTypeConstants.SCHEMA_KV) { }

		public AAll(List<VTAction> actions) : this()
		{
			Actions.AddRange(actions);
		}

        protected override void PopulateTable(VTTable table)
        {
			foreach (VTAction child in Actions)
				table.AddTwoColRow(new VTInteger(child.TypeId), child.AsVTData());
		}

        protected override void ReadFromTable(MetaContext context, VTTable table)
        {
			foreach (VTTableRow row in table.Rows)
			{
				VTInteger atypeVal = row["K"] as VTInteger;
				VTActionType actionType = (VTActionType)atypeVal.Value;
				VTAction action = actionType.NewAction(context);
				action.ReadFromData(context, row["V"]);

				Actions.Add(action);
			}
		}
    }

	public class AEmbedNav : VTActionWithScalarData
	{
		public string Bytes { get; private set; }

		public AEmbedNav() : base(VTActionType.EmbedNav) { }

		public AEmbedNav(byte[] bytes) : this()
		{
			Bytes = Encoding.UTF8.GetString(bytes);
		}

		public AEmbedNav(string bytes) : this()
		{
			// technically ASCII
			Bytes = bytes;
		}

		public override void ReadDataFrom(MetaFileReader reader)
		{
			ReadFromData(reader.MetaContext,
				reader.ReadExpectedData(typeof(AEmbedNav), typeof(VTByteArray)));
		}

		public override void ReadFromData(MetaContext context, VTDataType data)
		{
			VTByteArray byteArray = data as VTByteArray;
			Bytes = byteArray.Value;
		}

		public override VTDataType AsVTData()
		{
			return new VTByteArray(Bytes);
		}
	}

	public class ACallState : VTActionWithTableData
	{
		public string CallStateName { get; private set; }
		public string ReturnToStateName { get; private set; }

		public ACallState() : base(VTActionType.CallState, TableTypeConstants.SCHEMA_kv) { }

		public ACallState(string callState, string returnState) : this()
		{
			CallStateName = callState;
			ReturnToStateName = returnState;
		}

        protected override void PopulateTable(VTTable table)
        {
			table.AddTwoColRow(new VTString("st"), new VTString(CallStateName));
			table.AddTwoColRow(new VTString("ret"), new VTString(ReturnToStateName));
		}

        protected override void ReadFromTable(MetaContext context, VTTable table)
        {
			if (table.RowCount != 2)
				throw context.MalformedFor($"Expected 2 rows but got {table.RowCount} for ACallState");
			// TODO OPTIONAL VALIDATION
			CallStateName = table[0][1].GetValueAsString();
			ReturnToStateName = table[1][1].GetValueAsString();
		}
    }

	public class AReturn : VTActionWithZeroData
	{
		public AReturn() : base(VTActionType.Return) { }
	}

	public class AExprAction : VTActionWithTableData
	{
		public string Expression { get; private set; }

		public AExprAction() : base(VTActionType.Expr, TableTypeConstants.SCHEMA_kv) { }

		public AExprAction(string expr) : this()
		{
			Expression = expr;
		}

        protected override void PopulateTable(VTTable table)
        {
			table.AddTwoColRow(new VTString("e"), new VTString(Expression));
        }

        protected override void ReadFromTable(MetaContext context, VTTable table)
        {
			if (table.RowCount != 1)
				throw context.MalformedFor($"Expected only 1 row but got {table.RowCount} for AExprAction");
			Expression = table[0][1].GetValueAsString();
        }
    }

	public class AChatExpr : VTActionWithTableData
	{
		public string ChatExpression { get; private set; }

		public AChatExpr() : base(VTActionType.ChatExpr, TableTypeConstants.SCHEMA_kv) { }

		public AChatExpr(string chatExpr) : this()
		{
			ChatExpression = chatExpr;
		}

		protected override void PopulateTable(VTTable table)
		{
			table.AddTwoColRow(new VTString("e"), new VTString(ChatExpression));
		}

		protected override void ReadFromTable(MetaContext context, VTTable table)
		{
			if (table.RowCount != 1)
				throw context.MalformedFor($"Expected only 1 row but got {table.RowCount} for AChatExpr");
			ChatExpression = table[0][1].GetValueAsString();
		}
	}

	public class ASetWatchdog : VTActionWithTableData
	{
		public string StateName { get; private set; }

		public double Distance { get; private set; }

		public double Seconds { get; private set; }

		public ASetWatchdog() : base(VTActionType.SetWatchdog, TableTypeConstants.SCHEMA_kv) { }

		public ASetWatchdog(string stateName, double distance, double seconds) : this()
		{
			StateName = stateName;
			Distance = distance;
			Seconds = seconds;
		}

		protected override void PopulateTable(VTTable table)
		{
			table.AddTwoColRow(new VTString("s"), new VTString(StateName));
			table.AddTwoColRow(new VTString("r"), new VTDouble(Distance));
			table.AddTwoColRow(new VTString("t"), new VTDouble(Seconds));
		}

		protected override void ReadFromTable(MetaContext context, VTTable table)
		{
			if (table.RowCount != 3)
				throw context.MalformedFor($"Expected 3 rows but got {table.RowCount} for ASetWatchdog");
			
			StateName = table[0][1].GetValueAsString();
			Distance = (double)table[1][1].GetValue();
			Seconds = (double)table[2][1].GetValue();
		}
	}

	public class AClearWatchdog : VTActionWithTableData
    {
		public AClearWatchdog() : base(VTActionType.ClearWatchdog, TableTypeConstants.SCHEMA_kv) { }

        protected override void PopulateTable(VTTable table) { }

        protected override void ReadFromTable(MetaContext context, VTTable table)
        {
			if (table.RowCount != 0)
				throw context.MalformedFor($"Expected zero rows but got {table.RowCount} for AClearWatchdog");
        }
    }

	public class AGetOpt : VTActionWithTableData
    {
		public string OptionName { get; private set; }

		public string VarName { get; private set; }

		public AGetOpt() : base(VTActionType.GetOpt, TableTypeConstants.SCHEMA_kv) { }

		public AGetOpt(string optionName, string varName) : this()
        {
			OptionName = optionName;
			VarName = varName;
        }

        protected override void PopulateTable(VTTable table)
        {
			table.AddTwoColRow(new VTString("o"), new VTString(OptionName));
			table.AddTwoColRow(new VTString("v"), new VTString(VarName));
        }

        protected override void ReadFromTable(MetaContext context, VTTable table)
        {
			if (table.RowCount != 2)
				throw context.MalformedFor($"Expected 2 rows but got {table.RowCount} for AGetOpt");
			// TODO OPTIONAL VALIDATION
			OptionName = table[0][1].GetValueAsString();
			VarName = table[1][1].GetValueAsString();
		}
    }

	public class ASetOpt : VTActionWithTableData
	{
		public string OptionName { get; private set; }

		public string Expression { get; private set; }

		public ASetOpt() : base(VTActionType.SetOpt, TableTypeConstants.SCHEMA_kv) { }

		public ASetOpt(string optionName, string expr) : this()
		{
			OptionName = optionName;
			Expression = expr;
		}

		protected override void PopulateTable(VTTable table)
		{
			table.AddTwoColRow(new VTString("o"), new VTString(OptionName));
			table.AddTwoColRow(new VTString("v"), new VTString(Expression));
		}

		protected override void ReadFromTable(MetaContext context, VTTable table)
		{
			if (table.RowCount != 2)
				throw context.MalformedFor($"Expected 2 rows but got {table.RowCount} for ASetOpt");
			// TODO OPTIONAL VALIDATION
			OptionName = table[0][1].GetValueAsString();
			Expression = table[1][1].GetValueAsString();
		}
	}

	public class ACreateView : VTActionWithTableData
	{
		public string ViewName { get; private set; }

		public string XmlBytes { get; private set; }

		public ACreateView() : base(VTActionType.CreateView, TableTypeConstants.SCHEMA_kv) { }

		public ACreateView(string viewName, string xmlBytes) : this()
		{
			ViewName = viewName;	
			XmlBytes = xmlBytes;
		}

		protected override void PopulateTable(VTTable table)
		{
			table.AddTwoColRow(new VTString("n"), new VTString(ViewName));
			table.AddTwoColRow(new VTString("x"), new VTByteArray(XmlBytes));
		}

		protected override void ReadFromTable(MetaContext context, VTTable table)
		{
			if (table.RowCount != 2)
				throw context.MalformedFor($"Expected 2 rows but got {table.RowCount} for ACreateView");
			// TODO OPTIONAL VALIDATION
			ViewName = table[0][1].GetValueAsString();
			XmlBytes = table[1][1].GetValueAsString();
		}
	}

	public class ADestroyView : VTActionWithTableData
	{
		public string ViewName { get; private set; }

		public ADestroyView() : base(VTActionType.DestroyView, TableTypeConstants.SCHEMA_kv) { }

		public ADestroyView(string viewName) : this()
		{
			ViewName = viewName;
		}

		protected override void PopulateTable(VTTable table)
		{
			table.AddTwoColRow(new VTString("n"), new VTString(ViewName));
		}

		protected override void ReadFromTable(MetaContext context, VTTable table)
		{
			if (table.RowCount != 1)
				throw context.MalformedFor($"Expected only 1 row but got {table.RowCount} for ADestroyView");
			// TODO OPTIONAL VALIDATION
			ViewName = table[0][1].GetValueAsString();
		}
	}

	public class ADestroyAllViews : VTActionWithTableData
	{
		public ADestroyAllViews() : base(VTActionType.DestroyAllViews, TableTypeConstants.SCHEMA_kv) { }

		protected override void PopulateTable(VTTable table) { }

		protected override void ReadFromTable(MetaContext context, VTTable table)
		{
			if (table.RowCount != 0)
				throw context.MalformedFor($"Expected zero rows but got {table.RowCount} for ADestroyAllViews");
		}
	}

	public static class VTActionTypeExtensions
	{
		public static VTAction NewAction(this VTActionType type, MetaContext context)
		{
			switch (type)
			{
				case VTActionType.Unassigned: return new ANone();
				case VTActionType.None: return new ANone();
				case VTActionType.SetState: return new ASetState();
				case VTActionType.ChatCommand: return new AChatCommand();
				case VTActionType.All: return new AAll();
				case VTActionType.EmbedNav: return new AEmbedNav();
				case VTActionType.CallState: return new ACallState();
				case VTActionType.Return: return new AReturn();
				case VTActionType.ChatExpr: return new AChatExpr();
				case VTActionType.SetWatchdog: return new ASetWatchdog();
				case VTActionType.ClearWatchdog: return new AClearWatchdog();
				case VTActionType.GetOpt: return new AGetOpt();
				case VTActionType.SetOpt: return new ASetOpt();
				case VTActionType.CreateView: return new ACreateView();
				case VTActionType.DestroyView: return new ADestroyView();
				case VTActionType.DestroyAllViews: return new ADestroyAllViews();
				default:
					throw context.MalformedFor($"No such ATypeID = {type} ({(int)type})");
			}
		}
	}
}
