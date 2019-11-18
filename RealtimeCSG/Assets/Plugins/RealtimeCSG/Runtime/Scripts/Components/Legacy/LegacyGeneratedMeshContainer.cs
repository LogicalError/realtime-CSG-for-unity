using UnityEngine;
using System.Collections.Generic;
using RealtimeCSG;
using System;

namespace InternalRealtimeCSG
{
	[DisallowMultipleComponent]
	[ExecuteInEditMode]
	public sealed class LegacyGeneratedMeshContainer : MonoBehaviour
	{
		void Awake()
		{
		    UnityEngine.Object.DestroyImmediate(this.gameObject);
		} 
	}
}
