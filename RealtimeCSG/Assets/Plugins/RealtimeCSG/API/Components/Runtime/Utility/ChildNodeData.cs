using System;
using UnityEngine;
using InternalRealtimeCSG;
using UnityEngine.Rendering;

namespace RealtimeCSG
{
#if UNITY_EDITOR
	[Serializable]
    public sealed class ChildNodeData
    {
		// this allows us to help detect when the operation has been modified in the hierarchy
	    public RealtimeCSG.Components.CSGOperation		Parent			= null;
	    public RealtimeCSG.Components.CSGModel			Model			= null;
		public Transform		ModelTransform	= null;
		public ParentNodeData	OwnerParentData = null; // link to parents' parentData


		public int    ParentNodeID    { get { return (Parent != null) ? Parent.operationNodeID : RealtimeCSG.Components.CSGNode.InvalidNodeID; } }
		public int    ModelNodeID     { get { return (Model  != null) ? Model.modelNodeID      : RealtimeCSG.Components.CSGNode.InvalidNodeID; } }
		
		public void Reset()
		{
			Parent			= null;
			Model			= null;
			OwnerParentData = null;
			ModelTransform	= null;
		}
	}
#endif
}