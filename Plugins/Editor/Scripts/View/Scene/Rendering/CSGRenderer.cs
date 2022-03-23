using UnityEngine;
using UnityEditor;
using System;
using InternalRealtimeCSG;
using RealtimeCSG.Legacy;
using RealtimeCSG.Components;

namespace RealtimeCSG
{
	internal static class CSGRenderer
    {
		public const float visibleOuterLineDots		= 2.0f;
		public const float visibleInnerLineDots		= 4.0f;

		public const float invisibleOuterLineDots	= 2.0f;
		public const float invisibleInnerLineDots	= 4.0f;

		public const float invalidLineDots			= 2.0f;

		public const float unselected_factor		= 0.65f;
		public const float inner_factor				= 0.70f;
		public const float occluded_factor			= 0.80f;
		
		public static void DrawSelectedBrush(Int32 brushNodeID, Matrix4x4 transformation, Color wireframeColor, float thickness = -1)
		{
			Color selectedOuterColor		 = wireframeColor;	   //selectedOuterColor.a         = 1.0f;
			Color selectedInnerColor		 = selectedOuterColor * inner_factor;
			Color selectedOuterOccludedColor = selectedOuterColor * occluded_factor;
			Color selectedInnerOccludedColor = selectedInnerColor * occluded_factor;
			
			selectedOuterOccludedColor.a *= 0.5f;
			selectedInnerOccludedColor.a *= 0.5f;

			CSGRenderer.DrawOutlines(brushNodeID, transformation, selectedOuterColor, selectedOuterOccludedColor, selectedInnerColor, selectedInnerOccludedColor, thickness);
        }

		public static void DrawSelectedBrushes(LineMeshManager zTestLineMeshManager, LineMeshManager noZTestLineMeshManager, Int32[] brushNodeIDs, Matrix4x4[] transformations, Color wireframeColor, float thickness = -1)
		{
			Color selectedOuterColor		 = wireframeColor;	   //selectedOuterColor.a         = 1.0f;
			Color selectedInnerColor		 = selectedOuterColor * inner_factor;
			Color selectedOuterOccludedColor = selectedOuterColor * occluded_factor;
			Color selectedInnerOccludedColor = selectedInnerColor * occluded_factor;
			
			selectedOuterOccludedColor.a *= 0.5f;
			selectedInnerOccludedColor.a *= 0.5f;

			var wireframes = BrushOutlineManager.GetBrushOutlines(brushNodeIDs);			
			CSGRenderer.DrawOutlines(zTestLineMeshManager, noZTestLineMeshManager, wireframes, transformations, selectedOuterColor, selectedOuterOccludedColor, selectedInnerColor, selectedInnerOccludedColor, thickness);
        }

		public static void DrawSelectedBrushes(LineMeshManager zTestLineMeshManager, LineMeshManager noZTestLineMeshManager, GeometryWireframe[] wireframes, Matrix4x4[] transformations, Color wireframeColor, float thickness = -1)
		{
			Color selectedOuterColor		 = wireframeColor;	   //selectedOuterColor.a         = 1.0f;
			Color selectedInnerColor		 = selectedOuterColor * inner_factor;
			Color selectedOuterOccludedColor = selectedOuterColor * occluded_factor;
			Color selectedInnerOccludedColor = selectedInnerColor * occluded_factor;
			
			//selectedOuterOccludedColor.a *= 0.5f;
			//selectedInnerOccludedColor.a *= 0.5f;
		
			CSGRenderer.DrawOutlines(zTestLineMeshManager, noZTestLineMeshManager, wireframes, transformations, selectedOuterColor, selectedOuterOccludedColor, selectedInnerColor, selectedInnerOccludedColor, thickness);
        }
		/*
		public static void DrawSelectedBrush(GeometryWireframe outline, Matrix4x4 transformation, Color wireframeColor, float thickness = -1)
		{
			Color selectedOuterColor		 = wireframeColor;
			Color selectedInnerColor		 = wireframeColor * inner_factor;
			Color selectedOuterOccludedColor = selectedOuterColor * occluded_factor;
			Color selectedInnerOccludedColor = selectedInnerColor * occluded_factor;
			
			selectedOuterOccludedColor.a *= 0.5f;
			selectedInnerOccludedColor.a *= 0.5f;

			CSGRenderer.DrawOutlines(outline, transformation, selectedOuterColor, selectedOuterOccludedColor, selectedInnerColor, selectedInnerOccludedColor, thickness);
        }*/

