namespace SolidCode.Atlas.AssetManagement
{
    /// <summary>
    /// Determines how Atlas should handle the asset after it is loaded
    /// </summary>
    public enum AssetMode
    {
        /// <summary>
        /// Atlas won't automatically unload the asset
        /// </summary>
        KeepAlive,
        /// <summary>
        /// Atlas will automatically unload the asset when it is no longer in use
        /// </summary>
        Unload,
    }


    public abstract class Asset
    {
        public bool IsValid { get; protected set; }

        public Asset()
        {
        }

        public abstract void Load(string path, string name);
        public abstract void FromStreams(Stream[] stream, string name);

        public abstract void Dispose();

        ~Asset()
        {
            this.Dispose();
        }

    }
}