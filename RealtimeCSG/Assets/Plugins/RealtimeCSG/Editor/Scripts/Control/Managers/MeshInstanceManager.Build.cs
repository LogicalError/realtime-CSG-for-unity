using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEditor;
using RealtimeCSG;
using RealtimeCSG.Components;

namespace InternalRealtimeCSG
{
    internal sealed partial class MeshInstanceManager
    {
        [UnityEditor.Callbacks.PostProcessScene(0)]
        internal static void OnBuild()
        {
            // apparently only way to determine current scene while post processing a scene
            var randomObject = UnityEngine.Object.FindObjectOfType<Transform>();
            if (!randomObject)
                return;

            var currentScene = randomObject.gameObject.scene;
            
            var foundMeshContainers = SceneQueryUtility.GetAllComponentsInScene<GeneratedMeshes>(currentScene);
            foreach (var meshContainer in foundMeshContainers)
            {
                var model = meshContainer.owner;
                if (!model)
                {
                    UnityEngine.Object.DestroyImmediate(meshContainer.gameObject);
                    continue;
                }
                
                if (model.NeedAutoUpdateRigidBody)
                    AutoUpdateRigidBody(meshContainer);

                model.gameObject.hideFlags = HideFlags.None;
                meshContainer.transform.hideFlags = HideFlags.None;
                meshContainer.gameObject.hideFlags = HideFlags.None;
                meshContainer.hideFlags = HideFlags.None;

                var instances = meshContainer.GetComponentsInChildren<GeneratedMeshInstance>(true);
                foreach (var instance in instances)
                {
                    if (!instance)
                        continue;

                    instance.gameObject.hideFlags = HideFlags.None;// HideFlags.NotEditable;
                    instance.gameObject.SetActive(true);

                    //Refresh(instance, model, postProcessScene: true);
#if SHOW_GENERATED_MESHES
                    UpdateName(instance);
#endif

                    // TODO: make sure meshes are no longer marked as dynamic!

                    if (!HasRuntimeMesh(instance))
                    {
                        UnityEngine.Object.DestroyImmediate(instance.gameObject);
                        continue;
                    }

                    var surfaceType = GetSurfaceType(instance.MeshDescription, model.Settings);
                    if (surfaceType == RenderSurfaceType.ShadowOnly)
                    {
                        var meshRenderer = instance.gameObject.GetComponent<MeshRenderer>();
                        if (meshRenderer)
                        {
                            meshRenderer.sharedMaterial = MaterialUtility.DefaultMaterial;
                            meshRenderer.enabled = true;
                        }
                        RemoveIfEmpty(instance.gameObject);
                        continue;
                    }
                    if (surfaceType == RenderSurfaceType.Normal)
                    {
                        var meshRenderer = instance.gameObject.GetComponent<MeshRenderer>();
                        if (meshRenderer)
                        { 
                            meshRenderer.enabled = true;
                        }
                    }

                    if (surfaceType == RenderSurfaceType.Collider ||
                        surfaceType == RenderSurfaceType.Trigger)
                    {
                        var meshRenderer = instance.gameObject.GetComponent<MeshRenderer>();
                        if (meshRenderer)
                            UnityEngine.Object.DestroyImmediate(meshRenderer);

                        var meshFilter = instance.gameObject.GetComponent<MeshFilter>();
                        if (meshFilter)
                            UnityEngine.Object.DestroyImmediate(meshFilter);

                        if (surfaceType == RenderSurfaceType.Trigger)
                        {
                            var oldMeshCollider = instance.gameObject.GetComponent<MeshCollider>();
                            if (oldMeshCollider)
                            {
                                var newMeshCollider = model.gameObject.AddComponent<MeshCollider>();
                                EditorUtility.CopySerialized(oldMeshCollider, newMeshCollider);
                                UnityEngine.Object.DestroyImmediate(oldMeshCollider);
                            }
                        }
                        RemoveIfEmpty(instance.gameObject);
                        continue;
                    }
                    RemoveIfEmpty(instance.gameObject);
                }

                if (!meshContainer)
                    continue;

                var children = meshContainer.GetComponentsInChildren<GeneratedMeshInstance>();
                foreach (var child in children)
                {
                    child.hideFlags = HideFlags.None;
                    child.gameObject.hideFlags = HideFlags.None;
                    child.transform.hideFlags = HideFlags.None;
                    if (child.SharedMesh && !UsesLightmapUVs(model))
                        MeshUtility.Optimize(child.SharedMesh);
                }

                UnityEngine.Object.DestroyImmediate(meshContainer);
            }


            var meshInstances = SceneQueryUtility.GetAllComponentsInScene<GeneratedMeshInstance>(currentScene);
            foreach (var meshInstance in meshInstances)
            {
                if (meshInstance)
                {
                    UnityEngine.Object.DestroyImmediate(meshInstance);
                }
            }

            var csgnodes				= new HashSet<CSGNode>(SceneQueryUtility.GetAllComponentsInScene<CSGNode>(currentScene));
            var removableGameObjects	= new List<GameObject>();
            foreach (var csgnode in csgnodes)
            {
                if (!csgnode)
                    continue;

                var gameObject = csgnode.gameObject;
                var model = csgnode as CSGModel;

                if (
                        (model && model.name == InternalCSGModelManager.DefaultModelName && 
                            (model.transform.childCount == 0 ||
                                (model.transform.childCount == 1 &&
                                model.transform.GetChild(0).name == MeshContainerName &&
                                model.transform.GetChild(0).childCount == 0)
                            )
                        )
                    )
                {
                    UnityEngine.Object.DestroyImmediate(gameObject);
                    continue;
                } else
                if (model)
                {
                    gameObject.tag = "Untagged";
                    AssignLayerToChildren(gameObject);
                } else
                if (gameObject.CompareTag("Untagged"))
                    removableGameObjects.Add(gameObject);
                if (csgnode)
                    UnityEngine.Object.DestroyImmediate(csgnode);
            }

            var removableTransforms = new HashSet<Transform>();
            for (int i = 0; i < removableGameObjects.Count; i++)
            {
                var gameObject = removableGameObjects[i];
                var transform = gameObject.transform;
                if (removableTransforms.Contains(transform))
                    continue;
                RemoveWithChildrenIfPossible(transform, removableTransforms);
            }
        }

