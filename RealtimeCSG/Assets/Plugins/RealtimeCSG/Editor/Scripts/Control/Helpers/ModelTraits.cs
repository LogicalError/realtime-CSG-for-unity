using InternalRealtimeCSG;
using UnityEditor;
using RealtimeCSG.Components;
using UnityEngine;
using UnityEditor.SceneManagement;
using System;
using System.Collections.Generic;

namespace RealtimeCSG
{
	internal static class ModelTraits
    {
        static bool IsObjectPartOfAsset(string assetPath, UnityEngine.Object obj)
        {
            if (!obj)
                return false;

            var objPath = AssetDatabase.GetAssetPath(obj);
            return assetPath == objPath;
        }

        static bool IsObjectPartOfAnotherAsset(string assetPath, UnityEngine.Object obj)
        {
            if (!obj ||
                string.IsNullOrEmpty(assetPath))
                return false;
            
            var objPath = AssetDatabase.GetAssetPath(obj);
            if (assetPath == objPath)
                return false;

            if (!string.IsNullOrEmpty(objPath) &&
                assetPath != objPath)
                return true;
            return false;
        }

        static bool CanAddObjectToAsset(string assetPath, UnityEngine.Object obj)
        {
            if (!obj ||
                string.IsNullOrEmpty(assetPath))
                return false;

            var objPath = AssetDatabase.GetAssetPath(obj);
            if (assetPath == objPath)
                return false;

            if (!string.IsNullOrEmpty(objPath) &&
                assetPath != objPath)
            {
                //Debug.LogError(string.Format("Object is already owned by another prefab \"{0}\" \"{1}\"", assetPath, objPath));
                return false;
            }
            return true;
        }

        static bool CanRemoveObjectFromAsset(string assetPath, UnityEngine.Object obj, bool ignoreWhenPartOfOtherAsset = false)
        {
#if UNITY_2018_3_OR_NEWER
            if (!obj)
                return false;

            var objPath = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(objPath))
            {
                if (!ignoreWhenPartOfOtherAsset)
                    Debug.LogError("Cannot remove object from prefab because it is not owned by any asset");
                return false;
            }

            if (objPath != assetPath)
            {
                if (!ignoreWhenPartOfOtherAsset)
                    Debug.LogError("Trying to remove asset that is owned by another prefab");
                return false;
            }
#endif
            return true;
        }
        /*
        public static void AddObjectToModel(CSGModel model, UnityEngine.Object obj)
        {
            if (!model || !obj)
                return;

            if (!CSGPrefabUtility.IsPrefab(model))
                return;

            var asset       = CSGPrefabUtility.GetPrefabAsset(model.gameObject);
            var assetPath   = AssetDatabase.GetAssetPath(asset);
            if (!CanAddObjectToAsset(assetPath, obj))
                return;

            AssetDatabase.AddObjectToAsset(obj, asset);
        }

        public static void AddObjectsToModel(CSGModel model, UnityEngine.Object[] objs)
        {
            if (!model)
                return;

            if (!CSGPrefabUtility.IsPrefab(model))
                return;
            
            var asset       = CSGPrefabUtility.GetPrefabAsset(model.gameObject);
            var assetPath   = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(assetPath))
                return;

            foreach (var obj in objs)
            {
                if (!CanAddObjectToAsset(assetPath, obj))
                    continue;

                AssetDatabase.AddObjectToAsset(obj, asset);
            }
        }

        public static void RemoveObjectFromModel(CSGModel model, UnityEngine.Object obj)
        {
#if UNITY_2018_3_OR_NEWER
            if (!model || !obj)
                return;

            if (!CSGPrefabUtility.IsPrefab(model))
                return;

            var asset       = CSGPrefabUtility.GetPrefabAsset(model.gameObject);
            var assetPath   = AssetDatabase.GetAssetPath(asset);
            if (!CanRemoveObjectFromAsset(assetPath, obj))
                return;

            AssetDatabase.RemoveObjectFromAsset(obj);
#endif
        }
        */

