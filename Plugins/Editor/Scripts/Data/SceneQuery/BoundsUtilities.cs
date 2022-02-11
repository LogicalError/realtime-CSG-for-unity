using UnityEngine;
using InternalRealtimeCSG;
using RealtimeCSG.Components;

namespace RealtimeCSG
{
	internal enum CubeSide
	{
		NegativeX,
		NegativeY,
		NegativeZ,
		PositiveX,
		PositiveY,
		PositiveZ
	}

	internal sealed class BoundsUtilities
	{
		public static bool HasFrameBounds(FilteredSelection filteredSelection)
		{
			return (filteredSelection.NodeTargets.Length > 0);
		}

		public static Bounds OnGetFrameBounds(FilteredSelection filteredSelection)
		{
			var brushes		= filteredSelection.GetAllContainedBrushes();
			var bounds		= new Bounds();
			if (brushes.Count == 0)
			{
				foreach (var node in filteredSelection.NodeTargets)
				{
					bounds.Encapsulate(node.transform.position);
				}
			} else
			{
				foreach (var brush in brushes)
				{
					var transform	= brush.GetComponent<Transform>();
					var controlMesh = brush.ControlMesh;
					bounds.center = transform.TransformPoint(controlMesh.Vertices[0]);
					bounds.size = MathConstants.zeroVector3;
					for (int i = 1; i < controlMesh.Vertices.Length; i++)
						bounds.Encapsulate(transform.TransformPoint(controlMesh.Vertices[i]));
				}
			}
			// TODO: how to get bounds of random selected objects??
			//foreach (var other in filteredSelection.targetOthers)
			//{
			//	bounds.Encapsulate(...);
			//}
			return bounds;
		}

		static public AABB GetBounds(CSGBrush brush)
		{
			var min = MathConstants.PositiveInfinityVector3;
			var max = MathConstants.NegativeInfinityVector3;
			
			var controlMesh = brush.ControlMesh;
			if (controlMesh == null)
				return new AABB();

			var matrix = brush.transform.localToWorldMatrix;
			for (int p = 0; p < controlMesh.Vertices.Length; p++)
			{
				var point = matrix.MultiplyPoint(controlMesh.Vertices[p]);
				min.x = Mathf.Min(min.x, point.x);
				min.y = Mathf.Min(min.y, point.y);
				min.z = Mathf.Min(min.z, point.z);

				max.x = Mathf.Max(max.x, point.x);
				max.y = Mathf.Max(max.y, point.y);
				max.z = Mathf.Max(max.z, point.z);
			}
			var bounds = new AABB();
			bounds.MinX = min.x;
			bounds.MinY = min.y;
			bounds.MinZ = min.z;
			bounds.MaxX = max.x;
			bounds.MaxY = max.y;
			bounds.MaxZ = max.z;
			return bounds;
		}

		static public AABB GetBounds(CSGBrush[] brushes)
		{
			var min = MathConstants.PositiveInfinityVector3;
			var max = MathConstants.NegativeInfinityVector3;
			for (int b = 0; b < brushes.Length; b++)
			{
				if (!brushes[b])
					continue;

				var controlMesh = brushes[b].ControlMesh;
				if (controlMesh == null ||
					controlMesh.Vertices == null)
					continue;

				var matrix = brushes[b].transform.localToWorldMatrix;
				for (int p = 0; p < controlMesh.Vertices.Length; p++)
				{
					var point = matrix.MultiplyPoint(controlMesh.Vertices[p]);
					min.x = Mathf.Min(min.x, point.x);
					min.y = Mathf.Min(min.y, point.y);
					min.z = Mathf.Min(min.z, point.z);

					max.x = Mathf.Max(max.x, point.x);
					max.y = Mathf.Max(max.y, point.y);
					max.z = Mathf.Max(max.z, point.z);
				}
			}
			var bounds = new AABB();
			bounds.MinX = min.x;
			bounds.MinY = min.y;
			bounds.MinZ = min.z;
			bounds.MaxX = max.x;
			bounds.MaxY = max.y;
			bounds.MaxZ = max.z;
			return bounds;
		}

