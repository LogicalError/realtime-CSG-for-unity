using InternalRealtimeCSG;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace RealtimeCSG.Foundation
{
	partial struct CSGTreeBranch
	{
		[DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)] private static extern bool  GenerateBranch(Int32 userID, out Int32 generatedOperationNodeID);
		[DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)] private static extern Int32 GetBranchOperationType(Int32 branchNodeID);
		[DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)] private static extern bool  SetBranchOperationType(Int32 branchNodeID, CSGOperationType operation);
	}
}