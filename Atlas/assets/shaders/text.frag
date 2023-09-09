
#version 450

layout(location = 0) in vec2 fsin_UV;
layout(set = 0, binding = 1) uniform texture2D SurfaceTexture;
layout(set = 0, binding = 2) uniform sampler SurfaceSampler;
layout(set = 0, binding = 3) uniform ColorUniform {
    vec4 Color;
};
layout(location = 0) out vec4 fsout_Color;

void main()
{
    fsout_Color = texture(sampler2D(SurfaceTexture, SurfaceSampler), fsin_UV) * Color;
}