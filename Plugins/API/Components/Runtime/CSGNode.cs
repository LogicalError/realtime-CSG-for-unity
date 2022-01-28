using System;
using UnityEngine;
using InternalRealtimeCSG;
using UnityEngine.Serialization;

namespace RealtimeCSG.Components
{
#if UNITY_EDITOR
	[Serializable]
	public enum PrefabSourceAlignment : byte
	{
		AlignedFront,
		AlignedBack,
		AlignedLeft,
		AlignedRight,	
		AlignedTop,
		AlignedBottom
	}

	public enum PrefabDestinationAlignment : byte
	{
		AlignToSurface,
		AlignSurfaceUp,
		Default
	}
#endif

	/// <summary>Parent class of <see cref="CSGBrush"/>/<see cref="CSGOperation"/>/<see cref="CSGModel"/></summary>
#if UNITY_EDITOR
	[DisallowMultipleComponent]
#endif
	public abstract class CSGNode : MonoBehaviour
	{
#if UNITY_EDITOR
		[SerializeField] public PrefabInstantiateBehaviour	PrefabBehaviour				= PrefabInstantiateBehaviour.Reference;
		[SerializeField] public PrefabSourceAlignment		PrefabSourceAlignment		= PrefabSourceAlignment.AlignedTop;
		[SerializeField] public PrefabDestinationAlignment	PrefabDestinationAlignment	= PrefabDestinationAlignment.AlignToSurface;
		public const Int32 InvalidNodeID = 0;
#endif
	}

}
