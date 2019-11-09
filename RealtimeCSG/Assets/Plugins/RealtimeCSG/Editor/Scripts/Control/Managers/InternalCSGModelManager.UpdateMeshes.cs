using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using InternalRealtimeCSG;
using RealtimeCSG.Foundation;
using RealtimeCSG.Components;

namespace RealtimeCSG
{
	internal partial class InternalCSGModelManager
	{
		#region ClearMeshInstances
		public static void ClearMeshInstances()
		{
			MeshInstanceManager.Reset();

			for (var i = 0; i < Models.Length; i++)
			{
				var model = Models[i];
				model.generatedMeshes = null;
			}

			MeshInstanceManager.DestroyAllMeshInstances();
		}
		#endregion
		

		#region GetMeshTypesForModel
		static MeshQuery[] renderAndColliderMeshTypes = new MeshQuery[]
			{
				// MeshRenderers
				// Note: shadow-only surface could have masks, potentially could have shader that generates them based on existing vertex information
				new MeshQuery(LayerUsageFlags.CastShadows,                LayerUsageFlags.RenderCastShadows,		LayerParameterIndex.RenderMaterial,	VertexChannelFlags.All),
				new MeshQuery(LayerUsageFlags.Renderable,				  LayerUsageFlags.RenderReceiveCastShadows, LayerParameterIndex.RenderMaterial,	VertexChannelFlags.All),
				new MeshQuery(LayerUsageFlags.RenderCastShadows,          LayerUsageFlags.RenderReceiveCastShadows, LayerParameterIndex.RenderMaterial,	VertexChannelFlags.All),
				new MeshQuery(LayerUsageFlags.RenderReceiveShadows,       LayerUsageFlags.RenderReceiveCastShadows, LayerParameterIndex.RenderMaterial,	VertexChannelFlags.All),
				new MeshQuery(LayerUsageFlags.RenderReceiveCastShadows,   LayerUsageFlags.RenderReceiveCastShadows, LayerParameterIndex.RenderMaterial,	VertexChannelFlags.All),

				// MeshColliders
				new MeshQuery(LayerUsageFlags.Collidable,       parameterIndex: LayerParameterIndex.PhysicsMaterial),

#if UNITY_EDITOR
				// Helper surfaces (editor only)
				new MeshQuery(LayerUsageFlags.None,           mask: LayerUsageFlags.Renderable),	// hidden surfaces
				new MeshQuery(LayerUsageFlags.CastShadows		),
				new MeshQuery(LayerUsageFlags.ReceiveShadows	),
				new MeshQuery(LayerUsageFlags.Culled			)
#endif
			};

		static MeshQuery[] renderOnlyTypes = new MeshQuery[]
			{
				// MeshRenderers
				// Note: shadow-only surface could have masks, potentially could have shader that generates them based on existing vertex information
				new MeshQuery(LayerUsageFlags.CastShadows,                 LayerUsageFlags.RenderCastShadows,		 LayerParameterIndex.RenderMaterial,	VertexChannelFlags.All), 
				new MeshQuery(LayerUsageFlags.Renderable,                  LayerUsageFlags.RenderReceiveCastShadows, LayerParameterIndex.RenderMaterial,	VertexChannelFlags.All),
				new MeshQuery(LayerUsageFlags.RenderCastShadows,           LayerUsageFlags.RenderReceiveCastShadows, LayerParameterIndex.RenderMaterial,	VertexChannelFlags.All),
				new MeshQuery(LayerUsageFlags.RenderReceiveShadows,        LayerUsageFlags.RenderReceiveCastShadows, LayerParameterIndex.RenderMaterial,	VertexChannelFlags.All),
				new MeshQuery(LayerUsageFlags.RenderReceiveCastShadows,    LayerUsageFlags.RenderReceiveCastShadows, LayerParameterIndex.RenderMaterial,	VertexChannelFlags.All),

#if UNITY_EDITOR
				// Helper surfaces (editor only)
				new MeshQuery(LayerUsageFlags.None,           mask: LayerUsageFlags.Renderable),	// hidden surfaces
				new MeshQuery(LayerUsageFlags.CastShadows		),
				new MeshQuery(LayerUsageFlags.ReceiveShadows	),
				new MeshQuery(LayerUsageFlags.Culled			)
#endif
			};

