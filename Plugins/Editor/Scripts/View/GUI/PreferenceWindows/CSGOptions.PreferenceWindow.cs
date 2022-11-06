using RealtimeCSG.Legacy;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RealtimeCSG
{
    internal class CSGOptionsPreferenceWindow
    {
#if !UNITY_2018_4_OR_NEWER && !UNITY_2019_1_OR_NEWER
        [PreferenceItem("CSG Options")]
#endif
        public static void PreferenceWindow()
        {
            EditorGUI.BeginChangeCheck();
            {
#if !UNITY_2018_4_OR_NEWER && !UNITY_2019_1_OR_NEWER
                CSGProjectSettings.Instance.SaveMeshesInSceneFiles = EditorGUILayout.ToggleLeft("Save Meshes In Scene Files", CSGProjectSettings.Instance.SaveMeshesInSceneFiles);
                EditorGUILayout.Separator();
#endif
                CSGSettings.ShowTooltips = EditorGUILayout.ToggleLeft("Show Tool-Tips", CSGSettings.ShowTooltips);
                CSGSettings.SnapNonCSGObjects = EditorGUILayout.ToggleLeft("Snap Non-CSG Objects to the grid", CSGSettings.SnapNonCSGObjects);
                CSGSettings.AutoRigidbody = EditorGUILayout.ToggleLeft("Disable auto add rigidbodies", CSGSettings.AutoRigidbody);
                CSGSettings.DefaultPreserveUVs = EditorGUILayout.ToggleLeft("Preserve UVs (Default)", CSGSettings.DefaultPreserveUVs);
                EditorGUILayout.Space();
                CSGSettings.MaxCircleSides = EditorGUILayout.IntField("Max Circle Sides", CSGSettings.MaxCircleSides);
                CSGSettings.MaxSphereSplits = EditorGUILayout.IntField("Max Sphere Splits", CSGSettings.MaxSphereSplits);
                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Surfaces", EditorStyles.boldLabel);
                var beforeToggleWorldSpaceTexture = (CSGSettings.DefaultTexGenFlags & TexGenFlags.WorldSpaceTexture) != TexGenFlags.WorldSpaceTexture;
                var afterToggleWorldSpaceTexture = EditorGUILayout.ToggleLeft("Lock Texture To Object (Default)", beforeToggleWorldSpaceTexture);
                if (afterToggleWorldSpaceTexture != beforeToggleWorldSpaceTexture)
                {
                    if (afterToggleWorldSpaceTexture)
                        CSGSettings.DefaultTexGenFlags &= ~TexGenFlags.WorldSpaceTexture;
                    else
                        CSGSettings.DefaultTexGenFlags |= TexGenFlags.WorldSpaceTexture;
                }

                CSGSettings.ShowSceneInfo = EditorGUILayout.ToggleLeft("Show Scene Info", CSGSettings.ShowSceneInfo);
            }
            if (EditorGUI.EndChangeCheck())
            {
#if !UNITY_2018_4_OR_NEWER && !UNITY_2019_1_OR_NEWER
                CSGProjectSettings.Instance.Save();
#endif
                CSGSettings.Save();
            }
        }

        public static void ProjectSettingsWindow()
        {
            EditorGUI.BeginChangeCheck();
            {
                CSGProjectSettings.Instance.SaveMeshesInSceneFiles = !EditorGUILayout.ToggleLeft("[Experimental] Don't Save Meshes In Scene Files", !CSGProjectSettings.Instance.SaveMeshesInSceneFiles);

                if (!CSGProjectSettings.Instance.SaveMeshesInSceneFiles)
                {
                    EditorGUILayout.HelpBox(
                        "Meshes will be rebuilt on editor & scene load. This is an experimental option, if meshes are not rebuilding properly, please disable it.",
                        MessageType.Warning, true);
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "If enabled, meshes will be automatically rebuilt on editor & scene load, dramatically saving disk space.",
                        MessageType.Info, true);
                }

                CSGProjectSettings.Instance.SnapEverythingTo0001Grid = EditorGUILayout.ToggleLeft("Snap All CSG Objects To 0.001 Grid", CSGProjectSettings.Instance.SnapEverythingTo0001Grid);
                EditorGUILayout.HelpBox(
                    "If enabled, every CSG object will snap to a small grid regardless of snapping options. " +
                    "This ensures things line up when on a tiny scale, but can make it difficult to work at a small scale.",
                    MessageType.Info, true);
            }

            if (EditorGUI.EndChangeCheck())
            {
                CSGProjectSettings.Instance.Save();
            }
        }
    }

#if UNITY_2018_4_OR_NEWER
    // Register a SettingsProvider using IMGUI for the drawing framework:
    static class RealtimeCSGOptionsIMGUIRegister
    {
        [SettingsProvider]
        public static SettingsProvider CreateCSGPreferenceSettings()
        {
            var keys = CSGKeysPreferenceWindow.GetKeys();
            var keywords = new HashSet<string>();
            foreach (var key in keys)
            {
                var pieces = key.name.Split(' ', '/');
                foreach (var piece in pieces)
                    keywords.Add(piece);
            }

            var provider = new SettingsProvider("Preferences/RealtimeCSG", SettingsScope.User)
            {
                label = "Realtime CSG",

                guiHandler = (searchContext) =>
                {
                    GUILayout.Label("Options", EditorStyles.boldLabel);
                    GUILayout.Space(10);
                    CSGOptionsPreferenceWindow.PreferenceWindow();
                    EditorGUILayout.Separator();
                    GUILayout.Label("Keyboard settings", EditorStyles.boldLabel);
                    GUILayout.Space(10);
                    CSGKeysPreferenceWindow.PreferenceWindow();
                },

                keywords = new[] { "Tool-Tip", "ToolTip", "Tool-Tips", "ToolTips", "Snap", "Circle", "Sphere", "Lock", "Texture", "Surface" }
            };

            return provider;
        }

        [SettingsProvider]
        public static SettingsProvider CreateCSGProjectSettings()
        {
            var provider = new SettingsProvider("Project/RealtimeCSGProjectSettings", SettingsScope.Project)
            {
                label = "Realtime CSG",
                guiHandler = (searchContext) =>
                {
                    GUILayout.Label("Options", EditorStyles.boldLabel);
                    GUILayout.Space(10);
                    CSGOptionsPreferenceWindow.ProjectSettingsWindow();
                },

                keywords = new[] { "Meshes" }
            };

            return provider;
        }
    }
#endif
}
