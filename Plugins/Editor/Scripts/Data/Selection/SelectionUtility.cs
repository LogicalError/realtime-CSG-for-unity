using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using InternalRealtimeCSG;
using RealtimeCSG.Components;

namespace RealtimeCSG
{
	internal enum SelectionType
	{
		Replace,
		Additive,
		Subtractive,
		Toggle
	};

	internal static class SelectionUtility
	{
		private static CSGModel lastUsedModelInstance = null;
		internal static CSGModel LastUsedModel
		{
			get
			{
				CSGModel returnModel = null;
				if (lastUsedModelInstance != null)
				{
					var flags = lastUsedModelInstance.gameObject.hideFlags;
					if ((flags & (HideFlags.HideInHierarchy | HideFlags.NotEditable | HideFlags.DontSaveInBuild)) == 0)
						returnModel = lastUsedModelInstance;
				}
				if (returnModel != null &&
                    returnModel.gameObject.activeInHierarchy)
				{
					return returnModel;
				}

				foreach (var model in InternalCSGModelManager.Models)
				{
					var flags = model.gameObject.hideFlags;
					if ((flags & (HideFlags.HideInHierarchy | HideFlags.NotEditable | HideFlags.DontSaveInBuild)) == 0 &&
                        model.gameObject.activeInHierarchy)
					{
						// don't want new stuff to be added to a prefab instance
						if (CSGPrefabUtility.IsPrefabAssetOrInstance(model.gameObject))
						{
							continue;
						}
						return model;
					}
				}
				return null;
			}
			set
			{
                if (!value)
                    return;

				var flags = value.gameObject.hideFlags;
				if ((flags & (HideFlags.HideInHierarchy | HideFlags.NotEditable | HideFlags.DontSaveInBuild)) != 0 &&
                    value.gameObject.activeInHierarchy)
					return;

				// don't want new stuff to be added to a prefab instance
				if (CSGPrefabUtility.IsPrefabAssetOrInstance(value.gameObject))
				{
					return;
				}

				lastUsedModelInstance = value;
			}
		}

		public static Transform FindParentToAssignTo(LegacyBrushIntersection intersection)
		{
			if (intersection.brush == null || 
				CSGPrefabUtility.IsPrefabAsset(intersection.brush.gameObject))
			{
				var lastModel = SelectionUtility.LastUsedModel;
				if (lastModel == null || 
					CSGPrefabUtility.IsPrefabAsset(lastModel.gameObject))
					return null;

				return lastModel.transform;
			}

			var hoverParent	= intersection.brush.transform.parent;
			var iterator	= hoverParent;
			while (iterator != null)
			{
				var node = iterator.GetComponent<CSGNode>();
				if (node != null)
					hoverParent = node.transform;
				iterator = iterator.transform.parent;
			}
			if (!hoverParent)
				return null;
			if (CSGPrefabUtility.GetCorrespondingObjectFromSource(hoverParent.gameObject) != null)
				return null;
			return hoverParent;
		}

