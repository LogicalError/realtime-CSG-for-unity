using System;
using System.Runtime.InteropServices;
using Vector3 = UnityEngine.Vector3;
using Vector4 = UnityEngine.Vector4;

namespace RealtimeCSG.Foundation
{
	/// <summary>
	/// This class holds functionality to create and modify <see cref="RealtimeCSG.Foundation.BrushMesh"/>es
	/// </summary>
	public sealed class BrushMeshUtility
	{
		/// <summary>
		/// Creates a cube <see cref="RealtimeCSG.Foundation.BrushMesh"/> with <paramref name="size"/> and <paramref name="layers"/> and optional <paramref name="surfaceFlags"/>
		/// </summary>
		/// <remarks><note>These methods are a work in progress and may change somewhat over time</note></remarks>
		/// <param name="size">The size of the cube</param>
		/// <param name="layers">The <see cref="RealtimeCSG.Foundation.SurfaceLayers"/> that define how the surfaces of this cube are rendered or if they need to be part of a collider etc.</param>
		/// <param name="surfaceFlags">Optional flags that modify surface behavior.</param>
		/// <returns>A <see cref="RealtimeCSG.Foundation.BrushMesh"/> on success, null on failure</returns>
		public static BrushMesh CreateCube(UnityEngine.Vector3 size, SurfaceLayers layers, SurfaceFlags surfaceFlags = SurfaceFlags.None)
		{
			var halfSize = size * 0.5f;
			return CreateCube(-halfSize, halfSize, layers, surfaceFlags);
		}

