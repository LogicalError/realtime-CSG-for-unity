using UnityEngine;
using UnityEditor;
using System;
using System.Globalization;
using System.Collections.Generic;
using RealtimeCSG.Legacy;
using InternalRealtimeCSG;

namespace RealtimeCSG
{
	internal static class PaintUtility
	{

		#region Materials
		private static Material customDotMaterial;
		public static Material CustomDotMaterial
		{
			get
			{
				if (!customDotMaterial)
				{
					customDotMaterial = MaterialUtility.GenerateEditorMaterial("customDot");
				}
				return customDotMaterial;
			}
		}

		private static Material customWireMaterial;
		private static Material CustomWireMaterial
		{
			get
			{
				if (!customWireMaterial)
				{
					customWireMaterial = MaterialUtility.GenerateEditorMaterial("customWire");
				}
				return customWireMaterial;
			}
		}

		/*
		private static Material noZTestGenericLineMaterial;
		//static int thicknessID = -1;
		private static Material NoZTestGenericLineMaterial
		{
			get
			{
				if (!noZTestGenericLineMaterial)
				{
					noZTestGenericLineMaterial = MaterialUtility.GenerateEditorMaterial("NoZTestGenericLine");
					//thicknessID = Shader.PropertyToID("thickness");
				}
				return noZTestGenericLineMaterial;
			}
		}

		private static Material customThickWireMaterial;
		//static int thicknessID = -1;
		private static Material CustomThickWireMaterial
		{
			get
			{
				if (!customThickWireMaterial)
				{
					customThickWireMaterial = MaterialUtility.GenerateEditorMaterial("customThickWire");
					//thicknessID = Shader.PropertyToID("thickness");
				}
				return customThickWireMaterial;
			}
		}
		*/


		private static Material customSurfaceNoDepthMaterial;
		public static Material CustomSurfaceNoDepthMaterial
		{
			get
			{
				if (!customSurfaceNoDepthMaterial)
				{
					customSurfaceNoDepthMaterial = MaterialUtility.GenerateEditorMaterial("customNoDepthSurface");
				}
				return customSurfaceNoDepthMaterial;
			}
		}

		private static Material customWireDottedMaterial;
		private static Material CustomWireDottedMaterial
		{
			get
			{
				if (!customWireDottedMaterial)
				{
					customWireDottedMaterial = MaterialUtility.GenerateEditorMaterial("customWireDotted");
				}
				return customWireDottedMaterial;
			}
		}

		#endregion

		static PointMeshManager pointMeshManager	= new PointMeshManager();

		public static void DrawDoubleDots(Camera camera, Vector3[] positions, float[] sizes, Color[] colors, int points)
		{
			if (Event.current.type != EventType.Repaint)
			{
				return;
			}

			if (positions.Length < points ||
				sizes.Length	 < points ||
				colors.Length	 < (points * 2))
			{
				return;
			}

			pointMeshManager.Begin();
			pointMeshManager.DrawPoints(camera, positions, sizes, colors);
			pointMeshManager.End();
			pointMeshManager.Render(CustomDotMaterial, CustomSurfaceNoDepthMaterial);

			/*
			var right	= camera.transform.right;
			var up		= camera.transform.up;
				
			var p0	= (  right + up);
			var p1	= (  right - up);
			var p2	= (- right - up);
			var p3	= (- right + up);

			if (material && material.SetPass(0))
			{
				GL.Begin(GL.QUADS);
				for (int p = 0, d = 0; p < points; p++, d+=2)
				{
					var position	= positions[p];
					var size		= sizes[p];

					GL.Color(colors[d + 1]);
					{ 
						GL.Vertex(position + (p0 * size));
						GL.Vertex(position + (p1 * size));
						GL.Vertex(position + (p2 * size));
						GL.Vertex(position + (p3 * size));
					}
				}
				GL.End();
			}

			if (material && material.SetPass(0))
			{
				GL.Begin(GL.LINES);
				for (int p = 0, d = 0; p < points; p++, d+=2)
				{
					var position	= positions[p];
					var size		= sizes[p];
					var dp0			= position + (p0 * size);
					var dp1			= position + (p1 * size);
					var dp2			= position + (p2 * size);
					var dp3			= position + (p3 * size);

					GL.Color(colors[d + 0]);
					{ 
						GL.Vertex(dp0); GL.Vertex(dp1);
						GL.Vertex(dp1); GL.Vertex(dp2);
						GL.Vertex(dp2); GL.Vertex(dp3);
						GL.Vertex(dp3); GL.Vertex(dp0);
					}
				}
				GL.End();
			}*/
		}

		public static void DrawDoubleDots(Camera camera, Matrix4x4 matrix, Vector3[] positions, float[] sizes, Color[] colors, int points)
		{
			if (Event.current.type != EventType.Repaint)
			{
				return;
			}

			if (positions.Length < points ||
				sizes.Length	 < points ||
				colors.Length	 < (points * 2))
			{
				return;
			}

			var right	= camera.transform.right;
			var up		= camera.transform.up;
				
			var p0	= (  right + up);
			var p1	= (  right - up);
			var p2	= (- right - up);
			var p3	= (- right + up);
				

			var material = CustomDotMaterial;
			if (material && material.SetPass(0))
			{
				GL.Begin(GL.QUADS);
				for (int p = 0, d = 0; p < points; p++, d+=2)
				{
					var position	= matrix.MultiplyPoint(positions[p]);
					var size		= sizes[p];

					GL.Color(colors[d + 1]);
					{ 
						GL.Vertex(position + (p0 * size));
						GL.Vertex(position + (p1 * size));
						GL.Vertex(position + (p2 * size));
						GL.Vertex(position + (p3 * size));
					}
				}
				GL.End();
			}

			material = CustomSurfaceNoDepthMaterial;
			if (material && material.SetPass(0))
			{
				GL.Begin(GL.LINES);
				for (int p = 0, d = 0; p < points; p++, d+=2)
				{
					var position	= matrix.MultiplyPoint(positions[p]);
					var size		= sizes[p];
					var dp0			= position + (p0 * size);
					var dp1			= position + (p1 * size);
					var dp2			= position + (p2 * size);
					var dp3			= position + (p3 * size);

					GL.Color(colors[d + 0]);
					{ 
						GL.Vertex(dp0); GL.Vertex(dp1);
						GL.Vertex(dp1); GL.Vertex(dp2);
						GL.Vertex(dp2); GL.Vertex(dp3);
						GL.Vertex(dp3); GL.Vertex(dp0);
					}
				}
				GL.End();
			}
		}
		
		public static void SquareDotCap(int controlID, Vector3 position, Quaternion rotation, float size)
		{
			if (Event.current.type != EventType.Repaint)
			{
				return;
			}
			
			var camera	= Camera.current;
			var right	= camera.transform.right;
			var up		= camera.transform.up;
				
			var p0	= position + ((  right + up) * size);
			var p1	= position + ((  right - up) * size);
			var p2	= position + ((- right - up) * size);
			var p3	= position + ((- right + up) * size);
			
			position = Handles.matrix.MultiplyPoint (position);
						
			Color c  = Handles.color * new Color (1, 1, 1, .5f) + (Handles.lighting ? new Color (0,0,0,.5f) : new Color (0,0,0,0)) * new Color (1, 1, 1, 0.99f);

			var material = CustomDotMaterial;
			if (material && material.SetPass(0))
			{
				GL.Begin(GL.QUADS);
				{ 
					GL.Color(c);
					GL.Vertex(p0);
					GL.Vertex(p1);
					GL.Vertex(p2);
					GL.Vertex(p3);
				}
				GL.End();
			}
			material = CustomSurfaceNoDepthMaterial;
			if (material && material.SetPass(0))
			{
				GL.Begin(GL.LINES);
				{
					GL.Color(Color.black);
					GL.Vertex(p0); GL.Vertex(p1);
					GL.Vertex(p1); GL.Vertex(p2);
					GL.Vertex(p2); GL.Vertex(p3);
					GL.Vertex(p3); GL.Vertex(p0);
				}
				GL.End();
			}
		}
		
		public static void DiamondDotCap(int controlID, Vector3 position, Quaternion rotation, float size)
		{
			if (Event.current.type != EventType.Repaint)
			{
				return;
			}
			
			var camera	= Camera.current;
			var right	= camera.transform.right;
			var up		= camera.transform.up;
				
			var p0	= position + ((- right) * size);
			var p1	= position + ((- up   ) * size);
			var p2	= position + ((  right) * size);
			var p3	= position + ((  up   ) * size);
			
			position = Handles.matrix.MultiplyPoint (position);
						
			Color c  = Handles.color * new Color (1, 1, 1, .5f) + (Handles.lighting ? new Color (0,0,0,.5f) : new Color (0,0,0,0)) * new Color (1, 1, 1, 0.99f);

			var material = CustomDotMaterial;
			if (material && material.SetPass(0))
			{
				GL.Begin(GL.QUADS);
				{ 
					GL.Color(c);
					GL.Vertex(p0);
					GL.Vertex(p1);
					GL.Vertex(p2);
					GL.Vertex(p3);
				}
				GL.End();
			}
			material = CustomSurfaceNoDepthMaterial;
			if (material && material.SetPass(0))
			{
				GL.Begin(GL.LINES);
				{
					GL.Color(Color.black);
					GL.Vertex(p0); GL.Vertex(p1);
					GL.Vertex(p1); GL.Vertex(p2);
					GL.Vertex(p2); GL.Vertex(p3);
					GL.Vertex(p3); GL.Vertex(p0);
				}
				GL.End();
			}
		}

		static Vector2[] circlePoints = null;

		static void SetupCirclePoints()
		{
			const int steps = 16;
			circlePoints = new Vector2[steps];
			for (int i= 0; i < steps; i++)
			{
				circlePoints[i] = new Vector2(
						(float)Mathf.Cos((i / (float)steps) * Mathf.PI * 2),
						(float)Mathf.Sin((i / (float)steps) * Mathf.PI * 2)
					);
			}
		}

		public static void CircleDotCap(int controlID, Vector3 position, Quaternion rotation, float size)
		{
			if (Event.current.type != EventType.Repaint)
			{
				return;
			}
            var camera = Camera.current;

            DrawFilledCameraAlignedCircle(camera, position, size);
		}
		
		public static void DrawCameraAlignedCircle(Camera camera, Vector3 position, float size, Color innerColor, Color outerColor)
		{
			var right	= camera.transform.right;
			var up		= camera.transform.up;

			if (circlePoints == null)
				SetupCirclePoints();

			var points = new Vector3[circlePoints.Length];
			for (int i=0;i< circlePoints.Length;i++)
			{
				var circle = circlePoints[i];
				points[i] = position + (((right * circle.x) + (up * circle.y)) * size);
			}
			
			position = Handles.matrix.MultiplyPoint (position);
			
			{
				Color c = outerColor * new Color(1, 1, 1, .5f) + (Handles.lighting ? new Color(0, 0, 0, .5f) : new Color(0, 0, 0, 0)) * new Color(1, 1, 1, 0.99f);

				Handles.color = c;
				for (int i = points.Length - 1, j = 0; j < points.Length; i = j, j++)
				{
					Handles.DrawAAPolyLine(6.0f, points[i], points[j]);
				}
			}

			{
				Color c = innerColor * new Color(1, 1, 1, .5f) + (Handles.lighting ? new Color(0, 0, 0, .5f) : new Color(0, 0, 0, 0)) * new Color(1, 1, 1, 0.99f);

				Handles.color = c;
				for (int i = points.Length - 1, j = 0; j < points.Length; i = j, j++)
				{
					Handles.DrawAAPolyLine(2.0f, points[i], points[j]);
				}
			}
		}
		
		public static void DrawFilledCameraAlignedCircle(Camera camera, Vector3 position, float size)
		{
			var right	= camera.transform.right;
			var up		= camera.transform.up;

			if (circlePoints == null)
				SetupCirclePoints();

			var points = new Vector3[circlePoints.Length];
			for (int i=0;i< circlePoints.Length;i++)
			{
				var circle = circlePoints[i];
				points[i] = position + (((right * circle.x) + (up * circle.y)) * size);
			}
			
			position = Handles.matrix.MultiplyPoint (position);
						
			Color c  = Handles.color * new Color (1, 1, 1, .5f) + (Handles.lighting ? new Color (0,0,0,.5f) : new Color (0,0,0,0)) * new Color (1, 1, 1, 0.99f);

			var material = CustomDotMaterial;
			if (material && material.SetPass(0))
			{
				GL.Begin(GL.TRIANGLES);
				{ 
					GL.Color(c);
					for (int i = 1; i < points.Length - 1; i++)
					{
						GL.Vertex(points[0]);
						GL.Vertex(points[i]);
						GL.Vertex(points[i + 1]);
					}
				}
				GL.End();
			}
			material = CustomSurfaceNoDepthMaterial;
			if (material && material.SetPass(0))
			{
				GL.Begin(GL.LINES);
				{
					GL.Color(Color.black);
					GL.Vertex(points[0]);
					for (int i = 1; i < points.Length; i++)
					{
						GL.Vertex(points[i]);
						GL.Vertex(points[i]);
					}
					GL.Vertex(points[0]);
				}
				GL.End();
			}
		}

