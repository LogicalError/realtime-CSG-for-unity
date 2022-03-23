using UnityEditor;
using RealtimeCSG.Legacy;
using RealtimeCSG.Components;
using UnityEngine;

namespace RealtimeCSG
{
	internal class BrushOutlineRenderer
	{
		private readonly LineMeshManager _outlinesManager = new LineMeshManager();
		private readonly LineMeshManager _edgeColorsManager = new LineMeshManager();
        private readonly PolygonMeshManager _polygonManager = new PolygonMeshManager();

        public void Destroy()
		{
			_outlinesManager.Destroy();
			_edgeColorsManager.Destroy();
            _polygonManager.Destroy();
		}

		public void Update(Camera camera, CSGBrush[] brushes, ControlMesh[] controlMeshes, ControlMeshState[] meshStates)
		{
		    if (brushes.Length == 0)
            {
                _outlinesManager.Clear();
                _edgeColorsManager.Clear();
                _polygonManager.Clear();
                return;
            }

			_outlinesManager.Begin();
			_edgeColorsManager.Begin();
            _polygonManager.Begin();
			for (var t = 0; t < brushes.Length; t++)
			{
				var brush = brushes[t];
				if (!brush)
					continue;

				var meshState = meshStates[t];
				if (meshState.WorldPoints.Length == 0 &&
					meshState.Edges.Length == 0)
					continue;

				if (!meshState.UpdateColors(camera, brush, controlMeshes[t]))
					continue;

				_outlinesManager.DrawLines(meshState.WorldPoints, meshState.Edges, ColorSettings.MeshEdgeOutline, thickness: 1.0f);//, zTest: false);
				_edgeColorsManager.DrawLines(meshState.WorldPoints, meshState.Edges, meshState.EdgeColors, thickness: 1.0f);//, zTest: false);
				
			    for (int p = 0; p < meshState.PolygonPointIndices.Length; p++)
			    {
                    if (meshState.PolygonColors[p].a < (1.0f / 255.0f))
                        continue;

			        var color = meshState.PolygonColors[p];
			        var polygonPoints = meshState.PolygonPointIndices[p];
			        _polygonManager.DrawPolygon(meshState.WorldPoints, polygonPoints, color);
			    }
			}
            _polygonManager.End();
			_edgeColorsManager.End();
			_outlinesManager.End();
		}

		public void RenderOutlines()
		{
			var zTestGenericLineMaterial    = MaterialUtility.ZTestGenericLine;
			var noZTestGenericLineMaterial  = MaterialUtility.NoZTestGenericLine;
            var coloredPolygonMaterial      = MaterialUtility.ColoredPolygonMaterial;

            _polygonManager.Render(coloredPolygonMaterial);
			
			MaterialUtility.LineAlphaMultiplier = 0.75f;
            MaterialUtility.LineDashMultiplier = 4.0f;
			MaterialUtility.LineThicknessMultiplier = GUIConstants.thickLineScale * 2.0f;
			_outlinesManager.Render(noZTestGenericLineMaterial);
			
			MaterialUtility.LineDashMultiplier = 0.0f;
			MaterialUtility.LineThicknessMultiplier = GUIConstants.thickLineScale * 2.0f;
			_outlinesManager.Render(zTestGenericLineMaterial);


			MaterialUtility.LineDashMultiplier = 4.0f;
			MaterialUtility.LineThicknessMultiplier = GUIConstants.thickLineScale;
			_edgeColorsManager.Render(noZTestGenericLineMaterial);

			MaterialUtility.LineDashMultiplier = 0.0f;
			MaterialUtility.LineThicknessMultiplier = GUIConstants.thickLineScale;
			_edgeColorsManager.Render(zTestGenericLineMaterial);

            MaterialUtility.LineAlphaMultiplier = 1.0f;
		}
	}
}
