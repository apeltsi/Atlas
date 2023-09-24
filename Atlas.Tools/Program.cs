using System.Diagnostics;
using Atlas.Tools.AssetCompiler;

namespace Atlas.Tools;

public static class Program
{
    private static void Main(string[] args)
    {
        if (args.Length > 0)
            switch (args[0].ToLower())
            {
                case "compile":
                    Compiler.Compile(args);
                    break;
                case "run":
                    Compiler.Compile(args);
                    string remainingArguments = "run " + string.Join(" ", args[1..]);
                    // Now lets run "dotnet run" 
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "dotnet",
                            Arguments = remainingArguments,
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true
                        }
                    };
                    process.Start();
                    while (!process.HasExited)
                    {
                        Console.WriteLine(process.StandardOutput.ReadLine() ?? "");
                    }

                    process.WaitForExit();
                    break;
                default:
                    ColoredText("Error: Invalid command.", ConsoleColor.Red);
                    break;
            }
        else
            ColoredText("Error: No command was specified.", ConsoleColor.Red);
    }

    public static void ColoredText(string text, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        Console.ResetColor();
    }
}