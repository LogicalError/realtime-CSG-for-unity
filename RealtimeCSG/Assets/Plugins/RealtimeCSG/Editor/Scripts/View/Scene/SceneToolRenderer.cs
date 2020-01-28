using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealtimeCSG
{
	internal static class SceneToolRenderer
	{
		static int meshGeneration = -1;

		static LineMeshManager lineMeshManager = new LineMeshManager();

		internal static void Cleanup()
		{
			meshGeneration = -1;
			lineMeshManager.Destroy();
		}

		static bool forceOutlineUpdate = false;
		internal static void SetOutlineDirty() { forceOutlineUpdate = false; }
		
		internal static void OnPaint(SceneView sceneView)
        {
            if (!sceneView)
                return;

            var camera = sceneView.camera;
			SceneDragToolManager.OnPaint(camera);

			if (Event.current.type != EventType.Repaint)
				return;

            if (RealtimeCSG.CSGSettings.GridVisible)
				RealtimeCSG.CSGGrid.RenderGrid(camera);
			
			if (RealtimeCSG.CSGSettings.IsWireframeShown(sceneView))
			{
				if (forceOutlineUpdate || meshGeneration != InternalCSGModelManager.MeshGeneration)
				{
					forceOutlineUpdate = false;
					meshGeneration = InternalCSGModelManager.MeshGeneration;
					lineMeshManager.Begin();
					for (int i = 0; i < InternalCSGModelManager.Brushes.Count; i++)
					{
						var brush = InternalCSGModelManager.Brushes[i];
						if (!brush)
							continue;

						if (!brush.outlineColor.HasValue)
							brush.outlineColor = ColorSettings.GetBrushOutlineColor(brush);
						
						var brush_transformation = brush.compareTransformation.localToWorldMatrix;
						CSGRenderer.DrawSimpleOutlines(lineMeshManager, brush.brushNodeID, brush_transformation, brush.outlineColor.Value);
					}
					lineMeshManager.End();
				}

				MaterialUtility.LineDashMultiplier = 1.0f;
				MaterialUtility.LineThicknessMultiplier = 1.0f;
				MaterialUtility.LineAlphaMultiplier = 1.0f;
				lineMeshManager.Render(MaterialUtility.NoZTestGenericLine);
			}
		}
	}
}
