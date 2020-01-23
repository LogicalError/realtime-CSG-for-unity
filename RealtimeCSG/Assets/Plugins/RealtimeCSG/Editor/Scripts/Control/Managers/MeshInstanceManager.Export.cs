//#define SHOW_GENERATED_MESHES
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEditor;
using RealtimeCSG;
using RealtimeCSG.Components;

namespace InternalRealtimeCSG
{
	internal sealed partial class MeshInstanceManager
	{
		public static void Export(CSGModel model, ExportType exportType, bool exportColliders)
		{
			string typeName;
			string extension;
			switch (exportType)
			{
				case ExportType.FBX: typeName = "FBX"; extension = @"fbx"; break;
				default:
				//case ExportType.UnityMesh:
					typeName = "Mesh"; extension = @"prefab"; exportType = ExportType.UnityMesh; break;
			}
			var newPath = model.exportPath;
			if (exportType != ExportType.UnityMesh)
			{
				newPath = UnityFBXExporter.ExporterMenu.GetNewPath(model.gameObject, typeName, extension, model.exportPath);
				if (string.IsNullOrEmpty(newPath))
					return;
			}

			model.ShowGeneratedMeshes = false;
			var foundModels = model.GetComponentsInChildren<CSGModel>(true);
			for (var index = 0; index < foundModels.Length; index++)
			{
				if (!foundModels[index].ShowGeneratedMeshes)
					continue;
				foundModels[index].ShowGeneratedMeshes = false;
				UpdateGeneratedMeshesVisibility(foundModels[index]);
			}

			GameObject tempExportObject;

			if (!string.IsNullOrEmpty(model.exportPath))
			{
				tempExportObject = new GameObject(System.IO.Path.GetFileNameWithoutExtension(model.exportPath));
				if (string.IsNullOrEmpty(tempExportObject.name))
					tempExportObject.name = model.name;
			} else
				tempExportObject = new GameObject(model.name);

			tempExportObject.transform.position		= MathConstants.zeroVector3;
			tempExportObject.transform.rotation		= MathConstants.identityQuaternion;
			tempExportObject.transform.localScale	= MathConstants.oneVector3;

			int colliderCounter = 1;
			int shadowOnlyCounter = 1;
			var materialMeshCounters = new Dictionary<Material, int>();
			
			var currentScene = model.gameObject.scene;
			var foundMeshContainers = SceneQueryUtility.GetAllComponentsInScene<GeneratedMeshes>(currentScene);
			var bounds = new AABB();
			bounds.Reset();
			var foundMeshFilters = new List<MeshFilter>();
			var foundMeshColliders = new List<MeshCollider>();
            foreach (var meshContainer in foundMeshContainers)
            {
                var owner = meshContainer.owner;
                if (!owner || !ArrayUtility.Contains(foundModels, owner))
                    continue;

                if (!meshContainer || !meshContainer.HasMeshInstances)
                    continue;

                foreach (var instance in meshContainer.MeshInstances)
                {
                    if (!instance)
                        continue;

                    Refresh(instance, model, postProcessScene: true, skipAssetDatabaseUpdate: true);

                    var surfaceType = GetSurfaceType(instance.MeshDescription, owner.Settings);
                    if (surfaceType != RenderSurfaceType.Normal &&
                        surfaceType != RenderSurfaceType.ShadowOnly &&
                        surfaceType != RenderSurfaceType.Collider)
                        continue;

                    int counter = 0;
                    if (instance.RenderMaterial)
                    {
                        if (!materialMeshCounters.TryGetValue(instance.RenderMaterial, out counter))
                        {
                            counter = 1;
                        } else
                            counter++;
                    }

                    var mesh = instance.SharedMesh;
                    if (!mesh.isReadable)
                    {
                        //bounds.Extend(mesh.bounds.min);
                        //bounds.Extend(mesh.bounds.max);
                    } else
                    {
                        var vertices = mesh.vertices;
                        for (int v = 0; v < vertices.Length; v++)
                            bounds.Extend(vertices[v]);
                    }


                    var subObj = UnityEngine.Object.Instantiate(instance.gameObject, MathConstants.zeroVector3, MathConstants.identityQuaternion) as GameObject;
                    subObj.hideFlags = HideFlags.None;
                    subObj.transform.position = owner.transform.position;
                    subObj.transform.rotation = owner.transform.rotation;
                    subObj.transform.localScale = owner.transform.localScale;
                    subObj.transform.SetParent(tempExportObject.transform, false);

                    var genMeshInstance = subObj.GetComponent<GeneratedMeshInstance>();

                    UnityEngine.Object.DestroyImmediate(genMeshInstance);

                    if (surfaceType == RenderSurfaceType.Collider)
                    {
                        subObj.name = "no-material Mesh (" + colliderCounter + ") COLLIDER"; colliderCounter++;
                    } else
                    {
                        if (surfaceType == RenderSurfaceType.ShadowOnly)
                        {
                            subObj.name = "shadow-only Mesh (" + shadowOnlyCounter + ")"; shadowOnlyCounter++;
                            var meshRenderer = subObj.GetComponent<MeshRenderer>();
                            if (meshRenderer)
                                meshRenderer.sharedMaterial = MaterialUtility.DefaultMaterial;
                        } else
                        {
                            Material renderMaterial = instance.RenderMaterial;
                            if (!renderMaterial)
                            {
                                renderMaterial = MaterialUtility.DefaultMaterial;
                                subObj.name = "missing-material Mesh (" + counter + ")"; counter++;
                            } else
                                subObj.name = renderMaterial.name + " Mesh (" + counter + ")"; counter++;
                            materialMeshCounters[instance.RenderMaterial] = counter;
                        }

                        var meshFilter = subObj.GetComponent<MeshFilter>();
                        if (meshFilter)
                            foundMeshFilters.Add(meshFilter);
                    }

                    var meshCollider = subObj.GetComponent<MeshCollider>();
                    if (meshCollider)
                        foundMeshColliders.Add(meshCollider);
                }
            }

            Undo.IncrementCurrentGroup();
			var groupIndex = Undo.GetCurrentGroup();
			Undo.SetCurrentGroupName("Exported model");
			try
			{
				Vector3 position = model.transform.position;
				if (float.IsInfinity(position.x) || float.IsNaN(position.x)) position.x = 0;
				if (float.IsInfinity(position.y) || float.IsNaN(position.y)) position.y = 0;
				if (float.IsInfinity(position.z) || float.IsNaN(position.z)) position.z = 0;

				Vector3 center = bounds.Center;
				switch (model.originType)
				{
					default:
					case OriginType.ModelCenter:	center = bounds.Center + position; break;
					case OriginType.ModelPivot:		center = position; break;
					case OriginType.WorldSpace:		center = Vector3.zero; break;
				}
				if (float.IsInfinity(center.x) || float.IsNaN(center.x)) center.x = 0;
				if (float.IsInfinity(center.y) || float.IsNaN(center.y)) center.y = 0;
				if (float.IsInfinity(center.z) || float.IsNaN(center.z)) center.z = 0;
				
				var modifiedMeshes = new Dictionary<Mesh, Mesh>();
				foreach (var meshFilter in foundMeshFilters)
				{
					var mesh = meshFilter.sharedMesh;
					if (!mesh.isReadable)
					{
						continue;
					}
					
					Mesh newMesh;
					if (!modifiedMeshes.TryGetValue(mesh, out newMesh))
					{
						newMesh = (Mesh)UnityEngine.Object.Instantiate(mesh);
						var vertices = mesh.vertices;
						for (int v = 0; v < vertices.Length; v++)
						{
							vertices[v] += position;
							vertices[v] -= center;
						}
						newMesh.vertices = vertices;
						newMesh.RecalculateBounds();
						modifiedMeshes[mesh] = newMesh;
					}
					meshFilter.sharedMesh = newMesh;
					meshFilter.transform.position = Vector3.zero;
				}
				
				foreach (var meshCollider in foundMeshColliders)
				{
					var mesh = meshCollider.sharedMesh;
					if (!mesh.isReadable)
					{
						continue;
					}
					
					Mesh newMesh;
					if (!modifiedMeshes.TryGetValue(mesh, out newMesh))
					{
						newMesh = (Mesh)UnityEngine.Object.Instantiate(mesh);
						var vertices = mesh.vertices;
						for (int v = 0; v < vertices.Length; v++)
						{
							vertices[v] += position;
							vertices[v] -= center;
						}
						newMesh.vertices = vertices;
						newMesh.RecalculateBounds();
						modifiedMeshes[mesh] = newMesh;
					}
					meshCollider.sharedMesh = newMesh;
					meshCollider.transform.position = Vector3.zero;
				}
				
				UnityEngine.GameObject prefabObj;
				GameObject modelGameObject;
				switch (exportType)
				{
					case ExportType.FBX:
					{
						if (!UnityFBXExporter.FBXExporter.ExportGameObjToFBX(tempExportObject, newPath, exportColliders: exportColliders))
						{
							//InternalCSGModelManager.ClearMeshInstances();
							EditorUtility.DisplayDialog("Warning", "Failed to export the FBX file.", "Ok");
							return;
						}
						prefabObj = AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(newPath);

						modelGameObject = CSGPrefabUtility.Instantiate(prefabObj);
						
						foreach (var renderer in modelGameObject.GetComponentsInChildren<MeshRenderer>())
						{
							var gameObject = renderer.gameObject;
							if (gameObject.name.EndsWith("COLLIDER"))
							{
								var filter = gameObject.GetComponent<MeshFilter>();
								var meshCollider = gameObject.AddComponent<MeshCollider>();
								meshCollider.sharedMesh = filter.sharedMesh;
								UnityEngine.Object.DestroyImmediate(renderer);
								UnityEngine.Object.DestroyImmediate(filter);
							}
						}
						model.exportPath = newPath;
						break;
					}
					default:
					//case ExportType.UnityMesh:
					{
						prefabObj = tempExportObject;// AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(newPath);
						modelGameObject = tempExportObject;

						foreach (var meshFilter in tempExportObject.GetComponentsInChildren<MeshFilter>())
						{
							var mesh = meshFilter.sharedMesh;
							mesh.name = tempExportObject.name;
							meshFilter.mesh = mesh;
						}
						break;
					}
				}


				model.exportPath = newPath;

				if (exportType == ExportType.FBX && prefabObj)
				{
					foreach (var meshRenderer in prefabObj.GetComponentsInChildren<MeshRenderer>())
					{
						if (meshRenderer.sharedMaterials.Length != 1)
							continue;

						var gameObject	= meshRenderer.gameObject;
						var nameSplit	= gameObject.name.Split('|');
						if (nameSplit.Length == 1)
							continue;

						int instanceId;
						if (!int.TryParse(nameSplit[1], out instanceId))
							continue;

						var realMaterial = EditorUtility.InstanceIDToObject(instanceId) as Material;
						if (!realMaterial)
							continue;
						
						meshRenderer.sharedMaterial = realMaterial;
						gameObject.name = nameSplit[0];
					}
				}


				var staticFlags = GameObjectUtility.GetStaticEditorFlags(model.gameObject);
				var modelLayer = model.gameObject.layer;
				foreach (var transform in modelGameObject.GetComponentsInChildren<Transform>())
				{
					var gameObject = transform.gameObject;
					GameObjectUtility.SetStaticEditorFlags(gameObject, staticFlags);
					gameObject.layer = modelLayer;
				}

				modelGameObject.transform.SetParent(model.transform, true);
				modelGameObject.transform.SetSiblingIndex(0);
				modelGameObject.tag						= model.gameObject.tag;
				
				modelGameObject.transform.localPosition = center - position; 

				
				Undo.RegisterCreatedObjectUndo(modelGameObject, "Instantiated model");


				var exported = model.gameObject.AddComponent<CSGModelExported>();
				exported.containedModel			= null;
				exported.containedExportedModel = modelGameObject;
				exported.disarm					= true;
				Undo.RegisterCreatedObjectUndo (exported, "Created CSGModelExported");
				Undo.RegisterCompleteObjectUndo(exported, "Created CSGModelExported");


				var foundBrushes	= model.GetComponentsInChildren<CSGBrush>(true);
				var foundOperations = model.GetComponentsInChildren<CSGOperation>(true);
				var foundContainers = model.GetComponentsInChildren<GeneratedMeshes>(true);

				var foundBehaviours = new HashSet<MonoBehaviour>();
					
				foreach (var foundBrush		in foundBrushes	  ) foundBehaviours.Add(foundBrush);
				foreach (var foundOperation in foundOperations) foundBehaviours.Add(foundOperation);
				foreach (var foundModel		in foundModels	  ) foundBehaviours.Add(foundModel);
				foreach (var foundContainer in foundContainers) foundBehaviours.Add(foundContainer);


				exported.hiddenComponents = new HiddenComponentData[foundBehaviours.Count];
				var index = 0;
				foreach (var foundBehaviour in foundBehaviours)
				{
					Undo.RegisterCompleteObjectUndo(foundBehaviour, "Hide component");
					exported.hiddenComponents[index] = new HiddenComponentData {behaviour = foundBehaviour};
					index++;
				}

				for (var i = 0; i < exported.hiddenComponents.Length; i++)
				{
					exported.hiddenComponents[i].hideFlags	= exported.hiddenComponents[i].behaviour.hideFlags;
					exported.hiddenComponents[i].enabled	= exported.hiddenComponents[i].behaviour.enabled;
				}
				
				for (var i = 0; i < exported.hiddenComponents.Length; i++)
				{
					exported.hiddenComponents[i].behaviour.hideFlags	= exported.hiddenComponents[i].behaviour.hideFlags | ComponentHideFlags;
					exported.hiddenComponents[i].behaviour.enabled		= false;
				}

				EditorSceneManager.MarkSceneDirty(currentScene);
				Undo.CollapseUndoOperations(groupIndex);
				groupIndex = 0;
				exported.disarm = false;
			}
			finally
			{
				switch (exportType)
				{
					case ExportType.FBX:
					{
						UnityEngine.Object.DestroyImmediate(tempExportObject);
						break;
					}
				}
				if (groupIndex != 0)
					Undo.CollapseUndoOperations(groupIndex);
			}
		}

		public static void ReverseExport(CSGModelExported exported)
		{
			if (exported.hiddenComponents != null)
			{
				for (var i = exported.hiddenComponents.Length - 1; i >= 0; i--)
				{
					if (!exported.hiddenComponents[i].behaviour)
						continue;
					
					Undo.RegisterCompleteObjectUndo(exported.hiddenComponents[i].behaviour, "Show hidden component");
					exported.hiddenComponents[i].behaviour.enabled = exported.hiddenComponents[i].enabled;
					exported.hiddenComponents[i].behaviour.hideFlags = exported.hiddenComponents[i].hideFlags;
				}
			}
			if (exported.containedExportedModel)
			{
				UnityEditor.Undo.DestroyObjectImmediate(exported.containedExportedModel);
			}
		}
	}
}
