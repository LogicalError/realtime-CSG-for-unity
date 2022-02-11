// * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
// Author:             Daniel Cornelius (NukeAndBeans)
// Contact:            Twitter @nukeandbeans, Discord Nuke#3681
// License:
// Date/Time:          01-27-2022 @ 7:06 PM
// 
// Description:
// General utility window used to generate Editor/Scripts/Data/Generated/Icons/EmbeddedAssets.cs
// 
// * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *


using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;


namespace RealtimeCSG.Utilities
{
    public class Icon2EmbeddedAssetsUtility : EditorWindow
    {
        private static Icon2EmbeddedAssetsUtility _window;

#if RCSG_GEN_ICONS_UTILITY
        [MenuItem( "Tools/Icon Generator Utility" )]
#endif
        private static void Init()
        {
            _window              = GetWindow<Icon2EmbeddedAssetsUtility>( true );
            _window.titleContent = new GUIContent( "Icon2EmbeddedAssets" );
            _window.minSize      = new Vector2( 450, 160 );
            _window.maxSize      = new Vector2( 450, 600 );
            _window.ShowUtility();
        }

        private const string SCRIPT_NAME = "EmbeddedAssets";
        private const string NAMESPACE   = "RealtimeCSG";

        private GUIContent _convertContent;

        private Texture2D[] foundT2Ds;

        private bool    assetsLoaded = false;
        private Vector2 scrollPos;

        private void OnGUI()
        {
            if( _convertContent == null )
            {
                _convertContent = new GUIContent( "Convert" );
            }

            using( new GUILayout.HorizontalScope() )
            {
                GUILayout.FlexibleSpace();

                if( GUILayout.Button( "Find Icons" ) )
                {
                    foundT2Ds = null;

                    foundT2Ds = new Texture2D[]
                    {
                    };

                    foundT2Ds = Resources.LoadAll<Texture2D>( "RealtimeCSG/Icons" );
                }
            }

            if( foundT2Ds != null )
            {
                using( new GUILayout.HorizontalScope( "GameViewBackground" ) )
                {
                    using( GUILayout.ScrollViewScope sv = new GUILayout.ScrollViewScope( scrollPos, false, true ) )
                    {
                        scrollPos = sv.scrollPosition;

                        if( foundT2Ds.Length > 1 )
                        {
                            foreach( Texture2D t in foundT2Ds )
                            {
                                using( new GUILayout.HorizontalScope() )
                                {
                                    GUILayout.Label( t.name );
                                    GUILayout.FlexibleSpace();
                                    GUILayout.Label( string.Format( "{0}x{1} \t {2}", t.width, t.height, t.format ), GUILayout.Width( 110 ) );
                                }
                            }

                            assetsLoaded = true;
                        }
                    }

                    GUILayout.Space( 8 );

                    using( new GUILayout.VerticalScope( "HelpBox" ) )
                    {
                        GUILayout.FlexibleSpace();

                        if( !assetsLoaded ) GUI.enabled = false;

                        if( GUILayout.Button( _convertContent ) )
                        {
                            AssetDatabase.StartAssetEditing();

                            string[] folders    = AssetDatabase.FindAssets( "t:Script TexGenState" );
                            string   rcsgFolder = Path.GetDirectoryName( AssetDatabase.GUIDToAssetPath( folders[0] ) );
                            string   path       = string.Format( "{0}\\Generated\\Icons\\{1}.cs", rcsgFolder, SCRIPT_NAME );

                            File.WriteAllText( string.Format( "{0}\\{1}", Application.dataPath.Replace( "/", "\\" ).Replace( "Assets", "" ), path ), ConstructScript( foundT2Ds ) );
                            AssetDatabase.ImportAsset( path );

                            AssetDatabase.StopAssetEditing();
                            AssetDatabase.SaveAssets();
                            AssetDatabase.Refresh();
                        }

                        GUI.enabled = true;
                    }
                }
            }
        }

        private static string TexNameProperty( string s )
        {
            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase( s.Replace( "_", " " ) ).Replace( " ", string.Empty );
        }


