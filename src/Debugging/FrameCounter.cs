using System.Numerics;
using SolidCode.Caerus;
using SolidCode.Caerus.Components;
using SolidCode.Caerus.ECS;
using SolidCode.Caerus.Rendering;

class FrameCounter : Component
{
    TextRenderer tr;
    int frames = 0;
    float time = 0f;
    public override void Start()
    {
        if (entity == null) return;
        Debug.Log("Hello my name is " + entity.name);
        TextRenderer? textRenderer = entity.GetComponent<TextRenderer>();
        if (textRenderer == null) return;
        tr = textRenderer;
    }

    public override void Update()
    {
        time += Window.frameDeltaTime;
        if (frames >= 10)
        {
            frames = 0;
            tr.Text = "FPS: " + Math.Round(1f / (time / 10f));
            time = 0;
        }
        frames++;
    }
    public override void FixedUpdate()
    {
    }

}