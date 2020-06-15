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
				if (!model.IsRenderable) query = colliderMeshTypes;
				else query = renderAndColliderMeshTypes;
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
                if (!ModelTraits.IsModelEditable(model))
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
		private static bool UpdateMesh(CSGModel					model, 
									   string                   baseName,
                                       GeneratedMeshDescription meshDescription, 
									   RenderSurfaceType	    renderSurfaceType,
									   ref bool				    outputHasGeneratedNormals,
									   ref Mesh				    sharedMesh,
                                       bool                     editorOnly = false)
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

			UpdateMesh(baseName,
                       generatedMesh,
					   meshDescription,
					   renderSurfaceType,
					   ref outputHasGeneratedNormals,
					   ref sharedMesh,
                       editorOnly);
			return true;
		}

		public static bool UpdateMesh(string                    baseName,
                                      GeneratedMeshContents		generatedMesh, 
									  GeneratedMeshDescription	inputMeshDescription,
									  RenderSurfaceType			renderSurfaceType,
									  ref bool					outputHasGeneratedNormals,
									  ref Mesh					sharedMesh,
                                      bool                      editorOnly)
        {
            var startUpdateMeshTime = EditorApplication.timeSinceStartup;
			{
				MeshInstanceManager.ClearOrCreateMesh(baseName, editorOnly, ref outputHasGeneratedNormals, ref sharedMesh);

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

        private static bool TryGetMeshInstance(GeneratedMeshes meshContainer, CSGModel model, ModelSettingsFlags modelSettings, GeneratedMeshDescription meshDescription, RenderSurfaceType renderSurfaceType, out GeneratedMeshInstance meshInstance)
        {
            var startGetMeshInstanceTime = EditorApplication.timeSinceStartup;
            meshInstance = MeshInstanceManager.GetMeshInstance(meshContainer, meshDescription, modelSettings, renderSurfaceType);
            getMeshInstanceTime += EditorApplication.timeSinceStartup - startGetMeshInstanceTime;
            if (meshInstance &&
                meshDescription == meshInstance.MeshDescription &&
                meshInstance.SharedMesh &&
                meshInstance.IsValid())
                return true;

            meshInstance = null;
            return true;
        }

        private static GeneratedMeshInstance GenerateMeshInstance(GeneratedMeshes meshContainer, CSGModel model, ModelSettingsFlags modelSettings, GeneratedMeshDescription meshDescription, RenderSurfaceType renderSurfaceType, List<GameObject> unusedInstances)
        {
            GeneratedMeshInstance meshInstance = MeshInstanceManager.CreateMeshInstance(meshContainer, meshDescription, modelSettings, renderSurfaceType, unusedInstances);

            string baseName;
            switch(renderSurfaceType)
            {
                case RenderSurfaceType.Collider:    baseName = "Collider"; break;
                case RenderSurfaceType.Normal:      baseName = "Renderable"; break;
                default:                            baseName = "Unknown"; break;
            }

            meshInstance.MeshDescription = meshDescription;
			if (!UpdateMesh(model, baseName,
                            meshInstance.MeshDescription,
							meshInstance.RenderSurfaceType,
							ref meshInstance.HasGeneratedNormals,
							ref meshInstance.SharedMesh))
				return null;
			
			return meshInstance;
		}

        static string[] renderSurfaceMeshNames = new string[]
        {
            "Renderable helper surface",

            "Hidden helper surface",
            "Culled helper surface",
            "ShadowOnly helper surface",
            "Collider helper surface",
            "Trigger helper surface",

            "CastShadows helper surface",
            "ReceiveShadows helper surface"
        };

        private static bool TryGetHelperSurfaceDescription(GeneratedMeshes meshContainer, CSGModel model, ModelSettingsFlags modelSettings, GeneratedMeshDescription meshDescription, RenderSurfaceType renderSurfaceType, out HelperSurfaceDescription helperSurfaceDescription)
        {
            var startGetMeshInstanceTime = EditorApplication.timeSinceStartup;
            helperSurfaceDescription = MeshInstanceManager.GetHelperSurfaceDescription(meshContainer, modelSettings, meshDescription, renderSurfaceType);
            getMeshInstanceTime += EditorApplication.timeSinceStartup - startGetMeshInstanceTime;
			if (helperSurfaceDescription != null &&
				meshDescription == helperSurfaceDescription.MeshDescription &&
				helperSurfaceDescription.SharedMesh &&
				helperSurfaceDescription.IsValid())
			{
				return true;
			}
			helperSurfaceDescription = null;
            return true;
        }

        private static HelperSurfaceDescription GenerateHelperSurfaceDescription(GeneratedMeshes meshContainer, CSGModel model, ModelSettingsFlags modelSettings, GeneratedMeshDescription meshDescription, RenderSurfaceType renderSurfaceType)
        {
            HelperSurfaceDescription helperSurfaceDescription = MeshInstanceManager.CreateHelperSurfaceDescription(meshContainer, modelSettings, meshDescription, renderSurfaceType);

            helperSurfaceDescription.MeshDescription = meshDescription;
            if (!UpdateMesh(model, renderSurfaceMeshNames[(int)renderSurfaceType],
                            helperSurfaceDescription.MeshDescription,
							helperSurfaceDescription.RenderSurfaceType,
							ref helperSurfaceDescription.HasGeneratedNormals,
							ref helperSurfaceDescription.SharedMesh,
                            editorOnly: true))
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
		static readonly HashSet<int>		                __unfoundMeshInstances       = new HashSet<int>();
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
					
					if (!forceUpdate &&
						!model.forceUpdate)
						continue;

                    if (!ModelTraits.IsModelEditable(model))
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
                    {
                        continue;
                    }

                    var meshContainer = model.generatedMeshes;
                    if (!meshContainer)
                        continue;
					
					EnsureInitialized(model);
					
					bool needToUpdateMeshes;
					var startGetMeshDescriptionTime = EditorApplication.timeSinceStartup;
					{
						needToUpdateMeshes = External.GetMeshDescriptions(model, ref __meshDescriptions);
					}
					getMeshDescriptionTime += EditorApplication.timeSinceStartup - startGetMeshDescriptionTime;


                    if (!ModelTraits.IsModelEditable(model))
                        continue;

                    if (!needToUpdateMeshes)
						continue;

                    __foundHelperSurfaces.Clear();
					__foundGeneratedMeshInstance.Clear();
                    __unfoundMeshInstances.Clear();
                    {
                        var startUnityMeshUpdates = EditorApplication.timeSinceStartup;
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
                                GeneratedMeshInstance meshInstance;
                                if (TryGetMeshInstance(meshContainer, model, model.Settings, __meshDescriptions[meshIndex], renderSurfaceType, out meshInstance))
                                {
									if (meshInstance == null)
									{
										__unfoundMeshInstances.Add(meshIndex);
										continue;
									} else 
										__foundGeneratedMeshInstance.Add(meshInstance);
                                }
                            }
                            if (renderSurfaceType != RenderSurfaceType.Normal)
                            {
								HelperSurfaceDescription helperSurface;
								if (TryGetHelperSurfaceDescription(meshContainer, model, model.Settings, __meshDescriptions[meshIndex], renderSurfaceType, out helperSurface))
                                {
									if (helperSurface != null)
										__foundHelperSurfaces.Add(helperSurface);
									else
										__unfoundMeshInstances.Add(meshIndex);
                                }
                            }
                        }

						var unusedInstances = MeshInstanceManager.FindUnusedMeshInstances(meshContainer, __foundGeneratedMeshInstance);

                        foreach (int meshIndex in __unfoundMeshInstances)
                        {
                            haveUpdates = true;
                            var renderSurfaceType = MeshInstanceManager.GetSurfaceType(__meshDescriptions[meshIndex], model.Settings);
                            if (renderSurfaceType == RenderSurfaceType.Normal ||
                                renderSurfaceType == RenderSurfaceType.ShadowOnly ||
                                renderSurfaceType == RenderSurfaceType.Collider ||
                                renderSurfaceType == RenderSurfaceType.Trigger)
                            {
                                var meshInstance = GenerateMeshInstance(meshContainer, model, model.Settings, __meshDescriptions[meshIndex], renderSurfaceType, unusedInstances);
                                if (meshInstance != null) __foundGeneratedMeshInstance.Add(meshInstance);
                            }
                            if (renderSurfaceType != RenderSurfaceType.Normal)
							{
								var helperSurface = GenerateHelperSurfaceDescription(meshContainer, model, model.Settings, __meshDescriptions[meshIndex], renderSurfaceType);
                                __foundHelperSurfaces.Add(helperSurface);
                            }
                        }

						MeshInstanceManager.UpdateContainerComponents(meshContainer, __foundGeneratedMeshInstance, __foundHelperSurfaces);
                        unityMeshUpdates += (EditorApplication.timeSinceStartup - startUnityMeshUpdates);
                    }
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