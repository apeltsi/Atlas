using System.Numerics;
using SolidCode.Atlas.ECS;

namespace SolidCode.Atlas.UI;

public class RectTransform : Transform
{
    private AnchoredPoint _position = new AnchoredPoint(new RelativeVector(0f, 0f));

    public new AnchoredPoint Position
    {
        get => _position;
        set
        {
            _position = value;
            RectTransform rt = Entity?.Parent.GetComponent<RectTransform>()!;
            if (rt != null)
            {
                _position.Position.Parent = rt.Position.Position;
                _position.Parent = rt;
            }
        }
    }
    
    private RelativeVector _scale = new RelativeVector(0f, 0f);
    public new RelativeVector Scale
    {
        get => _scale;
        set
        {
            _scale = value;
            _scale.Parent = Entity?.Parent.GetComponent<RectTransform>()?.Scale;
        }
    }

    public void Start()
    {
        this.Position = _position;
        this.Scale = _scale;
    }
    
    public override Matrix4x4 GetTransformationMatrix()
    {
        this.Position = _position;
        this.Scale = _scale;

        Vector2 pos = this.Position.Evaluate();
        Vector2 scale = this.Scale.Evaluate();
        float rot = this.GlobalRotation * ((float)Math.PI / 180f);
        Matrix4x4 scaleMat = new Matrix4x4(
            scale.X, 0, 0, 0,
            0, scale.Y, 0, 0,
            0, 0, 1, 0,
            0, 0, 0, 1
        );
        Matrix4x4 rotation = new Matrix4x4(
            (float)Math.Cos(rot), (float)-Math.Sin(rot), 0, 0,
            (float)Math.Sin(rot), (float)Math.Cos(rot), 0, 0,
            0, 0, 1, 0,
            0, 0, 0, 1);
        Matrix4x4 translation = new Matrix4x4(
            1, 0, 0, pos.X,
            0, 1, 0, pos.Y,
            0, 0, 1, 0,
            0, 0, 0, 1);

        return translation * rotation * scaleMat;
    }

}