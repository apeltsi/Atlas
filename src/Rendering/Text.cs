
namespace SolidCode.Caerus.Rendering
{
    using FontStashSharp;
    using SolidCode.Caerus.Components;
    using Veldrid;

    public class TextDrawable : Drawable
    {
        public Transform transform;
        string text;
        FontSystem font;
        string fontPath;
        public TextDrawable(string text, string fontPath, int size, Transform transform)
        {
            this.text = text;
            this.transform = transform;
            this.fontPath = fontPath;
            CreateResources(Window._graphicsDevice);
        }

        public void UpdateText(string text)
        {
        }

        public override void CreateResources(GraphicsDevice _graphicsDevice)
        {
            this.font = new FontSystem();
            FontManager.AddFont(this.font, this.fontPath);
        }
        public override void Draw(CommandList cl)
        {
        }

        public override void Dispose()
        {
            this.font.Dispose();
        }


    }
}