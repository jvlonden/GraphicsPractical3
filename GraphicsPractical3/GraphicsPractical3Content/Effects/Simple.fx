//------------------------------------------- Defines -------------------------------------------

#define Pi 3.14159265
#define Num_Lights 10

//------------------------------------- Top Level Variables -------------------------------------

// Top level variables can and have to be set at runtime

// Positions of the lightsource and the camera
float3 Light,SpotlightPos1, CameraPosition, direction1;
// Matrices for 3D perspective projection 
float4x4 View, Projection, World, WorldNormal;
// Light Colors
float4 DiffuseColor, AmbientColor, SpecularColor,color1;
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

// Implement the Coloring using normals assignment here
float4 NormalColor(float3 normal)
{
	//copy the data to the output
	float4 output;
	output.xyz = normal	;
	output.w = 1.0;
	
	return output;
}

// Implement the Procedural texturing assignment here
float4 ProceduralColor(VertexShaderOutput input)
{
	//reverse the normal and save its values in iNormal
	float3 iNormal = -input.Normal;
	float width = 2;
	float height = 2;
	
	// if the sin of the Y and X coordinates are BOTH above or below 0
	if( (sin(Pi * input.PixelPosition.x / width) > 0 && sin(Pi * input.PixelPosition.y / height) > 0) || (sin(Pi * input.PixelPosition.x / width) <= 0 && sin(Pi * input.PixelPosition.y / height) <= 0) )
	{
		//color using the normal
		return NormalColor(input.Normal);	
	}
	else
	{
		//color using the inverse
		return NormalColor(iNormal);
	}
}

float4 Lighting(VertexShaderOutput input)
{
	float3 n = input.Normal;
	// the vector between the lightpoint and the position of the pixel
	float3 l = normalize(Light - input.PixelPosition);	
	// the vector between the eye and the .....
	float3 e = normalize(CameraPosition - input.PixelPosition);
	// the halfway vector
	float3 h = normalize(e + l);	
	
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
	float CookTorrance = D*F*G/EdotN;
	
	// the cosine of the angle between the normal and the directional light and clamp it between 0 and 1;
	float4 diffuse = DiffuseColor * saturate(LdotN);
	
	// the ambient light
	float4 ambient = AmbientColor * AmbientIntensity;
	
	// the specula light
	float4 specular = SpecularColor * CookTorrance * SpecularIntensity;
	
	// all the lighting
	return  ambient + diffuse + specular;
}

float4 LightingSpotlight(VertexShaderOutput input, float3 SpotlightPos, float3 direction, float4 color )//function for the pixel shader(spotlights)
{
    //calculate the vector between the lightpoint and the position of the pixel
	float3 lightDir = normalize(SpotlightPos - input.PixelPosition);
	
	//calculate the vector between the eye and the .....
	float3 viewDir = normalize(CameraPosition - input.PixelPosition);
    
    float3 dirToLight = normalize(SpotlightPos - CameraPosition);
    float coneDot = dot(-lightDir, direction);
    
    float diffuse;
    float4 ambient,result;
    
    float alpha, beta;
    alpha = 60.0;
    beta = 55.0;
    
    if(coneDot > radians(alpha))
    {
    //calculate the halfway vector	
	
	//calculate the cosine of the angle between the normal and the directional light and clamp it between 0 and 1;
	diffuse = saturate(mul(input.Normal, lightDir));
	
	//calculate the ambient light
	ambient = AmbientColor * AmbientIntensity;
    
    result = ambient + color * diffuse;
	
    
    }
    else if(coneDot < radians(alpha) && coneDot > radians(beta))
    {
    
	float fadeValue = (coneDot - radians(beta)) / (radians(alpha) - radians(beta));
	//calculate the cosine of the angle between the normal and the directional light and clamp it between 0 and 1;
	diffuse = saturate(mul(input.Normal, lightDir));
	
	//calculate the ambient light
	ambient = AmbientColor * AmbientIntensity;
    
    //combine all the lighting

    result = ambient + color * diffuse;
    result *= fadeValue;
    

    
        
    }
    
    
	


	return  result;

}

float4 MultipleLighting(VertexShaderOutput input)
{
	float4 color[Num_Lights], diffuse[Num_Lights];
	
	//calculate the vector between the lightpoint and the position of the pixel	
	
	float4 outputColor = float4(0,0,0,0);
	[loop]
	for(uint i = 0; i < 0; i++)
	{
		color[i].xyz = normalize(MLS[i].xyz - input.PixelPosition.xyz);
		diffuse[i].xyz = saturate(mul(input.Normal, color[i]));
		color[i].xyz = MLSDiffuseColors[i] * diffuse[i];
		color[i].w = 1.0;
		outputColor = outputColor + color[i];
	}

	//calculate the ambient light
	float4 ambient = AmbientColor * AmbientIntensity;
	//combine all the lighting
	return  ambient + outputColor;
}

//---------------------------------------- Technique: Simple ----------------------------------------

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
		color = Lighting(input);
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

//---------------------------------------- Technique: Spotlight ----------------------------------------

VertexShaderOutput SpotlightVertexShader(VertexShaderInput input)
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

float4 SpotlightPixelShader(VertexShaderOutput input) : COLOR0
{
	//scale of the textures
	float textureScale = 0.5;
	
	float4 color;
	if(true)
		color = LightingSpotlight(input,SpotlightPos1, direction1,color1);
	else
		color = tex2D(DiffuseTextureSampler, input.TexCoords / textureScale);

	return color;
}

technique Spotlight
{
	pass Pass0
	{
		VertexShader = compile vs_3_0 SpotlightVertexShader();
		PixelShader  = compile ps_3_0 SpotlightPixelShader();
	}
}

//---------------------------------------- Technique: Multiple Light Sources ----------------------------------------


VertexShaderOutput MLSVertexShader(VertexShaderInput input)
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

float4 MLSPixelShader(VertexShaderOutput input) : COLOR0
{
	//scale of the textures
	float textureScale = 0.5;
	
	float4 color = MultipleLighting(input);

	return color;
}

technique MultipleLightsSources
{
	pass Pass0
	{
		VertexShader = compile vs_2_0 MLSVertexShader();
		PixelShader  = compile ps_2_0 MLSPixelShader();
	}
}