//UNITY_SHADER_NO_UPGRADE
Shader "Hidden/CSG/internal/ZTestGenericLine_old"
{
	Properties 
	{
		_thicknessMultiplier("thicknessMultiplier", Float) = 1
		_dashMultiplier("dashMultiplier", Float) = 1
		_pixelsPerPoint("pixelsPerPoint", Float) = 1
//		_ZTest("ZTest", Int) = 4
//		_MainTex("Texture", 2D) = "white" {}
	}
	SubShader 
	{
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
		LOD 200
		Offset -1, -50
		Lighting Off
        ZWrite Off
		ZTest LEqual
		Cull Off
		Blend One OneMinusSrcAlpha
		Offset -1, -10

        Pass 
		{
			CGPROGRAM
				
				#pragma vertex vert
				#pragma fragment frag
				#pragma target 2.0
			
				#include "UnityCG.cginc"

				struct v2f 
				{
 					float4 vertex	: SV_POSITION;
 					//float2 uv		: TEXCOORD0;
					fixed4 color    : COLOR0;
					float dashSize  : TEXCOORD0;
					float4 screenPosition0 : TEXCOORD1;
					float4 screenPosition1 : TEXCOORD2;
				};

				float _thicknessMultiplier;
				float _dashMultiplier;
				float _pixelsPerPoint;

				//sampler2D _MainTex;
				//float4 _MainTex_ST;

				v2f vert(float4 vertex1  : POSITION,
						 float3 vertex2  : TEXCOORD0, // second vertex to compute angle with
						 float4 offset   : TEXCOORD1,
					     fixed4 color	 : COLOR0)
				{
					v2f o;

					float4	out_vertex1 = mul(UNITY_MATRIX_MVP,        vertex1    );
					float4	out_vertex2 = mul(UNITY_MATRIX_MVP, float4(vertex2, 1));

					float4 screenPosition0 = out_vertex1;
					float4 screenPosition1 = out_vertex2;

					screenPosition0.xy *= 0.5f;
					screenPosition0.y *= _ProjectionParams.x;
					screenPosition0.xy += screenPosition0.w;
#if defined(UNITY_HALF_TEXEL_OFFSET)
					screenPosition0.xy *= _ScreenParams.zw;
#endif
					screenPosition0.xy /= screenPosition0.w;

					screenPosition1.xy *= 0.5f;
					screenPosition1.y *= _ProjectionParams.x;
					screenPosition1.xy += screenPosition1.w;
#if defined(UNITY_HALF_TEXEL_OFFSET)
					screenPosition1.xy *= _ScreenParams.zw;
#endif
					screenPosition1.xy /= screenPosition1.w;

					screenPosition0.xy *= _ScreenParams.xy;
					screenPosition1.xy *= _ScreenParams.xy;


						float thickness = (offset.z * _thicknessMultiplier * 0.5f);
						float2 delta = normalize((screenPosition1 - screenPosition0).xy) * offset.x * ((thickness < 0.65f) ? 0.65f : thickness) * _pixelsPerPoint;
						float2 offset2D;

						offset2D.x = -delta.y;
						offset2D.y =  delta.x;
					
						screenPosition0.xy += offset2D * offset.y;

						o.screenPosition0 = ComputeScreenPos(out_vertex1);
						o.screenPosition1 = ComputeScreenPos(out_vertex2);
						o.screenPosition0.xy *= _ScreenParams.xy * 0.25f;
						o.screenPosition1.xy *= _ScreenParams.xy * 0.25f;
						o.dashSize = offset.w * _dashMultiplier * 2.00f;
						o.screenPosition0.w *= o.dashSize;
						o.screenPosition1.w *= o.dashSize;

					screenPosition0.xy /= _ScreenParams.xy;
					

					screenPosition0.xy *= screenPosition0.w;
#if defined(UNITY_HALF_TEXEL_OFFSET)
					screenPosition0.xy /= _ScreenParams.zw;
#endif
					screenPosition0.xy -= screenPosition0.w;
					screenPosition0.y /= _ProjectionParams.x;
					screenPosition0.xy *= 2.0f;


					out_vertex1 = screenPosition0;
					o.vertex = out_vertex1;

					//o.uv = TRANSFORM_TEX((offset.xy * 0.5f) + float2(0.5, 0.5), _MainTex);
					o.color = color;
					return o;
				}

				fixed4 frag (v2f i) : SV_Target
				{
					fixed4	color = i.color;

					float2  pos0  = (i.dashSize == 0) ? float2(0, 0) : (i.screenPosition1.xy) / (i.screenPosition1.w);
					float2  pos1  = (i.dashSize == 0) ? float2(0, 0) : (i.screenPosition0.xy) / (i.screenPosition0.w);
					float2  delta = pos1 - pos0;
					float   value = length(delta);


					// goes back and forth between dash on / dash off
					// Note: we might want to smooth the subpixel transition between on / off
					float	dist = frac(step(frac(value), 0.5f) * 0.5f) * 2.0f;
					color *= dist;
					color.rgb *= color.a;
					return  color;
				}

			ENDCG
		}
	}
}
