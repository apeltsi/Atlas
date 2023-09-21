using SolidCode.Atlas.ECS;
using SolidCode.Atlas.Rendering;

namespace SolidCode.Atlas.Tests;

public class EndOnFirstFrame : Component
{
    public void Update()
    {
        Window.Close();
    }
}