using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using VTMetaLib.VTank;
using VTMetaLib.MIMB;

namespace vtm
{
    internal class Program
    {
        private static readonly string DefaultMetaLoadPath = "C:\\Games\\VirindiPlugins\\VirindiTank\\AlastorAugGem.met";
        // private static readonly string DefaultMetaLoadPath = "C:\\temp\\universalremote.met";
        // private static readonly string DefaultMetaLoadPath = "C:\\temp\\AutoColoFinal.met";
        private static readonly string MimbXmlFilePath = "C:\\dev\\metas\\AlMac\\AlastorAugGem.xml";

        static void Main(string[] args)
        {
            string filePath = args.Length > 1 ? args[1] : DefaultMetaLoadPath;
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Unable to find file: {filePath}");
                return;
            }

            int prevNavRoutesLoaded = NavRoutes.NavRoutesLoaded;
            VTMeta meta = MetaFiles.LoadMetaFile(filePath);
            Console.WriteLine($"\n{meta.States.Count} States with {meta.Rules.Count} total Rules and {NavRoutes.NavRoutesLoaded - prevNavRoutesLoaded} Nav Routes:");
            List<string> stateNames = new List<string>();
            foreach (var entry in meta.States)
            {
                string name = entry.Key as string;
                List<VTRule> rules = entry.Value as List<VTRule>;
                Console.WriteLine($"\tState[{name}] with {rules.Count} rules");
            }
            stateNames.AddRange(meta.StateNames);
            stateNames.Sort();
            Console.WriteLine($"Successfully loaded meta from {filePath} with {meta.LineCount} lines consisting of a total of {meta.Rules.Count} rules among {stateNames.Count} distinct meta states");

            MetaFileBuilder builder = new MetaFileBuilder(MetaFileType.MetaFile, "C:\\temp\\test.met");
            meta.WriteTo(builder);
            builder.File.FlushToDisk();




            // try MiMB XML
            VTMeta metaFromMimb = Mimb.LoadMetaFromMimbXml(MimbXmlFilePath);
            Console.WriteLine($"Successfully produced VTMeta from Mimb XML file: {MimbXmlFilePath}, total rules: {metaFromMimb.Rules.Count}");

            builder = new MetaFileBuilder(MetaFileType.MetaFile, "C:\\temp\\testmimb.met");
            metaFromMimb.WriteTo(builder);
            builder.File.FlushToDisk();
            Console.WriteLine($"Successfully exported VTMeta to met file: {builder.File.Path}");
        }
    }
}