        private static void ScriptAppend( ref StringBuilder sb, string content, int depth )
        {
            int i = 0;

            while( i < depth )
            {
                sb.Append( "\t" );

                i++;
            }

            sb.Append( string.Format( "{0}\n", content ) );
        }

        private string ConstructScript( Texture2D[] textures )
        {
            StringBuilder sb = new StringBuilder();


            sb.AppendLine( "/*******************************************************" );
            sb.AppendLine( "* !!!!!! GENERATED, DO NOT MANUALLY EDIT !!!!!!" );
            sb.AppendLine( string.Format( "* Last updated on: {0:D}", DateTime.Now ) );
            sb.AppendLine( "*" );
            sb.AppendLine( "* Contains embedded versions of all the icons used by RealtimeCSG," );
            sb.AppendLine( "* which are automatically loaded as-needed." );
            sb.AppendLine( "********************************************************/" );
            sb.AppendLine();
            sb.AppendLine( "using System;" );
            sb.AppendLine( "using System.Collections.Generic;" );
            sb.AppendLine( "using UnityEditor;" );
            sb.AppendLine( "using UnityEngine;" );
            sb.AppendLine();
            sb.AppendLine( string.Format( "namespace {0}", NAMESPACE ) );
            sb.AppendLine( "{" );
            ScriptAppend( ref sb, string.Format( "public static class {0}", SCRIPT_NAME ), 1 );
            ScriptAppend( ref sb, "{", 1 );

            ScriptAppend( ref sb, "public static bool TryFindIcon( string name, out Texture2D icon )", 2 );
            ScriptAppend( ref sb, "{", 2 );
            ScriptAppend( ref sb, "return icons.TryGetValue( name, out icon );", 3 );
            ScriptAppend( ref sb, "}", 2 );

            ScriptAppend( ref sb, "public static Dictionary<string, Texture2D> icons = new Dictionary<string, Texture2D>()", 2 );
            ScriptAppend( ref sb, "{", 2 );

            foreach( Texture2D t in textures )
            {
                ScriptAppend( ref sb, string.Format( "{{ \"{0}\", {1} }},", t.name, TexNameProperty( t.name ) ), 3 );
            }

            ScriptAppend( ref sb, "};", 2 );

            foreach( Texture2D t in textures )
            {
                if( t != null )
                {
                    string tname       = TexNameProperty( t.name );
                    string tnameLower0 = tname.Replace( tname[0].ToString(), tname[0].ToString().ToLower() );

                    ScriptAppend( ref sb, string.Format( "private const string {0} = @\"{1}\";", t.name, Convert.ToBase64String( t.GetRawTextureData() ) ), 2 );
                    ScriptAppend( ref sb, string.Format( "private static Texture2D {0};", tnameLower0 ), 2 );
                    ScriptAppend( ref sb, string.Format( "private static Texture2D {0}", tname ), 2 );
                    ScriptAppend( ref sb, "{", 2 );
                    ScriptAppend( ref sb, "get", 3 );
                    ScriptAppend( ref sb, "{", 3 );
                    ScriptAppend( ref sb, string.Format( "if( {0} == null )", tnameLower0 ), 4 );
                    ScriptAppend( ref sb, "{", 4 );
                    ScriptAppend( ref sb, string.Format( "{0} = new Texture2D( {1},{2}, TextureFormat.RGBA32, false, PlayerSettings.colorSpace == ColorSpace.Linear  );", tnameLower0, t.width, t.height ), 5 );
                    ScriptAppend( ref sb, string.Format( "{0}.LoadRawTextureData( Convert.FromBase64String( {1} ) );", tnameLower0, t.name ), 5 );
                    ScriptAppend( ref sb, string.Format( "{0}.Apply();", tnameLower0 ), 5 );
                    sb.AppendLine();
                    ScriptAppend( ref sb, "}}", 4 );
                    ScriptAppend( ref sb, string.Format( "return {0};", tnameLower0 ), 4 );
                    ScriptAppend( ref sb, "}}", 3 );
                    ScriptAppend( ref sb, "}}", 2 );
                    sb.AppendLine();
                }
            }

            sb.AppendLine( "\t}" );
            sb.AppendLine( "}" );
            sb.AppendLine();

            return sb.ToString().Replace( "\t", "    " );
        }
    }
}
