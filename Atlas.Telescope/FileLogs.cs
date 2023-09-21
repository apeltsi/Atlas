using System.Reflection;

namespace SolidCode.Atlas.Telescope;

internal static class FileLogs
{
    private static StreamWriter? _primaryLogWriter;
    private static StreamWriter? _htmlLogWriter;

    internal static void InitializeFileLogs(string version)
    {
        try
        {
            if (!Directory.Exists("Logs")) Directory.CreateDirectory("logs");

            _primaryLogWriter = new StreamWriter("logs/All.log");

            _primaryLogWriter.WriteLine("ATLAS_VERSION: " + version);
            _primaryLogWriter.WriteLine("RUN_DATE: " + DateTime.Now);


            var assembly = Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream(assembly.GetManifestResourceNames()
                       .Single(str => str.EndsWith("LogViewer.html"))))
            {
                using (var reader = new StreamReader(stream))
                {
                    var result = reader.ReadToEnd();
                    _htmlLogWriter = new StreamWriter("logs/logs.html");

                    _htmlLogWriter.Write(result);
                    _htmlLogWriter.WriteLine("ENGINE_VERSION: " + version);
                    _htmlLogWriter.WriteLine("RUN_DATE: " + DateTime.Now);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    internal static void DoLog(string log)
    {
        _primaryLogWriter.WriteLine(log);
        _htmlLogWriter.WriteLine(log);
    }

    internal static void Dispose()
    {
        DoLog("--- END OF LOG ---");
        try
        {
            _primaryLogWriter.Close();
            _htmlLogWriter.Close();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
}