		readonly static MeshQuery[] colliderMeshTypes = new MeshQuery[]
			{
				// MeshColliders
				new MeshQuery(LayerUsageFlags.Collidable,    parameterIndex: LayerParameterIndex.PhysicsMaterial),

#if UNITY_EDITOR
				// Helper surfaces (editor only)
				new MeshQuery(LayerUsageFlags.None,        mask: LayerUsageFlags.Renderable),	// hidden surfaces
				new MeshQuery(LayerUsageFlags.Culled)
#endif
			};

		readonly static MeshQuery[] triggerMeshTypes = new MeshQuery[]
			{
				// MeshColliders
				new MeshQuery(LayerUsageFlags.Collidable,   parameterIndex: LayerParameterIndex.PhysicsMaterial),

#if UNITY_EDITOR
				// Helper surfaces (editor only)
				new MeshQuery(LayerUsageFlags.None,		mask: LayerUsageFlags.Renderable),	// hidden surfaces
				new MeshQuery(LayerUsageFlags.Culled)
#endif
			};

		readonly static MeshQuery[] emptyMeshTypes = new MeshQuery[]
			{
#if UNITY_EDITOR
				// Helper surfaces (editor only)
				new MeshQuery(LayerUsageFlags.None,		mask: LayerUsageFlags.Renderable),	// hidden surfaces
				new MeshQuery(LayerUsageFlags.Culled)
#endif
			};

		public static MeshQuery[] GetMeshTypesForModel(CSGModel model)
		{
			MeshQuery[] query;
			if (!model.HaveCollider)
			{ 
				if (!model.IsRenderable) query = emptyMeshTypes;
				else query = renderOnlyTypes;
			} else
			{
				if      ( model.IsTrigger	) query = triggerMeshTypes;
				else if (!model.IsRenderable) query = colliderMeshTypes;
				else                          query = renderAndColliderMeshTypes;
			}
			return query;
		}
		#endregion

		#region UpdateModelSettings
		public static bool UpdateModelSettings()
		{
			var forceHierarchyUpdate = false;
			for (var i = 0; i < Models.Length; i++)
			{
				var model = Models[i];
				if (!model || !model.isActiveAndEnabled)
					continue;
				var modelModified = false;
				var invertedWorld = model.InvertedWorld;
				if (invertedWorld)
				{
					if (model.infiniteBrush == null)
					{
                        CSGBrush infiniteBrush = null;
                        var childNodes = model.GetComponentsInChildren<CSGBrush>();
                        for (int c = 0; c < childNodes.Length; c++)
                        {
                            if (childNodes[c].Flags == BrushFlags.InfiniteBrush)
                            {
                                if (infiniteBrush != null)
                                    UnityEngine.Object.DestroyImmediate(childNodes[c]);
                                else
                                    infiniteBrush = childNodes[c];
                            }
                        }

                        if (infiniteBrush == null)
                        { 
                            var gameObject = new GameObject("*hidden infinite brush*");
                            gameObject.hideFlags = MeshInstanceManager.ComponentHideFlags;
                            gameObject.transform.SetParent(model.transform, false);
                            infiniteBrush = gameObject.AddComponent<CSGBrush>();
                        }

                        model.infiniteBrush = infiniteBrush;
						model.infiniteBrush.Flags = BrushFlags.InfiniteBrush;

						modelModified = true;
					}
					if (model.infiniteBrush.transform.GetSiblingIndex() != 0)
					{
						model.infiniteBrush.transform.SetSiblingIndex(0);
						modelModified = true;
					}
				} else
				{
					if (model.infiniteBrush)
					{
						if (model.infiniteBrush.gameObject)
							UnityEngine.Object.DestroyImmediate(model.infiniteBrush.gameObject);
						model.infiniteBrush = null;
						modelModified = true;
					}
				}
				if (modelModified)
				{
					var childBrushes = model.GetComponentsInChildren<CSGBrush>();
					for (int j = 0; j < childBrushes.Length; j++)
					{
						if (!childBrushes[j] ||
							childBrushes[j].ControlMesh == null)
							continue;
						childBrushes[j].ControlMesh.Generation++;
					}
					forceHierarchyUpdate = true;
				}
			}
			return forceHierarchyUpdate;
		}
		#endregion


