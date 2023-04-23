using SolidCode.Atlas;
using SolidCode.Atlas.Rendering;
using SolidCode.Atlas.ECS;
namespace SolidCode.Atlas.Tests;
public class UserConfirm : Component
{
    public void Update()
    {
        if (Input.Input.GetKeyDown(Veldrid.Key.Space))
        {
            Window.Close();
        }
        if (Input.Input.GetKeyDown(Veldrid.Key.BackSpace))
        {
            Assert.Fail("User marked test as failed.");
        }
    }
}