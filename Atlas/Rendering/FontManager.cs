using FontStashSharp;

namespace SolidCode.Atlas.Rendering
{
    public static class FontManager
    {
        private static Dictionary<string, byte[]> loadedFonts = new Dictionary<string, byte[]>();
        public static void AddFont(FontSystem result, string font)
        {
            if (loadedFonts.ContainsKey(font))
            {
                result.AddFont(loadedFonts[font]);
            }
            else
            {
                loadedFonts.Add(font, File.ReadAllBytes(Path.Join(Atlas.AssetsDirectory, font)));
                result.AddFont(loadedFonts[font]);

            }
        }
    }
}