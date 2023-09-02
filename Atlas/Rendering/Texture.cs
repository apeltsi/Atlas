
namespace SolidCode.Atlas.Rendering
{
    using SolidCode.Atlas.AssetManagement;
    using Veldrid;
    using SolidCode.Atlas.Telescope;

    public class Texture : Asset
    {
        public string Path { get; protected set; }
        public string Name { get; protected set; }

        public Veldrid.Texture? TextureData;
        private bool _autoDispose = true;
        public Texture()
        {
            Path = "";
            Name = "";
            TextureData = null;
        }

        public Texture(Veldrid.Texture textureData, bool autoDispose = true)
        {
            Path = "";
            Name = "";
            this.TextureData = textureData;
            _autoDispose = autoDispose;
        }

        public void LoadFromDisk(string absolutePath)
        {
            try
            {
                this.TextureData = KtxFile.LoadTexture(Renderer.GraphicsDevice, Renderer.GraphicsDevice.ResourceFactory, absolutePath, PixelFormat.R8_G8_B8_A8_UNorm);
                this.IsValid = true;
            }
            catch (Exception e)
            {
                Debug.Error(LogCategory.Framework, "Couldn't load texture " + this.Name + ": " + e.Message);
                this.IsValid = false;
            }
        }
        
        public override void Load(string path, string name)
        {
            this.Path = path + ".ktx";
            this.Name = name;
            try
            {
                this.TextureData = KtxFile.LoadTexture(Renderer.GraphicsDevice, Renderer.GraphicsDevice.ResourceFactory, System.IO.Path.Join(Atlas.AssetsDirectory, this.Path), PixelFormat.R8_G8_B8_A8_UNorm);
                this.IsValid = true;
            }
            catch (Exception e)
            {
                Debug.Error(LogCategory.Framework, "Couldn't load texture " + this.Name + ": " + e.Message);
                this.IsValid = false;
            }
        }
        public override void FromStreams(Stream[] streams, string name)
        {
            this.Name = name;
            try
            {
                this.TextureData = KtxFile.LoadTexture(Renderer.GraphicsDevice, Renderer.GraphicsDevice.ResourceFactory, streams[0], PixelFormat.R8_G8_B8_A8_UNorm);
                this.IsValid = true;
            }
            catch (Exception e)
            {
                Debug.Error(LogCategory.Framework, "Couldn't load texture " + this.Name + ": " + e.Message);
                Debug.Error(LogCategory.Framework, "" + e.StackTrace);
                this.IsValid = false;
            }

        }

        public override void Dispose()
        {
            if (TextureData != null && _autoDispose)
            {
                this.TextureData.Dispose();
                this.IsValid = false;
            }
        }
        
        ~Texture()
        {
            this.Dispose();
        }
    }
}