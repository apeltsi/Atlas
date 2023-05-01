using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Resources;
using SolidCode.Atlas.Audio;
using SolidCode.Atlas.Compute;
using SolidCode.Atlas.Rendering;
using SolidCode.Atlas.Telescope;

namespace SolidCode.Atlas.AssetManagement
{
    public class AssetPack
    {
        internal static Dictionary<string, AssetPack> loadedAssetPacks = new Dictionary<string, AssetPack>();
        internal static Dictionary<string, List<string>> loadFiles = new Dictionary<string, List<string>>();
        public string relativePath { get; protected set; }
        internal List<string> assetsLoaded = new List<string>();
        public delegate string[] AssetHandler(ZipArchive zip, ZipArchiveEntry entry, AssetMode mode);
        internal static Dictionary<string, AssetHandler> assetHandlers = new Dictionary<string, AssetHandler>();
        private Assembly _assembly = Assembly.GetExecutingAssembly();
        public static void AddAssetHandler(string extension, AssetHandler handler)
        {
            assetHandlers.Add(extension, handler);
        }
        public static void RemoveAssetHandler(string extension)
        {
            assetHandlers.Remove(extension);
        }
        /// <summary>
        /// Specifies an AssetPack that can later be loaded. 
        /// </summary>
        /// <param name="relativePath">The path relative to the assets folder, eg. "assets/default" would be "default". If this starts with %ASSEMBLY% then it will be the name of the AssetPack resource excluding the extension. So atlas.assetpack would be "%ASSEMBLY%/atlas".</param>
        /// <param name="assembly">(OPTIONAL! USE ONLY IF LOADING FROM ASSEMBLY) The assembly containing the AssetPack</param>
        public AssetPack(string relativePath, Assembly? assembly = null)
        {
            this.relativePath = relativePath;
            if (assembly != null)
            {
                _assembly = assembly;
            }
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
                    AddAssetHandler("ktx", DefaultHandlers.HandleBytedata<Texture>);
                }
                if (!assetHandlers.ContainsKey("frag"))
                {
                    AddAssetHandler("frag", DefaultHandlers.HandleShader);
                }
                if (!assetHandlers.ContainsKey("wav"))
                {
                    AddAssetHandler("wav", DefaultHandlers.HandleBytedata<AudioTrack>);
                }
                if (!assetHandlers.ContainsKey("ttf"))
                {
                    AddAssetHandler("ttf", DefaultHandlers.HandleBytedata<Font>);
                }
                if (!assetHandlers.ContainsKey("compute"))
                {
                    AddAssetHandler("compute", DefaultHandlers.HandleBytedata<ComputeShader>);
                }

            }
        }

        private Stream? FromAssembly(string identifier)
        {
            return _assembly.GetManifestResourceStream(_assembly.GetManifestResourceNames().Single(str => str.EndsWith(identifier)));
        }

        public Stream GetStream()
        {
            if (relativePath.StartsWith("%ASSEMBLY%/"))
            {
                string name = relativePath.Substring("%ASSEMBLY%/".Length);
                
                Stream s = FromAssembly(name + ".assetpack");
                if (s == null)
                    throw new MissingManifestResourceException("The AssetPack '" + relativePath + "' could not be found in the assembly. The assetpack should be named: '" + name + ".assetpack'");
                return s;
                
            }
            else
            {
                return File.Open(Path.Join(Atlas.AssetPackDirectory, relativePath + ".assetpack"), FileMode.Open);
            }
        }
        
        ///<summary>
        /// Loads the AssetPack into memory
        ///</summary>

        public void Load()
        {

            using (Stream stream = GetStream())
            {
                using (ZipArchive zip = new ZipArchive(stream, ZipArchiveMode.Read))
                    LoadFromArchive(zip);
            }
        }

        public static bool CheckIfLoaded(string relativePath)
        {
            lock (loadedAssetPacks)
            {
                return loadedAssetPacks.ContainsKey(relativePath);
            }
        }

        public Task LoadAsync()
        {
            return Task.Run(() => Load());
        }

        internal List<string> LoadSpecificFiles(List<string> files)
        {
            lock (loadedAssetPacks)
            {
                if (loadedAssetPacks.ContainsKey(this.relativePath))
                {
                    lock (loadedAssetPacks[this.relativePath])
                    {
                        return loadedAssetPacks[this.relativePath].assetsLoaded;
                    }
                }
            }
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
                    using (Stream stream = GetStream())
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
            lock (loadedAssetPacks)
            {
                loadedAssetPacks.Remove(this.relativePath);
            }
            foreach (string path in assetsLoaded)
            {
                AssetManager.FreeAsset(path);
            }
        }
        private void LoadFromArchive(ZipArchive zip, string[]? paths = null)
        {
            Stopwatch s = Stopwatch.StartNew();
            if (paths == null)
            {
                // Lets add this AssetPack to the loaded list so that the same assets don't get loaded multiple times
                lock (loadedAssetPacks)
                {
                    if (loadedAssetPacks.ContainsKey(this.relativePath))
                        return;
                    loadedAssetPacks.Add(this.relativePath, this);
                }
            }
            // We lock loadFiles so that we can't manually load anything twice accidentally
            lock (loadFiles) lock (loadedAssetPacks.ContainsKey(this.relativePath) ? loadedAssetPacks[this.relativePath] : this)
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
                }
            SolidCode.Atlas.Telescope.Debug.Log(LogCategory.Framework, assetsLoaded.Count + " asset(s) loaded from AssetPack '" + relativePath + "' (" + Math.Round(s.ElapsedMilliseconds / 1000.0, 2) + "s)");
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

            public static string[] HandleBytedata<T>(ZipArchive zip, ZipArchiveEntry entry, AssetMode mode) where T : Asset, new()
            {
                int startLength = "assets/".Length;
                string path = entry.FullName.Substring(startLength, entry.FullName.Length - ".ktx".Length - startLength);
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
                    AssetManager.LoadAssetToMemory<T>(new Stream[] { mstream }, path, mode);
                }
                return new string[] { entry.FullName };
            }

        }
    }
}