//UNITY_SHADER_NO_UPGRADE
Shader "Hidden/CSG/internal/OrthoGrid"
{
	Properties
	{
		_Alpha("Alpha", Float) = 1
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
				float4 color : COLOR;
			};
			struct v2f {
				fixed4 color : COLOR;
				float4 vertex : SV_POSITION;
			};
			float _Alpha;
			float _Depth;
			v2f vert (appdata_t v)
			{
				v2f o;
#if UNITY_VERSION >= 540
				o.vertex	= UnityObjectToClipPos(v.vertex);
#else
				o.vertex	= mul(UNITY_MATRIX_MVP, v.vertex);
#endif
				o.vertex.z = _Depth;
				o.color		= v.color;
				//o.color.a = 1.0f;
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				float4 color = i.color;
				color.a *= _Alpha;
				color.rgb *= color.a;
				return color;
			}
			ENDCG  
		}  
	}
}
 