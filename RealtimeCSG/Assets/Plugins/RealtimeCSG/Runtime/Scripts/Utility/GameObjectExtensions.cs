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
}
