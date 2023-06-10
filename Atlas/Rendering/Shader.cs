using System.Text;
using SolidCode.Atlas.AssetManagement;
using Veldrid;
using Veldrid.SPIRV;

namespace SolidCode.Atlas.Rendering
{
    public class Shader : Asset
    {
        public Veldrid.Shader[] Shaders { get; protected set; }

        public Shader()
        {
            this.Shaders = new Veldrid.Shader[0];
        }


        public override void Load(string path, string name)
        {
            string vertPath = Path.Join(Atlas.ShaderDirectory, path + ".vert");
            string fragPath = Path.Join(Atlas.ShaderDirectory, path + ".frag");

            var vertSource = File.ReadAllBytes(vertPath);
            var fragSource = File.ReadAllBytes(fragPath);
            FromSource(vertSource, fragSource);
        }

        private void FromSource(byte[] vertSource, byte[] fragSource)
        {
            bool isSPIRV = HasSpirvHeader(vertSource);
            if (Renderer.GraphicsDevice.BackendType == GraphicsBackend.Direct3D11)
                isSPIRV = false;
            ShaderDescription vertexShaderDesc = new ShaderDescription(
                ShaderStages.Vertex,
                vertSource,
                isSPIRV ? "vert" : "main");

            ShaderDescription fragmentShaderDesc = new ShaderDescription(
                ShaderStages.Fragment,
                fragSource,
                isSPIRV ? "pixel" : "main");
            try
            {
                Shaders = Renderer.GraphicsDevice.ResourceFactory.CreateFromSpirv(vertexShaderDesc, fragmentShaderDesc);
                this.IsValid = true;
            }
            catch (Exception ex)
            {
                Debug.Error(LogCategory.Rendering, ex.ToString());
            }
        }

        internal static bool HasSpirvHeader(byte[] bytes)
        {
            return bytes.Length > 4
                   && bytes[0] == 0x03
                   && bytes[1] == 0x02
                   && bytes[2] == 0x23
                   && bytes[3] == 0x07;
        }

        public override void FromStreams(Stream[] streams, string name)
        {
            FromSource(ReadFully(streams[0]), ReadFully(streams[1]));
        }
        
        internal static byte[] ReadFully(Stream input)
        {
            byte[] buffer = new byte[16*1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }


        public override void Dispose()
        {
            for (int i = 0; i < Shaders.Length; i++)
            {
                Shaders[i].Dispose();
            }
        }
    }
}