		//static readonly Color emptyColor = new Color(0, 0, 0, 0);

		public static void DrawSelectedBrush(Int32 brushNodeID, Shape shape, Matrix4x4 transformation, Color wireframeColor, int surfaceIndex, bool selectAllSurfaces, float thickness = -1)
        {
			if (selectAllSurfaces)
			{
				Color selectedOuterColor		 = wireframeColor;
				Color selectedInnerColor		 = wireframeColor;
				Color selectedOuterOccludedColor = selectedOuterColor * occluded_factor;
				Color selectedInnerOccludedColor = selectedInnerColor * occluded_factor;
			
				selectedOuterOccludedColor.a *= 0.5f;
				selectedInnerOccludedColor.a *= 0.5f;

				CSGRenderer.DrawOutlines(brushNodeID, transformation, selectedOuterColor, selectedOuterOccludedColor, selectedInnerColor, selectedInnerOccludedColor, thickness); 
			} else
            {
				Color unselectedOuterColor			= wireframeColor * unselected_factor;
				Color unselectedInnerColor			= wireframeColor * (unselected_factor * inner_factor);
				Color selectedOuterColor			= wireframeColor;
				Color selectedInnerColor			= wireframeColor * inner_factor;
				Color unselectedOuterOccludedColor	= unselectedOuterColor * occluded_factor;
				Color unselectedInnerOccludedColor	= unselectedInnerColor * occluded_factor;
//				Color selectedOuterOccludedColor	= selectedOuterColor * occluded_factor;
//				Color selectedInnerOccludedColor	= selectedInnerColor * occluded_factor; 
			
				unselectedOuterOccludedColor.a *= 0.5f;
				unselectedInnerOccludedColor.a *= 0.5f;
				
			    if (surfaceIndex  >= 0 && surfaceIndex < shape.Surfaces.Length)
                {
					CSGRenderer.DrawSurfaceOutlines(brushNodeID, shape, transformation, surfaceIndex, selectedOuterColor, selectedInnerColor);
				}
				CSGRenderer.DrawOutlines(brushNodeID, transformation, unselectedOuterColor, unselectedOuterOccludedColor, unselectedInnerColor, unselectedInnerOccludedColor, thickness);
			}
        }

		public static void DrawSurfaceOutlines(Int32 brushNodeID, Shape shape, Matrix4x4 transformation, int surfaceIndex, 
											   Color outerColor, Color innerColor, 
											   float thickness = GUIConstants.oldThickLineScale)
		{
			// .. could be a prefab
			if (brushNodeID == CSGNode.InvalidNodeID ||
				surfaceIndex == -1)
			{
				return;
			}
			
			if (surfaceIndex < 0 ||
                surfaceIndex >= shape.Surfaces.Length)
			{
				return;
			}
			
			GeometryWireframe outline;
			if (!InternalCSGModelManager.External.GetSurfaceOutline(brushNodeID,
																    surfaceIndex,
																    out outline))
				return;
			
			Handles.matrix = transformation;
			if (outerColor.a > 0)
			{
				Handles.color = outerColor;
				if (outline.visibleOuterLines != null && outline.visibleOuterLines.Length > 0)
				{
					Handles.DrawDottedLines(outline.vertices, outline.visibleOuterLines, visibleOuterLineDots);
					PaintUtility.DrawLines(transformation, outline.vertices, outline.visibleOuterLines, thickness, outerColor);
				}
				if (outline.invisibleOuterLines != null && outline.invisibleOuterLines.Length > 0)
				{
					Handles.DrawDottedLines(outline.vertices, outline.invisibleOuterLines, invisibleOuterLineDots);
				}
			}
			if (innerColor.a > 0)
			{
				Handles.color = innerColor;
				if (outline.visibleInnerLines != null && outline.visibleInnerLines.Length > 0)
				{
					Handles.DrawDottedLines(outline.vertices, outline.visibleInnerLines, visibleInnerLineDots);
					PaintUtility.DrawLines(transformation, outline.vertices, outline.visibleInnerLines, thickness, innerColor);
				}
				if (outline.invisibleInnerLines != null && outline.invisibleInnerLines.Length > 0)
				{
					Handles.DrawDottedLines(outline.vertices, outline.invisibleInnerLines, invisibleInnerLineDots);
				}
#if TEST_ENABLED
				if (outline.invalidLines != null && outline.invalidLines.Length > 0)
				{
					Handles.color = Color.red;
					Handles.DrawDottedLines(outline.vertices, outline.invalidLines, invalidLineDots);
				}
#endif
			}
			//if (visibleTriangles != null && visibleTriangles.Length > 0 && surfaceColor.a > 0)
			//{
				//PaintUtility.DrawTriangles(transformation, vertices, visibleTriangles, surfaceColor);
			//}
		}

