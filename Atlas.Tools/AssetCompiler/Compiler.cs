using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Compression;
using System.Text.RegularExpressions;
using Atlas.Tools;
using AtlasTools;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using static ICSharpCode.SharpZipLib.Zip.Compression.Deflater;

namespace Atlas.Tools.AssetCompiler
{
    public static class ArrayExtensions
    {
        // Not the fastest way of doing this but it gets the job done
        public static bool ContainsCaseInsensitive(this string[] source, string toCheck)
        {
            for (int i = 0; i < source.Length; i++)
            {
                if (source[i].ToLower() == toCheck.ToLower())
                {
                    return true;
                }
            }
            return false;
        }
    }

    public enum Progress
    {
        Queued,
        CollectingFiles,
        Compiling,
        Building,
        Finalizing,
        ReadQueue,
        ReadTest,
        Failed,
        Completed
    }
    public static class Compiler
    {
        static Dictionary<string, string> assetPacks = new Dictionary<string, string>();
        static Dictionary<string, Progress> assetPackProgress = new Dictionary<string, Progress>();
        static Dictionary<string, long> assetPackSpeeds = new Dictionary<string, long>();
        static Dictionary<string, long> assetPackSizes = new Dictionary<string, long>();
        internal static ConcurrentDictionary<string, string> assetMap = new ConcurrentDictionary<string, string>();
        public static List<string> errors = new List<string>();
        static int currentlyCompiling = 0;
        static bool ongoingReadTest = false;
        public static Regex assetpackexp = new Regex(@"(.*?)\.assetpack\.json$");
        static bool doReadTest = false;
        static bool renderDirty = true;
        static bool isDone = false;
        static Queue<string> readTestQueue = new Queue<string>();
        const string extension = ".assetpack";
        public static void Compile(string[] args)
        {
            Stopwatch s = new Stopwatch();
            s.Start();
            Program.ColoredText("Atlas Asset Compiler", ConsoleColor.Green);
            Program.ColoredText("--------------------", ConsoleColor.DarkGreen);
            string curDir = Directory.GetCurrentDirectory();
            string dataDir = Path.Join(curDir, "data");
            doReadTest = args.ContainsCaseInsensitive("--readtest") || args.ContainsCaseInsensitive("-r");
            if (!Directory.Exists(Path.Join(Directory.GetCurrentDirectory(), "assets")))
            {
                Directory.CreateDirectory(Path.Join(Directory.GetCurrentDirectory(), "assets"));
            }

            if (Directory.Exists(dataDir))
            {
                foreach (string file in Directory.EnumerateFiles(dataDir, "*.*", SearchOption.AllDirectories))
                {
                    Match match = assetpackexp.Match(Path.GetFileName(file));
                    if (match.Length > 0)
                    {
                        string packName = match.Groups[1].Value;

                        assetPacks.Add(packName, file);
                    }
                }
                if (assetPacks.Count == 0)
                {
                    Program.ColoredText("Error: No assetpacks to compile", ConsoleColor.Red);
                    return;
                }
                Program.ColoredText("--------------------", ConsoleColor.DarkGray);

                foreach (KeyValuePair<string, string> item in assetPacks)
                {
                    assetPackProgress.Add(item.Key, Progress.Queued);
                    Thread t = new Thread(() => CompileAssetPack(item.Key, item.Value));
                    t.Start();
                }

                while (!isDone)
                {
                    if (currentlyCompiling == 0 && doReadTest && ongoingReadTest == false && readTestQueue.Count > 0)
                    {
                        // Lets perform the read test now that all processes have completed
                        ongoingReadTest = true;
                        Thread t = new Thread(() => DoReadTest(readTestQueue.Dequeue()));
                        t.Start();
                    }
                    if (renderDirty)
                    {
                        renderDirty = false;
                        RenderStatus();
                    }

                    Thread.Sleep(100);
                }

                if (!args.ContainsCaseInsensitive("--no-map") )
                {
                    Program.ColoredText("Writing Asset Map...", ConsoleColor.Gray);
                    string map = JsonConvert.SerializeObject(assetMap);
                    File.WriteAllText(Path.Join(curDir, "assets/.assetmap"), map);
                }
                else
                {
                    Program.ColoredText("AssetMap will not be generated.", ConsoleColor.Gray);
                }
                RecursiveDelete(new DirectoryInfo(Path.Combine(Path.GetTempPath(), "atlastools-temp")));

                s.Stop();
                if (errors.Count == 0)
                {
                    Console.WriteLine("All assets compiled successfully (" + Math.Round(s.ElapsedMilliseconds / 100f) / 10f + "s)");
                }
                else
                {
                    Program.ColoredText("All assets compiled with errors (" + Math.Round(s.ElapsedMilliseconds / 100f) / 10f + "s)", ConsoleColor.Yellow);
                }
            }
            else
            {
                Program.ColoredText("Error: Couldn't find data directory", ConsoleColor.Red);
            }
        }

