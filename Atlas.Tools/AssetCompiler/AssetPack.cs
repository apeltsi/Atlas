using System.Text.RegularExpressions;
namespace Atlas.Tools.AssetCompiler;
public class AssetPack
{
    public string optimizeFor;
    public string[] include;
    public string[] excludeExtensions;

    public AssetPack(string[] include, string? optimizeFor, string[]? excludeExtensions)
    {
        this.include = include;
        if (optimizeFor == null)
        {
            this.optimizeFor = "standard";
        }
        else
        {
            this.optimizeFor = optimizeFor;
        }
        if (excludeExtensions == null)
        {
            this.excludeExtensions = new string[0];
        }
        else
        {
            this.excludeExtensions = excludeExtensions;
        }
    }

    public string[] GetAllPaths(string path)
    {
        List<string> collectedFiles = new List<string>();
        List<string>? cachedFileNames = null;
        string? dirname = Path.GetDirectoryName(path);
        if (dirname == null)
        {
            return collectedFiles.ToArray();
        }
        Regex rx = new Regex(@"^regex:(.*)");
        for (int i = 0; i < include.Length; i++)
        {
            string rule = include[i];
            Match m = rx.Match(rule);
            // REGEX
            if (m.Captures.Count > 0)
            {
                if (m.Groups.Count > 1)
                {
                    Regex regexRule = new Regex(m.Groups[1].Value);
                    if (cachedFileNames == null)
                    {
                        cachedFileNames = GetFilesAt(dirname);
                    }
                    foreach (string p in cachedFileNames)
                    {
                        if (regexRule.Match(p).Success)
                        {
                            collectedFiles.Add(p);
                        }
                    }
                }
            }
            else
            {
                // Check if the string is a path to a file
                if (!rule.Contains("*") && File.Exists(Path.Join(dirname, rule)))
                {
                    collectedFiles.Add(rule);
                }
                else
                {
                    string pattern = rule;
                    string dirpath = dirname;
                    SearchOption searchOption = SearchOption.TopDirectoryOnly;
                    if (pattern.StartsWith("**/"))
                    {
                        pattern = pattern.Substring(3);
                        searchOption = SearchOption.AllDirectories;
                    }
                    if (pattern.Contains("/") && searchOption == SearchOption.TopDirectoryOnly)
                    {
                        string[] dirs = pattern.Split("/");
                        pattern = dirs[^1];
                        List<string> dirsToSearch = new List<string>();
                        dirsToSearch.Add(dirpath);

                        // Lets find all of our directories
                        for (int depth = 0; depth < dirs.Length - 1; depth++)
                        {
                            string[] curdirs = dirsToSearch.ToArray();
                            dirsToSearch.Clear();
                            foreach (string dir in curdirs)
                            {
                                foreach (string directory in Directory.EnumerateDirectories(dir, dirs[depth], SearchOption.TopDirectoryOnly))
                                {
                                    dirsToSearch.Add(directory);
                                }

                            }
                        }

                        // Now lets look for any files in our directories
                        foreach (string dir in dirsToSearch)
                        {
                            foreach (string file in Directory.EnumerateFiles(dir, pattern, SearchOption.TopDirectoryOnly))
                            {
                                collectedFiles.Add(Path.GetRelativePath(dirname, file));
                            }
                        }
                    }
                    else
                    {
                        try
                        {
                            // So the rule isn't a regex rule or a file path, then we must assume that it is a search pattern
                            foreach (string file in Directory.EnumerateFiles(dirpath, pattern, searchOption))
                            {
                                collectedFiles.Add(Path.GetRelativePath(dirname, file));
                            }
                        }
                        catch { }

                    }
                }
            }
        }

        // Remove assetpack configs & excluded extensions
        List<string> filesToRemove = new List<string>();
        foreach (string file in collectedFiles)
        {
            if (Compiler.assetpackexp.Match(file).Success)
            {
                filesToRemove.Add(file);
                continue;
            }
            foreach (string excluded in this.excludeExtensions)
            {
                if (file.EndsWith(excluded))
                {
                    filesToRemove.Add(file);
                    break;
                }
            }
        }

        foreach (string file in filesToRemove)
        {
            collectedFiles.Remove(file);
        }

        // Remove duplicates and return value
        return collectedFiles.Distinct().ToArray();
    }
    static List<string> GetFilesAt(string path)
    {
        List<string> paths = new List<string>();
        foreach (string file in Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories))
        {
            paths.Add(Path.GetRelativePath(path, file));
        }
        return paths;
    }
}