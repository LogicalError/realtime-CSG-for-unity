using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using InternalRealtimeCSG;
using RealtimeCSG.Foundation;
using RealtimeCSG.Legacy;
using RealtimeCSG.Components;

namespace RealtimeCSG
{
	internal abstract class GeneratorBase : ScriptableObject//, IBrushGenerator
	{
		protected const float	hover_text_distance = 25.0f;
		
		internal static readonly int ShapeBuilderShapeHash	    = "CSGShapeBuilderShape".GetHashCode();
		internal static readonly int ShapeBuilderCenterHash	    = "CSGShapeBuilderCenter".GetHashCode();
		internal static readonly int ShapeBuilderPointHash	    = "CSGShapeBuilderPoint".GetHashCode();
		internal static readonly int ShapeBuilderBoxHash	    = "CSGShapeBuilderBox".GetHashCode();
        internal static readonly int ShapeBuilderTangentHash    = "CSGShapeBuilderTangent".GetHashCode();
        internal static readonly int shapeEditIDHash		    = "shapeEditID".GetHashCode();
		
		protected virtual bool IgnoreForcedGridForTangents {  get { return false; } }

		public delegate Vector3 SnapToGridFunction(Camera camera, Vector3 worldPosition, CSGPlane plane, ref List<Vector3> snappingEdges, out CSGBrush snappedOnBrush, CSGBrush[] ignoreBrushes, bool ignoreAllBrushes = false);
		public delegate Vector3 SnapToRayFunction (Camera camera, Vector3 worldPosition, Ray ray, ref List<Vector3> snappingEdges, out CSGBrush snappedOnBrush);

		[NonSerialized] internal SnapToGridFunction snapFunction = null;		
		[NonSerialized] internal SnapToRayFunction raySnapFunction = null;
		[NonSerialized] internal Action shapeCommitted = null;
		[NonSerialized] internal Action shapeCancelled = null;
		

		const CSGOperationType invalidCSGOperationType = (CSGOperationType)99;

		[Serializable]
		protected enum EditMode { CreatePlane, CreateShape, EditShape, ExtrudeShape }
		
		[SerializeField] protected EditMode			editMode			= EditMode.CreatePlane;
		[SerializeField] protected EditMode			prevEditMode		= EditMode.CreatePlane;

		
		protected bool IgnoreDepthForRayCasts(Camera camera)
		{
			return CSGSettings.Assume2DView(camera);
		}


		public bool CanCommit	{ get { return (editMode != EditMode.CreatePlane); } }
		public bool HaveBrushes { get { return (editMode != EditMode.CreatePlane); } }
		
		protected abstract IShapeSettings			ShapeSettings { get; }

		[SerializeField] protected CSGOperationType	currentCSGOperationType			= invalidCSGOperationType;
		[SerializeField] protected CSGOperationType	forceCurrentCSGOperationType	= invalidCSGOperationType;

		[SerializeField] protected Transform		parentTransform;
		[SerializeField] protected GameObject		parentGameObject;
		[SerializeField] protected CSGModel			parentModel;
		[SerializeField] protected GameObject		operationGameObject;
		[NonSerialized]  protected CSGModel			geometryModel		= null;

		[SerializeField] protected GameObject[]		generatedGameObjects;
		[SerializeField] protected CSGBrush[]		generatedBrushes;
		
		[NonSerialized] protected int				undoGroupIndex;

		[NonSerialized] protected List<Vector3>		visualSnappedEdges; // used to visualize edges which we're snapping against
		[NonSerialized] protected List<Vector3>		visualSnappedGrid;	// used to visualize grid lines we're snapping against	
		[NonSerialized] protected CSGBrush			visualSnappedBrush;	// used to visualize the brush we're snapping on
		
		[NonSerialized] protected CSGPlane		buildPlane			= new CSGPlane(0, 1, 0, 0);

		
		[SerializeField] protected UnityEngine.Object[]	prevSelection;
		[NonSerialized] protected bool			planeOnGeometry		= false;
		[NonSerialized] protected bool			shapeIsValid		= true;
		[NonSerialized] protected bool			isFinished			= false;
		[NonSerialized] protected bool			mouseIsDragging		= false;		
		[NonSerialized] protected bool			ignoreOrbit			= false;
		[NonSerialized] ViewTool				previousViewTool	= ViewTool.Pan;

