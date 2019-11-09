using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using InternalRealtimeCSG;

namespace RealtimeCSG
{
	[Serializable]
	internal sealed class CylinderDefinition
	{
		[SerializeField] public int			circleSides				= 18;
		[SerializeField] public float		circleOffset			= 0;
		[SerializeField] public bool		circleSmoothShading		= true;
		[SerializeField] public bool		circleSingleSurfaceEnds = true;
		[SerializeField] public bool		circleDistanceToSide    = true;
		[SerializeField] public bool		circleRecenter			= true;
		[SerializeField] public float		circleRadius			= 0.0f;
//		[SerializeField] public Vector3		circleTangent;
//		[SerializeField] public Vector3		circleBinormal;

	}
}
