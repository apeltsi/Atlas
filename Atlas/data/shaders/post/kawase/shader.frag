
#version 450
#extension GL_EXT_samplerless_texture_functions : require
layout(location = 0) in vec4 fsin_UV;
layout(set = 0, binding = 1) uniform texture2D Texture;
layout(set = 0, binding = 2) uniform sampler Sampler;

layout(location = 0) out vec4 fsout_Color;


vec4 KawaseBlur(int pixelOffset, vec2 texelSize)
{
    vec4 o = vec4(0.0, 0.0, 0.0, 0.0);

    o += texture(sampler2D(Texture, Sampler), fsin_UV.xy + (vec2(pixelOffset + 0.5, pixelOffset + 0.5) * texelSize)) * 0.25;
    o += texture(sampler2D(Texture, Sampler), fsin_UV.xy + (vec2(-pixelOffset - 0.5, pixelOffset + 0.5) * texelSize))* 0.25;
    o += texture(sampler2D(Texture, Sampler), fsin_UV.xy + (vec2(-pixelOffset - 0.5, -pixelOffset - 0.5) * texelSize)) * 0.25;
    o += texture(sampler2D(Texture, Sampler), fsin_UV.xy + (vec2(pixelOffset + 0.5, -pixelOffset - 0.5) * texelSize)) * 0.25;

    return o;
}

//---------------------------------------------------------------------------
void main()
{
    vec2 size = textureSize(Texture, 0);
    vec4 col = KawaseBlur(0, 1.0 / size);
    fsout_Color = col;
}


