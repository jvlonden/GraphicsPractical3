//------------------------------------------- Defines -------------------------------------------

#define Pi 3.14159265
#define MAX_LIGHTS 5

//------------------------------------- Top Level Variables -------------------------------------

// Positions of the lightsource and the camera
float3 Light, CameraPosition;
// Matrices for 3D perspective projection 
float4x4 View, Projection, World, WorldNormal;
// Light Colors
float4 DiffuseColor, AmbientColor, SpecularColor, SpotColSpotPos[MAX_LIGHTS];
// Other lighting values
float AmbientIntensity, SpecularPower, SpecularIntensity;
// Cook-Torrance
float Roughness, ReflectionCoefficient;
// Spotlights
float3 SpotPos[MAX_LIGHTS], SpotDir[MAX_LIGHTS];
float4 SpotCol[MAX_LIGHTS];
// Texture
Texture DiffuseTexture;
bool HasTexture;

sampler DiffuseTextureSampler = sampler_state 
{
	texture = <DiffuseTexture>;
	AddressU = mirror;
	AddressV = mirror;
};

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

float4 CookTorrance(float3 e, float3 l, float3 n, float3 h, float4 color, float4 intensity)
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
	return color * D*F*G/EdotN * intensity;
}

float4 Diffuse(float3 l, float3 n, float4 color)
{
	// the cosine of the angle between the normal and the directional light and clamp it between 0 and 1;
	return color * saturate(dot(l, n));
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
	
	// the ambient light
	float4 ambient = AmbientColor * AmbientIntensity;
	
	// all the lighting
	return  ambient + Diffuse(l, n, DiffuseColor) + CookTorrance(e, l, n, h, SpecularColor, SpecularIntensity);
}

float4 LightingSpotlight(VertexShaderOutput input, float3 SpotlightPos, float3 SpotlightDirection, float4 SpotlightColor, float4 DiffuseColor )
{
	// the vector between the lightpoint and the position of the pixel
	float3 l = normalize(SpotlightPos - input.PixelPosition);	
	float3 n = input.Normal;
	// the vector between the eye and the .....
	float3 e = normalize(CameraPosition - input.PixelPosition);
	// the halfway vector
	float3 h = normalize(e + l);

    float coneDot = dot(-l, SpotlightDirection);
    
    float4 diffuse = Diffuse(l, n, DiffuseColor * SpotlightColor);
	float4 specular = CookTorrance(e, l, n, h, SpotlightColor, SpecularIntensity);
    
    float alpha = radians(60.0);
    float beta = radians(55.0);
    
    if(coneDot > alpha)
		return diffuse + specular;

    else if(coneDot < alpha && coneDot > beta)
    {    
		float fadeValue = (coneDot - beta) / (alpha - beta);

		return float4((float3)((diffuse + specular) * fadeValue), 1.0f);
	}
	else
		return float4(0.0f, 0.0f, 0.0f, 1.0f);
}

float4 MultipleSpotlights(VertexShaderOutput input, float4 DiffuseColor)
{	
	float3 n = input.Normal;
	// the vector between the eye and the .....
	float3 e = normalize(CameraPosition - input.PixelPosition);

    float alpha = radians(60.0);
    float beta = radians(55.0);
    
	float4 cDiffuse = float4(0.0f, 0.0f, 0.0f, 1.0f);
	float4 cSpecular = float4(0.0f, 0.0f, 0.0f, 1.0f);
	float numOfLights = 0.0f;

	for(int i = 0; i < MAX_LIGHTS; i++)
	{
		// the vector between the spotlight and the position of the pixel
		float3 l = normalize(SpotPos[i] - input.PixelPosition);	
		// the halfway vector
		float3 h = normalize(e + l); 
		float coneDot = dot(-l, SpotDir[i]); 
		float4 diffuse = Diffuse(l, n, SpotCol[i]);
		float4 specular = CookTorrance(e, l, n, h, SpotCol[i], SpecularIntensity);

		if(coneDot > alpha)
		{
			cDiffuse += diffuse;
			cSpecular += specular;

			numOfLights += 1;
		}

		else if(coneDot < alpha && coneDot > beta)
		{    
			float fade = (coneDot - beta) / (alpha - beta);

			cDiffuse += float4((float3)diffuse * fade, 1.0f);
			cSpecular += float4((float3)specular * fade, 1.0f);
			
			numOfLights += fade;
		}
	}
	if(numOfLights > 1)
	{
		cDiffuse = cDiffuse / numOfLights;
		cSpecular = cSpecular / numOfLights;
	}

	return DiffuseColor * cDiffuse + cSpecular;
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
		color = LightingSpotlight(input, SpotPos[0], SpotDir[0], SpotCol[0], DiffuseColor);
	else
		color = LightingSpotlight(input, SpotPos[0], SpotDir[0], SpotCol[0], tex2D(DiffuseTextureSampler, input.TexCoords / textureScale));

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
		color = MultipleSpotlights(input, DiffuseColor);
	else
		color = MultipleSpotlights(input, tex2D(DiffuseTextureSampler, input.TexCoords / textureScale));

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



