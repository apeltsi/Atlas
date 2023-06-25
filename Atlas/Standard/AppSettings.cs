namespace SolidCode.Atlas.Standard;

public abstract class AppSettings
{
    protected AppSettings()
    {
        Debug.Log("Loading '" + this.GetType().Name + "'settings...");
    }

    public void Load()
    {
        
    }

    public void Revert()
    {
        Load();
    }

    public void Save()
    {
        
    }
}