		[NonSerialized] Quaternion				originalGridRotation;
		[NonSerialized] Vector3					originalGridCenter;
		[NonSerialized] bool					originalForceGrid;

		[SerializeField] protected Vector3		brushPosition;
		[SerializeField] protected Quaternion?	brushRotation;
		[SerializeField] protected bool			ignoreModelRotation = false;

		[NonSerialized] protected int			shapeEditId;
		[NonSerialized] protected int			shapeId;
		
		[SerializeField] protected Vector3		gridNormal;
		[SerializeField] protected Vector3		gridTangent;
		[SerializeField] protected Vector3		gridBinormal;
		[SerializeField] protected Matrix4x4	toGridMatrix		= Matrix4x4.identity;
		[SerializeField] protected Matrix4x4	fromGridMatrix		= Matrix4x4.identity;
		[SerializeField] protected Quaternion	toGridQuaternion	= Quaternion.identity;
		[SerializeField] protected Quaternion	fromGridQuaternion	= Quaternion.identity;
		
		public CSGOperationType CurrentCSGOperationType
		{
			get
			{
				if (forceCurrentCSGOperationType == invalidCSGOperationType)
					return currentCSGOperationType;
				else
					return forceCurrentCSGOperationType;
			}
			set
			{
				if (forceCurrentCSGOperationType == value)
					return;

				forceCurrentCSGOperationType = value;
				UpdateOperationType(value);
			}
		}

		protected void UpdateOperationType(CSGOperationType opType)
		{
			if (operationGameObject != null)
			{
				var operation = operationGameObject.GetComponent<CSGOperation>();
				if (operation != null)
					operation.OperationType = opType;

				var brush = operationGameObject.GetComponent<CSGBrush>();
				if (brush != null)
					brush.OperationType = opType;
			}
			InternalCSGModelManager.CheckForChanges();
			EditorApplication.RepaintHierarchyWindow();
		}

		public virtual void Init()
		{
			prevSelection			= new List<UnityEngine.Object>(Selection.objects).ToArray();

			Reset();

			shapeIsValid			= true;

			isFinished				= false;
			
			originalGridRotation	= RealtimeCSG.CSGGrid.ForcedGridRotation;
			originalGridCenter		= RealtimeCSG.CSGGrid.ForcedGridCenter;
			originalForceGrid		= RealtimeCSG.CSGGrid.ForceGrid;
			
			geometryModel			= null;
		}
		
		public virtual void Reset() 
		{
			editMode				= EditMode.CreatePlane;
			brushPosition			= MathConstants.zeroVector3;
			brushRotation			= null;
			ignoreModelRotation		= false;
			parentGameObject		= null;
			parentModel				= null;
			operationGameObject		= null;
			parentTransform			= null;
			generatedGameObjects	= null;
			generatedBrushes		= null;
			undoGroupIndex			= -1;
			forceCurrentCSGOperationType = invalidCSGOperationType;
			currentCSGOperationType = invalidCSGOperationType;
		}


		protected virtual bool StartEditMode(Camera camera)
		{
			if (editMode == EditMode.EditShape ||
				editMode == EditMode.ExtrudeShape)
			{
				return false;
			}

			Undo.RecordObject(this, "Created shape");
			if (GUIUtility.hotControl == shapeId)
			{
				GUIUtility.hotControl = 0;
				GUIUtility.keyboardControl = 0;
				EditorGUIUtility.SetWantsMouseJumping(0);
				EditorGUIUtility.editingTextField = false;
			}
			
			CalculateWorldSpaceTangents(camera);
			brushPosition = buildPlane.Project(ShapeSettings.GetCenter(buildPlane));
			editMode = EditMode.EditShape;

			CSGPlane newPlane = buildPlane;
			ShapeSettings.CalculatePlane(ref newPlane);
			if (newPlane.normal.sqrMagnitude != 0)
				buildPlane = newPlane;

            if (ModelTraits.IsModelEditable(geometryModel))
                SelectionUtility.LastUsedModel = geometryModel;

			UpdateBaseShape();

			//StartExtrudeMode();
			//GrabHeightHandle(ignoreFirstMouseUp: true);
			return true;
		}

