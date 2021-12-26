using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using log4net;

namespace VTMetaLib.IO
{
    public static class Loggers
    {
        public static readonly ILog Log = LogManager.GetLogger(typeof(LineReadable));
    }

    public abstract class LineReadable
    {
        /// <summary>
        /// The meta name (filename without the 'met' extension)
        /// </summary>
        public string Name { get; protected set; }

        public virtual string GetSourceText()
        {
            return Name;
        }

        public abstract List<string> Lines { get; }

        /// <summary>
        /// Returns the current line that is being read from the file. If the end of the file has been
        /// reached, this will return null instead of throwing an exception.
        /// </summary>
        public string Line
        {
            get
            {
                return LineNumber < Lines.Count ? Lines[LineNumber] : null;
            }
        }

        public int LineNumber { get; internal set; } = 0;

        public int Column { get; internal set; } = 0;

        private string currentLine = null;


        public string this[int index]
        {
            get
            {
                return Lines[index];
            }
        }

        public void MovePreviousLine()
        {
            if (LineNumber == 0)
                return;

            LineNumber--;
            currentLine = Lines[LineNumber];
            Column = 0;
        }

        public string ReadNextLine()
        {
            if (Column > 0 && currentLine != null && Column < currentLine.Length)
            {
                string prevLineText = currentLine;
                currentLine = currentLine.Substring(Column);
                Column = 0;
                return currentLine;
            }

            if (LineNumber >= Lines.Count)
                return null;

            if (currentLine == null)
                currentLine = Lines[0];
            else
            {
                LineNumber++;
                currentLine = LineNumber < Lines.Count ? Lines[LineNumber] : null;
            }
            Column = 0;

            return currentLine;
        }

        /// <summary>
        /// This is equivalent of referencing currentLine except that, in the event of the first line, this
        /// will fetch the value for us.
        /// </summary>
        public string GetCurrentLineOrNull()
        {
            if (currentLine == null && LineNumber < Lines.Count)
                return ReadNextLine();
            else if (LineNumber >= Lines.Count)
                return null;
            else
                return currentLine;
        }

        /// <summary>
        /// Returns *up to* the previous N lines that were read, INCLUDING the current line, gracefully terminating with what has been accumulated thus far
        /// whenever we hit the beginning of the file.
        /// </summary>
        /// <param name="prevLineCount">desired number of previous lines to include, excluding the current line</param>
        /// <param name="indent">the indentation of each line returned, in spaces</param>
        /// <param name="highlightCurrentLine">whether the current line should be somehow highlighted to indicate it is the location of a failure</param>
        /// <returns></returns>
        public List<string> GetPrevNLines(int prevLineCount, int indent = 4, bool highlightCurrentLine = true)
        {
            List<string> prevLines = new List<string>();
            int lineNum = LineNumber;
            if (lineNum == Lines.Count)
                lineNum = Lines.Count - 1;

            if (lineNum >= 0)
            {
                if (highlightCurrentLine)
                    prevLines.Add(">>" + Utils.Indent(Lines[lineNum], indent - 2));
                else
                    prevLines.Add(Utils.Indent(Lines[lineNum], indent));

                lineNum--;
            }

            int count = 0;
            while (lineNum > 0 && count++ < prevLineCount)
                prevLines.Insert(0, Lines[lineNum--]);

            return prevLines;
        }

        /// <summary>
        /// Returns *up to* the next N lines that will be read, EXCLUDING the current line, gracefully terminating with what has been accumulated thus far
        /// whenever we hit the end of the file.
        /// </summary>
        /// <param name="lineCount">the max number of lines that should be returned</param>
        /// <param name="indent">the indentation of each line, in spaces</param>
        public List<string> GetNextNLines(int lineCount, int indent = 4)
        {
            List<string> nextLines = new List<string>();
            int count = 0;
            int lineNum = LineNumber + 1;
            while (lineNum < Lines.Count && ++count < lineCount)
                nextLines.Add(Lines[lineNum++]);
            return nextLines;
        }

