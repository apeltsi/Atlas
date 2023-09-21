using SolidCode.Atlas.AssetManagement;
using SolidCode.Atlas.ECS;
using SolidCode.Atlas.Rendering;
using Veldrid;

namespace SolidCode.Atlas.Components;

public enum TextAlignment
{
    Left,
    Center,
    Right
}

public enum TextVerticalAlignment
{
    Top,
    Center,
    Bottom
}

public class TextRenderer : RenderComponent
{
    private TextAlignment _alignment = TextAlignment.Center;
    private Color _color = Color.White;
    private FontSet _fontSet = FontSet.GetDefault();
    private float _resolutionScale = 1f;
    private float _size = 50f;
    private string _text = "Hello World!";
    private TextDrawable? _textDrawable;
    private TextVerticalAlignment _verticalAlignment = TextVerticalAlignment.Center;

    /// <summary>
    /// The size of the text
    /// </summary>
    public float Size
    {
        get => _size;
        set
        {
            _size = value;
            if (_textDrawable != null) _textDrawable.UpdateText(_text, value, _resolutionScale);
        }
    }

    /// <summary>
    /// A multiplier for the resolution of the text.
    /// </summary>
    public float ResolutionScale
    {
        get => _resolutionScale;
        set
        {
            _resolutionScale = value;
            if (_textDrawable != null) _textDrawable.UpdateText(_text, value, _resolutionScale);
        }
    }

    /// <summary>
    /// The horizontal alignment of the text
    /// </summary>
    public TextAlignment HorizontalAlignment
    {
        get => _alignment;
        set
        {
            _alignment = value;
            if (_textDrawable != null) _textDrawable.UpdateAlignment(value, _verticalAlignment);
        }
    }

    /// <summary>
    /// The vertical alignment of the text
    /// </summary>
    public TextVerticalAlignment VerticalAlignment
    {
        get => _verticalAlignment;
        set
        {
            _verticalAlignment = value;
            if (_textDrawable != null) _textDrawable.UpdateAlignment(_alignment, value);
        }
    }

    /// <summary>
    /// A set of fonts to use for the text
    /// </summary>
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

    /// <summary>
    /// The color of the text
    /// </summary>
    public Color Color
    {
        get
        {
            if (_textDrawable == null) return _color;
            return _textDrawable.Color;
        }
        set
        {
            if (_textDrawable != null)
                _textDrawable.Color = value;
            else
                _color = value;
        }
    }

    /// <summary>
    /// The text content
    /// </summary>
    public string Text
    {
        get => _text;
        set
        {
            _text = value;
            if (_textDrawable != null) _textDrawable.UpdateText(value, Size, _resolutionScale);
        }
    }

    /// <summary>
    /// THIS METHOD SHOULD ONLY BE CALLED BY THE RENDERER UNLESS YOU KNOW WHAT YOU'RE DOING
    /// </summary>
    /// <param name="graphicsDevice">The graphics device</param>
    /// <returns>A drawable array</returns>
    public override Drawable[] StartRender(GraphicsDevice graphicsDevice)
    {
        AssetManager.RequireBuiltinAssets();
        _textDrawable = new TextDrawable(Text, _fontSet, Color, HorizontalAlignment, VerticalAlignment, Size,
            _resolutionScale, Entity.GetComponent<Transform>(true)!);
        return new Drawable[] { _textDrawable };
    }
}