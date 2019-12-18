using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEditor;
using InternalRealtimeCSG;
using RealtimeCSG.Components;

namespace RealtimeCSG
{
	internal partial class InternalCSGModelManager
	{
		class CSGSceneState
		{
			public GameObject	DefaultModelObject	= null;
			public CSGModel		DefaultModel		= null;
		}

		internal const string	DefaultModelName		= "[default-CSGModel]";
		internal const string   DefaultPrefabModelName	= "[prefab-CSGModel]";
		private static readonly Dictionary<Scene, CSGSceneState> SceneStates = new Dictionary<Scene, CSGSceneState>();

		#region DefaultCSGModel
		internal static CSGModel GetDefaultCSGModelForObject(Transform transform)
		{
			Scene currentScene;
			if (!transform || !transform.gameObject)
			{
				currentScene = SceneManager.GetActiveScene();
			} else
				currentScene = transform.gameObject.scene;
			
			return InternalCSGModelManager.GetDefaultCSGModel(currentScene);
		}

		internal static CSGModel GetDefaultCSGModelForObject(MonoBehaviour component)
		{
			Scene currentScene;
			if (!component || !component.gameObject)
			{
				currentScene = SceneManager.GetActiveScene();
			} else
				currentScene = component.gameObject.scene;
			
			return InternalCSGModelManager.GetDefaultCSGModel(currentScene);
		}

		internal static CSGModel GetDefaultCSGModel(Scene scene)
		{
			if (!scene.IsValid() ||
				!scene.isLoaded)
				return null;

			// make sure we're properly initialized (just in case)
			if (!SceneStates.ContainsKey(scene))
				SceneStates[scene] = new CSGSceneState();
			if (!SceneStates[scene].DefaultModel)
				InitializeDefaultCSGModel(scene, SceneStates[scene], createIfNoExists: true);

			return SceneStates[scene].DefaultModel;
		}
		#endregion

		
		#region InitializeDefaultCSGModel
		static void InitializeDefaultCSGModel(bool createIfNoExists = true)
		{
			var loadedScenes = new HashSet<Scene>();
			for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
			{
				var currentScene = SceneManager.GetSceneAt(sceneIndex);
				if (!currentScene.IsValid() ||
					!currentScene.isLoaded)
					continue;
				var sceneState = SceneStates[currentScene];
				InitializeDefaultCSGModel(currentScene, sceneState, createIfNoExists);
			}
			var usedScenes = SceneStates.Keys.ToArray();
			foreach(var usedScene in usedScenes)
			{
				if (!loadedScenes.Contains(usedScene))
					SceneStates.Remove(usedScene);
			}
		}

		static void InitializeDefaultCSGModel(Scene currentScene, CSGSceneState sceneState, bool createIfNoExists = true)
		{
			bool inPrefabMode = false;
			Transform prefabRootTransform = null;
#if UNITY_2018_3_OR_NEWER
			var currentPrefabStage = UnityEditor.Experimental.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
			if (currentPrefabStage != null)
			{
				var prefabRoot = currentPrefabStage.prefabContentsRoot;
				prefabRootTransform = prefabRoot.transform;
				inPrefabMode = (prefabRoot.scene == currentScene);
			}
#endif
			var defaultModelName = inPrefabMode ? DefaultPrefabModelName : DefaultModelName;

			// make sure we have a default CSG model
			sceneState.DefaultModelObject = SceneQueryUtility.GetUniqueHiddenGameObjectInSceneWithName(currentScene, defaultModelName);
			if (createIfNoExists)
			{
				if (!sceneState.DefaultModelObject)
				{
					sceneState.DefaultModelObject	= new GameObject(defaultModelName);
					sceneState.DefaultModel			= CreateCSGModel(sceneState.DefaultModelObject);
				} else
				{
					sceneState.DefaultModel			= sceneState.DefaultModelObject.GetComponent<CSGModel>();
					if (!sceneState.DefaultModel)
						sceneState.DefaultModel		= CreateCSGModel(sceneState.DefaultModelObject);
				}

				// make sure it's transformation has sensible values
				var transform = sceneState.DefaultModel.transform;
				if (inPrefabMode)
				{
					transform.SetParent(prefabRootTransform, false);
				} else
					transform.parent = null;

				transform.localPosition = MathConstants.zeroVector3;
				transform.localScale	= MathConstants.oneVector3;
				transform.localRotation = Quaternion.identity;

				var defaultFlags = //StaticEditorFlags.LightmapStatic |
									StaticEditorFlags.BatchingStatic |
									StaticEditorFlags.NavigationStatic |
									StaticEditorFlags.OccludeeStatic |
									StaticEditorFlags.OffMeshLinkGeneration |
									StaticEditorFlags.ReflectionProbeStatic;
				GameObjectUtility.SetStaticEditorFlags(sceneState.DefaultModel.gameObject, defaultFlags);

				RegisterModel(sceneState.DefaultModel);
			}

			// do not give default CSG model any extra abilities other than rendering
			//  since an invisible object doesn't work well with navmesh/colliders etc.
			// force the users to use a CSG-model, but gracefully let it still be 
			//  visible for objects outside of it.
			if (inPrefabMode)
				sceneState.DefaultModelObject.hideFlags = MeshInstanceManager.ComponentHideFlags | HideFlags.DontSave;
			else
				sceneState.DefaultModelObject.hideFlags = MeshInstanceManager.ComponentHideFlags;
		}
		#endregion
	   

	}
}