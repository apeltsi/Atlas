using System.Text;
using Veldrid;
using Veldrid.SPIRV;

namespace SolidCode.Caerus.Rendering
{
    public class Shader
    {
        private ShaderDescription vertexShaderDesc;
        private ShaderDescription fragmentShaderDesc;
        public Veldrid.Shader[] shaders { get; protected set; }
        public Shader(ResourceFactory factory, string vertPath, string fragPath)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            Debug.Log(LogCategories.Rendering, "Loading shader: ['" + vertPath + "' and '" + fragPath + "']");
            vertPath = Path.Join(Caerus.ShaderDirectory, vertPath);
            fragPath = Path.Join(Caerus.ShaderDirectory, fragPath);

            var vertSource = File.ReadAllText(vertPath);
            var fragSource = File.ReadAllText(fragPath);

            vertexShaderDesc = new ShaderDescription(
                ShaderStages.Vertex,
                Encoding.UTF8.GetBytes(vertSource),
                "main");
            fragmentShaderDesc = new ShaderDescription(
                ShaderStages.Fragment,
                Encoding.UTF8.GetBytes(fragSource),
                "main");
            try
            {
                shaders = factory.CreateFromSpirv(vertexShaderDesc, fragmentShaderDesc);
            }
            catch (Exception ex)
            {
                Debug.Error(LogCategories.Rendering, ex.ToString());
            }
            watch.Stop();
            Debug.Log(LogCategories.Rendering, "Shader loaded & compiled  [" + watch.ElapsedMilliseconds + "ms]");
        }

        public void Dispose()
        {
            for (int i = 0; i < shaders.Length; i++)
            {
                shaders[i].Dispose();
            }
        }
    }
}