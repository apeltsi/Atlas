#version 450
#extension GL_EXT_samplerless_texture_functions : require
layout(location = 0) in vec4 fsin_UV;
layout(set = 0, binding = 0) uniform texture2D Color;
layout(set = 0, binding = 1) uniform texture2D BloomColor;

layout(set = 0, binding = 2) uniform sampler Sampler;

layout(location = 0) out vec4 fsout_Color;
void main()
{
    fsout_Color=texture(sampler2D(Color, Sampler), fsin_UV.xy) + texture(sampler2D(BloomColor, Sampler), fsin_UV.xy);
}