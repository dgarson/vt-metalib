﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;

namespace MetaLib.VTank
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

        public static void Info(this MetaFileContext context, string message, ContextInformation contextInfo = ContextInformation.None, Exception exc = null)
        {
            string formatted = FormatMessageForContext(context, message, contextInfo);
            if (exc != null)
                Log.Info(formatted, exc);
            else
                Log.Info(formatted);
        }

        public static void Warn(this MetaFileContext context, string message, ContextInformation contextInfo = ContextInformation.None, Exception exc = null)
        {
            string formatted = FormatMessageForContext(context, message, contextInfo);
            if (exc != null)
                Log.Warn(formatted, exc);
            else
                Log.Warn(formatted);
        }

        public static void Error(this MetaFileContext context, string message, ContextInformation contextInfo = ContextInformation.None, Exception exc = null)
        {
            string formatted = FormatMessageForContext(context, message, contextInfo);
            if (exc != null)
                Log.Error(formatted, exc);
            else
                Log.Error(formatted);
        }

        public static void Debug(this MetaFileContext context, string message, ContextInformation contextInfo = ContextInformation.None, Exception exc = null)
        {
            string formatted = FormatMessageForContext(context, message, contextInfo);
            if (exc != null)
                Log.Debug(formatted, exc);
            else
                Log.Debug(formatted);
        }

        internal static string FormatMessageForContext(this MetaFileContext context, string message, ContextInformation contextInfo)
        {
            if (contextInfo == ContextInformation.None)
                return message;

            MetaFile file = context.MetaFile;
            StringBuilder msg = new StringBuilder($"{file.GetSourceText()} (line #{file.LineNumber}");
            if (file.Column > 0)
                msg.Append($", column #{file.Column})");
            else
                msg.Append(")");

            if (contextInfo == ContextInformation.SourceLocation)
            {
                msg.Append($": {message}");
                return msg.ToString();
            }

            msg.Append($") for type {context.MetaContext.CurrentlyReadingType}");

            if (contextInfo == ContextInformation.CurrentLine)
            {
                msg.Append($" at: \"{file.GetCurrentLineOrNull()}\"");
                return msg.ToString();
            }


            // Include all surrounding lines (up to 4 before and up to 4 after)
            List<string> lines = file.GetCurrentLineWithContext(4, 2, true, 4);
            if (lines.Count == 1)
                msg.Append($"\"{lines[0]}\"");
            else
            {
                foreach (var line in lines)
                    msg.Append($"{line}\n");
            }

            return msg.ToString();
        }

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