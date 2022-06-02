
namespace SolidCode.Caerus.Rendering
{
    using SharpText.Core;
    using SharpText.Veldrid;
    using SolidCode.Caerus.Components;
    using Veldrid;

    public class TextDrawable : Drawable
    {
        public Transform transform;
        string text;
        Font font;
        VeldridTextRenderer textRenderer;
        public TextDrawable(string text, string fontPath, int size, Transform transform)
        {
            this.text = text;
            this.transform = transform;
            try
            {
                font = new Font(Path.Join(Caerus.AssetsDirectory, fontPath), size);
            }
            catch (Exception e)
            {
                Debug.Error("Font couldn't be loaded: " + e.ToString());
            }
            CreateResources(Window._graphicsDevice);
        }

        public void UpdateText(string text)
        {
            Window._graphicsDevice.WaitForIdle();
            textRenderer.Update();
            textRenderer.DrawText(text, transform.position, new Color(255, 255, 255, 1), 1f);
        }

        public override void CreateResources(GraphicsDevice _graphicsDevice)
        {

            textRenderer = new VeldridTextRenderer(_graphicsDevice, Window.GetCommandList(), font, Window.DuplicatorFramebuffer);
            textRenderer.DrawText(text, transform.position, new Color(255, 255, 255, 1), 1f);
        }
        public override void Draw(CommandList cl)
        {
            textRenderer.Draw();
        }

        public override void Dispose()
        {
            textRenderer.Dispose();
        }


    }
}