using SolidCode.Atlas;
using SolidCode.Atlas.Rendering;
using SolidCode.Atlas.ECS;
namespace SolidCode.Atlas.Tests;
public class UserConfirm : Component
{
    public void Update()
    {
        if (Input.InputManager.GetKeyDown(Veldrid.Key.Space))
        {
            Window.Close();
        }
        if (Input.InputManager.GetKeyDown(Veldrid.Key.BackSpace))
        {
            Assert.Fail("User marked test as failed.");
        }
    }
}