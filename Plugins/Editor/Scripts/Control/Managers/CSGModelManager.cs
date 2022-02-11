using System.Collections.Generic;
using UnityEngine;
using InternalRealtimeCSG;
using RealtimeCSG.Components;

namespace RealtimeCSG
{
	public static class CSGModelManager
	{
		public static void ForceRebuild()
		{
#if UNITY_EDITOR
			if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
				return;
#endif

			InternalCSGModelManager.ForceRebuildAll();
			InternalCSGModelManager.OnHierarchyModified();
			InternalCSGModelManager.OnHierarchyModified();
			NativeMethodBindings.RebuildAll();
			InternalCSGModelManager.UpdateMeshes(forceUpdate: true);
		}

        public static void BuildLightmapUvs(bool force = false)
		{
			for (int m = 0; m < InternalCSGModelManager.Models.Length; m++)
			{
				if (!InternalCSGModelManager.Models[m])
					continue;

				if (!force &&!MeshInstanceManager.NeedToGenerateLightmapUVsForModel(InternalCSGModelManager.Models[m]))
					continue;

				MeshInstanceManager.GenerateLightmapUVsForModel(InternalCSGModelManager.Models[m]);
			}
		}

		public static void EnsureBuildFinished() { InternalCSGModelManager.CheckForChanges(true); }
		public static GameObject[] GetModelMeshes(CSGModel model)
		{
			var meshContainer = model.generatedMeshes;
			var meshInstances = MeshInstanceManager.GetAllModelMeshInstances(meshContainer);

			if (meshInstances == null)
				return new GameObject[0];

			var gameObjects = new List<GameObject>();
			for (var i = 0; i < meshInstances.Length; i++)
			{
				if (!meshInstances[i] ||
					meshInstances[i].RenderSurfaceType != RenderSurfaceType.Normal)
					continue;
				gameObjects.Add(meshInstances[i].gameObject);
			}

			return gameObjects.ToArray();
		}

		public static CSGModel[] GetAllModels()
		{
			return InternalCSGModelManager.Models;
		}
	}
}