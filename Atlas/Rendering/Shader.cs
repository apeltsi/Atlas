using System.Text;
using SolidCode.Atlas.AssetManagement;
using Veldrid;
using Veldrid.SPIRV;

namespace SolidCode.Atlas.Rendering
{
    public class Shader : Asset
    {
        public Veldrid.Shader[] shaders { get; protected set; }

        public Shader()
        {
            this.shaders = new Veldrid.Shader[0];
        }



        public override void Load(string path, string name)
        {
            string vertPath = Path.Join(Atlas.ShaderDirectory, path + ".vert");
            string fragPath = Path.Join(Atlas.ShaderDirectory, path + ".frag");

            var vertSource = File.ReadAllText(vertPath);
            var fragSource = File.ReadAllText(fragPath);
            FromSource(vertSource, fragSource);
        }

        private void FromSource(string vertSource, string fragSource)
        {

            ShaderDescription vertexShaderDesc = new ShaderDescription(
    ShaderStages.Vertex,
    Encoding.UTF8.GetBytes(vertSource),
    "main");
            ShaderDescription fragmentShaderDesc = new ShaderDescription(
                ShaderStages.Fragment,
                Encoding.UTF8.GetBytes(fragSource),
                "main");
            try
            {
                shaders = Window.GraphicsDevice.ResourceFactory.CreateFromSpirv(vertexShaderDesc, fragmentShaderDesc);
                this.IsValid = true;
            }
            catch (Exception ex)
            {
                Debug.Error(LogCategory.Rendering, ex.ToString());
            }


        }
        public override void FromStreams(Stream[] streams, string name)
        {
            using (StreamReader vreader = new StreamReader(streams[0]))
            using (StreamReader freader = new StreamReader(streams[1]))
                FromSource(vreader.ReadToEnd(), freader.ReadToEnd());

        }


        public override void Dispose()
        {
            for (int i = 0; i < shaders.Length; i++)
            {
                shaders[i].Dispose();
            }
        }
    }
}