using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using InternalRealtimeCSG;
using RealtimeCSG.Legacy;
using RealtimeCSG.Components;
using RealtimeCSG.Foundation;

namespace RealtimeCSG
{
	internal sealed class EditModeManager : ScriptableObject
	{
		static EditModeManager instance = null;
		
		[NonSerialized ] bool				generateMode			= false;		
		[NonSerialized ] FilteredSelection	filteredSelection = new FilteredSelection();
		
		[SerializeField] ToolEditMode		editMode				= ToolEditMode.Place;			
		[SerializeField] IEditMode			activeTool				= null;

		static IEditMode[]					brushTools              = null;

		
		public static FilteredSelection	FilteredSelection	{ get { InitTools(); return instance.filteredSelection; } }
		public static IEditMode			ActiveTool			{ get { InitTools(); return instance.activeTool; } }
			
		
		public static ToolEditMode EditMode
		{
			get
			{
				InitTools();
				return instance.editMode;
			}
			set
			{
				if (instance.editMode == value)
					return;

				Undo.RecordObject(instance, "Changed edit mode");

				instance.editMode = value;
				instance.generateMode = false; 
				
				RealtimeCSG.CSGSettings.EditMode = instance.editMode;
				RealtimeCSG.CSGSettings.Save();

				if (ActiveTool != null)
					CSG_EditorGUIUtility.RepaintAll();
			}
		}

		static public IEditMode CurrentTool
		{
			get 
			{
				InitTools();

				if (instance.generateMode)
					return brushTools[(int)ToolEditMode.Generate] as EditModeGenerate;
				
				var editMode = instance.editMode;
				if (editMode < firstEditMode ||
					editMode > lastEditModes)
					return brushTools[0];

				return brushTools[(int)editMode];
			}
		}

		
		static ToolEditMode firstEditMode;
		static ToolEditMode lastEditModes;

		const HideFlags scriptableObjectHideflags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor | HideFlags.NotEditable | HideFlags.HideInHierarchy;

		static void InitTools()
		{
			if (instance)
				return;


			var values = Enum.GetValues(typeof(ToolEditMode)).Cast<ToolEditMode>().ToList();
			values.Sort();
			firstEditMode = values[0];
			lastEditModes = values[values.Count - 1];
			
			Undo.undoRedoPerformed -= UndoRedoPerformed;
			Undo.undoRedoPerformed += UndoRedoPerformed;

			EditorApplication.modifierKeysChanged -= OnModifierKeysChanged;
			EditorApplication.modifierKeysChanged += OnModifierKeysChanged;


			var managers = Resources.FindObjectsOfTypeAll<EditModeManager>().ToArray();
			for (int i = 0; i < managers.Length; i++)
				DestroyImmediate(managers[i]);
			instance = ScriptableObject.CreateInstance<EditModeManager>();
			instance.hideFlags = scriptableObjectHideflags;

			var types = new Type[]
			{
				typeof(EditModePlace),
				typeof(EditModeGenerate),
				typeof(EditModeMeshEdit),
				typeof(EditModeClip),
				typeof(EditModeSurface)
			};
			if (types.Length != values.Count)
			{
				Debug.LogWarning("types.Length != values.Count");
			}

			brushTools = new IEditMode[values.Count];
			for (int j = 0; j < types.Length; j++)
			{
				var objects = Resources.FindObjectsOfTypeAll(types[j]).ToArray();
				for (int i = 0; i < objects.Length; i++)
					DestroyImmediate(objects[i]);

				var obj = ScriptableObject.CreateInstance(types[j]);
				brushTools[j] = obj as IEditMode;
				if (brushTools[j] == null)
				{
					Debug.LogWarning("brushTools[j] == null");
					continue;
				}
				if (!(brushTools[j] is ScriptableObject))
				{
					Debug.LogWarning("!(brushTools[j] is ScriptableObject)");
					continue;
				}
				obj.hideFlags = scriptableObjectHideflags;
			}

			EditModeGenerate.ShapeCommitted -= OnShapeCommittedEvent;
			EditModeGenerate.ShapeCommitted += OnShapeCommittedEvent;
			EditModeGenerate.ShapeCancelled -= OnShapeCancelledEvent;
			EditModeGenerate.ShapeCancelled += OnShapeCancelledEvent;
			
			RealtimeCSG.CSGSettings.Reload();
			instance.editMode = RealtimeCSG.CSGSettings.EditMode;

			EditModeManager.UpdateSelection(true); 
			InitTargets();
		}
				
