using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEditor;
using RealtimeCSG;
using RealtimeCSG.Legacy;
using RealtimeCSG.Components;
using Object = UnityEngine.Object;
using RealtimeCSG.Foundation;

namespace InternalRealtimeCSG
{
	internal sealed class PointSelection
	{
		public PointSelection(int brushNodeID, int pointIndex) { BrushNodeID = brushNodeID; PointIndex = pointIndex; }
		public readonly int BrushNodeID;
		public readonly int PointIndex;
	}

	internal static class SceneQueryUtility
	{
		#region GetAllComponentsInScene
		public static List<T> GetAllComponentsInScene<T>(Scene scene)
			where T : Component
		{
			var items = new List<T>();
			var rootItems = GetRootGameObjectsInScene(scene);
			for (int i = 0; i < rootItems.Length; i++)
			{
				var root = rootItems[i];
				if (!root)
					continue;
				items.AddRange(root.GetComponentsInChildren<T>(true));
			}
			return items;
		}

		public static GameObject[] GetRootGameObjectsInScene(Scene scene)
		{
			if (scene.isLoaded)
				return scene.GetRootGameObjects();
			
			var rootLookup = new HashSet<Transform>();
			var transforms = Object.FindObjectsOfType<Transform>();
			for (int i = 0; i < transforms.Length;i++)
				rootLookup.Add(transforms[i].root);

			var rootArray = rootLookup.ToArray();
			var gameObjectArray = new GameObject[rootArray.Length];
			for (int i = 0; i < rootArray.Length; i++)
				gameObjectArray[i] = rootArray[i].gameObject;

			return gameObjectArray;
		}
		#endregion

		#region GetFirstGameObjectInSceneWithName
		public static GameObject GetFirstGameObjectInSceneWithName(Scene scene, string name)
		{
			foreach (var root in scene.GetRootGameObjects())
			{
				if (!root)
					continue;
				if (root.name == name)
					return root;
				foreach (var transform in root.GetComponentsInChildren<Transform>(true))
				{
					if (transform.name == name)
						return transform.gameObject;
				}
			}
			return null;
		}
		#endregion

		#region GetUniqueHiddenGameObjectInSceneWithName
		internal static GameObject GetUniqueHiddenGameObjectInSceneWithName(Scene scene, string name)
		{
			if (!scene.IsValid() || !scene.isLoaded)
				return null;

			var rootGameObjects = scene.GetRootGameObjects();
			GameObject foundRoot = null;
			for (int i = 0; i < rootGameObjects.Length; i++)
			{
				var root = rootGameObjects[i];
				if (!root)
					continue;

				if (root.hideFlags != HideFlags.None &&
					root.name == name)
				{
					if (foundRoot)
					{
						Object.DestroyImmediate(root);
						continue;
					}
					foundRoot = root;
				}

				var rootChildren = root.GetComponentsInChildren<Transform>(true);
				for (int j = 0; j < rootChildren.Length; j++)
				{
					var child = rootChildren[j];
					if (child == root)
						continue;
					if (!child)
						continue;

					if (child.hideFlags == HideFlags.None ||
						child.name != name)
						continue;
					
					if (foundRoot)
					{
						Object.DestroyImmediate(child.gameObject);
						continue;
					}
					foundRoot = child.gameObject;
				}
			}
			return foundRoot;
		}
		#endregion


		#region GetGroupObjectIfObjectIsPartOfGroup
		public static GameObject GetGroupGameObjectIfObjectIsPartOfGroup(GameObject gameObject)
		{
			if (gameObject == null)
				return null;

			var node = gameObject.GetComponentInChildren<CSGNode>();
			if (!node)
				return gameObject;

			var operation = GetGroupOperationForNode(node);
			return operation == null ? gameObject : operation.gameObject;
		}
        #endregion

        internal static bool GameObjectContainsAttribute<T>(GameObject go) where T : Attribute
        {
            var behaviours = go.GetComponents(typeof(Component));
            for (var index = 0; index < behaviours.Length; index++)
            {
                var behaviour = behaviours[index];
                if (behaviour == null)
                    continue;

                var behaviourType = behaviour.GetType();
                if (behaviourType.GetCustomAttributes(typeof(T), true).Length > 0)
                    return true;
            }
            return false;
        }