		protected void RevertToEditVertices()
		{
			if (editMode != EditMode.ExtrudeShape)
				return;			
			editMode = EditMode.EditShape;
		}

		protected void CleanupGrid()  
		{
			RealtimeCSG.CSGGrid.ForcedGridRotation = originalGridRotation;
			RealtimeCSG.CSGGrid.ForcedGridCenter	= originalGridCenter;
			RealtimeCSG.CSGGrid.ForceGrid			= originalForceGrid;
		}

		protected void ResetVisuals()
		{
			visualSnappedEdges = null;
			visualSnappedGrid = null;
			visualSnappedBrush = null;
		}

		protected void PaintSnapVisualisation()
		{
			if (visualSnappedEdges != null)
				PaintUtility.DrawLines(visualSnappedEdges.ToArray(), GUIConstants.oldThickLineScale, ColorSettings.SnappedEdges);

			var _origMatrix = Handles.matrix;
			Handles.matrix = MathConstants.identityMatrix;
			if (visualSnappedGrid != null)
			{
				PaintUtility.DrawDottedLines(visualSnappedGrid.ToArray(), ColorSettings.SnappedEdges);
			}
			
			if (visualSnappedBrush != null)
			{
				if (visualSnappedBrush.compareTransformation != null &&
					visualSnappedBrush.ChildData != null &&
					visualSnappedBrush.ChildData.ModelTransform)
				{
					var color = ColorSettings.HoverOutlines;
					var brush_transformation = visualSnappedBrush.compareTransformation.localToWorldMatrix;
					CSGRenderer.DrawOutlines(visualSnappedBrush.brushNodeID, brush_transformation,
						color, color, color, color, GUIConstants.oldThickLineScale);
				}
			}
			Handles.matrix = _origMatrix;
		}

		protected void SetBrushTransformation(Transform transform)
		{
			var direction = buildPlane.normal;
			Quaternion rotation;
			if (brushRotation.HasValue)
				rotation = brushRotation.Value;
			else
				rotation = Quaternion.FromToRotation(MathConstants.upVector3, direction);

			if (!ignoreModelRotation)
				rotation *= Quaternion.Inverse(parentModel.transform.rotation);

			transform.rotation = rotation;

			transform.position = brushPosition;
		}

		protected void UpdateBrushPosition()
		{
			if (operationGameObject)
			{
				var transform = operationGameObject.transform;
				SetBrushTransformation(transform);
			}
		}

		protected virtual void CreateControlIDs()
		{
			//if (Event.current.type == EventType.Layout)
			{
				shapeEditId = GUIUtility.GetControlID(shapeEditIDHash, FocusType.Passive);
				HandleUtility.AddDefaultControl(shapeEditId);

				shapeId = GUIUtility.GetControlID(ShapeBuilderShapeHash, FocusType.Passive);
			}
		}


		protected void HandleKeyboard(EventType type)
		{
			if (EditorGUIUtility.editingTextField)
				return;

            var camera = Camera.current;
            switch (type)
			{
				case EventType.ValidateCommand:
				case EventType.KeyDown:
				{					
					if (Keys.PerformActionKey.IsKeyPressed() ||
						Keys.DeleteSelectionKey.IsKeyPressed() ||
						Keys.CancelActionKey.IsKeyPressed())
					{
						Event.current.Use(); 
						break;
					}
					break;
				}
				case EventType.KeyUp:
				{
					if (Keys.PerformActionKey.IsKeyPressed())
					{
						Commit(camera);
						Event.current.Use(); 
						break;
					} else
					if (Keys.CancelActionKey.IsKeyPressed())
					{
						PerformDeselectAll();
						Event.current.Use(); 
						break;
					} else
					if (Keys.DeleteSelectionKey.IsKeyPressed())
					{
						PerformDelete();
						Event.current.Use(); 
						break;
					}
					break;
				}
			}
		}
		

