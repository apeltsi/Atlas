namespace SolidCode.Caerus.ECS
{
    class Debug
    {

        public static void StartLogs()
        {
            if (!Directory.Exists("Logs"))
            {
                Directory.CreateDirectory("logs");
            }

            using (StreamWriter writer = new StreamWriter("logs/All.log"))
            {
                writer.WriteLine("/////////////////////////////////////");
                writer.WriteLine("// Caerus started up at " + GetTimestamp() + " //");
                writer.WriteLine("/////////////////////////////////////");

            }
        }

        static void LogToFileAndConsole(string log)
        {
            Console.WriteLine(log);
            if (!Directory.Exists("Logs"))
            {
                Directory.CreateDirectory("logs");
            }
            using (StreamWriter writer = new StreamWriter("logs/All.log", true))
            {
                writer.WriteLine(log);
            }
        }

        public static void Log(params string[] log)
        {
            Console.ForegroundColor = ConsoleColor.White;
            LogToFileAndConsole(GetTimestamp() + " [INFO] " + String.Join(",", log));
            Console.ResetColor();
        }

        public static void Warning(params string[] log)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            LogToFileAndConsole(GetTimestamp() + " [WARN] " + String.Join(",", log));
            Console.ResetColor();
        }

        public static void Error(params string[] log)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            LogToFileAndConsole(GetTimestamp() + " [ERROR] " + String.Join(",", log));
            Console.ResetColor();
        }

        public static string GetTimestamp()
        {
            DateTime date = DateTime.Now;
            return "[" + date.Hour.ToString("D2") + ":" + date.Minute.ToString("D2") + ":" + date.Second.ToString("D2") + "]";
        }
    }
}