using System.Runtime.InteropServices;

namespace SolidCode.Atlas.Rendering.Windows;
#if Windows
internal static class ForceHighPerformance
{
    [DllImport("nvapi64.dll", EntryPoint = "fake")]
    private static extern int LoadNvApi64();

    [DllImport("nvapi.dll", EntryPoint = "fake")]
    private static extern int LoadNvApi32();

    internal static void InitializeDedicatedGraphics()
    {
        // On hybrid nvidia systems, this will force the dedicated graphics card to be used
        try
        {
            if (Environment.Is64BitProcess)
                LoadNvApi64();
            else
                LoadNvApi32();
        }
        catch
        {
        } // will always fail since 'fake' entry point doesn't exists
    }
}
#endif