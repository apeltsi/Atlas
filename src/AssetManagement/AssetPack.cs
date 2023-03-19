using System.IO.Compression;
using System.Reflection;
using SolidCode.Atlas.Rendering;

namespace SolidCode.Atlas.AssetManagement
{
    public class AssetPack
    {
        public string relativePath { get; protected set; }
        private List<string> assetsLoaded = new List<string>();
        public AssetPack(string relativePath)
        {
            this.relativePath = relativePath;
        }

        internal void LoadAtlasAssetpack()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            using (Stream stream = assembly.GetManifestResourceStream(assembly.GetManifestResourceNames().Single(str => str.EndsWith("atlas.assetpack"))))

            using (ZipArchive zip = new ZipArchive(stream, ZipArchiveMode.Read))
                LoadFromArchive(zip);
        }
        ///<summary>
        /// Loads the assetpack into memory
        ///</summary>

        public void Load()
        {
            using (FileStream stream = File.Open(Path.Join(Atlas.AssetPackDirectory, relativePath), FileMode.Open))
            {
                using (ZipArchive zip = new ZipArchive(stream, ZipArchiveMode.Read))
                    LoadFromArchive(zip);
            }
        }
        ///<summary>
        /// Allows the AssetManager to unload the assets IF needed. If some assets are still in use they won't get unloaded as long as they're being used.
        ///</summary>
        public void Unload()
        {
            foreach (string path in assetsLoaded)
            {
                AssetManager.FreeAsset(path);
            }
        }
        private void LoadFromArchive(ZipArchive zip)
        {

            foreach (var entry in zip.Entries)
            {
                if (entry.Name.EndsWith("frag"))
                {
                    string shaderPath = entry.FullName.Substring(0, entry.FullName.Length - 5);
                    ZipArchiveEntry? e = zip.GetEntry(shaderPath + ".vert");
                    if (e != null)
                    {
                        shaderPath = shaderPath.Substring("shaders/".Length);
                        // Okay we have a valid shader
                        using (var fragStream = entry.Open())
                        using (var vertStream = e.Open())
                            AssetManager.LoadAsset<Shader>(new Stream[] { vertStream, fragStream }, shaderPath, AssetMode.KeepAlive);
                        assetsLoaded.Add(shaderPath);

                    }

                }
                else if (entry.Name.EndsWith("ktx"))
                {
                    int startLength = "assets/".Length;
                    string texturePath = entry.FullName.Substring(startLength, entry.FullName.Length - ".ktx".Length - startLength);
                    using (var mstream = new MemoryStream())
                    {
                        using (var stream = entry.Open())
                        {
                            stream.CopyTo(mstream);
                        }
                        mstream.Position = 0;
                        AssetManager.LoadAsset<Texture>(new Stream[] { mstream }, texturePath, AssetMode.KeepAlive);
                    }
                    assetsLoaded.Add(entry.FullName);
                }

            }
        }
    }
}