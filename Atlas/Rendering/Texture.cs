
namespace SolidCode.Atlas.Rendering
{
    using AssetManagement;
    using Veldrid;
    using Telescope;

    /// <summary>
    /// A texture that can be passed to drawables to render
    /// </summary>
    public class Texture : Asset
    {
        /// <summary>
        /// The path of the texture
        /// </summary>
        public string Path { get; protected set; }
        /// <summary>
        /// The name of the texture
        /// </summary>
        public string Name { get; protected set; }

        /// <summary>
        /// The actual texture data
        /// </summary>
        public Veldrid.Texture? TextureData;
        private bool _autoDispose = true;
        /// <summary>
        /// Creates a new, blank texture
        /// </summary>
        public Texture()
        {
            Path = "";
            Name = "";
            TextureData = null;
        }
        /// <summary>
        /// Creates a new texture from a Veldrid.Texture
        /// </summary>
        /// <param name="textureData">The texture data</param>
        /// <param name="autoDispose">Should Atlas automatically dispose this texture. (Most likely false if you're working with custom textures)</param>
        public Texture(Veldrid.Texture textureData, bool autoDispose = true)
        {
            Path = "";
            Name = "";
            this.TextureData = textureData;
            _autoDispose = autoDispose;
        }

        /// <summary>
        /// Loads the texture from disk
        /// </summary>
        /// <param name="absolutePath">The absolute file-system path of the texture</param>
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
        
        /// <summary>
        /// Loads the texture from disk
        /// </summary>
        /// <param name="path">The path of the texture</param>
        /// <param name="name">The name of the texture</param>
        public override void Load(string path, string name)
        {
            this.Path = path + ".ktx";
            this.Name = name;
            try
            {
                this.TextureData = KtxFile.LoadTexture(Renderer.GraphicsDevice, Renderer.GraphicsDevice.ResourceFactory, System.IO.Path.Join(Atlas.AssetsDirectory, "assets", this.Path), PixelFormat.R8_G8_B8_A8_UNorm);
                this.IsValid = true;
            }
            catch (Exception e)
            {
                Debug.Error(LogCategory.Framework, "Couldn't load texture " + this.Name + ": " + e.Message);
                this.IsValid = false;
            }
        }
        
        /// <summary>
        /// Loads a texture from a stream
        /// </summary>
        /// <param name="streams">Should be an array of exactly one stream, any other streams are ignored</param>
        /// <param name="name">The name of the texture</param>
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

        /// <summary>
        /// Disposes the texture
        /// <para/>
        /// Note: Atlas usually does this automatically, unless you have autoDispose = false in the constructor
        /// </summary>
        public override void Dispose()
        {
            if (TextureData != null && _autoDispose) {
                TickScheduler.RequestTick().Wait();
                Renderer.GraphicsDevice!.WaitForIdle();
                this.TextureData.Dispose();
                this.IsValid = false;
                TickScheduler.FreeThreads();
            }
        }
        
        ~Texture()
        {
            this.Dispose();
        }
    }
}