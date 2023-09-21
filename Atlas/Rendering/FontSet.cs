using System.Numerics;
using FontStashSharp;
using SolidCode.Atlas.AssetManagement;

namespace SolidCode.Atlas.Rendering;

public class FontSet
{
    private static readonly List<FontSet> _sets = new();

    private static FontSet? _default;
    public Font[] Fonts;
    internal FontTextureManager TextureManager = new();

    public FontSet(Font[] fonts)
    {
        Fonts = fonts;
        System = new FontSystem(new FontSystemSettings
        {
            TextureWidth = 2048, // Defaults to 1024, but 2048 gives more space for more & larger characters
            TextureHeight = 2048
        });
        // When the atlas is full, reset the font system so that it can create a new atlas.
        System.CurrentAtlasFull += (e, a) => System.Reset();
        foreach (var font in Fonts) System.AddFont(font.Data);
        _sets.Add(this);
    }

    public FontSystem System { get; }

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

    public static FontSet GetDefault()
    {
        if (_default == null)
        {
            if (!AssetManager.IsAssetLoaded("OpenSans-Regular")) new AssetPack("%ASSEMBLY%/default-font").Load();
            _default = new FontSet(new[] { AssetManager.GetAsset<Font>("OpenSans-Regular")! });
        }

        return _default;
    }

    internal static void DisposeAll()
    {
        foreach (var set in _sets) set.Dispose();
        Debug.Log("All FontSets disposed.");
    }
}