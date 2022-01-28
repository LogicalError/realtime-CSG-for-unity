using System;
using UnityEditor;
using UnityEngine;
using System.Linq;
using RealtimeCSG.Foundation;
using RealtimeCSG.Legacy;
using RealtimeCSG.Components;

namespace RealtimeCSG
{
	[Serializable]
	internal enum ShapeMode
	{
		FreeDraw,
		Box,
		Sphere,
		Cylinder,
//		SpiralStairs,
		LinearStairs,

		Last = LinearStairs
	}

	internal sealed class EditModeGenerate : ScriptableObject, IEditMode
	{
		public bool UsesUnitySelection	{ get { return false; } }
		public bool IgnoreUnityRect		{ get { return true; } }

		public static event Action ShapeCommitted = null;
		public static event Action ShapeCancelled = null;
		
		public ShapeMode BuilderMode
		{
			get
			{
				return builderMode;
			}
			set
			{
				if (builderMode == value)
					return;
				builderMode = value;
				RealtimeCSG.CSGSettings.ShapeBuildMode = builderMode;
				RealtimeCSG.CSGSettings.Save();
				ResetTool();
			}
		}
		
		IGenerator InternalCurrentGenerator
		{
			get
			{ 
				switch (builderMode)
				{
					default:
					case ShapeMode.FreeDraw:
					{
						return freedrawGenerator;
					}
					case ShapeMode.Cylinder:
					{
						return cylinderGenerator;
					}
					case ShapeMode.Box:
					{
						return boxGenerator;
                    }
                    case ShapeMode.Sphere:
                    {
                        return sphereGenerator;
                    }
					case ShapeMode.LinearStairs:
					{
						return linearStairsGenerator;
					}/*
					case ShapeMode.SpiralStairs:
					{
						return spiralStairsGenerator;
					}*/
                }
			}
		}

		public IGenerator CurrentGenerator
		{
			get
			{
				var generator = InternalCurrentGenerator;
				var obj = generator as ScriptableObject;
				if (obj != null && obj)
					return generator;				
				ResetTool();
				return generator;
			}
		}
		
		[SerializeField] CSGBrush[]			brushes				= new CSGBrush[0];	

		[SerializeField] ShapeMode			builderMode			= ShapeMode.FreeDraw;

		[SerializeField] GeneratorFreeDraw		freedrawGenerator;
		[SerializeField] GeneratorCylinder		cylinderGenerator;
		[SerializeField] GeneratorLinearStairs	linearStairsGenerator;
		[SerializeField] GeneratorSpiralStairs	spiralStairsGenerator;
        [SerializeField] GeneratorSphere		sphereGenerator;
        [SerializeField] GeneratorBox			boxGenerator;

		[NonSerialized] bool				isEnabled		= false;
		[NonSerialized] bool				hideTool		= false;

		public void SetTargets(FilteredSelection filteredSelection)
		{
			hideTool = filteredSelection.NodeTargets.Length > 0;
			brushes = filteredSelection.GetAllContainedBrushes().ToArray();
			lastLineMeshGeneration--;
			if (isEnabled)
				Tools.hidden = hideTool;
		}

		void OnEnable()
		{
			RealtimeCSG.CSGSettings.Reload();
			builderMode = RealtimeCSG.CSGSettings.ShapeBuildMode;
		}

		public void OnEnableTool()
		{
			isEnabled		= true;
			Tools.hidden	= hideTool;
			ResetTool();
		}
		
		public void OnDisableTool()
		{
            // Ensures that a commit is propertly finished when the toolmode is changed
            if (CurrentGenerator.CanCommit)
            {
                var genBase = InternalCurrentGenerator as GeneratorBase;
                if (genBase != null)
                {
                    genBase.EndCommit();
                }
            }

            isEnabled = false;
			Tools.hidden = false;
			ResetTool();
		}

