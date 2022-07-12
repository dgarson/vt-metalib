using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VTMetaLib.IO;

namespace VTMetaLib.VTank
{
    public class MetaException : Exception
    {
        public SeekableCharStream Context { get; private set; }

        public MetaException(SeekableCharStream context)
        {
            Context = context;
        }

        public MetaException(SeekableCharStream context, string message) : base(message)
        {
            Context = context;
        }

        public MetaException(SeekableCharStream context, string message, Exception inner) : base(message, inner)
        {
            Context = context;
        }
    }

    public class MetaElementNotFoundException : MetaException
    {
        public MetaElementNotFoundException(SeekableCharStream context, string message) : base(context, message) { }
    }

    public class RecordOutOfBoundsException : MetaException
    {
        public RecordOutOfBoundsException(SeekableCharStream context, string message) : base(context, message) { }
    }

    public class MalformedMetaException : MetaException
    {
        public MalformedMetaException(SeekableCharStream context, string message) : base(context, message) { }
    }
}
