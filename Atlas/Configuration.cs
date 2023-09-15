namespace SolidCode.Atlas;

public struct FrameworkConfiguration
{
    public ECSSettings ECS = new ();

    public FrameworkConfiguration()
    {
    }
}

public struct ECSSettings
{
    public ECSThreadSettings[] Threads = new[]
    {
        new ECSThreadSettings
        {
            Name = "Main",
            Frequency = 100,
            Sync = true
        }
    };

    public ECSSettings()
    {
        bool hasMain = false;
        foreach (var t in Threads)
        {
            if (t.Name == "Main")
            {
                hasMain = true;
                break;
            }
        }

        if (!hasMain)
        {
            throw new Exception("Main thread is required");
        }
    }
}

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

    public ECSThreadSettings()
    {
    }
}