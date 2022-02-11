using System;
using UnityEditor;
using UnityEngine;
using InternalRealtimeCSG;
using RealtimeCSG.Legacy;

namespace RealtimeCSG
{
	[Serializable]
    internal sealed class GeneratorSphereSettings : IShapeSettings
	{
		public Vector3[]	vertices				= new Vector3[0];
		public int[]		vertexIDs				= new int[0];
        public bool[]		onGeometryVertices		= new bool[0];	
		
		public int			sphereSplits
		{
			get { return Mathf.Max(1, RealtimeCSG.CSGSettings.SphereSplits); }
			set { if (RealtimeCSG.CSGSettings.SphereSplits == value) return; RealtimeCSG.CSGSettings.SphereSplits = value; RealtimeCSG.CSGSettings.Save(); }
		}
		public float		sphereOffset
		{
			get { return RealtimeCSG.CSGSettings.SphereOffset; }
			set { if (RealtimeCSG.CSGSettings.SphereOffset == value) return; RealtimeCSG.CSGSettings.SphereOffset = value; RealtimeCSG.CSGSettings.Save(); }
		}
		public bool sphereSmoothShading
		{
			get { return RealtimeCSG.CSGSettings.SphereSmoothShading; }
			set { if (RealtimeCSG.CSGSettings.SphereSmoothShading == value) return; RealtimeCSG.CSGSettings.SphereSmoothShading = value; RealtimeCSG.CSGSettings.Save(); }
		}
		public bool isHemiSphere
		{
			get { return RealtimeCSG.CSGSettings.HemiSphereMode; }
			set { if (RealtimeCSG.CSGSettings.HemiSphereMode == value) return; RealtimeCSG.CSGSettings.HemiSphereMode = value; RealtimeCSG.CSGSettings.Save(); }
		}

		public float		sphereRadius		= 0.0f;
		
		public void AddPoint(Vector3 position)
        {
            ArrayUtility.Add(ref vertices, position);
		}

		public void CalculatePlane(ref CSGPlane plane)
		{
		}

		public float	SphereRadius
		{
			get
			{
				if (vertices.Length <= 1)
					return 0;
				
				return (vertices[1] - vertices[0]).magnitude;
			}
			set
			{
				if (vertices.Length <= 1)
					return;
				
				var delta  = (vertices[1] - vertices[0]);
				var radius = delta.magnitude;
				if (radius == value)
					return;
				
				delta /= radius;
				radius = value;
				if (radius < MathConstants.MinimumScale)
					radius = MathConstants.MinimumScale;
				delta *= radius;

				vertices[1] = vertices[0] + delta;
			}
		}

		public void Reset()
		{
			vertices				= new Vector3[0];
		}

		public Vector3[] GetVertices(CSGPlane buildPlane, Vector3 worldPosition, Vector3 gridTangent, Vector3 gridBinormal, out bool isValid)
		{
			if (vertices.Length < 1)
			{
				isValid = false;
				return vertices;
			}
			if (sphereSplits < 1)
				sphereSplits = 1;

			var realSplits = (sphereSplits + 1) * 4;


			//var tangent = GeometryUtility.CalculateTangent(buildPlane.normal);

			var vertex1 = (vertices.Length > 1) ? vertices[1] : worldPosition;
				
			var delta  = (vertex1 - vertices[0]);
			sphereRadius = delta.magnitude;

			if (sphereRadius <= MathConstants.DistanceEpsilon)
			{
				isValid = false;
				return new Vector3[0];
			}


			var matrix			= GeometryUtility.Rotate2DToPlaneMatrix(buildPlane);
			var realVertices	= new Vector3[realSplits];

			float angle_offset = GeometryUtility.SignedAngle(gridTangent, delta / sphereRadius, buildPlane.normal);
			angle_offset -= 90;

			angle_offset += sphereOffset;
			angle_offset *= Mathf.Deg2Rad;

			Vector3 p1 = MathConstants.zeroVector3;
			for (int i = 0; i < realSplits; i++)
			{
				var angle = ((i * Mathf.PI * 2.0f) / (float)realSplits) + angle_offset;

				p1.x = (Mathf.Sin(angle) * sphereRadius);
				p1.z = (Mathf.Cos(angle) * sphereRadius);

				realVertices[i] = p1;
			}

			for (int i = 0; i < realSplits; i++)
			{
				var point = realVertices[i];
				realVertices[i] = GeometryUtility.ProjectPointOnPlane(buildPlane, vertices[0] + matrix.MultiplyPoint(point));
			}

			isValid = true;
			return realVertices;
		}
		
		public void ProjectShapeOnBuildPlane(CSGPlane plane)
		{
			for (int i = 0; i < vertices.Length; i++)
				vertices[i] = plane.Project(vertices[i]);
        }

        public void MoveShape(Vector3 offset)
        {
            for (int i = 0; i < vertices.Length; i++)
                vertices[i] = vertices[i] + offset;
        }

        public Vector3 GetCenter(CSGPlane plane)
		{
			return vertices[0];
		}

		public RealtimeCSG.AABB CalculateBounds(Quaternion rotation, Vector3 gridTangent, Vector3 gridBinormal)
		{
			var bounds = new RealtimeCSG.AABB();
			bounds.Reset();
			var radius = SphereRadius;
			var point1 = rotation * (vertices[0] + (gridTangent * radius) + (gridBinormal * radius));
			var point2 = rotation * (vertices[0] - (gridTangent * radius) - (gridBinormal * radius));
			var min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
			var max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

			min.x = Mathf.Min(min.x, point1.x);
			min.y = Mathf.Min(min.y, point1.y);
			min.z = Mathf.Min(min.z, point1.z);

			max.x = Mathf.Max(max.x, point1.x);
			max.y = Mathf.Max(max.y, point1.y);
			max.z = Mathf.Max(max.z, point1.z);

			min.x = Mathf.Min(min.x, point2.x);
			min.y = Mathf.Min(min.y, point2.y);
			min.z = Mathf.Min(min.z, point2.z);

			max.x = Mathf.Max(max.x, point2.x);
			max.y = Mathf.Max(max.y, point2.y);
			max.z = Mathf.Max(max.z, point2.z);
			
			bounds.Min = min;
			bounds.Max = max;
			return bounds;
		}
	}
}
