using SolidCode.Atlas.AssetManagement;
using SolidCode.Atlas.Rendering;
using Veldrid;
using Veldrid.SPIRV;
using Shader = Veldrid.Shader;

namespace SolidCode.Atlas.Compute;

public class ComputeShader : Asset
{
    public Shader? Shader { get; protected set; }

    public override void Load(string path, string name)
    {
        var truePath = Path.Join(Atlas.ShaderDirectory, path + ".compute");

        FromSource(File.ReadAllBytes(truePath));
    }

    private void FromSource(byte[] source)
    {
        var shaderDesc = new ShaderDescription(
            ShaderStages.Compute,
            source,
            "main");

        try
        {
            Shader = Renderer.GraphicsDevice.ResourceFactory.CreateFromSpirv(shaderDesc);
            IsValid = true;
        }
        catch (Exception ex)
        {
            Debug.Error(LogCategory.Framework, "Could not compile ComputeShader: " + ex);
        }
    }

    public override void FromStreams(Stream[] stream, string name)
    {
        FromSource(Rendering.Shader.ReadFully(stream[0]));
    }

    public override void Dispose()
    {
        Shader?.Dispose();
    }

    ~ComputeShader()
    {
        Dispose();
    }
}