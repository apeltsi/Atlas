using SolidCode.Caerus.Components;
using SolidCode.Caerus.ECS;

class DefaultComponent : Component
{
    int fixedUpdates = 0;
    public override void Start()
    {
        if (entity == null) return;
        Debug.Log("Hello my name is " + entity.name);
        Transform? t = entity.GetComponent<Transform>();
        if (t == null) return;
        Debug.Log("and im at " + t.position);
    }

    public override void Update()
    {
        Debug.Log("Update time!");
    }
    public override void FixedUpdate()
    {

    }

}