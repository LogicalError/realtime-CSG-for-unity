//UNITY_SHADER_NO_UPGRADE
Shader "Hidden/CSG/internal/Grid"
{
	Properties
	{
		_GridSpacing("Grid Spacing", Float) = (1.0, 1.0, 1.0)
		_GridSubdivisions("Grid Subdivisions", Float) = (4, 4, 0, 0)

		_GridSize("Grid Size", Float) = 1
		_GridColor("Grid Color", Color) = (1.0, 1.0, 1.0, 0.5)
		_CenterColor("Center Color", Color) = (1.0, 1.0, 1.0, 0.75)
		_StartLevel("Start Zoom Level", Float) = 4
//		_LowestLevel("Lowest Level", Float) = -2
		_GridLineThickness("Grid Line Thickness", Float) = 1
		_CenterLineThickness("Center Line Thickness", Float) = 2
		_SubdivisionTransparency("Subdivision Transparency", Float) = 0.7
	}

	SubShader
	{
		Tags
		{ 
			"ForceSupported" = "True" 
			"Queue" = "Overlay+5105" 
			"IgnoreProjector" = "True" 
			"RenderType" = "Transparent" 
			"PreviewType" = "Plane"
			"DisableBatching" = "True"
			"ForceNoShadowCasting" = "True"
			"LightMode" = "Always"
		}

		Pass
		{
			Blend One OneMinusSrcAlpha 
			ColorMask RGB
			Cull Off
			Offset -1,-1
			Lighting Off
			SeparateSpecular Off
			ZTest LEqual
			ZWrite Off

			CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#include "UnityCG.cginc"

				uniform float	_GridLineThickness;
				uniform float	_CenterLineThickness;
				uniform float2	_GridSpacing;
				uniform float2	_GridSubdivisions;

				uniform float	_GridSize;
				 
				uniform fixed4	_GridColor;
				uniform fixed4	_CenterColor;
				uniform float	_StartLevel;
//				uniform float	_LowestLevel;
				uniform float	_SubdivisionTransparency;

				struct vertexInput 
				{
					half4 vertex : POSITION;
				};

				struct vertexOutput 
				{
					float4 pos			: SV_POSITION;
					float2 uv			: TEXCOORD0;
					float4 objectPos	: TEXCOORD1;
					float4 normal		: NORMAL;
				};

				vertexOutput vert(vertexInput input) 
				{
					half3 worldGridCenter	= half4(_WorldSpaceCameraPos.x, 0, _WorldSpaceCameraPos.z, 1);
					float4 objectCenter		= mul(unity_ObjectToWorld, float4(0,0,0,1));
					objectCenter.xyz /= objectCenter.w;
					objectCenter.w = 0.0f;
					
					vertexOutput output;

					//half3 normal		= normalize(mul((float3x3)unity_ObjectToWorld, half3(0, 1, 0)));
					//half4 gridPlane	= half4(normal, -dot(normal, objectCenter));
					//float distance	= (gridPlane.x * _WorldSpaceCameraPos.x) + (gridPlane.y * _WorldSpaceCameraPos.y) + (gridPlane.z * _WorldSpaceCameraPos.z) + (gridPlane.w);					
					//float gridSize	= (max(1, abs(distance)) * 100);

					float gridSize		= max(1, abs(_GridSize)) * 200;

					float3 vposition	= (input.vertex.xzy * gridSize);
					float4 position		= float4(vposition, 1);

					output.objectPos	= mul(unity_ObjectToWorld, float4(vposition, 1));
					output.uv			= position.xz + objectCenter.xz;

					output.normal		= mul(unity_ObjectToWorld, fixed4(0, 1, 0, 0));

					//position += float4(worldGridCenter,1);
					position.y	= 0;
					output.pos	= UnityObjectToClipPos(position);
					return output;
				}

				// sample small and large grid in both x and y direction
				fixed4 lineSampler4(half4 uv, half4 lineSize)
				{
					fixed4 t = abs((frac(uv + lineSize) / lineSize) - 1);
					fixed4 s = saturate(exp(-pow(t, 80)));
					return s;
				}

				// sample center grid lines in both x and y direction
				fixed2 centerLineSampler2(half2 uv, half2 lineSize)
				{
					fixed2 t = abs(((uv + lineSize) / lineSize) - 1);
					fixed2 s = saturate(exp(-pow(t, 80)));
					return s;
				}

				fixed4 gridMultiSampler(float2 uv, half3 camDelta, float camDistance)
				{
					const half4 lowestLevel				= half4(2, 2, 2, 2);//_LowestLevel, _LowestLevel);
					const fixed4 transparency			= half4(1, 1, _SubdivisionTransparency, _SubdivisionTransparency);
					const half4 subdivisions			= half4(1, 1, 1.0 / _GridSubdivisions.xy);
					const half4 gridspacing				= _GridSpacing.xyxy;
					const half  startDepthPow			= pow(2, _StartLevel);
					const half4 constantThickness		= (startDepthPow * (_GridLineThickness / 1000)) * half4(1, 1, _GridSubdivisions.xy * 0.5);
					const half4 startDistance			= startDepthPow * gridspacing;
					const half4 constantStepSize		= gridspacing * subdivisions;
					const half2 constantCenterThickness = (startDepthPow * (_CenterLineThickness / 1000)) * half2(1, 1);


					half4 gridDepth			= log2(_GridSize);//camDistance / startDistance);
					half4 gridDepthClamp	= max(lowestLevel, gridDepth);
					half4 gridDepthFloor	= floor(gridDepthClamp);
					half4 gridDepthFrac		= 1 - (gridDepthClamp - gridDepthFloor);

					half4 stepSize			= constantStepSize * pow(2, gridDepthFloor);
					half2 centerStepSize	= constantStepSize * pow(2, gridDepthClamp);

					half4 lineDepth			= log2(camDistance / startDistance);
					half4 lineDepthClamp	= max(lowestLevel, lineDepth);
					half4 lineSize			= constantThickness       / max(1, pow(2, lineDepthClamp    - lineDepth));
					half2 centerLineSize	= constantCenterThickness / max(1, pow(2, lineDepthClamp.xy - lineDepth.xy));
					
#if SHADER_TARGET < 25 // no anti-aliasing on older hardware because no fwidth!
					float4	values		= uv.xyxy;

					// get the x/y lines for the thick and the thin grid lines
					fixed4	resultB		= lineSampler4(values / stepSize, lineSize);

					// get the x/y lines for the thick and the thin grid lines, one level higher
					stepSize *= 2;
					fixed4	resultA		= lineSampler4(values / stepSize, lineSize);

					// fade in between the grid line levels
					fixed4	result		= lerp(resultA, resultB, gridDepthFrac)
											* transparency; // make the thin lines more transparent

					// get the most visible line for this pixel
					float t = max(max(result.x, result.y), max(result.z, result.w));


					// get the center line thickness
					fixed2	resultC = centerLineSampler2(values/ centerStepSize, centerLineSize.xy);
					float c = lerp(max(resultC.x, resultC.y);
#else

					half4	pixelSize	= fwidth(uv).xyxy;
					
					float4	values		= uv.xyxy;
					half4	position1	= values + (half4( 0.125, -0.375,  0.125, -0.375) * pixelSize);
					half4	position2	= values + (half4(-0.375, -0.125, -0.375, -0.125) * pixelSize);
					half4	position3	= values + (half4( 0.375,  0.125,  0.375,  0.125) * pixelSize);
					half4	position4	= values + (half4(-0.125,  0.375, -0.125,  0.375) * pixelSize);

					// get the x/y lines for the thick and the thin grid lines
					float4	resultB		= lineSampler4(position1 / stepSize, lineSize) +
										  lineSampler4(position2 / stepSize, lineSize) +
										  lineSampler4(position3 / stepSize, lineSize) +
										  lineSampler4(position4 / stepSize, lineSize);
					
					// get the x/y lines for the thick and the thin grid lines, one level higher
					stepSize *= 2;
					float4	resultA		= lineSampler4(position1 / stepSize, lineSize) +
										  lineSampler4(position2 / stepSize, lineSize) +
										  lineSampler4(position3 / stepSize, lineSize) +
										  lineSampler4(position4 / stepSize, lineSize);
					
					// fade in between the grid line levels
					fixed4	result		= lerp(resultA, resultB, gridDepthFrac)
											* transparency
						; // make the thin lines more transparent

					// get the most visible line for this pixel
					float t = max(max(result.x, result.y), max(result.z, result.w)) 

								// subdivide by the number of samples
								/ 4;


					// get the center line thickness
					float2	resultC = centerLineSampler2(position1 / centerStepSize, centerLineSize.xy) +
									  centerLineSampler2(position2 / centerStepSize, centerLineSize.xy) +
									  centerLineSampler2(position3 / centerStepSize, centerLineSize.xy) +
									  centerLineSampler2(position4 / centerStepSize, centerLineSize.xy);
					float c =	max(resultC.x, resultC.y)

								// subdivide by the number of samples
								/ 4;

#endif
					// interpolate between the center grid lines and the regular grid
					return (fixed4(_GridColor.rgb,   _GridColor.a   * t) * (1 - c)) + 
						   (fixed4(_CenterColor.rgb, _CenterColor.a * c) *      c );
				}

				half4 frag(vertexOutput input) : COLOR
				{
					float3	camDelta	= _WorldSpaceCameraPos.xyz - (input.objectPos.xyz / input.objectPos.w);
					float	camDistance = length(camDelta);		
					

					float2	uv			= input.uv;


					// get the color for our pixel
					fixed4	color		= gridMultiSampler(uv, camDelta, camDistance);

					// fade out on grazing angles to hide line aliasing
					float	angle = abs(dot(camDelta / camDistance, input.normal));
					color.a	  *= angle;

					// pre-multiply our rgb color by our alpha
					color.rgb *= color.a;
					return color;
				}
			ENDCG
		}
	}
}