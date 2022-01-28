using InternalRealtimeCSG;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace RealtimeCSG.Foundation
{
	partial struct CSGTree
	{
		[DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)] private static extern bool GenerateTree(Int32 userID, out Int32	generatedTreeNodeID);

		[DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)] private static extern Int32 GetNumberOfBrushesInTree(Int32 nodeID);
		[DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)] private static extern bool DoesTreeContainBrush(Int32 nodeID, Int32 brushID);


		[DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)]
		private static extern bool GenerateMeshDescriptions(Int32				treeNodeID, 
														    Int32				meshTypeCount,
														    [In]IntPtr			meshTypes,
															VertexChannelFlags	vertexChannelMask,
														    [Out]out Int32		meshDescriptionCount);

		[DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)]
		private static extern bool GetMeshDescriptions(Int32		treeNodeID, 
													   Int32        meshDescriptionCount,
													   [Out] IntPtr meshDescriptions);


		private static GeneratedMeshDescription[] GetMeshDescriptions(Int32				 treeNodeID,
																	  MeshQuery[]		 meshTypes,
																	  VertexChannelFlags vertexChannelMask)
		{
			if (meshTypes == null)
				throw new ArgumentNullException("meshTypes");

			if (meshTypes.Length == 0)
				return null;

			Int32 meshDescriptionCount;
		
			GCHandle	nativeMeshTypeHandle	= GCHandle.Alloc(meshTypes, GCHandleType.Pinned);
			IntPtr		nativeMeshTypePtr		= nativeMeshTypeHandle.AddrOfPinnedObject();
			
			var result = GenerateMeshDescriptions(treeNodeID, meshTypes.Length, nativeMeshTypePtr, vertexChannelMask, out meshDescriptionCount);
			
			nativeMeshTypeHandle.Free();
			if (!result || meshDescriptionCount == 0)
				return null;
			
			var meshDescriptions        = new GeneratedMeshDescription[meshDescriptionCount];
			var meshDescriptionsHandle	= GCHandle.Alloc(meshDescriptions, GCHandleType.Pinned);
			var meshDescriptionsPtr		= meshDescriptionsHandle.AddrOfPinnedObject();
			
			result = GetMeshDescriptions(treeNodeID, meshDescriptionCount, meshDescriptionsPtr);
			
			meshDescriptionsHandle.Free();

			if (!result ||
				meshDescriptions[0].vertexCount <= 0 || 
				meshDescriptions[0].indexCount <= 0)
				return null;

			return meshDescriptions;
		}
		
		[DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)]
		private static extern bool GetGeneratedMesh(Int32			treeNodeID, 
													Int32			meshIndex,
													Int32			subMeshIndex,

													Int32			indexCount,
													[Out] IntPtr	indices,

													Int32			vertexCount,
													[Out] IntPtr	positions,
													[Out] IntPtr	tangents,
													[Out] IntPtr	normals,
													[Out] IntPtr	uvs,

													out Vector3		boundsCenter,
													out Vector3		boundsSize);
		
		private static GeneratedMeshContents GetGeneratedMesh(int treeNodeID, GeneratedMeshDescription meshDescription)
		{
			if (meshDescription.vertexCount <= 0 || 
				meshDescription.indexCount <= 0)
			{
				Debug.LogWarning(string.Format("{0} called with a {1} that isn't valid", typeof(CSGTree).Name, typeof(GeneratedMeshDescription).Name));
				return null;
			}
			
			var generatedMesh = new GeneratedMeshContents();
			var usedVertexChannels	= meshDescription.meshQuery.UsedVertexChannels;
			var vertexCount			= meshDescription.vertexCount;
			var indexCount			= meshDescription.indexCount;
			var meshIndex			= meshDescription.meshQueryIndex;
			var subMeshIndex		= meshDescription.subMeshQueryIndex;
			generatedMesh.description	= meshDescription;
			
			// create our arrays on the managed side with the correct size
			generatedMesh.tangents		= ((usedVertexChannels & VertexChannelFlags.Tangent) != 0) ? new Vector4[vertexCount] : null;
			generatedMesh.normals		= ((usedVertexChannels & VertexChannelFlags.Normal ) != 0) ? new Vector3[vertexCount] : null;
			generatedMesh.uv0			= ((usedVertexChannels & VertexChannelFlags.UV0    ) != 0) ? new Vector2[vertexCount] : null;
			generatedMesh.positions		= new Vector3[vertexCount];
			generatedMesh.indices		= new int[indexCount];
			
			GCHandle indicesHandle		= GCHandle.Alloc(generatedMesh.indices,  GCHandleType.Pinned);
			GCHandle positionHandle		= GCHandle.Alloc(generatedMesh.positions, GCHandleType.Pinned);
			GCHandle tangentHandle		= new GCHandle();
			GCHandle normalHandle		= new GCHandle();
			GCHandle uv0Handle			= new GCHandle();
			
			IntPtr	indicesPtr	= indicesHandle.AddrOfPinnedObject();
			IntPtr	positionPtr	= positionHandle.AddrOfPinnedObject();
			IntPtr	tangentPtr	= IntPtr.Zero;
			IntPtr	normalPtr	= IntPtr.Zero;
			IntPtr	uv0Ptr		= IntPtr.Zero;

			if (generatedMesh.tangents	!= null) { tangentHandle = GCHandle.Alloc(generatedMesh.tangents,	GCHandleType.Pinned); tangentPtr  = tangentHandle.AddrOfPinnedObject(); }
			if (generatedMesh.normals	!= null) { normalHandle	 = GCHandle.Alloc(generatedMesh.normals,	GCHandleType.Pinned); normalPtr   = normalHandle.AddrOfPinnedObject(); }
			if (generatedMesh.uv0		!= null) { uv0Handle	 = GCHandle.Alloc(generatedMesh.uv0,		GCHandleType.Pinned); uv0Ptr	  = uv0Handle.AddrOfPinnedObject(); }
			
			var boundsCenter = Vector3.zero;
			var boundsSize = Vector3.zero;
			var result = GetGeneratedMesh((Int32)treeNodeID,
										  (Int32)meshIndex,
										  (Int32)subMeshIndex,

										  (Int32)indexCount,
										  indicesPtr,

										  (Int32)vertexCount,
										  positionPtr,
										  tangentPtr,
										  normalPtr,
										  uv0Ptr,
										  out boundsCenter,
										  out boundsSize);
			
			if (generatedMesh.uv0		!= null) { uv0Handle	 .Free(); }
			if (generatedMesh.normals	!= null) { normalHandle	 .Free(); }
			if (generatedMesh.tangents	!= null) { tangentHandle .Free(); }
			positionHandle.Free(); 
			indicesHandle.Free();
			
			if (!result)
				return null;

			generatedMesh.bounds = new Bounds(boundsCenter, boundsSize);
			return generatedMesh;
		}
		
		
		// Do not use. This method might be removed/renamed in the future
		[EditorBrowsable(EditorBrowsableState.Never)]
		public int		CountOfBrushesInTree			{ get { return GetNumberOfBrushesInTree(treeNodeID); } }
		
		// Do not use. This method might be removed/renamed in the future
		[EditorBrowsable(EditorBrowsableState.Never)]
		public bool		IsInTree(CSGTreeBrush brush)	{ return DoesTreeContainBrush(treeNodeID, brush.NodeID); }		
	}
}