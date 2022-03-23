using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Text;
using System;
using System.Linq;
using InternalRealtimeCSG;

namespace RealtimeCSG
{

	internal class SurfaceVisibilityPopup : UnityEditor.PopupWindowContent
	{
		private class Flags
		{
			public readonly GUIContent		mixedValueContent	= new GUIContent("\u2014|Mixed Values");
			public readonly GUIContent		nothingContent		= new GUIContent("Nothing");
			public readonly GUIContent		onlyVisibleContent	= new GUIContent("Show Only Visible");
			public readonly GUIContent		allHelperContent	= new GUIContent("Show All Helper Surfaces");
			public readonly GUIContent		mixedContent		= new GUIContent("Mixed ...");
			public readonly GUIContent[]	helperSurfaceFlagContent;
			public readonly int[]			helperSurfaceFlagValues;
			public readonly int				allSurfaces;

			public Flags()
			{
				helperSurfaceFlagContent = Enum.GetNames (typeof(HelperSurfaceFlags)).Select(x => new GUIContent(ObjectNames.NicifyVariableName(x))).ToArray();

				var crappyArray = Enum.GetValues(typeof(HelperSurfaceFlags));
				helperSurfaceFlagValues = new int[crappyArray.Length];
				for (int i = 0; i < crappyArray.Length; i++)
					helperSurfaceFlagValues[i] = (int)crappyArray.GetValue(i);

				allSurfaces = 0;
				for (int i = 0; i < helperSurfaceFlagValues.Length; i++)
					allSurfaces |= helperSurfaceFlagValues[i];
			}
		}
		
		private static Flags s_Flags;

		private Camera m_Camera;

		const float kFrameWidth = 1f;
		const float kLineHeight = 20f;

		SurfaceVisibilityPopup(Camera camera)
		{
            m_Camera = camera;
		}

		
		public override Vector2 GetWindowSize()
		{
			if (s_Flags == null)
				s_Flags = new Flags();
			
			var windowHeight = 2f * kFrameWidth + kLineHeight * (s_Flags.helperSurfaceFlagContent.Length + 2) + 4;
			var windowSize = new Vector2(210, windowHeight);
			return windowSize;
		}


		public override void OnGUI(Rect rect)
		{
			if (!m_Camera)
				return;

			if (s_Flags == null)
				s_Flags = new Flags();
			
			// We do not use the layout event
			if (Event.current.type == EventType.Layout)
				return;

			// Content
			Draw(rect);

			// Use mouse move so we get hover state correctly in the menu item rows
			if (Event.current.type == EventType.MouseMove)
				Event.current.Use();

			// Escape closes the window
			if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
			{
				editorWindow.Close();
				GUIUtility.ExitGUI();
			}
		}

		private void Draw(Rect rect)
		{ 
			var allVisibleSurfaces	= (int)HelperSurfaceFlags.ShowVisibleSurfaces;
			var allHelperSurfaces	= s_Flags.allSurfaces & ~allVisibleSurfaces;

			var drawPos = new Rect(kFrameWidth, kFrameWidth, rect.width - 2 * kFrameWidth, kLineHeight);
			
			var helperSurfaces = (int)RealtimeCSG.CSGSettings.VisibleHelperSurfaces;
			EditorGUI.BeginChangeCheck();
			GUI.Toggle(drawPos, helperSurfaces == allVisibleSurfaces, s_Flags.onlyVisibleContent, CSG_GUIStyleUtility.Skin.menuItem);
			if (EditorGUI.EndChangeCheck())
			{
				RealtimeCSG.CSGSettings.VisibleHelperSurfaces = (HelperSurfaceFlags)allVisibleSurfaces;
				MeshInstanceManager.UpdateHelperSurfaceVisibility();
				RealtimeCSG.CSGSettings.Save();
				CSG_EditorGUIUtility.RepaintAll();
			}
			drawPos.y += kLineHeight;
		
			EditorGUI.BeginChangeCheck();
			GUI.Toggle(drawPos, helperSurfaces == allHelperSurfaces, s_Flags.allHelperContent, CSG_GUIStyleUtility.Skin.menuItem);
			if (EditorGUI.EndChangeCheck())
			{
				RealtimeCSG.CSGSettings.VisibleHelperSurfaces = (HelperSurfaceFlags)allHelperSurfaces;
				MeshInstanceManager.UpdateHelperSurfaceVisibility();
				RealtimeCSG.CSGSettings.Save();
				CSG_EditorGUIUtility.RepaintAll();
			}
			drawPos.y += kLineHeight;
			
			drawPos.y += 4;
			
			for (int i = 0; i < s_Flags.helperSurfaceFlagContent.Length; i++)
			{
				var flag = s_Flags.helperSurfaceFlagValues[i];
				DrawListElement(drawPos, s_Flags.helperSurfaceFlagContent[i], helperSurfaces, flag);
				drawPos.y += kLineHeight;
			}
		}

		void DrawListElement(Rect rect, GUIContent toggleName, int helperSurfaces, int flag)
		{
			EditorGUI.BeginChangeCheck();
			bool result = GUI.Toggle(rect, ((helperSurfaces & flag) != 0), toggleName, CSG_GUIStyleUtility.Skin.menuItem);
			if (EditorGUI.EndChangeCheck())
			{
				if (result) RealtimeCSG.CSGSettings.VisibleHelperSurfaces = (HelperSurfaceFlags)(helperSurfaces | flag);
				else        RealtimeCSG.CSGSettings.VisibleHelperSurfaces = (HelperSurfaceFlags)(helperSurfaces & ~flag);
				MeshInstanceManager.UpdateHelperSurfaceVisibility();
				RealtimeCSG.CSGSettings.Save();
				CSG_EditorGUIUtility.RepaintAll();
			}
		}

		public static void Button(SceneView sceneView, Rect currentRect)
		{
			if (s_Flags == null)
				s_Flags = new Flags();
			
			var helperSurfaces	= (int)RealtimeCSG.CSGSettings.VisibleHelperSurfaces;
			var style			= EditorStyles.toolbarDropDown;
			GUIContent buttonContent = s_Flags.mixedValueContent;
			if (!EditorGUI.showMixedValue)
			{
				var allVisibleSurfaces	= (int)HelperSurfaceFlags.ShowVisibleSurfaces;
				var allHelperSurfaces	= s_Flags.allSurfaces & ~allVisibleSurfaces;

				if (helperSurfaces == 0) { buttonContent = s_Flags.nothingContent; style = CSG_GUIStyleUtility.Skin.redToolbarDropDown; } else
				if (helperSurfaces == allVisibleSurfaces) buttonContent = s_Flags.onlyVisibleContent; else
				if (helperSurfaces == allHelperSurfaces) buttonContent = s_Flags.allHelperContent; else
				{
					int count = 0;
					for (int i = 0; i < s_Flags.helperSurfaceFlagContent.Length; i++)
					{
						var flag = s_Flags.helperSurfaceFlagValues[i];
						if ((helperSurfaces & flag) != 0)
						{
							count++;
							buttonContent = s_Flags.helperSurfaceFlagContent[i];
							if (count > 1) 
								break;
						}
					} 
					buttonContent = s_Flags.mixedContent;
				}
			}
			 
			if (GUI.Button(currentRect, buttonContent, style))
			{
				PopupWindow.Show(currentRect, new SurfaceVisibilityPopup(sceneView.camera));
			}
		}
	}
}
