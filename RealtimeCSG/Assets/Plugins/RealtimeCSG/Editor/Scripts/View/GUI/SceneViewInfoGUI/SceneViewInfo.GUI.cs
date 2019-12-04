/* * * * * * * * * * * * * * * * * * * * * *
RealtimeCSG.SceneViewInfo.GUI.cs

License:
Author: Daniel Cornelius

Description:
Handles drawing information in the scene view, such as brush and model count.
* * * * * * * * * * * * * * * * * * * * * */

using UnityEditor;
using UnityEngine;

namespace RealtimeCSG
{
    internal sealed partial class SceneViewInfoGUI
    {
        public static void DrawInfoGUI( SceneView sceneView )
        {
            InitStyles( sceneView );
            CSG_GUIStyleUtility.InitStyles();

            try
            {
                Handles.BeginGUI();

                if( !CSGSettings.ShowSceneInfo )
                    return;

                GUILayout.BeginArea( infoGUIRect /*, infoGUIBGStyle*/ );
                {
                    GUILayout.FlexibleSpace();

                    // -Nuke: internally there is a root model that all other models are grouped under, so we'll subtract 1 to keep an accurate count of user created brushes.
                    GUILayout.Label
                    (
                        ((InternalCSGModelManager.Models.Length - 1 <= 0) ? 0 : InternalCSGModelManager.Models.Length - 1).ToString( "Models:\t###0" ),
                        infoGUIStyle,
                        GUILayout.Height( infoGUILabelHeight )
                    );

                    GUILayout.Label
                    (
                        (InternalCSGModelManager.Brushes.Count /* + 10000*/).ToString( "Brushes:\t###0" ),
                        infoGUIStyle,
                        GUILayout.Height( infoGUILabelHeight )
                    );
                }

                GUILayout.EndArea();
            }
            finally { Handles.EndGUI(); }
        }
    }
}
