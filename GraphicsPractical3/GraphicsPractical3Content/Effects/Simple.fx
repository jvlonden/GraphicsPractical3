//------------------------------------------- Defines -------------------------------------------

#define Pi 3.14159265
#define Num_Lights 4

//------------------------------------- Top Level Variables -------------------------------------

// Positions of the lightsource and the camera
float3 Light, SpotlightPos1, CameraPosition, SpotDirection1;
// Matrices for 3D perspective projection 
float4x4 View, Projection, World, WorldNormal;
// Light Colors
float4 DiffuseColor, AmbientColor, SpecularColor, SpotColor1;
// Other lighting values
float AmbientIntensity, SpecularPower, SpecularIntensity;
// Cook-Torrance
float Roughness, ReflectionCoefficient;
// Multiple Light Sources
float3 MLS[Num_Lights];
float4 MLSDiffuseColors[Num_Lights];


Texture DiffuseTexture;
sampler DiffuseTextureSampler = sampler_state 
{
	texture = <DiffuseTexture>;
	AddressU = mirror;
	AddressV = mirror;
};
bool HasTexture;

//---------------------------------- Input / Output structures ----------------------------------

struct VertexShaderInput
{
	float4 Position3D : POSITION0;
	float3 Normal : NORMAL0;
	float2 TexCoords : TEXCOORD0;
};

struct VertexShaderOutput
{
	float4 Position2D : POSITION0;
	float3 Normal : TEXCOORD1;
	float3 PixelPosition : TEXCOORD2;	
	float2 TexCoords : TEXCOORD0;
};

//------------------------------------------ Functions ------------------------------------------

float CookTorrance(float3 e, float3 l, float3 n, float3 h)
{
	// some variables to make the final formulas clearer
	float HdotN = dot(h, n);
	float EdotN = dot(e, n);
	float EdotH = dot(e, h);
	float LdotN = dot(l, n);
	float a = acos(HdotN);
	float tan2a = pow(tan(a), 2);
	float cos4a = pow(HdotN, 4);
	float m2 = Roughness * Roughness;
	
	// distribution
	float D = exp(-tan2a/m2)/(Pi * m2 * cos4a);	
	// reflection
	float F = ReflectionCoefficient + (1 - ReflectionCoefficient)*pow((1  - EdotH), 5);
	// attenuation
	float G = min(2*HdotN*EdotN/EdotH,
			  min(2*HdotN*LdotN/EdotH,
			  1));
	// the specular light	  
	return D*F*G/EdotN;
}

float4 Lighting(VertexShaderOutput input, float3 lightPos)
{
	float3 n = input.Normal;
	// the vector between the lightpoint and the position of the pixel
	float3 l = normalize(lightPos - input.PixelPosition);	
	// the vector between the eye and the .....
	float3 e = normalize(CameraPosition - input.PixelPosition);
	// the halfway vector
	float3 h = normalize(e + l);	
	
	// the cosine of the angle between the normal and the directional light and clamp it between 0 and 1;
	float4 diffuse = DiffuseColor * saturate(dot(l, n));
	
	// the ambient light
	float4 ambient = AmbientColor * AmbientIntensity;
	
	// the specula light
	float4 specular = SpecularColor * CookTorrance(e, l, n, h) * SpecularIntensity;
	
	// all the lighting
	return  ambient + diffuse + specular;
}

float4 LightingSpotlight(VertexShaderOutput input, float3 SpotlightPos, float3 SpotlightDirection, float4 SpotlightColor, float4 ObjectColor )
{
	// the vector between the lightpoint and the position of the pixel
	float3 lightDir = normalize(SpotlightPos - input.PixelPosition);	
	float3 n = input.Normal;
	// the vector between the eye and the .....
	float3 e = normalize(CameraPosition - input.PixelPosition);
	// the halfway vector
	float3 h = normalize(e + lightDir);

    float coneDot = dot(-lightDir, SpotlightDirection);
    
    float4 diffuse = (SpotlightColor + ObjectColor) / 2 * saturate(dot(input.Normal, lightDir));
	float4 specular = SpotlightColor * CookTorrance(e, lightDir, n, h) * SpecularIntensity;
    
    float alpha, beta;
    alpha = 60.0;
    beta = 55.0;
    
    if(coneDot > radians(alpha))
		return diffuse + specular;

    else if(coneDot < radians(alpha) && coneDot > radians(beta))
    {    
		float fadeValue = (coneDot - radians(beta)) / (radians(alpha) - radians(beta));

		return (diffuse + specular) * fadeValue;
	}
	else
		return float4(0.0f, 0.0f, 0.0f, 1.0f);
}

