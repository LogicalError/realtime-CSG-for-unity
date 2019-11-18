#if UNITY_EDITOR
using RealtimeCSG;
using RealtimeCSG.Components;

namespace InternalRealtimeCSG
{
	public interface CSGSceneManagerInterface
	{
		void OnCreated					(CSGBrush component);
		void OnEnabled					(CSGBrush component);
		void OnValidate					(CSGBrush component);
		void OnTransformParentChanged	(CSGBrush component);
		void OnDisabled					(CSGBrush component);
		void OnDestroyed				(CSGBrush component);
		void EnsureInitialized			(CSGBrush component);


		void OnCreated					(CSGOperation component);
		void OnEnabled					(CSGOperation component);
		void OnPassthroughChanged		(CSGOperation component);
		void OnValidate					(CSGOperation component);
		void OnTransformParentChanged	(CSGOperation component);
		void OnDisabled					(CSGOperation component);
		void OnDestroyed				(CSGOperation component);
		void EnsureInitialized			(CSGOperation component);
		

		void OnCreated					(CSGModel component);
		void OnEnabled					(CSGModel component);
		void OnValidate					(CSGModel component);
		void OnTransformChildrenChanged (CSGModel component);
		void OnUpdate					(CSGModel component);
		void OnDisabled					(CSGModel component);
		void OnDestroyed				(CSGModel component);
		void EnsureInitialized			(CSGModel component);
		
		void OnCreated					(GeneratedMeshes container);
		void OnDestroyed				(GeneratedMeshes container);
		
		void OnCreated					(GeneratedMeshInstance component);

		void DestroyExportedModel		(CSGModelExported exportedModel, bool undoable = false);
	}
}
#endif