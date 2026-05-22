
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
	
	SamplerState g_sSampler0 < Filter( ANISOTROPIC ); AddressU( WRAP ); AddressV( WRAP ); AddressW( WRAP ); MaxAniso( 16 ); >;
	SamplerState g_sSampler1 < Filter( ANISOTROPIC ); AddressU( WRAP ); AddressV( WRAP ); AddressW( WRAP ); MaxAniso( 16 ); >;
	CreateInputTexture2D( WaveNormal, Linear, 8, "None", "_normal", ",0/,0/3", DefaultFile( "materials/sand/sand_wave.jpg" ) );
	CreateInputTexture2D( GrainNormal, Linear, 8, "None", "_normal", ",0/,0/2", DefaultFile( "materials/sand/sand_grain.jpg" ) );
	Texture2D g_tWaveNormal < Channel( RGBA, Box( WaveNormal ), Linear ); OutputFormat( DXT1 ); SrgbRead( False ); >;
	Texture2D g_tGrainNormal < Channel( RGBA, Box( GrainNormal ), Linear ); OutputFormat( DXT1 ); SrgbRead( False ); >;
	float4 g_vSandColor < UiType( Color ); UiGroup( ",0/,0/1" ); Default4( 0.71, 0.60, 0.17, 1.00 ); >;
	float g_flSparkleAngle < UiType( Slider ); UiStep( 0.2 ); UiGroup( "Sand sparkle,1/,0/0" ); Default1( 3.6 ); Range1( 0, 10 ); >;
	float g_flSparkleAmount < UiType( Slider ); UiStep( 1 ); UiGroup( "Sand sparkle,1/,0/0" ); Default1( 189 ); Range1( 0, 500 ); >;
	float g_flWaveScale < UiType( Slider ); UiStep( 1 ); UiGroup( ",0/,0/4" ); Default1( 5 ); Range1( 0, 100 ); >;
	float g_flGrainScale < UiType( Slider ); UiStep( 1 ); UiGroup( ",1/,0/5" ); Default1( 35 ); Range1( 0, 100 ); >;
		
	RenderState( CullMode, F_RENDER_BACKFACES ? NONE : DEFAULT );
		
	float3 ReorientedNormalBlendVector( float3 a, float3 b )
	{
		float3 t = a.xyz + float3( 0.0, 0.0, 1.0 );
		float3 u = b.xyz * float3( -1.0, -1.0, 1.0 );
		return ( t / t.z ) * dot( t, u ) - u;
	}
	
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
		
		float4 l_0 = g_vSandColor;
		float3 l_1 = i.vPositionWithOffsetWs.xyz + g_vHighPrecisionLightingOffsetWs.xyz;
		float3 l_2 = l_1 * float3( 0.1, 0.1, 0.1 );
		float l_3 = g_flSparkleAngle;
		float l_4 = g_flSparkleAmount;
		float l_5 = VoronoiNoise( l_2.xy, l_3, l_4 );
		float l_6 = step( 0.9990002, l_5 );
		float3 l_7 = CalculatePositionToCameraDirWs( i.vPositionWithOffsetWs.xyz + g_vHighPrecisionLightingOffsetWs.xyz );
		float l_8 = dot( i.vNormalWs, l_7 );
		float l_9 = l_6 * l_8;
		float l_10 = l_9 * 10;
		float l_11 = g_flWaveScale;
		float2 l_12 = (i.vTextureCoords.xy * l_11);
		float4 l_13 = g_tWaveNormal.Sample( g_sSampler0,l_12 );
		float3 l_14 = float3( l_13.r, l_13.g, l_13.b );
		float l_15 = g_flGrainScale;
		float2 l_16 = (i.vTextureCoords.xy * l_15);
		float4 l_17 = g_tGrainNormal.Sample( g_sSampler1,l_16 );
		float3 l_18 = float3( l_17.r, l_17.g, l_17.b );
		float3 l_19 = ReorientedNormalBlendVector( l_14, l_18 );
		
		m.Albedo = l_0.xyz;
		m.Emission = float3( l_10, l_10, l_10 );
		m.Opacity = 1;
		m.Normal = l_19;
		m.Roughness = 0.8;
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
				
		return ShadingModelStandard::Shade( m );
	}
}
