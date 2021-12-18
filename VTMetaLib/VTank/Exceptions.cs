using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaLib.VTank
{
    public class MetaException : Exception
    {
        public MetaContext Context { get; private set; }

        public MetaException(MetaContext context)
        {
            Context = context;
        }

        public MetaException(MetaContext context, string message) : base(message)
        {
            Context = context;
        }

        public MetaException(MetaContext context, string message, Exception inner) : base(message, inner)
        {
            Context = context;
        }
    }

    public class MetaElementNotFoundException : MetaException
    {
        public MetaElementNotFoundException(MetaContext context, string message) : base(context, message) { }
    }

    public class RecordOutOfBoundsException : MetaException
    {
        public RecordOutOfBoundsException(MetaContext context, string message) : base(context, message) { }
    }

    public class MalformedMetaException : MetaException
    {
        public MalformedMetaException(MetaContext context, string message) : base(context, message) { }
    }
}
