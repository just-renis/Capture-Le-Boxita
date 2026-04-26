
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
	
	SamplerState g_sSampler0 < Filter( ANISOTROPIC ); AddressU( WRAP ); AddressV( WRAP ); AddressW( WRAP ); MaxAniso( 32 ); >;
	CreateInputTexture2D( NormalMap, Linear, 8, "None", "_normal", ",0/,0/0", DefaultFile( "models/metal box/metal-normal.jpg" ) );
	Texture2D g_tNormalMap < Channel( RGBA, Box( NormalMap ), Linear ); OutputFormat( DXT1 ); SrgbRead( False ); >;
	float2 g_vTilescale < UiType( Slider ); UiStep( 0.1 ); UiGroup( "Dull metal roughness,0/,0/2" ); Default2( 0.7,0.7 ); Range2( 0,0, 10,10 ); >;
	float2 g_vTileoffset < UiType( Slider ); UiGroup( "Dull metal roughness,0/,0/2" ); Default2( 0.426,0.908 ); Range2( -3,-3, 3,3 ); >;
	float g_flRoughnessmin < UiType( Slider ); UiStep( 0.05 ); UiGroup( "Dull metal roughness,0/,0/1" ); Default1( 0.4 ); Range1( 0, 1 ); >;
	float g_flRoughnessmax < UiType( Slider ); UiStep( 0.05 ); UiGroup( "Dull metal roughness,0/,0/1" ); Default1( 0.7 ); Range1( 0, 1 ); >;
		
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
		
		float4 l_0 = float4( 0.74419, 0.74419, 0.74419, 1 );
		float4 l_1 = g_tNormalMap.Sample( g_sSampler0,i.vTextureCoords.xy );
		float2 l_2 = g_vTilescale;
		float2 l_3 = g_vTileoffset;
		float2 l_4 = TileAndOffsetUv( i.vTextureCoords.xy, l_2, l_3 );
		float l_5 = Simplex2D(l_4);
		float l_6 = g_flRoughnessmin;
		float l_7 = g_flRoughnessmax;
		float l_8 = (saturate( ( l_5 - -1 ) / ( 1 - -1 ) ) * ( l_7 - l_6 )) + l_6;
		
		m.Albedo = l_0.xyz;
		m.Opacity = 1;
		m.Normal = l_1.xyz;
		m.Roughness = l_8;
		m.Metalness = 1;
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