		public static void DrawLine(Vector3 from, Vector3 to, Color c)
		{
			if (Event.current.type != EventType.Repaint)
			{
				return;
			}
			
			var material = CustomWireMaterial;
			if (material && material.SetPass(0))
			{
				GL.Begin(GL.LINES);
				GL.Color(c);
				GL.Vertex(from);
				GL.Vertex(to);
				GL.End();
			}
		}

		public static void DrawLine(Vector3 pt0, Vector3 pt1, float thickness, Color color)
		{
			if (Event.current.type != EventType.Repaint)
			{
				return;
			}
				
			var material = MaterialUtility.ZTestGenericLine;
			if (material == null)
				return;

			MaterialUtility.LineAlphaMultiplier = 1.0f;
			MaterialUtility.LineThicknessMultiplier = 1.0f;
			MaterialUtility.LineDashMultiplier = 1.0f;
			MaterialUtility.InitGenericLineMaterial(material);
			
			if (material.SetPass(0))
			{
				thickness *= EditorGUIUtility.pixelsPerPoint;
				thickness *= 100;
				
				var thickness0 = new Vector3(thickness, -1, 0);
				var thickness1 = new Vector3(thickness,  1, 0);
				var thickness2 = new Vector3(thickness,  1, 0);
				var thickness3 = new Vector3(thickness, -1, 0);
						
				GL.Begin(GL.QUADS);
				GL.Color(color);
				
				GL.MultiTexCoord(0, pt0); GL.MultiTexCoord(1, thickness0); GL.Vertex(pt1);
				GL.MultiTexCoord(0, pt0); GL.MultiTexCoord(1, thickness1); GL.Vertex(pt1);
				GL.MultiTexCoord(0, pt1); GL.MultiTexCoord(1, thickness2); GL.Vertex(pt0);
				GL.MultiTexCoord(0, pt1); GL.MultiTexCoord(1, thickness3); GL.Vertex(pt0);
				GL.End();
			}
		}

		public static void DrawLine(Matrix4x4 matrix, Vector3 pt0, Vector3 pt1, float thickness, Color color)
		{
			if (Event.current.type != EventType.Repaint)
			{
				return;
			}
			
			var material = MaterialUtility.ZTestGenericLine;
			if (material == null)
				return;

			MaterialUtility.LineAlphaMultiplier = 1.0f;
			MaterialUtility.LineThicknessMultiplier = 1.0f;
			MaterialUtility.LineDashMultiplier = 1.0f;
			MaterialUtility.InitGenericLineMaterial(material);
			
			if (material.SetPass(0))
			{
				thickness *= EditorGUIUtility.pixelsPerPoint;
				thickness *= 100;

				//var cameraPosition = matrix.inverse.MultiplyPoint(camera.transform.position);
				
				var thickness0 = new Vector3(thickness, -1, 0);
				var thickness1 = new Vector3(thickness,  1, 0);
				var thickness2 = new Vector3(thickness,  1, 0);
				var thickness3 = new Vector3(thickness, -1, 0);

				GL.PushMatrix();
						
				GL.Begin(GL.QUADS);
				GL.Color(color);

				pt0 = matrix.MultiplyPoint(pt0);
				pt1 = matrix.MultiplyPoint(pt1);
				//var diff = pt1 - pt0;
				//GL.MultiTexCoord(1, diff);

				GL.MultiTexCoord(0, pt0); GL.MultiTexCoord(1, thickness0); GL.Vertex(pt1);
				GL.MultiTexCoord(0, pt0); GL.MultiTexCoord(1, thickness1); GL.Vertex(pt1);
				GL.MultiTexCoord(0, pt1); GL.MultiTexCoord(1, thickness2); GL.Vertex(pt0);
				GL.MultiTexCoord(0, pt1); GL.MultiTexCoord(1, thickness3); GL.Vertex(pt0);
				/*
				var	t = Vector3.Dot((cameraPosition - pt0), diff) / Vector3.Dot(diff, diff);
				if (t > 0.0f && t < 1.0f)
				{
					var pt2 = pt0 + (t * diff);
					GL.MultiTexCoord(0, thickness0); GL.Vertex(pt0pt0);
					GL.MultiTexCoord(0, thickness1); GL.Vertex(pt0);
					GL.MultiTexCoord(0, thickness2); GL.Vertex(pt2);
					GL.MultiTexCoord(0, thickness3); GL.Vertex(pt2);
					
					GL.MultiTexCoord(0, thickness0); GL.Vertex(pt2);
					GL.MultiTexCoord(0, thickness1); GL.Vertex(pt2);
					GL.MultiTexCoord(0, thickness2); GL.Vertex(pt1);
					GL.MultiTexCoord(0, thickness3); GL.Vertex(pt1);
				} else
				{
					GL.MultiTexCoord(0, thickness0); GL.Vertex(pt0);
					GL.MultiTexCoord(0, thickness1); GL.Vertex(pt0);
					GL.MultiTexCoord(0, thickness2); GL.Vertex(pt1);
					GL.MultiTexCoord(0, thickness3); GL.Vertex(pt1);
				}
				*/
				GL.End();
				GL.PopMatrix();
			}
		}

		public static void DrawLine(Matrix4x4 matrix, Vector3 from, Vector3 to, Color c)
		{
			if (Event.current.type != EventType.Repaint)
			{
				return;
			}

			var material = CustomWireMaterial;
			if (material && material.SetPass(0))
			{
				GL.PushMatrix();
				GL.MultMatrix(matrix);
				GL.Begin(GL.LINES);
				GL.Color(c);
				GL.Vertex(from);
				GL.Vertex(to);
				GL.End();
				GL.PopMatrix();
			}
		}

		public static void DrawDottedLine(Vector3 from, Vector3 to, Color c, float dotSize = 4.0f)
		{
			//Handles.matrix = matrix;
			Handles.color = c;
			//GL.Color(c);
			Handles.DrawDottedLine(from, to, dotSize);
			/*
			if (Event.current.type != EventType.Repaint)
			{
				return;
			}

			var material = CustomWireDottedMaterial;
			if (material && material.SetPass(0))
			{
				GL.Begin(GL.LINES);
				GL.Color(c);
				GL.MultiTexCoord2(1, dotSize, 0);
				GL.MultiTexCoord(0, from); GL.Vertex(to);
				GL.MultiTexCoord(0, to); GL.Vertex(from);
				GL.End();
			}
			CustomWireMaterial.SetPass(0);*/
		}

		public static void DrawDottedLine(Matrix4x4 matrix, Vector3 from, Vector3 to, Color c, float dotSize = 4.0f)
		{
			var origMatrix = Handles.matrix;
			var prevColor = Handles.color; 
			Handles.matrix = matrix;
			Handles.color = c;
			Handles.DrawDottedLine(from, to, dotSize);
			Handles.color = prevColor;
			Handles.matrix = origMatrix;
		}

		public static void DrawDottedLines(Vector3[] lines, Color c, float dotSize = 4.0f)
		{
			var origMatrix = Handles.matrix;
			var prevColor = Handles.color; 
			Handles.matrix = MathConstants.identityMatrix;
			Handles.color = c;
			Handles.DrawDottedLines(lines, dotSize);
			Handles.color = prevColor;
			Handles.matrix = origMatrix;
			/*
			if (Event.current.type != EventType.Repaint)
			{
				return;
			}

			if ((lines.Length & 1) == 1)
			{
				Debug.LogWarning("Uneven number of vertices in line array!");
			}

			var material = CustomWireDottedMaterial;
			if (material && material.SetPass(0))
			{
				GL.Begin(GL.LINES);
				GL.Color(c);
				GL.MultiTexCoord2(1, dotSize, 0);
				for (int i = 0; i < lines.Length; i += 2)
				{
					var pt0 = lines[i + 0];
					var pt1 = lines[i + 1];
					GL.MultiTexCoord(0, pt1); GL.Vertex(pt0);
					GL.MultiTexCoord(0, pt0); GL.Vertex(pt1);
				}
				GL.End();
			}
			CustomWireMaterial.SetPass(0);
			*/
		}

		public static void DrawDottedLines(Matrix4x4 matrix, Vector3[] lines, Color c, float dotSize = 4.0f)
		{
			Handles.matrix = matrix;
			Handles.color = c;
			//GL.MultMatrix(matrix);
			//GL.Color(c);
			Handles.DrawDottedLines(lines, dotSize);
			/*
			if (Event.current.type != EventType.Repaint)
			{
				return;
			}

			if ((lines.Length & 1) == 1)
			{
				Debug.LogWarning("Uneven number of vertices in line array!");
			}
			
			var material = CustomWireDottedMaterial;
			if (material && material.SetPass(0))
			{
				GL.PushMatrix();
				GL.MultMatrix(matrix);
				GL.Begin(GL.LINES);
				GL.Color(c);
				GL.MultiTexCoord2(1, dotSize, 0);
				for (int i = 0; i < lines.Length; i += 2)
				{
					var pt0 = lines[i + 0];
					var pt1 = lines[i + 1];
					GL.MultiTexCoord(0, pt1); GL.Vertex(pt0);
					GL.MultiTexCoord(0, pt0); GL.Vertex(pt1);
				}
				GL.End();
				GL.PopMatrix();
			}*/
		}
		
		public static void DrawDottedLines(Vector3[] vertices, Int32[] indices, Color[] colors, float sreenSpaceSize = 4.0f)
		{
			if (Event.current.type != EventType.Repaint)
			{
				return;
			}

			if (indices.Length != colors.Length * 2)
			{
				Debug.LogWarning("indices.Length != colors.Length * 2");
				return;
			}

			var material = CustomWireDottedMaterial;
			if (material && material.SetPass(0))
			{
				var dashSize = sreenSpaceSize * EditorGUIUtility.pixelsPerPoint;
				GL.Begin(GL.LINES);
				for (int i = 0, c = 0; i < indices.Length; i += 2, c++)
				{
					var i0 = indices[i + 0];
					var i1 = indices[i + 1];
					if (i0 < 0 || i0 >= vertices.Length ||
						i1 < 0 || i1 >= vertices.Length)
						continue;
					var pt0 = vertices[i0];
					var pt1 = vertices[i1];
					GL.Color(colors[c]);
					GL.MultiTexCoord(1, pt1); GL.MultiTexCoord2(2, dashSize, 0); GL.Vertex(pt0);
					GL.MultiTexCoord(1, pt1); GL.MultiTexCoord2(2, dashSize, 0); GL.Vertex(pt1);
				}
				GL.End();
			}
		}
		
		public static void DrawDottedLines(Vector3[] vertices, Int32[] indices, Color color, float sreenSpaceSize = 4.0f)
		{
			if (Event.current.type != EventType.Repaint)
			{
				return;
			}

			var material = CustomWireDottedMaterial;
			if (material && material.SetPass(0))
			{
				var dashSize = sreenSpaceSize * EditorGUIUtility.pixelsPerPoint;
				GL.Begin(GL.LINES);
				GL.Color(color);
				for (int i = 0, c = 0; i < indices.Length; i += 2, c++)
				{
					var i0 = indices[i + 0];
					var i1 = indices[i + 1];
					var pt0 = vertices[i0];
					var pt1 = vertices[i1];
					GL.MultiTexCoord(1, pt1); GL.MultiTexCoord2(2, dashSize, 0); GL.Vertex(pt0);
					GL.MultiTexCoord(1, pt1); GL.MultiTexCoord2(2, dashSize, 0); GL.Vertex(pt1);
				}
				GL.End();
			}
		}
		
		public static void DrawDottedLines(Matrix4x4 matrix, Vector3[] vertices, Int32[] indices, Color color, float sreenSpaceSize = 4.0f)
		{
			if (Event.current.type != EventType.Repaint)
			{
				return;
			}

			var material = CustomWireDottedMaterial;
			if (material && material.SetPass(0))
			{
				var dashSize = sreenSpaceSize * EditorGUIUtility.pixelsPerPoint;
				GL.PushMatrix();
				GL.MultMatrix(matrix);
				GL.Begin(GL.LINES);
				GL.Color(color);
				for (int i = 0, c = 0; i < indices.Length; i += 2, c++)
				{
					var i0 = indices[i + 0];
					var i1 = indices[i + 1];
					var pt0 = vertices[i0];
					var pt1 = vertices[i1];
					GL.MultiTexCoord(1, pt1); GL.MultiTexCoord2(2, dashSize, 0); GL.Vertex(pt0);
					GL.MultiTexCoord(1, pt1); GL.MultiTexCoord2(2, dashSize, 0); GL.Vertex(pt1);
				}
				GL.End();
				GL.PopMatrix();
			}
		}
		
