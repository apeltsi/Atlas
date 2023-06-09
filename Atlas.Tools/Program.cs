using Atlas.Tools;
using Atlas.Tools.AssetCompiler;

namespace AtlasTools
{
    public static class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                switch (args[0].ToLower())
                {
                    case "compile":
                        Compiler.Compile(args);
                        break;
                    default:
                        ColoredText("Error: Invalid command.", ConsoleColor.Red);
                        break;

                }
            }
            else
            {
                ColoredText("Error: No command was specified.", ConsoleColor.Red);
            }
        }

        public static void ColoredText(string text, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ResetColor();
        }

    }
}