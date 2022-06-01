using System.Numerics;
using SolidCode.Caerus;
using SolidCode.Caerus.Components;
using SolidCode.Caerus.ECS;
using SolidCode.Caerus.Rendering;
using Veldrid;

public class TextRenderer : RenderComponent
{
    private TextDrawable? textDrawable;
    private string _text = "A new text component";
    public int Size = 30;
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
                textDrawable.UpdateText(value);
            }
        }
    }
    public override Drawable[] StartRender(GraphicsDevice _graphicsDevice)
    {
        textDrawable = new TextDrawable(Text, "Comfortaa-Regular.ttf", Size, entity.GetComponent<Transform>());
        List<Drawable> drawables = new List<Drawable>();
        drawables.Add(textDrawable);
        return drawables.ToArray();
    }

    public override void Update()
    {
    }
    public override void FixedUpdate()
    {

    }
}