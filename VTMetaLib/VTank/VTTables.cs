using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using VTMetaLib.IO;

namespace VTMetaLib.VTank
{
    public class VTTable : VTDataType
    {

        public string Name { get; set; } = "";

        public int ColumnCount
        {
            get
            {
                return ColumnNames.Count;
            }
        }

        public List<string> ColumnNames { get; private set; } = new List<string>();

        public List<bool> ColumnIndexed { get; private set; } = new List<bool>();

        public List<VTTableRow> Rows { get; private set; } = new List<VTTableRow>();

        public int RowCount
        {
            get
            {
                return Rows.Count;
            }
        }

        public VTTable(string name = "") : base("TABLE", "notused")
        {
            Name = name;
        }

        public void AddColumn(string name, bool isIndexed)
        {
            ColumnNames.Add(name);
            ColumnIndexed.Add(isIndexed);
        }

        public override string GetTypeAsString()
        {
            return string.IsNullOrEmpty(Name) ? "TABLE" : Name;
        }

        public override object GetValue()
        {
            return Rows;
        }

        public override string GetValueAsString()
        {
            return "[[TABLE]]";
        }

        public override void SetValueFromString(string strValue)
        {
            throw new NotImplementedException();
        }

        public VTTableRow this[int index]
        {
            get
            {
                return Rows[index];
            }
            set
            {
                Rows[index] = value;
            }
        }

        public void RemoveRowAt(int index)
        {
            if (index < 0 || index >= Rows.Count)
                return;
            Rows.RemoveAt(index);
        }

        public void InsertRowAt(int index, VTTableRow row)
        {
            if (index < 0 || index >= Rows.Count)
                return;
            Rows.Insert(index, row);
        }

        internal void AppendRow(VTTableRow row)
        {
            Rows.Add(row);
        }

        public int FindColumnIndex(string colName)
        {
            int index = ColumnNames.IndexOf(colName);
            if (index < 0)
                throw new KeyNotFoundException($"Column [{colName}] not found in table: {Name}");
            return index;
        }

        public VTTableRow FindRecordByColumnValue(string colName, string queryVal)
        {
            return FindRecordByColumnValue(FindColumnIndex(colName), queryVal);
        }

        public VTTableRow FindRecordByColumnValue(int colIndex, string queryVal)
        {
            if (colIndex < 0 || colIndex >= ColumnNames.Count)
                return null;
            foreach (var record in Rows)
            {
                if (record[colIndex].GetValueAsString() == queryVal)
                    return record;
            }
            return null;
        }

        public string GetValuesString()
        {
            var values = new List<string>();
            for (var i = 0; i < RowCount; i++)
            {
                values.Add(this[i][1].GetValueAsString());
            }
            return String.Join(";", values.ToArray());
        }

        internal override void ReadFrom(LineReadable file)
        {
            Name = file.ReadNextLineAsString();
            int colCount = file.ReadNextLineAsInt();

            Rows.Clear();
            ColumnNames.Clear();
            ColumnIndexed.Clear();

            for (int i = 0; i < colCount; i++)
                ColumnNames.Add(file.ReadNextLineAsString());
            for (int i = 0; i < colCount; i++)
                ColumnIndexed.Add(file.ReadNextLineAsBoolean());
            
            int rowCount = file.ReadNextLineAsInt();
            for (int i = 0; i < rowCount; i++)
            {
                VTTableRow row = new VTTableRow(this);
                foreach (var colName in ColumnNames)
                    row[colName] = file.ReadTypedData(typeof(VTTable));
                Rows.Add(row);
            }
        }

        internal override void WriteTo(MetaFileBuilder writer)
        {
            writer.WriteLine(string.IsNullOrEmpty(Name) ? "TABLE" : Name);
            writer.WriteLine(ColumnCount.ToString());
            for (int i = 0; i < ColumnCount; i++)
                writer.WriteLine(ColumnNames[i]);
            for (int i = 0; i < ColumnCount; i++)
                writer.WriteLine(ColumnIndexed[i] ? "y" : "n");
            writer.WriteLine(RowCount.ToString());

            foreach (VTTableRow row in Rows)
            {
                for (int i = 0; i < ColumnCount; i++)
                    writer.WriteData(row[i]);
            }
        }
    }

    public class VTTableRow
    {
        public VTTable ParentTable { get; internal set; }

        internal Dictionary<string, VTDataType> Data = new Dictionary<string, VTDataType>();

        public VTTableRow(VTTable parent)
        {
            ParentTable = parent;
        }

        public VTDataType this[int index]
        {
            get
            {
                if (index < 0 || index >= ParentTable.ColumnNames.Count)
                    throw new IndexOutOfRangeException($"Index {index} out of bounds, column count is {ParentTable.ColumnNames.Count}");
                return this[ParentTable.ColumnNames[index]];
            }
            set
            {
                if (index < 0 || index >= ParentTable.ColumnNames.Count)
                    throw new IndexOutOfRangeException($"Index {index} out of bounds, column count is {ParentTable.ColumnNames.Count}");
                this[ParentTable.ColumnNames[index]] = value;
            }
        }

        public VTDataType this[string colName]
        {
            get
            {
                return Data[colName];
            }
            set
            {
                Data[colName] = value;
            }
        }

        internal void WriteAsVT(MetaFileBuilder writer)
        {
            for (int i = 0; i < ParentTable.ColumnNames.Count; i++)
                writer.WriteData(this[i]);
        }
    }

    public static class VTTableHelpers
    {
        public static VTTableRow AddTwoColRow(this VTTable table, VTDataType first, VTDataType second)
        {
            VTTableRow row = new VTTableRow(table);
            row[0] = first;
            row[1] = second;
            table.AppendRow(row);
            return row;
        }

        public static VTTable CreateTable_kv()
        {
            VTTable table = new VTTable("TABLE");
            table.AddColumn("k", false);
            table.AddColumn("v", false);
            return table;
        }

        public static VTTable CreateTable_KV()
        {
            VTTable table = new VTTable("TABLE");
            table.AddColumn("K", false);
            table.AddColumn("V", false);
            return table;
        }
    }
    }
