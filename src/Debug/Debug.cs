using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace SolidCode.Caerus
{
    public static class Debug
    {
        public static List<string> Categories;
        private static bool logLocked = false;
        private static Queue<string> logItems = new Queue<string>();
        private static bool logsEnabled = false;
#if DEBUG
        private static DebugServer? ds;
#endif
        public static void StartLogs(params string[] categories)
        {
            Categories = categories.ToList<string>();
            if (!Directory.Exists("Logs"))
            {
                Directory.CreateDirectory("logs");
            }

            using (StreamWriter writer = new StreamWriter("logs/All.log"))
            {
                writer.WriteLine("ENGINE_VERSION: " + Caerus.Version);
                writer.WriteLine("RUN_DATE: " + DateTime.Now.ToString());
            }

            Assembly assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(assembly.GetManifestResourceNames().Single(str => str.EndsWith("LogViewer.html"))))
            using (StreamReader reader = new StreamReader(stream))
            {
                string result = reader.ReadToEnd();
                using (StreamWriter writer = new StreamWriter("logs/logs.html"))
                {
                    writer.Write(result);
                    writer.WriteLine("ENGINE_VERSION: " + Caerus.Version);
                    writer.WriteLine("RUN_DATE: " + DateTime.Now.ToString());
                }
            }
#if DEBUG
            try
            {
                ds = new DebugServer();
            }
            catch (Exception e)
            {
                Debug.Error(e.Message);
                if (e.StackTrace != null)
                    Debug.Error(e.StackTrace);

            }
#endif
            logsEnabled = true;
            Thread t = new Thread(new ThreadStart(UpdateLogs));
            t.Start();
        }

        public static void UpdateLogs()
        {
            Console.WriteLine("Log started");
            while (logsEnabled)
            {
                if (logItems.Count > 0)
                {
                    WriteLogBuffer();
                }
                Thread.Sleep(10);
            }
        }

        static void LogToFileAndConsole(string prefix, string log, string category = "")
        {
            if (category == "" || !Categories.Contains(category))
            {
                category = Categories[0];
            }
            string logtext = GetTimestamp() + " " + prefix + " " + category + " > " + log;
            while (logLocked)
            {
                Thread.Sleep(20);
            }
            logItems.Enqueue(logtext);
        }

        static void WriteLogBuffer()
        {
            logLocked = true;
            Queue<string> queue = new Queue<string>(logItems);
            logItems.Clear();
            logLocked = false;
            foreach (string logtext in queue)
            {
                Console.WriteLine(logtext);
                if (!Directory.Exists("Logs"))
                {
                    Directory.CreateDirectory("logs");
                }
                try
                {
                    using (StreamWriter writer = new StreamWriter("logs/All.log", true))
                    {
                        writer.WriteLine(logtext);
                    }
                }
                catch (Exception e)
                {
                }
                try
                {
                    using (StreamWriter writer = new StreamWriter("logs/logs.html", true))
                    {
                        writer.WriteLine(logtext);
                    }
                }
                catch (Exception e)
                {
                }
#if DEBUG
                if (ds != null)
                    ds.Log(logtext);
#endif
            }
        }

        public static void Stop()
        {
            logsEnabled = false;
            WriteLogBuffer();
            ds.Stop();
        }

        public static void Log<T>(T category, params string[] log) where T : IComparable, IFormattable, IConvertible
        {
            int index = (int)category.ToInt32(CultureInfo.CurrentCulture);
            if (index >= Categories.Count)
            {
                index = 0;
                Error("Invalid category: " + category);
            }
            Console.ForegroundColor = ConsoleColor.White;
            LogToFileAndConsole("[INFO]", String.Join(",", log), Categories[index]);
            Console.ResetColor();
        }
        public static void Log(string log)
        {
            Console.ForegroundColor = ConsoleColor.White;
            LogToFileAndConsole("[INFO]", log);
            Console.ResetColor();
        }


        public static void Warning<T>(T category, params string[] log) where T : IComparable, IFormattable, IConvertible
        {
            int index = (int)category.ToInt32(CultureInfo.CurrentCulture);
            if (index >= Categories.Count)
            {
                index = 0;
                Error("Invalid category: " + category);
            }
            Console.ForegroundColor = ConsoleColor.Yellow;
            LogToFileAndConsole("[WARN]", String.Join(",", log), Categories[index]);
            Console.ResetColor();
        }

        public static void Warning(string log)
        {

            Console.ForegroundColor = ConsoleColor.Yellow;
            LogToFileAndConsole("[WARN]", log);
            Console.ResetColor();
        }


        public static void Error<T>(T category, params string[] log) where T : IComparable, IFormattable, IConvertible
        {
            int index = (int)category.ToInt32(CultureInfo.CurrentCulture);
            if (index >= Categories.Count)
            {
                index = 0;
                Error("Invalid category: " + category);
            }
            Console.ForegroundColor = ConsoleColor.Red;
            LogToFileAndConsole("[ERROR]", String.Join(",", log), Categories[index]);
            Console.ResetColor();
        }
        public static void Error(string log)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            LogToFileAndConsole("[ERROR]", log);
            Console.ResetColor();
        }


        public static string GetTimestamp()
        {
            DateTime date = DateTime.Now;
            return "[" + date.Hour.ToString("D2") + ":" + date.Minute.ToString("D2") + ":" + date.Second.ToString("D2") + "]";
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
}