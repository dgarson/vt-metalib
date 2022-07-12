using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VTMetaLib.IO;

namespace VTMetaLib.VTank
{

    /// <summary>
    /// Generic interface for all higher-order VTank data such as Actions or Conditions, rather than primitive types represented by VTDataType
    /// </summary>
    public interface VTEncodable
    {
        /// <summary>
        /// The integer id representing the type of encodable object this is, used for determining which encodable object type should be used 
        /// when reading from a meta file.
        /// </summary>
        public int TypeId { get; }

        /// <summary>
        /// Shorthand for reading the associated data type for this encodable object and handing it off to the
        /// ReadFromData method, using the reader's MetaContext.
        /// </summary>
        public void ReadDataFrom(SeekableCharStream reader);

        /// <summary>
        /// Reads the data for this encodable object from a VTData value
        /// </summary>
        /// <param name="file">the file within which this encodable object is being read</param>
        /// <param name="data">the data for this encodable (already read beforehand)</param>
        public void ReadFromData(SeekableCharStream file, VTDataType data);

        /// <summary>
        /// Writes this encodable object to the given file builder, which may not conform to the standard VTData type primitives.
        /// </summary>
        /// <param name="writer">the writer to append to</param>
        public void WriteTo(MetaFileBuilder writer);

        /// <summary>
        /// Converts this data portion of this encodable object as a VTDataType, which can be written out to a meta file.
        /// This MAY be an instance of VTNone if values are not applicable for this encodable object type.
        /// </summary>
        public VTDataType AsVTData();
    }

    public class TableSchema
    {
        public string TableName { get; set; }

        private List<ColumnSpec> columnSpecs = new List<ColumnSpec>();

        public TableSchema(string name = "", List<ColumnSpec> initialColumns = null)
        {
            TableName = name;
            if (initialColumns != null)
                columnSpecs.AddRange(initialColumns);
        }

        public TableSchema AddColumn(string name, bool indexed)
        {
            columnSpecs.Add(new ColumnSpec(name, indexed));
            return this;
        }

        public int ColumnCount
        {
            get {
                return columnSpecs.Count;
            }
        }

        public List<string> ColumnNames
        {
            get {
                return columnSpecs.ConvertAll(spec => spec.Name);
            }
        }

        /// <summary>
        /// Creates a new VTTable and calls AddColumn until all of the specified columns are present and the table is ready to have Rows added
        /// to it.
        /// </summary>
        public VTTable CreateTable()
        {
            VTTable table = new VTTable(TableName);
            foreach (ColumnSpec spec in columnSpecs)
                table.AddColumn(spec.Name, spec.Indexed);
            return table;
        }
    }

    /// <summary>
    /// Base class to facilitate reusable mechanism for reading and populating VTTables when used as a base for various higher-order VTank types
    /// </summary>
    public abstract class VTTableEncodable : VTEncodable
    {
        protected readonly TableSchema schema;
        
        protected readonly List<string> columnNames;

        public int TypeId { get; private set; }

        protected VTTableEncodable(int typeId, List<ColumnSpec> columnSpecs, string tableName = "")
        {
            TypeId = typeId;
            this.schema = new TableSchema(tableName, columnSpecs);
            this.columnNames = schema.ColumnNames;
        }

        protected VTTableEncodable(int typeId, TableSchema schema)
        {
            TypeId = typeId;
            this.schema = schema;
            this.columnNames = schema.ColumnNames;
        }

        public VTDataType AsVTData()
        {
            // create the table using the predefined 'column schemas' so only rows need to be added
            VTTable table = schema.CreateTable();

            // call to subclass to populate the table w/rows
            PopulateTable(table);

            // all done
            return table;
        }

        protected abstract void PopulateTable(VTTable table);

        public void ReadDataFrom(SeekableCharStream file)
        {
            VTTable table = file.ReadSpecialTableWithExpectedColumns(GetType().Name, columnNames);
            ReadFromData(file: file, table);
        }

        public void ReadFromData(SeekableCharStream file, VTDataType data)
        {
            if (data.GetType() != typeof(VTTable))
                throw file.MalformedFor($"Unexpected non-VTTable for Table-Encodable type {GetType().Name}, got {data.GetType().Name}");
            ReadFromTable(file, data as VTTable);
        }

        protected abstract void ReadFromTable(SeekableCharStream file, VTTable table);

        public void WriteTo(MetaFileBuilder writer)
        {
            writer.WriteData(AsVTData());
        }
    }

    /// <summary>
    /// Reusable base class that fully implements VTEncodable and consistently expects and returns data contents of integer value 0 (zero). 
    /// This is common of most higher-order VTank types that do not have any parameters.
    /// </summary>
    public abstract class VTZeroIntEncodable : VTEncodable
    {
        public int TypeId { get; private set; }

        protected VTZeroIntEncodable(int typeId)
        {
            TypeId = typeId;
        }

        public VTDataType AsVTData()
        {
            return new VTInteger(0);
        }

        public void ReadDataFrom(SeekableCharStream file)
        {
            VTInteger val = file.ReadExpectedData(GetType(), typeof(VTInteger)) as VTInteger;
            ReadFromData(file: file, val);

        }

        public void ReadFromData(SeekableCharStream file, VTDataType data)
        {
            file.VerifyExpectedData(GetType(), data, (int)0);
        }

        public void WriteTo(MetaFileBuilder writer)
        {
            writer.WriteData(AsVTData());
        }
    }

    /// <summary>
    /// Specification for a column that will be automatically registered in a VTTable inside VTTableEncodable as well as verified as being the
    /// exact column names when extracting data from a VTTable for reading the underlying subclass/implementation type.
    /// </summary>
    public class ColumnSpec
    {
        public string Name { get; private set; }

        public bool Indexed { get; set; }

        public ColumnSpec(string name, bool indexed = false)
        {
            Name = name;
            Indexed = indexed;
        }
    }
}
