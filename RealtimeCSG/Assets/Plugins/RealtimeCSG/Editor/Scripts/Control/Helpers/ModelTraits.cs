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
                Debug.LogError(string.Format("Object is already owned by another prefab \"{0}\" \"{1}\"", assetPath, objPath));
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


        public static void ReplaceObjectInModel(CSGModel model, UnityEngine.Object oldObj, UnityEngine.Object newObj, bool skipAssetDatabaseUpdate = false)
        {
            if (!model ||
                oldObj == newObj)
                return;

            if (!CSGPrefabUtility.IsPrefab(model))
                return;

            if (!skipAssetDatabaseUpdate)
                AssetDatabase.StartAssetEditing(); // We might be modifying a prefab, in which case we need to store a mesh inside it
            try
            {
                var asset       = CSGPrefabUtility.GetPrefabAsset(model.gameObject);
                var assetPath   = AssetDatabase.GetAssetPath(asset);
#if UNITY_2018_3_OR_NEWER
                if (oldObj)
                { 
                    if (CanRemoveObjectFromAsset(assetPath, oldObj))
                        AssetDatabase.RemoveObjectFromAsset(oldObj);
                }
#endif
                if (newObj)
                {
                    if (CanAddObjectToAsset(assetPath, newObj))
                    {
                        AssetDatabase.AddObjectToAsset(newObj, asset);
                    }
                }
            }
            finally
            {
                if (!skipAssetDatabaseUpdate)
                    AssetDatabase.StopAssetEditing();
            }
        }


        public static void ReplaceObjectInModel(CSGModel model, Mesh oldMesh, Mesh newMesh, bool skipAssetDatabaseUpdate = false)
        {
            if (!model ||
                oldMesh == newMesh)
                return;

            if (!CSGPrefabUtility.IsPrefab(model))
                return;

            if (!skipAssetDatabaseUpdate)
                AssetDatabase.StartAssetEditing(); // We might be modifying a prefab, in which case we need to store a mesh inside it
            try
            {
                var asset       = CSGPrefabUtility.GetPrefabAsset(model.gameObject);
                var assetPath   = AssetDatabase.GetAssetPath(asset);
                if (string.IsNullOrEmpty(assetPath))
                    return;

#if UNITY_2018_3_OR_NEWER
                if (oldMesh)
                { 
                    if (CanRemoveObjectFromAsset(assetPath, oldMesh, ignoreWhenPartOfOtherAsset: true))
                        AssetDatabase.RemoveObjectFromAsset(oldMesh);
                }
#endif
                if (newMesh)
                {
                    if (IsObjectPartOfAnotherAsset(assetPath, newMesh))
                    {
                        // Copy the mesh
                        newMesh = newMesh.Clone();
                    }
                    if (CanAddObjectToAsset(assetPath, newMesh))
                    {
                        AssetDatabase.AddObjectToAsset(newMesh, asset);
                    }
                }
            }
            finally
            {
                if (!skipAssetDatabaseUpdate)
                    AssetDatabase.StopAssetEditing();
            }
        }


        public static void ReplaceObjectsInModel(CSGModel model, HashSet<Mesh> oldMeshes, HashSet<Mesh> newMeshes, bool skipAssetDatabaseUpdate = false)
        {
            if (!model)
                return;

            if (!CSGPrefabUtility.IsPrefab(model))
                return;

            if (!skipAssetDatabaseUpdate)
                AssetDatabase.StartAssetEditing(); // We might be modifying a prefab, in which case we need to store a mesh inside it
            try
            {
                var asset = CSGPrefabUtility.GetPrefabAsset(model.gameObject);
                var assetPath = AssetDatabase.GetAssetPath(asset);
                if (string.IsNullOrEmpty(assetPath))
                    return;

#if UNITY_2018_3_OR_NEWER
                foreach (var oldMesh in oldMeshes)
                {
                    if (CanRemoveObjectFromAsset(assetPath, oldMesh, ignoreWhenPartOfOtherAsset: true))
                        AssetDatabase.RemoveObjectFromAsset(oldMesh);
                }
#endif

                foreach (var _newMesh in newMeshes)
                {
                    var newMesh = _newMesh;
                    if (IsObjectPartOfAnotherAsset(assetPath, newMesh))
                    {
                        // Copy the mesh
                        newMesh = newMesh.Clone();
                    }
                    if (CanAddObjectToAsset(assetPath, newMesh))
                        AssetDatabase.AddObjectToAsset(newMesh, asset);
                }
            }
            finally
            {
                if (!skipAssetDatabaseUpdate)
                    AssetDatabase.StopAssetEditing();
            }
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

        public static void OnPrefabInstanceUpdated(CSGModel[] models)
        {
            // TODO: somehow add meshes to prefab instances
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