		public static Quaternion FindDragOrientation(SceneView sceneView, Vector3 normal, PrefabSourceAlignment sourceSurfaceAlignment, PrefabDestinationAlignment destinationSurfaceAlignment)
		{
			Quaternion srcRotation;
			switch (sourceSurfaceAlignment)
			{
				default:
				case PrefabSourceAlignment.AlignedFront: srcRotation = Quaternion.LookRotation(MathConstants.forwardVector3); break;
				case PrefabSourceAlignment.AlignedBack: srcRotation = Quaternion.LookRotation(MathConstants.backVector3); break;
				case PrefabSourceAlignment.AlignedLeft: srcRotation = Quaternion.LookRotation(MathConstants.rightVector3); break;
				case PrefabSourceAlignment.AlignedRight: srcRotation = Quaternion.LookRotation(MathConstants.leftVector3); break;
				case PrefabSourceAlignment.AlignedTop: srcRotation = Quaternion.LookRotation(MathConstants.upVector3, MathConstants.forwardVector3); break;
				case PrefabSourceAlignment.AlignedBottom: srcRotation = Quaternion.LookRotation(MathConstants.downVector3, MathConstants.backVector3); break;
			}

			switch (destinationSurfaceAlignment)
			{
				default:
				case PrefabDestinationAlignment.AlignToSurface:
				{
					var tangent = MathConstants.upVector3; // assume up is up in the world
					var absX = Mathf.Abs(normal.x);
					var absY = Mathf.Abs(normal.y);
					var absZ = Mathf.Abs(normal.z);

					// if our surface is a floor / ceiling then assume up is the axis 
					// aligned vector that is most aligned with the camera's up vector
					if (absX <= absY && absX <= absZ && absY > absZ)
					{
						tangent = GeometryUtility.SnapToClosestAxis(sceneView.camera ? sceneView.camera.transform.up : MathConstants.upVector3);
					}

					return Quaternion.LookRotation(normal, tangent) * srcRotation;
				}
				case PrefabDestinationAlignment.AlignSurfaceUp:
				{
					normal.y = 0;
					normal.Normalize();
					if (normal.sqrMagnitude == 0)
						normal = GeometryUtility.SnapToClosestAxis(sceneView.camera ? sceneView.camera.transform.forward : MathConstants.forwardVector3);

					var tangent = MathConstants.upVector3; // assume up is up in the world
					var absX = Mathf.Abs(normal.x);
					var absY = Mathf.Abs(normal.y);
					var absZ = Mathf.Abs(normal.z);

					// if our surface is a floor / ceiling then assume up is the axis 
					// aligned vector that is most aligned with the camera's up vector
					if (absX <= absY && absX <= absZ && absY > absZ)
					{
						tangent = GeometryUtility.SnapToClosestAxis(sceneView.camera ? sceneView.camera.transform.up : MathConstants.upVector3);
					}

					return Quaternion.LookRotation(normal, tangent) * srcRotation;
				}
				case PrefabDestinationAlignment.Default:
				{
					return Quaternion.identity;
				}
			}
		}

		static bool				shiftPressed		= false;
		static bool				actionKeyPressed	= false;
		static EventModifiers	currentModifiers	= EventModifiers.None;
		public static EventModifiers CurrentModifiers { get { return currentModifiers; } internal set { currentModifiers = value; } } 

		public static void HandleEvents()
		{
			switch (Event.current.type)
			{
				case EventType.MouseDown:
				case EventType.MouseUp:
				case EventType.MouseDrag:
				case EventType.MouseMove:
				case EventType.KeyDown:
				case EventType.KeyUp:
				case EventType.ValidateCommand:
				case EventType.ExecuteCommand:
				case EventType.Repaint:
				{
					shiftPressed		= Event.current.shift;
					actionKeyPressed	= EditorGUI.actionKey;
					currentModifiers	= Event.current.modifiers & (EventModifiers.Alt | EventModifiers.Control | EventModifiers.Shift | EventModifiers.Command);
					break;
				}
			}
		}

		public static void HideObjectsRemoteOnly(List<GameObject> gameObjects)
		{
			if (gameObjects == null || 
				gameObjects.Count == 0)
				return;
			
			for (var i = gameObjects.Count - 1; i >= 0; i--)
			{
				if (gameObjects[i])
					gameObjects[i].SetActive(false);
			}

			InternalCSGModelManager.CheckTransformChanged();
			InternalCSGModelManager.OnHierarchyModified();
			InternalCSGModelManager.UpdateRemoteMeshes();
		}

		public static void ShowObjectsAndUpdate(List<GameObject> gameObjects)
		{
			if (gameObjects == null || 
				gameObjects.Count == 0)
				return;

			for (var i = gameObjects.Count - 1; i >= 0; i--)
			{
				if (gameObjects[i])
					gameObjects[i].SetActive(true);
			}

			InternalCSGModelManager.CheckTransformChanged();
			InternalCSGModelManager.OnHierarchyModified();
			InternalCSGModelManager.UpdateMeshes(forceUpdate: true);
			MeshInstanceManager.UpdateHelperSurfaceVisibility();
		}


