#pragma exclude_renderers d3d11_9x
#pragma exclude_renderers d3d9
#define EDGE_STEPS 1, 1.5, 2, 2, 2, 2, 2, 2, 2, 4
// Very heavily based on https://catlikecoding.com/unity/tutorials/advanced-rendering/fxaa

static const float edgeSteps[10] = {EDGE_STEPS};

struct vertex_info
{
    float4 Position : POSITION;
    float4 UV : TEXCOORD0;
};

[[vk::binding(0, 0)]]
Texture2D Tex0;
[[vk::binding(1, 0)]]
SamplerState Sampler;

[[vk::binding(0, 1)]]
cbuffer FXAAUniform
{
    float2 TexelSize;
    float ContrastThreshold;
    float RelativeThreshold;
    int EdgeStepCount;
    int EdgeGuess;
    float SubpixelBlending;
    float Extra;
};

struct vertex_to_pixel
{
    float4 position : SV_Position;
    float2 uv : TEXCOORD0;
};

vertex_to_pixel vert(in vertex_info IN)
{
    vertex_to_pixel OUT;

    OUT.position = float4(IN.Position.xy, 0, 1);
    OUT.uv = IN.UV.xy;

    return OUT;
};

float4 Sample(float2 uv)
{
    return Tex0.Sample(Sampler, uv);
}

float SampleLuminance(float2 uv)
{
    return Sample(uv).a;
}

float SampleLuminance(float2 uv, float uOffset, float vOffset)
{
    uv += TexelSize * float2(uOffset, vOffset);
    return SampleLuminance(uv);
}

struct LuminanceData
{
    float m, n, e, s, w;
    float ne, nw, se, sw;
    float highest, lowest, contrast;
};

LuminanceData SampleLuminanceNeighborhood(float2 uv, half4 center)
{
    LuminanceData l;
    l.m = center.a;
    l.n = SampleLuminance(uv, 0, 1);
    l.e = SampleLuminance(uv, 1, 0);
    l.s = SampleLuminance(uv, 0, -1);
    l.w = SampleLuminance(uv, -1, 0);

    l.ne = SampleLuminance(uv, 1, 1);
    l.nw = SampleLuminance(uv, -1, 1);
    l.se = SampleLuminance(uv, 1, -1);
    l.sw = SampleLuminance(uv, -1, -1);

    l.highest = max(max(max(max(l.n, l.e), l.s), l.w), l.m);
    l.lowest = min(min(min(min(l.n, l.e), l.s), l.w), l.m);
    l.contrast = l.highest - l.lowest;
    return l;
}

float DeterminePixelBlendFactor(LuminanceData l)
{
    float filter = 2 * (l.n + l.e + l.s + l.w);
    filter += l.ne + l.nw + l.se + l.sw;
    filter *= 1.0 / 12;
    filter = abs(filter - l.m);
    filter = saturate(filter / l.contrast);

    float blendFactor = smoothstep(0, 1, filter);
    return blendFactor * blendFactor * SubpixelBlending;
}

bool ShouldSkipPixel(LuminanceData l)
{
    float threshold =
        max(ContrastThreshold, RelativeThreshold * l.highest);
    return l.contrast < threshold;
}

struct EdgeData
{
    bool isHorizontal;
    float pixelStep;
    float oppositeLuminance, gradient;
};


EdgeData DetermineEdge(LuminanceData l)
{
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
    if (pGradient < nGradient)
    {
        e.pixelStep = -e.pixelStep;
        e.oppositeLuminance = nLuminance;
        e.gradient = nGradient;
    }
    else
    {
        e.oppositeLuminance = pLuminance;
        e.gradient = pGradient;
    }
    return e;
}