        internal static GameObject FindSelectionBase(GameObject go)
        {
            if (go == null)
                return null;

#if UNITY_2018_3_OR_NEWER
            Transform prefabBase = null;
            if (PrefabUtility.IsPartOfNonAssetPrefabInstance(go))
            {
                prefabBase = PrefabUtility.GetOutermostPrefabInstanceRoot(go).transform;
            }
#endif

            GameObject group = null;
            Transform groupTransform = null;
            var node = go.GetComponentInChildren<CSGNode>();
            if (node)
            {
                var operation = GetGroupOperationForNode(node);
                group = (operation == null) ? null : operation.gameObject;
                groupTransform = (operation == null) ? null : operation.transform;
            }


            Transform tr = go.transform;
            while (tr != null)
            {
#if UNITY_2018_3_OR_NEWER
                if (tr == prefabBase)
                    return tr.gameObject;
#endif
                if (tr == groupTransform)
                    return group;

                if (GameObjectContainsAttribute<SelectionBaseAttribute>(tr.gameObject))
                    return tr.gameObject;

                tr = tr.parent;
            }

            return go;
        }

        #region GetGroupOperationForNode (private)
        private static CSGOperation GetGroupOperationForNode(CSGNode node)
		{
			if (!node)
				return null;

			var parent = node.transform.parent;
			while (parent)
			{
				var model = parent.GetComponent<CSGModel>();
				if (model)
					return null;

				var parentOp = parent.GetComponent<CSGOperation>();
				if (parentOp &&
					//!parentOp.PassThrough && 
					parentOp.HandleAsOne)
					return parentOp;

				parent = parent.transform.parent;
			}
			return null;
		}
        #endregion

        #region GetTopMostGroupForNode
		public static CSGNode GetTopMostGroupForNode(CSGNode node)
		{
			if (!node)
				return null;

			var topSelected = node;
			var parent = node.transform.parent;
			while (parent)
			{
				var model = parent.GetComponent<CSGModel>();
				if (model)
					break;

				var parentOp = parent.GetComponent<CSGOperation>();
				if (parentOp &&
					parentOp.HandleAsOne &&
					!parentOp.PassThrough)
					topSelected = parentOp;

				parent = parent.transform.parent;
			}
			return topSelected;
		}
        #endregion


        #region DeselectAllChildBrushes (private)
		private static void DeselectAllChildBrushes(Transform transform, HashSet<GameObject> objectsInFrustum)
		{
			var visibleLayers = Tools.visibleLayers;
			 
			for (int i = 0, childCount = transform.childCount; i < childCount; i++)
			{
				var childTransform = transform.GetChild(i);
				var childNode = childTransform.GetComponent<CSGNode>();
				if (!childNode || (childNode is CSGModel) || ((1 << childNode.gameObject.layer) & visibleLayers) == 0)
					continue;

				var childGameObject = childTransform.gameObject;
				objectsInFrustum.Remove(childGameObject);
				DeselectAllChildBrushes(childTransform.transform, objectsInFrustum);
			}
		}
        #endregion

        #region AreAllBrushesSelected (private)
		private static bool AreAllBrushesSelected(Transform transform, HashSet<GameObject> objectsInFrustum)
		{			
			var visibleLayers = Tools.visibleLayers;
			

			var allChildrenSelected = true;
			var i = 0;
			var childCount = transform.childCount;
			for (; i < childCount; i++)
			{
				var childTransform = transform.GetChild(i);
				var childNode = childTransform.GetComponent<CSGNode>();
				if (!childNode || (childNode is CSGModel) || ((1 << childNode.gameObject.layer) & visibleLayers) == 0)
				{
					continue;
				}

				var childGameObject = childTransform.gameObject;
				if (!childTransform.gameObject.activeInHierarchy)
				{
					objectsInFrustum.Remove(childGameObject);
					continue;
				}

				if (objectsInFrustum.Contains(childGameObject))
				{
					objectsInFrustum.Remove(childGameObject);
					continue;
				}

				var childOperation = childNode as CSGOperation;
				if (childOperation == null ||
					!childOperation.PassThrough)
				{
					objectsInFrustum.Remove(childGameObject);
					allChildrenSelected = false;
					break;
				}

				var result = AreAllBrushesSelected(childTransform, objectsInFrustum);
				objectsInFrustum.Remove(childGameObject);

				if (result)
					continue;

				objectsInFrustum.Remove(childGameObject);
				allChildrenSelected = false;
				break;
			}
			if (allChildrenSelected)
				return true;

			for (; i < childCount; i++)
			{
				var childTransform = transform.GetChild(i);
				var childNode = childTransform.GetComponent<CSGNode>();
				if (!childNode || (childNode is CSGModel))
					continue;

				var childGameObject = childTransform.gameObject;
				objectsInFrustum.Remove(childGameObject);
				DeselectAllChildBrushes(childTransform.transform, objectsInFrustum);
			}
			return false;
		}
        #endregion


