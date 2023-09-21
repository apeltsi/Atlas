using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Compression;
using System.Text.RegularExpressions;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Zip.Compression;
using Newtonsoft.Json;

namespace Atlas.Tools.AssetCompiler;

public static class ArrayExtensions
{
    // Not the fastest way of doing this but it gets the job done
    public static bool ContainsCaseInsensitive(this string[] source, string toCheck)
    {
        for (var i = 0; i < source.Length; i++)
            if (source[i].ToLower() == toCheck.ToLower())
                return true;

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
    private const string Extension = ".assetpack";
    private static readonly Dictionary<string, string> _assetPacks = new();
    private static readonly Dictionary<string, Progress> _assetPackProgress = new();
    private static readonly Dictionary<string, long> _assetPackSpeeds = new();
    private static readonly Dictionary<string, long> _assetPackSizes = new();
    internal static ConcurrentDictionary<string, string> AssetMap = new();
    public static List<string> Errors = new();
    private static int _currentlyCompiling;
    private static bool _ongoingReadTest;
    public static Regex Assetpackexp = new(@"(.*?)\.assetpack\.json$");
    private static bool _doReadTest;
    private static bool _renderDirty = true;
    private static bool _isDone;
    private static readonly Queue<string> _readTestQueue = new();

    private static bool _firstRun = true;
    private static int _prevErrors;

    public static void Compile(string[] args)
    {
        var s = new Stopwatch();
        s.Start();
        Program.ColoredText("Atlas Asset Compiler", ConsoleColor.Green);
        Program.ColoredText("--------------------", ConsoleColor.DarkGreen);
        var curDir = Directory.GetCurrentDirectory();
        var assetsDir = Path.Join(curDir, "assets");
        _doReadTest = args.ContainsCaseInsensitive("--readtest") || args.ContainsCaseInsensitive("-r");
        if (!Directory.Exists(Path.Join(Directory.GetCurrentDirectory(), "assetpacks")))
            Directory.CreateDirectory(Path.Join(Directory.GetCurrentDirectory(), "assetpacks"));

        if (Directory.Exists(assetsDir))
        {
            foreach (var file in Directory.EnumerateFiles(assetsDir, "*.*", SearchOption.AllDirectories))
            {
                var match = Assetpackexp.Match(Path.GetFileName(file));
                if (match.Length > 0)
                {
                    var packName = match.Groups[1].Value;

                    _assetPacks.Add(packName, file);
                }
            }

            if (_assetPacks.Count == 0)
            {
                Program.ColoredText("Error: No assetpacks to compile", ConsoleColor.Red);
                return;
            }

            Program.ColoredText("--------------------", ConsoleColor.DarkGray);

            foreach (var item in _assetPacks)
            {
                _assetPackProgress.Add(item.Key, Progress.Queued);
                var t = new Thread(() => CompileAssetPack(item.Key, item.Value));
                t.Start();
            }

            while (!_isDone)
            {
                if (_currentlyCompiling == 0 && _doReadTest && _ongoingReadTest == false &&
                    _readTestQueue.Count > 0)
                {
                    // Lets perform the read test now that all processes have completed
                    _ongoingReadTest = true;
                    var t = new Thread(() => DoReadTest(_readTestQueue.Dequeue()));
                    t.Start();
                }

                if (_renderDirty)
                {
                    _renderDirty = false;
                    RenderStatus();
                }

                Thread.Sleep(100);
            }

            if (!args.ContainsCaseInsensitive("--no-map"))
            {
                Program.ColoredText("Writing Asset Map...", ConsoleColor.Gray);
                var map = JsonConvert.SerializeObject(AssetMap);
                File.WriteAllText(Path.Join(curDir, "assetpacks/.assetmap"), map);
            }
            else
            {
                Program.ColoredText("AssetMap will not be generated.", ConsoleColor.Gray);
            }

            RecursiveDelete(new DirectoryInfo(Path.Combine(Path.GetTempPath(), "atlastools-temp")));

            s.Stop();
            if (Errors.Count == 0)
                Console.WriteLine("All assets compiled successfully (" +
                                  Math.Round(s.ElapsedMilliseconds / 100f) / 10f + "s)");
            else
                Program.ColoredText(
                    "All assets compiled with errors (" + Math.Round(s.ElapsedMilliseconds / 100f) / 10f + "s)",
                    ConsoleColor.Yellow);
        }
        else
        {
            Program.ColoredText("Error: Couldn't find assets directory", ConsoleColor.Red);
        }
    }

    private static void RenderStatus()
    {
        if (_firstRun)
            _firstRun = false;
        else
            Console.CursorTop -= _assetPacks.Count + 2 + _prevErrors;

        var processing = 0;
        Console.WriteLine("Compiling Assetpacks:                   ");
        foreach (var item in _assetPacks)
        {
            var (text, color) = GetProgressText(_assetPackProgress[item.Key]);
            lock (_assetPackSizes)
            {
                if (_assetPackSpeeds.ContainsKey(item.Key) && _assetPackProgress[item.Key] == Progress.Completed)
                    Program.ColoredText(
                        "- AssetPack " + item.Key + ": " + text + " (" +
                        SimpleTimeFormat(_assetPackSpeeds[item.Key]) + ") (" +
                        FormatSize(_assetPackSizes[item.Key]) + ")                    ", color);
                else if (_assetPackSizes.ContainsKey(item.Key))
                    Program.ColoredText(
                        "- AssetPack " + item.Key + ": " + text + " (" + FormatSize(_assetPackSizes[item.Key]) +
                        ")                    ", color);
                else
                    Program.ColoredText("- AssetPack " + item.Key + ": " + text + "                        ",
                        color);
            }

            if (_assetPackProgress[item.Key] != Progress.Completed) processing++;
        }

        for (var i = 0; i < Errors.Count; i++) Program.ColoredText("Error: " + Errors[i], ConsoleColor.Red);

        _prevErrors = Errors.Count;
        Program.ColoredText("--------------------", ConsoleColor.DarkGray);
        if (processing == 0) _isDone = true;
    }

    private static string SimpleTimeFormat(long ms)
    {
        if (ms < 0) return "Read Failed";

        if (ms > 1000) return (float)ms / 1000 + "s";

        return ms + "ms";
    }

    private static string FormatSize(long bytes)
    {
        if (bytes < 1024)
            return bytes + "B";
        if (bytes < 1024 * 1024)
            return Math.Round(bytes / 102.4) / 10 + "KiB";
        if (bytes < 1024 * 1024 * 1024)
            return Math.Round(bytes / (102.4 * 1024)) / 10 + "MiB";
        return Math.Round(bytes / (102.4 * 1024 * 1024)) / 10 + "GiB";
    }

    private static (string, ConsoleColor) GetProgressText(Progress progress)
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

    private static void CompileAssetPack(string pack, string assetPack)
    {
        while (_currentlyCompiling > 10) Thread.Sleep(100);

        Interlocked.Increment(ref _currentlyCompiling);
        lock (_assetPackProgress)
        {
            _assetPackProgress[pack] = Progress.CollectingFiles;
        }

        _renderDirty = true;

        var aPack = JsonConvert.DeserializeObject<AssetPack>(File.ReadAllText(assetPack));

        if (aPack == null) return;

        var paths = aPack.GetAllPaths(assetPack);
        var assetPackPath = Path.GetDirectoryName(assetPack)!;
        _renderDirty = true;
        var dir = Directory.CreateDirectory(GetTemporaryDirectory());
        if (!dir.Exists) return;

        var buildableFiles = new List<string>();
        foreach (var file in paths)
        {
            var filepath = Path.GetDirectoryName(file)!;

            var dirPath = Path.Join(dir.FullName, filepath);
            if (!Directory.Exists(dirPath)) Directory.CreateDirectory(dirPath);

            buildableFiles.Add(file);
            File.Copy(Path.Join(assetPackPath, file), Path.Join(dirPath, Path.GetFileName(file)));
        }

        lock (_assetPackProgress)
        {
            _assetPackProgress[pack] = Progress.Building;
        }

        AssetBuilder.BuildFiles(dir.FullName, buildableFiles.ToArray(), pack);
        lock (_assetPackProgress)
        {
            _assetPackProgress[pack] = Progress.Compiling;
        }

        var fastZip = new FastZip();
        fastZip.CompressionLevel = GetCompressionLevel(aPack.optimizeFor);
        var recurse = true; // Include all files by recursing through the directory structure
        string? filter = null; // Dont filter any files at all
        if (!Directory.Exists(Path.Join(Directory.GetCurrentDirectory(), "assetpacks")))
        {
            // Lets clean up return before anything goes wrong
            RecursiveDelete(dir);
            lock (_assetPackProgress)
            {
                _assetPackProgress[pack] = Progress.Failed;
            }

            _renderDirty = true;
            Interlocked.Decrement(ref _currentlyCompiling);
            return;
        }

        var path = Path.Join(Directory.GetCurrentDirectory(), "assetpacks/" + pack + Extension);
        fastZip.CreateZip(path, dir.FullName, recurse, filter);
        lock (_assetPackProgress)
        {
            _assetPackProgress[pack] = Progress.Finalizing;
        }

        _renderDirty = true;
        if (File.Exists(path))
            lock (_assetPackSizes)
            {
                _assetPackSizes.Add(pack, new FileInfo(path).Length);
            }
        else
            lock (_assetPackSizes)
            {
                _assetPackSizes.Add(pack, 0);
            }

        // Delete temp folder
        if (_doReadTest)
        {
            lock (_assetPackProgress)
            {
                _assetPackProgress[pack] = Progress.ReadQueue;
            }

            _readTestQueue.Enqueue(pack);
        }
        else
        {
            lock (_assetPackProgress)
            {
                _assetPackProgress[pack] = Progress.Completed;
            }
        }

        _renderDirty = true;
        Interlocked.Decrement(ref _currentlyCompiling);
    }

    private static void DoReadTest(string pack)
    {
        if (!_assetPacks.ContainsKey(pack))
            return;
        _assetPackProgress[pack] = Progress.ReadTest;
        var path = Path.Join(Directory.GetCurrentDirectory(), "assetpacks/" + pack + Extension);
        if (File.Exists(path))
        {
            var s = new Stopwatch();
            s.Start();
            using (var fs = new FileStream(path, FileMode.Open))
            using (var zip = new ZipArchive(fs))
            {
                var entry = zip.Entries.First();

                using (var sr = new StreamReader(entry.Open()))
                {
                    // Lets just act like we're doing something with this data
                }
            }

            s.Stop();
            _assetPackSpeeds[pack] = s.ElapsedMilliseconds;
            _assetPackProgress[pack] = Progress.Completed;
            _ongoingReadTest = false;
            _renderDirty = true;
        }
        else
        {
            Errors.Add("Couldn't complete read test. File not found.");
            _assetPackSpeeds[pack] = -1;
            _assetPackProgress[pack] = Progress.Completed;
            _ongoingReadTest = false;
            _renderDirty = true;
        }
    }

    private static Deflater.CompressionLevel GetCompressionLevel(string optimizeFor)
    {
        switch (optimizeFor.ToLower())
        {
            case "speed":
                return Deflater.CompressionLevel.NO_COMPRESSION;
            case "balanced":
                return Deflater.CompressionLevel.BEST_SPEED;
            case "size":
                return Deflater.CompressionLevel.BEST_COMPRESSION;
            case "standard":
                return Deflater.CompressionLevel.DEFAULT_COMPRESSION;
            default:
                Errors.Add("Couldn't parse optimizeFor '" + optimizeFor + "'. Using standard compression.");
                return Deflater.CompressionLevel.DEFAULT_COMPRESSION;
        }
    }

    private static void RecursiveDelete(DirectoryInfo baseDir)
    {
        if (!baseDir.Exists)
            return;

        foreach (var dir in baseDir.EnumerateDirectories()) RecursiveDelete(dir);

        baseDir.Delete(true);
    }

    private static string GetTemporaryDirectory()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "atlastools-temp", Path.GetRandomFileName());
        Directory.CreateDirectory(tempDirectory);
        return tempDirectory;
    }
}