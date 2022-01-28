//UNITY_SHADER_NO_UPGRADE
Shader "Hidden/CSG/internal/customWireDotted"
{
	Properties { _MainTex ("Texture", Any) = "white" {} }
	SubShader 
	{
		Tags { "ForceSupported" = "True" }
		Lighting Off
		Blend One OneMinusSrcAlpha
        Cull Off
        ZWrite Off
		ZTest Always
		BindChannels { 
			Bind "vertex", vertex 
			Bind "color", color 
			Bind "TexCoord", texcoord 
		}
        Pass 
		{
			CGPROGRAM				
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0
			
			#include "UnityCG.cginc"

			struct v2f 
			{
 				float4 vertex   : SV_POSITION;
				fixed4 color    : COLOR0;
				float2 uv       : TEXCOORD0;
				float4 screenPosition0 : TEXCOORD1;
				float4 screenPosition1 : TEXCOORD2;
			};
			
			uniform float4 _MainTex_ST;
			
			v2f vert (float4 vertex1  : POSITION, 
			          float2 uv       : TEXCOORD0, // texture coordinate
			          float3 vertex2  : TEXCOORD1, // second vertex to compute angle with
					  float  dashSize : TEXCOORD2, // dash-size
					  float4 color    : COLOR0)
			{
				v2f o;

				float4	out_vertex1 = mul(UNITY_MATRIX_MVP, vertex1);
				float4	out_vertex2 = mul(UNITY_MATRIX_MVP, float4(vertex2, 1));
				
				float4	screenPosition0 = ComputeScreenPos(out_vertex1);
				float4	screenPosition1 = ComputeScreenPos(out_vertex2);

				//float2  delta = screenPosition1.xy - screenPosition0.xy;
				//float	sign = cross(float2(0,-1), delta.x) > cross(float2(1, 0), delta.y) ? 1 : -1;

				screenPosition0.xy = screenPosition0.xy * _ScreenParams.xy * 0.25f;
				screenPosition1.xy = screenPosition1.xy * _ScreenParams.xy * 0.25f;
				screenPosition0.w *= dashSize;
				screenPosition1.w *= dashSize;

				o.vertex = out_vertex1;
				o.color	 = color;
				o.uv.xy	 = TRANSFORM_TEX(uv.xy,_MainTex);
				o.screenPosition0 = screenPosition0;
				o.screenPosition1 = screenPosition1;
				return o;
			}
			
			sampler2D _MainTex;

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4	color	= tex2D(_MainTex, i.uv) * i.color;
				
				float2  pos0	= (i.screenPosition1.xy) / (i.screenPosition1.w); 
				float2  pos1	= (i.screenPosition0.xy) / (i.screenPosition0.w);
				float2  delta	= pos1 - pos0;				
				float   value	= length( delta );

				// goes back and forth between dash on / dash off
				// Note: we might want to smooth the subpixel transition between on / off
				float	dist			= frac(step( frac(value), 0.5f) * 0.5f) * 2.0f; 
				color *= dist;
				color.rgb *= color.a;
				return  color;
			}
			ENDCG
		}
	}
}
