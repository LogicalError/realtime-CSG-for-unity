//UNITY_SHADER_NO_UPGRADE
Shader "Hidden/CSG/internal/Background"
{

	Properties
	{
		_MainTex("Base (RGB)", 2D) = "white" {}
	}

	SubShader
	{
		Tags{ "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
		Blend One OneMinusSrcAlpha
		//Cull Off
		ZWrite Off
		ZTest Off
		Lighting Off
		Pass
		{
			CGPROGRAM
#pragma vertex vert_img
#pragma fragment frag

#include "UnityCG.cginc"

			uniform sampler2D _MainTex;
			//uniform float _bwBlend;

			float4 frag(v2f_img i) : COLOR
			{
				float4 c = tex2D(_MainTex, i.uv);
				c.a = 0.3f;
				c.rgb = 0.0f;
				c.rgb *= c.a;
				return c;
			}
			ENDCG
		}
	} 
	/*
	Properties
	{
		_BackgroundColor("BackgroundColor", Color) = (0,0,0,1)
	}

	SubShader
	{
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
		Pass
		{
			Blend One OneMinusSrcAlpha
			ZWrite Off
			ZTest LEqual
			Cull Off
			Lighting Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			struct appdata_t {
				float4 vertex : POSITION;
			};
			struct v2f {
				float4 vertex : SV_POSITION;
			};
			float4 _BackgroundColor;
			v2f vert (appdata_t v)
			{
				v2f o;
#if UNITY_VERSION >= 540
				o.vertex	= UnityObjectToClipPos(v.vertex);
#else
				o.vertex	= mul(UNITY_MATRIX_MVP, v.vertex);
#endif
				//o.vertex.z	= 1.0f;
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				return _BackgroundColor;
			}
			ENDCG  
		}  
	}
	*/
}