using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml;
using System.Text;
using System.Threading.Tasks;

namespace VTMetaLib.VTank
{

    public abstract class VTDataType
    {
        public string TypeName { get; private set; }
        public string TypeCode { get; private set; }

        public bool ExternalContent { get; internal set; }

        protected VTDataType(string typeName, string typeCode)
        {
            TypeName = typeName;
            TypeCode = typeCode;
        }

        public abstract object GetValue();

        public abstract string GetValueAsString();

        public virtual string GetTypeAsString()
        {
            return TypeCode;
        }

        // TODO: return a more complex type than bool, e.g. something incorporating info about exception/parse error?
        public abstract void SetValueFromString(string strValue);

        internal virtual void ReadFrom(MetaFile file)
        {
            string line = file.ReadNextRequiredLine(TypeName);
            SetValueFromString(line);
        }

        internal virtual void WriteTo(MetaFileBuilder writer)
        {
            writer.WriteLine(GetTypeAsString());
            writer.WriteLine(GetValueAsString());
        }

        public void Print(StreamWriter writer)  // = System.IO.SystemOUt ? etc
        {
            writer.Write($"{GetValue().ToString()}");
        }

        public void Print()
        {
            Console.WriteLine($"{GetValue().ToString()}");
        }










        public virtual void WriteAsXml(XmlWriter writer)
        {
            writer.WriteElementString(TypeName, GetValue().ToString());
        }

        public virtual void WriteAsVT(BinaryWriter writer)
        {
            writer.WriteAsVT(TypeCode);
            writer.WriteAsVT(GetValueAsString());
        }
    }

    public static class VTDataTypeExtensions
    {
        public static void WriteAsVT(this BinaryWriter writer, string data)
        {
            foreach (char c in data)
                writer.Write(c);
            writer.Write('\n');
        }
    }

    public class VTUnassigned : VTDataType
    {
        public VTUnassigned() : base("unassigned", "0")
        {
        }

        public override object GetValue()
        {
            return null;
        }

        public override string GetValueAsString()
        {
            return "0";
        }

        public override void SetValueFromString(string strValue)
        {
            // no-op
        }
    }

    public class VTBoolean : VTDataType
    {
        public bool Value { get; set; }

        public VTBoolean(bool initialValue) : this()
        {
            Value = initialValue;
        }
        public VTBoolean() : base("boolean", "b")
        {
        }
        public override void SetValueFromString(string strValue)
        {
            switch (strValue.ToLower())
            {
                case "true":
                case "yes":
                case "y":
                    Value = true;
                    return;
                case "false":
                case "no":
                case "n":
                    Value = false;
                    return;
                default:
                    // TODO more specific type
                    throw new IOException($"Unable to parse boolean value from string {strValue}");
            }
        }

        public override object GetValue()
        {
            return Value;
        }
        public override string GetValueAsString()
        {
            return Value.ToString();
        }

        public override void WriteAsXml(XmlWriter writer)
        {
            writer.WriteElementString("boolean", Value.ToString());
        }
    }

    public class VTInteger : VTDataType
    {
        public int Value { get; private set; }

        public VTInteger(int initialValue) : this()
        {
            Value = initialValue;
        }

        public VTInteger() : base("integer", "i")
        {

        }

        public override object GetValue()
        {
            return Value;
        }

        public override string GetValueAsString()
        {
            return Value.ToString();
        }

        public override void SetValueFromString(string strValue)
        {
            int newValue;
            if (!int.TryParse(strValue, out newValue))
            {
                throw new IOException($"Unable to parse integer from: {strValue}");
            }
            Value = newValue;
        }
    }

    public class VTUnsignedInteger : VTDataType
    {
        public uint Value { get; private set; }

        public VTUnsignedInteger(uint initialValue) : this()
        {
            Value = initialValue;
        }

        public VTUnsignedInteger() : base("uint", "u")
        {
        }
        public override object GetValue()
        {
            return Value;
        }

        public override string GetValueAsString()
        {
            return Value.ToString();
        }

        public override void SetValueFromString(string strValue)
        {
            uint newValue;
            if (!uint.TryParse(strValue, out newValue))
            {
                throw new IOException($"Unable to parse unsigned integer from: {strValue}");
            }
            Value = newValue;
        }
    }

    public class VTDouble : VTDataType
    {
        public double Value { get; private set; }

        public VTDouble(double initialValue) : this()
        {
            Value = initialValue;
        }

        public VTDouble() : base("double", "d")
        {
        }

        public override object GetValue()
        {
            return Value;
        }

        public override string GetValueAsString()
        {
            return Value.ToString();
        }

        public override void SetValueFromString(string strValue)
        {
            double newValue;
            if (!double.TryParse(strValue, out newValue))
            {
                throw new IOException($"Unable to parse double from value: {strValue}");
            }
            Value = newValue;
        }
    }

    public class VTFloat : VTDataType
    {
        public float Value { get; private set; }

        public VTFloat(float initialValue) : this()
        {
            Value = initialValue;
        }

        public VTFloat() : base("float", "f")
        {
        }

        public override object GetValue()
        {
            return Value;
        }

        public override string GetValueAsString()
        {
            return Value.ToString();
        }

        public override void SetValueFromString(string strValue)
        {
            float newValue;
            if (!float.TryParse(strValue, out newValue))
            {
                throw new IOException($"Unable to parse float from value: {strValue}");
            }
            Value = newValue;
        }
    }

    public class VTString : VTDataType
    {
        public string Value { get; private set; }

        public VTString(string initialValue) : this()
        {
            Value = initialValue;
        }

        public VTString() : base("string", "s")
        {
        }

        public override object GetValue()
        {
            return Value;
        }

        public override string GetValueAsString()
        {
            return Value;
        }

        public override void SetValueFromString(string strValue)
        {
            Value = strValue;
        }
    }

    public class VTByteArray : VTDataType
    {
        public string Value { get; private set; }

        public int ByteCount => Value.Length;

        public VTByteArray(string data) : this()
        {
            Value = data;
        }

        public VTByteArray() : base("bytearray", "ba")
        {
            /*
             * When ExternalContent == true, then the `GetValueAsString()` must be appropriately written to an external file, and the PATH
             *      of that external file is written instead of the 
             */
            ExternalContent = true;
        }

        public override object GetValue()
        {
            return Value;
        }

        public override string GetValueAsString()
        {
            return Value;
        }

        public override void SetValueFromString(string strValue)
        {
            Value = strValue;
        }

        internal override void ReadFrom(MetaFile file)
        {
            int byteCount = file.ReadNextLineAsInt();
            StringBuilder sb = new StringBuilder(byteCount);
            file.ReadNextRequiredLine($"bytearray[{byteCount}]");
            for (int i = 0; i < byteCount; i++)
            {
                char ch = (char)file.ReadNextChar();
                if (ch == 0)
                {
                    // add the line-feed as a byte read
                    i++;
                    sb.AppendLine();

                    // read another line if we have more to go...
                    if (i < byteCount)
                        file.ReadNextRequiredLine($"bytearray[{byteCount - i}]");
                    continue;
                }

                sb.Append(ch);
            }
            file.LineNumber--;
            SetValueFromString(sb.ToString());
        }

        internal override void WriteTo(MetaFileBuilder writer)
        {
            writer.WriteLine(ByteCount.ToString());
            // NO TERMINATING LINE FEED!
            writer.WriteString(Value);
        }
    }
}
