namespace SolidCode.Caerus.ECS
{
    /// <summary>Limits the allowed instance count of a component to 1. 
    /// The component cannot be added if a Entity if another instance of the component already exists in the ECS.
    /// </summary>
    public class SingleInstance : LimitInstanceCount
    {
        public SingleInstance()
        {
            this.count = 1;
        }
    }
    /// <summary>
    /// Limits the allowed instance count of a component to <c>count</c>. 
    /// The component cannot be added if a Entity if <c>count</c> of the component already exists in the ECS.
    /// </summary>
    public class LimitInstanceCount : Attribute
    {
        public int count { get; protected set; }
        public LimitInstanceCount(int count)
        {
            this.count = count;
        }
        public LimitInstanceCount()
        {
            this.count = 1;
        }
    }
}