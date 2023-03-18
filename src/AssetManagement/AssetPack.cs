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

        public void Load()
        {
            Debug.Log("Loading AssetPack: " + relativePath);
            if (relativePath == "atlas")
            {
                Assembly assembly = Assembly.GetExecutingAssembly();

                using (Stream stream = assembly.GetManifestResourceStream(assembly.GetManifestResourceNames().Single(str => str.EndsWith("atlas.assetpack"))))

                using (StreamReader reader = new StreamReader(stream))
                using (ZipArchive zip = new ZipArchive(stream, ZipArchiveMode.Read))
                    LoadFromArchive(zip);
            }
        }

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
                    string texturePath = entry.FullName.Substring("assets/".Length);
                    using (var stream = entry.Open())
                        AssetManager.LoadAsset<Texture>(new Stream[] { stream }, texturePath, AssetMode.KeepAlive);
                    assetsLoaded.Add(entry.FullName);
                }

            }
        }
    }
}