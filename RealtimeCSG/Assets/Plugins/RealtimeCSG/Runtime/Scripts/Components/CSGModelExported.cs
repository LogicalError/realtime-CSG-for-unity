using System;
using UnityEngine;
using RealtimeCSG.Components;

namespace InternalRealtimeCSG
{
	[Serializable]
	public sealed class HiddenComponentData
	{
		public MonoBehaviour behaviour;
		public bool enabled;
		public HideFlags hideFlags;
	}

	[ExecuteInEditMode]
	[DisallowMultipleComponent]
	[SelectionBase]
	public sealed class CSGModelExported : MonoBehaviour
	{
		[HideInInspector] public float Version = 1.00f;
        [HideInInspector][SerializeField] public CSGModel containedModel;
        [HideInInspector][SerializeField] public GameObject containedExportedModel;
		[HideInInspector][SerializeField] public HiddenComponentData[] hiddenComponents;
		[HideInInspector][SerializeField] public bool disarm;
		
        void Awake()
		{
			// cannot change visibility since this might have an effect on exporter
			this.hideFlags |= HideFlags.DontSaveInBuild;
		}
		
#if UNITY_EDITOR
		public void DestroyModel(bool undoable = false)
		{
			if (CSGSceneManagerRedirector.Interface != null)
			{
				CSGSceneManagerRedirector.Interface.DestroyExportedModel(this, undoable);
			} else
			{ 
				if (hiddenComponents != null)
				{
					foreach (var hiddenComponent in hiddenComponents)
					{
						var behaviour = hiddenComponent.behaviour;
						if (!behaviour)
							continue;
						
						DestroyImmediate(behaviour);
					}
				}
				if (containedModel)
				{
					DestroyImmediate(containedModel);
				}
			}
			containedModel = null;
			hiddenComponents = null;
			disarm = true;
		}
#endif
	}
}
