using SolidCode.Atlas.ECS;
using SolidCode.Atlas.Rendering;

namespace SolidCode.Atlas.AssetManagement
{
    public enum AssetMode
    {
        KeepAlive,
        Unload,
    }
    public abstract class Asset
    {
        public AssetMode Mode { get; protected set; }
        public string Path { get; protected set; }
        public bool IsValid { get; protected set; }

        protected Asset(string path, AssetMode mode)
        {
            Path = path;
            Mode = mode;
        }
    }

    public class Asset<AResource> : Asset where AResource : AssetResource, new()
    {
        public AResource Resource;
        public Asset(string path, AssetMode mode) : base(path, mode)
        {
            Resource = new AResource();
            Resource.Load(path);
            this.IsValid = Resource.IsValid;
        }

        ~Asset()
        {

        }
    }

    public abstract class AssetResource
    {
        public bool IsValid { get; protected set; }

        public AssetResource()
        {
        }

        public abstract void Load(string path);

        public abstract void Dispose();

    }
}