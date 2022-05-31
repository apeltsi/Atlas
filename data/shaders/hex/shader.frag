
#version 450
layout(set = 1, binding = 0) uniform ScreenSize
{
    vec4 WindowSize;
};
layout(location = 0) in vec4 fsin_Color;
layout(location = 0) out vec4 fsout_Color;

void main()
{
    float high = max(WindowSize.x, WindowSize.y);
    float a = gl_FragCoord.x / high;
    float b = gl_FragCoord.y / high;
    float radius = 0.01 * high / 1000.0;

    // Estimate the most likely hex and round to nearest values
    float x = 2.0/3.0*a/radius;
    float z = (1.0/3.0*sqrt(3.0)*b-1.0/3.0*a)/radius;
    float y = -x-z;

    float ix = round((floor(x-y)-floor(z-x))/3.0);
    float iy = round((floor(y-z)-floor(x-y))/3.0);
    float iz = round((floor(z-x)-floor(y-z))/3.0);

    // Adjust to flat coordinates on the offset numbering system
    vec2 corrected = vec2(ix, iz);
    corrected.x --;
    vec2 offset = vec2(corrected.x + 1.5, corrected.y + ceil(corrected.x/2.0) + 1.5);
    float val = sqrt(pow(offset.x / 10, 2) + pow(offset.y / 10, 2));
    fsout_Color = vec4(val, val, val, 0);
}