		protected void PaintSideLength(Camera camera, Vector3 edgeCenter, Vector3 circleCenter, float length, string name)
		{
			var textCenter2DA = HandleUtility.WorldToGUIPoint(circleCenter);
			var textCenter2DB = HandleUtility.WorldToGUIPoint(edgeCenter);
			var normal2D = (textCenter2DB - textCenter2DA).normalized;

			var textCenter2D = textCenter2DB;
			textCenter2D += normal2D * (hover_text_distance * 2);

			var textCenterRay = HandleUtility.GUIPointToWorldRay(textCenter2D);
			var textCenter = textCenterRay.origin + textCenterRay.direction * ((camera.farClipPlane + camera.nearClipPlane) * 0.5f);
			
			PaintUtility.DrawLine(edgeCenter, textCenter, Color.black);
			PaintUtility.DrawDottedLine(edgeCenter, textCenter, ColorSettings.SnappedEdges);
			
			if (float.IsNaN(length) || float.IsInfinity(length))
			{
				PaintUtility.DrawScreenText(textCenter2D, name + " --");
			} else
			{
				PaintUtility.DrawScreenText(textCenter2D, name + Units.ToRoundedDistanceString(length));
			}
		}

		protected void CalculateWorldSpaceTangents(Camera camera)
		{
			bool hadForcedGrid = false;
			if (IgnoreForcedGridForTangents)
			{
				hadForcedGrid = RealtimeCSG.CSGGrid.ForceGrid;
			}
			if (hadForcedGrid)
			{
				RealtimeCSG.CSGGrid.ForceGrid = false;
				RealtimeCSG.CSGGrid.UpdateGridOrientation(camera);
			}

			var gridSpaceNormal = RealtimeCSG.CSGGrid.VectorToGridSpace(buildPlane.normal);

			Vector3 gridSpaceTangent;
			Vector3 gridSpaceBinormal;

			GeometryUtility.CalculateTangents(gridSpaceNormal, out gridSpaceTangent, out gridSpaceBinormal);

			gridNormal   = RealtimeCSG.CSGGrid.VectorFromGridSpace(gridSpaceNormal);
			gridTangent  = RealtimeCSG.CSGGrid.VectorFromGridSpace(gridSpaceTangent);
			gridBinormal = RealtimeCSG.CSGGrid.VectorFromGridSpace(gridSpaceBinormal);
			
			toGridMatrix		= RealtimeCSG.CSGGrid.ToGridSpaceMatrix();
			fromGridMatrix		= RealtimeCSG.CSGGrid.FromGridSpaceMatrix();
			toGridQuaternion	= RealtimeCSG.CSGGrid.ToGridSpaceQuaternion();
			fromGridQuaternion	= RealtimeCSG.CSGGrid.FromGridSpaceQuaternion();
			
			if (hadForcedGrid)
			{
				RealtimeCSG.CSGGrid.ForceGrid = true;
				RealtimeCSG.CSGGrid.UpdateGridOrientation(camera);
			}
		}

		protected void SetCameraPosition(SceneView sceneView, Vector3 center, float size)
		{
			if (float.IsInfinity(size) || float.IsNaN(size) ||
				float.IsInfinity(center.x) || float.IsNaN(center.x) ||
				float.IsInfinity(center.y) || float.IsNaN(center.y) ||
				float.IsInfinity(center.z) || float.IsNaN(center.z))
				return;
			if (sceneView)
                sceneView.LookAt(center, sceneView.rotation, Mathf.Max(size, (sceneView.camera.transform.position - center).magnitude));
		}
		

		protected void HideGenerateBrushes()
		{
			if (generatedBrushes != null)
			{
				for (int i = 0; i < generatedBrushes.Length; i++)
				{
					if (generatedBrushes[i] &&
						generatedBrushes[i].gameObject &&
						generatedBrushes[i].gameObject.activeSelf)
						generatedBrushes[i].gameObject.SetActive(false);
				}
			}
		}

