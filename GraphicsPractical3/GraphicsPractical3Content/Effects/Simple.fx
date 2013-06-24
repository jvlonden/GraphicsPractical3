//------------------------------------------- Defines -------------------------------------------

#define Pi 3.14159265

//------------------------------------- Top Level Variables -------------------------------------

// Top level variables can and have to be set at runtime

// Positions of the lightsource and the camera
float3 Light, CameraPosition;
// Matrices for 3D perspective projection 
float4x4 View, Projection, World, WorldNormal;
// Light Colors
float4 DiffuseColor, AmbientColor, SpecularColor;
// Other lighting values
float AmbientIntensity, SpecularPower, SpecularIntensity;

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
	//calculate the vector between the lightpoint and the position of the pixel
	float3 lightDir = normalize(Light - input.PixelPosition);
	
	//calculate the vector between the eye and the .....
	float3 viewDir = normalize(CameraPosition - input.PixelPosition);
	//calculate the halfway vector
	float3 h = normalize(viewDir + lightDir);	
	
	//calculate the cosine of the angle between the normal and the directional light and clamp it between 0 and 1;
	float diffuse = saturate(mul(input.Normal, lightDir));
	
	//calculate the ambient light
	float4 ambient = AmbientColor * AmbientIntensity;
	
	//calculate the intensity of the specular light at a given point
	float4 specular = pow(saturate(mul(input.Normal, h)), SpecularPower) * SpecularIntensity;
	
	//combine all the lighting
	return  ambient + DiffuseColor * diffuse + SpecularColor * specular;
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
		VertexShader = compile vs_2_0 SimpleVertexShader();
		PixelShader  = compile ps_2_0 SimplePixelShader();
	}
}