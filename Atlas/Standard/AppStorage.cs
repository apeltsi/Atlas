using System.Reflection;

namespace SolidCode.Atlas.Standard;

public static class AppStorage
{
    private static string _dataPath;
    public static string DataPath
    {
        get
        {
            if (_dataPath == null)
            {
                Initialize();
            }
            return _dataPath;
        }
    } 
    private static bool isInitialized = false;
    /// <summary>
    /// Added between the AppData folder and the application name
    /// <para />
    /// So: AppData/Roaming/[PathPrefix]/[AppName]/[Path]
    /// <para />
    /// Defaults to "Atlas Framework"
    /// </summary>
    public static string PathPrefix = "Atlas Framework";
    private static void Initialize()
    {
        if (!isInitialized)
        {
            var appName = System.Reflection.Assembly.GetEntryAssembly().GetName().Name;
            var path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            path = Path.Combine(path, PathPrefix, appName);
            _dataPath = path;
            isInitialized = true;
        }
    }

    public static byte[] Load(string path)
    {
        // Lets load the file from the path
        var filePath = Path.Combine(DataPath, path);
        if (File.Exists(filePath))
        {
            // We'll have to read the file into a byte array
            return File.ReadAllBytes(filePath);
        }

        return Array.Empty<byte>();
    }

    public static void Save(string path, byte[] data)
    {
        // First we'll have to make sure that the path exists by creating any missing directories.
        var filePath = Path.Combine(DataPath, path);
        var directoryPath = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
        // Now lets save the data
        File.WriteAllBytes(filePath, data);
    }
}