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
        private static readonly string AugGemMetPath = "C:\\Games\\VirindiPlugins\\VirindiTank\\AlastorAugGem.met";
        private static readonly string UniveralRemoteMetPath = "C:\\dev\\metas\\AlMac\\universalremote.met";
        private static readonly string UltimateIBControlMetPath = "C:\\dev\\metas\\AlMac\\UltimateIBControl_Core.met";
        private static readonly string AutoColoFinalMetPath = "C:\\temp\\AutoColoFinal.met";
        private static readonly string MimbXmlFilePath = "C:\\dev\\metas\\AlMac\\AlastorAugGem.xml";


        // private static readonly string DefaultMetaLoadPath = "C:\\dev\\metas\\vt-metalib\\docs\\test.afy";

        static void Main(string[] args)
        {
            ReadAndRewriteAndVerifyFile(AugGemMetPath);
            ReadAndRewriteAndVerifyFile(UniveralRemoteMetPath);
            ReadAndRewriteAndVerifyFile(UltimateIBControlMetPath);
            ReadAndRewriteAndVerifyFile(AutoColoFinalMetPath);

            // FIXME some issues were introduced in parsing MIMB XML
            // ReadAndRewriteAndVerifyFile(MimbXmlFilePath);
        }

        internal static bool ReadAndRewriteAndVerifyFile(string filePath, bool detailed = true)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Unable to find file: {filePath}");
                throw new FileNotFoundException($"Unable to find file at path: {filePath}");
            }

            if (filePath.EndsWith(".afy"))
            {
                if (detailed)
                    Console.WriteLine($"Loading AFY file: {filePath}");
                IDeserializer deserializer = YamlSerialization.CreateDeserializer();
                string contents = File.ReadAllText(filePath).Replace("\t", "  ");
                if (detailed)
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
                    throw;
                }
                if (detailed)
                    Console.WriteLine($"Successfully parsed AFY model from file: {filePath}");

                VTMeta vtMet = afyFile.Meta.AsVTMeta();
                if (detailed)
                    Console.WriteLine($"Successfully converted AFY model to VTMeta with {vtMet.Rules.Count} aggregate rules across all states");

                string outputFilename = GetExportFilename(filePath);
                MetaFileBuilder builder = new MetaFileBuilder(MetaFileType.MetaFile, outputFilename);
                vtMet.WriteTo(builder);
                builder.FlushToDisk(outputFilename);
                if (detailed)
                    Console.WriteLine($"Successfully exported met file to {outputFilename}");

                return true;
            }
            if (filePath.EndsWith(".xml"))
            {
                // try MiMB XML
                VTMeta metaFromMimb = Mimb.LoadMetaFromMimbXml(filePath);
                if (detailed) 
                    Console.WriteLine($"Successfully produced VTMeta from Mimb XML file: {MimbXmlFilePath}, total rules: {metaFromMimb.Rules.Count}");

                string outputFilename = GetExportFilename(filePath);
                MetaFileBuilder builder = new MetaFileBuilder(MetaFileType.MetaFile, outputFilename);
                metaFromMimb.WriteTo(builder);
                builder.FlushToDisk(outputFilename);
                if (detailed)
                    Console.WriteLine($"Successfully exported VTMeta to met file: {outputFilename}");

                return true;
            }
            else if (filePath.EndsWith(".met"))
            {
                int prevNavRoutesLoaded = NavRoutes.NavRoutesLoaded;
                VTMeta meta = MetaFiles.LoadMetaFile(filePath);
                if (detailed)
                {
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
                }

                string outputFilename = GetExportFilename(filePath);
                MetaFileBuilder builder = new MetaFileBuilder(MetaFileType.MetaFile, outputFilename);
                meta.WriteTo(builder);
                builder.FlushToDisk(outputFilename);

                if (detailed)
                    Console.WriteLine("Verifying lines...");
                List<string> lines = MetaFiles.ReadAllLines(outputFilename);
                List<string> origLines = MetaFiles.ReadAllLines(filePath);
                if (lines.Count != origLines.Count && detailed)
                    Console.WriteLine($"Originally had {origLines.Count} lines but output meta has {lines.Count} lines");
                int maxLen = Math.Max(lines.Count, origLines.Count);
                int numDiffLines = 0;
                for (int i = 0; i < maxLen; i++)
                {
                    if (i >= lines.Count)
                    {
                        numDiffLines++;
                        if (detailed)
                            Console.WriteLine($"Line #{i} does not exist in output meta: {origLines[i]}");
                    }
                    else if (i >= origLines.Count)
                    {
                        numDiffLines++;
                        if (detailed)
                            Console.WriteLine($"Line #{i} does not exist in original meta: {lines[i]}");
                    }
                    else if (origLines[i] != lines[i])
                    {
                        numDiffLines++;
                        if (detailed)
                        {
                            Console.WriteLine($"Line #{i} differed:");
                            Console.WriteLine($"\tOld: {origLines[i]}");
                            Console.WriteLine($"\tNew: {lines[i]}");
                            Console.WriteLine("\t\tOriginal\t\t\t\tNew");
                            for (int j = Math.Max(0, i - 10); j <= i; j++)
                            {
                                Console.WriteLine($"\tLine #{j}:\t{origLines[j]}\t\t\t\t{lines[j]}");
                            }
                            for (int j = i + 1; j < Math.Min(origLines.Count - 1, i + 10); j++)
                            {
                                string newVal = j < lines.Count ? lines[j] : "";
                                Console.WriteLine($"\tLine #{j}:\t{origLines[j]}\t\t\t\t{newVal}");
                            }
                        }
                        break;
                    }
                }
                if (numDiffLines > 0)
                {
                    if (detailed)
                        Console.WriteLine($"Detected one or more differences in lines between input and output met file!");
                    return false;
                }
                else
                {
                    if (detailed)
                        Console.WriteLine($"Successfully verified that all {lines.Count} lines are identical between the input and output met files");
                    return true;
                }
            }
            else
            {
                Console.WriteLine($"Unexpected file extension/name found: {filePath}");
                return false;
            }
        }

        private static string GetExportFilename(string filePath)
        {
            int lastDot = filePath.LastIndexOf('.');
            return filePath.Substring(0, lastDot) + "_rewritten.met";
        }
    }
}