		static HashSet<CSGNode>		selectedNodes = new HashSet<CSGNode>();
		static HashSet<Transform>	selectedOthers = new HashSet<Transform>();
		public static void UpdateSelection(bool forceUpdate = false)
		{
			InitTools();
			
			GetTargetSelection(ref selectedNodes, ref selectedOthers);
				
			if (!instance.filteredSelection.UpdateSelection(selectedNodes, selectedOthers) &&
				!forceUpdate)
			{
				return;
			}
			
			InternalCSGModelManager.skipCheckForChanges = true;
			try
			{
				InternalCSGModelManager.CheckForChanges(); 
			}
			finally
			{
				InternalCSGModelManager.skipCheckForChanges = false;
			}

			foreach (var tool in brushTools)
				tool.SetTargets(instance.filteredSelection);
		}
		
		public static void ResetMessage()
		{
			if (SceneView.lastActiveSceneView     != null) { SceneView.lastActiveSceneView.RemoveNotification(); return; }
			if (SceneView.currentDrawingSceneView != null) { SceneView.lastActiveSceneView.RemoveNotification(); return; }
		}

		public static void ShowMessage(string message)
		{
			if (string.IsNullOrEmpty(message))
				return;

			if (SceneView.lastActiveSceneView     != null) { SceneView.lastActiveSceneView.ShowNotification(new GUIContent(message)); return; }
			if (SceneView.currentDrawingSceneView != null) { SceneView.lastActiveSceneView.ShowNotification(new GUIContent(message)); return; }
			Debug.LogWarning(message);
		}
				
		static void OnEnableTool(IEditMode tool)
		{
			if (tool != null)
				tool.OnEnableTool();
		}

		static void OnDisableTool(IEditMode tool)
		{ 
			if (tool != null)
				tool.OnDisableTool();
		}

		public static bool DeselectAll()
		{
			if (instance.activeTool != null &&
				instance.activeTool.DeselectAll())
				return true;
			return false;
		}
		
		static void UndoRedoPerformed()
		{
			if (EditorApplication.isPlayingOrWillChangePlaymode)
				return;

			if (instance.activeTool != null)
			{
				instance.activeTool.UndoRedoPerformed();
			}

			EditModeManager.UpdateSelection(forceUpdate: true);
		}

		static void OnModifierKeysChanged()
		{
			if (EditorApplication.isPlayingOrWillChangePlaymode)
				return;

			CSG_EditorGUIUtility.RepaintAll();
		}
		
		static bool GetTargetSelection(ref HashSet<CSGNode> nodes, ref HashSet<Transform> others)
		{
			selectedNodes.Clear();
			selectedOthers.Clear();

			if (Selection.gameObjects == null)
				return false;
			
			foreach (var gameObject in Selection.gameObjects)
			{
				if (!gameObject)
					continue;
				var node = gameObject.GetComponent<CSGNode>();
				if (node && node.enabled && (node.hideFlags & HideFlags.HideInInspector) == HideFlags.None)
					nodes.Add(node);
				else
					others.Add(gameObject.transform);
			}
			return true;
		}

		static void InitTargets()
		{
			var newTool = CurrentTool;
			if (newTool == instance.activeTool)
			{
				if (instance.filteredSelection.NodeTargets != null && 
					instance.filteredSelection.NodeTargets.Length > 0)
					OnEnableTool(instance.activeTool);
				else
					OnDisableTool(instance.activeTool);
			} else
			{
				UpdateTool();
			}
		}

		static void NextEditMode()
		{
			InitTools();
			if (instance.editMode == lastEditModes)
			{
				EditMode = firstEditMode;
			} else
			{
				EditMode = (ToolEditMode)(instance.editMode + 1);
			}
		}

