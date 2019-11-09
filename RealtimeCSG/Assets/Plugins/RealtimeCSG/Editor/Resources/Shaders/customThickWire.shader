//UNITY_SHADER_NO_UPGRADE
Shader "Hidden/CSG/internal/customThickWire"
{
	Properties 
	{
	}
	SubShader 
	{
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
		LOD 200
		Offset -1, -50
		ZTest LEqual
        Lighting Off
        ZWrite Off
        Cull Off
		Blend One OneMinusSrcAlpha

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
 					fixed4 color	: COLOR0;
					float  alpha    : TEXCOORD0;
				};

				//float thickness;

				v2f vert (appdata_full v)
				{
					float width	 = _ScreenParams.x * 0.5f;
					float height = _ScreenParams.y * 0.5f;
					float2 wh = float2(width,height);
						

					float3 forward	= normalize(UNITY_MATRIX_V[2].xyz);
					float3 right	= normalize(UNITY_MATRIX_V[0].xyz);

#if UNITY_VERSION >= 540
					float distance = dot(mul(unity_ObjectToWorld, v.vertex) - _WorldSpaceCameraPos, forward);
#else
					float distance = dot(mul(_Object2World, v.vertex) - _WorldSpaceCameraPos, forward);
#endif
					float4 p1 = float4(_WorldSpaceCameraPos + (forward * distance),1);
					float4 p2 = float4(p1 + right,1);
					
					p1 = mul(UNITY_MATRIX_VP, p1);
					p2 = mul(UNITY_MATRIX_VP, p2);
					float aw = (abs(p1.w) < 1.0e-7f) ? 0 : (1.0f / p1.w);
					float bw = (abs(p2.w) < 1.0e-7f) ? 0 : (1.0f / p2.w);
					float2 a = p1.xy * aw;
					float2 b = p2.xy * bw;
					float2 c = (a - b) * wh;
					float __thickness = (80.0f * v.texcoord.x) / max(0.0001f, length(c) );




					v2f o;
					float3 diff = normalize(v.texcoord1);
					float3 cam = normalize(UNITY_MATRIX_IT_MV[2].xyz);//_WorldSpaceCameraPos - v.vertex);
					//float3 cam = normalize(_WorldSpaceCameraPos - v.vertex);
					float3 vec = (__thickness * v.texcoord.y) * normalize(cross(diff, cam));//normalize(vecx) +

					o.vertex = mul (UNITY_MATRIX_MVP, v.vertex);
					v.vertex.xyz = v.vertex.xyz + vec;
					float4 other = mul (UNITY_MATRIX_MVP, v.vertex);

					//o.vertex.z += (other - o.vertex).xz;
					o.vertex.xy += (other - o.vertex).xy;// / _ScreenParams.xy;
					o.alpha = abs(v.texcoord.y);


					//o.pos.z += 0.00105f;	// I would use Offset if it actually worked ..
					o.color = v.color;
					return o;
				}

				fixed4 frag (v2f input) : SV_Target
				{
					fixed4 col = input.color;
					float f = 1 - abs(input.alpha);
					col.a *= 1;
					col.rgb *= col.a;
					return col;
				}

			ENDCG
		}
	}
}