        #region GetItemsInFrustum
        public static bool GetItemsInFrustum(Plane[] planes,
											 HashSet<GameObject> objectsInFrustum)
		{
			if (objectsInFrustum == null)
				return false;

			objectsInFrustum.Clear();
			var found = false;
			foreach (var model in InternalCSGModelManager.Models)
			{
				if (!ModelTraits.WillModelRender(model))
					continue;
				found = InternalCSGModelManager.External.GetItemsInFrustum(model, planes, objectsInFrustum) || found;
			}

			var visibleLayers = Tools.visibleLayers;

			var items = objectsInFrustum.ToArray();
			for (var i = items.Length - 1; i >= 0; i--)
			{
				var child = items[i];
				var node = child.GetComponent<CSGNode>();
				if (!node || ((1 << node.gameObject.layer) & visibleLayers) == 0)
					continue;

				if (!objectsInFrustum.Contains(child))
					continue;

				while (true)
				{
					var parent = GetGroupOperationForNode(node);
					if (!parent ||
						!AreAllBrushesSelected(parent.transform, objectsInFrustum))
						break;

					objectsInFrustum.Add(parent.gameObject);
					node = parent;
				}
			}
			return found;
		}
        #endregion

        #region GetPointsInFrustum
		internal static PointSelection[] GetPointsInFrustum(Camera camera,
															Plane[] planes,
														    CSGBrush[] brushes,
															ControlMeshState[] controlMeshStates,
															bool ignoreHiddenPoints)
		{
			var pointSelection = new List<PointSelection>();
			for (var t = 0; t < brushes.Length; t++)
			{
				var targetMeshState = controlMeshStates[t];
				if (targetMeshState == null)
					continue;

				var cameraState = targetMeshState.GetCameraState(camera, false);

				for (var p = 0; p < targetMeshState.WorldPoints.Length; p++)
				{
					if (ignoreHiddenPoints && cameraState.WorldPointBackfaced[p])
						continue;
					var point = targetMeshState.WorldPoints[p];
					var found = true;
					for (var i = 0; i < 6; i++)
					{
						if (!(planes[i].GetDistanceToPoint(point) > MathConstants.DistanceEpsilon))
							continue;

						found = false;
						break;
					}

					if (found)
					{
						pointSelection.Add(new PointSelection(t, p));
					}
				}
			}
			return pointSelection.ToArray();
		}
        #endregion

        #region DeepSelection (private)

        private static List<GameObject> deepClickIgnoreGameObjectList = new List<GameObject>();
        private static List<CSGBrush> deepClickIgnoreBrushList = new List<CSGBrush>();
        private static Vector2 _prevSceenPos = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        private static Camera _prevCamera;

        private static void ResetDeepClick(bool resetPosition = true)
		{
            deepClickIgnoreGameObjectList.Clear();
            deepClickIgnoreBrushList.Clear();
            if (resetPosition)
            {
                _prevSceenPos = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
                _prevCamera = null;
            }
        }

        #endregion

        #region Find..xx..Intersection

        #region FindClickWorldIntersection

        
        public class HideFlagsState
        {
            public Dictionary<UnityEngine.GameObject, CSGModel>	    generatedComponents;
            public Dictionary<UnityEngine.GameObject, HideFlags>	hideFlags;
        }

        public static HideFlagsState BeginPicking(GameObject[] ignoreGameObjects)
        {
            var state = new HideFlagsState()
            {
                generatedComponents = new Dictionary<UnityEngine.GameObject, CSGModel>(),
                hideFlags           = new Dictionary<UnityEngine.GameObject, HideFlags>()
            };

            foreach(var model in CSGModelManager.GetAllModels())
            {
                if (!model.generatedMeshes)
                    continue;

                var renderers	= model.generatedMeshes.GetComponentsInChildren<Renderer>();
                if (renderers != null)
                {
					foreach (var renderer in renderers)
						state.generatedComponents[renderer.gameObject] = model;
                }

                var colliders	= model.generatedMeshes.GetComponentsInChildren<Collider>();
                if (colliders != null)
                {
                    foreach (var collider in colliders)
                        state.generatedComponents[collider.gameObject] = model;
                }
			}
			if (state.generatedComponents != null)
            {
                foreach(var pair in state.generatedComponents)
                {
                    var gameObject  = pair.Key;
                    var model       = pair.Value;

                    state.hideFlags[gameObject] = gameObject.hideFlags;

                    if (ignoreGameObjects != null &&
                        ArrayUtility.Contains(ignoreGameObjects, model.gameObject))
                    {
                        gameObject.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
                    } else
                    {
                        gameObject.hideFlags = HideFlags.None;
                    }
                }
            }
            return state;
        }
        