		static public AABB GetBounds(Vector3[] points)
		{
			var min = MathConstants.PositiveInfinityVector3;
			var max = MathConstants.NegativeInfinityVector3;
			
			for (int p = 0; p < points.Length; p++)
			{
				var point = points[p];
				min.x = Mathf.Min(min.x, point.x);
				min.y = Mathf.Min(min.y, point.y);
				min.z = Mathf.Min(min.z, point.z);

				max.x = Mathf.Max(max.x, point.x);
				max.y = Mathf.Max(max.y, point.y);
				max.z = Mathf.Max(max.z, point.z);
			}
			var bounds = new AABB();
			bounds.MinX = min.x;
			bounds.MinY = min.y;
			bounds.MinZ = min.z;
			bounds.MaxX = max.x;
			bounds.MaxY = max.y;
			bounds.MaxZ = max.z;
			return bounds;
		}

		static public AABB GetBounds(Vector3[] worldPoints, Quaternion rotation, Vector3 scale, Vector3 center)
		{
			if (worldPoints == null || worldPoints.Length == 0)
				return new AABB();

			var min = MathConstants.PositiveInfinityVector3;
			var max = MathConstants.NegativeInfinityVector3;			
			
			var inverseRotation			= Quaternion.Inverse(rotation);

			var rotationMatrix			= Matrix4x4.TRS(Vector3.zero, rotation, Vector3.one);
			var inverseRotationMatrix	= Matrix4x4.TRS(Vector3.zero, inverseRotation, Vector3.one);
			var scaleMatrix				= Matrix4x4.TRS(Vector3.zero, Quaternion.identity, scale);
			var moveMatrix				= Matrix4x4.TRS( center, Quaternion.identity, Vector3.one);
			var inverseMoveMatrix		= Matrix4x4.TRS(-center, Quaternion.identity, Vector3.one);
			
			var combinedMatrix			= 
											moveMatrix *
											rotationMatrix *
											scaleMatrix *
											inverseRotationMatrix *
											inverseMoveMatrix
											;
			
			for (int p = 0; p < worldPoints.Length; p++)
			{
				var point = inverseRotation * combinedMatrix.MultiplyPoint(worldPoints[p]);

				min.x = Mathf.Min(min.x, point.x);
				min.y = Mathf.Min(min.y, point.y);
				min.z = Mathf.Min(min.z, point.z);

				max.x = Mathf.Max(max.x, point.x);
				max.y = Mathf.Max(max.y, point.y);
				max.z = Mathf.Max(max.z, point.z);
			}
			var bounds = new AABB();
			bounds.MinX = min.x;
			bounds.MinY = min.y;
			bounds.MinZ = min.z;
			bounds.MaxX = max.x;
			bounds.MaxY = max.y;
			bounds.MaxZ = max.z;
			return bounds;
		}
		
		static public Vector3 GetCenter(CSGBrush brush)
		{
			var min = MathConstants.PositiveInfinityVector3;
			var max = MathConstants.NegativeInfinityVector3;

			if (!brush)
				return min;
			
			var controlMesh = brush.ControlMesh;
			if (controlMesh == null)
				return min;

			var matrix = brush.transform.localToWorldMatrix;
			for (int p = 0; p < controlMesh.Vertices.Length; p++)
			{
				var point = matrix.MultiplyPoint(controlMesh.Vertices[p]);
				min.x = Mathf.Min(min.x, point.x);
				min.y = Mathf.Min(min.y, point.y);
				min.z = Mathf.Min(min.z, point.z);

				max.x = Mathf.Max(max.x, point.x);
				max.y = Mathf.Max(max.y, point.y);
				max.z = Mathf.Max(max.z, point.z);
			}
			return new Vector3((min.x * 0.5f) + (max.x * 0.5f), (min.y * 0.5f) + (max.y * 0.5f), (min.z * 0.5f) + (max.z * 0.5f));
		}
		
		static public Vector3 GetLocalCenter(CSGBrush brush)
		{
			var min = MathConstants.PositiveInfinityVector3;
			var max = MathConstants.NegativeInfinityVector3;

			if (!brush)
				return min;

			var controlMesh = brush.ControlMesh;
			if (controlMesh == null ||
				controlMesh.Vertices == null)
				return min;
			
			for (int p = 0; p < controlMesh.Vertices.Length; p++)
			{
				var point = controlMesh.Vertices[p];
				min.x = Mathf.Min(min.x, point.x);
				min.y = Mathf.Min(min.y, point.y);
				min.z = Mathf.Min(min.z, point.z);

				max.x = Mathf.Max(max.x, point.x);
				max.y = Mathf.Max(max.y, point.y);
				max.z = Mathf.Max(max.z, point.z);
			}

			return new Vector3((min.x * 0.5f) + (max.x * 0.5f), (min.y * 0.5f) + (max.y * 0.5f), (min.z * 0.5f) + (max.z * 0.5f));
		}