        /// <summary>
        /// Returns the current line as well as UP to the given number of lines before, and after, the current line, based on the availability of said lines
        /// relative to the beginning and end of the entire meta file.
        /// </summary>
        public List<string> GetCurrentLineWithContext(int beforeCount, int afterCount, bool highlightCurrentLine = true, int indent = 4)
        {
            List<string> results = new List<string>();
            // add 1 so we include the current line + the number requested for context
            results.AddRange(GetPrevNLines(beforeCount, indent, highlightCurrentLine));
            results.AddRange(GetNextNLines(afterCount, indent));
            return results;
        }

        public bool HasMoreChars()
        {
            var current = GetCurrentLineOrNull();
            return current != null ? Column < current.Length - 1 : false;
        }

        /// <summary>
        /// Reads the next character from the current line, or returns an integer value of 0 if there are
        /// no characters remaining on the current line.
        /// </summary>
        public char ReadNextChar()
        {
            var current = GetCurrentLineOrNull();
            return Column >= currentLine.Length ? (char)0 : currentLine[Column++];
        }

        public string ReadNextChars(int count)
        {
            var current = GetCurrentLineOrNull();
            StringBuilder sb = new StringBuilder(count);
            for (int i = 0; i < count; i++)
            {
                if (Column >= currentLine.Length)
                    ReadNextRequiredLine("nextChars with " + (i - count) + " remaining");
                // throw new IndexOutOfRangeException($"Unable to get column #{Column} when line only has {currentLine.Length} characters: \"{current}\"");
                sb.Append(currentLine[Column++]);
            }
            return sb.ToString();
        }

        public string ReadNextRequiredLine(string reason)
        {
            string nextLine = ReadNextLine();
            if (nextLine == null)
                throw new InvalidOperationException($"Unable to read another line for '{reason}' since no lines remaining after {Lines.Count} lines were read.");
            return nextLine;
        }

        public string ReadNextLineAsString()
        {
            return ReadNextRequiredLine("string");
        }

        /// <summary>
        /// Reads the next line and parses it as a double value.
        /// </summary>
        public double ReadNextLineAsDouble()
        {
            string str = ReadNextRequiredLine("double");
            double val;
            if (!double.TryParse(str, out val))
                throw new ArgumentException($"Invalid double value: {str}");
            return val;
        }

        public float ReadNextLineAsFloat()
        {
            string str = ReadNextRequiredLine("float");
            float val;
            if (!float.TryParse(str, out val))
                throw new ArgumentException($"Invalid float value: {str}");
            return val;
        }

        public int ReadNextLineAsInt()
        {
            string str = ReadNextRequiredLine("int");
            int val;
            if (!int.TryParse(str, out val))
                throw new ArgumentException($"Invalid integer value: {str}");
            return val;
        }

        public uint ReadNextLineAsUInt()
        {
            string str = ReadNextRequiredLine("uint");
            uint val;
            if (!uint.TryParse(str, out val))
                throw new ArgumentException($"Invalid unsigned integer value: {str}");
            return val;
        }

        public bool ReadNextLineAsBoolean()
        {
            string orig = ReadNextRequiredLine("boolean");
            string str = orig.ToLower();
            if (str == "y" || str == "true" || str == "1")
                return true;
            else if (str == "n" || str == "false" || str == "0")
                return false;
            else
                throw new ArgumentException($"Invalid boolean value: {orig}");
        }

        public bool HasMoreLines()
        {
            return LineNumber + 1 < Lines.Count;
        }

        public void Restart()
        {
            LineNumber = Column = 0;
        }

        public void Clear()
        {
            Restart();
            Column = 0;
            Lines.Clear();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var line in Lines)
                sb.AppendLine(line);
            return sb.ToString();
        }
    }

    public class InMemoryLines : LineReadable
    {

        public override List<string> Lines { get; } = new List<string>();

        public InMemoryLines(IEnumerable<string> lines = null)
        {
            if (lines != null)
                Lines.AddRange(lines);
        }

        public InMemoryLines AddLine(string line)
        {
            Lines.Add(line);
            return this;
        }

        public static InMemoryLines ReadAllFrom(TextReader reader)
        {
            string line;
            InMemoryLines lines = new InMemoryLines();
            while ((line = reader.ReadLine()) != null)
                lines.AddLine(line);
            return lines;
        }
    }
}
