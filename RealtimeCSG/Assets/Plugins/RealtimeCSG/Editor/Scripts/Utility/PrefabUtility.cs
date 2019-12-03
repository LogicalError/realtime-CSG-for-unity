using System;
using RealtimeCSG.Components;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RealtimeCSG
{
    public sealed class CSGPrefabUtility
    {
	    public static UnityEngine.Object GetCorrespondingObjectFromSource(UnityEngine.Object source)
	    {
    #if UNITY_2018_2_OR_NEWER
		    return PrefabUtility.GetCorrespondingObjectFromSource(source);
    #else
		    return PrefabUtility.GetPrefabParent(source);
    #endif
        }

        public static bool IsPrefab(CSGModel model)
        {
            if (!model)
                return false;

            var parent = CSGPrefabUtility.GetPrefabAsset(model.gameObject);
            if (parent)
                return true;

            return false;
        }

        public static bool AreInPrefabMode()
        {
    #if UNITY_2018_3_OR_NEWER && UNITY_EDITOR
            var mainStage = StageUtility.GetMainStageHandle();
            var currentStageHandle = StageUtility.GetCurrentStageHandle();
            if (mainStage != currentStageHandle)
                return true;
    #endif
            return false;
        }

        public static bool IsEditedInPrefabMode(CSGModel model)
        {
    #if UNITY_2018_3_OR_NEWER && UNITY_EDITOR
            if (!model)
                return false;

            if (!AreInPrefabMode())
                return false;

            var currentStageHandle = StageUtility.GetCurrentStageHandle();
            if (currentStageHandle.Contains(model.gameObject))
                return true;
    #endif
            return false;
        }

        public static CSGNode[] GetNodesInPrefabMode()
        {
    #if UNITY_2018_3_OR_NEWER && UNITY_EDITOR
            var mainStage = StageUtility.GetMainStageHandle();
            var currentStageHandle = StageUtility.GetCurrentStageHandle();
            if (mainStage != currentStageHandle)
                return currentStageHandle.FindComponentsOfType<CSGNode>();
    #endif
            return null;
        }

        public static bool IsPrefabAssetOrInstance(GameObject gameObject)
	    {
    #if UNITY_2018_3_OR_NEWER
		    var prefabAssetType		= PrefabUtility.GetPrefabAssetType(gameObject);
		    var prefabInstanceType	= PrefabUtility.GetPrefabInstanceStatus(gameObject);
		    if (prefabAssetType == PrefabAssetType.NotAPrefab &&
			    prefabInstanceType == PrefabInstanceStatus.NotAPrefab)
			    return false;
		    return true;
    #else
		    var prefabType = PrefabUtility.GetPrefabType(gameObject);
		    if (prefabType == PrefabType.None)
			    return false;

		    return true;
    #endif
	    }
	
	    public static bool IsPrefabAsset(GameObject gameObject)
	    {
    #if UNITY_2018_3_OR_NEWER
		    var prefabInstanceType = PrefabUtility.GetPrefabInstanceStatus(gameObject);
		    if (prefabInstanceType != PrefabInstanceStatus.NotAPrefab) 
			    return false;
		     
		    var prefabType = PrefabUtility.GetPrefabAssetType(gameObject);
		    return	prefabType != PrefabAssetType.NotAPrefab &&
				    prefabType != PrefabAssetType.Model;
    #else
		    var prefabType = PrefabUtility.GetPrefabType(gameObject);
		    if (prefabType == PrefabType.None)
			    return false;

		    return (prefabType == PrefabType.Prefab ||
				    prefabType == PrefabType.ModelPrefab);
    #endif
	    }
	
	    public static bool IsPrefabInstance(GameObject gameObject)
	    {
    #if UNITY_2018_3_OR_NEWER
            if (!PrefabUtility.IsPartOfAnyPrefab(gameObject))
                return false;
            var prefabType = PrefabUtility.GetPrefabInstanceStatus(gameObject);
		    return prefabType != PrefabInstanceStatus.NotAPrefab;
    #else
		    var prefabType = PrefabUtility.GetPrefabType(gameObject);
		    if (prefabType == PrefabType.None)
			    return false;

		    return (prefabType != PrefabType.Prefab &&
				    prefabType != PrefabType.ModelPrefab);
    #endif
	    }

        public static GameObject GetPrefabAsset(GameObject gameObject)
        {
    #if UNITY_2018_3_OR_NEWER
            if (AreInPrefabMode())
            {
                var prefabStage = UnityEditor.Experimental.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
                if (prefabStage.IsPartOfPrefabContents(gameObject))
                    return (GameObject)AssetDatabase.LoadMainAssetAtPath(prefabStage.prefabAssetPath);
            }
    #endif

            if (!IsPrefabInstance(gameObject))
                return null;

    #if UNITY_2018_3_OR_NEWER
            return (GameObject)AssetDatabase.LoadMainAssetAtPath(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(gameObject));
    #else
		    return (GameObject)PrefabUtility.GetPrefabParent(gameObject);
    #endif
        }

        public static GameObject GetOutermostPrefabInstanceRoot(GameObject gameObject)
	    {
		    if (!IsPrefabInstance(gameObject))
			    return null;
    #if UNITY_2018_3_OR_NEWER
		    return PrefabUtility.GetOutermostPrefabInstanceRoot(gameObject);
    #else
		    return PrefabUtility.FindPrefabRoot(gameObject);
    #endif
	    }

	    public static GameObject Instantiate(GameObject originalGameObject, bool copy = false)
	    {
    #if UNITY_2018_3_OR_NEWER
		    var prefabInstanceType	= PrefabUtility.GetPrefabInstanceStatus(originalGameObject);
		    if (prefabInstanceType != PrefabInstanceStatus.NotAPrefab && !copy)
		    {
			    var corrObj = GetCorrespondingObjectFromSource(originalGameObject);
			    var obj = PrefabUtility.InstantiatePrefab(corrObj);
			    var result = obj as GameObject;
			    return result;
		    }

		    var prefabAssetType		= PrefabUtility.GetPrefabAssetType(originalGameObject);
		    if (prefabAssetType != PrefabAssetType.NotAPrefab && !copy)
		    {
			    var obj = PrefabUtility.InstantiatePrefab(originalGameObject);
			    var result = obj as GameObject;
			    return result;
		    }

		    var inst = UnityEngine.Object.Instantiate<GameObject>(originalGameObject);
		    return inst;
    #else
		    var prefabType = PrefabUtility.GetPrefabType(originalGameObject);
		    if (prefabType == PrefabType.None || copy)
			    return UnityEngine.Object.Instantiate<GameObject>(originalGameObject);
		    else
		    if (prefabType == PrefabType.Prefab ||
			    prefabType == PrefabType.ModelPrefab)
			    // PrefabAsset
			    return PrefabUtility.InstantiatePrefab(originalGameObject) as GameObject;
		    else
			    // PrefabInstance
			    return PrefabUtility.InstantiatePrefab(GetCorrespondingObjectFromSource(originalGameObject)) as GameObject;
    #endif
	    }

        internal static bool IsPartOfAsset(Mesh sharedMesh)
        {
            return !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(sharedMesh));
        }
    }
}