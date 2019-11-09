using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEditor;
using InternalRealtimeCSG;
using System.Reflection;
using UnityEngine.Rendering;
using System.Runtime.InteropServices;

namespace RealtimeCSG
{
	internal partial class InternalCSGModelManager
	{
		private static bool				_isInitialized	= false;
		private static readonly object	_lockObj		= new object();

		internal static NativeMethods External;

		#region Clear
		public static void Clear()
		{
			BrushOutlineManager.ClearOutlines();
			ClearRegistration();
			ClearCaches();
			_isHierarchyModified = true;
		}
		#endregion
		
		#region InitOnNewScene
		public static void InitOnNewScene()
		{
			if (EditorApplication.isPlayingOrWillChangePlaymode)
			{
				return;
			}

			if (External == null ||
				External.ResetCSG == null)
			{
				NativeMethodBindings.RegisterUnityMethods();
				NativeMethodBindings.RegisterExternalMethods();
			}

			if (External == null)
				return;
			
			if (RegisterAllComponents())
				External.ResetCSG();
		}
		#endregion

		#region Shutdown
		public static void Shutdown()
		{
			SceneStates.Clear();

			ClearRegistration();
			ClearCaches();

			_isInitialized = false;
		}
		#endregion

		#region UndoRedoPerformed
		public static void UndoRedoPerformed()
		{
			BrushOutlineManager.ClearOutlines();
			
			CheckForChanges(forceHierarchyUpdate: true);
		}
		#endregion
		
		#region CheckForChanges
		public static bool skipCheckForChanges = false;
		public static void CheckForChanges(bool forceHierarchyUpdate = false)
		{
			if (EditorApplication.isPlayingOrWillChangePlaymode)
				return;

			if (!forceHierarchyUpdate && skipCheckForChanges)
				return;
			
			lock (_lockObj)
			{ 
				if (!_isInitialized)
				{
					RegisterAllComponents();
					forceHierarchyUpdate = true;
					_isInitialized = true;
					
					InternalCSGModelManager.OnHierarchyModified();
					UpdateRemoteMeshes(); 
				}
				
				forceHierarchyUpdate = InternalCSGModelManager.UpdateModelSettings() || forceHierarchyUpdate;
				
				// unfortunately this is the only reliable way I could find
				// to determine when a transform is modified, either in 
				// the inspector or the scene.
				InternalCSGModelManager.CheckTransformChanged(forceHierarchyUpdate);
				
				if (_isHierarchyModified || forceHierarchyUpdate)
				{
					InternalCSGModelManager.OnHierarchyModified();
					InternalCSGModelManager.OnHierarchyModified();
					_isHierarchyModified = false;
				}

				InternalCSGModelManager.UpdateMeshes();
				MeshInstanceManager.UpdateHelperSurfaceVisibility();
			}
		}
		#endregion

		#region ForceRebuildAll
		public static void ForceRebuildAll()
		{
			Clear();

			if (External == null ||
				External.ResetCSG == null)
			{
				NativeMethodBindings.RegisterUnityMethods();
				NativeMethodBindings.RegisterExternalMethods();
			}

			ClearMeshInstances();

			if (RegisterAllComponents())
				External.ResetCSG();

		}
		#endregion
	}
}