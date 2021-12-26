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
            List<string> lines = ReadAllLines(path);
            MetaFile file = new MetaFile(MetaFileType.MetaFile, path, lines);

            VTMeta meta = new VTMeta();
            meta.ReadFrom(file);
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

    public class MetaFile : LineReadable
    {

        /// <summary>
        /// The meta file path on the disk
        /// </summary>
        public string Path { get; private set; }

        public override List<string> Lines { get; } = new List<string>();

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
                writer.Write(ToString());
            }
            Loggers.WriterLog.Info($"Finished writing meta file: {Path}");
        }

        /// <summary>
        /// Returns the text that should be displayed when indicating the source file where an error occurred. If the path name was provided, then it will be
        /// included, otherwise this will just return the meta name (in-memory building of metas?)
        /// </summary>
        /// <returns></returns>
        public override string GetSourceText()
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
