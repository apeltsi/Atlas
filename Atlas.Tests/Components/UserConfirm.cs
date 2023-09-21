using SolidCode.Atlas.ECS;
using SolidCode.Atlas.Rendering;
using Veldrid;

namespace SolidCode.Atlas.Tests;

public class UserConfirm : Component
{
    public static bool Failed;

    public void Update()
    {
        if (Input.Input.GetKeyDown(Key.Space)) Window.Close();
        if (Input.Input.GetKeyDown(Key.BackSpace))
        {
            Failed = true;
            Window.Close();
        }
    }
}