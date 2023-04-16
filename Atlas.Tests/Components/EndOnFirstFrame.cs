using SolidCode.Atlas;
using SolidCode.Atlas.Rendering;
using SolidCode.Atlas.ECS;
namespace SolidCode.Atlas.Tests;
public class EndOnFirstFrame : Component
{
    public void Update()
    {
        Window.Close();
    }
}