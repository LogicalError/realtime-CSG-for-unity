using UnityEditor;
using UnityEngine;
using RealtimeCSG.Legacy;

namespace RealtimeCSG
{

	internal static class CSG_HandleUtility
	{
		public static float GetHandleSize(Vector3 position)
		{
			return HandleUtility.GetHandleSize(position) / 20.0f;
			//Mathf.Max(0.01f, );
		}

		public static CSGPlane GetNearPlane(Camera camera)
		{
			var cameraTransform = camera.transform;
			var normal = cameraTransform.forward;
			var pos = cameraTransform.position + ((camera.nearClipPlane + 0.01f) * normal);
			return new CSGPlane(normal, pos);
		}
	}
}