		public static void DrawDottedLines(Matrix4x4 matrix, Vector3[] vertices, Int32[] indices, Color[] colors, float sreenSpaceSize = 4.0f)
		{
			if (Event.current.type != EventType.Repaint)
			{
				return;
			}

			if (indices.Length != colors.Length * 2)
			{
				Debug.LogWarning("indices.Length != colors.Length * 2");
				return;
			}

			var material = CustomWireDottedMaterial;
			if (material && material.SetPass(0))
			{
				var dashSize = sreenSpaceSize * EditorGUIUtility.pixelsPerPoint;
				GL.PushMatrix();
				GL.MultMatrix(matrix);
				GL.Begin(GL.LINES);
				for (int i = 0, c = 0; i < indices.Length; i += 2, c++)
				{
					var i0 = indices[i + 0];
					var i1 = indices[i + 1];
					var pt0 = vertices[i0];
					var pt1 = vertices[i1];
					GL.Color(colors[c]);
					GL.MultiTexCoord(1, pt1); GL.MultiTexCoord2(2, dashSize, 0); GL.Vertex(pt0);
					GL.MultiTexCoord(1, pt1); GL.MultiTexCoord2(2, dashSize, 0); GL.Vertex(pt1);
				}
				GL.End();
				GL.PopMatrix();
			}
		}
		
		

		public static void DrawLines(Vector3[] lines, Color c)
		{
			if (Event.current.type != EventType.Repaint)
			{
				return;
			}

			if ((lines.Length & 1) == 1)
			{
				Debug.LogWarning("Uneven number of vertices in line array!");
			}

			var material = CustomWireMaterial;
			if (material && material.SetPass(0))
			{
				GL.Begin(GL.LINES);
				GL.Color(c);
				for (int i = 0; i < lines.Length; i += 2)
				{
					var pt0 = lines[i + 0];
					var pt1 = lines[i + 1];
					GL.Vertex(pt0);
					GL.Vertex(pt1);
				}
				GL.End();
			}
		}

		public static void DrawLines(Matrix4x4 matrix, Vector3[] lines, Color c)
		{
			if (Event.current.type != EventType.Repaint)
			{
				return;
			}

			if ((lines.Length & 1) == 1)
			{
				Debug.LogWarning("Uneven number of vertices in line array!");
			}

			var material = CustomWireMaterial;
			if (material && material.SetPass(0))
			{
				GL.PushMatrix();
				GL.MultMatrix(matrix);
				GL.Begin(GL.LINES);
				GL.Color(c);
				for (int i = 0; i < lines.Length; i += 2)
				{
					var pt0 = lines[i + 0];
					var pt1 = lines[i + 1];
					GL.Vertex(pt0);
					GL.Vertex(pt1);
				}
				GL.End();
				GL.PopMatrix();
			}
		}

		public static void DrawLines(Vector3[] vertices, Int32[] indices, Color[] colors)
		{
			if (Event.current.type != EventType.Repaint)
			{
				return;
			}

			if (indices.Length != colors.Length * 2)
			{
				Debug.LogWarning("indices.Length != colors.Length * 2");
				return;
			}

			var material = CustomWireMaterial;
			if (material && material.SetPass(0))
			{
				GL.Begin(GL.LINES);
				for (int i = 0, c = 0; i < indices.Length; i += 2, c++)
				{
					var i0 = indices[i + 0];
					var i1 = indices[i + 1];
					var pt0 = vertices[i0];
					var pt1 = vertices[i1];
					GL.Color(colors[c]);
					GL.Vertex(pt0);
					GL.Vertex(pt1);
				}
				GL.End();
			}
		}
			
		public static void DrawUnoccludedLines(Matrix4x4 matrix, Vector3[] vertices, Int32[] indices, Color color)
		{
			if (Event.current.type != EventType.Repaint)
			{
				return;
			}

			var material = CustomWireMaterial;
			if (material && material.SetPass(0))
			{
				var prevColor = Handles.color;
				var origMatrix = Handles.matrix;
				Handles.matrix = matrix;
				Handles.color = color;
				for (int i = 0, c = 0; i < indices.Length; i += 2, c++)
				{
					var i0 = indices[i + 0];
					var i1 = indices[i + 1];
					var pt0 = vertices[i0];
					var pt1 = vertices[i1];
					Handles.DrawLine(pt0, pt1);
				}
				Handles.color = prevColor;
				Handles.matrix = origMatrix;
			}
		}

		public static void DrawUnoccludedLines(Vector3[] vertices, Int32[] indices, Color color)
		{
			if (Event.current.type != EventType.Repaint)
			{
				return;
			}

			var material = CustomWireMaterial;
			if (material && material.SetPass(0))
			{
				var prevColor = Handles.color;
				Handles.color = color;
				for (int i = 0, c = 0; i < indices.Length; i += 2, c++)
				{
					var i0 = indices[i + 0];
					var i1 = indices[i + 1];
					var pt0 = vertices[i0];
					var pt1 = vertices[i1];
					Handles.DrawLine(pt0, pt1);
				}
				Handles.color = prevColor;
			}
		}

		public static void DrawLines(Vector3[] vertices, Int32[] indices, Color color)
		{
			if (Event.current.type != EventType.Repaint)
			{
				return;
			}

			var material = CustomWireMaterial;
			if (material && material.SetPass(0))
			{
				GL.Begin(GL.LINES);
				GL.Color(color);
				for (int i = 0, c = 0; i < indices.Length; i += 2, c++)
				{
					var i0 = indices[i + 0];
					var i1 = indices[i + 1];
					var pt0 = vertices[i0];
					var pt1 = vertices[i1];
					GL.Vertex(pt0);
					GL.Vertex(pt1);
				}
				GL.End();
			}
		}

		public static void DrawLines(Camera camera, Vector3[] vertices, Int32[] indices, float thickness, Color[] colors)
		{
			if (Event.current.type != EventType.Repaint)
			{
				return;
			}

			if (indices.Length != colors.Length * 2)
			{
				Debug.LogWarning("indices.Length != colors.Length * 2");
				return;
			}
			
			var material = MaterialUtility.ZTestGenericLine;
			if (material == null)
				return;

			MaterialUtility.LineAlphaMultiplier = 1.0f;
			MaterialUtility.LineThicknessMultiplier = 1.0f;
			MaterialUtility.LineDashMultiplier = 1.0f;
			MaterialUtility.InitGenericLineMaterial(material);
			
			if (material.SetPass(0))
			{
				thickness *= EditorGUIUtility.pixelsPerPoint;
				thickness *= 100;

                //var cameraPosition = camera.transform.position;

                var thickness0 = new Vector3(thickness, -1, 0);
				var thickness1 = new Vector3(thickness,  1, 0);
				var thickness2 = new Vector3(thickness,  1, 0);
				var thickness3 = new Vector3(thickness, -1, 0);
								
				GL.Begin(GL.QUADS);
				for (int i = 0, c = 0; i < indices.Length; i += 2, c++)
				{
					var i0 = indices[i + 0];
					var i1 = indices[i + 1];
					if (i0 < 0 || i0 >= vertices.Length ||
						i1 < 0 || i1 >= vertices.Length)
						continue;

					GL.Color(colors[c]);

					var pt0		= vertices[i0];
					var pt1		= vertices[i1];
					//var diff	= pt1 - pt0;

					//GL.MultiTexCoord(1, diff);
					
					GL.MultiTexCoord(0, pt0); GL.MultiTexCoord(1, thickness0); GL.Vertex(pt1);
					GL.MultiTexCoord(0, pt0); GL.MultiTexCoord(1, thickness1); GL.Vertex(pt1);
					GL.MultiTexCoord(0, pt1); GL.MultiTexCoord(1, thickness2); GL.Vertex(pt0);
					GL.MultiTexCoord(0, pt1); GL.MultiTexCoord(1, thickness3); GL.Vertex(pt0);
					/*
					var	t = Vector3.Dot((cameraPosition - pt0), diff) / Vector3.Dot(diff, diff);
					if (t > 0.0f && t < 1.0f)
					{
						var pt2 = pt0 + (t * diff);
						GL.MultiTexCoord(0, thickness0); GL.Vertex(pt0);
						GL.MultiTexCoord(0, thickness1); GL.Vertex(pt0);
						GL.MultiTexCoord(0, thickness2); GL.Vertex(pt2);
						GL.MultiTexCoord(0, thickness3); GL.Vertex(pt2);
					
						GL.MultiTexCoord(0, thickness0); GL.Vertex(pt2);
						GL.MultiTexCoord(0, thickness1); GL.Vertex(pt2);
						GL.MultiTexCoord(0, thickness2); GL.Vertex(pt1);
						GL.MultiTexCoord(0, thickness3); GL.Vertex(pt1);
					} else
					{
						GL.MultiTexCoord(0, thickness0); GL.Vertex(pt0);
						GL.MultiTexCoord(0, thickness1); GL.Vertex(pt0);
						GL.MultiTexCoord(0, thickness2); GL.Vertex(pt1);
						GL.MultiTexCoord(0, thickness3); GL.Vertex(pt1);
					}*/
				}
				GL.End();
			}
		}

		public static void DrawLines(Matrix4x4 matrix, Vector3[] vertices, Int32[] indices, Color[] colors)
		{
			if (Event.current.type != EventType.Repaint)
			{
				return;
			}

			if (indices.Length != colors.Length * 2)
			{
				Debug.LogWarning("indices.Length != colors.Length * 2");
				return;
			}

			var material = CustomWireMaterial;
			if (material && material.SetPass(0))
			{
				GL.PushMatrix();
				GL.MultMatrix(matrix);
				GL.Begin(GL.LINES);
				for (int i = 0, c = 0; i < indices.Length; i += 2, c++)
				{
					var i0 = indices[i + 0];
					var i1 = indices[i + 1];
					var pt0 = vertices[i0];
					var pt1 = vertices[i1];
					GL.Color(colors[c]);
					GL.Vertex(pt0);
					GL.Vertex(pt1);
				}
				GL.End();
				GL.PopMatrix();
			}
		}

		public static void DrawLines(Matrix4x4 matrix, Vector3[] vertices, Int32[] indices, int lineCount, Color c)
		{
			if (Event.current.type != EventType.Repaint)
			{
				return;
			}

			if (lineCount == 0)
			{
				return;
			}

			if (indices.Length < lineCount * 2)
			{
				Debug.LogWarning("indices.Length < lineCount * 2");
				return;
			}

			var material = CustomWireMaterial;
			if (material && material.SetPass(0))
			{
				GL.PushMatrix();
				GL.MultMatrix(matrix);
				GL.Begin(GL.LINES);
				GL.Color(c);
				var indexCount = lineCount * 2;
				for (int i = 0; i < indexCount; i += 2)
				{
					var i0 = indices[i + 0];
					var i1 = indices[i + 1];
					var pt0 = vertices[i0];
					var pt1 = vertices[i1];
					GL.Vertex(pt0);
					GL.Vertex(pt1);
				}
				GL.End();
				GL.PopMatrix();
			}
		}
		

		public static void DrawLines(Matrix4x4 matrix, Vector3[] vertices, Int32[] indices, Color c)
		{
			if (Event.current.type != EventType.Repaint)
			{
				return;
			}
			
			if ((indices.Length & 1) != 0)
			{
				Debug.LogWarning("indices.Length is uneven number");
				return;
			}

			var material = CustomWireMaterial;
			if (material && material.SetPass(0))
			{
				GL.PushMatrix();
				GL.MultMatrix(matrix);
				GL.Begin(GL.LINES);
				GL.Color(c);
				for (int i = 0; i < indices.Length; i += 2)
				{
					var i0 = indices[i + 0];
					var i1 = indices[i + 1];
					var pt0 = vertices[i0];
					var pt1 = vertices[i1];
					GL.Vertex(pt0);
					GL.Vertex(pt1);
				}
				GL.End();
				GL.PopMatrix();
			}
		}		

