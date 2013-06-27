//texture

texture screen;

//texture sampler

sampler TextureSampler = sampler_state
{
    Texture = <screen>;
};

//technique Grayscale

//pixel shader for grayscale

float4 PixelShaderFunctionGS(float2 TextureCoordinate : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(TextureSampler, TextureCoordinate);
 
 
    return dot(color, float3(0.3, 0.59, 0.11));
}

technique Grayscale
{
	pass Pass1
	{
		PixelShader = compile ps_2_0 PixelShaderFunctionGS();
	}
}

//technique normal

float4 PixelShaderFunction(float2 TextureCoordinate : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(TextureSampler, TextureCoordinate);
 
 
    return color;
}

technique normal
{
	pass Pass1
	{
		PixelShader = compile ps_2_0 PixelShaderFunction();
	}
}
