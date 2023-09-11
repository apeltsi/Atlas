namespace SolidCode.Atlas.AssetManagement
{
    public enum AssetMode
    {
        KeepAlive,
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