
#version 450

layout(location = 0) in vec4 fsin_UV;
layout(set = 0, binding = 0) uniform texture2D SurfaceTexture;
layout(set = 0, binding = 1) uniform sampler SurfaceSampler;

layout(location = 0) out vec4 fsout_Color;

void main()
{
    vec4 color = texture(sampler2D(SurfaceTexture, SurfaceSampler), fsin_UV.xy);
    float brightness = dot(color.rgb * color.a, vec3(0.2126, 0.7152, 0.0722));
    if(brightness < 0.7) {
        discard;
    }
    color *= color.a;
    color -= vec4(0.5, 0.5, 0.5, 0);
    color.a = 1;
    fsout_Color = color;
}