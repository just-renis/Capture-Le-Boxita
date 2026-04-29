
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
	Depth();
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
	#include "common/gradient.hlsl"
	#include "procedural.hlsl"

	#define S_UV2 1
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
	
	SamplerState g_sSampler0 < Filter( BILINEAR ); AddressU( WRAP ); AddressV( WRAP ); AddressW( WRAP ); MaxAniso( 8 ); >;
	SamplerState g_sSampler1 < Filter( BILINEAR ); AddressU( WRAP ); AddressV( WRAP ); AddressW( WRAP ); MaxAniso( 8 ); >;
	SamplerState g_sSampler2 < Filter( BILINEAR ); AddressU( WRAP ); AddressV( WRAP ); AddressW( WRAP ); MaxAniso( 8 ); >;
	CreateInputTexture2D( NormalMap, Linear, 8, "None", "_normal", ",0/,0/0", DefaultFile( "materials/rock/rock_normal.jpg" ) );
	CreateInputTexture2D( RoughnessMap, Linear, 8, "None", "_rough", ",0/,0/0", DefaultFile( "materials/rock/rock_rough.jpg" ) );
	CreateInputTexture2D( AmbientOcclusion, Linear, 8, "None", "_ao", ",0/,0/0", DefaultFile( "materials/rock/rock_ao.jpg" ) );
	Texture2D g_tNormalMap < Channel( RGBA, Box( NormalMap ), Linear ); OutputFormat( DXT1 ); SrgbRead( False ); >;
	Texture2D g_tRoughnessMap < Channel( RGBA, Box( RoughnessMap ), Linear ); OutputFormat( DXT1 ); SrgbRead( False ); >;
	Texture2D g_tAmbientOcclusion < Channel( RGBA, Box( AmbientOcclusion ), Linear ); OutputFormat( DXT1 ); SrgbRead( False ); >;
	float4 g_vRockSecondaryColor < UiType( Color ); UiGroup( ",0/,0/0" ); Default4( 0.33, 0.08, 0.08, 1.00 ); >;
	float4 g_vRockPrimaryColor < UiType( Color ); UiGroup( ",0/,0/0" ); Default4( 0.69, 0.55, 0.38, 1.00 ); >;
	float g_flUVScale < UiType( Slider ); UiGroup( ",0/,0/0" ); Default1( 1 ); Range1( 0, 10 ); >;
	float g_flGritStoneDetailScale < UiType( Slider ); UiStep( 1 ); UiGroup( ",0/,0/0" ); Default1( 50 ); Range1( 0, 100 ); >;
	float g_flRockSpeckles < UiType( Slider ); UiStep( 0.05 ); UiGroup( ",0/,0/0" ); Default1( 0.1 ); Range1( 0, 1 ); >;
	float4 g_vSandColor < UiType( Color ); UiGroup( ",0/,0/0" ); Default4( 0.94, 0.68, 0.29, 1.00 ); >;
	float2 g_vSandDustSmoothStep < UiType( Slider ); UiGroup( ",0/,0/0" ); Default2( 0.5,0.85 ); Range2( 0,0, 1,1 ); >;
	float3 g_vSandDustDirection < UiType( Slider ); UiGroup( ",0/,0/0" ); Default3( 0,0,1 ); Range3( -1,-1,-1, 1,1,1 ); >;
		
	RenderState( CullMode, F_RENDER_BACKFACES ? NONE : DEFAULT );
	
	float4 MainPs( PixelInput i ) : SV_Target0
	{
		
		Material m = Material::Init( i );
		m.Albedo = float3( 1, 1, 1 );
		m.Normal = float3( 0, 0, 1 );
		m.Roughness = 1;
		m.Metalness = 0;
		m.AmbientOcclusion = 1;
		m.TintMask = 1;
		m.Opacity = 1;
		m.Emission = float3( 0, 0, 0 );
		m.Transmission = 0;
		
		float4 l_0 = g_vRockSecondaryColor;
		float4 l_1 = g_vRockPrimaryColor;
		float3 l_2 = i.vPositionOs;
		float l_3 = g_flUVScale;
		float2 l_4 = (l_2.xy * l_3);
		float l_5 = Simplex2D(l_4);
		float3 l_6 = l_2 + float3( l_5, l_5, l_5 );
		float l_7 = l_6.z;
		float l_8 = l_7 * 10;
		float l_9 = sin( l_8 );
		float l_10 = (saturate( ( l_9 - -1 ) / ( 1 - -1 ) ) * ( 1 - 0 )) + 0;
		float4 l_11 = lerp( l_0, l_1, l_10 );
		float3 l_12 = i.vPositionOs;
		float l_13 = g_flGritStoneDetailScale;
		float2 l_14 = (l_12.xy * l_13);
		float l_15 = Simplex2D(l_14);
		float l_16 = g_flRockSpeckles;
		float4 l_17 = saturate( lerp( l_11, l_11*float4( l_15, l_15, l_15, l_15 ), l_16 ) );
		float4 l_18 = g_vSandColor;
		float2 l_19 = g_vSandDustSmoothStep;
		float3 l_20 = g_vSandDustDirection;
		float l_21 = dot( i.vNormalWs, l_20 );
		float l_22 = smoothstep( l_19.x, l_19.y, l_21 );
		float4 l_23 = lerp( l_17, l_18, l_22 );
		float4 l_24 = g_tNormalMap.Sample( g_sSampler0,i.vTextureCoords.xy );
		float4 l_25 = g_tRoughnessMap.Sample( g_sSampler1,i.vTextureCoords.xy );
		float4 l_26 = g_tAmbientOcclusion.Sample( g_sSampler2,i.vTextureCoords.xy );
		
		m.Albedo = l_23.xyz;
		m.Opacity = 1;
		m.Normal = l_24.xyz;
		m.Roughness = l_25.x;
		m.Metalness = 0;
		m.AmbientOcclusion = l_26.x;
		
		
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
				
		return ShadingModelStandard::Shade( m );
	}
}
