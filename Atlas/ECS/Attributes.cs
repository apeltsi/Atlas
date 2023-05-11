namespace SolidCode.Atlas.ECS
{
    /// <summary>
    /// Limits the allowed instance count of a component to 1. 
    /// The component cannot be added if a Entity if another instance of the component already exists in the ECS.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class SingleInstanceAttribute : LimitInstanceCountAttribute
    {
        public SingleInstanceAttribute()
        {
            this.Count = 1;
        }
    }
    /// <summary>
    /// Limits the allowed instance count of a component to <c>count</c>. 
    /// The component cannot be added onto a Entity if <c>count</c> of the component already exists in the ECS.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class LimitInstanceCountAttribute : Attribute
    {
        public int Count { get; protected set; }
        public LimitInstanceCountAttribute(int count)
        {
            this.Count = count;
        }
        public LimitInstanceCountAttribute()
        {
            this.Count = 1;
        }
    }
    /// <summary>
    /// Specifies what thread the entity's tick method will be called on
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ECSThreadAttribute : Attribute
    {
        public string Name { get; protected set; }
        public ECSThreadAttribute(string name)
        {
            this.Name = name;
        }
    }

}