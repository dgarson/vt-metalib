using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VTMetaLib.afy
{
    public static class Formatting
    {
        public static string FormatDouble(double d)
        {
            return d.ToString();
        }

        public static string FormatHeading(double heading)
        {
            return FormatDouble(heading);
        }

        public static string FormatDuration(double seconds)
        {
            return FormatDouble(seconds);
        }

        public static string FormatCoordinate(double coordVal)
        {
            return FormatDouble(coordVal);
        }

        internal static void AppendCoords(List<string> tokens, double x, double y, double z)
        {
            tokens.Add(FormatCoordinate(x));
            tokens.Add(FormatCoordinate(y));
            tokens.Add(FormatCoordinate(z));
        }

        internal static string ValidateGUID(string strGuid)
        {
            // TODO
            return strGuid;
        }
    }
}
