using System;
using System.Collections.Generic;
using UnityEngine;
using RealtimeCSG.Legacy;

namespace RealtimeCSG
{
	internal class ControlMeshSelection
	{
        [SerializeField] public SelectState[]   Points;
        [SerializeField] public SelectState[]   Edges;
        [SerializeField] public SelectState[]   Polygons;
		
        public void CopyTo(ControlMeshSelection other)
        {
			if (other == null)
				return;

            if (Points != null)
            {
				if (other.Points == null || Points.Length != other.Points.Length)
					other.Points = new SelectState[Points.Length];
                Array.Copy(Points, other.Points, Points.Length);
            }

            if (Edges != null)
            {
                if (other.Edges == null || Edges.Length != other.Edges.Length)
					other.Edges = new SelectState[Edges.Length];
                Array.Copy(Edges, other.Edges, Edges.Length);
            }

            if (Polygons != null)
            {
				if (other.Polygons == null || Polygons.Length != other.Polygons.Length)
					other.Polygons = new SelectState[Polygons.Length];
                Array.Copy(Polygons, other.Polygons, Polygons.Length);
            }
        }
		
		public ControlMeshSelection Clone()
		{
			var clone = new ControlMeshSelection();
			CopyTo(clone);
			return clone;
		}

		public void Clear()
		{
            Points = null;
            Edges = null;
            Polygons = null;
		}

		public void DeselectAll()
		{
			if (Points != null)
				Array.Clear(Points, 0, Points.Length);
			if (Edges != null)
				Array.Clear(Edges, 0, Edges.Length);
			if (Polygons != null)
				Array.Clear(Polygons, 0, Polygons.Length);

		}
		
        public bool Equals(ControlMeshSelection other)
        {
            if (Points != null)
            {
                for (var p = 0; p < Points.Length; p++)
                {
                    if (Points[p] != other.Points[p])
                        return false;
                }
            }

            if (other.Edges != null)
            {
                for (var e = 0; e < Edges.Length; e++)
                {
                    if (Edges[e] != other.Edges[e])
                        return false;
                }
            }

            if (other.Polygons != null)
            {
                for (var e = 0; e < Polygons.Length; e++)
                {
                    if (Polygons[e] != other.Polygons[e])
                        return false;
                }
            }
            return true;
        }
	}

    internal partial class ControlMeshState
    {
		// selection

		public readonly ControlMeshSelection Selection = new ControlMeshSelection();

        // backup stuff

		public readonly ControlMeshSelection SelectionBackup = new ControlMeshSelection();

        public void BackupSelection()
        {	
			Selection.CopyTo(SelectionBackup);
        }

        public void RevertSelection()
        {
			SelectionBackup.CopyTo(Selection);
		}

        public void DestroySelectionBackup()
        {
            SelectionBackup.Clear();
        }

        public bool HasSelectionChanged()
        {
			return !Selection.Equals(SelectionBackup);
        }

        public float GetClosestEdgeDistance(CSGPlane cameraPlane, int pointIndex0, int pointIndex1)
        {
            if (pointIndex0 < 0 || pointIndex0 >= WorldPoints.Length ||
                pointIndex1 < 0 || pointIndex1 >= WorldPoints.Length)
                return float.PositiveInfinity;

            var point0 = WorldPoints[pointIndex0];
            var point1 = WorldPoints[pointIndex1];
			
			var mousePoint = Event.current.mousePosition;
            var minDistance = CameraUtility.DistanceToLine(cameraPlane, mousePoint, point0, point1) * 3.0f;
            if (!(Mathf.Abs(minDistance) < 4.0f))
				return minDistance;

            var surfaceIndex1 = EdgeSurfaces[pointIndex0];
            var surfaceIndex2 = EdgeSurfaces[pointIndex1];
			 
            for (var p = 0; p < PolygonCenterPoints.Length; p++)
            {
                if (p != surfaceIndex1 &&
                    p != surfaceIndex2)
                    continue;

                var polygonCenterPoint          = PolygonCenterPoints[p];
                var polygonCenterPointOnLine    = GeometryUtility.ProjectPointOnInfiniteLine(PolygonCenterPoints[p], point0, (point1 - point0).normalized);
                var direction                   = (polygonCenterPointOnLine - polygonCenterPoint).normalized;

                var nudgedPoint0 = point0 - (direction * 0.05f);
                var nudgedPoint1 = point1 - (direction * 0.05f);

                var otherDistance = CameraUtility.DistanceToLine(cameraPlane, mousePoint, nudgedPoint0, nudgedPoint1);
                if (otherDistance < minDistance)
                {
                    minDistance = otherDistance;
                }
            }

            return minDistance;
        }


        public HashSet<short> GetSelectedPointIndices()
        {
            var indices = new HashSet<short>();

            indices.Clear();
            for (var p = 0; p < Selection.Points.Length; p++)
            {
                if ((Selection.Points[p] & SelectState.Selected) != SelectState.Selected)
                    continue;
                indices.Add((short)p);
            }
            for (var e = 0; e < Selection.Edges.Length; e++)
            {
                if ((Selection.Edges[e] & SelectState.Selected) != SelectState.Selected)
                    continue;
				
                indices.Add((short)Edges[(e * 2) + 0]);
                indices.Add((short)Edges[(e * 2) + 1]);
            }
            for (var p = 0; p < Selection.Polygons.Length; p++)
            {
                if ((Selection.Polygons[p] & SelectState.Selected) != SelectState.Selected)
                    continue;

                var pointIndices = PolygonPointIndices[p];
                for (var i = 0; i < pointIndices.Length; i++)
                {
                    indices.Add((short)pointIndices[i]);
                }
            }

            return indices;
        }
    }
}
