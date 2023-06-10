using SolidCode.Atlas.AssetManagement;
using SolidCode.Atlas.Rendering;
using Veldrid;
using Veldrid.SPIRV;

namespace SolidCode.Atlas.Compute;

public class ComputeShader : Asset
{
    public Veldrid.Shader? Shader { get; protected set; }
    public override void Load(string path, string name)
    {
        string truePath = Path.Join(Atlas.ShaderDirectory, path + ".compute");

        FromSource(File.ReadAllBytes(truePath));
    }

    private void FromSource(byte[] source)
    {
        ShaderDescription shaderDesc = new ShaderDescription(
            ShaderStages.Compute,
            source,
            "main");
        
        try
        {
            Shader = Renderer.GraphicsDevice.ResourceFactory.CreateFromSpirv(shaderDesc);
            this.IsValid = true;
        }
        catch (Exception ex)
        {
            Debug.Error(LogCategory.Framework, "Could not compile ComputeShader: " + ex.ToString());
        }

    }

    public override void FromStreams(Stream[] stream, string name)
    {
        FromSource(SolidCode.Atlas.Rendering.Shader.ReadFully(stream[0]));
    }

    public override void Dispose()
    {
        Shader?.Dispose();
    }
}