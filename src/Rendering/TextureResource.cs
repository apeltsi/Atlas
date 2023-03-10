
namespace SolidCode.Atlas.Rendering
{
    using SolidCode.Atlas.AssetManagement;
    using Veldrid;

    public class TextureResource : AssetResource
    {
        public string path { get; protected set; }
        public string name { get; protected set; }

        public Veldrid.Texture? texture;
        public TextureResource()
        {
            path = "";
            name = "";
            texture = null;
        }
        public override void Load(string path)
        {
            this.path = path + ".ktx";
            this.name = name;
            try
            {
                this.texture = KtxFile.LoadTexture(Window._graphicsDevice, Window._graphicsDevice.ResourceFactory, Path.Join(Atlas.AssetsDirectory, this.path), PixelFormat.R8_G8_B8_A8_UNorm);
                this.IsValid = true;
            }
            catch (Exception e)
            {
                Debug.Error("Couldn't load texture " + this.name + ": " + e.Message);
                this.IsValid = false;
            }
        }

        public override void Dispose()
        {
            if (texture != null)
            {
                this.texture.Dispose();
                this.IsValid = false;
            }

        }
    }
}