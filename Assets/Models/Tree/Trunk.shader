
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
	CreateInputTexture2D( MaterialParameter2, Srgb, 8, "None", "_color", ",0/,0/0", DefaultFile( "materials/tree/trunk_color.jpg" ) );
	Texture2D g_tMaterialParameter2 < Channel( RGBA, Box( MaterialParameter2 ), Linear ); OutputFormat( DXT1 ); SrgbRead( False ); >;
	TextureAttribute( LightSim_DiffuseAlbedoTexture, g_tMaterialParameter2 )
	TextureAttribute( RepresentativeTexture, g_tMaterialParameter2 )
	float4 g_vMaterialParameter1 < UiType( Color ); UiGroup( ",0/,0/0" ); Default4( 1.00, 0.81, 0.45, 1.00 ); >;
	float2 g_vSandSmoothing < UiStep( 0.05 ); UiGroup( ",0/,0/0" ); Default2( 0.4,0.8 ); Range2( 0,0, 1,1 ); >;
		
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
		
		float4 l_0 = g_tMaterialParameter2.Sample( g_sSampler0,i.vTextureCoords.xy );
		float4 l_1 = g_vMaterialParameter1;
		float2 l_2 = g_vSandSmoothing;
		float3 l_3 = float3( 0, 0, 1 );
		float l_4 = dot( i.vNormalWs, l_3 );
		float l_5 = smoothstep( l_2.x, l_2.y, l_4 );
		float4 l_6 = lerp( l_0, l_1, l_5 );
		
		m.Albedo = l_6.xyz;
		m.Opacity = 1;
		m.Roughness = 0.90030664;
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
