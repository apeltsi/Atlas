namespace SolidCode.Atlas;

/// <summary>
/// Configuration for the Atlas Framework
/// </summary>
public struct FrameworkConfiguration
{
    /// <summary>
    /// Configuration for the Entity Component System
    /// </summary>
    public ECSSettings ECS = new();

    /// <summary>
    /// Configuration for the Atlas Framework
    /// </summary>
    public FrameworkConfiguration()
    {
    }
}

/// <summary>
/// Settings for the Entity Component System
/// </summary>
public struct ECSSettings
{
    /// <summary>
    /// Configuration for the Tick Threads
    /// </summary>
    public ECSThreadSettings[] Threads =
    {
        new()
        {
            Name = "Main",
            Frequency = 100,
            Sync = true
        }
    };

    /// <summary>
    /// Settings for the Entity Component System
    /// </summary>
    public ECSSettings()
    {
        var hasMain = false;
        foreach (var t in Threads)
            if (t.Name == "Main")
            {
                hasMain = true;
                break;
            }

        if (!hasMain) throw new Exception("Main thread is required");
    }
}

/// <summary>
/// Configuration for a Tick Thread
/// </summary>
public struct ECSThreadSettings
{
    /// <summary>
    /// The Identifier of ECS Thread
    /// </summary>
    public string Name = "Main";

    /// <summary>
    /// Frequency of the thread in Hz
    /// </summary>
    public int Frequency = 100;

    /// <summary>
    /// Should this thread be synchronized with other ECS threads with the Sync enabled
    /// (Recommended value is true)
    /// </summary>
    public bool Sync = true;

    /// <summary>
    /// Creates a new Tick Thread Configuration
    /// </summary>
    public ECSThreadSettings()
    {
    }
}