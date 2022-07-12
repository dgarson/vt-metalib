using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace VTMetaLib.VTank
{
    public class MetaFileBuilder
    {
        private StringBuilder lineBuilder = new StringBuilder();
        private List<string> lines = new List<string>();


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

        public void FlushToDisk(string path)
        {
            if (File.Exists(path))
                Loggers.WriterLog.Info($"Overwriting existing meta file while writing: {path}");
            Loggers.WriterLog.Info($"Writing meta to file: {path}...");
            using (StreamWriter writer = new StreamWriter(path, false))
            {
                // simply write the current FileLines to the target path
                writer.Write(string.Join("\r\n", lines));
            }
            Loggers.WriterLog.Info($"Finished writing meta file: {path}");
        }

        /// <summary>
        /// Appends the given string to the current line that is being built. This does not yet add it to the
        /// FileLines, as the line is not considered complete yet. The corresponding Column number is incremented
        /// by the length of the given string as a result.
        /// </summary>
        public void WriteString(string str)
        {
            lineBuilder.Append(str);
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
            lineBuilder.Append(line);
            AddLineFromBuilder();
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
            lines.Add(line);
            lineBuilder.Clear();
        }


    }
}
