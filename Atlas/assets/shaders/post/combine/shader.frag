#version 450
#extension GL_EXT_samplerless_texture_functions : require
layout(location = 0) in vec4 fsin_UV;
layout(set = 0, binding = 0) uniform texture2D Color;
layout(set = 0, binding = 1) uniform texture2D BloomColor;

layout(set = 0, binding = 2) uniform sampler Sampler;

layout(location = 0) out vec4 fsout_Color;
vec4 KawaseBlur(texture2D Texture, float pixelOffset, vec2 texelSize)
{
    vec4 o = vec4(0.0, 0.0, 0.0, 0.0);

    o += texture(sampler2D(Texture, Sampler), fsin_UV.xy + (vec2(pixelOffset + 0.5, pixelOffset + 0.5) * texelSize)) * 0.25;
    o += texture(sampler2D(Texture, Sampler), fsin_UV.xy + (vec2(-pixelOffset - 0.5, pixelOffset + 0.5) * texelSize))* 0.25;
    o += texture(sampler2D(Texture, Sampler), fsin_UV.xy + (vec2(-pixelOffset - 0.5, -pixelOffset - 0.5) * texelSize)) * 0.25;
    o += texture(sampler2D(Texture, Sampler), fsin_UV.xy + (vec2(pixelOffset + 0.5, -pixelOffset - 0.5) * texelSize)) * 0.25;

    return o;
}

void main()
{
    vec2 size = textureSize(Color, 0);
    fsout_Color=KawaseBlur(Color, 1, 1.0 / size) + KawaseBlur(BloomColor, 1, 1.0 / size);
}