		static void PrevEditMode()
		{
			InitTools();
			if (instance.editMode == firstEditMode)
			{
				EditMode = lastEditModes;
			} else
			{
				EditMode = (ToolEditMode)(instance.editMode - 1);
			}
		}

		public static void UpdateTool()
		{
			if (EditorApplication.isPlayingOrWillChangePlaymode)
				return;

            IEditMode newTool;
            if (!RealtimeCSG.CSGSettings.EnableRealtimeCSG)
                newTool = null;
            else
                newTool = CurrentTool;
			if (instance.activeTool != newTool)
			{
				if (instance.activeTool != null)
					OnDisableTool(instance.activeTool);

				instance.activeTool		= newTool;

				if (newTool != null)
					OnEnableTool(newTool);
			}
		}
		
		static void FinishShapeBuilder()
		{	   
			Tools.hidden = false;
		}
		
		public static Vector3 SnapPointToGrid(Camera camera, Vector3 point, CSGPlane plane, ref List<Vector3> snappingEdges, out CSGBrush snappedOnBrush, CSGBrush[] ignoreBrushes, bool ignoreAllBrushes = false)
		{
			snappedOnBrush = null;
            // Note: relative snapping wouldn't make sense here since it's a single point that's being snapped and there is no relative movement
			var activeSnappingMode = RealtimeCSG.CSGSettings.ActiveSnappingMode;
			 
			var snappedPoint = point;
            switch(activeSnappingMode)
            {
                case SnapMode.RelativeSnapping: // TODO: fixme
                case SnapMode.GridSnapping:
			    {
				    snappedPoint = snappedPoint + RealtimeCSG.CSGGrid.ForceSnapDeltaToGrid(camera, MathConstants.zeroVector3, snappedPoint);
				    snappedPoint = RealtimeCSG.CSGGrid.PointFromGridSpace(RealtimeCSG.CSGGrid.CubeProject(camera, RealtimeCSG.CSGGrid.PlaneToGridSpace(plane), RealtimeCSG.CSGGrid.PointToGridSpace(snappedPoint)));

				    // snap twice to get rid of some tiny movements caused by the projection in depth	
				    snappedPoint = snappedPoint + RealtimeCSG.CSGGrid.ForceSnapDeltaToGrid(camera, MathConstants.zeroVector3, snappedPoint);
				    snappedPoint = RealtimeCSG.CSGGrid.PointFromGridSpace(RealtimeCSG.CSGGrid.CubeProject(camera, RealtimeCSG.CSGGrid.PlaneToGridSpace(plane), RealtimeCSG.CSGGrid.PointToGridSpace(snappedPoint)));

                    if (!ignoreAllBrushes)
                        return GridUtility.SnapToWorld(camera, plane, point, snappedPoint, ref snappingEdges, out snappedOnBrush, ignoreBrushes);

                    return snappedPoint;
                }
                default:
                case SnapMode.None:
			    {
				    snappedPoint = GeometryUtility.ProjectPointOnPlane(plane, snappedPoint);
                    return snappedPoint;
                }
            }
		}

		public static Vector3 SnapPointToRay(Camera camera, Vector3 point, Ray ray, ref List<Vector3> snappingEdges, out CSGBrush snappedOnBrush)
		{
			snappedOnBrush = null;
			
			var snappedPoint = point;
			
			snappingEdges = null;
            // Note: relative snapping wouldn't make sense here since it's a single point that's being snapped and there is no relative movement
            var doGridSnapping	= RealtimeCSG.CSGSettings.ActiveSnappingMode != SnapMode.None;
			if (doGridSnapping)
			{
				var delta = RealtimeCSG.CSGGrid.ForceSnapDeltaToRay(camera, ray, MathConstants.zeroVector3, snappedPoint);
				snappedPoint = snappedPoint + delta;
			}
			return snappedPoint;

		}


		static void OnShapeCancelledEvent()
		{
			if (!instance.generateMode)
				return;
			instance.generateMode = false;
		}

		static void OnShapeCommittedEvent()
		{
			instance.generateMode = false;
		}
		
