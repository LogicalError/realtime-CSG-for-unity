using InternalRealtimeCSG;
using UnityEditor;
using RealtimeCSG.Components;

namespace RealtimeCSG
{
	internal static class ModelTraits
	{
		public static bool WillModelRender(CSGModel model)
		{
#if UNITY_2018_3_OR_NEWER
			var currentPrefabStage = UnityEditor.Experimental.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
			if (currentPrefabStage != null)
			{
				var prefabRoot = currentPrefabStage.prefabContentsRoot;
				if (prefabRoot.scene != model.gameObject.scene)
					return false;
			}
#endif
			// Is our model valid ...?
			if (!model || !model.isActiveAndEnabled)
				return false;

			// Does our model have a meshRenderer?
			if (model.IsRenderable)
			{
				// If so, is it shadow-only?
//				if (model.ShadowsOnly)
//				{
					// .. and do we need to show shadow-only surfaces?
//					return CSGSettings.ShowCastShadowsSurfaces;
//				}

				// Otherwise, it is always rendering (with the exception of manually hidden surfaces)
				return true;
			}

			// Is it a trigger and are we showing triggers?
			if (model.IsTrigger && CSGSettings.ShowTriggerSurfaces)
				return true;

			// Check if it's a collider and are we showing colliders?
			if (model.HaveCollider && CSGSettings.ShowColliderSurfaces)
				return true;

			// Otherwise see if we're showing surfaces culled by the CSG process ...
			return CSGSettings.ShowCulledSurfaces;
		}

		public static RenderSurfaceType GetModelSurfaceType(CSGModel model)
		{
			if (model.IsRenderable) // if it's renderable then it's already being rendered using a MeshRenderer
			{
				// except if it's a shadow only surface, it has a MeshRenderer, but is not usually visible ...
//				if (model.ShadowsOnly)
//					return RenderSurfaceType.ShadowOnly;

				return RenderSurfaceType.Normal;
			}

			if (model.IsTrigger)
				return RenderSurfaceType.Trigger;

			if (model.HaveCollider)
				return RenderSurfaceType.Collider;

			return RenderSurfaceType.Culled;
		}

		public static bool NeedsRigidBody(CSGModel model)
		{
			var collidable			= (model.Settings & ModelSettingsFlags.NoCollider) != ModelSettingsFlags.NoCollider;
			var isTrigger			= collidable && (model.Settings & ModelSettingsFlags.IsTrigger) == ModelSettingsFlags.IsTrigger;
			var ownerStaticFlags	= GameObjectUtility.GetStaticEditorFlags(model.gameObject);
			var batchingstatic		= (ownerStaticFlags & StaticEditorFlags.BatchingStatic) == StaticEditorFlags.BatchingStatic;

			return (batchingstatic || collidable) && !isTrigger;
		}

		public static bool NeedsStaticRigidBody(CSGModel model)
		{
			var ownerStaticFlags = GameObjectUtility.GetStaticEditorFlags(model.gameObject);
			var batchingstatic = (ownerStaticFlags & StaticEditorFlags.BatchingStatic) == StaticEditorFlags.BatchingStatic;
			return batchingstatic;
		}
	}
}