        public static CSGModel EndPicking(HideFlagsState state, UnityEngine.GameObject pickedObject)
        {
            if (state == null || state.hideFlags == null)
                return null;
            
            foreach (var pair in state.hideFlags)
                pair.Key.hideFlags = pair.Value;
            
            if (object.Equals(pickedObject, null))
                return null;

            if (state.generatedComponents == null)
                return null;

            CSGModel model;
            if (state.generatedComponents.TryGetValue(pickedObject, out model))
                return model;
            return null;
        }

        static GameObject FindFirstWorldIntersection(Camera camera, Vector2 screenPos, Vector3 worldRayStart, Vector3 worldRayEnd, List<GameObject> ignoreGameObjects = null, List<CSGBrush> ignoreBrushes = null, bool ignoreInvisibleSurfaces = true)
        {
            var wireframeShown = CSGSettings.IsWireframeShown(camera);

            TryAgain:

            CSGModel model;
            GameObject gameObject = null;

            var ignoreGameObjectArray = (ignoreGameObjects == null || ignoreGameObjects.Count == 0) ? null : ignoreGameObjects.ToArray();
            {
                var flagState = BeginPicking(ignoreGameObjectArray);
                try { gameObject = HandleUtility.PickGameObject(screenPos, false, ignoreGameObjectArray); }
                finally { model = EndPicking(flagState, gameObject); }
            }

            if (!object.Equals(gameObject, null) && model)
            {
                if (ignoreGameObjects != null &&
                    ignoreGameObjects.Contains(model.gameObject))
                {
                    Debug.Assert(false);
                } else
                {
                    LegacyBrushIntersection[] _deepClickIntersections;
                    var ignoreBrushesArray = (ignoreGameObjects == null || ignoreGameObjects.Count == 0) ? null : ignoreBrushes.ToArray();
                    if (FindWorldIntersection(model, worldRayStart, worldRayEnd, out _deepClickIntersections, ignoreInvisibleSurfaces: ignoreInvisibleSurfaces && !wireframeShown, ignoreUnrenderables: !wireframeShown, ignoreBrushes: ignoreBrushesArray))
                    {
                        var visibleLayers = Tools.visibleLayers;
                        for (int i = 0; i < _deepClickIntersections.Length; i++)
                        {
                            gameObject = _deepClickIntersections[i].gameObject;
                            if (((1 << gameObject.layer) & visibleLayers) == 0)
                                continue;

                            return gameObject;
                        }
                    }
                    if (ignoreGameObjects != null)
                    {
                        ignoreGameObjects.Add(model.gameObject);
                        foreach (var component in model.generatedMeshes.GetComponentsInChildren<GeneratedMeshInstance>()) ignoreGameObjects.Add(component.gameObject);
                        goto TryAgain;
                    }
                }

                // Try finding a regular unity object instead
                gameObject = HandleUtility.PickGameObject(screenPos, false, ignoreGameObjectArray);
            }

                
            // If we really didn't find anything, just return null
            if (ReferenceEquals(gameObject, null) ||
                !gameObject)
                return null;
             
            // Make sure our found gameobject isn't sneakily a CSG related object (should not happen at this point)
            if (!gameObject.GetComponent<CSGModel>() &&
                !gameObject.GetComponent<CSGBrush>() &&
                !gameObject.GetComponent<CSGOperation>() &&
                !gameObject.GetComponent<GeneratedMeshInstance>() &&
                !gameObject.GetComponent<GeneratedMeshes>())
                return gameObject;

            // If we're not ignoring something, just return null after all
            if (ignoreGameObjects == null)
                return null;

            // Ignore this object and try again (might've been blocking a model)                
            ignoreGameObjects.Add(gameObject);
            goto TryAgain;
        }


