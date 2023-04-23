namespace SolidCode.Atlas.Components;

using System.Numerics;
using SolidCode.Atlas.AssetManagement;
using SolidCode.Atlas.ECS;
using SolidCode.Atlas.Rendering;
using Veldrid;

public class TextRenderer : RenderComponent
{
    private TextDrawable? textDrawable;
    private string _text = "Hello World!";
    public int Size = 100;
    public bool Centered = true;
    private Vector4 _color = new Vector4(1f, 1f, 1f, 1f);
    private List<Font> _fonts = new List<Font>() { AssetManager.GetAsset<Font>("OpenSans-Regular") };
    public List<Font> Fonts
    {
        get => _fonts;
        set
        {
            _fonts = value;
            if (textDrawable != null)
                textDrawable.UpdateFonts(_fonts.ToArray());
        }
    }
    public Vector4 Color
    {
        get
        {
            if (textDrawable == null)
            {
                return _color;
            }
            return textDrawable.color;
        }
        set
        {
            if (textDrawable != null)
            {
                textDrawable.color = value;
            }
            else
            {
                _color = value;
            }
        }
    }
    public string Text
    {
        get
        {
            return _text;
        }
        set
        {
            _text = value;
            if (textDrawable != null)
            {
                textDrawable.UpdateText(value, Size);
            }
        }
    }
    public override Drawable[] StartRender(GraphicsDevice _graphicsDevice)
    {
        textDrawable = new TextDrawable(Text, _fonts.ToArray(), Color, Centered, Size, entity.GetComponent<Transform>());
        return new Drawable[] { textDrawable };
    }

}