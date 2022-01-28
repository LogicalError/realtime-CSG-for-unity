using UnityEditor;
using UnityEngine;


namespace RealtimeCSG
{
    internal sealed partial class GeneratorSphereGUI
    {
        static bool SettingsToggle( bool value, GUIContent content, bool isSceneGUI )
        {
            if( isSceneGUI )
                return EditorGUILayout.ToggleLeft( content, value, width120 );
            else
                return EditorGUILayout.Toggle( content, value );
        }


        static float FloatSettingsControl( float value, float minValue, float maxValue, GUIContent content, bool isSceneGUI )
        {
            if( isSceneGUI )
            {
                GUILayout.BeginHorizontal( CSG_GUIStyleUtility.ContentEmpty );
                GUILayout.Label( content, width65 );
                GUI.enabled = false;

                value = EditorGUILayout.FloatField( Mathf.Clamp( value, minValue, maxValue ) );

                GUI.enabled = true;

                if( GUILayout.Button( "-", "buttonleft", width25 ) )
                    value--;

                if( GUILayout.Button( "+", "buttonright", width25 ) )
                    value++;

                GUILayout.EndHorizontal();

                return value;
            }
            else
            {
                GUILayout.BeginHorizontal( CSG_GUIStyleUtility.ContentEmpty );
                GUILayout.Label( content, width65 );

                GUI.enabled = false;

                value = EditorGUILayout.FloatField( Mathf.Clamp( value, minValue, maxValue ) );

                GUI.enabled = true;

                if( GUILayout.Button( "-", "buttonleft", width25 ) )
                    value--;

                if( GUILayout.Button( "+", "buttonright", width25 ) )
                    value++;

                GUILayout.EndHorizontal();

                return value;
            }
        }

        static int IntSettingsControl( int value, int minValue, int maxValue, GUIContent content, bool isSceneGUI )
        {
            if( isSceneGUI )
            {
                GUILayout.BeginHorizontal( CSG_GUIStyleUtility.ContentEmpty );
                GUILayout.Label( content, width65 );

                GUI.enabled = false;

                value = EditorGUILayout.IntField( Mathf.Clamp( value, minValue, maxValue ) );

                GUI.enabled = true;

                if( GUILayout.Button( "-", "buttonleft", width25 ) )
                    value--;

                if( GUILayout.Button( "+", "buttonright", width25 ) )
                    value++;

                GUILayout.EndHorizontal();

                return value;
            }
            else
            {
                GUILayout.BeginHorizontal( CSG_GUIStyleUtility.ContentEmpty );
                GUILayout.Label( content, width65 );

                GUI.enabled = false;

                value = EditorGUILayout.IntField( Mathf.Clamp( value, minValue, maxValue ) );

                GUI.enabled = true;

                if( GUILayout.Button( "-", "buttonleft", width25 ) )
                    value--;

                if( GUILayout.Button( "+", "buttonright", width25 ) )
                    value++;

                GUILayout.EndHorizontal();

                return value;
            }
        }

        static void SphereSettingsGUI( GeneratorSphere generator, bool isSceneGUI )
        {
            GUILayout.BeginVertical( CSG_GUIStyleUtility.ContentEmpty );
            {
                generator.SphereSmoothShading = SettingsToggle( generator.SphereSmoothShading, SmoothShadingContent, isSceneGUI );
                TooltipUtility.SetToolTip( SmoothShadingTooltip );
                generator.IsHemiSphere = SettingsToggle( generator.IsHemiSphere, HemiSphereContent, isSceneGUI );
                TooltipUtility.SetToolTip( HemiSphereTooltip );
            }
            GUILayout.EndVertical();
        }

        static void OnGUIContents( GeneratorSphere generator, bool isSceneGUI )
        {
            var distanceUnit = RealtimeCSG.CSGSettings.DistanceUnit;
            var nextUnit     = Units.CycleToNextUnit( distanceUnit );
            var unitText     = Units.GetUnitGUIContent( distanceUnit );
            
            GUILayout.BeginHorizontal( CSG_GUIStyleUtility.ContentEmpty );
            {
                if( isSceneGUI )
                    SphereSettingsGUI( generator, isSceneGUI );
            }
            GUILayout.EndHorizontal();

            GUILayout.Space( 5 );

            GUILayout.BeginVertical( CSG_GUIStyleUtility.ContentEmpty );
            {
                GUILayout.BeginHorizontal( CSG_GUIStyleUtility.ContentEmpty );
                {
                    EditorGUI.BeginDisabledGroup( !generator.CanCommit );
                    {
                        GUILayout.Label( RadiusContent, width65 );

                        if( isSceneGUI )
                            TooltipUtility.SetToolTip( RadiusTooltip );

                        var radius = generator.SphereRadius;
                        EditorGUI.BeginChangeCheck();
                        {
                            if( !isSceneGUI )
                                radius = Units.DistanceUnitToUnity( distanceUnit, EditorGUILayout.DoubleField( Units.UnityToDistanceUnit( distanceUnit, radius ) ) );
                            else
                                radius = Units.DistanceUnitToUnity( distanceUnit, EditorGUILayout.DoubleField( Units.UnityToDistanceUnit( distanceUnit, radius ), width65 ) );
                        }

                        if( EditorGUI.EndChangeCheck() )
                        {
                            generator.SphereRadius = radius;
                        }

                        if( GUILayout.Button( unitText, EditorStyles.miniLabel, width25 ) )
                        {
                            distanceUnit                         = nextUnit;
                            RealtimeCSG.CSGSettings.DistanceUnit = distanceUnit;
                            RealtimeCSG.CSGSettings.UpdateSnapSettings();
                            RealtimeCSG.CSGSettings.Save();
                            CSG_EditorGUIUtility.RepaintAll();
                        }
                    }
                    EditorGUI.EndDisabledGroup();
                }

                GUILayout.EndHorizontal();

                if( !isSceneGUI )
                    TooltipUtility.SetToolTip( RadiusTooltip );

            
                generator.SphereSplits = IntSettingsControl
                (
                    generator.SphereSplits,
                    1,
                    RealtimeCSG.CSGSettings.MaxSphereSplits,
                    SplitsContent,
                    isSceneGUI
                );

                TooltipUtility.SetToolTip( SplitsTooltip );
                
                generator.SphereOffset = FloatSettingsControl
                (
                    generator.SphereOffset,
                    0,
                    360,
                    OffsetContent,
                    isSceneGUI
                );

                TooltipUtility.SetToolTip( OffsetTooltip );
            
            }

            GUILayout.EndVertical();

            if( !isSceneGUI )
            {
                GUILayout.Space( 5 );

                SphereSettingsGUI( generator, isSceneGUI );
            }
        }

        public static bool OnShowGUI( GeneratorSphere generator, bool isSceneGUI )
        {
            CSG_GUIStyleUtility.InitStyles();
            OnGUIContents( generator, isSceneGUI );

            return true;
        }
    }
}
