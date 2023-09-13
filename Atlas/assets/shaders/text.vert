#version 450
layout(set = 0, binding = 0) uniform TransformMatrices
{
    mat4 Matrix;
    mat4 Transform;
    mat4 Camera;
};
layout(location = 0) in vec3 Position;
layout(location = 1) in uint Color;
layout(location = 2) in vec2 UV;
layout(location = 0) out vec2 fsin_UV;

void main()
{
    // Lets remove the scaling from the transform matrix, while preserving the rotation and translation
    mat4 translate_rotate = Transform;
    translate_rotate[0][0] = 1.0;
    translate_rotate[1][1] = 1.0;
    translate_rotate[2][2] = 1.0;
    gl_Position = vec4(Position.x / 20000.0 , -Position.y / 20000.0, 0, 1) * translate_rotate * Camera * Matrix;
    fsin_UV = UV;
}  