using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using InternalRealtimeCSG;
using UnityEngine.SceneManagement;

namespace RealtimeCSG
{
	internal static class CursorUtility
	{
	    private static readonly MouseCursor[] SegmentCursors = new MouseCursor[]
		{
			MouseCursor.ResizeVertical,
			MouseCursor.ResizeUpRight,
			MouseCursor.ResizeHorizontal,
			MouseCursor.ResizeUpLeft,
			MouseCursor.ResizeVertical,
			MouseCursor.ResizeUpRight,
			MouseCursor.ResizeHorizontal,
			MouseCursor.ResizeUpLeft
		};

		public static MouseCursor GetCursorForDirection(Matrix4x4 matrix, Vector3 center, Vector3 direction, float angleOffset = 0)
		{
			var worldCenterPoint1 = matrix.MultiplyPoint(center);
			var worldCenterPoint2 = worldCenterPoint1 +
									matrix.MultiplyVector(direction * 10.0f);
			var guiPoint1   = HandleUtility.WorldToGUIPoint(worldCenterPoint1);
			var guiPoint2   = HandleUtility.WorldToGUIPoint(worldCenterPoint2);
			var delta       = (guiPoint2 - guiPoint1).normalized;

			return GetCursorForDirection(delta, angleOffset);
		}

		public static MouseCursor GetCursorForDirection(Vector2 direction, float angleOffset = 0)
		{
			const float segmentAngle = 360 / 8.0f;
			var angle = (360 + (GeometryUtility.SignedAngle(MathConstants.upVector2, direction) + 180 + angleOffset)) % 360;// (Vector2.Angle(MathConstants.upVector2, direction) / 8) - (180 / 8);
			var segment = Mathf.FloorToInt(((angle / segmentAngle) + 0.5f) % 8.0f);

			return SegmentCursors[segment];
		}

		public static MouseCursor GetCursorForEdge(Vector2 direction)
		{
			const float segmentAngle = 360 / 8.0f;
			var angle = (360 + (GeometryUtility.SignedAngle(MathConstants.upVector2, direction) + 180)) % 360;// (Vector2.Angle(MathConstants.upVector2, direction) / 8) - (180 / 8);
			var segment = Mathf.FloorToInt(((angle / segmentAngle) + 2.5f) % 8.0f);

			return SegmentCursors[segment];
		}
		

		public static MouseCursor GetToolCursor()
		{
			switch (Tools.current)
			{
				case Tool.Move:		return MouseCursor.MoveArrow;
				case Tool.Rotate:	return MouseCursor.RotateArrow;
				case Tool.Scale:	return MouseCursor.ScaleArrow;
				case Tool.Rect:		return MouseCursor.SlideArrow;
				case Tool.View:		return MouseCursor.Orbit;
				default:			return MouseCursor.Arrow;
			}
		}

		public static MouseCursor GetSelectionCursor(SelectionType selectionType)
		{
			switch (selectionType)
			{
				case SelectionType.Additive:	return MouseCursor.ArrowPlus; 
				case SelectionType.Subtractive: return MouseCursor.ArrowMinus; 
				case SelectionType.Toggle:		return MouseCursor.Arrow;

				default:						return MouseCursor.Arrow;
			}
		}
	}
}