        public static bool FindClickWorldIntersection(Camera camera, Vector2 screenPos, out GameObject foundObject, bool ignoreInvisibleSurfaces = true)
		{
			foundObject = null;
			if (!camera)
				return false;

            var worldRay		= HandleUtility.GUIPointToWorldRay(screenPos);
			var worldRayStart	= worldRay.origin;
			var worldRayVector	= (worldRay.direction * (camera.farClipPlane - camera.nearClipPlane));
			var worldRayEnd		= worldRayStart + worldRayVector;

            // If we moved our mouse, reset our ignore list
            if (_prevSceenPos != screenPos ||
                _prevCamera != camera)
                ResetDeepClick();

            _prevSceenPos = screenPos;
            _prevCamera = camera;

            // Get the first click that is not in our ignore list
            foundObject = FindFirstWorldIntersection(camera, screenPos, worldRayStart, worldRayEnd, deepClickIgnoreGameObjectList, deepClickIgnoreBrushList, ignoreInvisibleSurfaces);

            // If we haven't found anything, try getting the first item in our list that's either a brush or a regular gameobject (loop around)
            if (object.Equals(foundObject, null))
            {
                bool found = false;
                for (int i = 0; i < deepClickIgnoreGameObjectList.Count; i++)
                {
                    foundObject = deepClickIgnoreGameObjectList[i];
                    
                    // We don't want models or mesh containers since they're in this list to skip, and should never be selected
                    if (!foundObject ||
                        foundObject.GetComponent<CSGModel>() ||
                        foundObject.GetComponent<GeneratedMeshInstance>() ||
                        foundObject.GetComponent<GeneratedMeshes>())
                        continue;

                    found = true;
                    break;
                }

                if (!found)
                {
                    // We really didn't find anything
                    foundObject = null;
                    ResetDeepClick();
                    return false;
                } else
                {
                    // Reset our list so we only skip our current selection on the next click
                    ResetDeepClick(
                        resetPosition: false // But make sure we remember our current mouse position
                        );
                }
            }

            // Remember our gameobject/brush so we don't select it on the next click
            var brush = foundObject.GetComponent<CSGBrush>();
            if (brush)
                deepClickIgnoreBrushList.Add(brush);
            deepClickIgnoreGameObjectList.Add(foundObject);
            return true;
        }
        #endregion

        #region FindMeshIntersection
		public static LegacyBrushIntersection FindMeshIntersection(Camera camera, Vector2 screenPos, CSGBrush[] ignoreBrushes = null, HashSet<Transform> ignoreTransforms = null)
		{
			var worldRay = HandleUtility.GUIPointToWorldRay(screenPos);
			var hit = HandleUtility.RaySnap(worldRay);
			while (hit != null)
			{
				var rh = (RaycastHit)hit;
				if (ignoreTransforms != null && ignoreTransforms.Contains(rh.transform))
				{
					worldRay.origin = rh.point + (worldRay.direction * 0.00001f);
					hit = HandleUtility.RaySnap(worldRay);
					continue;
				}

				// Check if it's a mesh ...
				if (rh.transform.GetComponent<MeshRenderer>() &&
					// .. but not one we generated
					!rh.transform.GetComponent<CSGNode>() &&
					!rh.transform.GetComponent<GeneratedMeshInstance>())
				{
					return new LegacyBrushIntersection
					{
						brushNodeID = CSGNode.InvalidNodeID,
						surfaceIndex = -1,
						worldIntersection = rh.point,
						worldPlane = new CSGPlane(-rh.normal, rh.point)
					};
				}
				break;
			}

			LegacyBrushIntersection intersection;
			if (FindWorldIntersection(camera, worldRay, out intersection, ignoreBrushes: ignoreBrushes))
				return intersection;

			var gridPlane = RealtimeCSG.CSGGrid.CurrentGridPlane;
			var intersectionPoint = gridPlane.RayIntersection(worldRay);
			if (float.IsNaN(intersectionPoint.x) ||
				float.IsNaN(intersectionPoint.y) ||
				float.IsNaN(intersectionPoint.z) ||
				float.IsInfinity(intersectionPoint.x) ||
				float.IsInfinity(intersectionPoint.y) ||
				float.IsInfinity(intersectionPoint.z))
			{
				intersectionPoint = worldRay.GetPoint(10);
				return new LegacyBrushIntersection
				{
					brushNodeID = CSGNode.InvalidNodeID,
					surfaceIndex = -1,
					worldIntersection = MathConstants.zeroVector3,
					worldPlane = new CSGPlane(gridPlane.normal, intersectionPoint)
				};
			}

			return new LegacyBrushIntersection
			{
				brushNodeID = CSGNode.InvalidNodeID,
				surfaceIndex = -1,
				worldIntersection = intersectionPoint,
				worldPlane = gridPlane
			};
		}
        #endregion

