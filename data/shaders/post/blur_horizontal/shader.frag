
#version 450
#extension GL_EXT_samplerless_texture_functions : require
layout(location = 0) in vec4 fsin_UV;
layout(set = 0, binding = 1) uniform texture2D SurfaceTexture;
layout(set = 0, binding = 2) uniform sampler SurfaceSampler;

layout(location = 0) out vec4 fsout_Color;
//---------------------------------------------------------------------------
void main()
{
    vec2 pos = fsin_UV.xy * 2 - 1;
    vec2 size = textureSize(SurfaceTexture, 0);
    float r = length(size) / 200;
    float xs = size.x;
    float ys = size.y;
    float x,y,rr=r*r,d,w,w0;
    vec2 p=0.5*(vec2(1.0,1.0)+pos);
    vec4 col=vec4(0.0,0.0,0.0,0.0);
    w0=0.5135/pow(r,0.96);
    for (d=1.0/xs,x=-r,p.x+=x*d;x<=r;x++,p.x+=d){ w=w0*exp((-x*x)/(2.0*rr)); col+=texture(sampler2D(SurfaceTexture, SurfaceSampler),p)*w; }
    // for (d=1.0/ys,y=-r,p.y+=y*d;y<=r;y++,p.y+=d){ w=w0*exp((-y*y)/(2.0*rr)); col+=texture(sampler2D(SurfaceTexture, SurfaceSampler),p)*w; }
    fsout_Color=col;
}