        public static void RemoveObjectsFromModel(CSGModel model, UnityEngine.Object[] objs)
        {
#if UNITY_2018_3_OR_NEWER
            if (!model)
                return;

            if (!CSGPrefabUtility.IsPrefab(model))
                return;

            var asset       = CSGPrefabUtility.GetPrefabAsset(model.gameObject);
            var assetPath   = AssetDatabase.GetAssetPath(asset);
            foreach (var obj in objs)
            {
                if (!obj)
                    continue;

                if (!CanRemoveObjectFromAsset(assetPath, obj))
                    continue;

                AssetDatabase.RemoveObjectFromAsset(obj);
            }
#endif
        }


#if UNITY_2018_3_OR_NEWER
        static Mesh CloneMesh(Mesh prevMesh, List<GeneratedMeshInstance> foundGeneratedMeshInstances, List<HelperSurfaceDescription> foundHelperSurfaces)
        {
            var newMesh = prevMesh.Clone();
            foreach (var generatedMeshInstance in foundGeneratedMeshInstances)
            {
                if (!generatedMeshInstance)
                    continue;
                if (generatedMeshInstance.SharedMesh == prevMesh)
                    generatedMeshInstance.SharedMesh = newMesh;
                if (generatedMeshInstance.TryGetComponent(out MeshFilter meshFilter))
                {
                    if (meshFilter.sharedMesh == prevMesh)
                        meshFilter.sharedMesh = newMesh;
                }
                if (generatedMeshInstance.TryGetComponent(out MeshCollider meshCollider))
                {
                    if (meshCollider.sharedMesh == prevMesh)
                        meshCollider.sharedMesh = newMesh;
                }
            }

            foreach (var helperSurface in foundHelperSurfaces)
            {
                if (helperSurface.SharedMesh == prevMesh)
                    helperSurface.SharedMesh = newMesh;
            }
            return newMesh;
        }

        static HashSet<Mesh> oldMeshes = new HashSet<Mesh>();
        static HashSet<Mesh> newMeshes = new HashSet<Mesh>();
        static List<GeneratedMeshInstance>      foundGeneratedMeshInstances = new List<GeneratedMeshInstance>();
        static List<HelperSurfaceDescription>   foundHelperSurfaces         = new List<HelperSurfaceDescription>();
        static HashSet<string>                  foundNestedPrefabs          = new HashSet<string>();
        

        public static void OnPrefabSaving(GameObject obj)
        {
            var foundGeneratedMeshes = obj.GetComponentsInChildren<GeneratedMeshes>();
            if (foundGeneratedMeshes.Length == 0)
                return;

            var defaultModel = InternalCSGModelManager.GetDefaultCSGModelForObject(obj.transform);
            newMeshes.Clear();
            foreach (var generatedMeshesInstance in foundGeneratedMeshes)
            {
                if (generatedMeshesInstance.owner == defaultModel)
                    continue;

                foreach (var generatedMeshInstance in generatedMeshesInstance.GetComponentsInChildren<GeneratedMeshInstance>())
                {
                    if (!generatedMeshInstance) // possible when it's deleted in a prefab
                        continue;
                    foundGeneratedMeshInstances.Add(generatedMeshInstance);
                    if (generatedMeshInstance.SharedMesh)
                        newMeshes.Add(generatedMeshInstance.SharedMesh);
                }

                foreach (var helperSurface in generatedMeshesInstance.HelperSurfaces)
                {
                    foundHelperSurfaces.Add(helperSurface);
                    newMeshes.Add(helperSurface.SharedMesh);
                }
            }

            var asset       = CSGPrefabUtility.GetPrefabAsset(obj);
            var assetPath   = AssetDatabase.GetAssetPath(asset);

            var assetObjects = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (var assetObject in assetObjects)
            {
                var mesh = assetObject as Mesh;
                if (!mesh)
                    continue;
                oldMeshes.Add(mesh);
            }

            if (newMeshes.Count == 0 && oldMeshes.Count == 0)
                return;

            // We might be modifying a prefab, in which case we need to store meshes inside it that belong to it
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var oldMesh in oldMeshes)
                {
                    if (newMeshes.Contains(oldMesh))
                        continue;

                    if (CanRemoveObjectFromAsset(assetPath, oldMesh, ignoreWhenPartOfOtherAsset: true))
                        AssetDatabase.RemoveObjectFromAsset(oldMesh);
                }

