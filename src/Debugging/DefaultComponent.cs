using System.Numerics;
using SolidCode.Caerus;
using SolidCode.Caerus.Components;
using SolidCode.Caerus.ECS;
using SolidCode.Caerus.Input;
using SolidCode.Caerus.Rendering;

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
        t.position = new Vector2(0f, 0f);
    }
    float time = 0f;
    int frames = 0;
    public override void Update()
    {

        Transform? t = entity.GetComponent<Transform>();
        if (t == null) return;
        t.rotation = 0.001f;
        t.scale *= 1.0001f;
        if (InputManager.GetKey(Veldrid.Key.A))
        {
            t.position += new Vector2(-0.01f, 0f);
        }
    }
    public override void FixedUpdate()
    {

    }

}