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
        //private static GUIStyle infoGUIBGStyle     = null;
        private static float    infoGUILabelHeight = 13;

        private static void InitStyles( SceneView sceneView )
        {
            infoGUIRect.x      = sceneView.position.width  - 126;
            infoGUIRect.y      = sceneView.position.height - 156;
            infoGUIRect.width  = 110;
            infoGUIRect.height = 100;

            infoGUIStyle = new GUIStyle( "MiniLabel" )
            {
                alignment   = TextAnchor.LowerLeft,
                normal      = new GUIStyleState() {textColor = infoGUITextColor},
                margin      = new RectOffset( 0, 0, 0, 0 ),
                padding     = new RectOffset( 0, 0, 0, 0 ),
                fixedHeight = infoGUILabelHeight,
                fontSize    = 11
            };

            // used for debug
            // infoGUIBGStyle = new GUIStyle( "box" )
            // {
            //     margin  = new RectOffset( 0, 0, 0, 0 ),
            //     padding = new RectOffset( 0, 0, 0, 0 )
            // };
        }
    }
}
