using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using VTMetaLib.VTank;
using VTMetaLib.MIMB;
using YamlDotNet.Serialization;
using VTMetaLib.afy.yaml;

namespace vtm
{
    public class AfyFile
    {
        [YamlMember(Alias = "Meta")]
        public MetaDefinition Meta { get; set; }
    }

    internal class Program
    {
        // private static readonly string DefaultMetaLoadPath = "C:\\Games\\VirindiPlugins\\VirindiTank\\AlastorAugGem.met";
        // private static readonly string DefaultMetaLoadPath = "C:\\temp\\universalremote.met";
        // private static readonly string DefaultMetaLoadPath = "C:\\temp\\AutoColoFinal.met";
        private static readonly string MimbXmlFilePath = "C:\\dev\\metas\\AlMac\\AlastorAugGem.xml";

        private static readonly string DefaultMetaLoadPath = "C:\\dev\\metas\\vt-metalib\\docs\\test.afy";

        static void Main(string[] args)
        {
            string filePath = args.Length > 1 ? args[1] : DefaultMetaLoadPath;
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Unable to find file: {filePath}");
                return;
            }

            if (filePath.EndsWith(".afy"))
            {
                Console.WriteLine($"Loading AFY file: {filePath}");
                IDeserializer deserializer = YamlSerialization.CreateDeserializer();
                string contents = File.ReadAllText(filePath).Replace("\t", "  ");
                Console.WriteLine($"Deserializing {contents.Length} characters from AFY file: {filePath}");

                AfyFile afyFile;
                try
                {
                    using (var reader = Yaml.ReaderForText(contents))
                    {
                        afyFile = deserializer.Deserialize<AfyFile>(reader);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unable to load AFY file: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
                    return;
                }
                Console.WriteLine($"Successfully parsed AFY model from file: {filePath}");

                VTMeta vtMet = afyFile.Meta.AsVTMeta();
                Console.WriteLine($"Successfully converted AFY model to VTMeta with {vtMet.Rules.Count} aggregate rules across all states");

                int lastDot = filePath.LastIndexOf('.');
                string outputFilename = filePath.Substring(0, lastDot) + "_out.met";
                MetaFileBuilder builder = new MetaFileBuilder(MetaFileType.MetaFile, outputFilename);
                vtMet.WriteTo(builder);
                builder.File.FlushToDisk();
                Console.WriteLine($"Successfully exported met file to {outputFilename}");
            }
            else if (filePath.EndsWith(".met"))
            {
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

                int lastDot = filePath.LastIndexOf('.');
                string outputFilename = filePath.Substring(0, lastDot) + "_rewritten.met";
                MetaFileBuilder builder = new MetaFileBuilder(MetaFileType.MetaFile, outputFilename);
                meta.WriteTo(builder);
                builder.File.FlushToDisk();
            }
            else if (filePath.EndsWith(".xml"))
            {
                // try MiMB XML
                VTMeta metaFromMimb = Mimb.LoadMetaFromMimbXml(MimbXmlFilePath);
                Console.WriteLine($"Successfully produced VTMeta from Mimb XML file: {MimbXmlFilePath}, total rules: {metaFromMimb.Rules.Count}");

                MetaFileBuilder builder = new MetaFileBuilder(MetaFileType.MetaFile, "C:\\temp\\testmimb.met");
                metaFromMimb.WriteTo(builder);
                builder.File.FlushToDisk();
                Console.WriteLine($"Successfully exported VTMeta to met file: {builder.File.Path}");
            }

        }
    }
}
