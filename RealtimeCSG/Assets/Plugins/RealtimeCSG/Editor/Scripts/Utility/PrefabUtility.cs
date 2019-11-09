using UnityEditor;
using UnityEngine;

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
		/*
#if UNITY_2018_2_OR_NEWER
		return	PrefabUtility.GetCorrespondingObjectFromSource(gameObject) == null && 
				PrefabUtility.GetPrefabObject(gameObject) != null && 
				gameObject.transform.parent == null; // Is a prefab
#else
		return	PrefabUtility.GetPrefabParent(gameObject) == null && 
				PrefabUtility.GetPrefabObject(gameObject) != null && 
				gameObject.transform.parent == null; // Is a prefab
#endif
		*/
	}
	
	public static bool IsPrefabInstance(GameObject gameObject)
	{
#if UNITY_2018_3_OR_NEWER
		var prefabType = PrefabUtility.GetPrefabInstanceStatus(gameObject);
		return prefabType != PrefabInstanceStatus.NotAPrefab;
#else
		var prefabType = PrefabUtility.GetPrefabType(gameObject);
		if (prefabType == PrefabType.None)
			return false;

		return (prefabType != PrefabType.Prefab &&
				prefabType != PrefabType.ModelPrefab);
#endif
		/*
#if UNITY_2018_2_OR_NEWER
		return	PrefabUtility.GetCorrespondingObjectFromSource(gameObject) == null && 
				PrefabUtility.GetPrefabObject(gameObject) != null && 
				gameObject.transform.parent == null; // Is a prefab
#else
		return	PrefabUtility.GetPrefabParent(gameObject) == null && 
				PrefabUtility.GetPrefabObject(gameObject) != null && 
				gameObject.transform.parent == null; // Is a prefab
#endif
		*/
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
}