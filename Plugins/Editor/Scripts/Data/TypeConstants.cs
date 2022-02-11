using System;
using UnityEngine;
using InternalRealtimeCSG;
using RealtimeCSG.Components;

namespace RealtimeCSG
{
	public static class TypeConstants
	{
		public static readonly Type CSGModelType				= typeof(CSGModel);
		public static readonly Type CSGOperationType			= typeof(CSGOperation);
		public static readonly Type CSGBrushType				= typeof(CSGBrush);
		public static readonly Type CSGNodeType					= typeof(CSGNode);

		public static readonly Type GeneratedMeshInstanceType   = typeof(GeneratedMeshInstance);

		public static readonly Type RigidbodyType				= typeof(Rigidbody);
		public static readonly Type MeshRendererType			= typeof(MeshRenderer);
		public static readonly Type MeshFilterType				= typeof(MeshFilter);
		public static readonly Type MeshColliderType			= typeof(MeshCollider);
	}
}
