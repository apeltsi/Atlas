#version 450

layout(location = 0) in vec2 Position;
layout(location = 1) in vec4 Color;
layout(std140) uniform TransformMatrices
{
    mat4 Matrix;
};

layout(location = 0) out vec4 fsin_Color;

void main()
{
    gl_Position = vec4(Position, 0, 1) * Matrix;
    fsin_Color = Color;
} 