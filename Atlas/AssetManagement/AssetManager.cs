using System.Collections.Concurrent;
using System.Text.Json;
using SolidCode.Atlas.Audio;
using SolidCode.Atlas.Rendering;
using SolidCode.Atlas.Telescope;
namespace SolidCode.Atlas.AssetManagement
{
    /*

    AssetManager

    The asset manager uses WeakReferences, they allow GC to do its thing 
    and get rid of assets that are no longer referenced anywhere even though
    we still have a reference here (It becomes null after being garbage collected). 

    This way we can give out a reference to the same resource from the AssetManager
    while still allowing the garbage collector to make some cuts.
    */

    public static class AssetManager
    {
        static ConcurrentDictionary<string, WeakReference<Asset>> loadedAssets = new ConcurrentDictionary<string, WeakReference<Asset>>();
        static ConcurrentDictionary<string, Asset> keepAliveAssets = new ConcurrentDictionary<string, Asset>();
        static Dictionary<string, List<string>> assetMap = new Dictionary<string, List<string>>(); // <string: path, string[] index 0 = assetpackname index n = truepath of file(s)
        
        // Quick Helper functions
        /// <summary>
        /// Shorthand for GetAsset&lt;Texture>(path, tryLoad)
        /// </summary>
        /// <param name="path">The path (excluding the extension) of the asset</param>
        /// <param name="tryLoad">Should the AssetManager try loading the asset if it isn't currently in memory. Defaults to <c>true</c></param>
        /// <returns>The <c>Texture</c></returns>
        public static Texture GetTexture(string path, bool tryLoad = true)
        {
            // We don't return a nullable here because worst case scenario we get the error texture. (Unless the internal AssetPack isn't loaded for whatever reason)
            return GetAsset<Texture>(path, tryLoad)!;
        }
        /// <summary>
        /// Shorthand for GetAsset&lt;AudioTrack>(path, tryLoad)
        /// </summary>
        /// <param name="path">The path (excluding the extension) of the asset</param>
        /// <param name="tryLoad">Should the AssetManager try loading the asset if it isn't currently in memory. Defaults to <c>true</c></param>
        /// <returns>The <c>AudioTrack</c></returns>
        public static AudioTrack? GetAudio(string path, bool tryLoad = true)
        {
            return GetAsset<AudioTrack>(path, tryLoad);
        }
        /// <summary>
        /// Shorthand for GetAsset&lt;Shader>(path, tryLoad)
        /// </summary>
        /// <param name="path">The path (excluding the extension) of the asset</param>
        /// <param name="tryLoad">Should the AssetManager try loading the asset if it isn't currently in memory. Defaults to <c>true</c></param>
        /// <returns>The <c>Shader</c></returns>
        public static Shader? GetShader(string path, bool tryLoad = true)
        {
            return GetAsset<Shader>(path, tryLoad);
        }
        /// <summary>
        /// Loads an asset into memory and returns it or alternatively if it's already loaded it will return the loaded asset.
        /// </summary>
        /// <param name="path">The path (excluding the extension) of the asset</param>
        /// <param name="tryLoad">Should the AssetManager try loading the asset if it isn't currently in memory. Defaults to <c>true</c></param>
        /// <typeparam name="T">The type of asset to load</typeparam>
        /// <returns>The asset</returns>
        public static T? GetAsset<T>(string path, bool tryLoad = true) where T : Asset, new()
        {
            Asset? asset = null;
            WeakReference<Asset>? a;
            loadedAssets.TryGetValue(path, out a);
            if (a != null)
            {
                a.TryGetTarget(out asset);
            }
            if (asset != null)
            {
                return asset as T;
            }
            if (!tryLoad)
            {
                return null;
            }
            // Okay, so the asset isn't currently loaded, lets try and load it up
            T? assetFromMemory = LoadAssetToMemory<T>(path, AssetMode.Unload);
            
            if (assetFromMemory == null)
            {
                // Okay so we still don't have an asset, lets see if we have a default available
                if (typeof(T) == typeof(Texture))
                {
                    return GetAsset<T>("error");
                }
            }
            return assetFromMemory;
        }

        internal static void LoadAssetMap()
        {
            try
            {
                string assetmapPath = Path.Join(Atlas.AssetPackDirectory, ".assetmap");
                Dictionary<string, List<string>> map = new Dictionary<string, List<string>>();
                if (File.Exists(assetmapPath))
                {
                    Dictionary<string, string> tempmap = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(assetmapPath));
                    foreach (KeyValuePair<string, string> item in tempmap)
                    {
                        string extensionlessPath = item.Key.Substring(0, item.Key.Length - (item.Key.Split(".").Last().Length + 1));
                        if (map.ContainsKey(extensionlessPath))
                        {
                            map[extensionlessPath].Add(item.Key);
                        }
                        else
                        {
                            List<string> paths = new List<string>();
                            paths.Add(item.Value);
                            paths.Add(item.Key);
                            map.Add(extensionlessPath, paths);
                        }
                    }
                }
                else
                {
                    Debug.Log(LogCategory.Framework, "Atlas starting without an AssetMap. Assets won't be loaded from AssetPacks unless manually loaded.");
                    return;
                }
                if (map == null)
                {
                    return;
                }
                assetMap = map;
                Debug.Log(LogCategory.Framework, "AssetMap loaded! (" + assetMap.Count + " assets mapped)");
            }
            catch (Exception e)
            {
                Debug.Error(LogCategory.Framework, "Couldn't load AssetMap: " + e.ToString());
            }
        }

