using UnityEditor;
using UnityEngine;


namespace RealtimeCSG
{
    internal sealed partial class GeneratorCylinderGUI
    {
        static bool SettingsToggle( bool value, GUIContent content, GUILayoutOption sceneWidth, bool isSceneGUI )
        {
            if( isSceneGUI )
                return EditorGUILayout.ToggleLeft( content, value, sceneWidth );
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

        static void CylinderSettingsGUI( GeneratorCylinder generator, bool isSceneGUI )
        {
            if( isSceneGUI )
                GUILayout.BeginHorizontal( GUILayout.MinWidth( 0 ) );

            if( isSceneGUI )
                GUILayout.BeginVertical( width110 );

            generator.CircleSmoothShading = SettingsToggle( generator.CircleSmoothShading, SmoothShadingContent, width110, isSceneGUI );
            TooltipUtility.SetToolTip( SmoothShadingTooltip );
            generator.CircleDistanceToSide = SettingsToggle( generator.CircleDistanceToSide, AlignToSideContent, width110, isSceneGUI );
            TooltipUtility.SetToolTip( AlignToSideTooltip );

            if( isSceneGUI )
            {
                GUILayout.EndVertical();
                GUILayout.BeginVertical( width80 );
            }

            generator.CircleSingleSurfaceEnds = !SettingsToggle( !generator.CircleSingleSurfaceEnds, RadialCapsContent, width80, isSceneGUI );
            TooltipUtility.SetToolTip( RadialCapsTooltip );
            generator.CircleRecenter = SettingsToggle( generator.CircleRecenter, FitShapeContent, width80, isSceneGUI );
            TooltipUtility.SetToolTip( FitShapeTooltip );

            if( isSceneGUI )
                GUILayout.EndVertical();

            if( isSceneGUI )
                GUILayout.EndHorizontal();
        }

        static void OnGUIContents( GeneratorCylinder generator, bool isSceneGUI )
        {
            GUILayout.BeginHorizontal( CSG_GUIStyleUtility.ContentEmpty );
            {
                if( isSceneGUI )
                    CylinderSettingsGUI( generator, isSceneGUI );
            }
            GUILayout.EndHorizontal();

            GUILayout.Space( 5 );

            GUILayout.BeginVertical( CSG_GUIStyleUtility.ContentEmpty );
            {
                var distanceUnit = RealtimeCSG.CSGSettings.DistanceUnit;
                var nextUnit     = Units.CycleToNextUnit( distanceUnit );
                var unitText     = Units.GetUnitGUIContent( distanceUnit );
                
                GUILayout.BeginHorizontal( CSG_GUIStyleUtility.ContentEmpty );
                {
                    GUILayout.Label( HeightContent, width65 );

                    if( isSceneGUI )
                        TooltipUtility.SetToolTip( HeightTooltip );

                    var height = generator.HaveHeight ? generator.Height : GeometryUtility.CleanLength( generator.DefaultHeight );
                    EditorGUI.BeginChangeCheck();
                    {
                        if( !isSceneGUI )
                            height = Units.DistanceUnitToUnity( distanceUnit, EditorGUILayout.DoubleField( Units.UnityToDistanceUnit( distanceUnit, height ) ) );
                        else
                            height = Units.DistanceUnitToUnity( distanceUnit, EditorGUILayout.DoubleField( Units.UnityToDistanceUnit( distanceUnit, height ), width65 ) );
                    }

                    if( EditorGUI.EndChangeCheck() )
                    {
                        if( generator.HaveHeight )
                            generator.Height = height;
                        else
                            generator.DefaultHeight = height;
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
                GUILayout.EndHorizontal();
                
                TooltipUtility.SetToolTip( HeightTooltip );
                
                GUILayout.BeginHorizontal( CSG_GUIStyleUtility.ContentEmpty );
                {
                    EditorGUI.BeginDisabledGroup( !generator.CanCommit );
                    {
                        GUILayout.Label( RadiusContent, width65 );

                        if( isSceneGUI )
                            TooltipUtility.SetToolTip( RadiusTooltip );

                        var radius = generator.RadiusA;
                        EditorGUI.BeginChangeCheck();
                        {
                            if( !isSceneGUI )
                                radius = Units.DistanceUnitToUnity( distanceUnit, EditorGUILayout.DoubleField( Units.UnityToDistanceUnit( distanceUnit, radius ) ) );
                            else
                                radius = Units.DistanceUnitToUnity( distanceUnit, EditorGUILayout.DoubleField( Units.UnityToDistanceUnit( distanceUnit, radius ), width65 ) );
                        }

                        if( EditorGUI.EndChangeCheck() )
                        {
                            generator.RadiusA = radius;
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
            }

            GUILayout.EndVertical();
            {
                generator.CircleSides = IntSettingsControl
                (
                    generator.CircleSides,
                    3,
                    RealtimeCSG.CSGSettings.MaxCircleSides,
                    SidesContent,
                    isSceneGUI
                );

                TooltipUtility.SetToolTip( SidesTooltip );
            }
            generator.CircleOffset = FloatSettingsControl
            (
                generator.CircleOffset,
                0,
                360,
                OffsetContent,
                isSceneGUI
            );

            TooltipUtility.SetToolTip( OffsetTooltip );


            if( !isSceneGUI )
            {
                GUILayout.Space( 5 );

                CylinderSettingsGUI( generator, isSceneGUI );
            }
        }

        public static bool OnShowGUI( GeneratorCylinder generator, bool isSceneGUI )
        {
            CSG_GUIStyleUtility.InitStyles();
            OnGUIContents( generator, isSceneGUI );

            return true;
        }
    }
}
