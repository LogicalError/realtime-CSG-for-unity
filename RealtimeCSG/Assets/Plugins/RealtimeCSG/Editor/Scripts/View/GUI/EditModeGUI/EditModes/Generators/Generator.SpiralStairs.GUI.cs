using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealtimeCSG
{
	internal sealed partial class GeneratorSpiralStairsGUI
	{
		static float FloatUnitsSettings(float value, GUIContent content, ToolTip tooltip, bool isSceneGUI)
		{
			var distanceUnit = RealtimeCSG.CSGSettings.DistanceUnit;
			var unitText = Units.GetUnitGUIContent(distanceUnit);

			float newValue;
			EditorGUI.BeginChangeCheck();
			{ 
				GUILayout.BeginHorizontal(CSG_GUIStyleUtility.ContentEmpty);
				{
					GUILayout.Label(content, width80);
					if (isSceneGUI)
						TooltipUtility.SetToolTip(tooltip);

					if (!isSceneGUI)
						newValue = Units.DistanceUnitToUnity(distanceUnit, EditorGUILayout.DoubleField(Units.UnityToDistanceUnit(distanceUnit, value)));
					else
						newValue = Units.DistanceUnitToUnity(distanceUnit, EditorGUILayout.DoubleField(Units.UnityToDistanceUnit(distanceUnit, value), width65));

					if (GUILayout.Button(unitText, EditorStyles.miniLabel, width25))
					{
						distanceUnit = Units.CycleToNextUnit(distanceUnit); ;
						RealtimeCSG.CSGSettings.DistanceUnit = distanceUnit;
						RealtimeCSG.CSGSettings.UpdateSnapSettings();
						RealtimeCSG.CSGSettings.Save();
						CSG_EditorGUIUtility.RepaintAll();
					}
				}
				GUILayout.EndHorizontal();
				if (!isSceneGUI)
					TooltipUtility.SetToolTip(tooltip);
			}
			if (EditorGUI.EndChangeCheck())
				return newValue;
			return value;
		}

		static int IntValueSettings(int value, GUIContent content, ToolTip tooltip, bool isSceneGUI)
		{
			int newValue;
			EditorGUI.BeginChangeCheck();
			{ 
				GUILayout.BeginHorizontal(CSG_GUIStyleUtility.ContentEmpty);
				{
					GUILayout.Label(content, width80);
					if (isSceneGUI)
						TooltipUtility.SetToolTip(tooltip);

					if (!isSceneGUI)
						newValue = EditorGUILayout.IntField(value);
					else
						newValue = EditorGUILayout.IntField(value, width65);
				}
				GUILayout.EndHorizontal();
				if (!isSceneGUI)
					TooltipUtility.SetToolTip(tooltip);
			}
			if (EditorGUI.EndChangeCheck())
				return newValue;
			return value;
		}


		static void OnGUIContents(GeneratorSpiralStairs generator, bool isSceneGUI)
		{	
			GUILayout.BeginVertical(CSG_GUIStyleUtility.ContentEmpty);
			{
				EditorGUI.BeginChangeCheck();
				var totalSteps	= Mathf.Max(IntValueSettings  (generator.TotalSteps,	TotalStepsContent,		TotalStepsTooltip,		isSceneGUI), 1);
				if (EditorGUI.EndChangeCheck()) { generator.TotalSteps = totalSteps; }

				EditorGUI.BeginChangeCheck();
				var stepDepth		= Mathf.Max(FloatUnitsSettings(generator.StepDepth,	StepDepthContent,		StepDepthTooltip,		isSceneGUI), GeneratorSpiralStairsSettings.kMinStepDepth);
				if (EditorGUI.EndChangeCheck()) { generator.StepDepth = stepDepth; }

				EditorGUI.BeginChangeCheck();
				var stepHeight	= Mathf.Max(FloatUnitsSettings(generator.StepHeight,	StepHeightContent,		StepHeightTooltip,		isSceneGUI), GeneratorSpiralStairsSettings.kMinStepHeight);
				if (EditorGUI.EndChangeCheck()) { generator.StepHeight = stepHeight; }

				GUILayout.Space(4);

				EditorGUI.BeginChangeCheck();
				var stairsWidth	= Mathf.Max(FloatUnitsSettings(generator.StairsWidth,   StairsWidthContent,		StairsWidthTooltip,		isSceneGUI), 0.01f);
				if (EditorGUI.EndChangeCheck()) { generator.StairsWidth = stairsWidth; }

				EditorGUI.BeginChangeCheck();
				var stairsHeight	= Mathf.Max(FloatUnitsSettings(generator.StairsHeight,  StairsHeightContent,	StairsHeightTooltip,	isSceneGUI), 0.01f);
				if (EditorGUI.EndChangeCheck()) { generator.StairsHeight = stairsHeight; }

				EditorGUI.BeginChangeCheck();
				var stairsDepth	= Mathf.Max(FloatUnitsSettings(generator.StairsDepth,	StairsDepthContent,		StairsDepthTooltip,		isSceneGUI), 0.01f);
				if (EditorGUI.EndChangeCheck()) { generator.StairsDepth = stairsDepth; }

				GUILayout.Space(4);

				EditorGUI.BeginChangeCheck();
				var extraDepth	= Mathf.Max(FloatUnitsSettings(generator.ExtraDepth,	ExtraDepthContent,		ExtraDepthTooltip,		isSceneGUI), 0);
				if (EditorGUI.EndChangeCheck()) { generator.ExtraDepth = extraDepth; }

				EditorGUI.BeginChangeCheck();
				var extraHeight	= Mathf.Max(FloatUnitsSettings(generator.ExtraHeight,	ExtraHeightContent,		ExtraHeightTooltip,		isSceneGUI), 0);				
				if (EditorGUI.EndChangeCheck()) { generator.ExtraHeight = extraHeight; }
			}
			GUILayout.EndVertical();
		}
		
		public static bool OnShowGUI(GeneratorSpiralStairs generator, bool isSceneGUI)
		{
			CSG_GUIStyleUtility.InitStyles();
			OnGUIContents(generator, isSceneGUI);
			return true;
		}
	}
}