		protected bool GenerateBrushObjects(int brushObjectCount, bool inGridSpace = true)
		{
			Undo.IncrementCurrentGroup();
			undoGroupIndex = Undo.GetCurrentGroup();

            var lastUsedModel			= SelectionUtility.LastUsedModel;
            if (!ModelTraits.IsModelEditable(lastUsedModel))
                lastUsedModel = null;
            var lastUsedModelTransform	= !lastUsedModel ? null : lastUsedModel.transform;

			if (!lastUsedModelTransform ||
				!lastUsedModel.isActiveAndEnabled)
			{
				if (prevSelection != null && prevSelection.Length > 0)
				{
					for (int i = 0; i < prevSelection.Length; i++)
					{
						UnityEngine.Object	obj		= prevSelection[i];
						CSGBrush			brush	= obj as CSGBrush;
						MonoBehaviour		mono	= obj as MonoBehaviour;
						GameObject			go		= obj as GameObject;
						if (!brush)
						{
							if (mono)
							{
								brush = mono.GetComponentInChildren<CSGBrush>();
							}
							if (go)
							{
								brush = go.GetComponentInChildren<CSGBrush>();
							}
						}

						if (!brush)
							continue;
						
						if ((brush.gameObject.hideFlags & (HideFlags.HideInHierarchy | HideFlags.NotEditable | HideFlags.DontSaveInBuild)) != 0)
							continue;

						if (brush.ChildData == null ||
							brush.ChildData.ModelTransform == null)
							continue;

						var model = brush.ChildData.Model;
						if (!model ||
							!model.isActiveAndEnabled)
							continue;

						lastUsedModelTransform = brush.ChildData.ModelTransform;
						break;
					}
				}
			}     

			if (generatedBrushes != null && generatedBrushes.Length > 0)
            {
                for (int i = generatedBrushes.Length - 1; i >= 0; i--)
                {
                    if (generatedBrushes[i])
                        continue;
                    ArrayUtility.RemoveAt(ref generatedBrushes, i);
                }
                for (int i = generatedGameObjects.Length - 1; i >= 0; i--)
                {
					if (generatedGameObjects[i])
					{				
						var brush = generatedGameObjects[i].GetComponentInChildren<CSGBrush>();
						if (brush && ArrayUtility.Contains(generatedBrushes, brush))
							continue;
					}
                    ArrayUtility.RemoveAt(ref generatedGameObjects, i);
                }
            }

			if (generatedGameObjects == null ||
				generatedGameObjects.Length != brushObjectCount)
			{
				if (generatedBrushes != null && generatedBrushes.Length > 0)
				{
					for (int i = 0; i < generatedBrushes.Length; i++)
					{
						InternalCSGModelManager.OnDestroyed(generatedBrushes[i]);
						GameObject.DestroyImmediate(generatedBrushes[i]);
					}
				}
				if (generatedGameObjects != null && generatedGameObjects.Length > 0)
				{
					for (int i = 0; i < generatedGameObjects.Length; i++)
						GameObject.DestroyImmediate(generatedGameObjects[i]);
				}

				if (parentGameObject != null)
					GameObject.DestroyImmediate(parentGameObject);

				//DebugEditorWindow.PrintDebugInfo();

				if (lastUsedModelTransform == null)
				{
					parentGameObject = OperationsUtility.CreateGameObject(lastUsedModelTransform, "Model", true);
					InternalCSGModelManager.CreateCSGModel(parentGameObject);
					parentModel = parentGameObject.GetComponent<CSGModel>();
					Undo.RegisterCreatedObjectUndo(parentGameObject, "Created model");

					if (brushObjectCount > 1)
					{
						operationGameObject = OperationsUtility.CreateGameObject(parentGameObject.transform, "Operation", true);
						var transform = operationGameObject.transform;
						SetBrushTransformation(transform);

						var operation = operationGameObject.AddComponent<CSGOperation>();
						if (CurrentCSGOperationType != invalidCSGOperationType)							
							operation.OperationType = CurrentCSGOperationType;
						operation.HandleAsOne = true;
						Undo.RegisterCreatedObjectUndo(operationGameObject, "Created operation");
						parentTransform = operationGameObject.transform;
					} else
					{
						parentTransform = parentGameObject.transform;
					}
				} else
				if (brushObjectCount > 1)
				{
					parentModel = lastUsedModelTransform.GetComponent<CSGModel>();
					parentGameObject = OperationsUtility.CreateGameObject(lastUsedModelTransform, "Brushes", true);
					var transform = parentGameObject.transform;
					SetBrushTransformation(transform);

					operationGameObject = parentGameObject;
					var operation = operationGameObject.AddComponent<CSGOperation>();
					if (CurrentCSGOperationType != invalidCSGOperationType)
						operation.OperationType = CurrentCSGOperationType;
					operation.HandleAsOne = true;
					parentTransform = operationGameObject.transform;
					Undo.RegisterCreatedObjectUndo(parentGameObject, "Created brush");
				} else
				{
					parentGameObject = null;
					operationGameObject = null;
					parentTransform = lastUsedModelTransform;
					parentModel = lastUsedModelTransform.GetComponent<CSGModel>();
				}

				
				generatedGameObjects = new GameObject[brushObjectCount];
				generatedBrushes = new CSGBrush[brushObjectCount];
				for (int p = 0; p < brushObjectCount; p++)
				{
					string name;
					if (brushObjectCount == 1)
						name = "Brush";
					else
						name = "Brush (" + p + ")";
					var gameObject = OperationsUtility.CreateGameObject(parentTransform, name, false);
					gameObject.SetActive(false);

					var brushComponent = gameObject.AddComponent<CSGBrush>();

					if (operationGameObject == null)
					{
						if (CurrentCSGOperationType != invalidCSGOperationType)
							brushComponent.OperationType = CurrentCSGOperationType;
						operationGameObject = gameObject;
						var transform = gameObject.transform;
						SetBrushTransformation(transform);
					}

					generatedBrushes[p] = brushComponent;
					generatedBrushes[p].ControlMesh = new ControlMesh();
					generatedBrushes[p].Shape = new Shape();
					Undo.RegisterCreatedObjectUndo(gameObject, "Created brush");
					generatedGameObjects[p] = gameObject;
				}
				//InternalCSGModelManager.Refresh(forceHierarchyUpdate: true);
				// brushes not registered at this point!??


				//DebugEditorWindow.PrintDebugInfo();
				//Selection.objects = generatedGameObjects;
			} else
				UpdateBrushPosition();

			return generatedBrushes != null && generatedBrushes.Length > 0 && generatedBrushes.Length == brushObjectCount;
		}

