
HEADER
{
	Description = "";
}

FEATURES
{
	#include "common/features.hlsl"
}

MODES
{
	Forward();
	Depth( S_MODE_DEPTH );
	ToolsShadingComplexity( "tools_shading_complexity.shader" );
}

COMMON
{
	#ifndef S_ALPHA_TEST
	#define S_ALPHA_TEST 0
	#endif
	#ifndef S_TRANSLUCENT
	#define S_TRANSLUCENT 0
	#endif
	
	#include "common/shared.hlsl"
	#include "procedural.hlsl"

	#define S_UV2 1
	#define CUSTOM_MATERIAL_INPUTS
}

struct VertexInput
{
	#include "common/vertexinput.hlsl"
	float4 vColor : COLOR0 < Semantic( Color ); >;
};

struct PixelInput
{
	#include "common/pixelinput.hlsl"
	float3 vPositionOs : TEXCOORD14;
	float3 vNormalOs : TEXCOORD15;
	float4 vTangentUOs_flTangentVSign : TANGENT	< Semantic( TangentU_SignV ); >;
	float4 vColor : COLOR0;
	float4 vTintColor : COLOR1;
	#if ( PROGRAM == VFX_PROGRAM_PS )
		bool vFrontFacing : SV_IsFrontFace;
	#endif
};

VS
{
	#include "common/vertex.hlsl"

	PixelInput MainVs( VertexInput v )
	{
		
		PixelInput i = ProcessVertex( v );
		i.vPositionOs = v.vPositionOs.xyz;
		i.vColor = v.vColor;
		
		ExtraShaderData_t extraShaderData = GetExtraPerInstanceShaderData( v.nInstanceTransformID );
		i.vTintColor = extraShaderData.vTint;
		
		VS_DecodeObjectSpaceNormalAndTangent( v, i.vNormalOs, i.vTangentUOs_flTangentVSign );
		return FinalizeVertex( i );
		
	}
}

PS
{
	#include "common/pixel.hlsl"
	
	SamplerState g_sSampler0 < Filter( ANISO ); AddressU( WRAP ); AddressV( WRAP ); >;
	CreateInputTexture2D( Color, Srgb, 8, "None", "_color", ",0/,0/0", DefaultFile( "textures/scopetarget.png" ) );
	CreateInputTexture2D( Retical, Srgb, 8, "None", "_color", ",0/,0/0", DefaultFile( "textures/scope retical.png" ) );
	CreateInputTexture2D( Roughness, Srgb, 8, "None", "_color", ",0/,0/0", DefaultFile( "materials/dev/black_color.tga" ) );
	Texture2D g_tColor < Channel( RGBA, Box( Color ), Srgb ); OutputFormat( DXT5 ); SrgbRead( True ); >;
	Texture2D g_tRetical < Channel( RGBA, Box( Retical ), Srgb ); OutputFormat( DXT5 ); SrgbRead( True ); >;
	Texture2D g_tRoughness < Channel( RGBA, Box( Roughness ), Srgb ); OutputFormat( DXT5 ); SrgbRead( True ); >;
	TextureAttribute( LightSim_DiffuseAlbedoTexture, g_tRetical )
	TextureAttribute( RepresentativeTexture, g_tRetical )
	float g_flSize < UiGroup( ",0/,0/0" ); Default1( 1.2646251 ); Range1( 0, 2 ); >;
	float g_flDistance < UiGroup( ",0/,0/0" ); Default1( 3.9479456 ); Range1( 0, 10 ); >;
	float g_flVignetteSize < UiGroup( ",0/,0/0" ); Default1( 0.49999997 ); Range1( 0, 1 ); >;
	float g_flVignetteHardness < UiGroup( ",0/,0/0" ); Default1( 7 ); Range1( 0, 10 ); >;
		
	float3 GetTangentViewVector( float3 vPosition, float3 vNormalWs, float3 vTangentUWs, float3 vTangentVWs )
	{
	    float3 vCameraToPositionDirWs = CalculateCameraToPositionDirWs( vPosition.xyz );
	    vNormalWs = normalize( vNormalWs.xyz );
	    float3 vTangentViewVector = Vec3WsToTs( vCameraToPositionDirWs.xyz, vNormalWs.xyz, vTangentUWs.xyz, vTangentVWs.xyz );
		
	    // Result
	    return vTangentViewVector.xyz;
	}
	
	float4 MainPs( PixelInput i ) : SV_Target0
	{
		
		Material m = Material::Init();
		m.Albedo = float3( 1, 1, 1 );
		m.Normal = float3( 0, 0, 1 );
		m.Roughness = 1;
		m.Metalness = 0;
		m.AmbientOcclusion = 1;
		m.TintMask = 1;
		m.Opacity = 1;
		m.Emission = float3( 0, 0, 0 );
		m.Transmission = 0;
		
		float2 l_0 = CalculateViewportUv( i.vPositionSs.xy );
		float4 l_1 = Tex2DS( g_tColor, g_sSampler0, l_0 );
		float l_2 = g_flSize;
		float l_3 = pow( l_2, 3 );
		float l_4 = 1 / l_3;
		float3 l_5 = GetTangentViewVector( i.vPositionWithOffsetWs.xyz + g_vHighPrecisionLightingOffsetWs.xyz, i.vNormalWs, i.vTangentUWs, i.vTangentVWs );
		float3 l_6 = normalize( l_5 );
		float l_7 = g_flDistance;
		float3 l_8 = l_6 * float3( l_7, l_7, l_7 );
		float l_9 = l_4 - 1;
		float l_10 = l_9 * -0.5;
		float3 l_11 = l_8 + float3( l_10, l_10, l_10 );
		float2 l_12 = TileAndOffsetUv( i.vTextureCoords.xy, float2( l_4, l_4 ), l_11.xy );
		float4 l_13 = Tex2DS( g_tRetical, g_sSampler0, l_12 );
		float4 l_14 = l_1 * l_13;
		float2 l_15 = float2( 0.5, 0.5 );
		float l_16 = distance( l_12, l_15 );
		float l_17 = g_flVignetteSize;
		float l_18 = l_16 / l_17;
		float l_19 = 1 - l_18;
		float l_20 = g_flVignetteHardness;
		float l_21 = l_19 * l_20;
		float l_22 = saturate( l_21 );
		float4 l_23 = l_14 * float4( l_22, l_22, l_22, l_22 );
		float4 l_24 = Tex2DS( g_tRoughness, g_sSampler0, i.vTextureCoords.xy );
		
		m.Albedo = l_23.xyz;
		m.Opacity = 1;
		m.Roughness = l_24.x;
		m.Metalness = 0;
		m.AmbientOcclusion = 1;
		
		
		m.AmbientOcclusion = saturate( m.AmbientOcclusion );
		m.Roughness = saturate( m.Roughness );
		m.Metalness = saturate( m.Metalness );
		m.Opacity = saturate( m.Opacity );
		
		// Result node takes normal as tangent space, convert it to world space now
		m.Normal = TransformNormal( m.Normal, i.vNormalWs, i.vTangentUWs, i.vTangentVWs );
		
		// for some toolvis shit
		m.WorldTangentU = i.vTangentUWs;
		m.WorldTangentV = i.vTangentVWs;
		m.TextureCoords = i.vTextureCoords.xy;
				
		return ShadingModelStandard::Shade( i, m );
	}
}
