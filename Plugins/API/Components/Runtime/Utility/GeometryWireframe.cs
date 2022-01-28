using System;
using UnityEngine;

namespace RealtimeCSG
{
#if UNITY_EDITOR
	[Serializable]
	public sealed class GeometryWireframe
	{
		public Vector3[]	vertices                = null;
		public Int32[]		visibleOuterLines       = null;
		public Int32[]		visibleInnerLines       = null;
		public Int32[]		visibleTriangles		= null;
		public Int32[]		invisibleOuterLines     = null;
		public Int32[]		invisibleInnerLines     = null;
		public Int32[]		invalidLines            = null;
		public UInt64		outlineGeneration		= 0;
	}
#endif
}