		static public Vector3 GetCenter(CSGBrush[] brushes)
		{
			var min = MathConstants.PositiveInfinityVector3;
			var max = MathConstants.NegativeInfinityVector3;
			for (int b = 0; b < brushes.Length; b++)
			{
				if (!brushes[b])
					continue;

				var controlMesh = brushes[b].ControlMesh;
				if (controlMesh == null)
					continue;

				var matrix = brushes[b].transform.localToWorldMatrix;
				for (int p = 0; p < controlMesh.Vertices.Length; p++)
				{
					var point = matrix.MultiplyPoint(controlMesh.Vertices[p]);
					min.x = Mathf.Min(min.x, point.x);
					min.y = Mathf.Min(min.y, point.y);
					min.z = Mathf.Min(min.z, point.z);

					max.x = Mathf.Max(max.x, point.x);
					max.y = Mathf.Max(max.y, point.y);
					max.z = Mathf.Max(max.z, point.z);
				}
			}
			if (float.IsInfinity(min.x))
				return min;
			return new Vector3((min.x * 0.5f) + (max.x * 0.5f), (min.y * 0.5f) + (max.y * 0.5f), (min.z * 0.5f) + (max.z * 0.5f));
		}
		
		static public AABB GetLocalBounds(CSGBrush[] brushes, Transform transform)
		{
			var min = MathConstants.PositiveInfinityVector3;
			var max = MathConstants.NegativeInfinityVector3;

			var matrix2 = transform.worldToLocalMatrix;
			for (int b = 0; b < brushes.Length; b++)
			{
				var controlMesh = brushes[b].ControlMesh;
				if (controlMesh == null ||
					controlMesh.Vertices == null)
					continue;

				var matrix1 = matrix2 * brushes[b].transform.localToWorldMatrix;
				for (int p = 0; p < controlMesh.Vertices.Length; p++)
				{
					var point = matrix1.MultiplyPoint(controlMesh.Vertices[p]);
					min.x = Mathf.Min(min.x, point.x);
					min.y = Mathf.Min(min.y, point.y);
					min.z = Mathf.Min(min.z, point.z);

					max.x = Mathf.Max(max.x, point.x);
					max.y = Mathf.Max(max.y, point.y);
					max.z = Mathf.Max(max.z, point.z);
				}
			}
			var bounds = new AABB();
			bounds.MinX = min.x;
			bounds.MinY = min.y;
			bounds.MinZ = min.z;
			bounds.MaxX = max.x;
			bounds.MaxY = max.y;
			bounds.MaxZ = max.z;
			return bounds;
		}
		
		static public AABB GetLocalBounds(CSGBrush[] brushes, Matrix4x4 worldToLocal)
		{
			var min = MathConstants.PositiveInfinityVector3;
			var max = MathConstants.NegativeInfinityVector3;
			
			for (int b = 0; b < brushes.Length; b++)
			{
				var controlMesh = brushes[b].ControlMesh;
				if (controlMesh == null ||
					controlMesh.Vertices == null)
					continue;

				var brushToGridspace = worldToLocal * brushes[b].transform.localToWorldMatrix;
				for (int p = 0; p < controlMesh.Vertices.Length; p++)
				{
					var point = brushToGridspace.MultiplyPoint(controlMesh.Vertices[p]);
					min.x = Mathf.Min(min.x, point.x);
					min.y = Mathf.Min(min.y, point.y);
					min.z = Mathf.Min(min.z, point.z);

					max.x = Mathf.Max(max.x, point.x);
					max.y = Mathf.Max(max.y, point.y);
					max.z = Mathf.Max(max.z, point.z);
				}
			}
			var bounds = new AABB();
			bounds.MinX = min.x;
			bounds.MinY = min.y;
			bounds.MinZ = min.z;
			bounds.MaxX = max.x;
			bounds.MaxY = max.y;
			bounds.MaxZ = max.z;
			return bounds;
		}
		
