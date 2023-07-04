
#version 450

layout(location = 0) in vec4 fsin_UV;
layout(set = 1, binding = 0) uniform ColorUniform
{
    vec4 Color;
};

layout(location = 0) out vec4 fsout_Color;

void main()
{
    fsout_Color = Color;
}