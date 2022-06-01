
namespace SolidCode.Caerus.Rendering
{
    using Veldrid;

    public class Texture
    {
        public string path { get; protected set; }
        public string name { get; protected set; }

        public Veldrid.Texture texture;
        public Texture(string path, string name, GraphicsDevice _graphicsDevice, ResourceFactory factory)
        {
            this.path = path;
            this.name = name;
            this.texture = KtxFile.LoadTexture(_graphicsDevice, factory, Path.Join(Caerus.AssetsDirectory, path), PixelFormat.R8_G8_B8_A8_UNorm);
        }

    }
}