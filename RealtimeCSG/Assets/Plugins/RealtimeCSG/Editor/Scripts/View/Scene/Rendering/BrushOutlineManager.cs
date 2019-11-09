using System.Collections.Generic;
using RealtimeCSG;
using RealtimeCSG.Components;

namespace InternalRealtimeCSG
{
	internal class BrushOutlineManager
	{
		private static readonly Dictionary<int, GeometryWireframe> OutlineCache = new Dictionary<int, GeometryWireframe>();

		#region ClearOutlines
		public static void ClearOutlines()
		{
			OutlineCache.Clear();
		}
		#endregion

		#region ForceUpdateOutlines
		public static void ForceUpdateOutlines(int brushNodeId)
		{
			var externalOutlineGeneration = InternalCSGModelManager.External.GetBrushOutlineGeneration(brushNodeId);
			var outline = new GeometryWireframe();
			if (!InternalCSGModelManager.External.GetBrushOutline(brushNodeId, out outline))
				return;

			outline.outlineGeneration = externalOutlineGeneration;
			OutlineCache[brushNodeId] = outline;
		}
		#endregion

		#region GetBrushOutline
		public static GeometryWireframe GetBrushOutline(int brushNodeId)
		{
			if (brushNodeId == CSGNode.InvalidNodeID)
				return null;

			var externalOutlineGeneration = InternalCSGModelManager.External.GetBrushOutlineGeneration(brushNodeId);

			GeometryWireframe outline;
			if (!OutlineCache.TryGetValue(brushNodeId, out outline))
				externalOutlineGeneration = externalOutlineGeneration - 1;
			
			if (outline != null &&
				externalOutlineGeneration == outline.outlineGeneration)
				return outline;

			outline = new GeometryWireframe();
			if (!InternalCSGModelManager.External.GetBrushOutline(brushNodeId, out outline))
				return null;
			
			outline.outlineGeneration = externalOutlineGeneration;
			OutlineCache[brushNodeId] = outline;
			return outline;
		}

		public static GeometryWireframe[] GetBrushOutlines(int[] brushNodeIDs)
		{
			var wireframes = new GeometryWireframe[brushNodeIDs.Length];

			for (var i = 0; i < brushNodeIDs.Length; i++)
			{
				var brushNodeId = brushNodeIDs[i];
				if (brushNodeId == CSGNode.InvalidNodeID)
				{
					wireframes[i] = null;
					continue;
				}
				var externalOutlineGeneration = InternalCSGModelManager.External.GetBrushOutlineGeneration(brushNodeId);

				GeometryWireframe outline;
				if (!OutlineCache.TryGetValue(brushNodeId, out outline))
					externalOutlineGeneration = externalOutlineGeneration - 1;
				
				if (outline == null ||
					externalOutlineGeneration != outline.outlineGeneration)
				{
					if (!InternalCSGModelManager.External.GetBrushOutline(brushNodeId, out outline))
					{
						outline = null;
					} else
					{
						outline.outlineGeneration = externalOutlineGeneration;
						OutlineCache[brushNodeId] = outline;
					}
				}
				wireframes[i] = outline;
			}
			return wireframes;
		}
		#endregion

		#region GetSurfaceOutline
		public static GeometryWireframe[] GetSurfaceOutlines(SelectedBrushSurface[] selectedSurfaces)
		{
			if (selectedSurfaces == null || selectedSurfaces.Length == 0)
				return new GeometryWireframe[0];

			var wireframes = new GeometryWireframe[selectedSurfaces.Length];
			for (var i = 0; i < selectedSurfaces.Length; i++)
			{
				var brush = selectedSurfaces[i].brush;
				if (!brush || brush.brushNodeID == CSGNode.InvalidNodeID) 
				{
					wireframes[i] = null;
					continue;
				}

				var brushNodeId		= brush.brushNodeID;
				var surfaceIndex	= selectedSurfaces[i].surfaceIndex;
				
				GeometryWireframe outline;
				if (!InternalCSGModelManager.External.GetSurfaceOutline(brushNodeId,
																	    surfaceIndex,
																	    out outline))
				{
					outline = null;
				}
				wireframes[i] = outline;
			}
			return wireframes;
		}
		#endregion

	}
}
