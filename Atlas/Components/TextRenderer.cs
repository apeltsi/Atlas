namespace SolidCode.Atlas.Components;

using System.Numerics;
using AssetManagement;
using ECS;
using Rendering;
using Veldrid;

public class TextRenderer : RenderComponent
{
    private TextDrawable? _textDrawable;
    private string _text = "Hello World!";
    private float _size = 50f;
    private float _resolutionScale = 1f;

    public float Size
    {
        get => _size;
        set
        {
            _size = value;
            if (_textDrawable != null)
            {
                _textDrawable.UpdateText(_text, value, _resolutionScale);
            }

        }
    }

    public float ResolutionScale
    {
        get => _resolutionScale;
        set
        {
            _resolutionScale = value;
            if (_textDrawable != null)
            {
                _textDrawable.UpdateText(_text, value, _resolutionScale);
            }
        }
    }
    public bool Centered = true;
    private Color _color = Color.White;
    private FontSet _fontSet = FontSet.GetDefault();
    public FontSet Fonts
    {
        get => _fontSet;
        set
        {
            _fontSet = value;
            if (_textDrawable != null)
                _textDrawable.UpdateFontSet(_fontSet);
        }
    }
    public Color Color
    {
        get
        {
            if (_textDrawable == null)
            {
                return _color;
            }
            return _textDrawable.Color;
        }
        set
        {
            if (_textDrawable != null)
            {
                _textDrawable.Color = value;
            }
            else
            {
                _color = value;
            }
        }
    }
    public string Text
    {
        get => _text;
        set
        {
            _text = value;
            if (_textDrawable != null)
            {
                _textDrawable.UpdateText(value, Size, _resolutionScale);
            }
        }
    }

    public Vector2 Measure()
    {
        return _fontSet.MeasureString(Size, _text);
    }
    
    public override Drawable[] StartRender(GraphicsDevice graphicsDevice)
    {
        AssetManager.RequireBuiltinAssets();
        _textDrawable = new TextDrawable(Text, _fontSet, Color, Centered, Size, _resolutionScale, Entity.GetComponent<Transform>(true)!);
        return new Drawable[] { _textDrawable };
    }

}