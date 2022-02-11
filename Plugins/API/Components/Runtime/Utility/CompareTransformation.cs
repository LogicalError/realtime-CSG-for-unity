using System;
using UnityEngine;
using RealtimeCSG.Legacy;
using InternalRealtimeCSG;

namespace RealtimeCSG
{
#if UNITY_EDITOR
	// this allows us to determine if our brush has changed it's transformation
	[Serializable]
	public sealed class CompareTransformation
	{
		public Matrix4x4    localToWorldMatrix		 = MathConstants.identityMatrix;
		public Matrix4x4    brushToModelSpaceMatrix  = MathConstants.identityMatrix;

		public void EnsureInitialized(Transform transform, Transform modelTransform)
		{
			localToWorldMatrix = transform.localToWorldMatrix;//worldToLocalMatrix;
			brushToModelSpaceMatrix = (modelTransform == null) ? localToWorldMatrix : modelTransform.worldToLocalMatrix * 
				localToWorldMatrix;
		}

		public void Reset()
		{
			localToWorldMatrix		= MathConstants.identityMatrix;
			brushToModelSpaceMatrix	= MathConstants.identityMatrix;
		}
	}
#endif
}