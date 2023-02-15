using SolidCode.Atlas;
using SolidCode.Atlas.ECS;
using SolidCode.Atlas.Rendering;

class FrameCounter : Component
{
    TextRenderer tr;
    int frames = 0;
    double time = 0f;
    public override void Start()
    {
        TextRenderer? textRenderer = entity.GetComponent<TextRenderer>();
        if (textRenderer == null) return;
        tr = textRenderer;
    }

    public override void Update()
    {
        time += Time.deltaTime;
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