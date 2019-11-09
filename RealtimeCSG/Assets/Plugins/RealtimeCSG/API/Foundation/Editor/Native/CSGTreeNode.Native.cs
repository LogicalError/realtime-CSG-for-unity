using InternalRealtimeCSG;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace RealtimeCSG.Foundation
{
	partial struct CSGTreeNode
	{
		[DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)] internal static extern bool	ClearDirty(Int32 nodeID);		
		[DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)] internal static extern bool	IsNodeDirty(Int32 nodeID);
		[DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)] internal static extern bool	SetDirty(Int32 nodeID);
		
		[DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)] internal static extern CSGNodeType GetTypeOfNode(Int32 nodeID);
		[DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)] internal static extern bool	IsNodeIDValid(Int32 nodeID);
		[DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)] internal static extern Int32	GetUserIDOfNode(Int32 nodeID);

		[DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)] internal static extern Int32	GetParentOfNode(Int32 nodeID);		
		[DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)] internal static extern Int32	GetTreeOfNode(Int32 nodeID);
		
		[DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)] private	static extern bool	SetChildNodes(Int32 nodeID, Int32 childCount, IntPtr childrenNodeIDs);

		[DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)] internal static extern Int32	GetChildNodeCount(Int32 nodeID);
		[DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)] private	static extern bool	GetChildNodes(Int32 nodeID, Int32 childCount, IntPtr children, int arrayIndex);


		[DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)] internal static extern bool	RemoveChildNode(Int32 nodeID, Int32 childNodeID);
		[DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)] internal static extern bool	AddChildNode(Int32 nodeID, Int32 childNodeID);
		[DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)] internal static extern bool	ClearChildNodes(Int32 nodeID);
		[DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)] internal static extern Int32	GetChildNodeAtIndex(Int32 nodeID, Int32 index);
		
		[DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)] internal static extern bool	DestroyNode(Int32 nodeID);
		[DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)] private  static extern bool	DestroyNodes(Int32 nodeCount, IntPtr nodeIDs);

		
		[DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)] internal static extern bool	RemoveChildNodeAt(Int32 nodeID, Int32 index); 
		[DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)] internal static extern bool	InsertChildNode(Int32 nodeID, Int32 index, Int32 childNodeID); 
		[DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)] internal static extern Int32	IndexOfChildNode(Int32 nodeID, Int32 childNodeID); 

		[DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)] internal static extern bool	RemoveChildNodeRange(Int32 nodeID, Int32 index, Int32 count);
		[DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)] private  static extern bool	InsertChildNodeRange(Int32 nodeID, Int32 index, Int32 count, IntPtr nodes);

		internal static bool SetChildNodes(Int32 nodeID, CSGTreeNode[] children)
		{
			if (children == null)
				return false;
			GCHandle	childrenHandle	= GCHandle.Alloc(children, GCHandleType.Pinned);
			IntPtr		childrenPtr		= childrenHandle.AddrOfPinnedObject();
			var result = SetChildNodes(nodeID, children.Length, childrenPtr);
			childrenHandle.Free();
			return result;
		}

		internal static CSGTreeNode[] GetChildNodes(Int32 nodeID)
		{
			var childCount = GetChildNodeCount(nodeID);
			var children = new CSGTreeNode[childCount];
			if (childCount == 0)
				return children;

			GCHandle	childrenHandle	= GCHandle.Alloc(children, GCHandleType.Pinned);
			IntPtr		childrenPtr		= childrenHandle.AddrOfPinnedObject();
			GetChildNodes(nodeID, childCount, childrenPtr, 0);
			childrenHandle.Free();
			return children;
		}

		internal static int CopyTo(Int32 nodeID, CSGTreeNode[] children, int arrayIndex)
		{
			if (children == null)
				throw new ArgumentNullException("children");

			var childCount = GetChildNodeCount(nodeID);
			if (childCount <= 0)
				return 0;

			if (children.Length + arrayIndex < childCount)
				throw new ArgumentException(string.Format("The array does not have enough elements, its length is {0} and needs at least {1}", children.Length, childCount), "children");

			return CopyToUnsafe(nodeID, childCount, children, arrayIndex);
		}


		internal static int CopyToUnsafe(Int32 nodeID, int childCount, CSGTreeNode[] children, int arrayIndex)
		{
			GCHandle	childrenHandle	= GCHandle.Alloc(children, GCHandleType.Pinned);
			IntPtr		childrenPtr		= childrenHandle.AddrOfPinnedObject();
			GetChildNodes(nodeID, childCount, childrenPtr, arrayIndex);
			childrenHandle.Free();
			return childCount;
		}

		internal static bool InsertChildNodeRange(Int32 nodeID, Int32 index, CSGTreeNode[] children)
		{
			if (children == null)
				return false;
			GCHandle	childrenHandle	= GCHandle.Alloc(children, GCHandleType.Pinned);
			IntPtr		childrenPtr		= childrenHandle.AddrOfPinnedObject();
			var result = InsertChildNodeRange(nodeID, index, children.Length, childrenPtr);
			childrenHandle.Free();
			return result;
		}
	}
}