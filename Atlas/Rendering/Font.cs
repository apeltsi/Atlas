using SolidCode.Atlas.AssetManagement;

namespace SolidCode.Atlas.Rendering;

public class Font : Asset
{
    public byte[] Data { get; protected set; }

    public override void Dispose()
    {
    }

    public override void FromStreams(Stream[] stream, string name)
    {
        var data = new List<byte>();
        Data = ((MemoryStream)stream[0]).ToArray();
        IsValid = true;
    }

    public override void Load(string path, string name)
    {
        Data = File.ReadAllBytes(Path.Join(Atlas.AssetsDirectory, "assets", path));
        IsValid = true;
    }
}