		public static void DrawLines(Matrix4x4 matrix, Vector3[] vertices, Int32[] indices, float thickness, Color[] colors)
		{
			if (Event.current.type != EventType.Repaint)
			{
				return;
			}

			if (indices.Length != colors.Length * 2)
			{
				Debug.LogWarning("indices.Length != colors.Length * 2");
				return;
			}

			var material = MaterialUtility.ZTestGenericLine;
			if (material == null)
				return;

			MaterialUtility.LineAlphaMultiplier = 1.0f;
			MaterialUtility.LineThicknessMultiplier = 1.0f;
			MaterialUtility.LineDashMultiplier = 1.0f;
			MaterialUtility.InitGenericLineMaterial(material);
			
			if (material.SetPass(0))
			{
				thickness *= EditorGUIUtility.pixelsPerPoint;
				thickness *= 100;

                //var cameraPosition = matrix.inverse.MultiplyPoint(camera.transform.position);

                var thickness0 = new Vector3(thickness, -1, 0);
				var thickness1 = new Vector3(thickness,  1, 0);
				var thickness2 = new Vector3(thickness,  1, 0);
				var thickness3 = new Vector3(thickness, -1, 0);

				GL.PushMatrix();
				GL.MultMatrix(matrix);

				GL.Begin(GL.QUADS);
				for (int i = 0, c = 0; i < indices.Length; i += 2, c++)
				{
					var i0		= indices[i + 0];
					var i1		= indices[i + 1];
					var pt0		= vertices[i0];
					var pt1		= vertices[i1];
					//var diff	= pt1 - pt0;

					GL.Color(colors[c]);
					//GL.MultiTexCoord(1, diff);
					
					GL.MultiTexCoord(0, pt0); GL.MultiTexCoord(1, thickness0); GL.Vertex(pt1);
					GL.MultiTexCoord(0, pt0); GL.MultiTexCoord(1, thickness1); GL.Vertex(pt1);
					GL.MultiTexCoord(0, pt1); GL.MultiTexCoord(1, thickness2); GL.Vertex(pt0);
					GL.MultiTexCoord(0, pt1); GL.MultiTexCoord(1, thickness3); GL.Vertex(pt0);
					/*
					var	t = Vector3.Dot((cameraPosition - pt0), diff) / Vector3.Dot(diff, diff);
					if (t > 0.0f && t < 1.0f)
					{
						var pt2 = pt0 + (t * diff);
						GL.MultiTexCoord(0, thickness0); GL.Vertex(pt0);
						GL.MultiTexCoord(0, thickness1); GL.Vertex(pt0);
						GL.MultiTexCoord(0, thickness2); GL.Vertex(pt2);
						GL.MultiTexCoord(0, thickness3); GL.Vertex(pt2);
					
						GL.MultiTexCoord(0, thickness0); GL.Vertex(pt2);
						GL.MultiTexCoord(0, thickness1); GL.Vertex(pt2);
						GL.MultiTexCoord(0, thickness2); GL.Vertex(pt1);
						GL.MultiTexCoord(0, thickness3); GL.Vertex(pt1);
					} else
					{
						GL.MultiTexCoord(0, thickness0); GL.Vertex(pt0);
						GL.MultiTexCoord(0, thickness1); GL.Vertex(pt0);
						GL.MultiTexCoord(0, thickness2); GL.Vertex(pt1);
						GL.MultiTexCoord(0, thickness3); GL.Vertex(pt1);
					}*/
				}
				GL.End();
				GL.PopMatrix();
			}
		}

		public static void DrawLines(Vector3[] vertices, float thickness, Color color)
		{
			if (Event.current.type != EventType.Repaint)
			{
				return;
			}
						
			var material = MaterialUtility.ZTestGenericLine;
			if (material == null)
				return;

			MaterialUtility.LineAlphaMultiplier = 1.0f;
			MaterialUtility.LineThicknessMultiplier = 1.0f;
			MaterialUtility.LineDashMultiplier = 1.0f;
			MaterialUtility.InitGenericLineMaterial(material);
			
			if (material.SetPass(0))
			{
				thickness *= EditorGUIUtility.pixelsPerPoint;
				thickness *= 100;

                //var cameraPosition = camera.transform.position;

                var thickness0 = new Vector3(thickness, -1, 0);
				var thickness1 = new Vector3(thickness,  1, 0);
				var thickness2 = new Vector3(thickness,  1, 0);
				var thickness3 = new Vector3(thickness, -1, 0);
				
				GL.Begin(GL.QUADS);
				GL.Color(color);
				for (int i = 0, c = 0; i < vertices.Length; i += 2, c++)
				{
					var pt0		= vertices[i + 0];
					var pt1		= vertices[i + 1];
					//var diff	= pt1 - pt0;
					//GL.MultiTexCoord(1, diff);
					
					GL.MultiTexCoord(0, pt0); GL.MultiTexCoord(1, thickness0); GL.Vertex(pt1);
					GL.MultiTexCoord(0, pt0); GL.MultiTexCoord(1, thickness1); GL.Vertex(pt1);
					GL.MultiTexCoord(0, pt1); GL.MultiTexCoord(1, thickness2); GL.Vertex(pt0);
					GL.MultiTexCoord(0, pt1); GL.MultiTexCoord(1, thickness3); GL.Vertex(pt0);
					/*
					var	t = Vector3.Dot((cameraPosition - pt0), diff) / Vector3.Dot(diff, diff);
					if (t > 0.0f && t < 1.0f)
					{
						var pt2 = pt0 + (t * diff);
						GL.MultiTexCoord(0, thickness0); GL.Vertex(pt0);
						GL.MultiTexCoord(0, thickness1); GL.Vertex(pt0);
						GL.MultiTexCoord(0, thickness2); GL.Vertex(pt2);
						GL.MultiTexCoord(0, thickness3); GL.Vertex(pt2);
					
						GL.MultiTexCoord(0, thickness0); GL.Vertex(pt2);
						GL.MultiTexCoord(0, thickness1); GL.Vertex(pt2);
						GL.MultiTexCoord(0, thickness2); GL.Vertex(pt1);
						GL.MultiTexCoord(0, thickness3); GL.Vertex(pt1);
					} else
					{
						GL.MultiTexCoord(0, thickness0); GL.Vertex(pt0);
						GL.MultiTexCoord(0, thickness1); GL.Vertex(pt0);
						GL.MultiTexCoord(0, thickness2); GL.Vertex(pt1);
						GL.MultiTexCoord(0, thickness3); GL.Vertex(pt1);
					}*/
				}
				GL.End();
			}
		}

		const float kHandleSize = 80.0f;
		const float kMinScreenDist = 0.0001f;
		
		static void GetHandleSizes(Camera cam, Matrix4x4 matrix, Vector3[] positions, float[] sizes, float thickness)
		{
			Matrix4x4 invMatrix = matrix.inverse;
			Transform tr		= cam.transform;
			Vector3 camPos		= tr.position;
			Vector3 invCamPos	= invMatrix.MultiplyPoint(camPos);
			Vector3 forward		= tr.TransformDirection(MathConstants.forwardVector3);
			Vector3 right		= tr.TransformDirection(MathConstants.rightVector3);
			Vector3 invForward	= invMatrix.MultiplyVector(forward);
			var clipmatrix		= cam.projectionMatrix * cam.worldToCameraMatrix;

			//_ScreenParams.x
			float width = cam.pixelRect.width * 0.5f;
			//_ScreenParams.y
//			float height = cam.pixelRect.height * 0.5f;

			float m00 = clipmatrix.m00, m30 = clipmatrix.m30;
			float m01 = clipmatrix.m01, m31 = clipmatrix.m31;
			float m02 = clipmatrix.m02, m32 = clipmatrix.m32;
			float m03 = clipmatrix.m03, m33 = clipmatrix.m33;
			
			float pixelsPerPoint = (thickness * kHandleSize) * EditorGUIUtility.pixelsPerPoint;
			
			for (int i = 0; i < positions.Length; i++)
			{
				float distance = Vector3.Dot(positions[i] - invCamPos, invForward);
				var p1 = camPos + (forward * distance);
				var p2 = p1 + right;

				float w1 = m30 * p1.x + m31 * p1.y + m32 * p1.z + m33;
				float w2 = m30 * p2.x + m31 * p2.y + m32 * p2.z + m33;

				float inv_w1 = (Mathf.Abs(w1) <= 1.0e-7f) ? 0 : (1.0f / w1);
				float inv_w2 = (Mathf.Abs(w2) <= 1.0e-7f) ? 0 : (1.0f / w2);

				float p1x = m00 * p1.x + m01 * p1.y + m02 * p1.z + m03;
				float p2x = m00 * p2.x + m01 * p2.y + m02 * p2.z + m03;

				float x = ((p1x * inv_w1) - (p2x * inv_w2)) * width;

				sizes[i] = pixelsPerPoint / Mathf.Max(kMinScreenDist, Mathf.Sqrt(x * x));
			}
		}
		
		public static void DrawLines(Matrix4x4 matrix, Vector3[] vertices, Int32[] indices, float thickness, Color color)//1
		{
			if (Event.current.type != EventType.Repaint)
			{
				return;
			}

			var material = MaterialUtility.ZTestGenericLine;
			if (!material)
				return;

			MaterialUtility.LineAlphaMultiplier = 1.0f;
			MaterialUtility.LineThicknessMultiplier = 1.0f;
			MaterialUtility.LineDashMultiplier = 1.0f;
			MaterialUtility.InitGenericLineMaterial(material);
			
			if (material.SetPass(0))
			{
				thickness *= EditorGUIUtility.pixelsPerPoint;
				thickness *= 100;
				
				var thickness0 = new Vector3(thickness, -1, 0);
				var thickness1 = new Vector3(thickness, +1, 0);
				var thickness2 = new Vector3(thickness, +1, 0);
				var thickness3 = new Vector3(thickness, -1, 0);

				GL.PushMatrix();
				
				GL.Begin(GL.QUADS);
				GL.Color(color);
				for (int i = 0, c = 0; i < indices.Length; i += 2, c++)
				{
					var i0		= indices[i + 0];
					var i1		= indices[i + 1];
					var pt0		= matrix.MultiplyPoint(vertices[i0]);
					var pt1		= matrix.MultiplyPoint(vertices[i1]);

					GL.MultiTexCoord(0, pt0); GL.MultiTexCoord(1, thickness0); GL.Vertex(pt1);
					GL.MultiTexCoord(0, pt0); GL.MultiTexCoord(1, thickness1); GL.Vertex(pt1);
					GL.MultiTexCoord(0, pt1); GL.MultiTexCoord(1, thickness2); GL.Vertex(pt0);
					GL.MultiTexCoord(0, pt1); GL.MultiTexCoord(1, thickness3); GL.Vertex(pt0);
				}
				GL.End();
				GL.PopMatrix();
			}
		}
		
		public static void DrawLines(Vector3[] vertices, Int32[] indices, float thickness, Color color)
		{
			if (Event.current.type != EventType.Repaint)
			{
				return;
			}
			
			var material = MaterialUtility.ZTestGenericLine;
			if (material == null)
				return;

			MaterialUtility.LineAlphaMultiplier = 1.0f;
			MaterialUtility.LineThicknessMultiplier = 1.0f;
			MaterialUtility.LineDashMultiplier = 1.0f;
			MaterialUtility.InitGenericLineMaterial(material);

			if (material.SetPass(0))
			{
				thickness *= EditorGUIUtility.pixelsPerPoint;
				thickness *= 100;
				
				var thickness0 = new Vector3(thickness, -1, 0);
				var thickness1 = new Vector3(thickness,  1, 0);
				var thickness2 = new Vector3(thickness,  1, 0);
				var thickness3 = new Vector3(thickness, -1, 0);

				GL.Begin(GL.QUADS);
				GL.Color(color);
				for (int i = 0, c = 0; i < indices.Length; i += 2, c++)
				{
					var i0		= indices[i + 0];
					var i1		= indices[i + 1];
					var pt0		= vertices[i0];
					var pt1		= vertices[i1];

					GL.MultiTexCoord(0, pt0); GL.MultiTexCoord(1, thickness0); GL.Vertex(pt1);
					GL.MultiTexCoord(0, pt0); GL.MultiTexCoord(1, thickness1); GL.Vertex(pt1);
					GL.MultiTexCoord(0, pt1); GL.MultiTexCoord(1, thickness2); GL.Vertex(pt0);
					GL.MultiTexCoord(0, pt1); GL.MultiTexCoord(1, thickness3); GL.Vertex(pt0);
				}
				GL.End();
			}
		}

		public static void DrawTriangles(Matrix4x4 matrix, Vector3[] vertices, int[] triangleIndices, Color c)
		{
			if (Event.current.type != EventType.Repaint)
			{
				return;
			}
			
			if ((triangleIndices.Length % 3) != 0)
			{
				Debug.LogWarning("triangleIndices.Length is not dividable by 3");
				return;
			}

			var material = MaterialUtility.ColoredPolygonMaterial;
			if (material && material.SetPass(0))
			{
				GL.PushMatrix(); 
				GL.MultMatrix(matrix);
				GL.Begin(GL.TRIANGLES);
				GL.Color(c);
				for (int i = 0; i < triangleIndices.Length; i ++)
				{
					var i0 = triangleIndices[i];
					var pt0 = vertices[i0];
					GL.Vertex(pt0);
				}
				GL.End();
				GL.PopMatrix();
			}
		}

		public static void DrawPolygon(Vector3[] vertices, Color c)
		{
			if (Event.current.type != EventType.Repaint)
			{
				return;
			}
			
			if (vertices.Length < 3)
			{
				Debug.LogWarning("vertices.Length is smaller than 3");
				return;
			}

			var material = MaterialUtility.ColoredPolygonMaterial;
			if (material && material.SetPass(0))
			{
				GL.Begin(GL.TRIANGLES);
				GL.Color(c);
				var v0 = vertices[0];
				var v1 = vertices[1];
				for (int i = 2; i < vertices.Length; i ++)
				{
					var v2 = vertices[i];
					GL.Vertex(v0);
					GL.Vertex(v1);
					GL.Vertex(v2);
					v1 = v2;
				}
				GL.End();
			}
		}