		const float kMinAlpha = 1 / 255.0f;

		public static void DrawSurfaceOutlines(LineMeshManager	   visibleLinesMeshManager, 
											   LineMeshManager	   invisibleLinesMeshManager,
											   PolygonMeshManager  visibleSurfaceMeshManager,
											   GeometryWireframe[] outlines, 
											   Matrix4x4[] transformations,
											   Color visibleInnerColor,   Color visibleOuterColor,   Color visibleOutlineColor,
											   Color invisibleInnerColor, Color invisibleOuterColor, Color invisibleOutlineColor,
											   Color surfaceColor, 
											   float thickness = GUIConstants.thickLineScale)
		{
			if (outlines == null)
				return;
			
			if (invisibleOutlineColor.a >= kMinAlpha)
			{
				for (int i = 0; i < outlines.Length; i++)
				{
					var outline			= outlines[i];
					var transformation	= transformations[i];

					if (outline.visibleOuterLines != null && outline.visibleOuterLines.Length > 0)
					{
						invisibleLinesMeshManager.DrawLines(transformation, outline.vertices, outline.visibleOuterLines, invisibleOutlineColor);
					}
					if (outline.invisibleOuterLines != null && outline.invisibleOuterLines.Length > 0)
					{
						invisibleLinesMeshManager.DrawLines(transformation, outline.vertices, outline.invisibleOuterLines, invisibleOutlineColor);
					}
					if (outline.visibleInnerLines != null && outline.visibleInnerLines.Length > 0)
					{
						invisibleLinesMeshManager.DrawLines(transformation, outline.vertices, outline.visibleInnerLines, invisibleOutlineColor);
					}
					if (outline.invisibleInnerLines != null && outline.invisibleInnerLines.Length > 0)
					{
						invisibleLinesMeshManager.DrawLines(transformation, outline.vertices, outline.invisibleInnerLines, invisibleOutlineColor);
					}
				}
			}
			
			if (invisibleInnerColor.a >= kMinAlpha)
			{
				for (int i = 0; i < outlines.Length; i++)
				{
					var outline		= outlines[i];
					var transformation = transformations[i];

					if (outline.visibleOuterLines != null && outline.visibleOuterLines.Length > 0)
					{
						invisibleLinesMeshManager.DrawLines(transformation, outline.vertices, outline.visibleOuterLines, invisibleInnerColor, dashSize: visibleOuterLineDots);
					}
					if (outline.invisibleOuterLines != null && outline.invisibleOuterLines.Length > 0)
					{
						invisibleLinesMeshManager.DrawLines(transformation, outline.vertices, outline.invisibleOuterLines, invisibleInnerColor, dashSize: invisibleOuterLineDots);
					}
				}
			}
			

			if (invisibleOuterColor.a >= kMinAlpha)
			{
				for (int i = 0; i < outlines.Length; i++)
				{
					var outline		= outlines[i];
					var transformation = transformations[i];

					if (outline.visibleInnerLines != null && outline.visibleInnerLines.Length > 0)
					{
						invisibleLinesMeshManager.DrawLines(transformation, outline.vertices, outline.visibleInnerLines, invisibleOuterColor, dashSize: visibleInnerLineDots);
					}
					if (outline.invisibleInnerLines != null && outline.invisibleInnerLines.Length > 0)
					{
						invisibleLinesMeshManager.DrawLines(transformation, outline.vertices, outline.invisibleInnerLines, invisibleOuterColor, dashSize: invisibleInnerLineDots);
					}
				}
			}
			
#if TEST_ENABLED
			for (int i = 0; i < outlines.Length; i++)
			{
				var outline = outlines[i];
				if (outline.invalidLines == null || outline.invalidLines.Length == 0)
					continue;

				var transformation = transformations[i];

				invisibleLinesMeshManager.DrawLines(transformation, outline.vertices, outline.invalidLines, Color.red, dashSize: invalidLineDots);
			}
#endif

			if (visibleOutlineColor.a >= kMinAlpha)
			{
				for (int i = 0; i < outlines.Length; i++)
				{
					var outline = outlines[i];

					var transformation = transformations[i];

					if (outline.visibleOuterLines != null && outline.visibleOuterLines.Length != 0)
					{
						visibleLinesMeshManager.DrawLines(transformation, outline.vertices, outline.visibleOuterLines, visibleOutlineColor, thickness: thickness + 2.0f);
					}
					if (outline.visibleInnerLines != null && outline.visibleInnerLines.Length != 0)
					{
						visibleLinesMeshManager.DrawLines(transformation, outline.vertices, outline.visibleInnerLines, visibleOutlineColor, thickness: thickness + 2.0f);
					}
				}
			}

			if (visibleOuterColor.a >= kMinAlpha || visibleInnerColor.a >= kMinAlpha)
			{
				for (int i = 0; i < outlines.Length; i++)
				{
					var outline = outlines[i];

					var transformation = transformations[i];

					if (visibleInnerColor.a >= kMinAlpha && outline.visibleOuterLines != null && outline.visibleOuterLines.Length != 0)
					{
						visibleLinesMeshManager.DrawLines(transformation, outline.vertices, outline.visibleOuterLines, visibleInnerColor, thickness: thickness * 0.5f);
					}
					if (visibleOuterColor.a >= kMinAlpha && outline.visibleInnerLines != null && outline.visibleInnerLines.Length != 0)
					{
						visibleLinesMeshManager.DrawLines(transformation, outline.vertices, outline.visibleInnerLines, visibleOuterColor, thickness: thickness * 0.5f);
					}
				}
			}

			if (surfaceColor.a >= kMinAlpha)
			{
				for (int i = 0; i < outlines.Length; i++)
				{
					var outline = outlines[i];

					if (outline.visibleTriangles == null || outline.visibleTriangles.Length == 0)
						continue;

					var transformation = transformations[i];
					
					visibleSurfaceMeshManager.DrawTriangles(transformation, outline.vertices, outline.visibleTriangles, surfaceColor);
				}
			}
		}