		static bool HandleBuilderEvents()
		{
			if (RealtimeCSG.CSGSettings.EditMode != instance.editMode)
			{
				RealtimeCSG.CSGSettings.EditMode = instance.editMode;
				RealtimeCSG.CSGSettings.Save();
			}
			
			switch (Event.current.type) 
			{
				case EventType.KeyDown:
				{
					if (GUIUtility.hotControl == 0 &&
						Keys.FreeBuilderMode.IsKeyPressed() && !instance.generateMode)
					{
						var generateBrushTool = brushTools[(int)ToolEditMode.Generate] as EditModeGenerate;
						generateBrushTool.BuilderMode = ShapeMode.FreeDraw;
						instance.generateMode = true;
						Event.current.Use();
						return true;
					}
					if (Keys.CylinderBuilderMode.IsKeyPressed() && !instance.generateMode)
					{
						var generateBrushTool = brushTools[(int)ToolEditMode.Generate] as EditModeGenerate;
						generateBrushTool.BuilderMode = ShapeMode.Cylinder;
						instance.generateMode = true;
						Event.current.Use();
						return true;
					}
					if (Keys.BoxBuilderMode.IsKeyPressed() && !instance.generateMode)
					{
						var generateBrushTool = brushTools[(int)ToolEditMode.Generate] as EditModeGenerate;
						generateBrushTool.BuilderMode = ShapeMode.Box;
						instance.generateMode = true;
						Event.current.Use();
						return true;
					}
					if (Keys.SphereBuilderMode.IsKeyPressed() && !instance.generateMode)
					{
						var generateBrushTool = brushTools[(int)ToolEditMode.Generate] as EditModeGenerate;
						generateBrushTool.BuilderMode = ShapeMode.Sphere;
						instance.generateMode = true;
						Event.current.Use();
						return true;
					}
					else if (Keys.SwitchToObjectEditMode	.IsKeyPressed()) { Event.current.Use(); return true; }
					else if (Keys.SwitchToGenerateEditMode	.IsKeyPressed()) { Event.current.Use(); return true; }
					else if (Keys.SwitchToMeshEditMode		.IsKeyPressed()) { Event.current.Use(); return true; }
					else if (Keys.SwitchToClipEditMode		.IsKeyPressed()) { Event.current.Use(); return true; }
					else if (Keys.SwitchToSurfaceEditMode	.IsKeyPressed()) { Event.current.Use(); return true; }
					break;
				}
				case EventType.KeyUp:
				{
					if (instance.generateMode &&
						(Keys.FreeBuilderMode.IsKeyPressed() ||
						Keys.CylinderBuilderMode.IsKeyPressed() ||
						Keys.BoxBuilderMode.IsKeyPressed() ||
						Keys.SphereBuilderMode.IsKeyPressed()))
					{
						Event.current.Use();
						var generateBrushTool = brushTools[(int)ToolEditMode.Generate] as EditModeGenerate;
						if (!generateBrushTool.HotKeyReleased())
						{
							instance.generateMode = false;
						}
					}
					else if (Keys.SwitchToObjectEditMode	.IsKeyPressed()) { InitTools(); EditMode = ToolEditMode.Place; Event.current.Use(); return true; }
					else if (Keys.SwitchToGenerateEditMode	.IsKeyPressed()) { InitTools(); EditMode = ToolEditMode.Generate; Event.current.Use(); return true; }
					else if (Keys.SwitchToMeshEditMode		.IsKeyPressed()) { InitTools(); EditMode = ToolEditMode.Edit; Event.current.Use(); return true; }
					else if (Keys.SwitchToClipEditMode		.IsKeyPressed()) { InitTools(); EditMode = ToolEditMode.Clip; Event.current.Use(); return true; }
					else if (Keys.SwitchToSurfaceEditMode	.IsKeyPressed()) { InitTools(); EditMode = ToolEditMode.Surfaces; Event.current.Use(); return true; }
					break;
				}
				case EventType.ValidateCommand:
				{
					if ((GUIUtility.hotControl == 0 && Keys.FreeBuilderMode  .IsKeyPressed()) ||
						Keys.CylinderBuilderMode.IsKeyPressed() ||
						Keys.SphereBuilderMode.IsKeyPressed() ||
						Keys.BoxBuilderMode.IsKeyPressed() ||
						Keys.SwitchToObjectEditMode.IsKeyPressed() ||
						Keys.SwitchToGenerateEditMode.IsKeyPressed() ||
						Keys.SwitchToMeshEditMode.IsKeyPressed() ||
						Keys.SwitchToClipEditMode.IsKeyPressed() ||
						Keys.SwitchToSurfaceEditMode.IsKeyPressed())
					{
						Event.current.Use();
						return true;
					}
					break;
				}
			}
			return false;
		}

