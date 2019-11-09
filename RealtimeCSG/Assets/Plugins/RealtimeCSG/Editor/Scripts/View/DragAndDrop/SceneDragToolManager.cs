using UnityEngine;
using UnityEditor;
using InternalRealtimeCSG;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using System.Linq;
using UnityEngine.SceneManagement;

namespace RealtimeCSG
{
	internal sealed class SceneDragToolManager
	{
		public static bool IsDraggingObjectInScene { get; internal set; }

		static SceneDragToolMeshDragOnSurface	meshDragTool				= new SceneDragToolMeshDragOnSurface();
		static SceneDragToolBrushDragOnSurface	brushDragTool				= new SceneDragToolBrushDragOnSurface();
		static SceneDragToolMaterials			materialDragTool			= new SceneDragToolMaterials();

		static ISceneDragTool					currentDragTool				= null;
		static bool								currentDragToolActive		= false;
		static Transform						currentTransformInInspector = null;
		static bool								draggingInScene				= false;

		internal static void UpdateDragAndDrop()
		{

			// TODO: never use drag & drop code when dropping into inspector
			//			instead:
			//			find 'new' components, check if they're part of a prefab, 
			//			check if that prefab has a copy flag, and replace it with a copy


			if (currentTransformInInspector)
			{
				if (currentDragTool != null)
				{
					DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
					if (currentDragToolActive)
					{
						currentDragTool.Reset();
					}
					currentDragTool = null;
					currentTransformInInspector = null;
					draggingInScene = false;
				}
				currentTransformInInspector = null;
			}
		}

		internal static void OnPaint()
		{
			if (currentDragTool != null)
				currentDragTool.OnPaint();
		}

		static void ValidateDrop(bool inSceneView, Transform transformInInspector)
		{
			if (currentDragTool != null)
				currentDragTool.Reset(); 
			currentDragTool = null;
			currentDragToolActive = false;
			currentTransformInInspector = transformInInspector;
			if (materialDragTool.ValidateDrop(inSceneView))
			{
				currentDragTool = materialDragTool;
			} else
			if (brushDragTool.ValidateDrop(inSceneView))
			{
				currentDragTool = brushDragTool;
			} else
			if (meshDragTool.ValidateDrop(inSceneView))
			{
				currentDragTool = meshDragTool;
			}
		}

		internal static void OnHandleDragAndDrop(bool inSceneView, Transform transformInInspector = null, Rect? selectionRect = null)
		{
			switch (Event.current.type)
			{
				case EventType.DragUpdated:
				{
					if (!draggingInScene)
					{
						ValidateDrop(inSceneView, transformInInspector);
					}

					if (currentDragTool != null)
					{
						if (!currentDragTool.ValidateDropPoint(inSceneView))
						{
							if (currentDragTool != null && currentDragToolActive)
							{
								currentDragTool.DragExited(inSceneView);
							}
							currentDragToolActive = false;
						} else
						{
							currentDragToolActive = true;
							IsDraggingObjectInScene = true;
							DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
							if (inSceneView)
							{
								if (currentDragTool.DragUpdated())
								{
									HandleUtility.Repaint();
								}
							} else
							{
								if (currentDragTool.DragUpdated(transformInInspector, selectionRect.Value))
								{
									CSG_EditorGUIUtility.UpdateSceneViews();
								}
							}
							Event.current.Use();
							draggingInScene = true;
						}
					}
					break;
				}
				case EventType.DragPerform:
				{
					if (currentDragTool != null)
					{
						DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
						if (currentDragToolActive)
						{
							currentDragTool.DragPerform(inSceneView);
							currentDragTool.Reset();
							Event.current.Use();
						}
						currentDragTool = null;
						currentTransformInInspector = null;
						draggingInScene = false;
					}
					break;
				}
				case EventType.DragExited:
				//case EventType.MouseMove:
				{
					if (currentDragTool != null)
					{
						currentDragTool.DragExited(inSceneView);
						Event.current.Use();
						IsDraggingObjectInScene = false;
						currentDragTool = null;
						currentTransformInInspector = null;
						draggingInScene = false;
						CSG_EditorGUIUtility.UpdateSceneViews();
					}
					break;
				}
			}
		}
	}
}