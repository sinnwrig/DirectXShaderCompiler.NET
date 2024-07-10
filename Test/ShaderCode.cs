public static class ShaderCode
{
    public const string HlslCode = @"
struct VertexInput
{
    float2 Position : POSITION;
    float4 Color : COLOR0;
};

struct VertexOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
};


VertexOutput vertex(VertexInput input)
{
    VertexOutput output;
    output.Position = float4(input.Position, 0, 1);
    output.Color = input.Color;
    return output;
}

#define DO_SOMETHING(x) x * 10 + 4 - 8 + sqrt(x) / abs(x)


float4 pixel(VertexOutput input) : SV_Target
{
    float value = DO_SOMETHING(input.Color.r);

    float value2 = DO_SOMETHING(value);

    float value3 = DO_SOMETHING(value2);

    input.Color *= 10;

    input.Color /= 43.55;

    input.Color.g = value2;
    input.Color.b = value;
    input.Color.a = value3;

    return input.Color;
}
";
}