using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace VTMetaLib.IO
{
    public class SeekableCharStream
    {
        private char[] contents;

        private int pos;

        private List<string> lines = new List<string>();
        private StringBuilder currentLine = new StringBuilder();

        internal SeekableCharStream(char[] contents)
        {
            this.contents = contents;
        }

        public char this[int index] { get => contents[index]; }

        public int Length { get { return contents.Length; } }

        public bool IsEOF { get => pos >= contents.Length; }

        public bool HasNext { get => pos < contents.Length; }

        public bool HasMoreChars(int count)
        {
            return pos + count < contents.Length;
        }

        public List<string> Lines { get => lines; }

        public int LineNumber { get => lines.Count; }

        public string CurrentLine { get => currentLine.ToString(); }

        public int Column { get => currentLine.Length; }

        /// <summary>
        /// Reads a character from this stream and pushes it to the <tt>ch</tt> out parameter. This method will return true if a character was in
        /// fact read from the stream, or false if there were no more characters to read, in which case the value of <tt>ch</tt> should be disregarded
        /// </summary>
        /// <param name="ch">set to the character read from this stream</param>
        /// <returns>true if a character was read, false if we were already at the end of this stream</returns>
        public bool NextChar(out char ch)
        {
            if (IsEOF)
            {
                ch = default(char);
                return false;
            }
            ch = contents[pos++];
            return true;
        }

        public char CurrentChar { get => pos == 0 ? contents[0] : contents[pos - 1]; }

        /// <summary>
        /// Consumes UP TO a given number of characters from the character stream. The number of characters returned may be fewer than the
        /// number desired if the end of stream is reached.
        /// </summary>
        /// <param name="count">the desired maximum number of characters to read</param>
        /// <param name="includeLF">whether line feeds should be counted as a character and included in the return value</param>
        /// <returns>string containing the consumed characters, or <tt>null</tt> if already at end of stream</returns>
        public string ConsumeChars(int count, bool includeLF = true)
        {
            if (IsEOF)
                return null;
            else if (count == 0)
                return "";
            char ch;
            if (count == 1) {
                if (NextChar(out ch))
                {
                    if (includeLF || (ch != '\r' && ch != '\n'))
                        currentLine.Append(ch);
                    return new string(new char[] { ch });
                }
                // nothing left in stream
                return null;
            }
            StringBuilder sb = new StringBuilder();
            while (sb.Length < count && NextChar(out ch))
            {
                // check whether we are allowed to include this character
                if (includeLF || (ch != '\r' && ch != '\n'))
                {
                    AppendChar(sb, ch);
                }
            }
            // only finish the line at this time if we know there are absolutely no characters left to read in the entire file
            if (IsEOF)
                FinishCurrentLine();

            // return the characters read
            return sb.ToString();
        }

        /// <summary>
        /// Checks for the pair of \r\n line feed separator vs just \n and processes both, returning true if a new line began
        /// <b>AND</b> we are not including new lines/LFs.
        /// </summary>
        /// <param name="sb">the string builder to append characters to</param>
        /// <param name="ch">the most recently read character</param>
        /// <param name="includeLF">true if \r and \n should be included in characters append to <tt>sb</tt></param>
        /// <returns></returns>
        private bool AppendAndCheckForNewLines(StringBuilder sb, char ch, bool includeLF)
        {
            if (IsEOF)
                return false;

            if (ch == '\r' || ch == '\n')
            {
                // include \r if desired
                if (includeLF)
                    AppendChar(sb, ch);

                if (ch == '\r' && pos < Length && contents[pos] == '\n')
                {
                    // make sure to append this character if including LFs because it is not iterated over in loop
                    if (includeLF)
                        AppendChar(sb, '\n');

                    // consume the '\n'
                    pos++;
                }

                // append currentLine to the lines list and then clear that buffer
                FinishCurrentLine();

                // a new line was detected, so return as much
                return true;
            }
            else
            {
                // append all other characters no matter what
                AppendChar(sb, ch);
                return false;
            }

        }

        private void AppendChar(StringBuilder sb, char ch)
        {
            sb.Append(ch);
            currentLine.Append(ch);
        }

        private string FinishCurrentLine()
        {
            string line = currentLine.ToString();
            lines.Add(line);
            currentLine.Clear();
            return line;
        }

        /// <summary>
        /// Consumes characters in the stream until either the end of line is reached (first occurrence of \r\n, \n or \r) or until the end of the 
        /// entire file is read, returning the contents that were read, with or without the terminating line feed(s). The default for this method is
        /// to exclude line feeds from the return value, unless otherwise specified.
        /// </summary>
        /// <param name="skipLeadingLF">if true, then any dangling newline characters at the VERY beginning of this consumed line will be ignored (default true)</param>
        /// <param name="includeLF">if true, then any line feed characters will be included in the returned string, but this will still terminate after the first ones encountered</param>
        /// <returns>string containing the consumed characters, or <tt>null</tt> if already at end of stream</returns>
        public string ConsumeLine(bool skipLeadingLF = true, bool includeLF = false)
        {
            if (IsEOF)
                return null;

            if (skipLeadingLF)
            {
                // skip all leading line feeds & carriage returns (in case we are starting at the "end" of the previous line, e.g. reading byte arrays)
                int startPos = pos;
                while (pos < Length && (contents[pos] == '\r' || contents[pos] == '\n'))
                    pos++;
                if (pos != startPos)
                {
                    // finish "previous" line now
                    string prevLine = currentLine.ToString();
                    lines.Add(prevLine);
                    currentLine.Clear();
                }
            }

            StringBuilder sb = new StringBuilder();
            bool eol = false;
            while (!eol && pos < Length)
            {
                char ch = contents[pos++];
                eol = AppendAndCheckForNewLines(sb, ch, includeLF);
            }
            return sb.ToString();
        }

        private FilePositionContext CreateLineContext()
        {
            return new FilePositionContext(lines.Count, GetPreviousLines(5), currentLine.ToString());
        }

        public List<string> GetPreviousLines(int maxCount)
        {
            List<string> prevLines = new List<string>();
            int startLineNum = Math.Max(lines.Count - maxCount, 0);
            int actualCount = lines.Count - startLineNum;
            for (int lineNum = startLineNum; lineNum < lines.Count; lineNum++)
                prevLines.Add(lines[lineNum]);
            return prevLines;
        }

        public static SeekableCharStream FromText(string text)
        {
            return new SeekableCharStream(text.ToCharArray());
        }

        public static SeekableCharStream FromFile(string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open))
                return FromStream(fs);
        }

        public static SeekableCharStream FromStream(Stream input)
        {
            using (StreamReader sr = new StreamReader(input, Encoding.UTF8))
            {
                string allText = sr.ReadToEnd();
                return FromText(allText);
            }
        }

        public static SeekableCharStream FromLines(List<string> lines)
        {
            return FromText(string.Join("\n", lines));
        }
    }

    public class FilePositionContext
    {
        public int LineNumber { get; private set; }
        public List<string> LineContext { get; private set; }
        public string CurrentLine { get; private set; }
        public int Column { get => CurrentLine.Length; }

        public FilePositionContext(int lineNumber, List<string> lineContext, string currentLine)
        {
            LineContext = lineContext;
            CurrentLine = currentLine;
            LineNumber = lineNumber;   
        }
    }

    public class InMemorySeekableCharStream : SeekableCharStream
    {
        public InMemorySeekableCharStream(List<string> lines) : base(string.Join('\n', lines.ToArray()).ToCharArray())
        {
        }
    }

    public static class SeekableCharStreamExtensions
    {
        public static string ReadNextRequiredLine(this SeekableCharStream stream, string reason, bool skipLeadingLF = true)
        {
            string nextLine = stream.ConsumeLine(skipLeadingLF);
            if (nextLine == null)
                throw new InvalidOperationException($"Unable to read another line for '{reason}' since no lines remaining after {stream.Lines.Count} lines were read.");
            return nextLine;
        }

        public static string ReadNextLineAsString(this SeekableCharStream stream)
        {
            return stream.ReadNextRequiredLine("string", false);
            // return stream.ReadNextRequiredLine("string");
        }

        /// <summary>
        /// Reads the next line and parses it as a double value.
        /// </summary>
        public static double ReadNextLineAsDouble(this SeekableCharStream stream)
        {
            string str = stream.ReadNextRequiredLine("double");
            double val;
            if (!double.TryParse(str, out val))
                throw new ArgumentException($"Invalid double value: {str}");
            return val;
        }

        public static float ReadNextLineAsFloat(this SeekableCharStream stream)
        {
            string str = stream.ReadNextRequiredLine("float");
            float val;
            if (!float.TryParse(str, out val))
                throw new ArgumentException($"Invalid float value: {str}");
            return val;
        }

        public static int ReadNextLineAsInt(this SeekableCharStream stream)
        {
            string str = stream.ReadNextRequiredLine("int");
            int val;
            if (!int.TryParse(str, out val))
                throw new ArgumentException($"Invalid integer value: {str}");
            return val;
        }

        public static uint ReadNextLineAsUInt(this SeekableCharStream stream)
        {
            string str = stream.ReadNextRequiredLine("uint");
            uint val;
            if (!uint.TryParse(str, out val))
                throw new ArgumentException($"Invalid unsigned integer value: {str}");
            return val;
        }

        public static bool ReadNextLineAsBoolean(this SeekableCharStream stream)
        {
            string orig = stream.ReadNextRequiredLine("boolean");
            string str = orig.ToLower();
            if (str == "y" || str == "true" || str == "1")
                return true;
            else if (str == "n" || str == "false" || str == "0")
                return false;
            else
                throw new ArgumentException($"Invalid boolean value: {orig}");
        }
    }
}
