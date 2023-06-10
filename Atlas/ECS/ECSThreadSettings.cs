namespace SolidCode.Atlas.ECS;

public struct ECSThreadSettings
{
    /// <summary>
    /// Name of ECS Thread
    /// </summary>
    public string Name;
    /// <summary>
    /// Frequency of the thread in Hz
    /// </summary>
    public int Frequency;
    /// <summary>
    /// Should this thread be synchronized with other ECS threads with the Sync enabled
    /// </summary>
    public bool Sync;
}