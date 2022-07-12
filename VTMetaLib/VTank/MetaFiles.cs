using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using VTMetaLib.IO;

namespace VTMetaLib.VTank
{
    public enum MetaFileType
    {
        MetaFile,
        NavRoute,
        SettingsProfile,
    }

    public static class MetaFiles
    {
        public static VTMeta LoadMetaFile(string path)
        {
            // List<string> lines = ReadAllLines(path);
            // MetaFile file = new MetaFile(MetaFileType.MetaFile, path, lines);
            SeekableCharStream reader = SeekableCharStream.FromFile(path);

            VTMeta meta = new VTMeta();
            meta.ReadFrom(reader);
            return meta;
        }

        public static List<string> ReadAllLines(string path)
        {
            using (StreamReader reader = new StreamReader(path))
            {
                List<string> lines = new List<string>();
                string line;
                while ((line = reader.ReadLine()) != null)
                    lines.Add(line);
                return lines;
            }
        }

        public static MetaFileContext ReadFileAndCreateContext(string path)
        {
            return ReadFileAndCreateContext(MetaFileType.MetaFile, path);
        }

        public static MetaFileContext ReadFileAndCreateContext(MetaFileType fileType, string path)
        {
            List<string> lines = ReadAllLines(path);
            return new MetaFileContext(fileType, path, lines);
        }

        public static MetaFileContext CreateContextForNewFile(MetaFileType fileType, string path)
        {
            return new MetaFileContext(fileType, path, true);
        }
    }

    public class MetaFile
    {

        /// <summary>
        /// The meta file path on the disk
        /// </summary>
        public string Path { get; private set; }

        public string Name { get; private set; }

        private List<string> writtenLines;

        public List<string> Lines
        {
            get
            {
                if (Reader != null)
                    return Reader.Lines;
                else if (writtenLines == null)
                    writtenLines = new List<string>();
                return writtenLines;
            }
        }


        public SeekableCharStream Reader { get; private set; }

        public bool HasPath
        {
            get
            {
                return !string.IsNullOrEmpty(Path);
            }
        }

        public bool FileExists
        {
            get
            {
                return !string.IsNullOrEmpty(Path) && File.Exists(Path);
            }
        }

        /// <summary>
        /// Flushes the current meta file contained in this object to the local filesystem. If the Path already exists, it will be overwritten, not appended to.
        /// </summary>
        public void FlushToDisk()
        {
            if (File.Exists(Path))
                Loggers.WriterLog.Info($"Overwriting existing meta file while writing: {Path}");
            Loggers.WriterLog.Info($"Writing meta to file: {Path}...");
            using (StreamWriter writer = new StreamWriter(Path, false))
            {
                // simply write the current FileLines to the target path
                writer.Write(string.Join("\r\n", writtenLines));
            }
            Loggers.WriterLog.Info($"Finished writing meta file: {Path}");
        }

        public void Error(string msg)
        {
            Loggers.WriterLog.Error(msg);
        }

        public void Debug(string msg)
        {
            Loggers.WriterLog.Debug(msg);
        }

        /// <summary>
        /// Returns the text that should be displayed when indicating the source file where an error occurred. If the path name was provided, then it will be
        /// included, otherwise this will just return the meta name (in-memory building of metas?)
        /// </summary>
        /// <returns></returns>
        public string GetSourceText()
        {
            return HasPath ? $"{Name} ({Path})" : Name;
        }

        /// <summary>
        /// The meta file type that is being read from or written to
        /// </summary>
        public MetaFileType FileType { get; private set; }

        public MetaFile(MetaFileType fileType, string path, List<string> lines = null, string name = "")
        {
            FileType = fileType;
            Name = string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(path) ? GetNameFromPath(path) : name;
            Path = string.IsNullOrEmpty(path) ? name : path;
            if (lines != null)
                Lines.AddRange(lines);

            if (FileExists && lines == null)
                Reader = SeekableCharStream.FromFile(Path);
        }

        public static string GetNameFromPath(string path)
        {
            int lastSlash = path.LastIndexOfAny(new char[] {'\\', '/'});
            string afterSlash = lastSlash >= 0 ? path.Substring(lastSlash + 1) : path;
            int lastDot = afterSlash.LastIndexOf('.');
            return lastDot > 0 ? afterSlash.Substring(0, lastDot) : afterSlash;
        }
    }
}
