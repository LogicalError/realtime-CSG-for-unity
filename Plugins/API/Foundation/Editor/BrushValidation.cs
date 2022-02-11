using System;
using System.Linq;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using Vector4 = UnityEngine.Vector4;
using Quaternion = UnityEngine.Quaternion;
using Matrix4x4 = UnityEngine.Matrix4x4;
using Mathf = UnityEngine.Mathf;
using Plane = UnityEngine.Plane;
using Debug = UnityEngine.Debug;
using System.Collections.Generic;
using RealtimeCSG.Foundation;

namespace RealtimeCSG.Foundation
{
	public sealed partial class BrushValidation
	{
		public static bool ValidateBrushMesh(BrushMesh brushMesh)
		{
			var vertices = brushMesh.vertices;
			if (vertices == null || vertices.Length == 0)
			{
				Debug.LogError("brushMesh has no vertices set");
				return false;
			}

			var halfEdges = brushMesh.halfEdges;
			if (halfEdges == null || halfEdges.Length == 0)
			{
				Debug.LogError("brushMesh has no halfEdges set");
				return false;
			}

			var polygons = brushMesh.polygons;
			if (polygons == null || polygons.Length == 0)
			{
				Debug.LogError("brushMesh has no polygons set");
				return false;
			}

			bool fail = false;

			for (int h = 0; h < halfEdges.Length; h++)
			{
				if (halfEdges[h].vertexIndex < 0)
				{
					Debug.LogError("brushMesh.halfEdges[" + h + "].vertexIndex is " + halfEdges[h].vertexIndex);
					fail = true;
				}
				else
				if (halfEdges[h].vertexIndex >= vertices.Length)
				{
					Debug.LogError("brushMesh.halfEdges[" + h + "].vertexIndex is " + halfEdges[h].vertexIndex + ", but there are " + vertices.Length + " vertices.");
					fail = true;
				}

				if (halfEdges[h].twinIndex < 0)
				{
					Debug.LogError("brushMesh.halfEdges[" + h + "].twinIndex is " + halfEdges[h].twinIndex);
					fail = true;
					continue;
				}
				else
				if (halfEdges[h].twinIndex >= halfEdges.Length)
				{
					Debug.LogError("brushMesh.halfEdges[" + h + "].twinIndex is " + halfEdges[h].twinIndex + ", but there are " + halfEdges.Length + " edges.");
					fail = true;
					continue;
				}

				var twinIndex = halfEdges[h].twinIndex;
				var twin = halfEdges[twinIndex];
				if (twin.twinIndex != h)
				{
					Debug.LogError("brushMesh.halfEdges[" + h + "].twinIndex is " + halfEdges[h].twinIndex + ", but the twinIndex of its twin is " + twin.twinIndex + " instead of " + h + ".");
					fail = true;
				}
			}

			for (int p = 0; p < polygons.Length; p++)
			{
				var firstEdge = polygons[p].firstEdge;
				var count = polygons[p].edgeCount;
				if (firstEdge < 0)
				{
					Debug.LogError("brushMesh.polygons[" + p + "].firstEdge is " + firstEdge);
					fail = true;
				}
				else
				if (firstEdge >= halfEdges.Length)
				{
					Debug.LogError("brushMesh.polygons[" + p + "].firstEdge is " + firstEdge + ", but there are " + halfEdges.Length + " edges.");
					fail = true;
				}
				if (count <= 0)
				{
					Debug.LogError("brushMesh.polygons[" + p + "].edgeCount is " + count);
					fail = true;
				}
				else
				if (firstEdge + count > halfEdges.Length)
				{
					Debug.LogError("brushMesh.polygons[" + p + "].firstEdge + brushMesh.polygons[" + p + "].edgeCount is " + (firstEdge + count) + ", but there are " + halfEdges.Length + " edges.");
					fail = true;
				}
				else
				if (p < polygons.Length - 1 &&
					polygons[p + 1].firstEdge != firstEdge + count)
				{
					Debug.LogError("brushMesh.polygons[" + (p + 1) + "].firstEdge does not equal brushMesh.polygons[" + p + "].firstEdge + brushMesh.polygons[" + p + "].edgeCount.");
					fail = true;
				}

				for (int i1 = 0, i0 = count - 1; i1 < count; i0 = i1, i1++)
				{
					var h0 = halfEdges[i0 + firstEdge]; // curr
					var h1 = halfEdges[i1 + firstEdge]; // curr.prev
					var t1 = halfEdges[h1.twinIndex];   // curr.prev.twin

					if (h0.vertexIndex != t1.vertexIndex)
					{
						Debug.LogError("brushMesh.halfEdges[" + (i0 + firstEdge) + "].vertexIndex (" + h0.vertexIndex + ") is not equal to brushMesh.halfEdges[" + h1.twinIndex + "].vertexIndex (" + t1.vertexIndex + ").");
						fail = true;
					}
				}
			}


			return !fail;
		}

		public static bool IsValidBounds(UnityEngine.Vector3 min, UnityEngine.Vector3 max)
		{
			const float kMinSize = 0.0001f;
			if (Mathf.Abs(max.x - min.x) < kMinSize ||
				Mathf.Abs(max.y - min.y) < kMinSize ||
				Mathf.Abs(max.z - min.z) < kMinSize ||
				float.IsInfinity(min.x) || float.IsInfinity(min.y) || float.IsInfinity(min.z) ||
				float.IsInfinity(max.x) || float.IsInfinity(max.y) || float.IsInfinity(max.z) ||
				float.IsNaN(min.x) || float.IsNaN(min.y) || float.IsNaN(min.z) ||
				float.IsNaN(max.x) || float.IsNaN(max.y) || float.IsNaN(max.z))
				return false;
			return true;
		}
	}
}