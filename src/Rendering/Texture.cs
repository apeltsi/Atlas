
namespace SolidCode.Atlas.Rendering
{
    using SolidCode.Atlas.AssetManagement;
    using Veldrid;

    public class Texture : Asset
    {
        public string path { get; protected set; }
        public string name { get; protected set; }

        public Veldrid.Texture? texture;
        public Texture()
        {
            path = "";
            name = "";
            texture = null;
        }
        public override void Load(string path, string name)
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
        public override void FromStreams(Stream[] streams, string name)
        {
            this.name = name;
            try
            {
                this.texture = KtxFile.LoadTexture(Window._graphicsDevice, Window._graphicsDevice.ResourceFactory, streams[0], PixelFormat.R8_G8_B8_A8_UNorm);
                this.IsValid = true;
            }
            catch (Exception e)
            {
                Debug.Error("Couldn't load texture " + this.name + ": " + e.Message);
                Debug.Error("" + e.StackTrace);
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