		static public void GetBoundsCornerPoints(AABB inputBounds, Vector3[] outputPoints)
		{
			if (outputPoints.Length != 8)
				return;

			outputPoints[0] = new Vector3(inputBounds.MinX, inputBounds.MinY, inputBounds.MinZ);
			outputPoints[1] = new Vector3(inputBounds.MaxX, inputBounds.MinY, inputBounds.MinZ);
			outputPoints[2] = new Vector3(inputBounds.MaxX, inputBounds.MinY, inputBounds.MaxZ);
			outputPoints[3] = new Vector3(inputBounds.MinX, inputBounds.MinY, inputBounds.MaxZ);

			outputPoints[4] = new Vector3(inputBounds.MinX, inputBounds.MaxY, inputBounds.MinZ);
			outputPoints[5] = new Vector3(inputBounds.MaxX, inputBounds.MaxY, inputBounds.MinZ);
			outputPoints[6] = new Vector3(inputBounds.MaxX, inputBounds.MaxY, inputBounds.MaxZ);
			outputPoints[7] = new Vector3(inputBounds.MinX, inputBounds.MaxY, inputBounds.MaxZ);
		}

		static public void GetBoundsSidePoints(AABB inputBounds, Vector3[] outputPoints)
		{
			if (outputPoints.Length != 6)
				return;

			var center = inputBounds.Center;

			outputPoints[0] = new Vector3(inputBounds.MinX, center.y, center.z);
			outputPoints[1] = new Vector3(inputBounds.MaxX, center.y, center.z);
			outputPoints[2] = new Vector3(center.x, inputBounds.MinY, center.z);
			outputPoints[3] = new Vector3(center.x, inputBounds.MaxY, center.z);
			outputPoints[4] = new Vector3(center.x, center.y, inputBounds.MinZ);
			outputPoints[5] = new Vector3(center.x, center.y, inputBounds.MaxZ);
		}

		static public Vector3 GetBoundsSidePoint(AABB inputBounds, int index)
		{
			var center = inputBounds.Center;
			switch (index)
			{
				default: return Vector3.zero;
				case 0: return new Vector3(inputBounds.MinX, center.y, center.z);
				case 1: return new Vector3(inputBounds.MaxX, center.y, center.z);
				case 2: return new Vector3(center.x, inputBounds.MinY, center.z);
				case 3: return new Vector3(center.x, inputBounds.MaxY, center.z);
				case 4: return new Vector3(center.x, center.y, inputBounds.MinZ);
				case 5: return new Vector3(center.x, center.y, inputBounds.MaxZ);
			}
		}

		static public void GetBoundsEdgePoints(AABB inputBounds, Vector3[] outputPoints)
		{
			if (outputPoints.Length != 12)
				return;

			var center = inputBounds.Center;

			outputPoints[ 0] = new Vector3(center.x,         inputBounds.MinY, inputBounds.MinZ);
			outputPoints[ 1] = new Vector3(inputBounds.MaxX, inputBounds.MinY, center.z        );
			outputPoints[ 2] = new Vector3(center.x,         inputBounds.MinY, inputBounds.MaxZ);
			outputPoints[ 3] = new Vector3(inputBounds.MinX, inputBounds.MinY, center.z        );

			outputPoints[ 4] = new Vector3(center.x,         inputBounds.MaxY, inputBounds.MinZ);
			outputPoints[ 5] = new Vector3(inputBounds.MaxX, inputBounds.MaxY, center.z        );
			outputPoints[ 6] = new Vector3(center.x,         inputBounds.MaxY, inputBounds.MaxZ);
			outputPoints[ 7] = new Vector3(inputBounds.MinX, inputBounds.MaxY, center.z        );

			outputPoints[ 8] = new Vector3(inputBounds.MinX, center.y,         inputBounds.MinZ);
			outputPoints[ 9] = new Vector3(inputBounds.MaxX, center.y,         inputBounds.MinZ);
			outputPoints[10] = new Vector3(inputBounds.MaxX, center.y,         inputBounds.MaxZ);
			outputPoints[11] = new Vector3(inputBounds.MinX, center.y,         inputBounds.MaxZ);
		}

