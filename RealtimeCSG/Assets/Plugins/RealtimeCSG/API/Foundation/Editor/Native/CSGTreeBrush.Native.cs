using InternalRealtimeCSG;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace RealtimeCSG.Foundation
{
	partial struct CSGTreeBrush
	{
		[DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)] private static extern bool  GenerateBrush(Int32 userID, out Int32 generatedNodeID);
		[DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)] private static extern CSGTreeBrushFlags GetBrushFlags(Int32 brushNodeID);
		[DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)] private static extern bool  SetBrushFlags(Int32 brushNodeID, CSGTreeBrushFlags flags);
		[DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)] private static extern Int32 GetBrushOperationType(Int32 brushNodeID);
		[DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)] private static extern bool  SetBrushOperationType(Int32 brushNodeID, CSGOperationType operation);
		[DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)] private static extern Int32 GetBrushMeshID(Int32 brushNodeID);
		[DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)] private static extern bool  SetBrushMeshID(Int32 brushNodeID, Int32 brushMeshID);
		[DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)] private static extern bool  GetNodeLocalTransformation(Int32 brushNodeID, [Out] out Matrix4x4 brushToTreeSpace);

		private static bool SetBrushMesh(Int32 brushNodeID, BrushMeshInstance brushMesh)
		{
			return SetBrushMeshID(brushNodeID, brushMesh.brushMeshID);
		}

		private static BrushMeshInstance GetBrushMesh(Int32 brushNodeID)
		{
			return new BrushMeshInstance { brushMeshID = GetBrushMeshID(brushNodeID) };
		}


		private static Matrix4x4 GetNodeLocalTransformation(Int32 brushNodeID)
		{
			Matrix4x4 result = Matrix4x4.identity;
			if (GetNodeLocalTransformation(brushNodeID, out result))
				return result;
			return Matrix4x4.identity;
		}

		[DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)] private static extern bool SetNodeLocalTransformation(Int32 brushNodeID, [In] ref Matrix4x4 brushToTreeSpace);
		[DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)] private static extern bool GetBrushBounds(Int32 brushNodeID, ref AABB bounds);

		private static Bounds GetBrushBounds(Int32 brushNodeID)
		{
			var	aabb = new AABB();
            if (GetBrushBounds(brushNodeID, ref aabb))
                return new Bounds(aabb.Center, aabb.Size);
			return new Bounds();
		}
	}
}