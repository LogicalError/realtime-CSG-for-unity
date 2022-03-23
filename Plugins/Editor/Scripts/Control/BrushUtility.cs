using System;
using UnityEngine;
using RealtimeCSG;
using RealtimeCSG.Components;
using GeometryUtility = RealtimeCSG.GeometryUtility;

namespace InternalRealtimeCSG
{
	[Serializable]
	internal enum HandleConstraints
	{
		Straight,	// Tangents are ignored (used for straight lines)
		Broken,		// Both tangents are assumed to go in different directions
		Mirrored	// Both tangents are aligned and mirror each other
	}

	[Serializable]
	internal struct TangentCurve2D
	{
		public Vector3 Tangent;
		public HandleConstraints Constraint;
	}

	[Serializable]
	internal class Curve2D
	{
		public Vector3[] Points;
		public TangentCurve2D[] Tangents;
	}

	public static class BrushUtility
	{
		public static void SetPivotToLocalCenter(CSGBrush brush)
		{
			if (!brush)
				return;
			
			var localCenter = BoundsUtilities.GetLocalCenter(brush);
			var worldCenter	= brush.transform.localToWorldMatrix.MultiplyPoint(localCenter);

			SetPivot(brush, worldCenter);
		}

		public static void SetPivot(CSGBrush brush, Vector3 newCenter)
		{
			if (!brush)
				return;

			var transform = brush.transform;
			var realCenter = transform.position;
			var difference = newCenter - realCenter;

			if (difference.sqrMagnitude < MathConstants.ConsideredZero)
				return;

			transform.position += difference;

			GeometryUtility.MoveControlMeshVertices(brush, -difference);
			SurfaceUtility.TranslateSurfacesInWorldSpace(brush, -difference);
			ControlMeshUtility.RebuildShape(brush);
		}

		public static void TranslatePivot(CSGBrush[] brushes, Vector3 offset)
		{
			if (brushes == null ||
				brushes.Length == 0 ||
				offset.sqrMagnitude < MathConstants.ConsideredZero)
				return;

			for (int i = 0; i < brushes.Length; i++)
				brushes[i].transform.position += offset;

			GeometryUtility.MoveControlMeshVertices(brushes, -offset);
			SurfaceUtility.TranslateSurfacesInWorldSpace(brushes, -offset);
			ControlMeshUtility.RebuildShapes(brushes);
		}
	}
}