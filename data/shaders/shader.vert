#version 450
layout(set = 0, binding = 0) uniform TransformMatrices
{
    mat4 Matrix;
    mat4 Transform;

};

layout(location = 0) in vec2 Position;
layout(location = 1) in vec4 Color;
layout(location = 0) out vec4 fsin_Color;

void main()
{
    gl_Position = vec4(Position, 0, 1) * Matrix * Transform;
    fsin_Color = Color;
} 