		/// <summary>
		/// Creates a cube <see cref="RealtimeCSG.Foundation.BrushMesh"/> with bounds defined by <paramref name="min"/> and <paramref name="max"/>, its <paramref name="layers"/> and optional <paramref name="surfaceFlags"/>
		/// </summary>
		/// <remarks><note>These methods are a work in progress and may change somewhat over time</note></remarks>
		/// <param name="min">The corner of the cube with the smallest x,y,z values</param>
		/// <param name="max">The corner of the cube with the largest x,y,z values</param>
		/// <param name="layers">The <see cref="RealtimeCSG.Foundation.SurfaceLayers"/> that define how the surfaces of this cube are rendered or if they need to be part of a collider etc.</param>
		/// <param name="surfaceFlags">Optional flags that modify surface behavior.</param>
		/// <returns>A <see cref="RealtimeCSG.Foundation.BrushMesh"/> on success, null on failure</returns>
		public static BrushMesh CreateCube(UnityEngine.Vector3 min, UnityEngine.Vector3 max, SurfaceLayers layers, SurfaceFlags surfaceFlags = SurfaceFlags.None)
		{
			if (min.x == max.x || min.y == max.y || min.z == max.z)
				return null;

			if (min.x > max.x) { float x = min.x; min.x = max.x; max.x = x; }
			if (min.y > max.y) { float y = min.y; min.y = max.y; max.y = y; }
			if (min.z > max.z) { float z = min.z; min.z = max.z; max.z = z; }

			var surfaceMatrices = new[]
			{
				// left/right
				new UVMatrix { U = new Vector4(-1, 0,  0, -min.x), V = new Vector4( 0, 1,  0,  min.y) }, 
				new UVMatrix { U = new Vector4( 1, 0,  0,  min.x), V = new Vector4( 0, 1,  0,  min.y) }, 
				
				// front/back
				new UVMatrix { U = new Vector4( 0, 0,  1,  min.z), V = new Vector4( 0, 1,  0,  min.y) }, 
				new UVMatrix { U = new Vector4( 0, 0, -1, -min.z), V = new Vector4( 0, 1,  0,  min.y) }, 
				
				// top/down
				new UVMatrix { U = new Vector4(-1, 0,  0, -min.x), V = new Vector4( 0, 0, -1, -min.z) }, 
				new UVMatrix { U = new Vector4( 1, 0,  0,  min.x), V = new Vector4( 0, 0, -1, -min.z) }, 
			};

			return new BrushMesh
			{
				polygons = new[]
				{
					// left/right
					new BrushMesh.Polygon{ polygonID = 0, firstEdge =  0, edgeCount = 4, surface = new SurfaceDescription { UV0 = surfaceMatrices[0], surfaceFlags = surfaceFlags, smoothingGroup = 0 }, layers = layers },
					new BrushMesh.Polygon{ polygonID = 1, firstEdge =  4, edgeCount = 4, surface = new SurfaceDescription { UV0 = surfaceMatrices[1], surfaceFlags = surfaceFlags, smoothingGroup = 0 }, layers = layers },
				
					// front/back
					new BrushMesh.Polygon{ polygonID = 2, firstEdge =  8, edgeCount = 4, surface = new SurfaceDescription { UV0 = surfaceMatrices[2], surfaceFlags = surfaceFlags, smoothingGroup = 0 }, layers = layers },
					new BrushMesh.Polygon{ polygonID = 3, firstEdge = 12, edgeCount = 4, surface = new SurfaceDescription { UV0 = surfaceMatrices[3], surfaceFlags = surfaceFlags, smoothingGroup = 0 }, layers = layers },
				
					// top/down
					new BrushMesh.Polygon{ polygonID = 4, firstEdge = 16, edgeCount = 4, surface = new SurfaceDescription { UV0 = surfaceMatrices[4], surfaceFlags = surfaceFlags, smoothingGroup = 0 }, layers = layers },
					new BrushMesh.Polygon{ polygonID = 5, firstEdge = 20, edgeCount = 4, surface = new SurfaceDescription { UV0 = surfaceMatrices[5], surfaceFlags = surfaceFlags, smoothingGroup = 0 }, layers = layers }
				},

				halfEdges = new[]
				{
					// polygon 0
					new BrushMesh.HalfEdge{ twinIndex = 17, vertexIndex = 0 },	//  0
					new BrushMesh.HalfEdge{ twinIndex =  8, vertexIndex = 1 },	//  1
					new BrushMesh.HalfEdge{ twinIndex = 20, vertexIndex = 2 },	//  2
					new BrushMesh.HalfEdge{ twinIndex = 13, vertexIndex = 3 },	//  3
				
					// polygon 1
					new BrushMesh.HalfEdge{ twinIndex = 10, vertexIndex = 4 },	//  4
					new BrushMesh.HalfEdge{ twinIndex = 19, vertexIndex = 5 },	//  5
					new BrushMesh.HalfEdge{ twinIndex = 15, vertexIndex = 6 },	//  6
					new BrushMesh.HalfEdge{ twinIndex = 22, vertexIndex = 7 },	//  7
					
					// polygon 2
					new BrushMesh.HalfEdge{ twinIndex =  1, vertexIndex = 0 },	//  8
					new BrushMesh.HalfEdge{ twinIndex = 16, vertexIndex = 4 },	//  9
					new BrushMesh.HalfEdge{ twinIndex =  4, vertexIndex = 7 },	// 10
					new BrushMesh.HalfEdge{ twinIndex = 21, vertexIndex = 1 },	// 11
					
					// polygon 3
					new BrushMesh.HalfEdge{ twinIndex = 18, vertexIndex = 3 },	// 16
					new BrushMesh.HalfEdge{ twinIndex =  3, vertexIndex = 2 },	// 17
					new BrushMesh.HalfEdge{ twinIndex = 23, vertexIndex = 6 },	// 18
					new BrushMesh.HalfEdge{ twinIndex =  6, vertexIndex = 5 },	// 19
					 
					// polygon 4
					new BrushMesh.HalfEdge{ twinIndex =  9, vertexIndex = 0 },	// 20
					new BrushMesh.HalfEdge{ twinIndex =  0, vertexIndex = 3 },	// 21
					new BrushMesh.HalfEdge{ twinIndex = 12, vertexIndex = 5 },	// 22
					new BrushMesh.HalfEdge{ twinIndex =  5, vertexIndex = 4 },	// 23
					
					// polygon 5
					new BrushMesh.HalfEdge{ twinIndex =  2, vertexIndex = 1 },	// 12
					new BrushMesh.HalfEdge{ twinIndex = 11, vertexIndex = 7 },	// 13
					new BrushMesh.HalfEdge{ twinIndex =  7, vertexIndex = 6 },	// 14
					new BrushMesh.HalfEdge{ twinIndex = 14, vertexIndex = 2 }	// 15
				},

				vertices = new[]
				{
					new Vector3( min.x, min.y, min.z), // 0
					new Vector3( min.x, max.y, min.z), // 1
					new Vector3( max.x, max.y, min.z), // 2
					new Vector3( max.x, min.y, min.z), // 3

					new Vector3( min.x, min.y, max.z), // 4  
					new Vector3( max.x, min.y, max.z), // 5
					new Vector3( max.x, max.y, max.z), // 6
					new Vector3( min.x, max.y, max.z)  // 7
				}
			};
		}
	}
}
