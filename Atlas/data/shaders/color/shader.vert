#version 450
layout(set = 0, binding = 0) uniform TransformMatrices
{
    mat4 Matrix;
    mat4 Transform;
    mat4 Camera;
};

layout(location = 0) in vec2 Position;
layout(location = 1) in vec4 UV;
layout(location = 0) out vec4 fsin_UV;

void main()
{
    gl_Position = vec4(Position, 0, 1) * Transform * Camera * Matrix;
    fsin_UV = vec4(UV.x, UV.y, UV.z, UV.w);
} 