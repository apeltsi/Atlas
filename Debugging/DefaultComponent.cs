using SolidCode.Caerus.Components;
using SolidCode.Caerus.ECS;

class DefaultComponent : Component
{
    int fixedUpdates = 0;
    public override void Start()
    {
        Debug.Log("Hello my name is " + entity.name);
        Debug.Log("and im at " + entity.GetComponent<Transform>().position);
    }

    public override void Update()
    {
        Debug.Log("Update time!");
    }
    public override void FixedUpdate()
    {
        fixedUpdates++;
        if (fixedUpdates > 50)
        {
            Debug.Log("Heya");
            fixedUpdates = 0;
        }
    }

}