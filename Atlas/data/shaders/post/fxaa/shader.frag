#version 450
layout(location = 0) in vec4 fsin_UV;

layout(set = 0, binding = 0) uniform texture2D Color;

layout(set = 0, binding = 1) uniform sampler Sampler;
layout(set = 1, binding = 0) uniform TexelSizeUniform
{
    vec2 TexelSize;
    float ContrastThreshold;
    float RelativeThreshold;
};


layout(location = 0) out vec4 fsout_Color;

struct LuminanceData {
    float m;
    float n;
    float e;
    float s;
    float w;
    
    float ne;
    float nw;
    float se;
    float sw;
    
    float highest;
    float lowest;
    float contrast;
};

vec4 sampleTexture(vec2 uv) {
    vec4 color = texture(sampler2D(Color, Sampler), uv);
    return color;
}

float sampleLuminance(vec2 uv, float uOffset, float vOffset)
{
    uv += vec2(uOffset, vOffset) * TexelSize;
    return sampleTexture(uv).a;
}

LuminanceData sampleLuminanceNeighborhood (vec2 uv, vec4 center) {
    LuminanceData l = LuminanceData(
        center.a, // m
        sampleLuminance(uv, 0.0, 1.0), // n
        sampleLuminance(uv, 1.0, 0.0), // e
        sampleLuminance(uv, 0.0, -1.0), // s
        sampleLuminance(uv, -1.0, 0.0), // w
        
        sampleLuminance(uv, 1, 1), // ne
        sampleLuminance(uv, -1, 1), // nw
        sampleLuminance(uv, 1, -1), // se
        sampleLuminance(uv, -1, -1), // sw

        0,
        0,
        0); 
    
    
    l.highest = max(max(max(max(l.n, l.e), l.s), l.w), l.m);
    l.lowest = min(min(min(min(l.n, l.e), l.s), l.w), l.m);
    l.contrast = l.highest - l.lowest;
    return l;
}

bool ShouldSkipPixel (LuminanceData l) {
    float threshold =
    max(ContrastThreshold, RelativeThreshold * l.highest);
    return l.contrast < threshold;
}

float DeterminePixelBlendFactor (LuminanceData l) {
    float _filter = 2 * (l.n + l.e + l.s + l.w);
    _filter += l.ne + l.nw + l.se + l.sw;
    _filter *= 1.0 / 12;
    _filter = abs(_filter - l.m);
    _filter = clamp(_filter / l.contrast, 0.0, 1.0);
    float blendFactor = smoothstep(0.0, 1.0, _filter);
    return blendFactor * blendFactor;
}

struct EdgeData {
    bool isHorizontal;
    float pixelStep;
};
EdgeData DetermineEdge (LuminanceData l) {
    EdgeData e;
    float horizontal =
    abs(l.n + l.s - 2 * l.m) * 2 +
    abs(l.ne + l.se - 2 * l.e) +
    abs(l.nw + l.sw - 2 * l.w);
    float vertical =
    abs(l.e + l.w - 2 * l.m) * 2 +
    abs(l.ne + l.nw - 2 * l.n) +
    abs(l.se + l.sw - 2 * l.s);
    e.isHorizontal = horizontal >= vertical;
    
    float pLuminance = e.isHorizontal ? l.n : l.e;
    float nLuminance = e.isHorizontal ? l.s : l.w;
    float pGradient = abs(pLuminance - l.m);
    float nGradient = abs(nLuminance - l.m);
    e.pixelStep =
    e.isHorizontal ? TexelSize.y : TexelSize.x;

    if (pGradient < nGradient) {
        e.pixelStep = -e.pixelStep;
    }
    return e;
}


vec4 ApplyFXAA(vec2 uv) {
    vec4 center = sampleTexture(uv);
    LuminanceData luma = sampleLuminanceNeighborhood(uv, center);
    if(ShouldSkipPixel(luma)) {
        return center;
    }
    float pixelBlend = DeterminePixelBlendFactor(luma);
    EdgeData e = DetermineEdge(luma);
    if (e.isHorizontal) {
        uv.y += e.pixelStep * pixelBlend;
    }
    else {
        uv.x += e.pixelStep * pixelBlend;
    }
    return vec4(sampleTexture(uv).rgb, luma.m);
}

void main()
{
    fsout_Color = ApplyFXAA(fsin_UV.xy);
}