        static bool firstRun = true;
        static int prevErrors = 0;
        static void RenderStatus()
        {
            if (firstRun)
            {
                firstRun = false;
            }
            else
            {
                Console.CursorTop -= (assetPacks.Count + 2 + prevErrors);
            }
            int processing = 0;
            Console.WriteLine("Compiling Assetpacks:                   ");
            foreach (KeyValuePair<string, string> item in assetPacks)
            {
                (string text, ConsoleColor color) = GetProgressText(assetPackProgress[item.Key]);
                if (assetPackSpeeds.ContainsKey(item.Key) && assetPackProgress[item.Key] == Progress.Completed)
                {
                    Program.ColoredText("- AssetPack " + item.Key + ": " + text + " (" + SimpleTimeFormat(assetPackSpeeds[item.Key]) + ") (" + FormatSize(assetPackSizes[item.Key]) + ")                    ", color);
                }
                else if (assetPackSizes.ContainsKey(item.Key))
                {
                    Program.ColoredText("- AssetPack " + item.Key + ": " + text + " (" + FormatSize(assetPackSizes[item.Key]) + ")                    ", color);
                }
                else
                {
                    Program.ColoredText("- AssetPack " + item.Key + ": " + text + "                        ", color);
                }
                if (assetPackProgress[item.Key] != Progress.Completed)
                {
                    processing++;
                }

            }

            for (int i = 0; i < errors.Count; i++)
            {
                Program.ColoredText("Error: " + errors[i], ConsoleColor.Red);
            }
            prevErrors = errors.Count;
            Program.ColoredText("--------------------", ConsoleColor.DarkGray);
            if (processing == 0)
            {
                isDone = true;
            }

        }

        static string SimpleTimeFormat(long ms)
        {
            if (ms < 0)
            {
                return "Read Failed";
            }
            if (ms > 1000)
            {
                return (float)ms / 1000 + "s";
            }
            return ms + "ms";
        }

        static string FormatSize(long bytes)
        {
            if (bytes < 1024)
            {
                return bytes + "B";
            }
            else if (bytes < 1024 * 1024)
            {
                return (Math.Round(bytes / 102.4) / 10) + "KiB";
            }
            else if (bytes < 1024 * 1024 * 1024)
            {
                return (Math.Round(bytes / (102.4 * 1024)) / 10) + "MiB";
            }
            else
            {
                return (Math.Round(bytes / (102.4 * 1024 * 1024)) / 10) + "GiB";
            }
        }

        static (string, ConsoleColor) GetProgressText(Progress progress)
        {
            switch (progress)
            {
                case Progress.Queued:
                    return ("Queued", ConsoleColor.DarkYellow);
                case Progress.CollectingFiles:
                    return ("Collecting Files...", ConsoleColor.Magenta);
                case Progress.Building:
                    return ("Building...", ConsoleColor.Yellow);
                case Progress.Compiling:
                    return ("Compiling...", ConsoleColor.Blue);
                case Progress.Finalizing:
                    return ("Finalizing...", ConsoleColor.Cyan);
                case Progress.Completed:
                    return ("Completed", ConsoleColor.Green);
                case Progress.ReadQueue:
                    return ("Queued for read test", ConsoleColor.DarkYellow);
                case Progress.ReadTest:
                    return ("Testing read speeds...", ConsoleColor.Blue);
                case Progress.Failed:
                    return ("Failed", ConsoleColor.Red);
                default:
                    return ("Idle", ConsoleColor.DarkYellow);
            }
        }

