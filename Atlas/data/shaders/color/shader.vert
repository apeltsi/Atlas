#version 450
layout(set = 0, binding = 0) uniform TransformMatrices
{
    mat4 Matrix;
    mat4 Transform;
    mat4 Camera;
};

layout(location = 0) in vec2 Position;

void main()
{
    gl_Position = vec4(Position, 0, 1) * Transform * Camera * Matrix;
} 