        #region FindUnityWorldIntersection
		public static bool FindUnityWorldIntersection(Camera camera, Vector2 screenPos, out GameObject foundObject, bool ignoreInvisibleSurfaces = true)
		{
			foundObject = null;
			if (!camera)
				return false;

			var wireframeShown	= CSGSettings.IsWireframeShown(camera);
			var worldRay		= HandleUtility.GUIPointToWorldRay(screenPos);
			var worldRayStart	= worldRay.origin;
			var worldRayVector	= (worldRay.direction * (camera.farClipPlane - camera.nearClipPlane));
			var worldRayEnd		= worldRayStart + worldRayVector;

			CSGModel intersectionModel = null;

			LegacyBrushIntersection[] intersections;
			if (FindMultiWorldIntersection(worldRayStart, worldRayEnd, out intersections, ignoreInvisibleSurfaces: ignoreInvisibleSurfaces && !wireframeShown))
			{
				var visibleLayers = Tools.visibleLayers;
				for (int i = 0; i < intersections.Length; i++)
				{
					if (((1 << intersections[i].gameObject.layer) & visibleLayers) == 0)
						continue;
					intersectionModel = intersections[i].model;
					break;
				}
			}
			/*
			GameObject[] modelMeshes = null;
			HideFlags[] hideFlags = null;
			if (intersectionModel != null)
			{
				modelMeshes = CSGModelManager.GetModelMeshes(intersectionModel);
				if (modelMeshes != null)
				{
					hideFlags = new HideFlags[modelMeshes.Length];
					for (var i = 0; i < modelMeshes.Length; i++)
					{
						hideFlags[i] = modelMeshes[i].hideFlags;
						modelMeshes[i].hideFlags = HideFlags.None;
					}
				}
			}
			*/


			CSGModel foundModel;
			GameObject gameObject = null;
			var flagState = BeginPicking(null);
			try { gameObject = HandleUtility.PickGameObject(screenPos, false, null); }
			finally 
			{ 
				foundModel = EndPicking(flagState, gameObject);
			}
			if (foundModel == intersectionModel && intersectionModel)
				return false;
			/*
			var gameObject = HandleUtility.PickGameObject(screenPos, false);

			if (modelMeshes != null)
			{
				for (var i = 0; i < modelMeshes.Length; i++)
				{
					var modelMesh = modelMeshes[i];
					if (!modelMesh)
						continue;

					if (gameObject == modelMesh)
						gameObject = null;

					modelMesh.hideFlags = hideFlags[i];
				}
			}
			*/
			if (!gameObject ||
				gameObject.GetComponent<Canvas>() ||
				gameObject.GetComponent<CSGModel>() ||
				gameObject.GetComponent<CSGBrush>() ||
				gameObject.GetComponent<CSGOperation>() ||
				gameObject.GetComponent<GeneratedMeshInstance>() ||
				gameObject.GetComponent<GeneratedMeshes>())
				return false;

			foundObject = gameObject;
			return true;
		}
        #endregion

        #region FindWorldIntersection
		public static bool FindWorldIntersection(Camera camera, Vector2 screenPos, out LegacyBrushIntersection intersection, bool ignoreInvisibleSurfaces = true, bool ignoreUnrenderables = true, CSGBrush[] ignoreBrushes = null)
		{
			var worldRay = HandleUtility.GUIPointToWorldRay(screenPos);
			return FindWorldIntersection(camera, worldRay, out intersection, ignoreInvisibleSurfaces, ignoreUnrenderables, ignoreBrushes);
		}

		public static bool FindWorldIntersection(Camera camera, Ray worldRay, out LegacyBrushIntersection intersection, bool ignoreInvisibleSurfaces = true, bool ignoreUnrenderables = true, CSGBrush[] ignoreBrushes = null)
		{
			var rayStart = worldRay.origin;
			var rayVector = (worldRay.direction * (camera.farClipPlane - camera.nearClipPlane));
			var rayEnd = rayStart + rayVector;

			return FindWorldIntersection(rayStart, rayEnd, out intersection, ignoreInvisibleSurfaces, ignoreUnrenderables, ignoreBrushes);
		}
		
