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
                CSGSettings.ShowTooltips		= EditorGUILayout.ToggleLeft("Show Tool-Tips",						CSGSettings.ShowTooltips);
                CSGSettings.SnapNonCSGObjects	= EditorGUILayout.ToggleLeft("Snap Non-CSG Objects to the grid",	CSGSettings.SnapNonCSGObjects);
                CSGSettings.DefaultPreserveUVs  = EditorGUILayout.ToggleLeft("Preserve UVs (Default)",              CSGSettings.DefaultPreserveUVs);
                EditorGUILayout.Space();
                CSGSettings.MaxCircleSides		= EditorGUILayout.IntField("Max Circle Sides",  CSGSettings.MaxCircleSides);
                CSGSettings.MaxSphereSplits		= EditorGUILayout.IntField("Max Sphere Splits", CSGSettings.MaxSphereSplits);
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

                CSGSettings.ShowSceneInfo = EditorGUILayout.ToggleLeft( "Show Scene Info", CSGSettings.ShowSceneInfo );
            }
            if (EditorGUI.EndChangeCheck())
            {
                CSGSettings.Save();
            }
        }
    }

#if UNITY_2018_4_OR_NEWER
    // Register a SettingsProvider using IMGUI for the drawing framework:
    static class RealtimeCSGOptionsIMGUIRegister
    {
        [SettingsProvider]
        public static SettingsProvider CreateMyCustomSettingsProvider()
        {
            var keys = CSGKeysPreferenceWindow.GetKeys();
            var keywords = new HashSet<string>();
            foreach (var key in keys)
            {
                var pieces = key.name.Split(' ', '/');
                foreach (var piece in pieces)
                    keywords.Add(piece);
            }

            var provider = new SettingsProvider("Project/RealtimeCSG", SettingsScope.Project)
            {
                label = "Realtime CSG",
                guiHandler = (searchContext) =>
                {
                    GUILayout.Label("Options", EditorStyles.boldLabel);
                    GUILayout.Space(10);
                    CSGOptionsPreferenceWindow.PreferenceWindow();

                    GUILayout.Space(30);
                    GUILayout.Label("Keyboard settings", EditorStyles.boldLabel);
                    GUILayout.Space(10);
                    CSGKeysPreferenceWindow.PreferenceWindow();
                },

                keywords = new[] { "Tool-Tip", "ToolTip", "Tool-Tips", "ToolTips", "Snap", "Circle", "Sphere", "Lock", "Texture", "Surface" }
            };

            return provider;
        }
    }
#endif
}
