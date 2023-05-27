using System.Numerics;
using FontStashSharp;
using SolidCode.Atlas.AssetManagement;

namespace SolidCode.Atlas.Rendering;

public class FontSet
{
    public Font[] Fonts;
    public FontSystem System { get; private set; }
    private static List<FontSet> _sets = new List<FontSet>();
    internal FontTextureManager TextureManager = new FontTextureManager();
    public FontSet(Font[] fonts)
    {
        Fonts = fonts;
        System = new FontSystem();
        foreach (var font in Fonts)
        {
            System.AddFont(font.Data);
        }
        _sets.Add(this);
    }

    public Vector2 MeasureString(float size, string text)
    {
        return System.GetFont(size).MeasureString(text);
    }

    public void Dispose()
    {
        System.Dispose();
        TextureManager.Dispose();
        Fonts = Array.Empty<Font>();
    }

    private static FontSet? _default;
    public static FontSet GetDefault()
    {
        if (_default == null)
        {
            _default = new FontSet(new Font[] { AssetManager.GetAsset<Font>("OpenSans-Regular")! });
        }
        return _default;
    }

    internal static void DisposeAll()
    {
        foreach (var set in _sets)
        {
            set.Dispose();
        }
        Debug.Log("All FontSets disposed.");
    }
}