		public static void DrawPolygon(Matrix4x4 matrix, Vector3[] vertices, Color c)
		{
			if (Event.current.type != EventType.Repaint)
			{
				return;
			}
			
			if (vertices.Length < 3)
			{
				Debug.LogWarning("vertices.Length is smaller than 3");
				return;
			}

			var material = MaterialUtility.ColoredPolygonMaterial;
			if (material && material.SetPass(0))
			{
				GL.PushMatrix(); 
				GL.MultMatrix(matrix);
				GL.Begin(GL.TRIANGLES);
				GL.Color(c);
				var v0 = vertices[0];
				var v1 = vertices[1];
				for (int i = 2; i < vertices.Length; i ++)
				{
					var v2 = vertices[i];
					GL.Vertex(v0);
					GL.Vertex(v1);
					GL.Vertex(v2);
					v1 = v2;
				}
				GL.End();
				GL.PopMatrix();
			}
		}
		public static void DrawDottedWirePolygon(Vector3[] vertices, Color c, float sreenSpaceSize = 4.0f)
		{
			if (Event.current.type != EventType.Repaint)
			{
				return;
			}

			if (vertices.Length < 3)
			{
				Debug.LogWarning("vertices.Length is smaller than 3");
				return;
			}


			var material = CustomWireDottedMaterial;
			if (material && material.SetPass(0))
			{
				var dashSize = sreenSpaceSize * EditorGUIUtility.pixelsPerPoint;
				GL.Begin(GL.LINES);
				GL.Color(c);
				var pt0 = vertices[vertices.Length - 1];
				for (int i = 0; i < vertices.Length; i ++)
				{
					var pt1 = vertices[i];
					GL.MultiTexCoord(1, pt1); GL.MultiTexCoord2(2, dashSize, 0); GL.Vertex(pt0);
					GL.MultiTexCoord(1, pt1); GL.MultiTexCoord2(2, dashSize, 0); GL.Vertex(pt1);
					pt0 = pt1;
				}
				GL.End();
			}
		}

		public static void DrawWirePolygon(Vector3[] vertices, Color c)
		{
			if (Event.current.type != EventType.Repaint)
			{
				return;
			}
			
			if (vertices.Length < 3)
			{
				Debug.LogWarning("vertices.Length is smaller than 3");
				return;
			}

			var material = MaterialUtility.ColoredPolygonMaterial;
			if (material && material.SetPass(0))
			{
				GL.Begin(GL.LINES);
				GL.Color(c);
				var v0 = vertices[vertices.Length - 1];
				for (int i = 0; i < vertices.Length; i ++)
				{
					var v1 = vertices[i];
					GL.Vertex(v0);
					GL.Vertex(v1);
					v0 = v1;
				}
				GL.End();
			}
		}

		public static void DrawWirePolygon(Matrix4x4 matrix, Vector3[] vertices, Color c)
		{
			if (Event.current.type != EventType.Repaint)
			{
				return;
			}
			
			if (vertices.Length < 3)
			{
				Debug.LogWarning("vertices.Length is smaller than 3");
				return;
			}

			var material = MaterialUtility.ColoredPolygonMaterial;
			if (material && material.SetPass(0))
			{
				GL.PushMatrix(); 
				GL.MultMatrix(matrix);
				GL.Begin(GL.LINES);
				GL.Color(c);
				var v0 = vertices[vertices.Length - 1];
				for (int i = 0; i < vertices.Length; i ++)
				{
					var v1 = vertices[i];
					GL.Vertex(v0);
					GL.Vertex(v1);
					v0 = v1;
				}
				GL.End();
				GL.PopMatrix();
			}
		}

		public static void DrawDottedLines(Matrix4x4 matrix, Vector3[] vertices, Int32[] indices, int lineCount, Color c, float dotSize = 4.0f)
		{
			Handles.matrix = matrix;
			Handles.color = c;
			//GL.MultMatrix(matrix);
			//GL.Color(c);
			Handles.DrawDottedLines(vertices, indices, dotSize);
			/*
			if (Event.current.type != EventType.Repaint)
			{
				return;
			}

			if (lineCount == 0)
			{
				return;
			}

			if (indices.Length < lineCount * 2)
			{
				Debug.LogWarning("indices.Length < lineCount * 2");
				return;
			}
			
			var material = CustomWireDottedMaterial;
			if (material && material.SetPass(0))
			{
				GL.PushMatrix();
				GL.MultMatrix(matrix);
				GL.Begin(GL.LINES);
				GL.Color(c);
				GL.MultiTexCoord2(1, dotSize, 0);
				var indexCount = lineCount * 2;
				for (int i = 0; i < indexCount; i += 2)
				{
					var pt0 = vertices[indices[i + 0]];
					var pt1 = vertices[indices[i + 1]];
					GL.MultiTexCoord(0, pt1); GL.Vertex(pt0);
					GL.MultiTexCoord(0, pt0); GL.Vertex(pt1);
				}
				GL.End();
				GL.PopMatrix();
			}*/
			}

		public static void DrawWorldText(Camera camera, Vector3 center, GUIContent content, GUIStyle style = null)
		{
			var screenPoint = camera.WorldToScreenPoint(center);
			if (screenPoint.z < 0)
				return;
			
			var prevMatrix = Handles.matrix;
			Handles.matrix = Matrix4x4.identity;


			var pos2D = HandleUtility.WorldToGUIPoint(center);
			
			if (style == null)
				style = CSG_GUIStyleUtility.sceneTextLabel;

			var calcSize	= style.CalcSize(content);
			Handles.BeginGUI();
			GUI.Label(new Rect(pos2D.x - (calcSize.x * 0.5f), 
							   pos2D.y - (calcSize.y * 0.5f), 
							   calcSize.x, calcSize.y), content, style);
			Handles.EndGUI();
			
			Handles.matrix = prevMatrix;
		}

		public static void DrawWorldText(Camera camera, Vector3 center, string text, GUIStyle style = null)
		{
			DrawWorldText(camera, center, new GUIContent(text), style);
		}
		
		public static void DrawScreenText(Vector2 center, GUIContent content, GUIStyle style = null)
		{
			Handles.BeginGUI();
			if (style == null)
				style = CSG_GUIStyleUtility.sceneTextLabel;
			var calcSize = style.CalcSize(content);
			GUI.Label(new Rect(center.x - (calcSize.x * 0.5f), 
							   center.y - (calcSize.y * 0.5f), 
							   calcSize.x + 4, calcSize.y + 4), content, style);
			Handles.EndGUI();
		}
		
		public static void DrawScreenText(Vector2 center, string text, GUIStyle style = null)
		{
			DrawScreenText(center, new GUIContent(text), style);
		}

		//HoverTextDistance * 2
		public static void DrawScreenText(Camera camera, Vector3 center, float distance, string text, GUIStyle style = null)
		{
            var screenPoint = camera.WorldToScreenPoint(center);
			if (screenPoint.z < 0)
				return;
			
			//EditorGUIUtility.ScreenToGUIPoint(screenPoint); // broken
			

			var prevMatrix = Handles.matrix;
			Handles.matrix = Matrix4x4.identity;
			
			var textCenter2D = HandleUtility.WorldToGUIPoint(center);
			textCenter2D.y += distance;

			var textCenterRay	= HandleUtility.GUIPointToWorldRay(textCenter2D);
			var textCenter		= textCenterRay.origin + textCenterRay.direction * ((camera.farClipPlane + camera.nearClipPlane) * 0.5f);

			Handles.color = Color.black;
			Handles.DrawLine(center, textCenter);
			PaintUtility.DrawScreenText(textCenter2D, text);
			
			Handles.matrix = prevMatrix;
		}

		
		const float hatch_distance = 0.06f;

		static int GetHatchCountForRadius(Vector3 center, float radius)
		{

			var prevMatrix = Handles.matrix;
			Handles.matrix = Matrix4x4.identity;

			float handleSize = HandleUtility.GetHandleSize(center);
			int hatch_count		= 0;
			var circumference	= (float)(2 * Math.PI * radius);
			if (circumference > (handleSize * 2.0f))
			{
				int possible_steps_of_hatch_distance = Mathf.FloorToInt(circumference / (handleSize * hatch_distance));
				if (possible_steps_of_hatch_distance > 0.0f)
				{ 
					hatch_count = possible_steps_of_hatch_distance;
					if		(hatch_count >= 360) hatch_count = 360;
					else if (hatch_count >=  72) hatch_count =  72;
					else if (hatch_count >=  24) hatch_count =  24;
					else if (hatch_count >=   8) hatch_count =   8;
					else if (hatch_count >=   4) hatch_count =   4;
					else hatch_count = 0;
				}
			}
			
			Handles.matrix = prevMatrix;
			return hatch_count;
		}
		

		public static void DrawRotateCirclePie(Vector3 center, Vector3 normal, Vector3 tangent, float radius, 
											   float originalAngle, float startAngle, float endAngle, 
											   Color color)
											   //Color innerColor, Color outerColor)
		{
			var originalMatrix = Handles.matrix;
			Handles.matrix = MathConstants.identityMatrix;

			Color innerColor = color; innerColor.a *= 0.75f;
			Color outerColor = color;

			var hatch_count		= GetHatchCountForRadius(center, radius);
			var innerDistance	= tangent * radius;
			var outerDistance	= tangent * (radius * 1.25f);
			var deltaAngle		= endAngle - startAngle;
			if (deltaAngle >  360) deltaAngle =  360;
			if (deltaAngle < -360) deltaAngle = -360;

			endAngle = startAngle + deltaAngle;
				
			var startPointO	= center + (Quaternion.AngleAxis(startAngle, normal) * outerDistance);
			var endPointO	= center + (Quaternion.AngleAxis(endAngle, normal) * outerDistance);
				
			var startPointI	= center + (Quaternion.AngleAxis(startAngle, normal) * innerDistance);
			var endPointI	= center + (Quaternion.AngleAxis(endAngle, normal) * innerDistance);
				
			Handles.color = outerColor;
			Handles.DrawLine(startPointI, startPointO);
			Handles.DrawLine(endPointI, endPointO);


			var material = CustomSurfaceNoDepthMaterial;
			if (material && material.SetPass(0))
			{	
				GL.Begin(GL.QUADS);
				GL.Color(innerColor);
				
				float step;
				if (hatch_count > 0)
				{
					if (hatch_count < 2)
						step = 360.0f;
					else
						step = 360.0f / hatch_count;
				} else
					step	= 15;
					
				if (deltaAngle < 0)
				{
					Vector3 vectorI0 = center + (Quaternion.AngleAxis(startAngle, normal) * innerDistance);
					Vector3 vectorO0 = center + (Quaternion.AngleAxis(startAngle, normal) * outerDistance);

					var steps	= Mathf.Floor(-deltaAngle / step * 4);
					for (float a = 1; a <= steps; a ++)
					{
						Vector3 vectorI1 = center + (Quaternion.AngleAxis(startAngle + (a * (-step * 0.25f)), normal) * innerDistance);
						Vector3 vectorO1 = center + (Quaternion.AngleAxis(startAngle + (a * (-step * 0.25f)), normal) * outerDistance);

						GL.Vertex(vectorO1);
						GL.Vertex(vectorO0);
						GL.Vertex(vectorI0);
						GL.Vertex(vectorI1);

						vectorI0 = vectorI1;
						vectorO0 = vectorO1;
					}
					if ((steps * step) + deltaAngle > MathConstants.EqualityEpsilon)
					{
						GL.Vertex(vectorO0);
						GL.Vertex(endPointO);
						GL.Vertex(endPointI);
						GL.Vertex(vectorI0);
					}
				} else
				{
					Vector3 vectorO0 = center + (Quaternion.AngleAxis(startAngle, normal) * outerDistance);
					Vector3 vectorI0 = center + (Quaternion.AngleAxis(startAngle, normal) * innerDistance);
					float steps = Mathf.Floor(deltaAngle / step * 4);
					for (float a = 1; a <= steps; a ++)
					{
						Vector3 vectorI1 = center + (Quaternion.AngleAxis(startAngle + (a * (step * 0.25f)), normal) * innerDistance);
						Vector3 vectorO1 = center + (Quaternion.AngleAxis(startAngle + (a * (step * 0.25f)), normal) * outerDistance);

						GL.Vertex(vectorO0);
						GL.Vertex(vectorO1);
						GL.Vertex(vectorI1);
						GL.Vertex(vectorI0);

						vectorO0 = vectorO1;
						vectorI0 = vectorI1;
					}
					if ((steps * step) + deltaAngle > MathConstants.EqualityEpsilon)
					{
						Vector3 deltaI = center + (Quaternion.AngleAxis(endAngle, normal) * innerDistance);
						Vector3 deltaO = center + (Quaternion.AngleAxis(endAngle, normal) * outerDistance);
						GL.Vertex(vectorO0);
						GL.Vertex(deltaO);
						GL.Vertex(deltaI);
						GL.Vertex(vectorI0);
					}
				}
				GL.End();
			}
			
			Handles.matrix = originalMatrix;
		}
		