		[NonSerialized] LineMeshManager zTestLineMeshManager	= new LineMeshManager();
		[NonSerialized] LineMeshManager noZTestLineMeshManager	= new LineMeshManager();

		void OnDestroy()
		{
			zTestLineMeshManager.Destroy();
			noZTestLineMeshManager.Destroy();
		} 

		private static readonly int SceneWindowHash		= "SceneWindowHash".GetHashCode();

		static List<EditModeToolWindowSceneGUI>  currentEditorWindows = new List<EditModeToolWindowSceneGUI>();

		public static bool InitSceneGUI(SceneView sceneView)
		{
			currentEditorWindows	= EditModeToolWindowSceneGUI.GetEditorWindows();

			return (currentEditorWindows.Count == 0);
		}
		
		public static void OnSceneGUI(SceneView sceneView)
		{
			if (EditorApplication.isPlayingOrWillChangePlaymode)
				return;

			CameraUtility.InitDistanceChecks(sceneView);
			SelectionUtility.HandleEvents();
			InitTools();

			HandleBuilderEvents();
			{
				UpdateTool();
				
				if (instance.activeTool != null)
				{
					if (RealtimeCSG.CSGSettings.EnableRealtimeCSG)
					{
						// handle the tool
						var sceneSize = sceneView.position.size;
						var sceneRect = new Rect(0, 0, sceneSize.x, sceneSize.y - ((CSG_GUIStyleUtility.BottomToolBarHeight + 4) + 17));
						
                        // This helps prevent weird issues with overlapping sceneviews + avoid some performance issues with multiple sceneviews open
                        if (EditorWindow.mouseOverWindow == sceneView || (Event.current.type != EventType.MouseMove && Event.current.type != EventType.Layout))
                            instance.activeTool.HandleEvents(sceneView, sceneRect);
					} else
					{
						if (Event.current.type == EventType.Repaint)
						{
							var brushes			= instance.filteredSelection.BrushTargets;
							var wireframes		= new List<GeometryWireframe>(brushes.Length);
							var transformations = new List<Matrix4x4>(brushes.Length);
							for (int i = 0; i < brushes.Length; i++)
							{
								var brush = brushes[i];
								if (!brush)
									continue;

								if (brush.ChildData == null ||
									!brush.ChildData.Model)
									continue;

								var brushTransformation = brush.compareTransformation.localToWorldMatrix;

								wireframes.Add(BrushOutlineManager.GetBrushOutline(brushes[i].brushNodeID));
								transformations.Add(brushTransformation);
							}
							if (wireframes.Count > 0)
							{
								CSGRenderer.DrawSelectedBrushes(instance.zTestLineMeshManager, instance.noZTestLineMeshManager,
									wireframes.ToArray(), transformations.ToArray(),
									ColorSettings.SelectedOutlines, GUIConstants.thickLineScale);
							}
							MaterialUtility.LineDashMultiplier = 1.0f;
							MaterialUtility.LineThicknessMultiplier = 1.0f;
							MaterialUtility.LineAlphaMultiplier = 1.0f;
							instance.zTestLineMeshManager.Render(MaterialUtility.ZTestGenericLine);
							instance.zTestLineMeshManager.Render(MaterialUtility.ZTestGenericLine);
						}
					}
				}
			}

			int sceneWindowId	= GUIUtility.GetControlID (SceneWindowHash, FocusType.Passive);			
			var sceneWindowType = Event.current.GetTypeForControl(sceneWindowId);
			if (sceneWindowType == EventType.Repaint)
			{
				if (currentEditorWindows.Count > 0)
				{
					for (int i = 0; i < currentEditorWindows.Count; i++)
						currentEditorWindows[i].Repaint();
					return;
				}
			}

			if (sceneWindowType == EventType.MouseMove)
			{
				SceneDragToolManager.IsDraggingObjectInScene = false;
			}

			if (RealtimeCSG.CSGSettings.EnableRealtimeCSG)
			{
				if (sceneView && sceneWindowType != EventType.Used && !SceneDragToolManager.IsDraggingObjectInScene)
				{
					if (currentEditorWindows.Count == 0)
					{
						try
						{
							Handles.BeginGUI();
							Rect windowRect = new Rect(Vector2.zero, sceneView.position.size); 
							EditModeSelectionGUI.HandleWindowGUI(windowRect);
						}
						finally
						{
							Handles.EndGUI();
						}
					}
				}
			}
		}


