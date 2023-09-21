#version 450
layout(set = 0, binding = 0) uniform TransformMatrices
{
    mat4 Matrix;
    mat4 Transform;
    mat4 Camera;
};
layout(location = 0) in vec2 Position;
layout(location = 1) in vec4 UV;
layout(location = 2) in vec2 InstancePosition;
layout(location = 3) in float InstanceRotation;
layout(location = 4) in vec2 InstanceScale;
layout(location = 5) in vec4 InstanceColor;
layout(location = 0) out vec4 fsin_UV;
layout(location = 1) out vec4 fsin_Color;
void main()
{
    mat4 rotMat = mat4(
    cos(InstanceRotation), -sin(InstanceRotation), 0, 0,
    sin(InstanceRotation), cos(InstanceRotation), 0, 0,
    0, 0, 1, 0,
    0, 0, 0, 1
    );
    vec4 rotatedPos = vec4(Position, 0, 1) * rotMat;
    gl_Position = vec4((rotatedPos.xy * InstanceScale + InstancePosition), 0, 1) * Transform * Camera * Matrix;
    fsin_UV = vec4(UV.x, UV.y, UV.z, UV.w);
    fsin_Color = InstanceColor;
} 