		public static void DrawOutlines(GeometryWireframe outline, Matrix4x4 transformation, 
										Color outerColor, Color outerColorOccluded, Color innerColor, Color innerColorOccluded, 
										float thickness = -1)
		{
			if (outline == null || 
				outline.vertices == null ||
				outline.vertices.Length == 0 ||

				(outline.visibleOuterLines		== null &&
				 outline.invisibleOuterLines	== null &&
				 outline.visibleInnerLines		== null &&
				 outline.invisibleInnerLines	== null &&
				 outline.invalidLines			== null))
				return;
            
			Handles.matrix = transformation;

			if (outline.visibleOuterLines != null && outline.visibleOuterLines.Length > 0)
			{
				if (thickness <= 0)
				{
					PaintUtility.DrawLines(transformation, outline.vertices, outline.visibleOuterLines, outerColor);
				} else
				{
					//PaintUtility.DrawUnoccludedLines(transformation, outline.vertices, outline.visibleOuterLines, outerColor);
					PaintUtility.DrawLines(transformation, outline.vertices, outline.visibleOuterLines, thickness, outerColor);
				}
			}
			
			if (outline.visibleInnerLines != null && outline.visibleInnerLines.Length > 0)
			{
				if (thickness <= 0)
				{
					PaintUtility.DrawLines(transformation, outline.vertices, outline.visibleInnerLines, innerColor);
				} else
				{
					//PaintUtility.DrawUnoccludedLines(transformation, outline.vertices, outline.visibleInnerLines, innerColor);
					PaintUtility.DrawLines(transformation, outline.vertices, outline.visibleInnerLines, thickness, innerColor);
				}
			}

			if (outline.visibleOuterLines != null && outline.visibleOuterLines.Length > 0)
			{
				Handles.color = outerColorOccluded;
				Handles.DrawDottedLines(outline.vertices, outline.visibleOuterLines, visibleOuterLineDots);
			}
			if (outline.visibleInnerLines != null && outline.visibleInnerLines.Length > 0)
			{
				Handles.color = innerColorOccluded;
				Handles.DrawDottedLines(outline.vertices, outline.visibleInnerLines, visibleInnerLineDots);
			}

			if (outline.invisibleOuterLines != null && outline.invisibleOuterLines.Length > 0)
			{
				Handles.color = outerColorOccluded;
				Handles.DrawDottedLines(outline.vertices, outline.invisibleOuterLines, invisibleOuterLineDots);
			}
			if (outline.invisibleInnerLines != null && outline.invisibleInnerLines.Length > 0)
			{
				Handles.color = innerColor;
				Handles.DrawDottedLines(outline.vertices, outline.invisibleInnerLines, invisibleInnerLineDots);
			}
#if TEST_ENABLED
			if (outline.invalidLines != null && outline.invalidLines.Length > 0)
			{
				Handles.color = Color.red;
				Handles.DrawDottedLines(outline.vertices, outline.invalidLines, invalidLineDots);
			}
#endif
		}