		#region RefreshMeshes
		public static void RefreshMeshes()
		{
			if (EditorApplication.isPlayingOrWillChangePlaymode)
				return;

			if (skipCheckForChanges)
				return;

			lock (_lockObj)
			{
				InternalCSGModelManager.UpdateModelSettings();
				InternalCSGModelManager.UpdateMeshes();
				MeshInstanceManager.UpdateHelperSurfaceVisibility();
			}
		}
		#endregion

		#region UpdateRemoteMeshes
		public static void UpdateRemoteMeshes()
		{
			if (External != null &&
				External.UpdateAllModelMeshes != null)
				External.UpdateAllModelMeshes();
		}
		#endregion

		#region UpdateMeshes


		#region DoForcedMeshUpdate
		static bool forcedUpdateRequired = false;

		public static void DoForcedMeshUpdate()
		{
			forcedUpdateRequired = true;
		}
		#endregion

		internal static int MeshGeneration = 0;

		#region GetModelMesh
		internal static double getMeshInstanceTime	= 0.0;
		internal static double getModelMeshesTime	= 0.0;
		internal static double updateMeshTime		= 0.0;
		
		const int MaxVertexCount = 65000;
		private static bool UpdateMesh(CSGModel						model, 
									   GeneratedMeshDescription		meshDescription, 
									   RenderSurfaceType			renderSurfaceType,
									   ref GeneratedMeshContents	outputGeneratedMeshContents,
									   ref bool						outputHasGeneratedNormals,
									   ref Mesh						sharedMesh)
		{
			// create our arrays on the C# side with the correct size
			GeneratedMeshContents generatedMesh;
			var startGetModelMeshesTime = EditorApplication.timeSinceStartup;
			{
				generatedMesh = External.GetModelMesh(model.modelNodeID, meshDescription);
				if (generatedMesh == null)
					return false;
			}
			getModelMeshesTime += EditorApplication.timeSinceStartup - startGetModelMeshesTime;

			UpdateMesh(generatedMesh,
					   meshDescription,
					   renderSurfaceType,
					   ref outputHasGeneratedNormals,
					   ref sharedMesh);
			/*
#if UNITY_2018_3_OR_NEWER
			var modelGameObject = model.gameObject;
			var prefabStage = UnityEditor.Experimental.SceneManagement.PrefabStageUtility.GetPrefabStage(modelGameObject);
			if (prefabStage != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(sharedMesh)))
			{
				var prefabAssetPath = prefabStage.prefabAssetPath;
				if (!string.IsNullOrEmpty(prefabAssetPath))
					AssetDatabase.AddObjectToAsset(sharedMesh, prefabAssetPath);
				UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(modelGameObject.scene);
			}
#endif
			*/
			outputGeneratedMeshContents = generatedMesh;
			return true;
		}

		public static bool UpdateMesh(GeneratedMeshContents		generatedMesh, 
									  GeneratedMeshDescription	inputMeshDescription,
									  RenderSurfaceType			renderSurfaceType,
									  ref bool					outputHasGeneratedNormals,
									  ref Mesh					sharedMesh)
		{
			var startUpdateMeshTime = EditorApplication.timeSinceStartup;
			{
				MeshInstanceManager.ClearMesh(ref outputHasGeneratedNormals, ref sharedMesh);

				// finally, we start filling our (sub)meshes using the C# arrays
				sharedMesh.vertices = generatedMesh.positions;
				if (generatedMesh.normals  != null) sharedMesh.normals	= generatedMesh.normals;
				if (generatedMesh.tangents != null) sharedMesh.tangents	= generatedMesh.tangents;
//				if (generatedMesh.colors   != null) sharedMesh.colors	= generatedMesh.colors;
				if (generatedMesh.uv0      != null) sharedMesh.uv		= generatedMesh.uv0;
			
				// fill the mesh with the given indices
				sharedMesh.SetTriangles(generatedMesh.indices, 0, calculateBounds: true);
				//sharedMesh.RecalculateBounds();
				//sharedMesh.bounds = generatedMesh.bounds; // why doesn't this work?
			}
			updateMeshTime += EditorApplication.timeSinceStartup - startUpdateMeshTime;
			
			if (renderSurfaceType != RenderSurfaceType.Normal)
				outputHasGeneratedNormals = ((inputMeshDescription.meshQuery.UsedVertexChannels & VertexChannelFlags.Normal) != 0);
			return true;
		}