        static bool RemoveWithChildrenIfPossible(Transform transform, HashSet<Transform> removableTransforms)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                var childTransform = transform.GetChild(i);
                //if (!childTransform.CompareTag(CSGContstants.kRemoveNodeTag))
                //    return false;
                if (childTransform.GetComponent<CSGNode>() == null)
                {
                    transform.gameObject.tag = "Untagged";
                    return false;
                }
                if (!RemoveWithChildrenIfPossible(childTransform, removableTransforms))
                {
                    transform.gameObject.tag = "Untagged";
                    return false;
                }
            }
            UnityEngine.Object.DestroyImmediate(transform.gameObject);
            return true;
        }

        internal static void DestroyAllMeshInstances()
        {
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            for (var sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
            {
                var scene = SceneManager.GetSceneAt(sceneIndex);
                if (!scene.isLoaded)
                    continue;
                var sceneModels = SceneQueryUtility.GetAllComponentsInScene<CSGModel>(scene);
                for (int i = 0; i < sceneModels.Count; i++)
                {
                    var selfTransform = sceneModels[i].transform;
                    var transforms = selfTransform.GetComponentsInChildren<Transform>();
                    foreach (var generateMeshesTransform in transforms)
                    {
                        if (!generateMeshesTransform || generateMeshesTransform.parent != selfTransform)
                            continue;

                        if (generateMeshesTransform.name != MeshInstanceManager.MeshContainerName)
                            continue;

                        var childTransforms = generateMeshesTransform;
                        foreach (Transform childTransform in childTransforms)
                        {
                            GameObjectExtensions.Destroy(childTransform.gameObject);
                        }
                    }
                }
            }
        }
    }
}
