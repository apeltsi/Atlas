using System.Numerics;
using SolidCode.Caerus;
using SolidCode.Caerus.Components;
using SolidCode.Caerus.ECS;

class DefaultComponent : Component
{
    public override void Start()
    {
        if (entity == null) return;
        Debug.Log("Hello my name is " + entity.name);
        Transform? t = entity.GetComponent<Transform>();
        if (t == null) return;
        Debug.Log("and im at " + t.position);
        t.position = new Vector2(0.1f, 0.2f);
    }

    public override void Update()
    {
        Transform? t = entity.GetComponent<Transform>();
        if (t == null) return;
        t.position += new Vector2(0.0001f, 0.0001f);
        t.scale += new Vector2(0.0025f, 0.0025f);
    }
    public override void FixedUpdate()
    {

    }

}