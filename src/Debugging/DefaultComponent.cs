using System.Numerics;
using SolidCode.Caerus;
using SolidCode.Caerus.Components;
using SolidCode.Caerus.ECS;
using SolidCode.Caerus.Input;

class DefaultComponent : Component
{
    bool dir = true;
    public override void Start()
    {
        if (entity == null) return;
        Debug.Log("Hello my name is " + entity.name);
        Transform? t = entity.GetComponent<Transform>();
        if (t == null) return;
        Debug.Log("and im at " + t.position);
        t.scale = new Vector2(1f, 1f);
    }

    public override void Update()
    {
        Transform? t = entity.GetComponent<Transform>();
        if (t == null) return;
        if (InputManager.GetKey(Veldrid.Key.A))
        {
            t.position += new Vector2(-5f, 0f);
        }
        if (InputManager.GetKey(Veldrid.Key.D))
        {
            t.position += new Vector2(5f, 0f);

        }
        if (dir)
        {
            t.scale *= new Vector2(1.04f, 1.04f);
        }
        else
        {
            t.scale *= new Vector2(0.96f, 0.96f);
        }
        if (t.scale.X > 2f)
        {
            dir = false;
        }
        else if (t.scale.X < 0.5f)
        {
            dir = true;
        }
    }
    public override void FixedUpdate()
    {

    }

}