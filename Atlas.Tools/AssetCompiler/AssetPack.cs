using System.Text.RegularExpressions;

namespace Atlas.Tools.AssetCompiler;

public class AssetPack
{
    public string[] excludeExtensions;
    public string[] include;
    public string optimizeFor;

    public AssetPack(string[] include, string? optimizeFor, string[]? excludeExtensions)
    {
        this.include = include;
        if (optimizeFor == null)
            this.optimizeFor = "standard";
        else
            this.optimizeFor = optimizeFor;
        if (excludeExtensions == null)
            this.excludeExtensions = new string[0];
        else
            this.excludeExtensions = excludeExtensions;
    }

    public string[] GetAllPaths(string path)
    {
        var collectedFiles = new List<string>();
        List<string>? cachedFileNames = null;
        var dirname = Path.GetDirectoryName(path);
        if (dirname == null) return collectedFiles.ToArray();
        var rx = new Regex(@"^regex:(.*)");
        for (var i = 0; i < include.Length; i++)
        {
            var rule = include[i];
            var m = rx.Match(rule);
            // REGEX
            if (m.Captures.Count > 0)
            {
                if (m.Groups.Count > 1)
                {
                    var regexRule = new Regex(m.Groups[1].Value);
                    if (cachedFileNames == null) cachedFileNames = GetFilesAt(dirname);
                    foreach (var p in cachedFileNames)
                        if (regexRule.Match(p).Success)
                            collectedFiles.Add(p);
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
                    var pattern = rule;
                    var dirpath = dirname;
                    var searchOption = SearchOption.TopDirectoryOnly;
                    if (pattern.StartsWith("**/"))
                    {
                        pattern = pattern.Substring(3);
                        searchOption = SearchOption.AllDirectories;
                    }

                    if (pattern.Contains("/") && searchOption == SearchOption.TopDirectoryOnly)
                    {
                        var dirs = pattern.Split("/");
                        pattern = dirs[^1];
                        var dirsToSearch = new List<string>();
                        dirsToSearch.Add(dirpath);

                        // Lets find all of our directories
                        for (var depth = 0; depth < dirs.Length - 1; depth++)
                        {
                            var curdirs = dirsToSearch.ToArray();
                            dirsToSearch.Clear();
                            foreach (var dir in curdirs)
                            foreach (var directory in Directory.EnumerateDirectories(dir, dirs[depth],
                                         SearchOption.TopDirectoryOnly))
                                dirsToSearch.Add(directory);
                        }

                        // Now lets look for any files in our directories
                        foreach (var dir in dirsToSearch)
                        foreach (var file in Directory.EnumerateFiles(dir, pattern, SearchOption.TopDirectoryOnly))
                            collectedFiles.Add(Path.GetRelativePath(dirname, file));
                    }
                    else
                    {
                        try
                        {
                            // So the rule isn't a regex rule or a file path, then we must assume that it is a search pattern
                            foreach (var file in Directory.EnumerateFiles(dirpath, pattern, searchOption))
                                collectedFiles.Add(Path.GetRelativePath(dirname, file));
                        }
                        catch
                        {
                        }
                    }
                }
            }
        }

        // Remove assetpack configs & excluded extensions
        var filesToRemove = new List<string>();
        foreach (var file in collectedFiles)
        {
            if (Compiler.Assetpackexp.Match(file).Success)
            {
                filesToRemove.Add(file);
                continue;
            }

            foreach (var excluded in excludeExtensions)
                if (file.EndsWith(excluded))
                {
                    filesToRemove.Add(file);
                    break;
                }
        }

        foreach (var file in filesToRemove) collectedFiles.Remove(file);

        // Remove duplicates and return value
        return collectedFiles.Distinct().ToArray();
    }

    private static List<string> GetFilesAt(string path)
    {
        var paths = new List<string>();
        foreach (var file in Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories))
            paths.Add(Path.GetRelativePath(path, file));
        return paths;
    }
}