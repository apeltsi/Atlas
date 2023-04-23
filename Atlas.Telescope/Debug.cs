using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;

namespace SolidCode.Atlas.Telescope;
public class LiveData
{
    public string type { get; set; }
    public ECSElement hierarchy { get; set; }
    public string[] globalData { get; set; }

    public LiveData(string[] globalData, ECSElement hierarchy)
    {
        this.type = "livedata";
        this.hierarchy = hierarchy;
        this.globalData = globalData;
    }
}

public static class Debug
{
    public delegate void TelescopeAction();
    public static LiveData? LiveData;
    internal static Dictionary<string, TelescopeAction> actions = new Dictionary<string, TelescopeAction>();
    public static void RegisterTelescopeAction(string name, TelescopeAction action)
    {
        actions.Add(name, action);
    }
    public static void UnregisterTelescopeAction(string name)
    {
        actions.Remove(name);
    }

    private static bool ArgsIncludeString(string arg)
    {
        foreach (var argument in Environment.GetCommandLineArgs())
        {
            if (arg.ToLower() == argument.ToLower())
            {
                return true;
            }
        }

        return false;
    }
    
    public static void UseMultiProcessDebugging(string version)
    {
        var exists = System.Diagnostics.Process.GetProcessesByName(System.IO.Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetEntryAssembly().Location)).Count() > 1;
        bool disable = ArgsIncludeString("--disable-multi-process-debugging");
        if (exists || disable)
        {
            if (disable)
            {
                Console.WriteLine("Multi Process Debugging is Disabled!");
            }
            // We're the child process. So we get the exiting job of actually doing the work!
            return;
        }
        else
        {
            Console.WriteLine("Multi Process Debugging is Active");
            // We're the parent process. So we get the exiting job of spawning the child process! (and doing important debugger stuff)
            // Get the path of the current executable
            string currentExePath = Process.GetCurrentProcess().MainModule.FileName;

            // Configure and start a new process with the same path
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = currentExePath,
                Arguments = Environment.GetCommandLineArgs().ToList().Aggregate((a, b) => a + " " + b),
                UseShellExecute = false,
                CreateNoWindow = false,
                RedirectStandardOutput = true
            };
            FileLogs.InitializeFileLogs(version);
            Process childProcess = Process.Start(startInfo);
            childProcess.BeginOutputReadLine();
            childProcess.ErrorDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(args.Data);
                    Console.ResetColor();
                    FileLogs.DoLog(args.Data);
                }
            };

            childProcess.OutputDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                {
                    if (args.Data.Contains(" [ERROR] "))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                    } else if (args.Data.Contains(" [WARN] "))
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                    } else if(!args.Data.Contains(" [INFO] "))
                    {
                        if((args.Data.ToLower().Contains("error") || args.Data.ToLower().Contains("err") || args.Data.ToLower().Contains("exception")))
                            Console.ForegroundColor = ConsoleColor.Red;
                        if(args.Data.ToLower().Contains("warning") || args.Data.ToLower().Contains("warn"))
                            Console.ForegroundColor = ConsoleColor.Yellow;
                    }
                    Console.WriteLine(args.Data);
                    Console.ResetColor();
                    FileLogs.DoLog(args.Data);
                }
            };
            
            while (!childProcess.HasExited)
            {
                Thread.Sleep(1000);
            }
            FileLogs.Dispose();
            Environment.Exit(0);
        }
    }
    
    public static List<string> Categories;
    private static bool logsEnabled = false;
#if DEBUG
    private static DebugServer? ds;
#endif
    public static void StartLogs(string[] categories)
    {
        Categories = categories.ToList<string>();
        
#if DEBUG
        try
        {
            ds = new DebugServer();
        }
        catch (Exception e)
        {
            Debug.Error(0, e.Message);
            if (e.StackTrace != null)
                Debug.Error(0, e.StackTrace);

        }
#endif
        logsEnabled = true;
        
    }
    
    static void PerformLog(string prefix, string log, string category = "")
    {
        if (category == "" || !Categories.Contains(category))
        {
            category = Categories[0];
        }
        string logtext = GetTimestamp() + " " + prefix + " " + category + " > " + log;
        Console.WriteLine(logtext);
#if DEBUG
        if(ds != null)
            lock(ds)
                ds.Log(logtext);
#endif
    }

    public static void Log<T>(T category, params string[] log) where T : IComparable, IFormattable, IConvertible
    {
        int index = (int)category.ToInt32(CultureInfo.CurrentCulture);
        if (index >= Categories.Count)
        {
            index = 0;
            Error(0, "(Telescope) Invalid category: " + category);
        }
        Console.ForegroundColor = ConsoleColor.White;
        PerformLog("[INFO]", String.Join(" ", log), Categories[index]);
        Console.ResetColor();
    }
    public static void Warning<T>(T category, params string[] log) where T : IComparable, IFormattable, IConvertible
    {
        int index = (int)category.ToInt32(CultureInfo.CurrentCulture);
        if (index >= Categories.Count)
        {
            index = 0;
            Error(0, "(Telescope) Invalid category: " + category);
        }
        Console.ForegroundColor = ConsoleColor.Yellow;
        PerformLog("[WARN]", String.Join(" ", log), Categories[index]);
        Console.ResetColor();
    }

    public static void Error<T>(T category, params string[] log) where T : IComparable, IFormattable, IConvertible
    {
        int index = (int)category.ToInt32(CultureInfo.CurrentCulture);
        if (index >= Categories.Count)
        {
            index = 0;
            Error(0, "(Telescope) Invalid category: " + category);
        }
        Console.ForegroundColor = ConsoleColor.Red;
        PerformLog("[ERROR]", String.Join(" ", log), Categories[index]);
        Console.ResetColor();
    }

    public static string GetTimestamp()
    {
        DateTime date = DateTime.Now;
        return "[" + date.Hour.ToString("D2") + ":" + date.Minute.ToString("D2") + ":" + date.Second.ToString("D2") + "]";
    }

    public static void Dispose()
    {
        logsEnabled = false;
        

        actions = new Dictionary<string, TelescopeAction>();
#if DEBUG
        ds.Stop();
#endif
    }

    #region Gets the build date and time (by reading the COFF header)

    // http://msdn.microsoft.com/en-us/library/ms680313

    struct _IMAGE_FILE_HEADER
    {
        public ushort Machine;
        public ushort NumberOfSections;
        public uint TimeDateStamp;
        public uint PointerToSymbolTable;
        public uint NumberOfSymbols;
        public ushort SizeOfOptionalHeader;
        public ushort Characteristics;
    };

    static DateTime GetBuildDateTime(Assembly assembly)
    {
        var path = assembly.GetName().CodeBase;
        if (File.Exists(path))
        {
            var buffer = new byte[Math.Max(Marshal.SizeOf(typeof(_IMAGE_FILE_HEADER)), 4)];
            using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                fileStream.Position = 0x3C;
                fileStream.Read(buffer, 0, 4);
                fileStream.Position = BitConverter.ToUInt32(buffer, 0); // COFF header offset
                fileStream.Read(buffer, 0, 4); // "PE\0\0"
                fileStream.Read(buffer, 0, buffer.Length);
            }
            var pinnedBuffer = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                var coffHeader = (_IMAGE_FILE_HEADER)Marshal.PtrToStructure(pinnedBuffer.AddrOfPinnedObject(), typeof(_IMAGE_FILE_HEADER));

                return TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1) + new TimeSpan(coffHeader.TimeDateStamp * TimeSpan.TicksPerSecond));
            }
            finally
            {
                pinnedBuffer.Free();
            }
        }
        return new DateTime();
    }

    #endregion
}
