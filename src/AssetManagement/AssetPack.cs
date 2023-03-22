using System.IO.Compression;
using System.Reflection;
using SolidCode.Atlas.Rendering;

namespace SolidCode.Atlas.AssetManagement
{
    public class AssetPack
    {
        private static Dictionary<string, List<string>> loadFiles = new Dictionary<string, List<string>>();
        public string relativePath { get; protected set; }
        private List<string> assetsLoaded = new List<string>();
        public delegate string[] AssetHandler(ZipArchive zip, ZipArchiveEntry entry, AssetMode mode);
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
            using (FileStream stream = File.Open(Path.Join(Atlas.AssetPackDirectory, relativePath + ".assetpack"), FileMode.Open))
            {
                using (ZipArchive zip = new ZipArchive(stream, ZipArchiveMode.Read))
                    LoadFromArchive(zip);
            }
        }

        internal List<string> LoadSpecificFiles(List<string> files)
        {
            lock (loadFiles)
            {
                if (loadFiles.ContainsKey(this.relativePath))
                {
                    lock (loadFiles[this.relativePath])
                    {
                        loadFiles[this.relativePath].AddRange(files);
                    }
                }
                else
                {
                    loadFiles.Add(this.relativePath, files);
                }
            }

            lock (loadFiles)
            {
                if (loadFiles[this.relativePath].Count > 0 && loadFiles.ContainsKey(this.relativePath))
                {
                    using (FileStream stream = File.Open(Path.Join(Atlas.AssetPackDirectory, relativePath + ".assetpack"), FileMode.Open))
                    {
                        using (ZipArchive zip = new ZipArchive(stream, ZipArchiveMode.Read))
                            LoadFromArchive(zip, loadFiles[this.relativePath].ToArray());
                    }
                    loadFiles.Remove(this.relativePath);
                }
            }




            lock (loadFiles)
            {
                loadFiles.Remove(relativePath);
            }
            return assetsLoaded;

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
        private void LoadFromArchive(ZipArchive zip, string[]? paths = null)
        {
            List<Thread> threads = new List<Thread>();
            foreach (var entry in zip.Entries)
            {
                AssetMode mode = AssetMode.KeepAlive;
                if (paths != null)
                {
                    mode = AssetMode.Unload;
                    bool matches = false;
                    foreach (string path in paths)
                    {
                        if (path == entry.FullName)
                        {
                            matches = true;
                        }
                    }
                    if (!matches) continue;
                }
                string extension = entry.Name.Split(".").Last();
                if (assetHandlers.ContainsKey(extension))
                {
                    Thread t = new Thread(() => LoadAssetFromPack(zip, entry, extension, mode));
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
            Debug.Log(LogCategory.Framework, assetsLoaded.Count + " asset(s) loaded from AssetPack '" + relativePath + "'");
        }

        private void LoadAssetFromPack(ZipArchive zip, ZipArchiveEntry entry, string extension, AssetMode mode)
        {
            string[] assets = assetHandlers[extension].Invoke(zip, entry, mode);
            lock (assetsLoaded)
            {
                assetsLoaded.AddRange(assets);
            }
        }

        private static class DefaultHandlers
        {
            public static string[] HandleShader(ZipArchive zip, ZipArchiveEntry entry, AssetMode mode)
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
                        AssetManager.LoadAssetToMemory<Shader>(new Stream[] { vertMemoryStream, fragMemoryStream }, shaderPath, mode);
                    }
                    return new string[] { shaderPath };
                }
                return new string[0];
            }

            public static string[] HandleTexture(ZipArchive zip, ZipArchiveEntry entry, AssetMode mode)
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
                    AssetManager.LoadAssetToMemory<Texture>(new Stream[] { mstream }, texturePath, mode);
                }
                return new string[] { entry.FullName };
            }
        }
    }
}