		void ResetTool()
		{
			RealtimeCSG.CSGGrid.ForceGrid = false;
			if (!freedrawGenerator)
			{
				freedrawGenerator = ScriptableObject.CreateInstance<GeneratorFreeDraw>();
				freedrawGenerator.snapFunction		= EditModeManager.SnapPointToGrid;
				freedrawGenerator.raySnapFunction	= EditModeManager.SnapPointToRay;
				freedrawGenerator.shapeCancelled	= OnShapeCancelledEvent;
				freedrawGenerator.shapeCommitted	= OnShapeCommittedEvent;
			}
			if (!cylinderGenerator)
			{
				cylinderGenerator = ScriptableObject.CreateInstance<GeneratorCylinder>();
				cylinderGenerator.snapFunction		= EditModeManager.SnapPointToGrid;
				cylinderGenerator.raySnapFunction	= EditModeManager.SnapPointToRay;
				cylinderGenerator.shapeCancelled	= OnShapeCancelledEvent;
				cylinderGenerator.shapeCommitted	= OnShapeCommittedEvent;
			}
			if (!boxGenerator)
			{
				boxGenerator = ScriptableObject.CreateInstance<GeneratorBox>();
				boxGenerator.snapFunction			= EditModeManager.SnapPointToGrid;
				boxGenerator.raySnapFunction		= EditModeManager.SnapPointToRay;
				boxGenerator.shapeCancelled			= OnShapeCancelledEvent;
				boxGenerator.shapeCommitted			= OnShapeCommittedEvent;
            }
            if (!sphereGenerator)
            {
                sphereGenerator = ScriptableObject.CreateInstance<GeneratorSphere>();
                sphereGenerator.snapFunction = EditModeManager.SnapPointToGrid;
                sphereGenerator.raySnapFunction = EditModeManager.SnapPointToRay;
                sphereGenerator.shapeCancelled = OnShapeCancelledEvent;
                sphereGenerator.shapeCommitted = OnShapeCommittedEvent;
            }
			if (!linearStairsGenerator)
			{
				linearStairsGenerator = ScriptableObject.CreateInstance<GeneratorLinearStairs>();
				linearStairsGenerator.snapFunction = EditModeManager.SnapPointToGrid;
				linearStairsGenerator.raySnapFunction = EditModeManager.SnapPointToRay;
				linearStairsGenerator.shapeCancelled = OnShapeCancelledEvent;
				linearStairsGenerator.shapeCommitted = OnShapeCommittedEvent;
			}
			if (!spiralStairsGenerator)
			{
				spiralStairsGenerator = ScriptableObject.CreateInstance<GeneratorSpiralStairs>();
				spiralStairsGenerator.snapFunction = EditModeManager.SnapPointToGrid;
				spiralStairsGenerator.raySnapFunction = EditModeManager.SnapPointToRay;
				spiralStairsGenerator.shapeCancelled = OnShapeCancelledEvent;
				spiralStairsGenerator.shapeCommitted = OnShapeCommittedEvent;
			}

            var generator = InternalCurrentGenerator;
			if (generator != null)
			{
				var obj = generator as ScriptableObject;
				if (obj)
					generator.Init();
			}
		}

		public bool HotKeyReleased()
		{
			if (CurrentGenerator == null)
				return false;
			return CurrentGenerator.HotKeyReleased();
		}

		void OnShapeCancelledEvent()
		{
			CurrentGenerator.Init();
			ShapeCancelled.Invoke();
		}

		void OnShapeCommittedEvent()
		{
			ShapeCommitted.Invoke();
		}
		
		public bool UndoRedoPerformed()
		{
			return CurrentGenerator.UndoRedoPerformed();
		}

		public bool DeselectAll()
		{
			CurrentGenerator.PerformDeselectAll();
			return true;
		}

		void SetOperationType(CSGOperationType operationType)
		{
			CurrentGenerator.CurrentCSGOperationType = operationType;
		}

		void OnDestroy()
		{
			zTestLineMeshManager.Destroy();
			noZTestLineMeshManager.Destroy();
		}

		LineMeshManager zTestLineMeshManager = new LineMeshManager();
		LineMeshManager noZTestLineMeshManager = new LineMeshManager();
		int lastLineMeshGeneration = -1;


