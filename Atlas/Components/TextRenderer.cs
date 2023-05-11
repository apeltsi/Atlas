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

    public Vector2 Measure()
    {
        return _fontSet.MeasureString(Size, _text);
    }
    
    public override Drawable[] StartRender(GraphicsDevice _graphicsDevice)
    {
        textDrawable = new TextDrawable(Text, _fontSet, Color, Centered, Size, entity.GetComponent<Transform>(true));
        return new Drawable[] { textDrawable };
    }

}