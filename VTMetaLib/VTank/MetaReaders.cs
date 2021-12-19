using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaLib.VTank
{
    public class MetaFileReader
    {
        public MetaFile File
        {
            get {
                return FileContext.MetaFile;
            }
        }

        public MetaContext MetaContext
        {
            get {
                return FileContext.MetaContext;
            }
        }

        public MetaFileContext FileContext { get; private set; }

        public MetaFileReader(MetaFileContext fileContext)
        {
            FileContext = fileContext;
        }

        public MalformedMetaException MalformedFor(string message)
        {
            return MetaContext.MalformedFor(message);
        }

        public string ReadNextLine(Type dtType, string reason)
        {
            string line = File.ReadNextLine();
            if (line == null)
                throw MalformedFor($"No lines remaining to read for type {dtType.Name}: {reason}");
            return line;
        }

        public char ReadNextChar(Type dtType, string reason)
        {
            char ch = File.ReadNextChar();
            if (ch == (char)0)
                throw MalformedFor($"Unable to read another character for type {dtType.Name}: {reason}");
            return ch;
        }

        public VTDouble ReadDouble()
        {
            string line = ReadNextLine(typeof(VTDouble), "double");
            double value;
            if (!double.TryParse(line, out value))
                throw MalformedFor($"Invalid numerical value for double: {line}");
            // TODO:
            // could do something like...
            //      new VTDouble(value, context.CaptureContext());
            //  where CaptureContext() creatures a detached copy of the current cursor in meta (e.g. line/col) and maybe
            //      additional lines before (and possibly ability to fetch lines after at a later time via a method call?)
            return new VTDouble(value);
        }

        public VTFloat ReadFloat()
        {
            string line = ReadNextLine(typeof(VTDouble), "float");
            float value;
            if (!float.TryParse(line, out value))
                throw MalformedFor($"Invalid numerical value for float: {line}");
            return new VTFloat(value);
        }

        public VTInteger ReadInteger()
        {
            return new VTInteger(ReadAndParseInt(typeof(VTInteger), "int"));
        }

        internal int ReadAndParseInt(Type dtType, string reason)
        {
            string line = ReadNextLine(dtType, "int");
            int value;
            if (!int.TryParse(line, out value))
                throw MalformedFor($"Invalid numerical value for integer: {line}");
            return value;
        }

        public VTUnsignedInteger ReadUnsignedInt()
        {
            string line = ReadNextLine(typeof(VTUnsignedInteger), "uint");
            uint value;
            if (!uint.TryParse(line, out value))
                throw MalformedFor($"Invalid numerical value for unsigned integer: {line}");
            return new VTUnsignedInteger(value);
        }

        public VTBoolean ReadBoolean()
        {
            return new VTBoolean(ReadAndParseBool(typeof(VTBoolean), "boolean"));
        }

        internal bool ReadAndParseBool(Type dtType, string reason)
        {
            string line = ReadNextLine(dtType, "bool");
            if (line == "True" || line == "y")
                return true;
            else if (line == "False" || line == "n")
                return false;
            else
                throw MalformedFor($"Unable to parse boolean value from {line} for {reason}");
        }

        public VTString ReadString()
        {
            string line = ReadNextLine(typeof(VTString), "string");
            return new VTString(line);
        }

        public VTByteArray ReadByteArray()
        {
            int count = ReadAndParseInt(typeof(VTByteArray), "byte count");
            /* string fullLine = */ ReadNextLine(typeof(VTByteArray), "byte array data");
            String bytes = File.ReadNextChars(count);
            return new VTByteArray(bytes);
        }

        internal VTDataType ReadTypedData(Type parentType)
        {
            string typeStr = ReadNextLine(parentType, $"data type for record in {parentType.Name}");
            switch (typeStr)
            {
                // table type (unnamed)
                case "TABLE":
                    // XXX TODO FIXME should this parse more... ? hmm
                    return new VTTable();
                // integer type
                case "i":
                    return ReadInteger();
                // double type
                case "d":
                    return ReadDouble();
                // string type
                case "s":
                    return ReadString();
                // boolean type
                case "b":
                    return ReadBoolean();
                // unsigned integer type
                case "u":
                    return ReadUnsignedInt();
                // float type (unused?)
                case "f":
                    return ReadFloat();
                // byte array
                case "ba":
                    return ReadByteArray();
                // null value
                case "0":
                    return new VTUnassigned();
            }
            throw MalformedFor($"Invalid data type string '{typeStr}'");
        }

        public VTTable ReadTable(string tablePurpose = "", string name = "")
        {
            string tableName = string.IsNullOrEmpty(name) ? "TABLE" : name;
            tablePurpose = tablePurpose ?? "TABLE";

            VTTable table = new VTTable(name);
            int columnCount = ReadAndParseInt(typeof(VTTable), $"columnCount for {tableName} for {tablePurpose}");

            // COLUMN NAMES
            for (int i = 0; i < columnCount; i++)
            {
                string colName = ReadNextLine(typeof(VTTable), $"columnName for {tableName} for {tablePurpose}");
                if (string.IsNullOrEmpty(colName))
                    throw MalformedFor($"Found blank name for column #{i} of table {tableName} for {tablePurpose}");
                table.ColumnNames.Add(colName);
            }

            // COLUMN INDEXING
            for (int i = 0; i < columnCount; i++)
            {
                bool indexed = ReadAndParseBool(typeof(VTTable), $"isIndexedCol {i} of {tableName} for {tablePurpose}");
                table.ColumnIndexed.Add(indexed);
            }


            // BODY
            int recordCount = ReadAndParseInt(typeof(VTTable), $"recordCount of {tableName} for {tablePurpose}");
            for (int i = 0; i < recordCount; i++)
            {
                VTTableRow record = ReadTableRecord(table, i);
                table.Rows.Add(record);
            }

            // all done
            return table;
        }

        internal VTTableRow ReadTableRecord(VTTable table, int recordNum)
        {
            VTTableRow row = new VTTableRow(table);
            for (int i = 0; i < table.ColumnCount; i++)
            {

                // TODO: wrap this in another try-catch to bubble up additional contextual information about where in meta
                //      processing we are!
                VTDataType data = ReadTypedData(typeof(VTTable));

                row[table.ColumnNames[i]] = data;
            }

            // finished reading every column
            return row;
        }

        internal ReadingForType ReadingType(Type type)
        {
            return new ReadingForType(MetaContext, type);
        }

        public string CurrentLine
        {
            get
            {
                return File.GetCurrentLineOrNull();
            }
        }
    }
    public class ReadingForType : IDisposable
    {
        private readonly MetaContext metaContext;

        public ReadingForType(MetaContext metaContext, Type type)
        {
            this.metaContext = metaContext;
            metaContext.BeginReadingType(type);
        }

        public void Dispose()
        {
            metaContext.FinishReadingType();
        }
    }

    public static class VTMetaReaderExtensions
    {
        public static VTDataType ReadExpectedData(this MetaFileReader reader, Type parentType, Type expectedType)
        {
            VTDataType data = reader.ReadTypedData(parentType);
            if (data.GetType() != expectedType)
                throw reader.MalformedFor($"{parentType.Name}: Expected to read {expectedType.Name} but got {data.GetType()}: {data.GetValueAsString()}");
            return data;
        }

        public static void VerifyExpectedData(this MetaContext context, Type parentType, VTDataType val, object expected)
        {
            var dtVal = val.GetValue();
            if (!Object.Equals(dtVal, expected))
                throw context.MalformedFor($"{parentType.Name}: Expected to get value {expected} but got value: {dtVal}");
        }

        public static VTTable ReadSpecialTableWithExpectedColumns(this MetaFileReader reader, string tablePurpose, List<string> colNames)
        {
            VTTable table = reader.ReadTable();
            if (table.ColumnNames.Count != colNames.Count)
                throw reader.MalformedFor($"Unexpected number of columns ({table.ColumnNames.Count}) when {colNames.Count} expected in table for {tablePurpose}");
            return table;
        }
    }
}