        public static T? LoadAssetToMemory<T>(string path, AssetMode mode) where T : Asset, new()
        {
            string prefix = "";
            if (typeof(T) == typeof(Shader))
            {
                prefix = "shaders/";
            }
            else
            {
                prefix = "assets/";
            }
            if (assetMap.ContainsKey(prefix + path))
            {
                List<string> paths = assetMap[prefix + path];
                Debug.Warning(LogCategory.Framework, "AssetPack containing asset '" + path + "' was not loaded. Attempting to load asset manually from '" + paths[0] + "'");

                AssetPack pack = new AssetPack(paths[0]);
                pack.LoadSpecificFiles(paths.GetRange(1, paths.Count - 1));
                return GetAsset<T>(path, false);
            }

            T a = new T();
            lock (Renderer.GraphicsDevice)
            {
                a.Load(path, Path.GetFileName(path));
            }
            return FinalizeLoadingAsset<T>(a, mode, path);
        }
        public static T? LoadAssetToMemory<T>(Stream[] streams, string path, AssetMode mode) where T : Asset, new()
        {
            T a = new T();
            lock (Renderer.GraphicsDevice)
            {
                a.FromStreams(streams, Path.GetFileName(path));
            }
            return FinalizeLoadingAsset<T>(a, mode, path);
        }

        private static T? FinalizeLoadingAsset<T>(T a, AssetMode mode, string path) where T : Asset, new()
        {
            if (!a.IsValid)
            {
                Debug.Warning(LogCategory.Framework, "Couldn't load asset: " + path);
                return null;
            }
            loadedAssets.TryAdd(path, new WeakReference<Asset>(a, false));
            if (mode == AssetMode.KeepAlive)
            {
                keepAliveAssets.TryAdd(path, a);
            }
            return a;
        }

        public static void FreeAsset(string path)
        {
            if (keepAliveAssets.ContainsKey(path))
            {
                Asset? asset;
                keepAliveAssets.TryGetValue(path, out asset);
                if (asset != null)
                {
                    keepAliveAssets.TryRemove(new KeyValuePair<string, Asset>(path, asset));
                }
            }
        }
        
        /// <summary>
        /// Returns true if the asset specified is currently loaded.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool IsAssetLoaded(string path)
        {
            Cleanup();
            return loadedAssets.ContainsKey(path);
        }

        /// <summary>
        /// Only returns when the main builtin assets are loaded.
        /// </summary>
        public static void RequireBuiltinAssets()
        {
            Window.RequireBuiltinAssets();
        }

        /// <summary>
        /// WARNING: This method runs garbage collection. IT CAN BE VERY EXPENSIVE. Generally it is advised to let the garbage collector decide when the time is ripe.
        /// </summary>
        public static void ForceUnloadAssets()
        {
            GC.Collect();
            Cleanup();
        }

        internal static void Dispose()
        {
            Debug.Log(LogCategory.Framework, "Disposing AssetManager...");
            foreach (var asset in loadedAssets)
            {
                Asset? a = null;
                asset.Value.TryGetTarget(out a);
                if (a != null)
                {
                    a.Dispose();
                }
            }
            foreach (var asset in keepAliveAssets)
            {
                Asset a = asset.Value;
                a.Dispose();
            }
            loadedAssets = new ConcurrentDictionary<string, WeakReference<Asset>>();
            keepAliveAssets = new ConcurrentDictionary<string, Asset>();
            assetMap = new Dictionary<string, List<string>>();
            AssetPack.assetHandlers = new Dictionary<string, SolidCode.Atlas.AssetManagement.AssetPack.AssetHandler>();
            AssetPack.loadedAssetPacks = new Dictionary<string, AssetPack>();
            AssetPack.loadFiles = new Dictionary<string, List<string>>();
        }

        /// <summary>
        /// Removes some null values from loadedAssets. Note that this doesn't actually unload any assets, only cleanup the remaining nulls from unloaded assets.
        /// </summary>
        private static void Cleanup()
        {
            lock (loadedAssets) // Lets make sure that we have the assets for ourselves ;)
            {
                Dictionary<string, WeakReference<Asset>> curAssets = loadedAssets.ToDictionary(kvp => kvp.Key, kvp => kvp.Value, loadedAssets.Comparer);
                foreach (KeyValuePair<string, WeakReference<Asset>> asset in curAssets)
                {
                    Asset? a;
                    asset.Value.TryGetTarget(out a);
                    if (a == null)
                    {
                        loadedAssets.Remove(asset.Key, out _);
                    }
                }
            }
        }
    }
}