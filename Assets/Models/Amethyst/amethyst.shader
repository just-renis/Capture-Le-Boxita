
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
	SamplerState g_sSampler1 < Filter( ANISOTROPIC ); AddressU( WRAP ); AddressV( WRAP ); AddressW( WRAP ); MaxAniso( 32 ); >;
	SamplerState g_sSampler2 < Filter( ANISOTROPIC ); AddressU( WRAP ); AddressV( WRAP ); AddressW( WRAP ); MaxAniso( 32 ); >;
	SamplerState g_sSampler3 < Filter( ANISOTROPIC ); AddressU( WRAP ); AddressV( WRAP ); AddressW( WRAP ); MaxAniso( 32 ); >;
	CreateInputTexture2D( MaterialParameter1, Srgb, 8, "None", "_mask", ",0/,0/0", DefaultFile( "materials/edge_mask.jpg" ) );
	CreateInputTexture2D( MaterialParameter2, Linear, 8, "None", "_normal", ",0/,0/0", DefaultFile( "materials/diamond/diamond_edge_normal.jpg" ) );
	CreateInputTexture2D( MaterialParameter0, Linear, 8, "None", "_normal", ",0/,0/0", DefaultFile( "materials/amethyst/amethyst_normal.jpg" ) );
	Texture2D g_tMaterialParameter1 < Channel( RGBA, Box( MaterialParameter1 ), Linear ); OutputFormat( DXT1 ); SrgbRead( False ); >;
	Texture2D g_tMaterialParameter2 < Channel( RGBA, Box( MaterialParameter2 ), Linear ); OutputFormat( DXT1 ); SrgbRead( False ); >;
	Texture2D g_tMaterialParameter0 < Channel( RGBA, Box( MaterialParameter0 ), Linear ); OutputFormat( DXT1 ); SrgbRead( False ); >;
	TextureAttribute( LightSim_DiffuseAlbedoTexture, g_tMaterialParameter1 )
	TextureAttribute( RepresentativeTexture, g_tMaterialParameter1 )
	float2 g_vRotationcenter < UiGroup( "Texture UV,2/,0/2" ); Default2( 0.5,0.5 ); Range2( 0,0, 1,1 ); >;
	float g_flRotation < UiType( Slider ); UiStep( 1 ); UiGroup( "Texture UV,2/,0/1" ); Default1( -50 ); Range1( -360, 360 ); >;
	float2 g_vTilescale < UiType( Slider ); UiStep( 0.01 ); UiGroup( "Roughness UV,1/,0/1" ); Default2( 0.43,0.7 ); Range2( -10,-10, 10,10 ); >;
	float2 g_vTileoffset < UiType( Slider ); UiStep( 0.01 ); UiGroup( "Roughness UV,1/,0/1" ); Default2( 1.84,0.82 ); Range2( -5,-5, 5,5 ); >;
	float g_flMinRoguhness < UiType( Slider ); UiGroup( "Roughness UV,1/,0/3" ); Default1( 0.176 ); Range1( 0, 1 ); >;
	float g_flMaxRoghness < UiType( Slider ); UiGroup( "Roughness UV,1/,0/4" ); Default1( 0.49853882 ); Range1( 0, 1 ); >;
		
	RenderState( CullMode, F_RENDER_BACKFACES ? NONE : DEFAULT );
		
	float2 UVRotation( float2 vUv, float2 vRotationCenter, float flRotation )
	{
	    vUv = vUv.xy - vRotationCenter; // Offset incoming UV's by the specified Rotation Center. For example, the incoming uv's (0.0,0.0) could become (0.5,0.5).
	
	    // Convert degrees to radians
	    flRotation = radians(flRotation);
	
	    // U
	    float x = (vUv.x * cos(flRotation)) - (vUv.y * sin(flRotation));
	
	    // V
	    float y = (vUv.x * sin(flRotation)) + (vUv.y * cos(flRotation));
	
	    return (float2(x,y) - vRotationCenter); // Output the rotation result and then revert UV's 0,0 to its initial position.
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
		
		float4 l_0 = float4( 0.12294575, 0, 0.5674418, 1 );
		float4 l_1 = float4( 0.2967442, 0, 0.30697674, 1 );
		float4 l_2 = g_tMaterialParameter1.Sample( g_sSampler0,i.vTextureCoords.xy );
		float4 l_3 = lerp( l_0, l_1, l_2 );
		float4 l_4 = g_tMaterialParameter2.Sample( g_sSampler1,i.vTextureCoords.xy );
		float2 l_5 = g_vRotationcenter;
		float l_6 = g_flRotation;
		float2 l_7 = UVRotation( i.vTextureCoords.xy, l_5, l_6 );
		float4 l_8 = g_tMaterialParameter0.Sample( g_sSampler2,l_7 );
		float4 l_9 = g_tMaterialParameter1.Sample( g_sSampler3,i.vTextureCoords.xy );
		float4 l_10 = lerp( l_4, l_8, l_9 );
		float2 l_11 = g_vTilescale;
		float2 l_12 = g_vTileoffset;
		float2 l_13 = TileAndOffsetUv( i.vTextureCoords.xy, l_11, l_12 );
		float l_14 = Simplex2D(l_13);
		float l_15 = g_flMinRoguhness;
		float l_16 = g_flMaxRoghness;
		float l_17 = (saturate( ( l_14 - -1 ) / ( 1 - -1 ) ) * ( l_16 - l_15 )) + l_15;
		
		m.Albedo = l_3.xyz;
		m.Opacity = 1;
		m.Normal = l_10.xyz;
		m.Roughness = l_17;
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
