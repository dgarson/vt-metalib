using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using VTMetaLib.IO;

namespace VTMetaLib.VTank
{
    public enum ContextInformation
    {
        None,
        SourceLocation,
        CurrentLine,
        SurroundingLines
    }

    public static class Loggers
    {

        public static bool IncludeSurroundingLinesInContext { get; set; } = true;

        public static ILog EncodingLog = LogManager.GetLogger("VTank.Encoding");
        public static ILog ReaderLog = LogManager.GetLogger("VTank.Reader");
        public static ILog WriterLog = LogManager.GetLogger("VTank.Writer");

        public static ILog Log = LogManager.GetLogger("VTank.Log");

        public static void Info(this SeekableCharStream context, string message, ContextInformation contextInfo = ContextInformation.None, Exception exc = null)
        {
            string formatted = FormatMessageForContext(context, message, contextInfo);
            if (exc != null)
                Log.Info(formatted, exc);
            else
                Log.Info(formatted);
        }

        public static void Warn(this SeekableCharStream context, string message, ContextInformation contextInfo = ContextInformation.None, Exception exc = null)
        {
            string formatted = FormatMessageForContext(context, message, contextInfo);
            if (exc != null)
                Log.Warn(formatted, exc);
            else
                Log.Warn(formatted);
        }

        public static void Error(this SeekableCharStream context, string message, ContextInformation contextInfo = ContextInformation.None, Exception exc = null)
        {
            string formatted = FormatMessageForContext(context, message, contextInfo);
            if (exc != null)
                Log.Error(formatted, exc);
            else
                Log.Error(formatted);
        }

        public static void Debug(this SeekableCharStream context, string message, ContextInformation contextInfo = ContextInformation.None, Exception exc = null)
        {
            string formatted = FormatMessageForContext(context, message, contextInfo);
            if (exc != null)
                Log.Debug(formatted, exc);
            else
                Log.Debug(formatted);
        }

        internal static string FormatMessageForContext(this SeekableCharStream file, string message, ContextInformation contextInfo)
        {
            if (contextInfo == ContextInformation.None)
                return message;

            // StringBuilder msg = new StringBuilder($"{file.GetSourceText()} (line #{file.LineNumber}");
            StringBuilder msg = new StringBuilder($"(on Line #{file.LineNumber}");
            if (file.Column > 0)
                msg.Append($", column #{file.Column})");
            else
                msg.Append(')');

            if (contextInfo == ContextInformation.SourceLocation)
            {
                msg.Append($": {message}");
                return msg.ToString();
            }

            msg.Append($")");

            if (contextInfo == ContextInformation.CurrentLine)
            {
                msg.Append($" at: \"{file.CurrentLine}\"");
                return msg.ToString();
            }


            // include up to 4 preceding lines for context
            List<string> prevLines = file.GetPreviousLines(4);
            foreach (var line in prevLines)
                msg.Append($"{line}\n");
            msg.Append($"\"{file.CurrentLine}\"");
            return msg.ToString();
        }
    }
}
