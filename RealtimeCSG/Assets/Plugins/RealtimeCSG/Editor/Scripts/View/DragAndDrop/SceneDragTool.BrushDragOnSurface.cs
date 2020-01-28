using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using InternalRealtimeCSG;
using RealtimeCSG.Components;

namespace RealtimeCSG
{
	internal sealed class SceneDragToolBrushDragOnSurface : ISceneDragTool
	{
		SelectedBrushSurface		hoverBrushSurface			= null;
		Vector3						hoverPosition;
		Quaternion					hoverRotation;
		Transform					hoverParent;
		int							hoverSiblingIndex;
		bool                        containsModel               = false;
		List<GameObject>			dragGameObjects				= null;
		List<GameObject>			visualDragGameObject		= null;
		CSGBrush[]					ignoreBrushes		        = null;
		HashSet<Transform>			ignoreTransforms            = null;
		Vector3[]					projectedBounds				= null;
		bool						haveNoParent				= false;
		PrefabSourceAlignment		sourceSurfaceAlignment		= PrefabSourceAlignment.AlignedFront;
		PrefabDestinationAlignment	destinationSurfaceAlignment	= PrefabDestinationAlignment.AlignToSurface;
		
		Vector3			prevForcedGridCenter	= MathConstants.zeroVector3;
		Quaternion		prevForcedGridRotation	= MathConstants.identityQuaternion;

		#region ValidateDrop
		public bool ValidateDrop(SceneView sceneView)
		{
			if (!sceneView)
				return false;

			Reset();
			if (DragAndDrop.objectReferences == null ||
				DragAndDrop.objectReferences.Length == 0)
			{
				dragGameObjects = null;
				return false;
			}

			dragGameObjects = new List<GameObject>();
			containsModel = false;
			foreach (var obj in DragAndDrop.objectReferences)
			{
				var gameObject = obj as GameObject;
				if (gameObject == null)
					continue;

				if (gameObject.GetComponent<CSGNode>() == null)
					continue;

				if (gameObject.GetComponentsInChildren<CSGBrush>() == null)
					continue;

				if (!CSGPrefabUtility.IsPrefabAsset(gameObject))
					continue;

				dragGameObjects.Add(gameObject);
				containsModel = containsModel || (gameObject.GetComponent<CSGModel>() != null);
			}
			if (dragGameObjects.Count != 1)
			{
				dragGameObjects = null;
				return false;
			}

			var dragGameObjectBounds = new AABB();
			dragGameObjectBounds.Reset();
			foreach (var gameObject in dragGameObjects)
			{
				var brushes = gameObject.GetComponentsInChildren<CSGBrush>();
				if (brushes.Length == 0)
					continue;
				dragGameObjectBounds.Add(BoundsUtilities.GetLocalBounds(brushes, gameObject.transform.worldToLocalMatrix));
			}

			if (!dragGameObjectBounds.Valid)
				dragGameObjectBounds.Extend(MathConstants.zeroVector3);
						
			projectedBounds = new Vector3[8];
			BoundsUtilities.GetBoundsCornerPoints(dragGameObjectBounds, projectedBounds);
			haveNoParent = false;
			return true;
		}
        #endregion

		#region ValidateDropPoint
		public bool ValidateDropPoint(SceneView sceneView)
		{
			return true;
		}
		#endregion

		#region Reset
				public void Reset()
				{
					CleanUp();
					hoverBrushSurface	= null;
					dragGameObjects		= null;

					hoverPosition = MathConstants.zeroVector3;
					hoverRotation = MathConstants.identityQuaternion;
					hoverParent = null;
					hoverSiblingIndex = int.MaxValue;
				}
		#endregion

		void CleanUp()
		{
			if (visualDragGameObject != null)
			{
				for (int i = visualDragGameObject.Count - 1; i >= 0; i--)
				{
					if (!visualDragGameObject[i])
						continue;
					GameObject.DestroyImmediate(visualDragGameObject[i]);
				}
			}
			visualDragGameObject = null;
			ignoreBrushes = null;
		}


		public SelectedBrushSurface[] HoverOnBrush(CSGBrush hoverBrush, int surfaceIndex)
		{
			if (!hoverBrush)
				return null;
			
			return new SelectedBrushSurface[] 
			{
				new SelectedBrushSurface(hoverBrush, surfaceIndex)
			};
		}
		
		void EnableVisualObjects()
		{
			if (visualDragGameObject == null ||
				visualDragGameObject.Count != dragGameObjects.Count)
			{
				CreateVisualObjects();
			}

			var realParent = (!hoverParent || CSGPrefabUtility.IsPrefabAsset(hoverParent.gameObject)) ? null : hoverParent;

			int counter = 0;
			foreach (var obj in visualDragGameObject)
			{
				if (!obj)
					continue;
				obj.transform.rotation = hoverRotation;
				obj.transform.position = hoverPosition;
				if (realParent)
				{
					obj.transform.SetParent(realParent, true);
				} else
					obj.transform.parent = null;
				obj.transform.SetSiblingIndex(hoverSiblingIndex + counter);
				counter++;
			}

			InternalCSGModelManager.CheckTransformChanged();
			InternalCSGModelManager.OnHierarchyModified();
			InternalCSGModelManager.UpdateMeshes(forceUpdate: true);
			MeshInstanceManager.UpdateHelperSurfaceVisibility();

			if (ignoreBrushes == null && visualDragGameObject != null)
			{
				var foundIgnoreBrushes = new List<CSGBrush>();
				foreach (var obj in visualDragGameObject)
					foundIgnoreBrushes.AddRange(obj.GetComponentsInChildren<CSGBrush>());

				ignoreBrushes = foundIgnoreBrushes.ToArray();
			}
		}
		