		public static Vector3 HandlePivot(Camera camera, Vector3 center, Quaternion rotation, Color color, bool active)
		{
			if (Event.current.type == EventType.Repaint)
			{
				float handleSize = CSG_HandleUtility.GetHandleSize(center);

				var originalColor = Handles.color;

				Handles.color = color;
				DrawFilledCameraAlignedCircle(camera, center, handleSize * GUIConstants.handleScale * 1.00f);

				DrawCameraAlignedCircle(camera, center, handleSize * GUIConstants.handleScale * 4.0f, Color.white, Color.black);

				Handles.color = originalColor;
			}
			if (!active)
				return center;
			
			var activeSnappingMode	= RealtimeCSG.CSGSettings.ActiveSnappingMode;
			return RealtimeCSG.Helpers.CSGHandles.PositionHandle(camera, center, rotation, activeSnappingMode);
		}

		public static Vector3 HandlePosition(Camera camera, Vector3 center, Quaternion rotation, Vector3[] snapVertices = null, RealtimeCSG.Helpers.CSGHandles.InitFunction initFunction = null, RealtimeCSG.Helpers.CSGHandles.InitFunction shutdownFunction = null)
		{
			var activeSnappingMode = RealtimeCSG.CSGSettings.ActiveSnappingMode;
			return RealtimeCSG.Helpers.CSGHandles.PositionHandle(camera, center, rotation, activeSnappingMode, snapVertices, initFunction, shutdownFunction);
		}

		public static Vector3 HandleScale(Vector3 scale, Vector3 center, Quaternion rotation, RealtimeCSG.Helpers.CSGHandles.InitFunction initFunction = null, RealtimeCSG.Helpers.CSGHandles.InitFunction shutdownFunction = null)
		{
			var doSnapping = RealtimeCSG.CSGSettings.ScaleSnapping;
			return RealtimeCSG.Helpers.CSGHandles.ScaleHandle(scale, center, rotation, doSnapping, initFunction, shutdownFunction);
		}

		public static Quaternion HandleRotation(Camera camera, Vector3 center, Quaternion rotation, RealtimeCSG.Helpers.CSGHandles.InitFunction initFunction = null, RealtimeCSG.Helpers.CSGHandles.InitFunction shutdownFunction = null)
		{
			var doSnapping = RealtimeCSG.CSGSettings.RotationSnapping;
			return RealtimeCSG.Helpers.CSGHandles.DoRotationHandle(camera, rotation, center, doSnapping, initFunction, shutdownFunction);
		}

		public static void DrawProjectedPivot(Camera camera, Vector3 center, Color color)
		{
			if (Event.current.type != EventType.Repaint)
				return;
			
			float handleSize = CSG_HandleUtility.GetHandleSize(center);

			var originalColor = Handles.color;

			Handles.color = color;
			DrawFilledCameraAlignedCircle(camera, center, handleSize * GUIConstants.handleScale * 1.00f);
				
			Handles.color = originalColor;
		}


		public static void DrawCircle(Vector3 center, Vector3 normal, Vector3 tangent, float radius, Color color)
		{			
			var originalMatrix = Handles.matrix;
			Handles.matrix = MathConstants.identityMatrix;
			
			var distance = (tangent * radius);
			
			Handles.color = color;
			Vector3 vector0 = center + (Quaternion.AngleAxis(-15, normal) * distance);
			for (int a = 0; a < 360; a += 15)
			{
				Vector3 vector1 = center + (Quaternion.AngleAxis(a, normal) * distance);

				Handles.DrawAAPolyLine(vector0, vector1);
				vector0 = vector1;
			}

			Handles.matrix = originalMatrix;
		}
		
		public static void DrawRotateCircle(Camera camera, Vector3 center, Vector3 normal, Vector3 tangent, float radius, 
											float originalAngle, float startAngle, float endAngle,
											//Color colorOuter, Color colorHatch,
											Color color,
											string axisName = null,
											bool centerText = false)
		{
			var originalMatrix = Handles.matrix;
			Handles.matrix = MathConstants.identityMatrix;

			Color colorOuter = color; colorOuter.a *= 0.8f;
			Color colorHatch = color;
			Color innerColor = color; innerColor.a *= 0.3f;

			float angleOffset	= startAngle - originalAngle;
			int hatch_count		= GetHatchCountForRadius(center, radius);
			Vector3 innerDistance = (tangent * radius);
			Vector3 outerDistance = (tangent * (radius * 1.25f));
			if (hatch_count > 0.0f)
			{
				float handleSize = HandleUtility.GetHandleSize(center);

				float maxSize = radius * 0.3f;

				Vector3[] tickDistance = new Vector3[]
				{
					outerDistance + (tangent * Mathf.Min(Mathf.Min(handleSize * 0.05f, handleSize * radius * 0.075f), maxSize)),
					outerDistance + (tangent * Mathf.Min(Mathf.Min(handleSize * 0.1f,  handleSize * radius * 0.15f), maxSize)),
					outerDistance + (tangent * Mathf.Min(Mathf.Min(handleSize * 0.5f,  handleSize * radius * 0.2f), maxSize)),
					outerDistance + (tangent * Mathf.Min(Mathf.Min(handleSize * 0.6f,  handleSize * radius * 0.3f), maxSize))
				};
				Vector3 labelDistance = outerDistance + (tangent * Mathf.Min(Mathf.Min(handleSize,  handleSize * radius * 0.5f), maxSize));

				int step = 360 / hatch_count;

				var halfColorOuter = colorOuter;
				halfColorOuter.a *= 0.5f;

				var vertices = new List<Vector3>((360 / step) * 2 * 4);

				Handles.matrix = MathConstants.identityMatrix;
				Vector3 vectorO0 = center + (Quaternion.AngleAxis(-step + angleOffset, normal) * outerDistance);
				Vector3 vectorI0 = center + (Quaternion.AngleAxis(-step + angleOffset, normal) * innerDistance);
				for (int a = 0; a < 360; a += step)
				{
					Vector3 vectorO1 = center + (Quaternion.AngleAxis(a + angleOffset, normal) * outerDistance);
					Vector3 vectorI1 = center + (Quaternion.AngleAxis(a + angleOffset, normal) * innerDistance);
					Vector3 vectorO2 = center + (Quaternion.AngleAxis(a + angleOffset - (step * 0.5f), normal) * outerDistance);
					Vector3 vectorI2 = center + (Quaternion.AngleAxis(a + angleOffset - (step * 0.5f), normal) * innerDistance);

					int tick_type;
					if		((a % 90) == 0) tick_type = 3;
					else if ((a % 45) == 0) tick_type = 2;
					else if ((a %  5) == 0) tick_type = 1;
					else tick_type = 0;

					Vector3 vectora = center + (Quaternion.AngleAxis(a + angleOffset, normal) * tickDistance[tick_type]);
					/*
					Handles.color = colorOuter;
					Handles.DrawAAPolyLine(vectorO0, vectorO2);
					Handles.DrawAAPolyLine(vectorO2, vectorO1);
					Handles.DrawAAPolyLine(vectorI0, vectorI2);
					Handles.DrawAAPolyLine(vectorI2, vectorI1);
					*/
					//Handles.color = halfColorOuter;
					//Handles.DrawAAPolyLine(vectorI1, vectorO1);
					//Handles.DrawAAPolyLine(vectorI2, vectorO2);

					Handles.color = colorHatch;
					Handles.DrawAAPolyLine(vectora, vectorO1);
					DrawLine(vectorO0, vectorO2, GUIConstants.oldThinLineScale, colorOuter);
					DrawLine(vectorO2, vectorO1, GUIConstants.oldThinLineScale, colorOuter);

					DrawLine(vectorI0, vectorI2, GUIConstants.oldThinLineScale, colorOuter); 
					DrawLine(vectorI2, vectorI1, GUIConstants.oldThinLineScale, colorOuter);

					DrawLine(vectora, vectorO1, GUIConstants.oldThinLineScale, colorHatch);

					vertices.Add(vectorI2); vertices.Add(vectorI0);
					vertices.Add(vectorO0); vertices.Add(vectorO2);

					vertices.Add(vectorI1); vertices.Add(vectorI2);
					vertices.Add(vectorO2); vertices.Add(vectorO1);

					if (tick_type >= 2)
					{
						if ((tick_type == 3 && hatch_count > 24) || hatch_count > 72)
						{ 
							DrawWorldText(camera, center + (Quaternion.AngleAxis(a + angleOffset, normal) * labelDistance), 
										  Units.ToAngleString(Mathf.RoundToInt(a)));
						}
					}

					vectorO0 = vectorO1;
					vectorI0 = vectorI1;
				}
				Vector3 startI1 = center + (Quaternion.AngleAxis(angleOffset, normal) * innerDistance);
				Vector3 startO1 = center + (Quaternion.AngleAxis(angleOffset, normal) * outerDistance);

				Handles.color = colorOuter;
				Handles.DrawAAPolyLine(startI1, startO1);

				float snap_step = Mathf.Max(3.0f, RealtimeCSG.CSGSettings.SnapRotation);
				for (float a = snap_step; a <= 180; a += snap_step)
				{
					Vector3 vectorI1 = center + (Quaternion.AngleAxis(angleOffset + a, normal) * innerDistance);
					Vector3 vectorO1 = center + (Quaternion.AngleAxis(angleOffset + a, normal) * outerDistance);
					
					Vector3 vectorI2 = center + (Quaternion.AngleAxis(angleOffset - a, normal) * innerDistance);
					Vector3 vectorO2 = center + (Quaternion.AngleAxis(angleOffset - a, normal) * outerDistance);

					DrawLine(vectorI1, vectorO1, GUIConstants.oldThinLineScale, colorOuter);
					DrawLine(vectorI2, vectorO2, GUIConstants.oldThinLineScale, colorOuter);
				}

				if (centerText)
				{
					if (!string.IsNullOrEmpty(axisName))
						DrawWorldText(camera, center,
										string.Format(CultureInfo.InvariantCulture, "{0}: Δ{1}", 
										axisName,
										Units.ToRoundedAngleString(endAngle - startAngle)));
					else
						DrawWorldText(camera, center,
										string.Format(CultureInfo.InvariantCulture, "Δ{0}",
										Units.ToRoundedAngleString(endAngle - startAngle)));
				} else
				{
					DrawWorldText(camera, center + (Quaternion.AngleAxis(startAngle, normal) * labelDistance), 
								  Units.ToAngleString(originalAngle));
					{
						/*
						DrawWorldText(center + (Quaternion.AngleAxis(endAngle, normal) * (innerDistance * 0.75f)), 
									  string.Format(CultureInfo.InvariantCulture, "Δ{0}\n{1}", 
									  Units.ToRoundedAngleString(endAngle - startAngle), 
									  Units.ToRoundedAngleString(originalAngle + endAngle - startAngle)));
						/*/
						if (!string.IsNullOrEmpty(axisName))
							DrawWorldText(camera, center + (Quaternion.AngleAxis(endAngle, normal) * (innerDistance * 0.75f)), 
										  string.Format(CultureInfo.InvariantCulture, "{0}: Δ{1}", 
										  axisName,
										  Units.ToRoundedAngleString(endAngle - startAngle)));
						else
							DrawWorldText(camera, center + (Quaternion.AngleAxis(endAngle, normal) * (innerDistance * 0.75f)),
										  string.Format(CultureInfo.InvariantCulture, "Δ{0}",
										  Units.ToRoundedAngleString(endAngle - startAngle)));
						//*/
					}
				}



				var material = CustomSurfaceNoDepthMaterial;
				if (material && material.SetPass(0))
				{
					GL.Begin(GL.QUADS);
					GL.Color(innerColor);
					for (int i = 0; i < vertices.Count; i += 8)
					{
						GL.Vertex(vertices[i + 0]);
						GL.Vertex(vertices[i + 1]);
						GL.Vertex(vertices[i + 2]);
						GL.Vertex(vertices[i + 3]);

						GL.Vertex(vertices[i + 4]);
						GL.Vertex(vertices[i + 5]);
						GL.Vertex(vertices[i + 6]);
						GL.Vertex(vertices[i + 7]);
					}
					GL.End();
				}
			} else
			{
				Handles.matrix = MathConstants.identityMatrix;
				Handles.color = colorOuter;
				Vector3 vector0 = center + (Quaternion.AngleAxis(-15 + angleOffset, normal) * outerDistance);
				for (int a = 0; a < 360; a += 15)
				{
					Vector3 vector1 = center + (Quaternion.AngleAxis(a + angleOffset, normal) * outerDistance);

					Handles.DrawAAPolyLine(vector0, vector1);
					vector0 = vector1;
				}
			}


			Handles.matrix = originalMatrix;
		}