		public static bool IsSnappingToggled
		{
			get 
			{
				return actionKeyPressed;
			}
		}

		public static SelectionType GetEventSelectionType()
		{
			if (shiftPressed && actionKeyPressed) return SelectionType.Subtractive;
			if (                actionKeyPressed) return SelectionType.Toggle;
			if (shiftPressed                    ) return SelectionType.Additive;
			return SelectionType.Replace;
		}

		public static void DeselectAll()
		{
			if (EditModeManager.DeselectAll())
				return;
			DeselectAllBrushes();
		}

		public static void DeselectAllBrushes()
		{
			Selection.activeObject = null;
		}

		public static void ToggleSelectedObjectVisibility()
		{
			Undo.IncrementCurrentGroup();
			int undo_group_index = Undo.GetCurrentGroup();

			var selected = Selection.gameObjects.ToArray();
			Undo.RecordObjects(selected, "Toggle Object Visibility");
			bool haveVisibleSelection = false;
			for (int i = 0; i < selected.Length; i++)
			{
				haveVisibleSelection = selected[i].activeInHierarchy || haveVisibleSelection;
			}
			if (haveVisibleSelection)
			{
				for (int i = 0; i < selected.Length; i++)
					selected[i].SetActive(false);
			} else
			{
				for (int i = 0; i < selected.Length; i++)
					selected[i].SetActive(true);
			}

			Undo.CollapseUndoOperations(undo_group_index);
		}

		public static void HideSelectedObjects()
		{
			Undo.IncrementCurrentGroup();
			int undo_group_index = Undo.GetCurrentGroup();

			var selected = Selection.gameObjects.ToArray();
			Undo.RecordObjects(selected, "Hiding Objects");
			for (int i = 0; i < selected.Length; i++)
				selected[i].SetActive(false);

			Undo.CollapseUndoOperations(undo_group_index);
		}

		public static void UnHideAll()
		{
			Undo.IncrementCurrentGroup();
			int undo_group_index = Undo.GetCurrentGroup();

			for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
			{
				var activeScene = SceneManager.GetSceneAt(sceneIndex);
				var rootGameObjects = activeScene.GetRootGameObjects();
				for (int i = 0; i < rootGameObjects.Length; i++)
				{
					var children = rootGameObjects[i].GetComponentsInChildren<Transform>(true);
					for (int c = 0; c < children.Length; c++)
					{
						var transform = children[c];
						var gameObject = transform.gameObject;
						if (gameObject.activeInHierarchy || (gameObject.hideFlags != HideFlags.None))
							continue;

						Undo.RecordObject(gameObject, "Un-hiding Object");
						gameObject.SetActive(true);
					}
				}
			}
			Undo.CollapseUndoOperations(undo_group_index);
		}

