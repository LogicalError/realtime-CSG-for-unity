#if UNITY_EDITOR
using System;
using UnityEngine;
using InternalRealtimeCSG;
using System.Runtime.InteropServices;
using RealtimeCSG.Legacy;
using RealtimeCSG.Components;

namespace RealtimeCSG
{
	[Serializable, StructLayout(LayoutKind.Sequential, Pack = 4)]
    public sealed class LegacyBrushIntersection
	{
		public GameObject	gameObject;
		public CSGModel     model;
		public CSGBrush     brush;
		public Int32		brushUserID;
		public Int32		brushNodeID;
		public int			surfaceIndex;

		public CSGPlane     localPlane;
		public CSGPlane     modelPlane;
		public CSGPlane		worldPlane;
		public Vector3		worldIntersection;
		public Vector2		surfaceIntersection;
		public float        distance;
	};

	[Serializable, StructLayout(LayoutKind.Sequential, Pack = 4)]
	public sealed class LegacySurfaceIntersection
	{
		public CSGPlane     localPlane;
		public CSGPlane     modelPlane;
		public CSGPlane     worldPlane;
		public Vector2      surfaceIntersection;
		public Vector3      worldIntersection;
		public float        distance;
	};
}
#endif