		public static void DrawArrowCap(Vector3 position, Vector3 direction, float size)
		{
			if (Event.current.type != EventType.Repaint)
				return;

			if (direction == MathConstants.zeroVector3)
				return;
#if UNITY_5_6_OR_NEWER
			Handles.ConeHandleCap (-1, position + direction * size, Quaternion.LookRotation (direction), size * .2f, EventType.Repaint);
#else
			Handles.ConeCap (-1, position + direction * size, Quaternion.LookRotation (direction), size * .2f);
#endif
			Handles.DrawLine (position, position + direction * size * .9f);
		}

		public static void AddArrowCapControl(int controlID, Vector3 position, Vector3 direction, float size)
		{
			var prevMatrix = Handles.matrix;
			Handles.matrix = Matrix4x4.identity;

			float line_distance		= HandleUtility.DistanceToLine(position, position + direction * size);
			float cap_distance		= HandleUtility.DistanceToCircle(position + direction * size, size * .2f);
			HandleUtility.AddControl(controlID, line_distance);
			HandleUtility.AddControl(controlID, cap_distance);
			
			Handles.matrix = prevMatrix;
		}
		
		public static void DrawLength(Camera camera, string axisName, float handleSize, Matrix4x4 matrix, Vector3 sideNormal, Vector3 vertexA, Vector3 vertexB, Color color, GUIStyle style = null)
		{
			vertexA	= matrix.MultiplyPoint(vertexA);
			vertexB	= matrix.MultiplyPoint(vertexB);
			
			var normal	= matrix.MultiplyVector(sideNormal);
			var delta	= (vertexB - vertexA);
			var length	= delta.magnitude;
			delta /= length;
			
			var lineA3	= vertexA + (delta * handleSize * 0.125f);
			var lineB3	= vertexB - (delta * handleSize * 0.125f);

			var lineA4 = lineA3 - (normal * handleSize * 0.0625f);
			var lineB4 = lineB3 - (normal * handleSize * 0.0625f);

			var lineA5 = lineA3 + (normal * handleSize * 0.0625f);
			var lineB5 = lineB3 + (normal * handleSize * 0.0625f);
			/*
			var lineA1	= vertexA - (normal * handleSize * 0.5f);
			var lineB1	= vertexB - (normal * handleSize * 0.5f);

			var lineA2	= vertexA + (normal * handleSize * 0.5f);
			var lineB2	= vertexB + (normal * handleSize * 0.5f);

			Handles.color = Color.black;
			Handles.DrawAAPolyLine(3.0f, lineA1, lineA2);
			Handles.DrawAAPolyLine(3.0f, lineB1, lineB2);
			*/
			Handles.color = color;
			Handles.DrawAAPolyLine(3.0f, lineA3, lineB3);
			Handles.DrawAAConvexPolygon(vertexA, lineA4, lineA5);
			Handles.DrawAAConvexPolygon(vertexB, lineB4, lineB5);

			var center = ((vertexB + vertexA) * 0.5f);

			var screenPoint = camera.WorldToScreenPoint(center);
			if (screenPoint.z < 0)
				return;
			
			//EditorGUIUtility.ScreenToGUIPoint(screenPoint); // broken		
			
			var prevMatrix = Handles.matrix;
			Handles.matrix = Matrix4x4.identity;

			var center2D = HandleUtility.WorldToGUIPoint(center);
			var offset	 = HandleUtility.WorldToGUIPoint(center + normal);
			var offset2D = (offset - center2D).normalized;
			var content  = new GUIContent(axisName + Units.ToRoundedDistanceString(length));
			Handles.BeginGUI();
			if (style == null)
				style = CSG_GUIStyleUtility.sceneTextLabel;
			var calcSize = style.CalcSize(content);
			var radius = (calcSize.magnitude * 0.5f) + 4.0f;
			center2D += radius * offset2D;
			GUI.Label(new Rect(center2D.x - (calcSize.x * 0.5f),
							   center2D.y - (calcSize.y * 0.5f),
							   calcSize.x, calcSize.y), content, style);
			Handles.EndGUI();
			
			Handles.matrix = prevMatrix;
		}

		public static void DrawEdgeLength(Camera camera, string axisName, float handleSize, Vector3 normal, Vector3 vertexA, Vector3 vertexB, Color color, GUIStyle style = null)
		{
			var tangent	= (vertexB - vertexA);
			var length	= tangent.magnitude;
			tangent /= length;
			
			var extrusion = Mathf.Min(2.0f, handleSize);

			var lineA1	= vertexA + (normal * (extrusion * 0.5f));
			var lineB1	= vertexB + (normal * (extrusion * 0.5f));
			
			var lineA2	= vertexA + (normal * extrusion);
			var lineB2	= vertexB + (normal * extrusion);
			

			var prevMatrix = Handles.matrix;
			Handles.matrix = Matrix4x4.identity;

			var handleSizeA = Mathf.Min(length / 3.0f, HandleUtility.GetHandleSize(vertexA) * 0.15f);
			var handleSizeB = Mathf.Min(length / 3.0f, HandleUtility.GetHandleSize(vertexB) * 0.15f);
			

			Handles.color = Color.black;
			Handles.DrawAAPolyLine(3.0f, vertexA, lineA2);
			Handles.DrawAAPolyLine(3.0f, vertexB, lineB2);

			//if (length > (handleSizeA + (handleSize * 0.125f) + handleSizeB))
			{
				var lineA3	= lineA1 + (tangent * handleSizeA);
				var lineB3	= lineB1 - (tangent * handleSizeB);

				var lineA4	= lineA3 - (normal * (handleSizeA * 0.5f));
				var lineB4	= lineB3 - (normal * (handleSizeB * 0.5f));

				var lineA5	= lineA3 + (normal * (handleSizeA * 0.5f));
				var lineB5	= lineB3 + (normal * (handleSizeB * 0.5f));

				Handles.DrawAAPolyLine(7.0f, lineA3, lineB3);

				Handles.color = color;
				Handles.DrawAAConvexPolygon(lineA1, lineA4, lineA5);
				Handles.DrawAAConvexPolygon(lineB1, lineB4, lineB5);

				Handles.color = Color.black;
				Handles.DrawAAPolyLine(2.0f, lineA1, lineA4, lineA5);
				Handles.DrawAAPolyLine(2.0f, lineB1, lineB4, lineB5);

				Handles.color = color;
				Handles.DrawAAPolyLine(5.0f, lineA3, lineB3);
			}/* else
			{
				Handles.DrawAAPolyLine(7.0f, lineA1, lineB1);
				
				Handles.color = color;
				Handles.DrawAAPolyLine(5.0f, lineA1, lineB1);
			}*/

			var center = ((lineB1 + lineA1) * 0.5f);

			var screenPoint = camera.WorldToScreenPoint(center);
			if (screenPoint.z < 0)
			{
				Handles.matrix = prevMatrix;
				return;
			}
			
			//EditorGUIUtility.ScreenToGUIPoint(screenPoint); // broken		
			
			var center2D = HandleUtility.WorldToGUIPoint(center);
			var offset	 = HandleUtility.WorldToGUIPoint(center + normal);
			var offset2D = (offset - center2D).normalized;
			var content  = new GUIContent(axisName + Units.ToRoundedDistanceString(length));
			Handles.BeginGUI();
			if (style == null)
				style = CSG_GUIStyleUtility.sceneTextLabel;
			var calcSize = style.CalcSize(content);
			var radius = (calcSize.magnitude * 0.5f) + 4.0f;
			center2D += radius * offset2D;
			GUI.Label(new Rect(center2D.x - (calcSize.x * 0.5f),
							   center2D.y - (calcSize.y * 0.5f),
							   calcSize.x, calcSize.y), content, style);
			Handles.EndGUI();

			
			Handles.matrix = prevMatrix;
		}

		static void DrawBoundsEdgeLength(Camera camera, string axisName, float handleSize, Vector3 cameraPosition, Matrix4x4 matrix, Vector3[] vertices, int edgeIndex, Color color)
		{
			var vertexA		= vertices[BoundsUtilities.AABBEdgeIndices[edgeIndex][0]];
			var vertexB		= vertices[BoundsUtilities.AABBEdgeIndices[edgeIndex][1]];

			var sideIndexA	= BoundsUtilities.AABBEdgeSides[edgeIndex][0];
			var sideIndexB	= BoundsUtilities.AABBEdgeSides[edgeIndex][1];

			var sideNormalA	= BoundsUtilities.AABBSideNormal[sideIndexA];
			var sideNormalB	= BoundsUtilities.AABBSideNormal[sideIndexB];
			
			var edge_point	= GeometryUtility.ProjectPointOnInfiniteLine(cameraPosition, vertexA, (vertexB - vertexA).normalized);
			var orientation = matrix.MultiplyVector((edge_point - cameraPosition).normalized);
			var dotA		= Mathf.Abs(Vector3.Dot(orientation, matrix.MultiplyVector(sideNormalA)));
			var dotB		= Mathf.Abs(Vector3.Dot(orientation, matrix.MultiplyVector(sideNormalB)));

			Vector3 sideNormal;
			if (dotA < dotB) sideNormal = sideNormalA;
			else             sideNormal = sideNormalB;
			
			vertexA		= matrix.MultiplyPoint(vertexA);
			vertexB		= matrix.MultiplyPoint(vertexB);
			sideNormal	= matrix.MultiplyVector(sideNormal).normalized;

			DrawEdgeLength(camera, axisName, handleSize, sideNormal, vertexA, vertexB, color);
		}

		static void DrawBoundsEdgeLength(Camera camera, string axisName, float handleSize, Vector3 cameraPosition, Quaternion rotation, Vector3[] vertices, int edgeIndex, Color color)
		{
			var vertexA		= vertices[BoundsUtilities.AABBEdgeIndices[edgeIndex][0]];
			var vertexB		= vertices[BoundsUtilities.AABBEdgeIndices[edgeIndex][1]];

			var sideIndexA	= BoundsUtilities.AABBEdgeSides[edgeIndex][0];
			var sideIndexB	= BoundsUtilities.AABBEdgeSides[edgeIndex][1];

			var sideNormalA	= BoundsUtilities.AABBSideNormal[sideIndexA];
			var sideNormalB	= BoundsUtilities.AABBSideNormal[sideIndexB];
			
			var edge_point	= GeometryUtility.ProjectPointOnInfiniteLine(cameraPosition, vertexA, (vertexB - vertexA).normalized);
			var orientation = (edge_point - cameraPosition).normalized;
			var dotA		= Mathf.Abs(Vector3.Dot(orientation, rotation * sideNormalA));
			var dotB		= Mathf.Abs(Vector3.Dot(orientation, rotation * sideNormalB));

			Vector3 sideNormal;
			if (dotA < dotB) sideNormal = sideNormalA;
			else             sideNormal = sideNormalB;
			
			vertexA		= rotation * vertexA;
			vertexB		= rotation * vertexB;
			sideNormal	= rotation * sideNormal;

			DrawEdgeLength(camera, axisName, handleSize, sideNormal, vertexA, vertexB, color);
		}

