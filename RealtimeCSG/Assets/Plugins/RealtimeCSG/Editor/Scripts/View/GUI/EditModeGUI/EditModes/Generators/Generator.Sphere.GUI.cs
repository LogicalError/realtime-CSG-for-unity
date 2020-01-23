using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealtimeCSG
{
	internal sealed partial class GeneratorSphereGUI
	{
		static bool SettingsToggle(bool value, GUIContent content, bool isSceneGUI)
		{
			if (isSceneGUI)
				return EditorGUILayout.ToggleLeft(content, value, width120);
			else
				return EditorGUILayout.Toggle(content, value);
		}

		static float SettingsSlider(float value, float minValue, float maxValue, GUIContent content, bool isSceneGUI)
		{
			if (isSceneGUI)
			{
				GUILayout.BeginHorizontal(CSG_GUIStyleUtility.ContentEmpty);
				GUILayout.Label(content, width65);
				var result = EditorGUILayout.Slider(value, minValue, maxValue, width120);
				GUILayout.EndHorizontal();
				return result;
			} else
			{
				GUILayout.BeginHorizontal(CSG_GUIStyleUtility.ContentEmpty);
				GUILayout.Label(content, width65);
				var result = EditorGUILayout.Slider(value, minValue, maxValue);
				GUILayout.EndHorizontal();
				return result;
			}
		}

		static int IntSettingsSlider(int value, int minValue, int maxValue, GUIContent content, bool isSceneGUI)
		{
			if (isSceneGUI)
			{
				GUILayout.BeginHorizontal(CSG_GUIStyleUtility.ContentEmpty);
				GUILayout.Label(content, width65);
				var result = EditorGUILayout.IntSlider(value, minValue, maxValue, width120);
				GUILayout.EndHorizontal();
				return result;
			} else
			{
				GUILayout.BeginHorizontal(CSG_GUIStyleUtility.ContentEmpty);
				GUILayout.Label(content, width65);
				var result = EditorGUILayout.IntSlider(value, minValue, maxValue);
				GUILayout.EndHorizontal();
				return result;
			}
		}

		static void SphereSettingsGUI(GeneratorSphere generator, bool isSceneGUI)
		{
			GUILayout.BeginVertical(CSG_GUIStyleUtility.ContentEmpty);
			{
				generator.SphereSmoothShading		= SettingsToggle(generator.SphereSmoothShading,		SmoothShadingContent,		isSceneGUI);
				TooltipUtility.SetToolTip(SmoothShadingTooltip);
				generator.IsHemiSphere				= SettingsToggle(generator.IsHemiSphere,			HemiSphereContent,			isSceneGUI);
				TooltipUtility.SetToolTip(HemiSphereTooltip);
			}
			GUILayout.EndVertical();
		}

		static void OnGUIContents(GeneratorSphere generator, bool isSceneGUI)
		{
			var distanceUnit = RealtimeCSG.CSGSettings.DistanceUnit;
			var nextUnit = Units.CycleToNextUnit(distanceUnit);
			var unitText = Units.GetUnitGUIContent(distanceUnit);
			//GUILayout.BeginVertical(GUIStyleUtility.ContentEmpty);
			//{
				//bool enabled = generator.HaveBrushes;
				GUILayout.BeginHorizontal(CSG_GUIStyleUtility.ContentEmpty);
				{
					/*
					EditorGUI.BeginDisabledGroup(!enabled);
					{
						if (isSceneGUI)
							GUILayout.BeginVertical(GUI.skin.box, width100);
						else
							GUILayout.BeginVertical(GUIStyle.none);
						{
							bool mixedValues = !enabled;
							CSGOperationType operation = generator.CurrentCSGOperationType;
							EditorGUI.BeginChangeCheck();
							operation = CSG_EditorGUIUtility.ChooseOperation(operation, mixedValues);
							if (EditorGUI.EndChangeCheck())
							{
								generator.CurrentCSGOperationType = operation;
							}
						}
						GUILayout.EndVertical();
					}
					EditorGUI.EndDisabledGroup();
					*/
					if (isSceneGUI)
						SphereSettingsGUI(generator, isSceneGUI);
				}
				GUILayout.EndHorizontal();

				GUILayout.Space(5);

				GUILayout.BeginVertical(CSG_GUIStyleUtility.ContentEmpty);
				{
					GUILayout.BeginHorizontal(CSG_GUIStyleUtility.ContentEmpty);
					{
						EditorGUI.BeginDisabledGroup(!generator.CanCommit);
						{
							GUILayout.Label(RadiusContent, width65);
							if (isSceneGUI)
								TooltipUtility.SetToolTip(RadiusTooltip);
							var radius = generator.SphereRadius;
							EditorGUI.BeginChangeCheck();
							{
								if (!isSceneGUI)
									radius = Units.DistanceUnitToUnity(distanceUnit, EditorGUILayout.DoubleField(Units.UnityToDistanceUnit(distanceUnit, radius)));
								else
									radius = Units.DistanceUnitToUnity(distanceUnit, EditorGUILayout.DoubleField(Units.UnityToDistanceUnit(distanceUnit, radius), width65));
							}
							if (EditorGUI.EndChangeCheck())
							{
								generator.SphereRadius = radius; 
							}
							if (GUILayout.Button(unitText, EditorStyles.miniLabel, width25))
							{
								distanceUnit = nextUnit;
								RealtimeCSG.CSGSettings.DistanceUnit = distanceUnit;
								RealtimeCSG.CSGSettings.UpdateSnapSettings();
								RealtimeCSG.CSGSettings.Save();
								CSG_EditorGUIUtility.RepaintAll();
							}
						}
						EditorGUI.EndDisabledGroup();
					}
					GUILayout.EndHorizontal();
					if (!isSceneGUI)
						TooltipUtility.SetToolTip(RadiusTooltip);

					{
						generator.SphereSplits		= IntSettingsSlider(generator.SphereSplits, 1, RealtimeCSG.CSGSettings.MaxSphereSplits, SplitsContent, isSceneGUI);
						TooltipUtility.SetToolTip(SplitsTooltip);
					}
					{
						generator.SphereOffset		= SettingsSlider(generator.SphereOffset, 0, 360, OffsetContent, isSceneGUI);
						TooltipUtility.SetToolTip(OffsetTooltip);
					}

				}
				GUILayout.EndVertical();

				if (!isSceneGUI)
				{
					GUILayout.Space(5);

					SphereSettingsGUI(generator, isSceneGUI);

					//GUILayout.Space(10);
				}/* else
				{
					GUILayout.Space(10);
				}*/
				/*
				EditorGUI.BeginDisabledGroup(!generator.CanCommit);
				{ 
					GUILayout.BeginHorizontal(GUIStyleUtility.ContentEmpty);
					{
						if (GUILayout.Button(CommitContent)) { generator.DoCommit(); }
						TooltipUtility.SetToolTip(CommitTooltip);
						if (GUILayout.Button(CancelContent)) { generator.DoCancel(); }
						TooltipUtility.SetToolTip(CancelTooltip);
					}
					GUILayout.EndHorizontal();
				}
				EditorGUI.EndDisabledGroup();
				*/
			//}
			//GUILayout.EndVertical();
		}
		
		public static bool OnShowGUI(GeneratorSphere generator, bool isSceneGUI)
		{
			CSG_GUIStyleUtility.InitStyles();
			OnGUIContents(generator, isSceneGUI);
			return true;
		}
	}
}