		public void HandleEvents(SceneView sceneView, Rect sceneRect)
		{
			if (CurrentGenerator == null)
				return;

			CurrentGenerator.HandleEvents(sceneView, sceneRect); 
			switch (Event.current.type)
			{
				case EventType.ValidateCommand:
				{
					if (!EditorGUIUtility.editingTextField && Keys.MakeSelectedAdditiveKey    .IsKeyPressed()) { Event.current.Use(); break; }
					if (!EditorGUIUtility.editingTextField && Keys.MakeSelectedSubtractiveKey .IsKeyPressed()) { Event.current.Use(); break; }
					if (!EditorGUIUtility.editingTextField && Keys.MakeSelectedIntersectingKey.IsKeyPressed()) { Event.current.Use(); break; }
					if (!EditorGUIUtility.editingTextField && Keys.CancelActionKey            .IsKeyPressed()) { Event.current.Use(); break; }
					if (Keys.HandleSceneValidate(EditModeManager.CurrentTool, false)) { Event.current.Use(); HandleUtility.Repaint(); break; }
					break;
				}

				case EventType.KeyDown:
				{
					if (!EditorGUIUtility.editingTextField && Keys.MakeSelectedAdditiveKey    .IsKeyPressed()) { Event.current.Use(); break; }
					if (!EditorGUIUtility.editingTextField && Keys.MakeSelectedSubtractiveKey .IsKeyPressed()) { Event.current.Use(); break; }
					if (!EditorGUIUtility.editingTextField && Keys.MakeSelectedIntersectingKey.IsKeyPressed()) { Event.current.Use(); break; }
					if (!EditorGUIUtility.editingTextField && Keys.CancelActionKey            .IsKeyPressed()) { Event.current.Use(); break; }
					if (Keys.HandleSceneKeyDown(EditModeManager.CurrentTool, false)) { Event.current.Use(); HandleUtility.Repaint(); break; }
					break;
				}

				case EventType.KeyUp:
				{
					if (!EditorGUIUtility.editingTextField && Keys.MakeSelectedAdditiveKey    .IsKeyPressed()) { SetOperationType(CSGOperationType.Additive);     Event.current.Use(); break; }
					if (!EditorGUIUtility.editingTextField && Keys.MakeSelectedSubtractiveKey .IsKeyPressed()) { SetOperationType(CSGOperationType.Subtractive);  Event.current.Use(); break; }
					if (!EditorGUIUtility.editingTextField && Keys.MakeSelectedIntersectingKey.IsKeyPressed()) { SetOperationType(CSGOperationType.Intersecting); Event.current.Use(); break; }
					if (!EditorGUIUtility.editingTextField && Keys.CancelActionKey            .IsKeyPressed()) { CurrentGenerator.PerformDeselectAll(); Event.current.Use(); break; }
					if (Keys.HandleSceneKeyUp(EditModeManager.CurrentTool, false)) { Event.current.Use(); HandleUtility.Repaint(); break; }
					break;
				}

				case EventType.Repaint:
				{
					if (lastLineMeshGeneration != InternalCSGModelManager.MeshGeneration)
					{
						lastLineMeshGeneration = InternalCSGModelManager.MeshGeneration;

						var brushTransformation	= new Matrix4x4[brushes.Length];
						var brushNodeIDs		= new Int32[brushes.Length];
						for (int i = brushes.Length - 1; i >= 0; i--)
						{
							var brush = brushes[i];
							if (brush.brushNodeID == CSGNode.InvalidNodeID ||	// could be a prefab
								brush.compareTransformation == null ||
								brush.ChildData == null ||
								brush.ChildData.ModelTransform == null ||
								!brush.ChildData.ModelTransform)
							{
								ArrayUtility.RemoveAt(ref brushTransformation, i);
								ArrayUtility.RemoveAt(ref brushNodeIDs, i);
								continue;
							}
							brushTransformation[i] = brush.compareTransformation.localToWorldMatrix;
							brushNodeIDs[i] = brush.brushNodeID;
						}
						CSGRenderer.DrawSelectedBrushes(zTestLineMeshManager, noZTestLineMeshManager, brushNodeIDs, brushTransformation, 
							ColorSettings.SelectedOutlines, GUIConstants.lineScale);
					}
					
					MaterialUtility.LineAlphaMultiplier = 1.0f;
					MaterialUtility.LineDashMultiplier = 2.0f;
					MaterialUtility.LineThicknessMultiplier = 2.0f;
					noZTestLineMeshManager.Render(MaterialUtility.NoZTestGenericLine);
					MaterialUtility.LineThicknessMultiplier = 1.0f;
					zTestLineMeshManager.Render(MaterialUtility.ZTestGenericLine);

					break;
				}
			}
		}

		public void OnInspectorGUI(EditorWindow window, float height)
		{
			EditModeGenerateGUI.OnInspectorGUI(this, window, height);
		}
		
		public Rect GetLastSceneGUIRect()
		{
			return EditModeGenerateGUI.GetLastSceneGUIRect(this);
		}

		public bool OnSceneGUI(Rect windowRect)
		{
			return EditModeGenerateGUI.OnSceneGUI(windowRect, this);
		}

		public void GenerateFromPolygon(Camera camera, CSGBrush brush, CSGPlane plane, Vector3 direction, Vector3[] meshVertices, int[] indices, uint[] smoothingGroups, bool drag, CSGOperationType forceDragSource, bool autoCommitExtrusion)
		{
			BuilderMode = ShapeMode.FreeDraw;
			freedrawGenerator.GenerateFromPolygon(camera, brush, plane, direction, meshVertices, indices, smoothingGroups, drag, forceDragSource, autoCommitExtrusion);
		}
	}
}
