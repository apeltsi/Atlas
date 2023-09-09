#version 450
layout(location = 0) in vec4 fsin_UV;
layout(set = 0, binding = 0) uniform texture2D Color;

layout(set = 0, binding = 1) uniform sampler Sampler;

layout(location = 0) out vec4 fsout_Color;
void main()
{
    vec4 s = texture(sampler2D(Color, Sampler), fsin_UV.xy);
    float luma = clamp(dot(s.rgb * s.a, vec3(0.2126, 0.7152, 0.0722)), 0.0, 1.0);
    fsout_Color = vec4(s.rgb,luma);
}