using SolidCode.Atlas;
using SolidCode.Atlas.Rendering;
using SolidCode.Atlas.ECS;
namespace SolidCode.Atlas.Tests;
public class UserConfirm : Component
{
    public static bool Failed = false;
    public void Update()
    {
        if (Input.Input.GetKeyDown(Veldrid.Key.Space))
        {
            Window.Close();
        }
        if (Input.Input.GetKeyDown(Veldrid.Key.BackSpace))
        {
            Failed = true;
            Window.Close();
        }
    }
}