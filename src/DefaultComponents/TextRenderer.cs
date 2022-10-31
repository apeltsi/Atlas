using System.Numerics;
using SolidCode.Caerus;
using SolidCode.Caerus.Components;
using SolidCode.Caerus.ECS;
using SolidCode.Caerus.Rendering;
using Veldrid;

public class TextRenderer : RenderComponent
{
    private TextDrawable? textDrawable;
    private string _text = "Hello World!";
    public int Size = 100;
    public bool Centered = true;
    private Vector4 _color;
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
        TextDrawable drawable = new TextDrawable(Text, new string[3] { "Comfortaa-Regular.ttf", "Gugi-Regular.ttf", "NotoSansJP-Regular.otf" }, Color, Centered, Size, entity.GetComponent<Transform>());
        textDrawable = drawable;
        return new Drawable[] { drawable };
    }

    public override void Update()
    {
    }
    public override void FixedUpdate()
    {

    }
}