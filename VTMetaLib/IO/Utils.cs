using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VTMetaLib.IO
{
    public static class Utils
    {

        public static readonly string SPACES = "                                                                                         ";

        public static string Indent(string str, int indentation)
        {
            while (indentation > 0)
            {
                int appended = indentation >= SPACES.Length ? SPACES.Length : indentation;
                str = SPACES.Substring(0, appended) + str;
                indentation -= appended;
            }
            return str;
        }
    }
}