		private static bool ValidateMesh(GeneratedMeshDescription meshDescription)
		{
			var indexCount	= meshDescription.indexCount;
			var vertexCount = meshDescription.vertexCount;
			if ((vertexCount <= 0 || vertexCount > MaxVertexCount) || (indexCount <= 0))
			{
				if (vertexCount > 0 && indexCount > 0)
				{
					Debug.LogError("Mesh has too many vertices (vertexCount > " + MaxVertexCount + ")");
				}
				return false;
			}
			return true;
		}

		private static GeneratedMeshInstance GenerateMeshInstance(GeneratedMeshes meshContainer, CSGModel model, ModelSettingsFlags modelSettings, GeneratedMeshDescription meshDescription, RenderSurfaceType renderSurfaceType)
		{
			GeneratedMeshInstance meshInstance;

			var startGetMeshInstanceTime = EditorApplication.timeSinceStartup;
			{
				meshInstance = MeshInstanceManager.GetMeshInstance(meshContainer, meshDescription, modelSettings, renderSurfaceType);
				if (!meshInstance)
					return null;
			}
			getMeshInstanceTime += EditorApplication.timeSinceStartup - startGetMeshInstanceTime;
			if (meshDescription == meshInstance.MeshDescription && 
				meshInstance.IsValid())
				return meshInstance;

			meshInstance.MeshDescription = meshDescription;
			if (!UpdateMesh(model, 
						    meshInstance.MeshDescription,
							meshInstance.RenderSurfaceType,
							ref meshInstance.GeneratedMeshContents,
							ref meshInstance.HasGeneratedNormals,
							ref meshInstance.SharedMesh))
				return null;
			
			return meshInstance;
		}

		private static HelperSurfaceDescription GenerateHelperSurfaceDescription(GeneratedMeshes meshContainer, CSGModel model, ModelSettingsFlags modelSettings, GeneratedMeshDescription meshDescription, RenderSurfaceType renderSurfaceType)
		{
			HelperSurfaceDescription helperSurfaceDescription;

			var startGetMeshInstanceTime = EditorApplication.timeSinceStartup;
			{
				helperSurfaceDescription = MeshInstanceManager.GetHelperSurfaceDescription(meshContainer, modelSettings, meshDescription, renderSurfaceType);
			}
			getMeshInstanceTime += EditorApplication.timeSinceStartup - startGetMeshInstanceTime;
			if (meshDescription == helperSurfaceDescription.MeshDescription && 
				helperSurfaceDescription.IsValid())
				return helperSurfaceDescription;

			helperSurfaceDescription.MeshDescription = meshDescription;
			if (!UpdateMesh(model, 
							helperSurfaceDescription.MeshDescription,
							helperSurfaceDescription.RenderSurfaceType,
							ref helperSurfaceDescription.GeneratedMeshContents,
							ref helperSurfaceDescription.HasGeneratedNormals,
							ref helperSurfaceDescription.SharedMesh))
				return null;

			return helperSurfaceDescription;
		}
		#endregion

		#region RemoveForcedUpdates
		public static void RemoveForcedUpdates()
		{
			var modelCount = Models.Length;
			if (modelCount == 0)
				return;

			for (var i = 0; i < modelCount; i++)
			{
				var model = Models[i];
				model.forceUpdate  = false;
			}
		}
		#endregion

		internal static uint		__prevSubMeshCount		= 0;
		internal static UInt64[]	__vertexHashValues		= null;
		internal static UInt64[]	__triangleHashValues	= null;
		internal static UInt64[]	__surfaceHashValues		= null;
		internal static Int32[]		__vertexCounts			= null;
		internal static Int32[]		__indexCounts			= null;

		static readonly HashSet<HelperSurfaceDescription>	__foundHelperSurfaces		 = new HashSet<HelperSurfaceDescription>();
		static readonly HashSet<GeneratedMeshInstance>		__foundGeneratedMeshInstance = new HashSet<GeneratedMeshInstance>();
		static GeneratedMeshDescription[]					__meshDescriptions			 = new GeneratedMeshDescription[0];


