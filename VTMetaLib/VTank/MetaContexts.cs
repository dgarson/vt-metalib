using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using log4net;

namespace MetaLib.VTank
{
    public class MetaContext
    {
        private readonly Dictionary<string, string> NavRoutesByPath = new Dictionary<string, string>();

        /// <summary>
        /// The parent meta file that is being read from or written to. This keeps track of our cursor/line
        /// in the corresponding file.
        /// </summary>
        public MetaFileContext FileContext { get; internal set; }

        public MetaFile MetaFile
        {
            get
            {
                return FileContext.MetaFile;
            }
        }

        /// <summary>
        /// The name of the state being read/written
        /// </summary>
        public string CurrentState { get; set; }

        /// <summary>
        /// The name of the type that is being read/written
        /// </summary>
        public string CurrentValueTypeName { get; set; }

        /// <summary>
        /// Stack of types that are being read, going into nested types where application. A peek() indicates the type that is being read now,
        /// or was most recently attempted to be read, in even of failure.
        /// </summary>
        public Stack<Type> TypesBeingRead { get; } = new Stack<Type>();

        /// <summary>
        /// A stack of the tables that are above the current record being read from the MetaFile. If there
        /// is no parent table in the stack, then the record is part of the State root.
        /// </summary>
        public Stack<VTTable> ParentTables { get; } = new Stack<VTTable>();

        /// <summary>
        /// The text value for the current line, if reading from an input file
        /// </summary>
        public string CurrentValueText { get; set; }
        /// <summary>
        /// The VTank data value that is for the current line, either parsed from (for reading) or being used to write to (for output)
        /// </summary>
        public VTDataType CurrentLineValue { get; set; }

        public MetaContext(MetaFileContext context)
        {
            FileContext = context;
        }

        /// <summary>
        /// Returns the the current table, if within a table, otherwise returns null.
        /// </summary>
        public VTTable CurrentTable()
        {
            return ParentTables.Count > 0 ? ParentTables.Peek() : null;
        }

        /// <summary>
        /// Checks whether we are actively reading/writing data from/to a table
        /// </summary>
        public bool IsInTable()
        {
            return ParentTables.Count > 0;
        }

        /// <summary>
        /// Method that should be called prior to entering a nested VTTable, pushing it onto the stack.
        /// </summary>

        public void BeginTable(VTTable table)
        {
            ParentTables.Push(table);
        }

        public Type CurrentlyReadingType
        {
            get
            {
                return TypesBeingRead.Count > 0 ? TypesBeingRead.Peek() : null;
            }
        }

        public void BeginReadingType(Type readingType)
        {
            TypesBeingRead.Push(readingType);
        }

        public Type FinishReadingType()
        {
            if (TypesBeingRead.Count == 0)
            {
                FileContext.Error("Unable to FinishReadingType because no VT data types were being read!");
                return null;
            }
            return TypesBeingRead.Pop();
        }

        /// <summary>
        /// Method that should be called after finishing reading any data/records for a nested VTTable. It pops the current table off of the
        /// parent table name stack and allows future records to be read within the context of that parent VTTable.
        /// If no parent table is present, then the data belongs to the root / a state itself
        /// </summary>
        public VTTable EndTable()
        {
            return ParentTables.Pop();
        }

        public string GetNavRouteFromFilesystem(string path)
        {
            string route;
            if (!NavRoutesByPath.TryGetValue(path, out route))
            {
                if (!File.Exists(path))
                {
                    FileContext.Error($"Unable to load nav route from filesystem because it does not exist: {path}");
                    throw NotFound($"Unable to load nav route from filesystem because it does not exist: {path}");
                }
                FileContext.Debug($"Loading nav route for first time from filesystem: {path}");
                route = File.ReadAllText(path);
                NavRoutesByPath[path] = route;
            }
            return route;
        }

        /// <summary>
        /// Captures the current state of this MetaContext in a duplicate object to avoid updates from any further processing. The referenced MetaFile
        /// will be the same.
        /// </summary>
        /// <returns></returns>
        public MetaContextState Memoize()
        {
            // TODO FIXME PLZ and update the Exception classes to use MetaContextState
            //////
            ///
            return null;
        }

        /// <summary>
        /// Returns an exception that captures the current state of the MetaContext, by cloning its state and attaching it to the returned exception.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public MalformedMetaException MalformedFor(string message)
        {
            return new MalformedMetaException(this, message);
        }

        public MetaElementNotFoundException NotFound(string message)
        {
            return new MetaElementNotFoundException(this, message);
        }
    }

    public class MetaFileContext
    {
        public MetaFile MetaFile { get; internal set; }

        public MetaContext MetaContext { get; internal set; }

        public MetaFileReader Reader { get; internal set; }

        public bool IsNewFile { get; private set; }

        public MetaFileContext(MetaFileType fileType, string path, List<string> lines)
        {
            MetaFile = new MetaFile(fileType, path, lines);
            MetaContext = new MetaContext(this);
            Reader = new MetaFileReader(this);
            IsNewFile = !File.Exists(path);
        }

        public MetaFileContext(MetaFileType fileType, string path, bool writing = false)
        {
            if (writing)
                MetaFile = new MetaFile(fileType, path);
            else
            {
                if (!File.Exists(path))
                    throw new FileNotFoundException($"Unable to find meta file at path: {path}");
                List<string> lines = MetaFiles.ReadAllLines(path);
                MetaFile = new MetaFile(fileType, path, lines);
            }
            MetaContext = new MetaContext(this);
            Reader = new MetaFileReader(this);
            IsNewFile = !File.Exists(path);
        }
    }

    /// <summary>
    /// Immutable version of MetaContext that also incorporates some data from the MetaFile.
    /// </summary>
    public class MetaContextState
    {
        /// <summary>
        /// The parent meta file that is being read from or written to. This keeps track of our cursor/line
        /// in the corresponding file.
        /// </summary>
        public MetaFile MetaFile { get; internal set; }

        /// <summary>
        /// The name of the state being read/written
        /// </summary>
        public string CurrentState { get; set; }

        /// <summary>
        /// The name of the type that is being read/written
        /// </summary>
        public string CurrentValueTypeName { get; set; }

        /// <summary>
        /// The type of the corresponding VTDataType implementation class
        /// </summary>
        public Type Type { get; internal set; }
    }
}