		public static void DrawOutlines(LineMeshManager zTestLineMeshManager, LineMeshManager noZTestLineMeshManager, 
										GeometryWireframe[] outlines, Matrix4x4[] transformations, 
										Color outerColor, Color outerColorOccluded, Color innerColor, Color innerColorOccluded, 
										float thickness = -1)
		{
			if (Event.current.type != EventType.Repaint)
			{
				return;
			}

			if (outlines == null || transformations == null ||
				outlines.Length != transformations.Length)
			{
				zTestLineMeshManager.Clear();
				noZTestLineMeshManager.Clear();
				return;
			}

			zTestLineMeshManager.Begin();
			if (thickness <= 0)
			{
				for (int i = 0; i < outlines.Length; i++)
				{
					var outline = outlines[i];
					if (outline == null ||
						outline.vertices == null ||
						outline.vertices.Length == 0)
						continue;

					var transformation = transformations[i];

					if (outline.visibleOuterLines != null && outline.visibleOuterLines.Length > 0)
					{
						zTestLineMeshManager.DrawLines(transformation, outline.vertices, outline.visibleOuterLines, outerColor);//, zTest: true);
						//PaintUtility.DrawLines(transformation, outline.vertices, outline.visibleOuterLines, outerColor);//CustomWireMaterial
					}

					if (outline.visibleInnerLines != null && outline.visibleInnerLines.Length > 0)
					{
						zTestLineMeshManager.DrawLines(transformation, outline.vertices, outline.visibleInnerLines, innerColor);//, zTest: true);//CustomWireMaterial
						//PaintUtility.DrawLines(transformation, outline.vertices, outline.visibleInnerLines, innerColor);//CustomWireMaterial
					}
				}
			} else
			{
				for (int i = 0; i < outlines.Length; i++)
				{
					var outline = outlines[i];
					if (outline == null ||
						outline.vertices == null ||
						outline.vertices.Length == 0)
						continue;
										
					var transformation = transformations[i];
					
					if (outline.visibleOuterLines != null && outline.visibleOuterLines.Length > 0)
					{
						//PaintUtility.DrawUnoccludedLines(transformation, outline.vertices, outline.visibleOuterLines, outerColor);
						zTestLineMeshManager.DrawLines(transformation, outline.vertices, outline.visibleOuterLines, outerColor, thickness: thickness);//, zTest: true);
						//PaintUtility.DrawLines(transformation, outline.vertices, outline.visibleOuterLines, thickness, outerColor);//CustomThickWireMaterial
					}

					if (outline.visibleInnerLines != null && outline.visibleInnerLines.Length > 0)
					{
						//PaintUtility.DrawUnoccludedLines(transformation, outline.vertices, outline.visibleInnerLines, innerColor);
						zTestLineMeshManager.DrawLines(transformation, outline.vertices, outline.visibleInnerLines, innerColor, thickness: thickness);//, zTest: true);
						//PaintUtility.DrawLines(transformation, outline.vertices, outline.visibleInnerLines, thickness, innerColor);//CustomThickWireMaterial
					}
				}
			}
			zTestLineMeshManager.End();
			
			noZTestLineMeshManager.Begin();
			for (int i = 0; i < outlines.Length; i++)
			{
				var outline = outlines[i];
				if (outline == null ||
					outline.vertices == null ||
					outline.vertices.Length == 0)
					continue;

				
				var transformation = transformations[i];
				Handles.matrix = transformation;

				if (outline.visibleOuterLines != null && outline.visibleOuterLines.Length > 0)
				{
					//Handles.color = outerColorOccluded;
					//Handles.DrawDottedLines(outline.vertices, outline.visibleOuterLines, visibleOuterLineDots);	// internal
					noZTestLineMeshManager.DrawLines(transformation, outline.vertices, outline.visibleOuterLines, outerColorOccluded, dashSize: visibleOuterLineDots);//, zTest: false);
				}

				if (outline.visibleInnerLines != null && outline.visibleInnerLines.Length > 0)
				{
					//Handles.color = innerColorOccluded;
					//Handles.DrawDottedLines(outline.vertices, outline.visibleInnerLines, visibleInnerLineDots); // internal
					noZTestLineMeshManager.DrawLines(transformation, outline.vertices, outline.visibleInnerLines, innerColorOccluded, dashSize: visibleInnerLineDots);//, zTest: false);
				}

				if (outline.invisibleOuterLines != null && outline.invisibleOuterLines.Length > 0)
				{
					//Handles.color = outerColorOccluded;
					//Handles.DrawDottedLines(outline.vertices, outline.invisibleOuterLines, invisibleOuterLineDots); // internal
					noZTestLineMeshManager.DrawLines(transformation, outline.vertices, outline.invisibleOuterLines, outerColorOccluded, dashSize: invisibleOuterLineDots);//, zTest: false);
				}
				if (outline.invisibleInnerLines != null && outline.invisibleInnerLines.Length > 0)
				{
					//Handles.color = innerColor;
					//Handles.DrawDottedLines(outline.vertices, outline.invisibleInnerLines, invisibleInnerLineDots); // internal
					noZTestLineMeshManager.DrawLines(transformation, outline.vertices, outline.invisibleInnerLines, innerColor, dashSize: invisibleInnerLineDots);//, zTest: false);
				}
#if TEST_ENABLED
				if (outline.invalidLines != null && outline.invalidLines.Length > 0)
				{
					//Handles.color = Color.red;
					//Handles.DrawDottedLines(outline.vertices, outline.invalidLines, invalidLineDots);   // internal
					noZTestLineMeshManager.DrawLines(transformation, outline.vertices, outline.invalidLines, Color.red, dashSize: invalidLineDots);//, zTest: false);
				}
#endif
			}

			noZTestLineMeshManager.End();
		}

