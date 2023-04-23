using System.Reflection;

namespace SolidCode.Atlas.Telescope;

internal static class FileLogs
{
    private static StreamWriter? _primaryLogWriter;
    private static StreamWriter? _htmlLogWriter;

    internal static void InitializeFileLogs(string version)
    {
        if (!Directory.Exists("Logs"))
        {
            Directory.CreateDirectory("logs");
        }

        _primaryLogWriter = new StreamWriter("logs/All.log");
        
        _primaryLogWriter.WriteLine("ATLAS_VERSION: " + version);
        _primaryLogWriter.WriteLine("RUN_DATE: " + DateTime.Now.ToString());
        

        Assembly assembly = Assembly.GetExecutingAssembly();
        using (Stream stream = assembly.GetManifestResourceStream(assembly.GetManifestResourceNames().Single(str => str.EndsWith("LogViewer.html"))))
        {
            using (StreamReader reader = new StreamReader(stream))
            {
                string result = reader.ReadToEnd();
                _htmlLogWriter = new StreamWriter("logs/logs.html");
                
                _htmlLogWriter.Write(result);
                _htmlLogWriter.WriteLine("ENGINE_VERSION: " + version);
                _htmlLogWriter.WriteLine("RUN_DATE: " + DateTime.Now.ToString());
                
            }
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
        _primaryLogWriter.Close();
        _htmlLogWriter.Close();
    }
}