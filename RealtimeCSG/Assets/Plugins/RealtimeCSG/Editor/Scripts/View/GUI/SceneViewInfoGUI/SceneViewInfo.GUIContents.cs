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
        private static Rect      infoGUIRect        = new Rect();
        private static Color     infoGUITextColor   = new Color32( 160, 160, 160, 255 );
        private static GUIStyle  infoGUIStyle       = null;
        private static GUIStyle  infoGUIBGStyle     = null;
        private static float     infoGUILabelHeight = 34;
        private static Texture2D m_InfoGUIBGTex     = null;

        private static Texture2D InfoGUIBGTex
        {
            get
            {
                if( m_InfoGUIBGTex == null )
                    m_InfoGUIBGTex = Resources.Load<Texture2D>( "GUI/infobg_blk" );

                return m_InfoGUIBGTex;
            }
        }

        private static void InitStyles( SceneView sceneView )
        {
            infoGUIRect.x      = sceneView.position.width  - 116;
            infoGUIRect.y      = sceneView.position.height - 74;
            infoGUIRect.width  = 110;
            infoGUIRect.height = infoGUILabelHeight;

            infoGUIStyle = new GUIStyle( "MiniLabel" )
            {
                alignment = TextAnchor.UpperLeft,
                normal    = new GUIStyleState() { textColor = infoGUITextColor },
                margin    = new RectOffset( 0, 0, 0, 0 ),
                padding   = new RectOffset( 0, 0, 0, 0 ),
                fontSize = 11
            };

            infoGUIBGStyle = new GUIStyle( "box" )
            {
                normal = new GUIStyleState() { background = InfoGUIBGTex }
            };
        }
    }
}
