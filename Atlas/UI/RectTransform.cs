﻿using System.Numerics;
using SolidCode.Atlas.ECS;

namespace SolidCode.Atlas.UI;

public class RectTransform : Transform
{
    private AnchoredPoint _position = new(new Vector2(0f, 0f));

    private RelativeVector _scale = new(0f, 0f);

    public new AnchoredPoint Position
    {
        get => _position;
        set
        {
            _position = value;
            var rt = Entity?.Parent.GetComponent<RectTransform>();
            if (rt != null) _position.Parent = rt;
        }
    }

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
        Position = _position;
        Scale = _scale;
    }

    public override Matrix4x4 GetTransformationMatrix()
    {
        Position = _position;
        Scale = _scale;
        // Set parents
        var rt = Entity?.Parent.GetComponent<RectTransform>();
        _position.Parent = rt;
        _scale.Parent = rt?.Scale;


        var pos = Position.Evaluate();
        var scale = Scale.Evaluate();
        var rot = GlobalRotation * ((float)Math.PI / 180f);
        var scaleMat = new Matrix4x4(
            scale.X, 0, 0, 0,
            0, scale.Y, 0, 0,
            0, 0, 1, 0,
            0, 0, 0, 1
        );
        var rotation = new Matrix4x4(
            (float)Math.Cos(rot), (float)-Math.Sin(rot), 0, 0,
            (float)Math.Sin(rot), (float)Math.Cos(rot), 0, 0,
            0, 0, 1, 0,
            0, 0, 0, 1);
        var translation = new Matrix4x4(
            1, 0, 0, pos.X,
            0, 1, 0, pos.Y,
            0, 0, 1, 0,
            0, 0, 0, 1);

        return translation * rotation * scaleMat;
    }
}