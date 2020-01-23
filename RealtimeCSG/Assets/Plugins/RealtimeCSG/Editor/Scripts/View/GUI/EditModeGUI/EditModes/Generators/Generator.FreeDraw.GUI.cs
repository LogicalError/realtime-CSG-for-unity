using InternalRealtimeCSG;
using RealtimeCSG.Legacy;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealtimeCSG
{
	internal sealed partial class GeneratorFreeDrawGUI
	{
		static void FreeDrawSettingsGUI(GeneratorFreeDraw generator, bool isSceneGUI)
		{
			var distanceUnit = RealtimeCSG.CSGSettings.DistanceUnit;
			var nextUnit = Units.CycleToNextUnit(distanceUnit);
			var unitText = Units.GetUnitGUIContent(distanceUnit);

			GUILayout.BeginVertical(CSG_GUIStyleUtility.ContentEmpty);
			{
				{
					GUILayout.BeginHorizontal(CSG_GUIStyleUtility.ContentEmpty);
					{
						GUILayout.Label(HeightContent, width65);
						var height = generator.HaveHeight ? generator.Height : GeometryUtility.CleanLength(generator.DefaultHeight);
						EditorGUI.BeginChangeCheck();
						{
							height = Units.DistanceUnitToUnity(distanceUnit, EditorGUILayout.DoubleField(Units.UnityToDistanceUnit(distanceUnit, height)));
						}
						if (EditorGUI.EndChangeCheck())
						{
							if (generator.HaveHeight)
								generator.Height = height;
							else
								generator.DefaultHeight = height;
						}
						if (GUILayout.Button(unitText, EditorStyles.miniLabel, width20))
						{
							distanceUnit = nextUnit;
							RealtimeCSG.CSGSettings.DistanceUnit = distanceUnit;
							RealtimeCSG.CSGSettings.UpdateSnapSettings();
							RealtimeCSG.CSGSettings.Save();
							CSG_EditorGUIUtility.RepaintAll();
						}
					}
					GUILayout.EndHorizontal();
					TooltipUtility.SetToolTip(HeightTooltip);
				}
				{
					GUILayout.BeginHorizontal(CSG_GUIStyleUtility.ContentEmpty);
					{
						GUILayout.Label(CurveSidesContent, width65);
						var subdivisions = generator.CurveSides;
						EditorGUI.BeginChangeCheck();
						{
							subdivisions = (uint)Mathf.Clamp(EditorGUILayout.IntField((int)subdivisions), 0, 32);
						}
						if (EditorGUI.EndChangeCheck() && generator.CurveSides != subdivisions)
						{
							generator.CurveSides = subdivisions;
						}
					}
					GUILayout.EndHorizontal();
					TooltipUtility.SetToolTip(CurveSidesTooltip);
				}
				{
					GUILayout.BeginHorizontal(CSG_GUIStyleUtility.ContentEmpty);
					{
						EditorGUI.BeginDisabledGroup(!generator.HaveSelectedEdgesOrVertices);
						{
							var tangentState = generator.SelectedTangentState;
							EditorGUI.BeginChangeCheck();
							{
								GUILayout.Label(EdgeTypeContent, width65);
								EditorGUI.showMixedValue = !tangentState.HasValue;
								tangentState = (HandleConstraints)EditorGUILayout.EnumPopup(tangentState.HasValue ? tangentState.Value : HandleConstraints.Straight);
								EditorGUI.showMixedValue = false;
							}
							if (EditorGUI.EndChangeCheck())
							{
								generator.SelectedTangentState = tangentState;
							}
						}
						EditorGUI.EndDisabledGroup();
					}
					GUILayout.EndHorizontal();
					TooltipUtility.SetToolTip(EdgeTypeTooltip);
				}
				EditorGUILayout.Space();

				EditorGUI.BeginDisabledGroup(!generator.HaveSelectedEdges);
				{
					if (GUILayout.Button(SplitSelectedContent))
					{
						generator.SplitSelectedEdge();
					}
					TooltipUtility.SetToolTip(SplitSelectedTooltip);
				}
				EditorGUI.EndDisabledGroup();

				/*
				GUILayout.BeginHorizontal(GUIStyleUtility.ContentEmpty);
				{
					if (isSceneGUI)
						GUILayout.Label(AlphaContent, width75);
					else
						EditorGUILayout.PrefixLabel(AlphaContent);
					var alpha = generator.Alpha;
					EditorGUI.BeginChangeCheck();
					{
						alpha = EditorGUILayout.Slider(alpha, -1.0f, 3.0f);
					}
					if (EditorGUI.EndChangeCheck() && generator.Alpha != alpha)
					{
						generator.Alpha = alpha;
					}
				}
				GUILayout.EndHorizontal();
				*/
			}
			GUILayout.EndVertical();
		}
		
		static void OnGUIContents(GeneratorFreeDraw generator, bool isSceneGUI)
		{
			//GUILayout.BeginVertical(GUIStyleUtility.ContentEmpty);
			//{
//				bool enabled = generator.HaveBrushes;
				GUILayout.BeginHorizontal(CSG_GUIStyleUtility.ContentEmpty);
				{
					/*
					EditorGUI.BeginDisabledGroup(!enabled);
					{
						GUILayout.BeginHorizontal(GUIStyleUtility.ContentEmpty);
						{
							GUILayout.BeginVertical(isSceneGUI ? GUI.skin.box : GUIStyle.none);
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
						GUILayout.EndHorizontal();
					}
					EditorGUI.EndDisabledGroup();
					*/
					if (isSceneGUI)
						FreeDrawSettingsGUI(generator, isSceneGUI);
				}
				GUILayout.EndHorizontal();

				if (!isSceneGUI)
				{
					GUILayout.Space(5);

					FreeDrawSettingsGUI(generator, isSceneGUI);

					//GUILayout.Space(10);
				} /*else
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
		
		public static bool OnShowGUI(GeneratorFreeDraw generator, bool isSceneGUI)
		{
			CSG_GUIStyleUtility.InitStyles();
			OnGUIContents(generator, isSceneGUI);
			return true;
		}
	}
}
