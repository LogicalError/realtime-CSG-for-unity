/* * * * * * * * * * * * * * * * * * * * * *
RealtimeCSG.SceneViewInfo.GUIContents.cs

License:
Author: Daniel Cornelius

Description:
Handles styles for SceneViewInfoGUI
* * * * * * * * * * * * * * * * * * * * * */

using UnityEditor;
using UnityEngine;

namespace RealtimeCSG
{
    internal sealed partial class SceneViewInfoGUI
    {
        private static Rect     infoGUIRect        = new Rect();
        private static Color    infoGUITextColor   = new Color32( 128, 128, 128, 255 );
        private static GUIStyle infoGUIStyle       = null;
        private static float    infoGUILabelHeight = 100;

        private static void InitStyles( SceneView sceneView )
        {
            infoGUIRect.x      = sceneView.position.width  - 126;
            infoGUIRect.y      = sceneView.position.height - 156;
            infoGUIRect.width  = 100;
            infoGUIRect.height = 100;

            infoGUIStyle = new GUIStyle( "MiniLabel" )
            {
                alignment   = TextAnchor.LowerLeft,
                normal      = new GUIStyleState() {textColor = infoGUITextColor},
                fixedHeight = infoGUILabelHeight,
                fontSize    = 11
            };
        }
    }
}
