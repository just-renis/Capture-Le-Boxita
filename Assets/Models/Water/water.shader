
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
	#define S_TRANSLUCENT 1
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
	
	float2 g_vWaveHeight < UiGroup( "Wave Foam Settings,3/,0/2" ); Default2( 0,20 ); Range2( 0,0, 50,50 ); >;
	float2 g_vWaveHeightDistance < UiGroup( "Wave Foam Settings,3/,0/3" ); Default2( 200,2000 ); Range2( 0,0, 5000,5000 ); >;
	
	PixelInput MainVs( VertexInput v )
	{
		
		PixelInput i = ProcessVertex( v );
		i.vPositionOs = v.vPositionOs.xyz;
		
		i.vColor = v.vColor;
		
		ExtraShaderData_t extraShaderData = GetExtraPerInstanceShaderData( v.nInstanceTransformID );
		i.vTintColor = extraShaderData.vTint;
		
		VS_DecodeObjectSpaceNormalAndTangent( v, i.vNormalOs, i.vTangentUOs_flTangentVSign );
				
		float3 l_0 = i.vPositionWs;
		float l_1 = l_0.x;
		float l_2 = l_0.y;
		float l_3 = l_1 + l_2;
		float l_4 = l_3 + g_flTime;
		float l_5 = sin( l_4 );
		float2 l_6 = g_vWaveHeight;
		float2 l_7 = g_vWaveHeightDistance;
		float3 l_8 = g_vCameraPositionWs;
		float l_9 = distance( l_8, l_0 );
		float l_10 = smoothstep( l_7.x, l_7.y, l_9 );
		float l_11 = lerp( l_6.x, l_6.y, l_10 );
		float l_12 = l_5 * l_11;
		float3 l_13 = float3( 0, 0, l_12 );
		i.vPositionWs.xyz += l_13;
		i.vPositionPs.xyzw = Position3WsToPs( i.vPositionWs.xyz );
		return FinalizeVertex( i );
		
	}
}

PS
{
	#include "common/pixel.hlsl"
	
	SamplerState g_sSampler0 < Filter( ANISOTROPIC ); AddressU( WRAP ); AddressV( WRAP ); AddressW( WRAP ); MaxAniso( 8 ); >;
	CreateInputTexture2D( WaveNormal, Srgb, 8, "None", "_color", "Wave Texture Settings,2/,0/0", DefaultFile( "materials/water/wave_color.jpg" ) );
	Texture2D g_tWaveNormal < Channel( RGBA, Box( WaveNormal ), Linear ); OutputFormat( DXT1 ); SrgbRead( False ); >;
	TextureAttribute( LightSim_DiffuseAlbedoTexture, g_tWaveNormal )
	TextureAttribute( RepresentativeTexture, g_tWaveNormal )
	float2 g_vWaveNormalScale < UiType( Slider ); UiGroup( "Wave Texture Settings,2/,0/1" ); Default2( 1,1 ); Range2( 0,0, 100,100 ); >;
	float2 g_vWavePanningSpeed < UiGroup( "Wave Texture Settings,2/,0/2" ); Default2( 0,0.108 ); Range2( 0,0, 1,1 ); >;
	float4 g_vShallowWaterColor < UiType( Color ); UiGroup( "Water Depth Color,1/,0/1" ); Default4( 0.00, 0.93, 1.00, 1.00 ); >;
	float4 g_vDeepWaterColor < UiType( Color ); UiGroup( "Water Depth Color,1/,0/0" ); Default4( 0.00, 0.22, 0.76, 1.00 ); >;
	float g_flDepthIntensity < UiGroup( "Water Depth Color,1/,0/2" ); Default1( 1 ); Range1( -10, 10 ); >;
	float4 g_vWaveTopColor < UiType( Color ); UiGroup( "Wave Foam Settings,3/,0/0" ); Default4( 1.00, 1.00, 1.00, 1.00 ); >;
	float2 g_vWaveTopColorBlend < UiGroup( "Wave Foam Settings,3/,0/1" ); Default2( 10,20 ); Range2( 0,0, 20,20 ); >;
	float g_flWaterHeight < UiGroup( ",0/,0/0" ); Default1( 0 ); Range1( -50000, 50000 ); >;
	float2 g_vWaterOpacity < UiGroup( "Water Depth Color,1/,0/3" ); Default2( 0.2,0.95 ); Range2( 0,0, 1,1 ); >;
		
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
		
		float2 l_0 = g_vWaveNormalScale;
		float2 l_1 = (i.vTextureCoords.xy * l_0);
		float2 l_2 = g_vWavePanningSpeed;
		float2 l_3 = (l_1 + g_flTime * l_2);
		float4 l_4 = g_tWaveNormal.Sample( g_sSampler0,l_3 );
		float4 l_5 = saturate( lerp( l_4, float4( 0.0093, 0.58721, 1, 1 ), 0.5 ) );
		float4 l_6 = g_vShallowWaterColor;
		float4 l_7 = g_vDeepWaterColor;
		float2 l_8 = i.vPositionSs.xy;
		float l_9 = Depth::GetLinear( l_8 );
		float l_10 = i.vPositionSs.w;
		float l_11 = l_9 - l_10;
		float l_12 = g_flDepthIntensity;
		float l_13 = l_11 * l_12;
		float l_14 = saturate( l_13 );
		float4 l_15 = lerp( l_6, l_7, l_14 );
		float4 l_16 = g_vWaveTopColor;
		float2 l_17 = g_vWaveTopColorBlend;
		float3 l_18 = i.vPositionWithOffsetWs.xyz + g_vHighPrecisionLightingOffsetWs.xyz;
		float l_19 = l_18.z;
		float l_20 = g_flWaterHeight;
		float l_21 = l_19 - l_20;
		float l_22 = smoothstep( l_17.x, l_17.y, l_21 );
		float4 l_23 = lerp( l_15, l_16, l_22 );
		float4 l_24 = saturate( lerp( l_5, l_23, 0.5 ) );
		float2 l_25 = g_vWaterOpacity;
		float l_26 = lerp( l_25.x, l_25.y, l_14 );
		
		m.Albedo = l_24.xyz;
		m.Opacity = l_26;
		m.Roughness = 0.05;
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