		static public Vector3 GetBoundsEdgePoint(AABB inputBounds, int index)
		{
			var center = inputBounds.Center;
			switch (index)
			{
				default: return Vector3.zero;
				case  0: return new Vector3(center.x, inputBounds.MinY, inputBounds.MinZ);
				case  1: return new Vector3(inputBounds.MaxX, inputBounds.MinY, center.z);
				case  2: return new Vector3(center.x, inputBounds.MinY, inputBounds.MaxZ);
				case  3: return new Vector3(inputBounds.MinX, inputBounds.MinY, center.z);

				case  4: return new Vector3(center.x, inputBounds.MaxY, inputBounds.MinZ);
				case  5: return new Vector3(inputBounds.MaxX, inputBounds.MaxY, center.z);
				case  6: return new Vector3(center.x, inputBounds.MaxY, inputBounds.MaxZ);
				case  7: return new Vector3(inputBounds.MinX, inputBounds.MaxY, center.z);

				case  8: return new Vector3(inputBounds.MinX, center.y, inputBounds.MinZ);
				case  9: return new Vector3(inputBounds.MaxX, center.y, inputBounds.MinZ);
				case 10: return new Vector3(inputBounds.MaxX, center.y, inputBounds.MaxZ);
				case 11: return new Vector3(inputBounds.MinX, center.y, inputBounds.MaxZ);
			}
		}


		public static readonly Vector3[] UnitCube = new Vector3[]
		{
			new Vector3(-1, -1, -1),	// -X -Y -Z    0
			new Vector3( 1, -1, -1),	// +X -Y -Z    1
			new Vector3( 1, -1,  1),	// +X -Y +Z    2
			new Vector3(-1, -1,  1),	// -X -Y +Z    3

			new Vector3(-1,  1, -1),	// -X +Y -Z    4
			new Vector3( 1,  1, -1),	// +X +Y -Z    5
			new Vector3( 1,  1,  1),	// +X +Y +Z    6
			new Vector3(-1,  1,  1)		// -X +Y +Z    7
		};

		// useful for drawing lines
		public static readonly int[] AABBLineIndices = new int[]
		{
			0, 1, 1, 2, 2, 3, 3, 0,
			4, 5, 5, 6, 6, 7, 7, 4,
			0, 4, 1, 5, 2, 6, 3, 7
		};

		public static readonly int[][] AABBEdgeIndices = new int[12][]
		{
			new int[2] {0, 1}, //  0  {-X -Y -Z} {+X -Y -Z}
			new int[2] {1, 2}, //  1  {+X -Y -Z} {+X -Y +Z}
			new int[2] {3, 2}, //  2  {-X -Y +Z} {+X -Y +Z}
			new int[2] {0, 3}, //  3  {-X -Y -Z} {-X -Y +Z}

			new int[2] {4, 5}, //  4  {-X +Y -Z} {+X +Y -Z}
			new int[2] {5, 6}, //  5  {+X +Y -Z} {+X +Y +Z}
			new int[2] {7, 6}, //  6  {-X +Y +Z} {+X +Y +Z}
			new int[2] {4, 7}, //  7  {-X +Y -Z} {-X +Y +Z}

			new int[2] {0, 4}, //  8  {-X -Y -Z} {-X +Y -Z}
			new int[2] {1, 5}, //  9  {+X -Y -Z} {+X +Y -Z}
			new int[2] {2, 6}, // 10  {+X -Y +Z} {+X +Y +Z}
			new int[2] {3, 7}  // 11  {-X -Y +Z} {-X +Y +Z}
		};

		public static readonly int[] AABBTopIndices		= new int[] {0, 1, 2, 3};
		public static readonly int[] AABBBottomIndices	= new int[] {4, 5, 6, 7};
		
		// index to AABBEdgeIndices
		public static readonly int[][] AABBSideEdgeIndices = new int[][]
		{
			new int[] {  3, 11,  7,  8 }, // 0
			new int[] {  5, 10,  1,  9 }, // 1

			new int[] {  0,  1,  2,  3 }, // 2
			new int[] {  6,  5,  4,  7 }, // 3

			new int[] {  4,  9,  0,  8 }, // 4
			new int[] {  2, 10,  6, 11 }, // 5
		};
		
