using System;
using UnityEditor;
using UnityEngine;
using InternalRealtimeCSG;
using RealtimeCSG.Legacy;

namespace RealtimeCSG
{
	[Serializable]
    internal sealed class GeneratorCylinderSettings : IShapeSettings
	{
		public Vector3[]	backupVertices			= new Vector3[0];
		public Vector3[]	vertices				= new Vector3[0];
		public int[]		vertexIDs				= new int[0];
        public bool[]		onGeometryVertices		= new bool[0];	
		
		public int			circleSides
		{
			get { return Mathf.Max(3, RealtimeCSG.CSGSettings.CircleSides); }
			set { if (RealtimeCSG.CSGSettings.CircleSides == value) return; RealtimeCSG.CSGSettings.CircleSides = value; RealtimeCSG.CSGSettings.Save(); }
		}
		public float		circleOffset
		{
			get { return RealtimeCSG.CSGSettings.CircleOffset; }
			set { if (RealtimeCSG.CSGSettings.CircleOffset == value) return; RealtimeCSG.CSGSettings.CircleOffset = value; RealtimeCSG.CSGSettings.Save(); }
		}
		public bool			circleSmoothShading
		{
			get { return RealtimeCSG.CSGSettings.CircleSmoothShading; }
			set { if (RealtimeCSG.CSGSettings.CircleSmoothShading == value) return; RealtimeCSG.CSGSettings.CircleSmoothShading = value; RealtimeCSG.CSGSettings.Save(); }
		}
		public bool			circleSingleSurfaceEnds
		{
			get { return RealtimeCSG.CSGSettings.CircleSingleSurfaceEnds; }
			set { if (RealtimeCSG.CSGSettings.CircleSingleSurfaceEnds == value) return; RealtimeCSG.CSGSettings.CircleSingleSurfaceEnds = value; RealtimeCSG.CSGSettings.Save(); }
		}
		public bool			circleDistanceToSide 
		{
			get { return RealtimeCSG.CSGSettings.CircleDistanceToSide; }
			set { if (RealtimeCSG.CSGSettings.CircleDistanceToSide == value) return; RealtimeCSG.CSGSettings.CircleDistanceToSide = value; RealtimeCSG.CSGSettings.Save(); }
		}
		public bool			circleRecenter
		{
			get { return RealtimeCSG.CSGSettings.CircleRecenter; }
			set { if (RealtimeCSG.CSGSettings.CircleRecenter == value) return; RealtimeCSG.CSGSettings.CircleRecenter = value; RealtimeCSG.CSGSettings.Save(); }
		}
		public float		radiusA				= 0.0f;
		public float		radiusB				= 0.0f;
		
		public void AddPoint(Vector3 position)
        {
            ArrayUtility.Add(ref vertices, position);
		}

		public void CalculatePlane(ref CSGPlane plane)
		{
		}

		public float	RadiusA
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

		public float RadiusB
		{
			get;
			set;
		}

		public void Reset()
		{
			backupVertices			= new Vector3[0];
			vertices				= new Vector3[0];

			circleSides				= Mathf.Max(3, RealtimeCSG.CSGSettings.CircleSides);
			circleOffset			= RealtimeCSG.CSGSettings.CircleOffset;
			circleSmoothShading		= RealtimeCSG.CSGSettings.CircleSmoothShading;
			circleSingleSurfaceEnds = RealtimeCSG.CSGSettings.CircleSingleSurfaceEnds;
			circleDistanceToSide	= RealtimeCSG.CSGSettings.CircleDistanceToSide;
			circleRecenter			= RealtimeCSG.CSGSettings.CircleRecenter;
		}

		public void CopyBackupVertices()
		{
			backupVertices = new Vector3[vertices.Length];
			Array.Copy(vertices, backupVertices, vertices.Length);
		}

		public Vector3[] GetVertices(CSGPlane buildPlane, Vector3 worldPosition, Vector3 gridTangent, Vector3 gridBinormal, out bool isValid)
		{
			if (vertices.Length < 1)
			{
				isValid = false;
				return vertices;
			}

			if (circleSides < 3)
				circleSides = 3;

			//var tangent = GeometryUtility.CalculateTangent(buildPlane.normal);

			var vertex1 = (vertices.Length > 1) ? vertices[1] : worldPosition;
				
			var delta  = (vertex1 - vertices[0]);
			radiusA = delta.magnitude;

			if (radiusA <= MathConstants.DistanceEpsilon)
			{
				isValid = false;
				return new Vector3[0];
			}


			var matrix			= GeometryUtility.Rotate2DToPlaneMatrix(buildPlane);
			var realVertices	= new Vector3[circleSides];

			float angle_offset = GeometryUtility.SignedAngle(gridTangent, delta / radiusA, buildPlane.normal);
			if (circleDistanceToSide)
			{
				angle_offset += 90;
				if ((circleSides & 1) != 1)
					angle_offset += (180.0f / circleSides);
			} else
				angle_offset -= 90;

			angle_offset += circleOffset;
			angle_offset *= Mathf.Deg2Rad;

			Vector3 p1 = MathConstants.zeroVector3;
			for (int i = 0; i < circleSides; i++)
			{
				var angle = ((i * Mathf.PI * 2.0f) / (float)circleSides) + angle_offset;

				p1.x = (Mathf.Sin(angle) * radiusA);
				p1.z = (Mathf.Cos(angle) * radiusA);

				realVertices[i] = p1;
			}

			if (circleRecenter)
			{
				var dirx	= matrix.MultiplyVector(delta.normalized);
				var dirz	= Vector3.Cross(MathConstants.upVector3, dirx);

				float minx = float.PositiveInfinity;
				float minz = float.PositiveInfinity;
				float maxx = float.NegativeInfinity;
				float maxz = float.NegativeInfinity;
				for (int i = 0; i < circleSides; i++)
				{
					var point = realVertices[i];
					var x = Vector3.Dot(point, dirx);
					var z = Vector3.Dot(point, dirz);
					
					minx = Mathf.Min(x, minx);
					minz = Mathf.Min(z, minz);

					maxx = Mathf.Max(x, maxx);
					maxz = Mathf.Max(z, maxz);
				}
				
				var scalex	= (radiusA * 2) / (maxx - minx);
				var scalez	= (radiusA * 2) / (maxz - minz);
				var centerx	= ((maxx + minx) * -0.5f) * dirx;
				var centerz	= ((maxz + minz) * -0.5f) * dirz;
			
				for (int i = 0; i < circleSides; i++)
				{
					var point = realVertices[i];
					var x = Vector3.Dot(point, dirx);
					var z = Vector3.Dot(point, dirz);


					var ptx = x * dirx;
					var ptz = z * dirz;

					ptx += centerx;
					ptz += centerz;
					ptx *= scalex;
					ptz *= scalez;
					realVertices[i] = ptx + ptz;
				}
			}

			for (int i = 0; i < circleSides; i++)
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
                vertices[i] = backupVertices[i] + offset;
        }

        public Vector3 GetCenter(CSGPlane plane)
		{
			return vertices[0];
		}

		public RealtimeCSG.AABB CalculateBounds(Quaternion rotation, Vector3 gridTangent, Vector3 gridBinormal)
		{
			var bounds = new RealtimeCSG.AABB();
			bounds.Reset();
			var radius = Mathf.Max(RadiusA, RadiusB);
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