		void CreateVisualObjects(bool inSceneView = false)
		{
			CleanUp();
			
			prevForcedGridCenter		= RealtimeCSG.CSGGrid.ForcedGridCenter;
			prevForcedGridRotation		= RealtimeCSG.CSGGrid.ForcedGridRotation;

			sourceSurfaceAlignment		= PrefabSourceAlignment.AlignedFront;
			destinationSurfaceAlignment = PrefabDestinationAlignment.AlignToSurface;

			
			visualDragGameObject = new List<GameObject>();

			var foundTransforms = new List<Transform>();

			foreach (var obj in dragGameObjects)
			{
				foundTransforms.AddRange(obj.GetComponentsInChildren<Transform>());
				CSGNode node = obj.GetComponent<CSGNode>();
				if (!node)
					continue;
				sourceSurfaceAlignment		= node.PrefabSourceAlignment;
				destinationSurfaceAlignment = node.PrefabDestinationAlignment;

				bool createCopyInsteadOfInstance = node && node.PrefabBehaviour == PrefabInstantiateBehaviour.Copy;

				GameObject copy = CSGPrefabUtility.Instantiate(obj, createCopyInsteadOfInstance);
				if (!copy)
					continue;


				copy.name = obj.name;
				visualDragGameObject.Add(copy);
			}
			
			ignoreTransforms = new HashSet<Transform>(foundTransforms);
			
			if (inSceneView)
			{ 
				var model	= SelectionUtility.LastUsedModel;
				if (model && !containsModel)
				{
					var parent	= model.transform;
					int counter = 0;
					foreach (var obj in visualDragGameObject)
					{
						if (!obj)
							continue;
						if (obj.activeSelf)
						{
							obj.transform.SetParent(parent, false);
							obj.transform.SetSiblingIndex(hoverSiblingIndex + counter);
							counter++;
						}
					}
				}
			} else
			{
				var parent = hoverParent;
				int counter = 0;
				foreach (var obj in visualDragGameObject)
				{
					if (!obj)
						continue;
					if (obj.activeSelf)
					{
						obj.transform.SetParent(parent, false);
						obj.transform.SetSiblingIndex(hoverSiblingIndex + counter);
						counter++;
					}
				}
			}
		}

        #region DragUpdated
		public bool DragUpdated(Transform transformInInspector, Rect selectionRect)
		{
			InternalCSGModelManager.skipCheckForChanges = true;
			try
			{
				DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

				hoverBrushSurface	= null;
				hoverParent			= (!transformInInspector) ? null : transformInInspector.parent;
				hoverSiblingIndex	= (!transformInInspector) ? int.MaxValue : transformInInspector.transform.GetSiblingIndex();

				float middle = (selectionRect.yMax + selectionRect.yMin) * 0.5f;
				if (Event.current.mousePosition.y > middle)
					hoverSiblingIndex++;

				hoverRotation = MathConstants.identityQuaternion;
				hoverPosition = MathConstants.zeroVector3;
				haveNoParent = true;
				return true;
			}
			finally
			{
				if (!UpdateLoop.IsActive())
					UpdateLoop.ResetUpdateRoutine();
			}
		}

		public bool DragUpdated(SceneView sceneView)
		{
			InternalCSGModelManager.skipCheckForChanges = true;
			try
			{
				DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
				
                var camera              = sceneView.camera;
				var intersection		= SceneQueryUtility.FindMeshIntersection(camera, Event.current.mousePosition, ignoreBrushes, ignoreTransforms);
				var normal				= intersection.worldPlane.normal;

				hoverPosition			= intersection.worldIntersection;
				hoverParent				= SelectionUtility.FindParentToAssignTo(intersection);
				hoverBrushSurface		= intersection.brush ? new SelectedBrushSurface(intersection.brush, intersection.surfaceIndex) : null;
				hoverRotation			= SelectionUtility.FindDragOrientation(sceneView, normal, sourceSurfaceAlignment, destinationSurfaceAlignment);
				haveNoParent			= !hoverParent;
				hoverSiblingIndex		= int.MaxValue;

				RealtimeCSG.CSGGrid.SetForcedGrid(camera, intersection.worldPlane);

                // Since we're snapping points to grid and do not have a relative distance, relative snapping makes no sense
				var doGridSnapping	= RealtimeCSG.CSGSettings.ActiveSnappingMode != SnapMode.None;
				if (doGridSnapping)
				{
					var localPoints = new Vector3[8];
					var localPlane	= intersection.worldPlane;
					for (var i = 0; i < localPoints.Length; i++)
						localPoints[i] = GeometryUtility.ProjectPointOnPlane(localPlane, (hoverRotation * projectedBounds[i]) + hoverPosition);

					hoverPosition += RealtimeCSG.CSGGrid.SnapDeltaToGrid(camera, MathConstants.zeroVector3, localPoints);
				}
                hoverPosition   = GeometryUtility.ProjectPointOnPlane(intersection.worldPlane, hoverPosition);// + (normal * 0.01f);

				EnableVisualObjects();
				return true;
			}
			finally
			{
				if (!UpdateLoop.IsActive())
					UpdateLoop.ResetUpdateRoutine();
			}
		}
        #endregion
		