float DetermineEdgeBlendFactor(LuminanceData l, EdgeData e, float2 uv)
{
    float2 uvEdge = uv;
    float2 edgeStep;
    if (e.isHorizontal)
    {
        uvEdge.y += e.pixelStep * 0.5;
        edgeStep = float2(TexelSize.x, 0);
    }
    else
    {
        uvEdge.x += e.pixelStep * 0.5;
        edgeStep = float2(0, TexelSize.y);
    }

    float edgeLuminance = (l.m + e.oppositeLuminance) * 0.5;
    float gradientThreshold = e.gradient * 0.25;

    float2 puv = uvEdge + edgeStep * edgeSteps[0];
    float pLuminanceDelta = SampleLuminance(puv) - edgeLuminance;
    bool pAtEnd = abs(pLuminanceDelta) >= gradientThreshold;

    // Ive had enough, this is getting manually unrolled due to issues with the compiler
    // I've tried everything, but SPIRV-Cross & DXC don't want to cooperate with me, ive rewritten this shader 3 times now, this is the only way it works
    // I am truly sorry to whoever has to see this
    int pi = 1;
    if (pi < EdgeStepCount && !pAtEnd)
    {
        puv += edgeStep * edgeSteps[pi];
        pLuminanceDelta = SampleLuminance(puv) - edgeLuminance;
        pAtEnd = abs(pLuminanceDelta) >= gradientThreshold;
        pi++;
        if (pi < EdgeStepCount && !pAtEnd)
        {
            puv += edgeStep * edgeSteps[pi];
            pLuminanceDelta = SampleLuminance(puv) - edgeLuminance;
            pAtEnd = abs(pLuminanceDelta) >= gradientThreshold;
            pi++;
            if (pi < EdgeStepCount && !pAtEnd)
            {
                puv += edgeStep * edgeSteps[pi];
                pLuminanceDelta = SampleLuminance(puv) - edgeLuminance;
                pAtEnd = abs(pLuminanceDelta) >= gradientThreshold;
                pi++;
                if (pi < EdgeStepCount && !pAtEnd)
                {
                    puv += edgeStep * edgeSteps[pi];
                    pLuminanceDelta = SampleLuminance(puv) - edgeLuminance;
                    pAtEnd = abs(pLuminanceDelta) >= gradientThreshold;
                    pi++;
                    if (pi < EdgeStepCount && !pAtEnd)
                    {
                        puv += edgeStep * edgeSteps[pi];
                        pLuminanceDelta = SampleLuminance(puv) - edgeLuminance;
                        pAtEnd = abs(pLuminanceDelta) >= gradientThreshold;
                        pi++;
                        if (pi < EdgeStepCount && !pAtEnd)
                        {
                            puv += edgeStep * edgeSteps[pi];
                            pLuminanceDelta = SampleLuminance(puv) - edgeLuminance;
                            pAtEnd = abs(pLuminanceDelta) >= gradientThreshold;
                            pi++;
                            if (pi < EdgeStepCount && !pAtEnd)
                            {
                                puv += edgeStep * edgeSteps[pi];
                                pLuminanceDelta = SampleLuminance(puv) - edgeLuminance;
                                pAtEnd = abs(pLuminanceDelta) >= gradientThreshold;
                                pi++;
                                if (pi < EdgeStepCount && !pAtEnd)
                                {
                                    puv += edgeStep * edgeSteps[pi];
                                    pLuminanceDelta = SampleLuminance(puv) - edgeLuminance;
                                    pAtEnd = abs(pLuminanceDelta) >= gradientThreshold;
                                    pi++;
                                    if (pi < EdgeStepCount && !pAtEnd)
                                    {
                                        puv += edgeStep * edgeSteps[pi];
                                        pLuminanceDelta = SampleLuminance(puv) - edgeLuminance;
                                        pAtEnd = abs(pLuminanceDelta) >= gradientThreshold;
                                        pi++;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    if (!pAtEnd)
    {
        puv += edgeStep * EdgeGuess;
    }
    float2 nuv = uvEdge - edgeStep * edgeSteps[0];
    float nLuminanceDelta = SampleLuminance(nuv) - edgeLuminance;
    bool nAtEnd = abs(nLuminanceDelta) >= gradientThreshold;
    int ni = 1;

    // See note above
    if (ni < EdgeStepCount && !nAtEnd)
    {
        nuv -= edgeStep * edgeSteps[ni];
        nLuminanceDelta = SampleLuminance(nuv) - edgeLuminance;
        nAtEnd = abs(nLuminanceDelta) >= gradientThreshold;
        ni++;
        if (ni < EdgeStepCount && !nAtEnd)
        {
            nuv -= edgeStep * edgeSteps[ni];
            nLuminanceDelta = SampleLuminance(nuv) - edgeLuminance;
            nAtEnd = abs(nLuminanceDelta) >= gradientThreshold;
            ni++;
            if (ni < EdgeStepCount && !nAtEnd)
            {
                nuv -= edgeStep * edgeSteps[ni];
                nLuminanceDelta = SampleLuminance(nuv) - edgeLuminance;
                nAtEnd = abs(nLuminanceDelta) >= gradientThreshold;
                ni++;
                if (ni < EdgeStepCount && !nAtEnd)
                {
                    nuv -= edgeStep * edgeSteps[ni];
                    nLuminanceDelta = SampleLuminance(nuv) - edgeLuminance;
                    nAtEnd = abs(nLuminanceDelta) >= gradientThreshold;
                    ni++;
                    if (ni < EdgeStepCount && !nAtEnd)
                    {
                        nuv -= edgeStep * edgeSteps[ni];
                        nLuminanceDelta = SampleLuminance(nuv) - edgeLuminance;
                        nAtEnd = abs(nLuminanceDelta) >= gradientThreshold;
                        ni++;
                        if (ni < EdgeStepCount && !nAtEnd)
                        {
                            nuv -= edgeStep * edgeSteps[ni];
                            nLuminanceDelta = SampleLuminance(nuv) - edgeLuminance;
                            nAtEnd = abs(nLuminanceDelta) >= gradientThreshold;
                            ni++;
                            if (ni < EdgeStepCount && !nAtEnd)
                            {
                                nuv -= edgeStep * edgeSteps[ni];
                                nLuminanceDelta = SampleLuminance(nuv) - edgeLuminance;
                                nAtEnd = abs(nLuminanceDelta) >= gradientThreshold;
                                ni++;
                                if (ni < EdgeStepCount && !nAtEnd)
                                {
                                    nuv -= edgeStep * edgeSteps[ni];
                                    nLuminanceDelta = SampleLuminance(nuv) - edgeLuminance;
                                    nAtEnd = abs(nLuminanceDelta) >= gradientThreshold;
                                    ni++;
                                    if (ni < EdgeStepCount && !nAtEnd)
                                    {
                                        nuv -= edgeStep * edgeSteps[ni];
                                        nLuminanceDelta = SampleLuminance(nuv) - edgeLuminance;
                                        nAtEnd = abs(nLuminanceDelta) >= gradientThreshold;
                                        ni++;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    if (!nAtEnd)
    {
        nuv -= edgeStep * EdgeGuess;
    }

    float pDistance, nDistance;
    if (e.isHorizontal)
    {
        pDistance = puv.x - uv.x;
        nDistance = uv.x - nuv.x;
    }
    else
    {
        pDistance = puv.y - uv.y;
        nDistance = uv.y - nuv.y;
    }

    float shortestDistance;
    bool deltaSign;
    if (pDistance <= nDistance)
    {
        shortestDistance = pDistance;
        deltaSign = pLuminanceDelta >= 0;
    }
    else
    {
        shortestDistance = nDistance;
        deltaSign = nLuminanceDelta >= 0;
    }

    if (deltaSign == (l.m - edgeLuminance >= 0))
    {
        return 0;
    }
    return 0.5 - shortestDistance / (pDistance + nDistance);
}

half4 ApplyFXAA(float2 uv)
{
    half4 center = Sample(uv);
    LuminanceData l = SampleLuminanceNeighborhood(uv, center);
    if (ShouldSkipPixel(l))
    {
        return center;
    }
    float pixelBlend = DeterminePixelBlendFactor(l);
    EdgeData e = DetermineEdge(l);
    float edgeBlend = DetermineEdgeBlendFactor(l, e, uv);
    float finalBlend = max(pixelBlend, edgeBlend);
    if (e.isHorizontal)
    {
        uv.y += e.pixelStep * finalBlend;
    }
    else
    {
        uv.x += e.pixelStep * finalBlend;
    }
    return half4(Sample(uv).rgb, l.m);
}

half4 pixel(in vertex_to_pixel IN) : SV_TARGET
{
    return ApplyFXAA(IN.uv);
};
