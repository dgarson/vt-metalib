using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VTMetaLib.VTank
{
    public class MetaException : Exception
    {
        public MetaFile Context { get; private set; }

        public MetaException(MetaFile context)
        {
            Context = context;
        }

        public MetaException(MetaFile context, string message) : base(message)
        {
            Context = context;
        }

        public MetaException(MetaFile context, string message, Exception inner) : base(message, inner)
        {
            Context = context;
        }
    }

    public class MetaElementNotFoundException : MetaException
    {
        public MetaElementNotFoundException(MetaFile context, string message) : base(context, message) { }
    }

    public class RecordOutOfBoundsException : MetaException
    {
        public RecordOutOfBoundsException(MetaFile context, string message) : base(context, message) { }
    }

    public class MalformedMetaException : MetaException
    {
        public MalformedMetaException(MetaFile context, string message) : base(context, message) { }
    }
}
