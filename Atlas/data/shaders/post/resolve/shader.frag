#version 450
layout(location = 0) in vec4 fsin_UV;
layout(set = 0, binding = 0) uniform texture2D Color;

layout(set = 0, binding = 1) uniform sampler Sampler;

layout(location = 0) out vec4 fsout_Color;
void main()
{
    fsout_Color=texture(sampler2D(Color, Sampler), fsin_UV.xy);
}