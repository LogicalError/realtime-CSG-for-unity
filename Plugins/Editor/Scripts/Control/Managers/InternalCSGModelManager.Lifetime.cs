using UnityEngine;
using UnityEditor;
using InternalRealtimeCSG;

namespace RealtimeCSG
{
	internal partial class InternalCSGModelManager
	{
		private static bool				_isInitialized	= false;
		private static readonly object	_lockObj		= new object();

		internal static NativeMethods External;

		#region Clear
#if UNITY_2019_4_OR_NEWER
        [RuntimeInitializeOnLoadMethod( RuntimeInitializeLoadType.SubsystemRegistration )]        
#endif
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
		internal static bool IgnoreMaterials = false;
		public static void UndoRedoPerformed()
		{
			BrushOutlineManager.ClearOutlines();
			
			CheckForChanges(forceHierarchyUpdate: true);

			if (!IgnoreMaterials)
			{
				foreach (var brush in Brushes)
				{
					try
					{
						//brush.EnsureInitialized();
						if (brush.Shape != null)
							ShapeUtility.CheckMaterials(brush.Shape);
					}
					finally { }
				}
				foreach (var brush in Brushes)
				{
					try
					{
						InternalCSGModelManager.CheckSurfaceModifications(brush, true);
						//InternalCSGModelManager.ValidateBrush(brush);
					}
					finally { }
				}
			}
		}
		#endregion
		
		#region CheckForChanges
		public static bool skipCheckForChanges = false;
		public static void CheckForChanges(bool forceHierarchyUpdate = false)
		{
			if (RealtimeCSG.CSGModelManager.IsInPlayMode)
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

#if UNITY_EDITOR

		[UnityEditor.InitializeOnEnterPlayMode]
		public static void OnEnterPlayMode()
		{
			// If saving meshes to scene files, we don't need to dynamically rebuild on scene changes
			if (CSGProjectSettings.Instance.SaveMeshesInSceneFiles)
				return;

			if (!ensureExternalMethodsPopulated())
				return;

			EditorApplication.playModeStateChanged += onPlayModeChange;
			UnityEngine.SceneManagement.SceneManager.sceneLoaded += sceneLoaded;
		}

		static bool ensureExternalMethodsPopulated()
		{
			if (External == null ||
			    External.ResetCSG == null)
			{
				NativeMethodBindings.RegisterUnityMethods();
				NativeMethodBindings.RegisterExternalMethods();
			}

			if (External == null)
			{
				Debug.LogError("RealtimeCSG: Cannot rebuild meshes for some reason. External modules not loaded. Please save meshes into the Scene.");
				return false;
			}

			return true;
		}

		static void rebuildMeshes()
		{
			if (!ensureExternalMethodsPopulated())
				return;

			RealtimeCSG.CSGModelManager.AllowInEditorPlayMode = true;
			InternalCSGModelManager.Shutdown();
			DoForcedMeshUpdate();
			InternalCSGModelManager.CheckForChanges(false);
			RealtimeCSG.CSGModelManager.AllowInEditorPlayMode = false;
		}

		static void sceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
		{
			rebuildMeshes();
		}

		static void onPlayModeChange(PlayModeStateChange playMode)
		{
			if (playMode == PlayModeStateChange.EnteredEditMode)
			{
				UnityEngine.SceneManagement.SceneManager.sceneLoaded -= sceneLoaded;
				EditorApplication.playModeStateChanged -= onPlayModeChange;

				rebuildMeshes();
			}
		}
#endif
	}
}