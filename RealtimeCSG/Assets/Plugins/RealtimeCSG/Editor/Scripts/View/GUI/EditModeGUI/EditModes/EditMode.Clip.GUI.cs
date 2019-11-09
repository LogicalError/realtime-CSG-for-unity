using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using InternalRealtimeCSG;

namespace RealtimeCSG
{
	internal sealed partial class EditModeClipGUI
	{
		static int SceneViewMeshOverlayHash = "SceneViewClipOverlay".GetHashCode();

		static void InitLocalStyles()
		{
			if (ContentClipLabel != null)
				return;

			ContentClipLabel	= new GUIContent(CSG_GUIStyleUtility.brushEditModeNames[(int)ToolEditMode.Clip]);
		}
		
		static bool doCommit = false; // unity bug workaround
		static bool doCancel = false; // unity bug workaround

		static Rect lastGuiRect;
		public static Rect GetLastSceneGUIRect(EditModeClip tool)
		{
			return lastGuiRect;
		}

		public static void OnSceneGUI(Rect windowRect, EditModeClip tool)
		{
			doCommit = false; // unity bug workaround
			doCancel = false; // unity bug workaround

			CSG_GUIStyleUtility.InitStyles();
			InitLocalStyles();
			GUILayout.BeginHorizontal(CSG_GUIStyleUtility.ContentEmpty);
			{
				GUILayout.BeginVertical(CSG_GUIStyleUtility.ContentEmpty);
				{
					GUILayout.BeginVertical(CSG_GUIStyleUtility.ContentEmpty);
					{
						GUILayout.FlexibleSpace();

						CSG_GUIStyleUtility.ResetGUIState();
						
						GUIStyle windowStyle = GUI.skin.window;
						GUILayout.BeginVertical(ContentClipLabel, windowStyle, CSG_GUIStyleUtility.ContentEmpty);
						{
							OnGUIContents(true, tool);
						}
						GUILayout.EndVertical();
						var currentArea = GUILayoutUtility.GetLastRect();
						lastGuiRect = currentArea;

						var buttonArea = currentArea;
						buttonArea.x += buttonArea.width - 17;
						buttonArea.y += 2;
						buttonArea.height = 13;
						buttonArea.width = 13;
						if (GUI.Button(buttonArea, GUIContent.none, "WinBtnClose"))
							EditModeToolWindowSceneGUI.GetWindow();
						TooltipUtility.SetToolTip(CSG_GUIStyleUtility.PopOutTooltip, buttonArea);

						int controlID = GUIUtility.GetControlID(SceneViewMeshOverlayHash, FocusType.Passive, currentArea);
						switch (Event.current.GetTypeForControl(controlID))
						{
							case EventType.MouseDown:	{ if (currentArea.Contains(Event.current.mousePosition)) { GUIUtility.hotControl = controlID; GUIUtility.keyboardControl = controlID; Event.current.Use(); } break; }
							case EventType.MouseMove:	{ if (currentArea.Contains(Event.current.mousePosition)) { Event.current.Use(); } break; }
							case EventType.MouseUp:		{ if (GUIUtility.hotControl == controlID) { GUIUtility.hotControl = 0; GUIUtility.keyboardControl = 0; Event.current.Use(); } break; }
							case EventType.MouseDrag:	{ if (GUIUtility.hotControl == controlID) { Event.current.Use(); } break; }
							case EventType.ScrollWheel: { if (currentArea.Contains(Event.current.mousePosition)) { Event.current.Use(); } break; }
						}
					}
					GUILayout.EndVertical();
				}
				GUILayout.EndVertical();
				GUILayout.FlexibleSpace();
			}
			GUILayout.EndHorizontal();

			if (tool != null)
			{ 
				if (doCommit) tool.Commit();	// unity bug workaround
				if (doCancel) tool.Cancel();	// unity bug workaround
			}
		}