                foreach (var _newMesh in newMeshes)
                {
                    var newMesh = _newMesh;
                    if (oldMeshes.Contains(newMesh))
                    {
                        if (IsObjectPartOfAsset(assetPath, newMesh))
                            continue;
                    }
                    if (CanAddObjectToAsset(assetPath, newMesh))
                        AssetDatabase.AddObjectToAsset(newMesh, asset);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }
#endif
        public static bool IsDefaultModel(CSGModel model)
        {
            if ((model.hideFlags & MeshInstanceManager.ComponentHideFlags) == MeshInstanceManager.ComponentHideFlags ||
                (model.hideFlags & HideFlags.DontSave) == HideFlags.DontSave ||
                (model.hideFlags & HideFlags.DontSaveInEditor) == HideFlags.DontSaveInEditor)
                return true;
            return false;
        }

        public static bool IsModelEditable(CSGModel model)
        {
            if (!model)
                return false;

#if UNITY_2018_3_OR_NEWER && UNITY_EDITOR
            if (CSGPrefabUtility.AreInPrefabMode())
            {
                // Nested prefabs do not play nice with editing in scene, so it's best to edit them in prefab mode themselves
                // We only allow editing of nested prefabs when in prefab mode
                return CSGPrefabUtility.IsEditedInPrefabMode(model);
            }
#endif
            return model.isActiveAndEnabled;
        }

        public static bool IsModelSelectable(CSGModel model)
        {
            if (!model || !model.isActiveAndEnabled)
                return false;
            if (((1 << model.gameObject.layer) & Tools.visibleLayers) == 0)
                return false;
            return true;
        }

        public static bool WillModelRender(CSGModel model)
        {
            // Is our model valid ...?
            if (!IsModelEditable(model))
                return false;

#if UNITY_2018_3_OR_NEWER
            if (CSGPrefabUtility.AreInPrefabMode())
            {
                if (!CSGPrefabUtility.IsEditedInPrefabMode(model))
                    return false;
			}
#endif
			// Does our model have a meshRenderer?
			if (model.IsRenderable)
			{
				// If so, is it shadow-only?
//				if (model.ShadowsOnly)
//				{
					// .. and do we need to show shadow-only surfaces?
//					return CSGSettings.ShowCastShadowsSurfaces;
//				}

				// Otherwise, it is always rendering (with the exception of manually hidden surfaces)
				return true;
			}

			// Is it a trigger and are we showing triggers?
			if (model.IsTrigger && CSGSettings.ShowTriggerSurfaces)
				return true;

			// Check if it's a collider and are we showing colliders?
			if (model.HaveCollider && CSGSettings.ShowColliderSurfaces)
				return true;

			// Otherwise see if we're showing surfaces culled by the CSG process ...
			return CSGSettings.ShowCulledSurfaces;
		}

		public static RenderSurfaceType GetModelSurfaceType(CSGModel model)
		{
			if (model.IsRenderable) // if it's renderable then it's already being rendered using a MeshRenderer
			{
				// except if it's a shadow only surface, it has a MeshRenderer, but is not usually visible ...
//				if (model.ShadowsOnly)
//					return RenderSurfaceType.ShadowOnly;

				return RenderSurfaceType.Normal;
			}

			if (model.IsTrigger)
				return RenderSurfaceType.Trigger;

			if (model.HaveCollider)
				return RenderSurfaceType.Collider;

			return RenderSurfaceType.Culled;
		}

		public static bool NeedsRigidBody(CSGModel model)
        {
            if (!IsModelEditable(model))
                return false;

            var collidable			= (model.Settings & ModelSettingsFlags.NoCollider) != ModelSettingsFlags.NoCollider;
			var isTrigger			= collidable && (model.Settings & ModelSettingsFlags.IsTrigger) == ModelSettingsFlags.IsTrigger;
			var ownerStaticFlags	= GameObjectUtility.GetStaticEditorFlags(model.gameObject);
			var batchingstatic		= (ownerStaticFlags & StaticEditorFlags.BatchingStatic) == StaticEditorFlags.BatchingStatic;

			return (batchingstatic || collidable) && !isTrigger;
		}

		public static bool NeedsStaticRigidBody(CSGModel model)
        {
            if (!IsModelEditable(model))
                return false;

            var ownerStaticFlags = GameObjectUtility.GetStaticEditorFlags(model.gameObject);
			var batchingstatic = (ownerStaticFlags & StaticEditorFlags.BatchingStatic) == StaticEditorFlags.BatchingStatic;
			return batchingstatic;
		}
	}
}
