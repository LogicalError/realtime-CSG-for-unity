using InternalRealtimeCSG;
using UnityEditor;
using UnityEngine;

namespace RealtimeCSG.Helpers
{
	public class CSGBounds
	{
		internal static Vector3[]	sidePoints		= new Vector3[6];
		internal static float[]		sidePointSizes	= new float[6];
		internal static float[]		sideSizes		= new float[6];
		internal static Color[]		sideColors		= new Color[12];
		internal static bool[]		sideBackfaced	= new bool[6];
		internal static int[]		sideControlIDs	= new int[6];
		
		internal static Vector3[]	edgePoints		= new Vector3[12];
		internal static float[]		edgePointSizes	= new float[12];
		internal static float[]		edgeSizes		= new float[12];
		internal static Color[]		edgeColors		= new Color[24];
		internal static int[]		edgeControlIDs	= new int[12];

		internal static Vector3[]	cornerPoints	= new Vector3[8];

		internal static int			prevNearestControl = 0;
		internal static AABB		originalBounds	= new AABB();
		internal static readonly int BoundsHash = "Bounds".GetHashCode();		

		internal static AABB Do(Camera camera, AABB bounds, Quaternion worldToLocalRotation, bool showEdgePoints = true)
		{
			for (int i = 0; i < sideControlIDs.Length; i++)
				sideControlIDs[i] = GUIUtility.GetControlID(BoundsHash, FocusType.Passive);

			for (int i = 0; i < edgeControlIDs.Length; i++)
				edgeControlIDs[i] = GUIUtility.GetControlID(BoundsHash, FocusType.Passive);

			UpdateColors(camera, bounds, worldToLocalRotation, showEdgePoints);

			var evt = Event.current;
			switch (evt.type)
			{
				case EventType.Repaint:
				{
					Render(camera, bounds, worldToLocalRotation, showEdgePoints);
					break;
				}
			}

			//var isStatic = (!Tools.hidden && EditorApplication.isPlaying && ContainsStatic(Selection.gameObjects));

			//var prevDisabled = CSGHandles.disabled;


			var localToWorldRotation = Quaternion.Inverse(worldToLocalRotation);
			var localToWorld = Matrix4x4.TRS(Vector3.zero, localToWorldRotation, Vector3.one);

			var origMatrix = Handles.matrix;
			Handles.matrix = localToWorld;

			RealtimeCSG.Helpers.CSGHandles.InitFunction init = delegate
			{
				originalBounds = bounds;
			};
			RealtimeCSG.Helpers.CSGHandles.InitFunction shutdown = delegate
			{
				originalBounds = bounds;
			};

			var activeSnappingMode = RealtimeCSG.CSGSettings.ActiveSnappingMode;
			for (int i = 0; i < sidePoints.Length; i++)
			{
				Handles.color	= sideColors[(i * 2) + 1];

				var position	= sidePoints[i];
				var normal		= BoundsUtilities.AABBSideNormal[i];
				var size		= sideSizes[i];
				var id			= sideControlIDs[i];
				EditorGUI.BeginChangeCheck();
				position = CSGSlider1D.Do(camera, id, position, normal, size, CSGHandles.HoverArrowHandleCap, activeSnappingMode, null, init, shutdown);
				if (EditorGUI.EndChangeCheck())
				{
					var originalPoint = BoundsUtilities.GetBoundsSidePoint(originalBounds, i);
					bounds = originalBounds;

					var delta = Vector3.Dot(normal, (position - originalPoint));
					if		(normal.x < 0) bounds.MinX -= delta;
					else if (normal.x > 0) bounds.MaxX += delta;
					else if	(normal.y < 0) bounds.MinY -= delta;
					else if (normal.y > 0) bounds.MaxY += delta;
					else if (normal.z < 0) bounds.MinZ -= delta;
					else if (normal.z > 0) bounds.MaxZ += delta;
				}
			}
			
			if (showEdgePoints)
			{
				for (int i = 0; i < edgePoints.Length; i++)
				{
					Handles.color	= edgeColors[(i * 2) + 1];

					var position	= edgePoints[i];
					var sideIndices = BoundsUtilities.AABBEdgeSides[i];
					var normal		= BoundsUtilities.AABBEdgeTangents[i];
					var direction1	= BoundsUtilities.AABBSideNormal[sideIndices[0]];
					var direction2	= BoundsUtilities.AABBSideNormal[sideIndices[1]];
					var size		= edgeSizes[i] / 20.0f;
					var id			= edgeControlIDs[i];
					EditorGUI.BeginChangeCheck();
					position = CSGHandles.Slider2D(camera, id, position, Vector3.zero, normal, direction1, direction2, size, null, activeSnappingMode, null, init, shutdown);
					if (EditorGUI.EndChangeCheck())
					{
						var originalPoint = BoundsUtilities.GetBoundsEdgePoint(originalBounds, i);
						bounds = originalBounds;

						var delta = (position - originalPoint);
						var sides = BoundsUtilities.AABBEdgeCubeSides[i];
						switch (sides[0])
						{
							case CubeSide.NegativeX: bounds.MinX += delta.x; break;
							case CubeSide.PositiveX: bounds.MaxX += delta.x; break;
							case CubeSide.NegativeY: bounds.MinY += delta.y; break;
							case CubeSide.PositiveY: bounds.MaxY += delta.y; break;
							case CubeSide.NegativeZ: bounds.MinZ += delta.z; break;
							case CubeSide.PositiveZ: bounds.MaxZ += delta.z; break;
						}

						switch (sides[1])
						{
							case CubeSide.NegativeX: bounds.MinX += delta.x; break;
							case CubeSide.PositiveX: bounds.MaxX += delta.x; break;
							case CubeSide.NegativeY: bounds.MinY += delta.y; break;
							case CubeSide.PositiveY: bounds.MaxY += delta.y; break;
							case CubeSide.NegativeZ: bounds.MinZ += delta.z; break;
							case CubeSide.PositiveZ: bounds.MaxZ += delta.z; break;
						}
					}
				}
			}

			Handles.matrix = origMatrix;

			if (bounds.MinX > bounds.MaxX) { float t = bounds.MinX; bounds.MinX = bounds.MaxX; bounds.MaxX = t; }
			if (bounds.MinY > bounds.MaxY) { float t = bounds.MinY; bounds.MinY = bounds.MaxY; bounds.MaxY = t; }
			if (bounds.MinZ > bounds.MaxZ) { float t = bounds.MinZ; bounds.MinZ = bounds.MaxZ; bounds.MaxZ = t; }
			return bounds;
		}