		static CSGModel[] __foundModels = new CSGModel[0];
		public static bool FindWorldIntersection(Vector3 rayStart, Vector3 rayEnd, out LegacyBrushIntersection intersection, bool ignoreInvisibleSurfaces = true, bool ignoreUnrenderables = true, CSGBrush[] ignoreBrushes = null)
		{
			intersection = null;
			if (InternalCSGModelManager.External == null ||
				InternalCSGModelManager.External.RayCastMulti == null)
				return false;

			var forceIgnoreInvisibleSurfaces = ignoreInvisibleSurfaces && !CSGSettings.ShowCulledSurfaces;

			var visibleLayers = Tools.visibleLayers;
			int foundModelCount = 0;

			if (__foundModels.Length < InternalCSGModelManager.Models.Length)
				__foundModels = new CSGModel[InternalCSGModelManager.Models.Length];

			for (var g = 0; g < InternalCSGModelManager.Models.Length; g++)
			{
				var model = InternalCSGModelManager.Models[g];
                
                if (!ModelTraits.IsModelSelectable(model))
					continue;

				if (ignoreUnrenderables && !ModelTraits.WillModelRender(model) &&
					!Selection.Contains(model.gameObject.GetInstanceID()))
					continue;

				__foundModels[foundModelCount] = model;
				foundModelCount++;
			}

			if (foundModelCount == 0)
				return false;

			LegacyBrushIntersection[] modelIntersections;
			if (!InternalCSGModelManager.External.RayCastMulti(	foundModelCount,
																__foundModels,
																rayStart,
																rayEnd,
																forceIgnoreInvisibleSurfaces,
																out modelIntersections,
																ignoreBrushes: ignoreBrushes))
				return false;

			for (var i = 0; i < modelIntersections.Length; i++)
			{
				var modelIntersection	= modelIntersections[i];
					
				if (intersection != null &&
					modelIntersection.distance > intersection.distance)
					continue;

				var brush = modelIntersection.gameObject.GetComponent<CSGBrush>();
				if (BrushTraits.IsSurfaceUnselectable(brush, modelIntersection.surfaceIndex, brush.ChildData.Model.IsTrigger, !ignoreInvisibleSurfaces))
					continue;

				modelIntersection.brush = brush;

				intersection = modelIntersection;
			}

			if (intersection == null)
				return false;
			
			return true;
		}
        #endregion

        #region FindWorldIntersection
        static bool FindWorldIntersection(CSGModel model, Vector3 worldRayStart, Vector3 worldRayEnd, out LegacyBrushIntersection[] intersections, bool ignoreInvisibleSurfaces = true, bool ignoreUnrenderables = true, CSGBrush[] ignoreBrushes = null)
        {
            intersections = null;
            if (InternalCSGModelManager.External == null ||
                InternalCSGModelManager.External.RayCastIntoModelMulti == null)
                return false;

            var foundIntersections = new Dictionary<CSGNode, LegacyBrushIntersection>();

            var visibleLayers = Tools.visibleLayers;
            ignoreInvisibleSurfaces = ignoreInvisibleSurfaces && !CSGSettings.ShowCulledSurfaces;
            if (!ModelTraits.IsModelSelectable(model))
                return false;

            if (ignoreUnrenderables && !ModelTraits.WillModelRender(model) &&
                !Selection.Contains(model.gameObject.GetInstanceID()))
                return false;

			LegacyBrushIntersection[] modelIntersections;
            if (!InternalCSGModelManager.External.RayCastIntoModelMulti(model,
                                                                        worldRayStart,
                                                                        worldRayEnd,
                                                                        ignoreInvisibleSurfaces,
                                                                        out modelIntersections,
                                                                        ignoreBrushes: ignoreBrushes))
                return false;

			for (var i = 0; i < modelIntersections.Length; i++)
            {
                var intersection = modelIntersections[i];
                var brush = intersection.gameObject.GetComponent<CSGBrush>();
                if (BrushTraits.IsSurfaceUnselectable(brush, intersection.surfaceIndex, brush.ChildData.Model.IsTrigger, ignoreSurfaceFlags: !ignoreInvisibleSurfaces))
                    continue;

				var currentNode = GetTopMostGroupForNode(brush);
                LegacyBrushIntersection other;
                if (foundIntersections.TryGetValue(currentNode, out other)
                    && other.distance <= intersection.distance)
                    continue;

				intersection.brush = brush;
                intersection.model = model;

                foundIntersections[currentNode] = modelIntersections[i];
            }

			if (foundIntersections.Count == 0)
                return false;

            var sortedIntersections = foundIntersections.Values.ToArray();
            Array.Sort(sortedIntersections, (x, y) => (int)Mathf.Sign(x.distance - y.distance));

            intersections = sortedIntersections;
            return true;
        }
        #endregion