		// index to AABBEdgeIndices
		public static readonly int[][] AABBSideEdgeIndicesDirection1 = new int[][]
		{
			new int[] {  3,  7 }, // 0  -Y +Y
			new int[] {  1,  5 }, // 1  -Y +Y

			new int[] {  0,  2 }, // 2  -Z +Z
			new int[] {  4,  6 }, // 3  -Z +Z

			new int[] {  0,  4 }, // 4  -Y +Y
			new int[] {  2,  6 }, // 5  -Y +Y
		};
		
		// index to AABBEdgeIndices
		public static readonly int[][] AABBSideEdgeIndicesDirection2 = new int[][]
		{
			new int[] {  8, 11 }, // 0  -Z +Z
			new int[] {  9, 10 }, // 1  -Z +Z

			new int[] {  3,  1 }, // 2  -X +X
			new int[] {  7,  5 }, // 3  -X +X

			new int[] {  8,  9 }, // 4  -X +X
			new int[] { 11, 10 }, // 5  -X +X
		};

		public static readonly int[][] AABBSidePointIndices = new int[][]
		{
			new int[] { 0, 3, 7, 4 }, // 0
			new int[] { 5, 6, 2, 1 }, // 1
			
			new int[] { 0, 1, 2, 3 }, // 2
			new int[] { 7, 6, 5, 4 }, // 3

			new int[] { 4, 5, 1, 0 }, // 4
			new int[] { 3, 2, 6, 7 }  // 5
		};

		public static readonly int[] AABBYAxi = new int[] {  8,  9, 10, 11 };
		public static readonly int[] AABBXAxi = new int[] {  0,  2,  4,  6 };
		public static readonly int[] AABBZAxi = new int[] {  1,  3,  5,  7 };

		public static readonly PrincipleAxis[][] AABBEdgeAxis = new PrincipleAxis[12][]
		{
			new PrincipleAxis[] { PrincipleAxis.Y, PrincipleAxis.Z }, // 0,1 
			new PrincipleAxis[] { PrincipleAxis.X, PrincipleAxis.Y }, // 1,2
			new PrincipleAxis[] { PrincipleAxis.Y, PrincipleAxis.Z }, // 2,3
			new PrincipleAxis[] { PrincipleAxis.X, PrincipleAxis.Y }, // 3,0

			new PrincipleAxis[] { PrincipleAxis.Y, PrincipleAxis.Z }, // 4,5
			new PrincipleAxis[] { PrincipleAxis.X, PrincipleAxis.Y }, // 5,6
			new PrincipleAxis[] { PrincipleAxis.Y, PrincipleAxis.Z }, // 6,7
			new PrincipleAxis[] { PrincipleAxis.X, PrincipleAxis.Y }, // 7,4

			new PrincipleAxis[] { PrincipleAxis.X, PrincipleAxis.Z }, // 0,4
			new PrincipleAxis[] { PrincipleAxis.X, PrincipleAxis.Z }, // 1,5
			new PrincipleAxis[] { PrincipleAxis.X, PrincipleAxis.Z }, // 2,6
			new PrincipleAxis[] { PrincipleAxis.X, PrincipleAxis.Z }  // 3,7
		};

		public static readonly PrincipleAxis2[] AABBEdgeAxis2 = new PrincipleAxis2[12]
		{
			PrincipleAxis2.YZ, // 0,1			
			PrincipleAxis2.XY, // 1,2			
			PrincipleAxis2.YZ, // 2,3			
			PrincipleAxis2.XY, // 3,0
			
			PrincipleAxis2.YZ, // 4,5			
			PrincipleAxis2.XY, // 5,6			
			PrincipleAxis2.YZ, // 6,7			
			PrincipleAxis2.XY, // 7,4
			
			PrincipleAxis2.XZ, // 0,4
			PrincipleAxis2.XZ, // 1,5
			PrincipleAxis2.XZ, // 2,6
			PrincipleAxis2.XZ  // 3,7
		};


		public static readonly PrincipleAxis[] AABBEdgeTangentAxis = new PrincipleAxis[12]
		{
			PrincipleAxis.X, // 0,1
			PrincipleAxis.Z, // 1,2
			PrincipleAxis.X, // 2,3
			PrincipleAxis.Z, // 3,0

			PrincipleAxis.X, // 4,5
			PrincipleAxis.Z, // 5,6
			PrincipleAxis.X, // 6,7
			PrincipleAxis.Z, // 7,4

			PrincipleAxis.Y, // 0,4
			PrincipleAxis.Y, // 1,5
			PrincipleAxis.Y, // 2,6
			PrincipleAxis.Y  // 3,7
		};