		static void GetActiveEdges(Vector3 cameraPosition, Vector3 cameraDirection, bool ortho, Vector3[] vertices, ref int xAxis, ref int yAxis, ref int zAxis)
		{
			xAxis = -1;
			yAxis = -1;
			zAxis = -1;
			float prevXAxisDist = float.PositiveInfinity;
			float prevYAxisDist = float.PositiveInfinity;
			float prevZAxisDist = float.PositiveInfinity;
			
			var backfaced			= new bool[6];		// [-X, +X, -Y, +Y, -Z, +Z]
			var planes				= new CSGPlane[6];  // [-X, +X, -Y, +Y, -Z, +Z]
			for (int i = 0; i < BoundsUtilities.AABBSideNormal.Length; i++)
			{
				var vertex = vertices[BoundsUtilities.AABBSidePointIndices[i][0]];
				planes[i] = new CSGPlane(BoundsUtilities.AABBSideNormal[i], vertex);
				
				backfaced[i] = Vector3.Dot(planes[i].normal, (cameraPosition - vertex)) < 0;
			}
			
			var disabledX = ortho && (Mathf.Abs(Vector3.Dot(planes[0].normal, cameraDirection)) > 0.99f);
			var disabledY = ortho && (Mathf.Abs(Vector3.Dot(planes[2].normal, cameraDirection)) > 0.99f);
			var disabledZ = ortho && (Mathf.Abs(Vector3.Dot(planes[4].normal, cameraDirection)) > 0.99f);

			for (int i = 0; i < 4; i++)
			{
				var edgeIndex = BoundsUtilities.AABBXAxi[i];
				var side1 = BoundsUtilities.AABBEdgeSides[edgeIndex][0];
				var side2 = BoundsUtilities.AABBEdgeSides[edgeIndex][1];
				if (backfaced[side1] != backfaced[side2])
				{
					var A = vertices[BoundsUtilities.AABBEdgeIndices[edgeIndex][0]];
					var B = vertices[BoundsUtilities.AABBEdgeIndices[edgeIndex][1]];
					if ((A - B).sqrMagnitude > MathConstants.EqualityEpsilonSqr)
					{
						var C = (A + B) * 0.5f;
						var dist = (cameraPosition.x - C.x);
						if (xAxis == -1 || dist < prevXAxisDist)
						{
							xAxis = -1;
							prevXAxisDist = dist;
						}

						if (xAxis == -1)
							xAxis = edgeIndex;
					}
				}

				edgeIndex = BoundsUtilities.AABBZAxi[i];
				side1 = BoundsUtilities.AABBEdgeSides[edgeIndex][0];
				side2 = BoundsUtilities.AABBEdgeSides[edgeIndex][1];
				if (backfaced[side1] != backfaced[side2])
				{
					var A = vertices[BoundsUtilities.AABBEdgeIndices[edgeIndex][0]];
					var B = vertices[BoundsUtilities.AABBEdgeIndices[edgeIndex][1]];
					if ((A - B).sqrMagnitude > MathConstants.EqualityEpsilonSqr)
					{
						var C = (A + B) * 0.5f;
						var dist = (cameraPosition.z - C.z);
						if (zAxis == -1 || dist < prevZAxisDist)
						{
							zAxis = -1;
							prevZAxisDist = dist;
						}

						if (zAxis == -1)
							zAxis = edgeIndex;
					}
				}
			}
			if (disabledX) xAxis = -1;
			if (disabledZ) zAxis = -1;

			if (zAxis == -1 && xAxis == -1)
			{
				for (int i = 0; i < 4; i++)
				{
					var edgeIndex = BoundsUtilities.AABBYAxi[3 - i];
					var side1 = BoundsUtilities.AABBEdgeSides[edgeIndex][0];
					var side2 = BoundsUtilities.AABBEdgeSides[edgeIndex][1];
					if (backfaced[side1] != backfaced[side2])
					{
						var A = vertices[BoundsUtilities.AABBEdgeIndices[edgeIndex][0]];
						var B = vertices[BoundsUtilities.AABBEdgeIndices[edgeIndex][1]];
						if ((A - B).sqrMagnitude > MathConstants.EqualityEpsilonSqr)
						{
							var C = (A + B) * 0.5f;
							var dist = (cameraPosition.y - C.y);
							if (yAxis == -1 || dist < prevYAxisDist)
							{
								yAxis = -1;
								prevYAxisDist = dist;
							}

							if (yAxis == -1)
								yAxis = edgeIndex;
						}
					}
				}
			} else
			{
				var xA = xAxis == -1 ? -1 : BoundsUtilities.AABBEdgeIndices[xAxis][0];
				var xB = xAxis == -1 ? -1 : BoundsUtilities.AABBEdgeIndices[xAxis][1];

				var zA = zAxis == -1 ? -1 : BoundsUtilities.AABBEdgeIndices[zAxis][0];
				var zB = zAxis == -1 ? -1 : BoundsUtilities.AABBEdgeIndices[zAxis][1];
				
				for (int i = 0; i < 4; i++)
				{
					var edgeIndex = BoundsUtilities.AABBYAxi[3 - i];
					var side1 = BoundsUtilities.AABBEdgeSides[edgeIndex][0];
					var side2 = BoundsUtilities.AABBEdgeSides[edgeIndex][1];
					if (backfaced[side1] != backfaced[side2])
					{
						var aIndex = BoundsUtilities.AABBEdgeIndices[edgeIndex][0];
						var bIndex = BoundsUtilities.AABBEdgeIndices[edgeIndex][1];
						var xAIndex = xAxis == -1 ? -1 : aIndex;
						var xBIndex = xAxis == -1 ? -1 : bIndex;
						var zAIndex = zAxis == -1 ? -1 : aIndex;
						var zBIndex = zAxis == -1 ? -1 : bIndex;
						if (((xAIndex == xA || xAIndex == xB) && (zBIndex == zA || zBIndex == zB)) ||
							((xBIndex == xA || xBIndex == xB) && (zAIndex == zA || zAIndex == zB)))
						{
							var A = vertices[aIndex];
							var B = vertices[bIndex];
							if ((A - B).sqrMagnitude > MathConstants.EqualityEpsilonSqr)
							{
								var C = (A + B) * 0.5f;
								var dist = (cameraPosition.y - C.y);
								if (yAxis == -1 || dist < prevYAxisDist)
								{
									yAxis = -1;
									prevYAxisDist = dist;
								}

								if (yAxis == -1)
									yAxis = edgeIndex;
							}
						}
					}
				}

				if (yAxis == -1)
				{
					for (int i = 0; i < 4; i++)
					{
						var edgeIndex = BoundsUtilities.AABBYAxi[3 - i];
						var side1 = BoundsUtilities.AABBEdgeSides[edgeIndex][0];
						var side2 = BoundsUtilities.AABBEdgeSides[edgeIndex][1];
						if (backfaced[side1] != backfaced[side2])
						{
							var aIndex = BoundsUtilities.AABBEdgeIndices[edgeIndex][0];
							var bIndex = BoundsUtilities.AABBEdgeIndices[edgeIndex][1];
							
							if (aIndex == xA || aIndex == zA || bIndex == zB || bIndex == xB ||
								aIndex == xB || aIndex == zB || bIndex == zA || bIndex == xA)
							{
								var A = vertices[aIndex];
								var B = vertices[bIndex];
								if ((A - B).sqrMagnitude > MathConstants.EqualityEpsilonSqr)
								{
									var C = (A + B) * 0.5f;
									var dist = (cameraPosition.y - C.y);
									if (yAxis == -1 || dist < prevYAxisDist)
									{
										yAxis = -1;
										prevYAxisDist = dist;
									}

									if (yAxis == -1)
										yAxis = edgeIndex;
								}
							}
						}
					}
				}
			}
			if (disabledY) yAxis = -1;
		}
		
		public static void RenderBoundsSizes(Matrix4x4 invMatrix, Matrix4x4 matrix, 
											 Camera camera, Vector3[] vertices, 
											 Color colorAxisX, Color colorAxisY, Color colorAxisZ,
											 bool showAxisX, bool showAxisY, bool showAxisZ)
		{
			int xAxis = -1;
			int yAxis = -1;
			int zAxis = -1;
			
			var ortho				= camera.orthographic;
			var cameraPosition		= invMatrix.MultiplyPoint(camera.transform.position);
			var cameraDirection		= invMatrix.MultiplyVector(camera.transform.forward);
			GetActiveEdges(cameraPosition, cameraDirection, ortho, vertices, ref xAxis, ref yAxis, ref zAxis);
			
			float handleSize = 0;
			
			var prevMatrix = Handles.matrix;
			Handles.matrix = Matrix4x4.identity;

			// we still check the handleSizes for axi that are not shown, to make things look more consistent when turning axi on/off
			if (xAxis >= 0)
			{
				handleSize = Mathf.Max(handleSize, HandleUtility.GetHandleSize(matrix.MultiplyPoint(vertices[BoundsUtilities.AABBEdgeIndices[xAxis][0]])));
				handleSize = Mathf.Max(handleSize, HandleUtility.GetHandleSize(matrix.MultiplyPoint(vertices[BoundsUtilities.AABBEdgeIndices[xAxis][1]])));
			}
			if (yAxis >= 0)
			{
				handleSize = Mathf.Max(handleSize, HandleUtility.GetHandleSize(matrix.MultiplyPoint(vertices[BoundsUtilities.AABBEdgeIndices[yAxis][0]])));
				handleSize = Mathf.Max(handleSize, HandleUtility.GetHandleSize(matrix.MultiplyPoint(vertices[BoundsUtilities.AABBEdgeIndices[yAxis][1]])));
			}
			if (zAxis >= 0)
			{
				handleSize = Mathf.Max(handleSize, HandleUtility.GetHandleSize(matrix.MultiplyPoint(vertices[BoundsUtilities.AABBEdgeIndices[zAxis][0]])));
				handleSize = Mathf.Max(handleSize, HandleUtility.GetHandleSize(matrix.MultiplyPoint(vertices[BoundsUtilities.AABBEdgeIndices[zAxis][1]])));
			}

			if (showAxisX && xAxis >= 0) DrawBoundsEdgeLength(camera, "x: ", handleSize, cameraPosition, matrix, vertices, xAxis, colorAxisX);
			if (showAxisY && yAxis >= 0) DrawBoundsEdgeLength(camera, "y: ", handleSize, cameraPosition, matrix, vertices, yAxis, colorAxisY);
			if (showAxisZ && zAxis >= 0) DrawBoundsEdgeLength(camera, "z: ", handleSize, cameraPosition, matrix, vertices, zAxis, colorAxisZ); 
			
			Handles.matrix = prevMatrix;
		}
		
		public static void RenderBoundsSizes(Quaternion invRotation, Quaternion rotation, 
											 Camera camera, Vector3[] vertices, 
											 Color colorAxisX, Color colorAxisY, Color colorAxisZ,
											 bool showAxisX, bool showAxisY, bool showAxisZ)
		{
			int xAxis = -1;
			int yAxis = -1;
			int zAxis = -1;
			

			var ortho			= camera.orthographic;			
			var cameraPosition	= invRotation * camera.transform.position;
			var cameraDirection	= invRotation * camera.transform.forward;
			GetActiveEdges(cameraPosition, cameraDirection, ortho, vertices, ref xAxis, ref yAxis, ref zAxis);
			
			float handleSize = 0;

			float closest_distance = float.PositiveInfinity;
			float distance;
			Vector3 vertex;
			Vector3 closestPointToCamera = Vector3.zero;

			if (//showAxisX && 
				xAxis >= 0)
			{
				vertex = rotation * vertices[BoundsUtilities.AABBEdgeIndices[xAxis][0]];
				distance = (vertex - camera.transform.position).magnitude;
				if (closest_distance == float.PositiveInfinity ||
					distance < closest_distance) { closest_distance = distance; closestPointToCamera = vertex; }

				vertex = rotation * vertices[BoundsUtilities.AABBEdgeIndices[xAxis][1]];
				distance = (vertex - camera.transform.position).magnitude;
				if (closest_distance == float.PositiveInfinity ||
					distance < closest_distance) { closest_distance = distance; closestPointToCamera = vertex; }
			}
			if (//showAxisY && 
				yAxis >= 0)
			{
				vertex = rotation * vertices[BoundsUtilities.AABBEdgeIndices[yAxis][0]];
				distance = (vertex - camera.transform.position).magnitude;
				if (closest_distance == float.PositiveInfinity ||
					distance < closest_distance) { closest_distance = distance; closestPointToCamera = vertex; }

				vertex = rotation * vertices[BoundsUtilities.AABBEdgeIndices[yAxis][1]];
				distance = (vertex - camera.transform.position).magnitude;
				if (closest_distance == float.PositiveInfinity ||
					distance < closest_distance) { closest_distance = distance; closestPointToCamera = vertex; }
			}
			if (//showAxisZ && 
				zAxis >= 0)
			{
				vertex = rotation * vertices[BoundsUtilities.AABBEdgeIndices[zAxis][0]];
				distance = (vertex - camera.transform.position).magnitude;
				if (closest_distance == float.PositiveInfinity ||
					distance < closest_distance) { closest_distance = distance; closestPointToCamera = vertex; }

				vertex = rotation * vertices[BoundsUtilities.AABBEdgeIndices[zAxis][1]];
				distance = (vertex - camera.transform.position).magnitude;
				if (closest_distance == float.PositiveInfinity ||
					distance < closest_distance) { closest_distance = distance; closestPointToCamera = vertex; }
			}
			
			var prevMatrix = Handles.matrix;
			Handles.matrix = Matrix4x4.identity;

			handleSize = HandleUtility.GetHandleSize(closestPointToCamera);

			if (showAxisX && xAxis >= 0) DrawBoundsEdgeLength(camera, "x: ", handleSize, cameraPosition, rotation, vertices, xAxis, colorAxisX);
			if (showAxisY && yAxis >= 0) DrawBoundsEdgeLength(camera, "y: ", handleSize, cameraPosition, rotation, vertices, yAxis, colorAxisY);
			if (showAxisZ && zAxis >= 0) DrawBoundsEdgeLength(camera, "z: ", handleSize, cameraPosition, rotation, vertices, zAxis, colorAxisZ);
			
			Handles.matrix = prevMatrix;
		}

		public static void DrawSelectionRectangle(Rect rect)
		{
			var origMatrix = Handles.matrix;
			Handles.matrix = origMatrix;
			if (rect.width >= 0 || rect.height >= 0)
			{
				Handles.BeginGUI();
				CSG_GUIStyleUtility.InitStyles();
				CSG_GUIStyleUtility.selectionRectStyle.Draw(rect, GUIContent.none, false, false, false, false);
				Handles.EndGUI();
			}
			Handles.matrix = origMatrix;
		}
	}
}