float4 MultipleLighting(VertexShaderOutput input)
{
	float4 color[Num_Lights], diffuse[Num_Lights];
	

	
	float4 outputColor = float4(0,0,0,0);
	[loop]
	for(uint i = 0; i < Num_Lights; i++)
	{
		outputColor = outputColor + LightingSpotlight (input, MLS[i], float3(0,-1,0), MLSDiffuseColors[i], DiffuseColor);
	}


	return  outputColor;
}

//---------------------------------------- Technique: Simple ------------------------------------

VertexShaderOutput SimpleVertexShader(VertexShaderInput input)
{
	// Allocate an empty output struct
	VertexShaderOutput output = (VertexShaderOutput)0;

	// Do the matrix multiplications for perspective projection and the world transform
	float4 worldPosition = mul(input.Position3D, World);
    float4 viewPosition  = mul(worldPosition, View);
	output.Position2D    = mul(viewPosition, Projection);
	
	output.TexCoords = input.TexCoords;
	
	//perform the transposed inverse world transform on the normals
	output.Normal		 = normalize(mul(input.Normal, (float3x3)WorldNormal));
	
	//pass along the position data to the output (for pixel shader use)
	output.PixelPosition = worldPosition;
	
	return output;
}

float4 SimplePixelShader(VertexShaderOutput input) : COLOR0
{
	//scale of the textures
	float textureScale = 0.5;
	
	float4 color;
	if(!HasTexture)
		color = Lighting(input, Light);
	else
		color = tex2D(DiffuseTextureSampler, input.TexCoords / textureScale);

	return color;
}

technique Simple
{
	pass Pass0
	{
		VertexShader = compile vs_3_0 SimpleVertexShader();
		PixelShader  = compile ps_3_0 SimplePixelShader();
	}
}

//---------------------------------------- Technique: Spotlight --------------------------------

float4 SpotlightPixelShader(VertexShaderOutput input) : COLOR0
{
	//scale of the textures
	float textureScale = 0.5;

	float4 color;
	if(!HasTexture)
		color = LightingSpotlight(input, SpotlightPos1, SpotDirection1, SpotColor1, DiffuseColor);
	else
		color = LightingSpotlight(input, SpotlightPos1, SpotDirection1, SpotColor1, tex2D(DiffuseTextureSampler, input.TexCoords / textureScale));

	return color;
}

technique Spotlight
{
	pass Pass0
	{
		VertexShader = compile vs_3_0 SimpleVertexShader();
		PixelShader  = compile ps_3_0 SpotlightPixelShader();
	}
}

//---------------------------------------- Technique: Multiple Light Sources ----------------------


float4 MLSPixelShader(VertexShaderOutput input) : COLOR0
{
	//scale of the textures
	float textureScale = 0.5;

	float4 color;
	if(!HasTexture)
		color = MultipleLighting(input);
	else
		color = tex2D(DiffuseTextureSampler, input.TexCoords / textureScale);

	return color;
}

technique MultipleLightsSources
{
	pass Pass0
	{
		VertexShader = compile vs_3_0 SimpleVertexShader();
		PixelShader  = compile ps_3_0 MLSPixelShader();
	} 
}

//---------------------------------------- Technique: GrayScale ----------------------------------------

float4 GrayScalePixelShader(VertexShaderOutput input) : COLOR0
{
	//scale of the textures
	float textureScale = 0.5;
	
	float4 color;
	if(!HasTexture)
		color = Lighting(input, Light);
	else
		color = tex2D(DiffuseTextureSampler, input.TexCoords / textureScale);
	
	return dot(color, float3(0.3, 0.59, 0.11));
}

technique GrayScale
{
	pass Pass0
	{
		VertexShader = compile vs_3_0 SimpleVertexShader();
		PixelShader  = compile ps_3_0 GrayScalePixelShader();
	}
}