		public static readonly Vector3[] AABBEdgeTangents = new Vector3[12]
		{
			new Vector3(1, 0, 0), // 0,1
			new Vector3(0, 0, 1), // 1,2
			new Vector3(1, 0, 0), // 2,3
			new Vector3(0, 0, 1), // 3,0

			new Vector3(1, 0, 0), // 4,5
			new Vector3(0, 0, 1), // 5,6
			new Vector3(1, 0, 0), // 6,7
			new Vector3(0, 0, 1), // 7,4

			new Vector3(0, 1, 0), // 0,4
			new Vector3(0, 1, 0), // 1,5
			new Vector3(0, 1, 0), // 2,6
			new Vector3(0, 1, 0)  // 3,7
		};


		public static readonly CubeSide[][] AABBEdgeCubeSides = new CubeSide[12][]
		{
																	   // sides  normal1    normal2
			new CubeSide[] { CubeSide.NegativeZ, CubeSide.NegativeY }, // 4,2	( 0, 0,-1) ( 0,-1, 0)
			new CubeSide[] { CubeSide.PositiveX, CubeSide.NegativeY }, // 1,2	( 1, 0, 0) ( 0,-1, 0)
			new CubeSide[] { CubeSide.PositiveZ, CubeSide.NegativeY }, // 5,2	( 0, 0, 1) ( 0,-1, 0)
			new CubeSide[] { CubeSide.NegativeX, CubeSide.NegativeY }, // 0,2	(-1, 0, 0) ( 0,-1, 0)

			new CubeSide[] { CubeSide.NegativeZ, CubeSide.PositiveY }, // 4,3	( 0, 0,-1) ( 0, 1, 0)
			new CubeSide[] { CubeSide.PositiveX, CubeSide.PositiveY }, // 1,3	( 1, 0, 0) ( 0, 1, 0)
			new CubeSide[] { CubeSide.PositiveZ, CubeSide.PositiveY }, // 5,3	( 0, 0, 1) ( 0, 1, 0)
			new CubeSide[] { CubeSide.NegativeX, CubeSide.PositiveY }, // 0,3	(-1, 0, 0) ( 0, 1, 0)

			new CubeSide[] { CubeSide.NegativeX, CubeSide.NegativeZ }, // 0,4	(-1, 0, 0) ( 0, 0,-1)
			new CubeSide[] { CubeSide.PositiveX, CubeSide.NegativeZ }, // 1,4	( 1, 0, 0) ( 0, 0,-1)
			new CubeSide[] { CubeSide.PositiveX, CubeSide.PositiveZ }, // 1,5	( 1, 0, 0) ( 0, 0, 1)
			new CubeSide[] { CubeSide.NegativeX, CubeSide.PositiveZ }  // 0,5	(-1, 0, 0) ( 0, 0, 1)
		};


		// for each side returns the two side - indices
		public static readonly int[][] AABBEdgeSides = new int[12][]
		{
			new int[2] { 4, 2 },
			new int[2] { 1, 2 },
			new int[2] { 5, 2 },
			new int[2] { 0, 2 },

			new int[2] { 4, 3 },
			new int[2] { 1, 3 },
			new int[2] { 5, 3 },
			new int[2] { 0, 3 },

			new int[2] { 0, 4 },
			new int[2] { 1, 4 },
			new int[2] { 1, 5 },
			new int[2] { 0, 5 } 
		};

		public static readonly PrincipleAxis[] AABBSideAxis = new PrincipleAxis[6]
		{
			PrincipleAxis.X,
			PrincipleAxis.X,
			PrincipleAxis.Y,
			PrincipleAxis.Y,
			PrincipleAxis.Z,
			PrincipleAxis.Z 
		};

		public static readonly Vector3[] AABBSideNormal = new Vector3[6]
		{
			new Vector3(-1, 0, 0), // 0
			new Vector3( 1, 0, 0), // 1
			new Vector3( 0,-1, 0), // 2
			new Vector3( 0, 1, 0), // 3
			new Vector3( 0, 0,-1), // 4
			new Vector3( 0, 0, 1)  // 5
		};
	}
}