		static bool inUpdateMeshes = false;
		public static bool UpdateMeshes(System.Text.StringBuilder text = null, bool forceUpdate = false)
		{
			if (EditorApplication.isPlaying
				|| EditorApplication.isPlayingOrWillChangePlaymode)
				return false;
			
			if (inUpdateMeshes)
				return false;

			MeshInstanceManager.Update();
			
			var unityMeshUpdates		= 0.0;
			var getMeshDescriptionTime	= 0.0;
			
			getMeshInstanceTime		= 0.0;
			getModelMeshesTime		= 0.0;
			updateMeshTime			= 0.0;

			inUpdateMeshes = true;
			try
			{
				if (External == null)
					return false;

				if (forcedUpdateRequired)
				{
					forceUpdate = true;
					forcedUpdateRequired = false;
				}

				var modelCount = Models.Length;
				if (modelCount == 0)
					return false;

				for (var i = 0; i < modelCount; i++)
				{
					var model = Models[i];
					if (!model.isActive)
						continue;
					
					if (!forceUpdate &&
						!model.forceUpdate)
						continue;
					
					External.SetDirty(model.modelNodeID);
					model.forceUpdate = false;
				}

				// update the model meshes
				if (!External.UpdateAllModelMeshes())
					return false; // nothing to do

				MeshGeneration++;
				bool haveUpdates = forceUpdate;
				for (var i = 0; i < modelCount; i++)
				{
					var model = Models[i];
					
					if (!(new CSGTreeNode { nodeID = model.modelNodeID }.Dirty))
						continue;
					
					if (!model.isActive)
						continue;
					
					var meshContainer = model.generatedMeshes;
					if (!meshContainer)
						return false;
					
					EnsureInitialized(model);
					
					bool needToUpdateMeshes;
					var startGetMeshDescriptionTime = EditorApplication.timeSinceStartup;
					{
						needToUpdateMeshes = External.GetMeshDescriptions(model, ref __meshDescriptions);
					}
					getMeshDescriptionTime += EditorApplication.timeSinceStartup - startGetMeshDescriptionTime;

					if (!needToUpdateMeshes)
						continue;
					__foundHelperSurfaces.Clear();
					__foundGeneratedMeshInstance.Clear();
					var startUnityMeshUpdates = EditorApplication.timeSinceStartup;
					{
						for (int meshIndex = 0; meshIndex < __meshDescriptions.Length; meshIndex++)
						{
							if (!ValidateMesh(__meshDescriptions[meshIndex]))
								continue;

							haveUpdates = true;
							var renderSurfaceType = MeshInstanceManager.GetSurfaceType(__meshDescriptions[meshIndex], model.Settings);
							if (renderSurfaceType == RenderSurfaceType.Normal ||
								renderSurfaceType == RenderSurfaceType.ShadowOnly ||
								renderSurfaceType == RenderSurfaceType.Collider ||
								renderSurfaceType == RenderSurfaceType.Trigger)
							{ 
								var meshInstance	= GenerateMeshInstance(meshContainer, model, model.Settings, __meshDescriptions[meshIndex], renderSurfaceType);
								if (meshInstance != null) __foundGeneratedMeshInstance.Add(meshInstance);
							} else
							{
								var helperSurface	= GenerateHelperSurfaceDescription(meshContainer, model, model.Settings, __meshDescriptions[meshIndex], renderSurfaceType);
								__foundHelperSurfaces.Add(helperSurface);
							}
						}
					}
					unityMeshUpdates += (EditorApplication.timeSinceStartup - startUnityMeshUpdates);

					MeshInstanceManager.UpdateContainerComponents(meshContainer, __foundGeneratedMeshInstance, __foundHelperSurfaces);
				}
				if (haveUpdates)
					MeshInstanceManager.UpdateHelperSurfaceVisibility(force: true);

				
				if (text != null)
				{
					text.AppendFormat(System.Globalization.CultureInfo.InvariantCulture,
										"All mesh generation {0:F} ms " +
										"+ retrieving {1:F} ms " +
										"+ Unity mesh updates {2:F} ms " +
										"+ overhead {3:F} ms. ",
										getMeshDescriptionTime * 1000, 
										getModelMeshesTime * 1000, 
										updateMeshTime * 1000,
										(unityMeshUpdates - (getModelMeshesTime + updateMeshTime)) * 1000);
				}

				return true;
			}
			finally
			{
				inUpdateMeshes = false;
			}
		}
		#endregion
	}
}