        static void CompileAssetPack(string pack, string assetPack)
        {
            while (currentlyCompiling > 10)
            {
                Thread.Sleep(100);
            }
            Interlocked.Increment(ref currentlyCompiling);
            lock (assetPackProgress)
                assetPackProgress[pack] = Progress.CollectingFiles;
            renderDirty = true;

            AssetPack? aPack = JsonConvert.DeserializeObject<AssetPack>(File.ReadAllText(assetPack));

            if (aPack == null)
            {
                return;
            }
            string[] paths = aPack.GetAllPaths(assetPack);
            string assetPackPath = Path.GetDirectoryName(assetPack);
            renderDirty = true;
            DirectoryInfo dir = Directory.CreateDirectory(GetTemporaryDirectory());
            if (!dir.Exists)
            {
                return;
            }

            List<string> buildableFiles = new List<string>();
            foreach (string file in paths)
            {
                string filepath = Path.GetDirectoryName(file);
                
                string dirPath = Path.Join(dir.FullName, filepath);
                if (!Directory.Exists(dirPath))
                {
                    Directory.CreateDirectory(dirPath);
                }
                buildableFiles.Add(file);
                File.Copy(Path.Join(assetPackPath, file), Path.Join(dirPath, Path.GetFileName(file)));
            }
            lock (assetPackProgress)
                assetPackProgress[pack] = Progress.Building;
            AssetBuilder.BuildFiles(dir.FullName, buildableFiles.ToArray(), pack);
            lock (assetPackProgress)
                assetPackProgress[pack] = Progress.Compiling;

            FastZip fastZip = new FastZip();
            fastZip.CompressionLevel = GetCompressionLevel(aPack.optimizeFor);
            bool recurse = true;  // Include all files by recursing through the directory structure
            string filter = null; // Dont filter any files at all
            if (!Directory.Exists(Path.Join(Directory.GetCurrentDirectory(), "assets")))
            {
                // Lets clean up return before anything goes wrong
                RecursiveDelete(dir);
                lock (assetPackProgress)
                    assetPackProgress[pack] = Progress.Failed;
                renderDirty = true;
                Interlocked.Decrement(ref currentlyCompiling);
                return;
            }
            string path = Path.Join(Directory.GetCurrentDirectory(), "assets/" + pack + extension);
            fastZip.CreateZip(path, dir.FullName, recurse, filter);
            lock (assetPackProgress)
                assetPackProgress[pack] = Progress.Finalizing;
            renderDirty = true;
            if (File.Exists(path))
            {
                lock (assetPackSizes)
                    assetPackSizes.Add(pack, new System.IO.FileInfo(path).Length);
            }
            else
            {
                lock (assetPackSizes)
                    assetPackSizes.Add(pack, 0);
            }
            // Delete temp folder
            if (doReadTest)
            {
                lock (assetPackProgress)
                    assetPackProgress[pack] = Progress.ReadQueue;
                readTestQueue.Enqueue(pack);
            }
            else
            {
                lock (assetPackProgress)
                    assetPackProgress[pack] = Progress.Completed;
            }
            renderDirty = true;
            Interlocked.Decrement(ref currentlyCompiling);
        }

        static void DoReadTest(string pack)
        {
            if (!assetPacks.ContainsKey(pack))
                return;
            assetPackProgress[pack] = Progress.ReadTest;
            string path = Path.Join(Directory.GetCurrentDirectory(), "assets/" + pack + extension);
            if (File.Exists(path))
            {
                Stopwatch s = new Stopwatch();
                s.Start();
                int files = 0;
                string data = "";
                using (FileStream fs = new FileStream(path, FileMode.Open))
                using (ZipArchive zip = new ZipArchive(fs))
                {
                    var entry = zip.Entries.First();

                    using (StreamReader sr = new StreamReader(entry.Open()))
                    {
                        // Lets just act like we're doing something with this data
                        files++;
                        data += sr.ReadToEnd();
                    }
                }
                s.Stop();
                assetPackSpeeds[pack] = s.ElapsedMilliseconds;
                assetPackProgress[pack] = Progress.Completed;
                ongoingReadTest = false;
                renderDirty = true;
            }
            else
            {
                errors.Add("Couldn't complete read test. File not found.");
                assetPackSpeeds[pack] = -1;
                assetPackProgress[pack] = Progress.Completed;
                ongoingReadTest = false;
                renderDirty = true;
            }
        }

        static ICSharpCode.SharpZipLib.Zip.Compression.Deflater.CompressionLevel GetCompressionLevel(string optimizeFor)
        {
            switch (optimizeFor.ToLower())
            {
                case "speed":
                    return ICSharpCode.SharpZipLib.Zip.Compression.Deflater.CompressionLevel.NO_COMPRESSION;
                case "balanced":
                    return ICSharpCode.SharpZipLib.Zip.Compression.Deflater.CompressionLevel.BEST_SPEED;
                case "size":
                    return ICSharpCode.SharpZipLib.Zip.Compression.Deflater.CompressionLevel.BEST_COMPRESSION;
                case "standard":
                    return ICSharpCode.SharpZipLib.Zip.Compression.Deflater.CompressionLevel.DEFAULT_COMPRESSION;
                default:
                    errors.Add("Couldn't parse optimizeFor '" + optimizeFor + "'. Using standard compression.");
                    return ICSharpCode.SharpZipLib.Zip.Compression.Deflater.CompressionLevel.DEFAULT_COMPRESSION;
            }
        }

        static void RecursiveDelete(DirectoryInfo baseDir)
        {
            if (!baseDir.Exists)
                return;

            foreach (var dir in baseDir.EnumerateDirectories())
            {
                RecursiveDelete(dir);
            }
            baseDir.Delete(true);
        }
        static string GetTemporaryDirectory()
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), "atlastools-temp", Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }
    }
}