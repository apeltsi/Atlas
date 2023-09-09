#version 450

layout(location = 0) in vec4 fsin_UV;
layout(location = 1) in vec4 fsin_Color;
layout(set = 1, binding = 0) uniform ColorUniform
{
    vec4 Color;
};

layout(set = 0, binding = 1) uniform texture2D SurfaceTexture;
layout(set = 0, binding = 2) uniform sampler SurfaceSampler;

layout(location = 0) out vec4 fsout_Color;

void main()
{
    fsout_Color = texture(sampler2D(SurfaceTexture, SurfaceSampler), fsin_UV.xy) * Color * fsin_Color;
}