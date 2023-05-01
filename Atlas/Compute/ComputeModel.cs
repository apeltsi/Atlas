/*
 
 
 
using SolidCode.Atlas.AssetManagement;
using SolidCode.Atlas.Rendering;
using Veldrid;

namespace SolidCode.Atlas.Compute;

public class ComputeModel<TResourceSet>
{
    private Pipeline? _pipeline;
    private ResourceSet _resourceSet;
    
    public ComputeModel(string shaderPath)
    {
        ComputeShader? shader = AssetManager.GetAsset<ComputeShader>(shaderPath);
        
        if (shader == null || shader.Shader == null)
        {
            Debug.Error(LogCategory.Framework, "Could not create ComputeModel: Shader is null.");
            return;
        }
        
        CreateResources(shader);
    }
    
    
    private void CreateResources(ComputeShader shader)
    {
        ResourceFactory factory = Renderer.GraphicsDevice.ResourceFactory;
        _resourceSet = factory.CreateResourceSet(resourceSetDescription);
        new BufferDescription
        {
            SizeInBytes = 0,
            Usage = BufferUsage.StructuredBufferReadWrite,
            StructureByteStride = 0,
            RawBuffer = false
        }
        var pipelineDesc = new ComputePipelineDescription
        {
            ComputeShader = shader.Shader!,
            ResourceLayouts = new ResourceLayout[]
            {
            },
            ThreadGroupSizeX = 1,
            ThreadGroupSizeY = 1,
            ThreadGroupSizeZ = 1,
            
        };
        
        _pipeline = factory.CreateComputePipeline(pipelineDesc);
    }

    public void Dispatch(uint xGroups, uint yGroups, uint zGroups)
    {
        Renderer.CommandList.SetPipeline(_pipeline!);
        Renderer.CommandList.SetComputeResourceSet(0, );
        Renderer.CommandList.Dispatch(xGroups, yGroups, zGroups);
        
        GraphicsDevice gd = Renderer.GraphicsDevice!;
        gd.WaitForIdle();
        Renderer.CommandList.Dispatch(xGroups, yGroups, zGroups);
        gd.SubmitCommands(Renderer.CommandList);
    }
}*/