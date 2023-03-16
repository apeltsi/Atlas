using System.Text;
using Veldrid;
using Veldrid.SPIRV;

namespace SolidCode.Atlas.Rendering
{
    public class Shader
    {
        public Veldrid.Shader[] shaders { get; protected set; }
        public Shader(ResourceFactory factory, string vertPath, string fragPath)
        {
            vertPath = Path.Join(Atlas.ShaderDirectory, vertPath);
            fragPath = Path.Join(Atlas.ShaderDirectory, fragPath);

            var vertSource = File.ReadAllText(vertPath);
            var fragSource = File.ReadAllText(fragPath);

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
                shaders = factory.CreateFromSpirv(vertexShaderDesc, fragmentShaderDesc);
            }
            catch (Exception ex)
            {
                Debug.Error(LogCategory.Rendering, ex.ToString());
            }
        }

        public void Dispose()
        {
            // TODO(amos): There should probably be a way to unload shaders that haven't been used for a while
            for (int i = 0; i < shaders.Length; i++)
            {
                shaders[i].Dispose();
            }
        }
    }
}