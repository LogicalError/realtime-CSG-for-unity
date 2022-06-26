using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.IO;

namespace RealtimeCSG
{
    public class CSGProjectSettings : ScriptableObject
    {
        const string ASSET_PATH = "ProjectSettings/CSGProjectSettings.asset";

        private static CSGProjectSettings s_Instance;
        public static CSGProjectSettings Instance => s_Instance == null ? CreateOrLoad() : s_Instance;

        public bool SaveMeshesInSceneFiles = true;
        public bool SnapEverythingTo0001Grid = true;

        CSGProjectSettings()
        {
            s_Instance = this;
        }

        private static CSGProjectSettings CreateOrLoad()
        {
            InternalEditorUtility.LoadSerializedFileAndForget(ASSET_PATH);

            if (s_Instance == null)
            {
                var created = CreateInstance<CSGProjectSettings>();
                created.hideFlags = HideFlags.HideAndDontSave;
            }

            System.Diagnostics.Debug.Assert(s_Instance != null);
            return s_Instance;
        }

        public void Save()
        {
            if (s_Instance == null)
            {
                Debug.Log("Cannot save ScriptableSingleton: no instance!");
                return;
            }

            string folderPath = Path.GetDirectoryName(ASSET_PATH);
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);
            InternalEditorUtility.SaveToSerializedFileAndForget(new[] { s_Instance }, ASSET_PATH, allowTextSerialization: true);
        }
    }
}
