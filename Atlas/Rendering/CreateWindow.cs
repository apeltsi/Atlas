using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace SolidCode.Atlas.Rendering;

public static class CreateWindow
{
    public static Sdl2Window CreateWindowWithFlags(ref WindowCreateInfo windowCI, SDL_WindowFlags flags)
    {
        flags |= SDL_WindowFlags.OpenGL | SDL_WindowFlags.Resizable
                                        | GetWindowFlags(windowCI.WindowInitialState);
        if (windowCI.WindowInitialState != WindowState.Hidden) flags |= SDL_WindowFlags.Shown;
        var window = new Sdl2Window(
            windowCI.WindowTitle,
            windowCI.X,
            windowCI.Y,
            windowCI.WindowWidth,
            windowCI.WindowHeight,
            flags,
            false);

        return window;
    }

    private static SDL_WindowFlags GetWindowFlags(WindowState state)
    {
        switch (state)
        {
            case WindowState.Normal:
                return 0;
            case WindowState.FullScreen:
                return SDL_WindowFlags.Fullscreen;
            case WindowState.Maximized:
                return SDL_WindowFlags.Maximized;
            case WindowState.Minimized:
                return SDL_WindowFlags.Minimized;
            case WindowState.BorderlessFullScreen:
                return SDL_WindowFlags.FullScreenDesktop;
            case WindowState.Hidden:
                return SDL_WindowFlags.Hidden;
            default:
                throw new VeldridException("Invalid WindowState: " + state);
        }
    }
}