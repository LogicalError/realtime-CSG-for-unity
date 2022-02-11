using InternalRealtimeCSG;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace RealtimeCSG.Foundation
{
	partial struct BrushMeshInstance
	{
		[DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)]
		static extern Int32 CreateBrushMesh(Int32 userID,
											Int32 vertexCount,	 [In] IntPtr vertices,												    
											Int32 halfEdgeCount, [In] IntPtr halfEdges,
											Int32 polygonCount,	 [In] IntPtr polygons);

		private static Int32 CreateBrushMesh(Int32 userID,   
											 Int32 vertexCount,   Vector3[]            vertices,
										     Int32 halfEdgeCount, BrushMesh.HalfEdge[] halfEdges,
										     Int32 polygonCount,  BrushMesh.Polygon[]  polygons)
		{
			var verticesHandle	= GCHandle.Alloc(vertices,  GCHandleType.Pinned);
			var halfEdgesHandle	= GCHandle.Alloc(halfEdges, GCHandleType.Pinned);
			var polygonsHandle	= GCHandle.Alloc(polygons,  GCHandleType.Pinned);
			var brushMeshID = CreateBrushMesh(userID,
											  vertexCount,   verticesHandle .AddrOfPinnedObject(),
											  halfEdgeCount, halfEdgesHandle.AddrOfPinnedObject(),
											  polygonCount,	 polygonsHandle .AddrOfPinnedObject());			
			polygonsHandle.Free();
			halfEdgesHandle.Free();
			verticesHandle.Free();
			return brushMeshID;
		}

		private static Int32 CreateBrushMesh(int userID, BrushMesh brushMesh)
		{
			if (brushMesh			== null ||
				brushMesh.vertices	== null ||
				brushMesh.halfEdges	== null ||
				brushMesh.polygons	== null)
				return 0;

			var edgeCount = brushMesh.halfEdges.Length;
			if (edgeCount < 12)
				return 0;
			
			var polygonCount = brushMesh.polygons.Length;
			if (polygonCount <= 4)
				return 0;
			
			var vertexCount = brushMesh.vertices.Length;
			var result = CreateBrushMesh(userID,
									     vertexCount,  brushMesh.vertices,
									     edgeCount,    brushMesh.halfEdges,
									     polygonCount, brushMesh.polygons);
			if (result <= 0)
				result = 0;
			return result;
		}

		[DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)]
		private static extern Int32 GetBrushMeshUserID(Int32 brushMeshIndex);


		[DllImport(CSGManager.NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool UpdateBrushMesh(Int32 brushMeshIndex,
												   Int32 vertexCount,    [In] IntPtr vertices,
												   Int32 halfEdgeCount,  [In] IntPtr halfEdges,
												   Int32 polygonCount,   [In] IntPtr polygons);
		private static bool UpdateBrushMesh(Int32 brushMeshIndex,
											Int32 vertexCount,    Vector3[]            vertices,
											Int32 halfEdgeCount,  BrushMesh.HalfEdge[] halfEdges,
											Int32 polygonCount,   BrushMesh.Polygon[]  polygons)
		{
			if (vertices == null || halfEdges == null || polygons == null) return false;
			if (vertexCount < 0 || halfEdgeCount < 0 || polygonCount < 0) return false;

			var verticesHandle	= GCHandle.Alloc(vertices, GCHandleType.Pinned);
			var halfEdgesHandle	= GCHandle.Alloc(halfEdges, GCHandleType.Pinned);
			var polygonsHandle	= GCHandle.Alloc(polygons, GCHandleType.Pinned);
			var result = UpdateBrushMesh(brushMeshIndex,
										 vertexCount,    verticesHandle.AddrOfPinnedObject(),
										 halfEdgeCount,  halfEdgesHandle.AddrOfPinnedObject(),
										 polygonCount,   polygonsHandle.AddrOfPinnedObject());			
			polygonsHandle.Free();
			halfEdgesHandle.Free();
			verticesHandle.Free();
			return result;
		}
		
		private static bool UpdateBrushMesh(Int32		brushMeshIndex,
											BrushMesh	brushMesh)
		{
			if (brushMeshIndex == 0 ||
				brushMesh.vertices	== null ||
				brushMesh.halfEdges == null ||
				brushMesh.polygons	== null)
				return false;

			var edgeCount = brushMesh.halfEdges.Length;
			if (edgeCount < 12)
				return false;
			
			var polygonCount = brushMesh.polygons.Length;
			if (polygonCount < 5)
				return false;
			
			var vertexCount = brushMesh.vertices.Length;
			var result = UpdateBrushMesh(brushMeshIndex,
										 vertexCount,  brushMesh.vertices,
									     edgeCount,    brushMesh.halfEdges,
									     polygonCount, brushMesh.polygons);
			return result;
		}
		
		[DllImport(CSGManager.NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool DestroyBrushMesh(Int32	brushMeshIndex);

		[DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)]
		private static extern bool IsBrushMeshIDValid(Int32	brushMeshIndex);
	}
}