		internal static void UpdateColors(Camera camera, AABB bounds, Quaternion worldToLocalRotation, bool showEdgePoints)
		{
			if (!camera)
				return;

			var localToWorldRotation = Quaternion.Inverse(worldToLocalRotation);

			BoundsUtilities.GetBoundsCornerPoints(bounds, cornerPoints);
			BoundsUtilities.GetBoundsSidePoints(bounds, sidePoints);
			BoundsUtilities.GetBoundsEdgePoints(bounds, edgePoints);


			var boundOutlinesColor	= ColorSettings.MeshEdgeOutline;
			var localToWorld = Matrix4x4.TRS(Vector3.zero, localToWorldRotation, Vector3.one);
			PaintUtility.DrawDottedLines(localToWorld, cornerPoints, BoundsUtilities.AABBLineIndices, boundOutlinesColor, 4.0f);
			PaintUtility.DrawLines(localToWorld, cornerPoints, BoundsUtilities.AABBLineIndices, GUIConstants.oldLineScale, boundOutlinesColor);


			var isOrtho = camera.orthographic;

			if (isOrtho)
			{
				var cameraDirection = worldToLocalRotation * camera.transform.forward;
				for (int i = 0; i < BoundsUtilities.AABBSideNormal.Length; i++)
				{
					var normal = BoundsUtilities.AABBSideNormal[i];
					sideBackfaced[i] = Mathf.Abs(Vector3.Dot(normal, cameraDirection)) > 0.9999f;
				}
			} else
			{
				var cameraPosition	= worldToLocalRotation * camera.transform.position;
				for (int i = 0; i < BoundsUtilities.AABBSideNormal.Length; i++)
				{
					var normal = BoundsUtilities.AABBSideNormal[i];
					sideBackfaced[i] = Vector3.Dot(normal, (cameraPosition - sidePoints[i])) < 0;
				}
			}

			var nearestControl	= HandleUtility.nearestControl;
			var hotControl		= GUIUtility.hotControl;

			for (int i = 0; i < sidePoints.Length; i++)
			{
				var id = sideControlIDs[i];
				var state  = (hotControl == id || nearestControl == id) ? SelectState.Hovering : SelectState.None;
				var color1 = ColorSettings.MeshEdgeOutline;
				var color2 = ColorSettings.PolygonInnerStateColor[(int)state];

				bool disabled = false;
				switch (BoundsUtilities.AABBSideAxis[i])
				{
					case PrincipleAxis.X: disabled = RealtimeCSG.CSGSettings.LockAxisX; break;
					case PrincipleAxis.Y: disabled = RealtimeCSG.CSGSettings.LockAxisY; break;
					case PrincipleAxis.Z: disabled = RealtimeCSG.CSGSettings.LockAxisZ; break;
				}
				if (disabled)
					color2 = Color.red;

				var handleSize = HandleUtility.GetHandleSize(localToWorldRotation * sidePoints[i]);
				var pointHandleSize = handleSize / 20.0f;
				if (sideBackfaced[i])
				{
					if (!isOrtho)
					{
						color1.a *= GUIConstants.backfaceTransparency;
						color2.a *= GUIConstants.backfaceTransparency;
						pointHandleSize *= GUIConstants.backHandleScale;
					} else
						pointHandleSize = 0;
				} else
					pointHandleSize *= GUIConstants.handleScale;

				sidePointSizes[i] = pointHandleSize;
				sideSizes[i] = handleSize;
				sideColors[(i * 2) + 0] = color1;
				sideColors[(i * 2) + 1] = color2;
			}

			if (showEdgePoints)
			{ 
				for (int i = 0; i < edgePoints.Length; i++)
				{
					var id = edgeControlIDs[i];
					var state = (hotControl == id || nearestControl == id) ? SelectState.Hovering : SelectState.None;
					var color1 = ColorSettings.MeshEdgeOutline;
					var color2 = ColorSettings.PointInnerStateColor[(int)state];

					bool disabled1 = false;
					switch (BoundsUtilities.AABBEdgeAxis[i][0])
					{
						case PrincipleAxis.X: disabled1 = RealtimeCSG.CSGSettings.LockAxisX; break;
						case PrincipleAxis.Y: disabled1 = RealtimeCSG.CSGSettings.LockAxisY; break;
						case PrincipleAxis.Z: disabled1 = RealtimeCSG.CSGSettings.LockAxisZ; break;
					}
					bool disabled2 = false;
					switch (BoundsUtilities.AABBEdgeAxis[i][1])
					{
						case PrincipleAxis.X: disabled2 = RealtimeCSG.CSGSettings.LockAxisX; break;
						case PrincipleAxis.Y: disabled2 = RealtimeCSG.CSGSettings.LockAxisY; break;
						case PrincipleAxis.Z: disabled2 = RealtimeCSG.CSGSettings.LockAxisZ; break;
					}
					if (disabled1 && disabled2)
						color2 = Color.red;

					var handleSize = HandleUtility.GetHandleSize(localToWorldRotation * edgePoints[i]);
					var pointHandleSize = handleSize / 20.0f;
					var side1 = BoundsUtilities.AABBEdgeSides[i][0];
					var side2 = BoundsUtilities.AABBEdgeSides[i][1];
					if (isOrtho)
					{
						if (sideBackfaced[side1] || sideBackfaced[side2])
						{
							pointHandleSize = 0;
						} else
							pointHandleSize *= GUIConstants.handleScale;
					} else
					{
						if (sideBackfaced[side1] && sideBackfaced[side2])
						{
							color1.a *= GUIConstants.backfaceTransparency;
							color2.a *= GUIConstants.backfaceTransparency;
							pointHandleSize *= GUIConstants.backHandleScale;
						} else
							pointHandleSize *= GUIConstants.handleScale;
					}

					edgePointSizes[i] = pointHandleSize;
					edgeSizes[i] = handleSize;
					edgeColors[(i * 2) + 0] = color1;
					edgeColors[(i * 2) + 1] = color2;
				}
			}

		}

		internal static void Render(Camera camera, AABB bounds, Quaternion worldToLocalRotation, bool showEdgePoints)
		{
			if (!camera)
				return;

			var localToWorldRotation = Quaternion.Inverse(worldToLocalRotation);
			var localToWorld = Matrix4x4.TRS(Vector3.zero, localToWorldRotation, Vector3.one);


			//PaintUtility.DrawDoubleDots(localToWorld, cornerPoints, cornerSizes, cornerColors, cornerPoints.Length);
			PaintUtility.DrawDoubleDots(camera, localToWorld, sidePoints, sidePointSizes, sideColors, sidePoints.Length);
			if (showEdgePoints)
				PaintUtility.DrawDoubleDots(camera, localToWorld, edgePoints, edgePointSizes, edgeColors, edgePoints.Length);

			PaintUtility.RenderBoundsSizes(worldToLocalRotation, localToWorldRotation, camera, cornerPoints, 						
											RealtimeCSG.CSGSettings.LockAxisX ? Color.red : Color.white, 
											RealtimeCSG.CSGSettings.LockAxisY ? Color.red : Color.white, 
											RealtimeCSG.CSGSettings.LockAxisZ ? Color.red : Color.white,
											true, true, true);
		}
	}
}
