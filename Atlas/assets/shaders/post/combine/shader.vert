#version 450
layout(location = 0) in vec4 Position;
layout(location = 1) in vec4 UV;
layout(location = 0) out vec4 fsin_UV;

void main()
{
    gl_Position = vec4(Position.xy, 0, 1);
    fsin_UV = UV;
} 