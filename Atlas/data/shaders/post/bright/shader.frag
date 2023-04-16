
#version 450

layout(location = 0) in vec4 fsin_UV;
layout(set = 0, binding = 1) uniform texture2D SurfaceTexture;
layout(set = 0, binding = 2) uniform sampler SurfaceSampler;

layout(location = 0) out vec4 fsout_Color;

void main()
{
    vec4 color = texture(sampler2D(SurfaceTexture, SurfaceSampler), fsin_UV.xy);
    float brightness = dot(color.rgb * color.a, vec3(0.2126, 0.7152, 0.0722));
    if(brightness < 0.9) { // this equals (0.75, 0.75, 0.75)
        discard;
    }
    color -= vec4(0.75, 0.75, 0.75, 0);
    color *= 2;
    color.a = 1;
    fsout_Color = color;
}