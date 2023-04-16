namespace SolidCode.Atlas.ECS
{
    /// <summary>Limits the allowed instance count of a component to 1. 
    /// The component cannot be added if a Entity if another instance of the component already exists in the ECS.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class SingleInstanceAttribute : LimitInstanceCountAttribute
    {
        public SingleInstanceAttribute()
        {
            this.count = 1;
        }
    }
    /// <summary>
    /// Limits the allowed instance count of a component to <c>count</c>. 
    /// The component cannot be added onto a Entity if <c>count</c> of the component already exists in the ECS.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class LimitInstanceCountAttribute : Attribute
    {
        public int count { get; protected set; }
        public LimitInstanceCountAttribute(int count)
        {
            this.count = count;
        }
        public LimitInstanceCountAttribute()
        {
            this.count = 1;
        }
    }
    /// <summary>
    /// Specifies what thread the entity's tick method will be called on
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class TickThreadAttribute : Attribute
    {
        // TODO: This function
        public string name { get; protected set; }
        public TickThreadAttribute(string name)
        {
            this.name = name;
        }
    }

}