		internal void MarkAllBrushesDirty()
		{
			for (int i = 0; i < generatedBrushes.Length; i++)
			{
				var brush = generatedBrushes[i];
				if (!brush)
					continue;

                EditorUtility.SetDirty(brush);
			}
		}

		public abstract AABB GetShapeBounds();
		
		protected void HandleCameraOrbit(SceneView sceneView, bool allowCameraOrbit)
		{
			if (!ignoreOrbit &&
				Tools.viewTool == ViewTool.Orbit &&
				previousViewTool != ViewTool.Orbit &&
				allowCameraOrbit)
			{
				var bounds	= GetShapeBounds();
				var size	= bounds.Size.magnitude;
				var center	= bounds.Center;
				SetCameraPosition(sceneView, center, size);
			}
			previousViewTool = Tools.viewTool;

			if (Event.current.type == EventType.MouseUp ||
				Event.current.type == EventType.MouseMove) ignoreOrbit = false;
		}
		

		public bool UndoRedoPerformed()
		{
			if (UpdateBaseShape(registerUndo: false))
				return false;

			Cancel();
			return true;
		}

		public virtual void HandleEvents(SceneView sceneView, Rect sceneRect)
		{
			if (Event.current.type == EventType.MouseDown ||
				Event.current.type == EventType.MouseMove) mouseIsDragging = false; else
			if (Event.current.type == EventType.MouseDrag) mouseIsDragging = true;

			CreateControlIDs();

			if (mouseIsDragging || 
				(Event.current.type == EventType.MouseDown && Tools.viewTool == ViewTool.Orbit))
				HandleCameraOrbit(sceneView, editMode != EditMode.CreatePlane);
						
			// pretend we dragged so we don't click if we just changed edit modes
			if (prevEditMode != editMode)
			{
				prevEditMode = editMode;
				mouseIsDragging = true;
            }

            switch (editMode)
			{
				default:
				case EditMode.CreatePlane:
				case EditMode.CreateShape:	HandleCreateShapeEvents(sceneView, sceneRect); break;

				case EditMode.ExtrudeShape:	
				case EditMode.EditShape:	HandleEditShapeEvents(sceneView, sceneRect); break;
			}
		}
		
