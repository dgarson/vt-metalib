using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VTMetaLib.VTank
{
    public class MetaException : Exception
    {
        public LineReadable Context { get; private set; }

        public MetaException(LineReadable context)
        {
            Context = context;
        }

        public MetaException(LineReadable context, string message) : base(message)
        {
            Context = context;
        }

        public MetaException(LineReadable context, string message, Exception inner) : base(message, inner)
        {
            Context = context;
        }
    }

    public class MetaElementNotFoundException : MetaException
    {
        public MetaElementNotFoundException(LineReadable context, string message) : base(context, message) { }
    }

    public class RecordOutOfBoundsException : MetaException
    {
        public RecordOutOfBoundsException(LineReadable context, string message) : base(context, message) { }
    }

    public class MalformedMetaException : MetaException
    {
        public MalformedMetaException(LineReadable context, string message) : base(context, message) { }
    }
}