		static void OnGUIContents(bool isSceneGUI, EditModeClip tool)
		{
			EditModeCommonGUI.StartToolGUI();

			if (tool.ClipBrushCount == 0)
			{
				GUILayout.Label(string.Format("no brushes selected", tool.ClipBrushCount), CSG_GUIStyleUtility.redTextArea);
			} else
			{ 
				if (tool.ClipBrushCount == 1)
					GUILayout.Label(string.Format("{0} brush selected", tool.ClipBrushCount));
				else
					GUILayout.Label(string.Format("{0} brushes selected", tool.ClipBrushCount));
			}
			EditorGUILayout.Space();
			EditorGUI.BeginDisabledGroup(tool == null);
			{ 
				GUILayout.BeginVertical(isSceneGUI ? GUI.skin.box : GUIStyle.none);
				{
					var newClipMode = (tool != null) ? tool.clipMode : ((ClipMode)999);
					var skin = CSG_GUIStyleUtility.Skin;
					for (int i = 0; i < clipModeValues.Length; i++)
					{
						var selected = newClipMode == clipModeValues[i];
						GUIContent content;
						GUIStyle style;
						if (selected)	{ style = CSG_GUIStyleUtility.selectedIconLabelStyle;   content = skin.clipNamesOn[i]; }
						else			{ style = CSG_GUIStyleUtility.unselectedIconLabelStyle; content = skin.clipNames[i];   }
						if (GUILayout.Toggle(selected, content, style))
						{
							newClipMode = clipModeValues[i];
						}
						TooltipUtility.SetToolTip(CSG_GUIStyleUtility.clipTooltips[i]);
					}
					if (tool != null && tool.clipMode != newClipMode)
					{
						tool.SetClipMode(newClipMode);
					}
				}
				GUILayout.EndVertical();
				if (!isSceneGUI)
					GUILayout.Space(10);

				bool disabled = (tool == null || tool.editMode != EditModeClip.EditMode.EditPoints);

				var defaultMaterial = CSGSettings.DefaultMaterial;
				GUILayout.BeginVertical(isSceneGUI ? MaterialSceneWidth : CSG_GUIStyleUtility.ContentEmpty);
				{
					GUILayout.BeginHorizontal(CSG_GUIStyleUtility.ContentEmpty);
					{
						if (isSceneGUI)
						{
							EditorGUI.BeginChangeCheck();
							{
								defaultMaterial = CSG_EditorGUIUtility.MaterialImage(defaultMaterial);
							}
							if (EditorGUI.EndChangeCheck() && defaultMaterial)
							{
								CSGSettings.DefaultMaterial = defaultMaterial;
								CSGSettings.Save();
							}
							GUILayout.BeginVertical(CSG_GUIStyleUtility.ContentEmpty);
						}
						{
							EditorGUI.BeginDisabledGroup(disabled);
							{
								if (GUILayout.Button(ContentCancel)) { doCancel = true; }
								TooltipUtility.SetToolTip(CancelTooltip);
								if (GUILayout.Button(ContentCommit)) { doCommit = true; }
								TooltipUtility.SetToolTip(CommitTooltip);
							}
							EditorGUI.EndDisabledGroup();
						}
						if (isSceneGUI)
							GUILayout.EndVertical();
					}
					GUILayout.EndHorizontal();
					if (isSceneGUI)
					{
						GUILayout.Space(2);
						EditorGUI.BeginChangeCheck();
						{
							defaultMaterial = EditorGUILayout.ObjectField(defaultMaterial, typeof(Material), true) as Material;
						}
						if (EditorGUI.EndChangeCheck() && defaultMaterial)
						{
							CSGSettings.DefaultMaterial = defaultMaterial;
							CSGSettings.Save();
						}
					}
				}
				if (!isSceneGUI)
				{
					EditorGUILayout.Space();
					GUILayout.BeginHorizontal(CSG_GUIStyleUtility.ContentEmpty);
					{
						EditorGUILayout.LabelField(ContentDefaultMaterial, largeLabelWidth);
						GUILayout.BeginVertical(CSG_GUIStyleUtility.ContentEmpty);
						{
							EditorGUI.BeginChangeCheck();
							{
								defaultMaterial = EditorGUILayout.ObjectField(defaultMaterial, typeof(Material), true) as Material;
							}
							if (EditorGUI.EndChangeCheck() && defaultMaterial)
							{
								CSGSettings.DefaultMaterial = defaultMaterial;
								CSGSettings.Save();
							}
						}
						GUILayout.Space(2);
						GUILayout.BeginHorizontal(CSG_GUIStyleUtility.ContentEmpty);
						{
							GUILayout.Space(5);
							defaultMaterial = CSG_EditorGUIUtility.MaterialImage(defaultMaterial, small: false);
						}
						GUILayout.EndHorizontal();
						GUILayout.EndVertical();
					}
					GUILayout.EndHorizontal();
					/*
					// Unity won't let us do this
					GUILayout.BeginVertical(GUIStyleUtility.ContentEmpty);
					OnGUIContentsMaterialInspector(first_material, multiple_materials);
					GUILayout.EndVertical();
					*/
				}
				GUILayout.EndVertical();
			}
			EditorGUI.EndDisabledGroup();
		}

		public static void OnInspectorGUI(EditorWindow window, float height)
		{
			lastGuiRect = Rect.MinMaxRect(-1, -1, -1, -1);
			var tool = EditModeManager.ActiveTool as EditModeClip;

			doCommit = false; // unity bug workaround
			doCancel = false; // unity bug workaround
			
			CSG_GUIStyleUtility.InitStyles();
			InitLocalStyles();
			OnGUIContents(false, tool);

			if (tool != null)
			{ 
				if (doCommit) tool.Commit();	// unity bug workaround
				if (doCancel) tool.Cancel();	// unity bug workaround
			}
		}
	}
}