		public static void GenerateFromSurface(Camera camera, CSGBrush cSGBrush, CSGPlane polygonPlane, Vector3 direction, Vector3[] points, int[] pointIndices, uint[] smoothingGroups, bool drag, CSGOperationType forceDragSource, bool autoCommitExtrusion)
		{
			EditModeManager.EditMode = ToolEditMode.Generate;
			UpdateTool();
			var generateBrushTool = brushTools[(int)ToolEditMode.Generate] as EditModeGenerate;
			generateBrushTool.GenerateFromPolygon(camera, cSGBrush, polygonPlane, direction, points, pointIndices, smoothingGroups, drag, forceDragSource, autoCommitExtrusion);
		}

		public delegate void SetTransformation(Transform newTransform, Transform originalTransform);

				
		public static Transform[] CloneTargets(SetTransformation setTransform = null)
		{
			if (instance.filteredSelection.NodeTargets.Length == 0)
				return new Transform[0];

			var groupId = Undo.GetCurrentGroup();
			//Undo.IncrementCurrentGroup();

			var newTargets		= new GameObject[instance.filteredSelection.NodeTargets.Length];
			var newTransforms	= new Transform[instance.filteredSelection.NodeTargets.Length];
			for (int i = 0; i < instance.filteredSelection.NodeTargets.Length; i++)
			{
				var originalGameObject	= instance.filteredSelection.NodeTargets[i].gameObject;
				var originalTransform	= originalGameObject.GetComponent<Transform>();
				
				newTargets[i] = CSGPrefabUtility.Instantiate(originalGameObject);
				var newTransform = newTargets[i].GetComponent<Transform>();
				if (originalTransform.parent != null)
				{
					newTransform.SetParent(originalTransform.parent, false);
					newTransform.SetSiblingIndex(originalTransform.GetSiblingIndex() + 1);
					newTransform.name = GameObjectUtility.GetUniqueNameForSibling(originalTransform.parent, originalTransform.name);
				}
				if (setTransform == null)
				{
					newTransform.localScale		= originalTransform.localScale;
					newTransform.localPosition	= originalTransform.localPosition;
					newTransform.localRotation	= originalTransform.localRotation;
				} else
					setTransform(newTransform, originalTransform);

				var childBrushes = newTargets[i].GetComponentsInChildren<CSGBrush>();

				Dictionary<uint, uint> uniqueSmoothingGroups = new Dictionary<uint, uint>();
				foreach (var childBrush in childBrushes)
				{
					for (int g = 0; g < childBrush.Shape.TexGens.Length; g++)
					{
						var smoothingGroup = childBrush.Shape.TexGens[g].SmoothingGroup;
						if (smoothingGroup == 0)
							continue;

						uint newSmoothingGroup;
						if (!uniqueSmoothingGroups.TryGetValue(smoothingGroup, out newSmoothingGroup))
						{
							newSmoothingGroup = SurfaceUtility.FindUnusedSmoothingGroupIndex();
							uniqueSmoothingGroups[smoothingGroup] = newSmoothingGroup;
						}

						childBrush.Shape.TexGens[g].SmoothingGroup = newSmoothingGroup;
					}
				}

				newTransforms[i] = newTransform;
				Undo.RegisterCreatedObjectUndo(newTargets[i], "Created clone of " + originalGameObject.name);
			}

			Selection.objects = newTargets;
			Undo.CollapseUndoOperations(groupId);

			return newTransforms;
		}
	}
}