        #region FindMultiWorldIntersection
        public static bool FindMultiWorldIntersection(Vector3 worldRayStart, Vector3 worldRayEnd, out LegacyBrushIntersection[] intersections, bool ignoreInvisibleSurfaces = true, bool ignoreUnrenderables = true, CSGBrush[] ignoreBrushes = null)
		{
			intersections = null;
			if (InternalCSGModelManager.External == null ||
				InternalCSGModelManager.External.RayCastIntoModelMulti == null)
				return false;

			var foundIntersections = new Dictionary<CSGNode, LegacyBrushIntersection>();
			
			var visibleLayers = Tools.visibleLayers;
			ignoreInvisibleSurfaces = ignoreInvisibleSurfaces && !CSGSettings.ShowCulledSurfaces;
			for (var g = 0; g < InternalCSGModelManager.Models.Length; g++)
			{
				var model = InternalCSGModelManager.Models[g];
                if (!ModelTraits.IsModelSelectable(model))
					continue;
					
				if (ignoreUnrenderables && !ModelTraits.WillModelRender(model) &&
					!Selection.Contains(model.gameObject.GetInstanceID()))
					continue;

				LegacyBrushIntersection[] modelIntersections;
				if (!InternalCSGModelManager.External.RayCastIntoModelMulti(model,
																			worldRayStart,
																			worldRayEnd,
																			ignoreInvisibleSurfaces,
																			out modelIntersections,
																			ignoreBrushes: ignoreBrushes))
					continue;
				
				for (var i = 0; i < modelIntersections.Length; i++)
				{
					var intersection	= modelIntersections[i];
					var brush			= intersection.gameObject.GetComponent<CSGBrush>();
					if (BrushTraits.IsSurfaceUnselectable(brush, intersection.surfaceIndex, brush.ChildData.Model.IsTrigger, ignoreSurfaceFlags: !ignoreInvisibleSurfaces))
						continue;
					
					var currentNode = GetTopMostGroupForNode(brush);
					LegacyBrushIntersection other;
					if (foundIntersections.TryGetValue(currentNode, out other)
						&& other.distance <= intersection.distance)
						continue;

					intersection.brush = brush;
					intersection.model = model;

					foundIntersections[currentNode] = modelIntersections[i];
				}
			}

			if (foundIntersections.Count == 0)
				return false;

			var sortedIntersections = foundIntersections.Values.ToArray();
			Array.Sort(sortedIntersections, (x, y) => (int)Mathf.Sign(x.distance - y.distance));
			
			intersections = sortedIntersections;
			return true;
		}
        #endregion

        #region FindBrushIntersection
		public static bool FindBrushIntersection(CSGBrush brush, Matrix4x4 modelTransformation, Vector3 rayStart, Vector3 rayEnd, out LegacyBrushIntersection intersection)
		{
			intersection = null;
			if (!brush || InternalCSGModelManager.External.RayCastIntoBrush == null)
				return false;
			
			if (!InternalCSGModelManager.External.RayCastIntoBrush(brush.brushNodeID, 
																   rayStart,
																   rayEnd,
																   modelTransformation,
																   out intersection,
																   false))
				return false;

			if (BrushTraits.IsSurfaceUnselectable(brush, intersection.surfaceIndex, brush.ChildData.Model.IsTrigger))
				return false;
			return true;
		}
        #endregion

        #region FindSurfaceIntersection
		public static bool FindSurfaceIntersection(Camera camera, CSGBrush brush, Matrix4x4 modelTransformation, Int32 surfaceIndex, Vector2 screenPos, out LegacySurfaceIntersection intersection)
		{
			var worldRay = HandleUtility.GUIPointToWorldRay(screenPos);
			var rayStart = worldRay.origin;
			var rayVector = (worldRay.direction * (camera.farClipPlane - camera.nearClipPlane));
			var rayEnd = rayStart + rayVector;

			intersection = null;
			if (!brush ||
				InternalCSGModelManager.External.RayCastIntoBrushSurface == null)
				return false;
			
			if (!InternalCSGModelManager.External.RayCastIntoBrushSurface(brush.brushNodeID,
																		  surfaceIndex,
																		  rayStart,
																		  rayEnd,
																		  modelTransformation,
																		  out intersection))
			{
				return false;
			}

			if (BrushTraits.IsSurfaceUnselectable(brush, surfaceIndex, brush.ChildData.Model.IsTrigger))
			{
				return false;
			}
			return true;
		}
        #endregion

        #endregion	
	}
}