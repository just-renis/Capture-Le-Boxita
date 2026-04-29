
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
	
	float2 g_vColorSmoothStep < UiType( Slider ); UiGroup( ",0/,0/3" ); Default2( 0,1 ); Range2( 0,0, 1,1 ); >;
	float g_flCactusColorMix < UiGroup( ",0/,0/1" ); Default1( 0.783 ); Range1( 0, 1 ); >;
	float2 g_vCactusColorOffset < UiType( Slider ); UiGroup( ",0/,0/2" ); Default2( 2.436,0 ); Range2( 0,0, 5,5 ); >;
	float g_flSpikeAmount < UiType( Slider ); UiStep( 0.5 ); UiGroup( ",0/,0/4" ); Default1( 9 ); Range1( 0, 20 ); >;
	float2 g_vSpikeVoronoiNoise < UiType( Slider ); UiStep( 0.1 ); UiGroup( ",0/,0/5" ); Default2( 1.4,10 ); Range2( 0,0, 2,10 ); >;
	float g_flCactusRidges < UiType( Slider ); UiStep( 1 ); UiGroup( ",0/,0/0" ); Default1( 14 ); Range1( 0, 50 ); >;
		
	RenderState( CullMode, F_RENDER_BACKFACES ? NONE : DEFAULT );
		
	float3 Height2Normal( float flHeight , float flStrength, float3 vPosition, float3 vNormal )
	{
	    float3 worldDerivativeX = ddx(vPosition);
	    float3 worldDerivativeY = ddy(vPosition);
	
	    float3 crossX = cross(vNormal, worldDerivativeX);
	    float3 crossY = cross(worldDerivativeY, vNormal);
	
	    float d = dot(worldDerivativeX, crossY);
	
	    float sgn = d < 0.0 ? (-1.f) : 1.f;
	    float surface = sgn / max(0.00000000000001192093f, abs(d));
	
	    float dHdx = ddx(flHeight);
	    float dHdy = ddy(flHeight);
	
	    float3 surfGrad = surface * (dHdx*crossY + dHdy*crossX);
	
	    return normalize(vNormal - (flStrength * surfGrad));
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
		
		float4 l_0 = float4( 0.08967, 0.15022, 0, 1 );
		float4 l_1 = float4( 0.01938, 0.11389, 0, 1 );
		float2 l_2 = g_vColorSmoothStep;
		float l_3 = g_flCactusColorMix;
		float2 l_4 = g_vCactusColorOffset;
		float2 l_5 = TileAndOffsetUv( i.vTextureCoords.xy, float2( l_3, l_3 ), l_4 );
		float l_6 = Simplex2D(l_5);
		float l_7 = smoothstep( l_2.x, l_2.y, l_6 );
		float4 l_8 = lerp( l_0, l_1, l_7 );
		float4 l_9 = float4( 0.30698, 0.30012, 0.23844, 1 );
		float l_10 = g_flSpikeAmount;
		float2 l_11 = TileAndOffsetUv( i.vTextureCoords.xy, float2( l_10, l_10 ), float2( 0, 0 ) );
		float2 l_12 = g_vSpikeVoronoiNoise;
		float l_13 = VoronoiNoise( l_11, l_12.x, l_12.y );
		float l_14 = 1 - l_13;
		float l_15 = smoothstep( 0.7, 0.95, l_14 );
		float4 l_16 = lerp( l_8, l_9, l_15 );
		float l_17 = pow( l_14, 8 );
		float l_18 = g_flCactusRidges;
		float2 l_19 = TileAndOffsetUv( i.vTextureCoords.xy, float2( l_18, l_18 ), float2( 0, 0 ) );
		float l_20 = l_19.x;
		float l_21 = l_20 * 20;
		float l_22 = sin( l_21 );
		float l_23 = (saturate( ( l_22 - -1 ) / ( 1 - -1 ) ) * ( 1 - 0 )) + 0;
		float l_24 = l_17 + l_23;
		float3 l_25 = Height2Normal( l_24, 0.1, i.vPositionWithOffsetWs.xyz + g_vHighPrecisionLightingOffsetWs.xyz, i.vNormalWs );
		
		m.Albedo = l_16.xyz;
		m.Opacity = 1;
		m.Normal = l_25;
		m.Roughness = 1;
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