		public abstract bool Commit(Camera camera);
		internal abstract bool UpdateBaseShape(bool registerUndo = true);
		protected abstract void HandleCreateShapeEvents(SceneView sceneView, Rect sceneRect);
		protected abstract void HandleEditShapeEvents(SceneView sceneView, Rect sceneRect);
	    protected abstract void MoveShape(Vector3 offset);
		public abstract void PerformDeselectAll();
		public abstract void PerformDelete();

		void Cleanup()
		{
			if (prevSelection != null)	Selection.objects			= prevSelection;
			else						Selection.activeTransform	= null;
			prevSelection = null;
			
			if (GUIUtility.hotControl == shapeId)
			{
				GUIUtility.hotControl = 0;
				GUIUtility.keyboardControl = 0;
				EditorGUIUtility.SetWantsMouseJumping(0);
				EditorGUIUtility.editingTextField = false;
			}
			
			isFinished  = true;
			CSG_EditorGUIUtility.RepaintAll();
		}

		public virtual void Cancel()
		{
			ResetVisuals();

			CleanupGrid();
			
			{
				if (generatedBrushes != null && generatedBrushes.Length > 0)
				{
					for (int i = 0; i < generatedBrushes.Length; i++)
					{
						InternalCSGModelManager.OnDestroyed(generatedBrushes[i]);
						GameObject.DestroyImmediate(generatedBrushes[i]);
					}
				}
				if (generatedGameObjects != null && generatedGameObjects.Length > 0)
				{
					for (int i = 0; i < generatedGameObjects.Length; i++)
						GameObject.DestroyImmediate(generatedGameObjects[i]);
				}
				if (parentGameObject != null)
					GameObject.DestroyImmediate(parentGameObject);
			}

			Cleanup();
			Reset();

			if (shapeCancelled != null)
				shapeCancelled();			
		}
		
		
		public void EndCommit()
        {
            if (generatedBrushes == null)
				return;
			var bounds = BoundsUtilities.GetBounds(generatedBrushes);
			if (!bounds.IsEmpty())
            {/*
                var center = bounds.Center - operationGameObject.transform.position;
				GeometryUtility.MoveControlMeshVertices(generatedBrushes, -center);
				SurfaceUtility.TranslateSurfacesInWorldSpace(generatedBrushes, -center);
                operationGameObject.transform.position += center;*/
				ControlMeshUtility.RebuildShapes(generatedBrushes);
                var model = operationGameObject.GetComponentInParent<CSGModel>();
                model.forceUpdate = true;

                InternalCSGModelManager.CheckForChanges(forceHierarchyUpdate: true);
				Undo.CollapseUndoOperations(undoGroupIndex);
				Cleanup();

				if (generatedGameObjects != null &&
					generatedGameObjects.Length > 0) 
					Selection.objects = generatedGameObjects;

				Reset();
			}

			if (shapeCommitted != null)
				shapeCommitted();
		}

		

		// unity bug workaround

		[NonSerialized] bool doCommit = false;
		[NonSerialized] bool doCancel = false;

		public void StartGUI()
		{
			doCommit = false;
			doCancel = false;
		}

		public void DoCancel()
		{
			doCancel = true;
		}
		
		public void DoCommit()
		{
			doCommit = true;
		}

		public void FinishGUI()
		{
            var camera = Camera.current;
			if (doCommit) Commit(camera);
			if (doCancel) Cancel();
		}
	}
}
