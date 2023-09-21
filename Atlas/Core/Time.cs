using SolidCode.Atlas.ECS;

namespace SolidCode.Atlas;

public static class Time
{
    /// <summary>
    /// Time between the current frame and the start of the application. Measured in seconds
    /// </summary>

    public static double time { get; internal set; }

    /// <summary>
    /// Time between the current tick and the start of the application. Measured in seconds
    /// NOTE: This can only be called from Tick() Threads created by the ECS
    /// </summary>

    public static double tickTime => TickManager.GetThreadTime();

    /// <summary>
    /// The time between the current frame and the previous frame. Measured in seconds
    /// </summary>

    public static double deltaTime { get; internal set; }

    /// <summary>
    /// The time between the current tick and the previous tick. Measured in seconds
    /// NOTE: This can only be called from Tick() Threads created by the ECS
    /// </summary>

    public static double tickDeltaTime => TickManager.GetThreadDelta();

    /// <summary>
    /// The time since the start of the application. Measured in seconds
    /// </summary>
    public static double trueTime => Atlas.ECSStopwatch.Elapsed.TotalSeconds;
}