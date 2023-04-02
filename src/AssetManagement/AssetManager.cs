using System.Collections.Concurrent;
using System.Text.Json;
using SolidCode.Atlas.Rendering;

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
            return LoadAssetToMemory<T>(path, AssetMode.Unload);
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
            lock (Window._graphicsDevice)
            {
                a.Load(path, Path.GetFileName(path));
            }
            return FinalizeLoadingAsset<T>(a, mode, path);
        }
        public static T? LoadAssetToMemory<T>(Stream[] streams, string path, AssetMode mode) where T : Asset, new()
        {
            T a = new T();
            lock (Window._graphicsDevice)
            {
                a.FromStreams(streams, Path.GetFileName(path));
            }
            return FinalizeLoadingAsset<T>(a, mode, path);
        }

        private static T? FinalizeLoadingAsset<T>(T a, AssetMode mode, string path) where T : Asset, new()
        {
            if (!a.IsValid)
            {
                Debug.Log(LogCategory.Framework, "Couldn't load asset: " + path);
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
        /// WARNING: This method runs garbage collection. IT CAN BE VERY EXPENSIVE. Generally it is adivsed to let the garbage collector decide when the time is ripe.
        /// </summary>
        static void ForceUnloadAssets()
        {
            GC.Collect();
            Cleanup();
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