		public static void HideUnselectedObjects()
		{
			Undo.IncrementCurrentGroup();
			var undoGroupIndex  = Undo.GetCurrentGroup();

			var selected		= Selection.gameObjects.ToList();
			var selectedIDs		= new HashSet<int>();

			var models = InternalCSGModelManager.Models;
			for (var i = 0; i < models.Length; i++)
			{
				var model = models[i];
                if (!ModelTraits.IsModelSelectable(model))
					continue;

				if (!model.generatedMeshes)
					continue;

				var meshContainerChildren = model.generatedMeshes.GetComponentsInChildren<Transform>();
				foreach (var child in meshContainerChildren)
					selected.Add(child.gameObject);
			}

			for (int i = 0; i < selected.Count; i++) // we keep adding parents, and their parents until we hit the root-objects
			{
				selectedIDs.Add(selected[i].GetInstanceID());
				var transform = selected[i].transform;
				var parent    = transform.parent;
				if (parent == null)
					continue;
				selected.Add(parent.gameObject);
			}

			for (var sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
			{
				var activeScene = SceneManager.GetSceneAt(sceneIndex);
				var rootGameObjects = activeScene.GetRootGameObjects();
				for (var i = 0; i < rootGameObjects.Length; i++)
				{
					var children = rootGameObjects[i].GetComponentsInChildren<Transform>();
					for (var c = 0; c < children.Length; c++)
					{
						var transform = children[c];
						var gameObject = transform.gameObject;
						if (!gameObject.activeInHierarchy || (gameObject.hideFlags != HideFlags.None))
							continue;

						if (selectedIDs.Contains(gameObject.GetInstanceID()))
							continue;

						Undo.RecordObject(gameObject, "Hiding Object");
						gameObject.SetActive(false);
					}
				}
			}

			Undo.CollapseUndoOperations(undoGroupIndex);
		}


#region DoSelectionClick
		public static void DoSelectionClick(SceneView sceneView, bool ignoreInvisibleSurfaces = true)
		{
            var camera = sceneView.camera;
			GameObject gameobject;
			SceneQueryUtility.FindClickWorldIntersection(camera, Event.current.mousePosition, out gameobject, ignoreInvisibleSurfaces);
            
			gameobject = SceneQueryUtility.FindSelectionBase(gameobject);

            var selectedObjectsOnClick = new List<int>(Selection.instanceIDs);
			bool addedSelection = false;
			if (EditorGUI.actionKey)
			{
				if (gameobject != null)
				{
					var instanceID = gameobject.GetInstanceID();
					if (selectedObjectsOnClick.Contains(instanceID))
					{
						selectedObjectsOnClick.Remove(instanceID);
					}
					else
					{
						selectedObjectsOnClick.Add(instanceID);
						addedSelection = true;
					}

					if (selectedObjectsOnClick.Count == 0)
						Selection.activeTransform = null;
					else
						Selection.instanceIDs = selectedObjectsOnClick.ToArray();
				}
			}
			else
			if (Event.current.shift)
			{
				if (gameobject != null)
				{
					var instanceID = gameobject.GetInstanceID();
					selectedObjectsOnClick.Add(instanceID);
					Selection.instanceIDs = selectedObjectsOnClick.ToArray();
					addedSelection = true;
				}
			}
			else
			if (Event.current.alt)
			{
				if (gameobject != null)
				{
					var instanceID = gameobject.GetInstanceID();
					selectedObjectsOnClick.Remove(instanceID);
					Selection.instanceIDs = selectedObjectsOnClick.ToArray();
					return;
				}
			}
			else
			{
				Selection.activeGameObject = gameobject;
				addedSelection = true;
			}

			if (!addedSelection)
			{
				foreach (var item in Selection.GetFiltered(typeof(CSGBrush), SelectionMode.Deep))
				{
					var brush = item as CSGBrush;
					if (brush.ChildData == null ||
                        !ModelTraits.IsModelEditable(brush.ChildData.Model))
						continue;
					SelectionUtility.LastUsedModel = brush.ChildData.Model;
					break;
				}
			}
			else
			if (gameobject != null)
			{
				var brush = gameobject.GetComponent<CSGBrush>();
				if (brush != null)
				{
					if (brush.ChildData == null ||
                        !ModelTraits.IsModelEditable(brush.ChildData.Model))
						return;
					SelectionUtility.LastUsedModel = brush.ChildData.Model;
				}
			}
		}
#endregion


#region DoesSelectionContainCSGNodes
		public static bool DoesSelectionContainCSGNodes()
		{
			var gameObjects = Selection.gameObjects;
			if (gameObjects == null)
				return false;

			foreach (var gameObject in gameObjects)
			{
				if (gameObject.GetComponentInChildren<CSGNode>())
					return true;
			}
			return false;
		}
#endregion

	}
}
