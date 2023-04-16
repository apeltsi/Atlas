using SolidCode.Atlas.AssetManagement;

namespace SolidCode.Atlas.Rendering
{
    public class Font : Asset
    {
        public byte[] Data { get; protected set; }
        public override void Dispose()
        {

        }

        public override void FromStreams(Stream[] stream, string name)
        {
            try
            {
                List<byte> data = new List<byte>();
                Data = ((MemoryStream)stream[0]).ToArray();
                IsValid = true;
            }
            finally
            {

            }
        }

        public override void Load(string path, string name)
        {
            try
            {
                Data = File.ReadAllBytes(Path.Join(Atlas.AssetsDirectory, path));
                IsValid = true;
            }
            finally
            {

            }
        }
    }
}