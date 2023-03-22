using System.IO.Compression;
using System.Reflection;
using SolidCode.Atlas.Rendering;

namespace SolidCode.Atlas.AssetManagement
{
    public class AssetPack
    {
        public string relativePath { get; protected set; }
        private List<string> assetsLoaded = new List<string>();
        public delegate string[] AssetHandler(ZipArchive zip, ZipArchiveEntry entry);
        private static Dictionary<string, AssetHandler> assetHandlers = new Dictionary<string, AssetHandler>();
        public static void AddAssetHandler(string extension, AssetHandler handler)
        {
            assetHandlers.Add(extension, handler);
        }
        public static void RemoveAssetHandler(string extension)
        {
            assetHandlers.Remove(extension);
        }
        public AssetPack(string relativePath)
        {
            this.relativePath = relativePath;
            if (!assetHandlers.ContainsKey("ktx") || !assetHandlers.ContainsKey("frag"))
            {
                AddDefaultHandlers();
            }
        }

        private static void AddDefaultHandlers()
        {
            lock (assetHandlers)
            {
                // First, lets make sure that the default handlers don't exist
                if (!assetHandlers.ContainsKey("ktx"))
                {
                    AddAssetHandler("ktx", DefaultHandlers.HandleTexture);
                }
                if (!assetHandlers.ContainsKey("frag"))
                {
                    AddAssetHandler("frag", DefaultHandlers.HandleShader);
                }
            }
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
            List<Thread> threads = new List<Thread>();
            foreach (var entry in zip.Entries)
            {
                string extension = entry.Name.Split(".").Last();
                if (assetHandlers.ContainsKey(extension))
                {
                    Thread t = new Thread(() => LoadAssetFromPack(zip, entry, extension));
                    threads.Add(t);
                    t.Start();
                }
            }
            while (threads.Count > 0)
            {
                bool removed = false;
                foreach (Thread t in threads)
                {
                    if (!t.IsAlive)
                    {
                        threads.Remove(t);
                        removed = true;
                        break;
                    }
                }
                if (!removed)
                    Thread.Sleep(5);
            }
        }

        private void LoadAssetFromPack(ZipArchive zip, ZipArchiveEntry entry, string extension)
        {
            string[] assets = assetHandlers[extension].Invoke(zip, entry);
            lock (assetsLoaded)
            {
                assetsLoaded.AddRange(assets);
            }
        }

        private static class DefaultHandlers
        {
            public static string[] HandleShader(ZipArchive zip, ZipArchiveEntry entry)
            {
                string shaderPath = entry.FullName.Substring(0, entry.FullName.Length - 5);
                ZipArchiveEntry? e = zip.GetEntry(shaderPath + ".vert");
                if (e != null)
                {
                    using (var fragMemoryStream = new MemoryStream())
                    using (var vertMemoryStream = new MemoryStream())
                    {
                        lock (zip)
                        {

                            shaderPath = shaderPath.Substring("shaders/".Length);
                            // Okay we have a valid shader


                            using (var fragStream = entry.Open())
                            using (var vertStream = e.Open())
                            {
                                fragStream.CopyTo(fragMemoryStream);
                                vertStream.CopyTo(vertMemoryStream);
                            }
                            fragMemoryStream.Position = 0;
                            vertMemoryStream.Position = 0;
                        }
                        AssetManager.LoadAsset<Shader>(new Stream[] { vertMemoryStream, fragMemoryStream }, shaderPath, AssetMode.KeepAlive);
                    }
                    return new string[] { shaderPath };
                }
                return new string[0];
            }

            public static string[] HandleTexture(ZipArchive zip, ZipArchiveEntry entry)
            {
                int startLength = "assets/".Length;
                string texturePath = entry.FullName.Substring(startLength, entry.FullName.Length - ".ktx".Length - startLength);
                using (var mstream = new MemoryStream())
                {
                    lock (zip)
                    {
                        using (var stream = entry.Open())
                        {
                            stream.CopyTo(mstream);
                        }
                        mstream.Position = 0;
                    }
                    AssetManager.LoadAsset<Texture>(new Stream[] { mstream }, texturePath, AssetMode.KeepAlive);
                }
                return new string[] { entry.FullName };
            }
        }
    }
}