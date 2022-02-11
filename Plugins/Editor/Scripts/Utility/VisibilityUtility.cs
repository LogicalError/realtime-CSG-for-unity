using RealtimeCSG.Components;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RealtimeCSG
{
    public sealed class CSGVisibilityUtility
    {
        public static void SetGameObjectVisibility(GameObject gameObject, bool visible)
        {
#if UNITY_2019_2_OR_NEWER
            if (SceneVisibilityManager.instance.IsHidden(gameObject) == visible)
            {
                if (visible)
                    SceneVisibilityManager.instance.Show(gameObject, true);
                else
                    SceneVisibilityManager.instance.Hide(gameObject, true);
            }
#else
            // Unfortunately in older versions we have no concept of visibility, 
            // so we need to (de)activate the gameObject instead, which dirties the scene
            if (gameObject.activeSelf != visible)
			    gameObject.SetActive(visible);
#endif
        }

    }
}