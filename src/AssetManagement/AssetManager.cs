using System.Collections.Concurrent;
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
        public static T? GetAsset<T>(string path) where T : Asset, new()
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
            // Okay, so the asset isn't currently loaded, lets try and load it up
            return LoadAsset<T>(path, AssetMode.Unload);
        }

        public static T? LoadAsset<T>(string path, AssetMode mode) where T : Asset, new()
        {
            Debug.Log(LogCategory.Framework, "Loading Asset: " + path);
            T a = new T();
            lock (Window._graphicsDevice)
            {
                a.Load(path, Path.GetFileName(path));
            }
            return FinalizeLoadingAsset<T>(a, mode, path);
        }
        public static T? LoadAsset<T>(Stream[] streams, string path, AssetMode mode) where T : Asset, new()
        {
            Debug.Log(LogCategory.Framework, "Loading Asset: " + path);

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