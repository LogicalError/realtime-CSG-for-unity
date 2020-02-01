using InternalRealtimeCSG;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace RealtimeCSG.Foundation
{
	sealed partial class CSGManager
	{
		internal const string NativePluginName = "RealtimeCSG[" + Versioning.PluginDLLVersion + "]";

		[DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)] private static extern bool HasBeenCompiledInDebugMode();

		[DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)] private static extern bool ClearDirty(Int32 nodeID);		
		
		[DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)] private static extern void ClearAllNodes();
		[DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)] private static extern bool UpdateAllTreeMeshes();
		[DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)] private static extern void RebuildAll();
				
		[DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)] private static extern int GetNodeCount();
		[DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)] private static extern int GetBrushCount();
		[DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)] private static extern int GetBranchCount();
		[DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)] private static extern int GetTreeCount();
		[DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)] private static extern int GetBrushMeshCount();

		[DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)] private static extern bool GetAllTreeNodes(Int32 nodeCount, IntPtr allNodeIDs);
		[DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)] private static extern bool GetAllTrees(Int32 nodeCount, IntPtr allNodeIDs);
		[DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)] private static extern bool GetAllBrushMeshInstances(Int32 nodeCount, IntPtr allNodeIDs);

		[DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)] private static extern bool DestroyNodes(Int32 nodeCount, IntPtr nodeIDs);

		private static bool DestroyNodes(Int32 nodeCount, CSGTreeNode[] nodeIDs)
		{
			if (nodeIDs == null)
				return false;
			GCHandle nodeIDsHandle = GCHandle.Alloc(nodeIDs, GCHandleType.Pinned);
			IntPtr nodeIDsPtr = nodeIDsHandle.AddrOfPinnedObject();
			var result = DestroyNodes(nodeCount, nodeIDsPtr);
			nodeIDsHandle.Free();
			return result;
		}
				
		private static CSGTreeNode[] GetAllTreeNodes()
		{
			var nodeCount = GetNodeCount();
			var allTreeNodeIDs = new CSGTreeNode[nodeCount];
			if (nodeCount == 0)
				return allTreeNodeIDs;

			GCHandle allNodeIDsHandle = GCHandle.Alloc(allTreeNodeIDs, GCHandleType.Pinned);
			IntPtr allNodeIDsPtr = allNodeIDsHandle.AddrOfPinnedObject();
			GetAllTreeNodes(nodeCount, allNodeIDsPtr);
			allNodeIDsHandle.Free();
			return allTreeNodeIDs;
		}

		private static CSGTree[] GetAllTrees()
		{
			var nodeCount = GetTreeCount();
			var allTrees = new CSGTree[nodeCount];
			if (nodeCount == 0)
				return allTrees;

			GCHandle allTreeIDsHandle = GCHandle.Alloc(allTrees, GCHandleType.Pinned);
			IntPtr allTreeIDsPtr = allTreeIDsHandle.AddrOfPinnedObject();
			GetAllTrees(nodeCount, allTreeIDsPtr);
			allTreeIDsHandle.Free();
			return allTrees;
		}

		private static BrushMeshInstance[] GetAllBrushMeshInstances()
		{
			var nodeCount = GetBrushMeshCount();
			var allInstances = new BrushMeshInstance[nodeCount];
			if (nodeCount == 0)
				return allInstances;

			GCHandle	allInstancesIDsHandle	= GCHandle.Alloc(allInstances, GCHandleType.Pinned);
			IntPtr		allInstancesIDsPtr		= allInstancesIDsHandle.AddrOfPinnedObject();
			GetAllBrushMeshInstances(nodeCount, allInstancesIDsPtr);
			allInstancesIDsHandle.Free();
			return allInstances;
		}


		// Do not use. This method might be removed/renamed in the future
		[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
		public static bool	ClearDirty	(CSGTreeNode   node)	{ return ClearDirty(node.NodeID); }
	}
}