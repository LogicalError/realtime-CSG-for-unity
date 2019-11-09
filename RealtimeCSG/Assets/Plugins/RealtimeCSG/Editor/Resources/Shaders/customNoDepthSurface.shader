//UNITY_SHADER_NO_UPGRADE
Shader "Hidden/CSG/internal/customNoDepthSurface"
{
	Properties 
	{
	}
	SubShader 
	{
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
		LOD 200
		Offset -1, -10
		ZTest Off
        Lighting Off
        ZWrite Off
        Cull Off
		Blend One OneMinusSrcAlpha

        Pass 
		{
			CGPROGRAM
				
				#pragma vertex vert
				#pragma fragment frag
			
				#include "UnityCG.cginc"

				struct v2f 
				{
 					float4 pos   : SV_POSITION;
 					fixed4 color : COLOR0;
				};

				v2f vert (appdata_full v)
				{
					v2f o;
					o.pos	= mul (UNITY_MATRIX_MVP, v.vertex);
					//o.pos.z += 10.00105f;	// I would use Offset if it actually worked ..
					o.color = v.color;
					return o;
				}

				fixed4 frag (v2f input) : SV_Target
				{
					fixed4 col = input.color;
					col.rgb *= col.a;
					return col;
				}

			ENDCG
		}
	}
}
