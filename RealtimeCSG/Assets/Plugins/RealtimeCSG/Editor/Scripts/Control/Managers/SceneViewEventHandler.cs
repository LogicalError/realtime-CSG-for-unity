using UnityEngine;
using UnityEditor;
using InternalRealtimeCSG;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using System.Linq;
using UnityEngine.SceneManagement;
using RealtimeCSG.Helpers;

namespace RealtimeCSG
{
	internal sealed class SceneViewEventHandler
	{
		static bool mousePressed;

        static int prevFocusControl;

		internal static void OnScene(SceneView sceneView)
		{
            CSGSettings.RegisterSceneView(sceneView);
            if (!RealtimeCSG.CSGSettings.EnableRealtimeCSG)
				return;

			if (EditorApplication.isPlayingOrWillChangePlaymode)
				return;
			UpdateLoop.UpdateOnSceneChange();

			if (!RealtimeCSG.CSGSettings.EnableRealtimeCSG)
				ColorSettings.isInitialized = false;
			else
			if (!ColorSettings.isInitialized)
			{
				if (Event.current.type == EventType.Repaint)
				{
					ColorSettings.Update();
				}
			}

			if (!UpdateLoop.IsActive())
				UpdateLoop.ResetUpdateRoutine();

			if (Event.current.type == EventType.MouseDown ||
				Event.current.type == EventType.MouseDrag) { mousePressed = true; }
			else if (Event.current.type == EventType.MouseUp ||
				Event.current.type == EventType.MouseMove) { mousePressed = false; }

			SceneDragToolManager.OnHandleDragAndDrop(sceneView);
			RectangleSelectionManager.Update(sceneView);
			EditModeManager.InitSceneGUI(sceneView);

			if (Event.current.type == EventType.Repaint)
				MeshInstanceManager.UpdateHelperSurfaces();

			if (Event.current.type == EventType.Repaint)
			{
				SceneToolRenderer.OnPaint(sceneView);
			} else
			//if (fallbackGUI)
			{
				SceneViewBottomBarGUI.ShowGUI(sceneView);
                SceneViewInfoGUI.DrawInfoGUI( sceneView );
			}

			EditModeManager.OnSceneGUI(sceneView);

			//if (fallbackGUI)
			{
				TooltipUtility.InitToolTip(sceneView);
				if (Event.current.type == EventType.Repaint)
				{
					SceneViewBottomBarGUI.ShowGUI(sceneView);
                	SceneViewInfoGUI.DrawInfoGUI( sceneView );
				}
				if (!mousePressed)
				{
					Handles.BeginGUI();
					TooltipUtility.DrawToolTip(getLastRect: false);
					Handles.EndGUI();
				}
			}

            if (Event.current.type == EventType.Layout)
            {
                var currentFocusControl = CSGHandles.FocusControl;
                if (prevFocusControl != currentFocusControl)
                {
                    prevFocusControl = currentFocusControl;
                    HandleUtility.Repaint();
                }
            }
		}
	}
}
