
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
	
	float4 g_vBarkRidgeColor < UiType( Color ); UiGroup( ",0/,0/0" ); Default4( 0.40, 0.32, 0.27, 1.00 ); >;
	float4 g_vBarkPrimaryColor < UiType( Color ); UiGroup( ",0/,0/0" ); Default4( 1.00, 0.85, 0.67, 1.00 ); >;
	float2 g_vBarkSmoothStep < UiGroup( ",0/,0/0" ); Default2( 0.195,0.6 ); Range2( 0,0, 1,1 ); >;
	float2 g_vBarkScale < UiGroup( ",0/,0/0" ); Default2( 1,1 ); Range2( 0,0, 20,20 ); >;
	float4 g_vSandColor < UiType( Color ); UiGroup( ",0/,0/0" ); Default4( 1.00, 0.78, 0.37, 1.00 ); >;
	float4 g_vFallenSandColor < UiType( Color ); UiGroup( ",0/,0/0" ); Default4( 1.00, 0.88, 0.59, 1.00 ); >;
	float2 g_vSandSmoothStep < UiGroup( ",0/,0/0" ); Default2( 0.5,0.8 ); Range2( 0,0, 1,1 ); >;
	float3 g_vSandDirection < UiGroup( ",0/,0/0" ); Default3( 0,0,1 ); Range3( -1,-1,-1, 1,1,1 ); >;
	float g_flBarkRidgeNormalStrength < UiGroup( ",0/,0/0" ); Default1( 0.7998106 ); Range1( 0, 1 ); >;
		
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
		
		float4 l_0 = g_vBarkRidgeColor;
		float4 l_1 = g_vBarkPrimaryColor;
		float2 l_2 = g_vBarkSmoothStep;
		float2 l_3 = g_vBarkScale;
		float l_4 = Simplex2D(float2( l_3.x, l_3.x ));
		float l_5 = l_4 * 10;
		float2 l_6 = float2( l_5, l_3.y);
		float2 l_7 = TileAndOffsetUv( i.vTextureCoords.xy, l_6, float2( 0, 0 ) );
		float l_8 = Simplex2D(l_7);
		float l_9 = smoothstep( l_2.x, l_2.y, l_8 );
		float4 l_10 = lerp( l_0, l_1, l_9 );
		float4 l_11 = g_vSandColor;
		float3 l_12 = i.vPositionOs;
		float l_13 = l_12.z;
		float l_14 = (saturate( ( l_13 - 0 ) / ( 50 - 0 ) ) * ( 0 - 1 )) + 1;
		float4 l_15 = lerp( l_10, l_11, l_14 );
		float4 l_16 = g_vFallenSandColor;
		float2 l_17 = g_vSandSmoothStep;
		float3 l_18 = g_vSandDirection;
		float l_19 = dot( i.vNormalWs, l_18 );
		float l_20 = smoothstep( l_17.x, l_17.y, l_19 );
		float4 l_21 = lerp( l_15, l_16, l_20 );
		float l_22 = g_flBarkRidgeNormalStrength;
		float3 l_23 = Height2Normal( l_9, l_22, i.vPositionWithOffsetWs.xyz + g_vHighPrecisionLightingOffsetWs.xyz, i.vNormalWs );
		
		m.Albedo = l_21.xyz;
		m.Opacity = 1;
		m.Normal = l_23;
		m.Roughness = 0.9;
		m.Metalness = 0;
		m.AmbientOcclusion = l_9;
		
		
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
