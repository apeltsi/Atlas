namespace SolidCode.Atlas
{
    public static class Debug
    {
        public static void Log(params string[] log)
        {
            Log(LogCategory.General, log);
        }
        public static void Log<T>(T category, params string[] log) where T : IComparable, IFormattable, IConvertible
        {
            Telescope.Debug.Log<T>(category, log);
        }
        public static void Warning(params string[] log)
        {
            Warning(LogCategory.General, log);
        }
        public static void Warning<T>(T category, params string[] log) where T : IComparable, IFormattable, IConvertible
        {
            Telescope.Debug.Warning<T>(category, log);
        }
        public static void Error(params string[] log)
        {
            Error(LogCategory.General, log);
        }
        public static void Error<T>(T category, params string[] log) where T : IComparable, IFormattable, IConvertible
        {
            Telescope.Debug.Error<T>(category, log);
        }

    }
}