float4x4 World;
float4x4 WorldViewProjection;

float3 LightDirection;
float3 LightDiffuse;
float3 LightSpecular;
float3 LightAmbient;
float LightShininess;

float3 CameraPosition;

int Size;

int Level;
int Levels;
int BlockSize;
int NeighborLevels[4];
float NeighborBlendings[4];
float Blending;
float2 Offset;

float4 TerrainColor;

bool HeightmapEnabled;
bool TextureEnabled;
bool LightingEnabled;
bool GeomorphEnabled;
bool BruteForceEnabled;

float HeightOffset;
float Bumpiness;
float TextureRepeat;

texture2D Texture;
texture2D Heightmap;
texture2D Normalmap;

sampler2D HeightmapSampler = sampler_state { 
	texture = <Heightmap>; 
	MagFilter = point; 
	MinFilter = point; 
	MipFilter = point; 
	AddressU = clamp; 
	AddressV = clamp;
};

sampler2D TextureSampler = sampler_state { 
	texture = <Texture>; 
	MagFilter = linear; 
	MinFilter = linear; 
	MipFilter = linear; 
	AddressU = mirror; 
	AddressV = mirror;
};

sampler2D NormalmapSampler = sampler_state { 
	texture = <Normalmap>; 
	MagFilter = linear;
	MinFilter = linear; 
	MipFilter = linear; 
	AddressU = clamp; 
	AddressV = clamp;
};

struct VertexShaderInput
{
	float4 Position	: POSITION0;
};

struct VertexShaderOutput
{
	float4 Position		: POSITION0;
	float2 TexCoords		: TEXCOORD0;
	float2 NormalCoords	: TEXCOORD1;
	float3 View				: TEXCOORD2;
	float3 Light			: TEXCOORD3;
};

float GetHeight(float x, float y)
{
	return tex2Dlod(HeightmapSampler, float4(float2(x, y) / (Size - 1), 0, 0)).r;
}

float GetMorphing(float x, float y, float height, float level, float blending)
{
	float result = 0.0f;
	
	int delta = pow(2, level);
			
	int i = x / delta;
	int j = y / delta;
	
	int a = i % 2;
	int b = j % 2;
	
	if (level < Levels - 1 && (a != 0 || b != 0))
	{	
		if (level < Levels - 2 && a + b == 2 && (i % 4) + (j % 4) == 4)
			b = -b;
			
		a *= delta;
		b *= delta;

		float h0 = GetHeight(x - a, y - b);
		float h1 = GetHeight(x + a, y + b);
		
		delta = pow(2, Level);
		
		i = x - Offset.x;
		j = y - Offset.y;		
		
		if (level == Level && blending != Blending)
			blending = max(blending, Blending);
		
		result = (height - (h0 + h1) / 2.0f) * blending;
	}
	
	return result;
}

float GetMorphing(float x, float y, float height)
{	
	float result = 0.0f;
	
	int delta = pow(2, Level);
	
	int i = x - Offset.x;
	int j = y - Offset.y;
	
	int index = -1;
	
	if (j == BlockSize - 1 && NeighborLevels[0] >= Level)
		index = 0;
	else if (i == BlockSize - 1 && NeighborLevels[1] >= Level)
		index = 1;
	else if (j == 0 && NeighborLevels[2] >= Level)
		index = 2;
	else if (i == 0 && NeighborLevels[3] >= Level)
		index = 3;		
			
	if (index == -1)
		result = GetMorphing(x, y, height, Level, Blending);
	else
		result = GetMorphing(x, y, height, NeighborLevels[index], NeighborBlendings[index]);	
		
	return result;
}

float4 GetPosition(float x, float y)
{
	float height = GetHeight(x, y);
	
	if (GeomorphEnabled && !BruteForceEnabled && Level < Levels - 1)
		height -= GetMorphing(x, y, height);

	return float4(x, (height + HeightOffset) * Bumpiness, -y, 1);
}

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
	VertexShaderOutput output;
	
	float x = input.Position.x;
	float z = input.Position.z;
	
	float4 position = float4(x, 0.0f, z, 1.0f);
	float3 view = float3(0.0f, 1.0f, 0.0f);
	float2 texCoords = float2(0.0f, 0.0f);

	float y = -z;
	
	float2 normalCoords = float2(x, y) / (Size - 1);
	
	if (HeightmapEnabled)
		position = GetPosition(x, y); 
	
	if (TextureEnabled)
		texCoords = float2(x, y) * TextureRepeat / (Size - 1);
		
	if (LightingEnabled)
		view = CameraPosition - mul(position, World);
	
	output.Position = mul(position, WorldViewProjection);
	output.View = view;
	output.Light = -LightDirection;
	output.TexCoords = texCoords;
	output.NormalCoords = normalCoords;

	return output;
}

float4 GetColor(float4 color, float3 normal, float3 view, float3 light)
{
	float3 reflection = 2.0f * dot(normal, light) * normal - light;
	
	float diffuseIntensity = saturate(dot(normal, light));
	float specularIntensity = pow(saturate(dot(view, reflection)), LightShininess);
	
	float3 ambient = LightAmbient;
	float3 diffuse	= LightDiffuse * diffuseIntensity;
	float3 specular = LightSpecular * specularIntensity;
	
	return float4(color * (saturate(ambient + diffuse)) + specular, 1.0f);
}

float3 GetNormal(float2 normalCoords)
{
	float4 normal = (tex2D(NormalmapSampler, normalCoords) - 0.5f) * 2.0f;
	
	return normalize(float3(normal.x * Bumpiness, 2.0f, normal.z * Bumpiness));
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
	float4 pixel;

	if (TextureEnabled) 
		pixel = tex2D(TextureSampler, input.TexCoords);
	else
		pixel = TerrainColor;
	
	if (LightingEnabled)
	{
		float3 light = normalize(input.Light);
		float3 view	 = normalize(input.View);
		float3 normal = mul(GetNormal(input.NormalCoords), World);
		
		pixel = GetColor(pixel, normal, view, light);
	}

	return pixel;
}

technique Technique1
{
	pass Pass1
	{		
		VertexShader = compile vs_3_0 VertexShaderFunction();
		PixelShader = compile ps_3_0 PixelShaderFunction();
	}
}
