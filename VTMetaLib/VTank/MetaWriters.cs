using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaLib.VTank
{
    public class MetaFileBuilder
    {
        private StringBuilder lineBuilder = null;

        public MetaFile File
        {
            get
            {
                return FileContext.MetaFile;
            }
        }

        public MetaFileContext FileContext { get; private set; }

        public MetaContext MetaContext
        {
            get
            {
                return FileContext.MetaContext;
            }
        }

        public MetaFileBuilder(MetaFileType fileType, string path) : this(MetaFiles.CreateContextForNewFile(fileType, path))
        {
        }

        public MetaFileBuilder(MetaFileContext context)
        {
            FileContext = context;
        }

        /// <summary>
        /// Begins writing the next line, resetting the column number and the line builder. If there is any
        /// line that was already being built, this will add it to FileLines, by default, prior to beginning
        /// this next line.
        /// </summary>
        public void BeginWritingNextLine(bool writeCurrentLineIfNotEmpty = true)
        {
            if (writeCurrentLineIfNotEmpty && lineBuilder.Length > 0)
                AddLineFromBuilder();

            File.Column = 0;
            lineBuilder.Clear();
        }

        /// <summary>
        /// Appends the given string to the current line that is being built. This does not yet add it to the
        /// FileLines, as the line is not considered complete yet. The corresponding Column number is incremented
        /// by 1 as a result.
        public void WriteChar(char ch)
        {
            // if we encounter a newline, that should always flush the current line to the file/buffer
            if (ch == '\n')
            {
                // TODO: log this
                FinishWritingLine(true);
                return;
            }

            lineBuilder.Append(ch);
            File.Column++;
        }

        /// <summary>
        /// Appends the given string to the current line that is being built. This does not yet add it to the
        /// FileLines, as the line is not considered complete yet. The corresponding Column number is incremented
        /// by the length of the given string as a result.
        /// </summary>
        public void WriteString(string str)
        {
            lineBuilder.Append(str);
            File.Column += str.Length;
        }

        /// <summary>
        /// Finishes writing the current line, indicating whether a blank line should be written, if there is no
        /// current text in the lineBuilder.
        /// </summary>
        public void FinishWritingLine(bool writeIfEmpty = false)
        {
            if (writeIfEmpty || lineBuilder.Length > 0)
                AddLineFromBuilder();
        }

        /// <summary>
        /// Writes an entire line to the meta, bypassing any line building. This increments the line number
        /// and clears the lineBuilder as well as resetting the column number. This does not check for a blank
        /// string and will always add the given line to FileLines.
        /// </summary>
        public void WriteLine(string line)
        {
            AddLineToEnd(line);
        }

        public void WriteData(VTDataType data)
        {
            data.WriteTo(this);
        }

        internal void AddLineFromBuilder()
        {
            AddLineToEnd(lineBuilder.ToString());
        }

        internal void AddLineToEnd(string line)
        {
            File.FileLines.Add(line);
            File.LineNumber++;
            File.Column = 0;
            lineBuilder.Clear();
        }
    }
}