		public static void DrawOutlines(Int32 brushNodeID, Matrix4x4 transformation, Color outerColor, Color outerColorOccluded, Color innerColor, Color innerColorOccluded, float thickness = -1)
		{
			// .. could be a prefab
			if (brushNodeID == CSGNode.InvalidNodeID)
				return;

			var outline = BrushOutlineManager.GetBrushOutline(brushNodeID);
			DrawOutlines(outline, transformation, outerColor, outerColorOccluded, innerColor, innerColorOccluded, thickness);
		}
		

		public static void DrawSimpleOutlines(LineMeshManager lineMeshManager, GeometryWireframe outline, Matrix4x4 transformation, Color color)
		{
			if (outline == null || 
				outline.vertices == null ||
				outline.vertices.Length == 0 ||

				(outline.visibleOuterLines		== null &&
				 outline.invisibleOuterLines	== null &&
				 outline.visibleInnerLines		== null &&
				 outline.invisibleInnerLines	== null &&
				 outline.invalidLines			== null))
				return;
            
			var vertices = outline.vertices;
			var indices  = outline.visibleOuterLines;
			if (indices != null &&
				indices.Length > 0 &&
				(indices.Length & 1) == 0)
			{
				lineMeshManager.DrawLines(transformation, vertices, indices, color);
			}
				
			indices = outline.invisibleOuterLines;
			if (indices != null &&
				indices.Length > 0 &&
				(indices.Length & 1) == 0)
			{
				lineMeshManager.DrawLines(transformation, vertices, indices, color);
			}
		}

		public static void DrawSimpleOutlines(LineMeshManager lineMeshManager, Int32 brushNodeID, Matrix4x4 transformation, Color color)
		{
			// .. could be a prefab
			if (brushNodeID == CSGNode.InvalidNodeID)
				return;

			var outline = BrushOutlineManager.GetBrushOutline(brushNodeID);
			DrawSimpleOutlines(lineMeshManager, outline, transformation, color);
		}
	}
}
