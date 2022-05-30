using System.Globalization;

namespace SolidCode.Caerus
{
    class Debug
    {
        public static List<string> Categories;
        public static void StartLogs(params string[] categories)
        {
            Categories = categories.ToList<string>();
            if (!Directory.Exists("Logs"))
            {
                Directory.CreateDirectory("logs");
            }

            using (StreamWriter writer = new StreamWriter("logs/All.log"))
            {
                writer.WriteLine("All logs are listed below:");
                writer.WriteLine("/////////////////////////////////////");
                writer.WriteLine("// Caerus started up at " + GetTimestamp() + " //");
                writer.WriteLine("/////////////////////////////////////");

            }
            foreach (string category in Categories)
            {
                using (StreamWriter writer = new StreamWriter("logs/" + category + ".log"))
                {
                    writer.WriteLine("Only " + category + " logs are listed below:");
                    writer.WriteLine("/////////////////////////////////////");
                    writer.WriteLine("// Caerus started up at " + GetTimestamp() + " //");
                    writer.WriteLine("/////////////////////////////////////");

                }
            }
        }

        static void LogToFileAndConsole(string prefix, string log, string category = "")
        {
            if (category == "" || !Categories.Contains(category))
            {
                category = Categories[0];
            }
            Console.WriteLine(GetTimestamp() + " " + prefix + " " + category + " > " + log);
            if (!Directory.Exists("Logs"))
            {
                Directory.CreateDirectory("logs");
            }
            using (StreamWriter writer = new StreamWriter("logs/All.log", true))
            {
                writer.WriteLine(GetTimestamp() + " " + prefix + " " + category + " > " + log);
            }
            using (StreamWriter writer = new StreamWriter("logs/" + category + ".log", true))
            {
                writer.WriteLine(GetTimestamp() + " " + prefix + " > " + log);
            }
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
    }
}