        #region DragPerform
		public void DragPerform(SceneView sceneView)
		{
			try
			{
				InternalCSGModelManager.skipCheckForChanges = true;
				if (visualDragGameObject == null)
				{
					CreateVisualObjects(sceneView != null);
				}

				if (sceneView && haveNoParent && !containsModel)
				{					
					var model = SelectionUtility.LastUsedModel;
					if (!model)
					{
						model = OperationsUtility.CreateModelInstanceInScene(selectModel: false);
						InternalCSGModelManager.EnsureInitialized(model);
						InternalCSGModelManager.CheckTransformChanged();
						InternalCSGModelManager.OnHierarchyModified();
					}
					var parent = model.transform;

					int counter = 0;
					foreach (var obj in visualDragGameObject)
					{
						if (!obj)
							continue;
						if (obj.activeSelf)
						{
							obj.transform.SetParent(parent, false);
							obj.transform.SetSiblingIndex(hoverSiblingIndex + counter);
							counter++;
						}
					}
				}

				if (visualDragGameObject != null)
				{
					var selection = new List<GameObject>();
					for (int i = visualDragGameObject.Count - 1; i >= 0; i--)
					{
						if (!visualDragGameObject[i])
							continue;
						if (visualDragGameObject[i].activeSelf)
						{
							Undo.RegisterCreatedObjectUndo(visualDragGameObject[i], "Instantiated prefab");
							selection.Add(visualDragGameObject[i]);
						} else
						{
							GameObject.DestroyImmediate(visualDragGameObject[i]);
						}
					}
					visualDragGameObject = null;

					if (selection.Count > 0)
					{
						UnityEditor.Selection.objects = selection.ToArray();
					}
				}

				if (sceneView)
				{
					for (int i = 0; i < SceneView.sceneViews.Count; i++)
					{
						var sceneview = SceneView.sceneViews[i] as SceneView;
						if (!sceneview)
							continue;

						if (sceneview.camera.pixelRect.Contains(Event.current.mousePosition))
							sceneview.Focus();
					}
				}
				visualDragGameObject = null;

				InternalCSGModelManager.CheckForChanges(forceHierarchyUpdate: true);
			}
			finally
			{
				InternalCSGModelManager.skipCheckForChanges = false;
				RealtimeCSG.CSGGrid.ForcedGridCenter	= prevForcedGridCenter;
				RealtimeCSG.CSGGrid.ForcedGridRotation = prevForcedGridRotation;
				RealtimeCSG.CSGGrid.ForceGrid			= false;
			}
		}
        #endregion

        #region DragExited
		public void DragExited(SceneView sceneView)
		{
			try
			{
				InternalCSGModelManager.skipCheckForChanges = true;
				try { CleanUp(); } catch { }
				InternalCSGModelManager.CheckTransformChanged();
				InternalCSGModelManager.OnHierarchyModified();
				InternalCSGModelManager.UpdateMeshes(forceUpdate: true);
				MeshInstanceManager.UpdateHelperSurfaceVisibility();
				HandleUtility.Repaint();
			}
			finally
			{
				InternalCSGModelManager.skipCheckForChanges = false;
				RealtimeCSG.CSGGrid.ForcedGridCenter	= prevForcedGridCenter;
				RealtimeCSG.CSGGrid.ForcedGridRotation = prevForcedGridRotation;
				RealtimeCSG.CSGGrid.ForceGrid			= false;
			}
		}
        #endregion

        #region Paint
		public void OnPaint(Camera camera)
        {
            RealtimeCSG.CSGGrid.RenderGrid(camera);
			if (hoverBrushSurface == null)
				return;
			
			var brush = hoverBrushSurface.brush;
			if (brush.ChildData == null ||
				!brush.ChildData.ModelTransform)
				return;
				
			var highlight_surface		= hoverBrushSurface.surfaceIndex;
			var brush_transformation	= brush.compareTransformation.localToWorldMatrix;
			CSGRenderer.DrawSelectedBrush(brush.brushNodeID, brush.Shape, 
											brush_transformation, ColorSettings.WireframeOutline, 
											highlight_surface, 
											false, GUIConstants.oldLineScale);
		}
        #endregion
	}
}
