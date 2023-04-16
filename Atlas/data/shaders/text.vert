#version 450
layout(set = 0, binding = 0) uniform TransformMatrices
{
    mat4 Matrix;
    mat4 Transform;
    mat4 Camera;
    vec4 Offset;
};
layout(location = 0) in vec3 Position;
layout(location = 1) in uint Color;
layout(location = 2) in vec2 UV;
layout(location = 0) out vec2 fsin_UV;

void main()
{
    gl_Position = vec4((Position.x - Offset.x) / 200.0 , (-Position.y - Offset.y) / 200.0, 0, 1) * Transform * Camera * Matrix;
    fsin_UV = UV;
}  