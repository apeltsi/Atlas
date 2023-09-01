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
    private float _size = 100f;

    public float Size
    {
        get => _size;
        set
        {
            _size = value;
            if (textDrawable != null)
            {
                textDrawable.UpdateText(_text, value);
            }

        }
    }
    public bool Centered = true;
    private Color _color = SolidCode.Atlas.Color.White;
    private FontSet _fontSet = FontSet.GetDefault();
    public FontSet Fonts
    {
        get => _fontSet;
        set
        {
            _fontSet = value;
            if (textDrawable != null)
                textDrawable.UpdateFontSet(_fontSet);
        }
    }
    public Color Color
    {
        get
        {
            if (textDrawable == null)
            {
                return _color;
            }
            return textDrawable.Color;
        }
        set
        {
            if (textDrawable != null)
            {
                textDrawable.Color = value;
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

    public Vector2 Measure()
    {
        return _fontSet.MeasureString(Size, _text);
    }
    
    public override Drawable[] StartRender(GraphicsDevice _graphicsDevice)
    {
        AssetManager.RequireBuiltinAssets();
        textDrawable = new TextDrawable(Text, _fontSet, Color, Centered, Size, Entity.GetComponent<Transform>(true));
        return new Drawable[] { textDrawable };
    }

}