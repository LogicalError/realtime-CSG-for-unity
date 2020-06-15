using UnityEngine;

public static class GameObjectExtensions
{
#if !UNITY_2019_2_OR_NEWER
    public static bool TryGetComponent<T>(this GameObject gameObject, out T component) where T : UnityEngine.Component
    {
        component = gameObject.GetComponent<T>();
        if (!component) { component = null; return false; }
        return true;
    }

    public static bool TryGetComponent<T>(this Component obj, out T component) where T : UnityEngine.Component
    {
        component = obj.GetComponent<T>();
        if (!component) { component = null; return false; }
        return true;
    }
#endif

    const string kUnableToDelete = "<unable to delete>";

    public static void Destroy(UnityEngine.Object obj)
    {
        if (!obj)
            return;

        UnityEngine.Object.DestroyImmediate(obj);
    }

    public static void Destroy(GameObject gameObject)
    {
        if (!gameObject)
            return;

        // Cannot destroy gameObjects when certain hideflags are set
        if (!TryDestroy(gameObject))
        {
            // Work-around for nested prefab instance issues ..
            if (gameObject.activeSelf &&
                gameObject.name != kUnableToDelete)
            {
                gameObject.hideFlags = HideFlags.DontSaveInBuild;
                gameObject.SetActive(false);
                SanitizeGameObject(gameObject);
                gameObject.name = kUnableToDelete;
            }
        }
    }

    public static void SanitizeGameObject(GameObject gameObject)
    {
        {
            var childComponents = gameObject.GetComponentsInChildren<MonoBehaviour>();
            foreach (var component in childComponents)
            {
                try { UnityEngine.Object.DestroyImmediate(component); } catch { }
            }
        }
        {
            var childComponents = gameObject.GetComponentsInChildren<Component>();
            foreach (var component in childComponents)
            {
                if (!(component is Transform))
                {
                    try { UnityEngine.Object.DestroyImmediate(component); } catch { }
                }
            }
        }
        {
            var transform = gameObject.transform;
            foreach (Transform childTransform in transform)
            {
                Destroy(childTransform.gameObject);
            }
        }
    }

    // Sometimes we're not allowed to destroy objects, so we try to destroy it and return false if we failed
    public static bool TryDestroy(GameObject gameObject)
    {
        if (!gameObject)
            return false;

        var prevHideFlags = gameObject.hideFlags;

        // Cannot destroy gameObjects when certain hideflags are set
        gameObject.hideFlags = HideFlags.None;
        try
        {
            UnityEngine.Object.DestroyImmediate(gameObject);
            return true;
        }
        catch
        {
            gameObject.hideFlags = prevHideFlags;
            return false;
        }
    }
}
