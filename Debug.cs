namespace SolidCode.Caerus.ECS
{
    class Debug
    {
        public static void Log(params string[] log)
        {
            Console.WriteLine(GetTimestamp() + " " + String.Join(",", log));
        }

        public static string GetTimestamp()
        {
            DateTime date = DateTime.Now;
            return "[" + date.Hour.ToString("D2") + ":" + date.Minute.ToString("D2") + ":" + date.Second.ToString("D2") + "]";
        }
    }
}