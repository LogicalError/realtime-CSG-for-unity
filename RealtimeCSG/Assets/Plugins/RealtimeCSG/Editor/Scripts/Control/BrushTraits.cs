using System;
using RealtimeCSG.Legacy;
using RealtimeCSG.Components;
using UnityEngine;

namespace RealtimeCSG
{
	internal static class BrushTraits
	{
		public static bool IsSurfaceUnselectable(CSGBrush brush, int surfaceIndex, bool isTrigger, bool ignoreSurfaceFlags = false)
		{
			if (!brush)
			{
				return true;
			}

			var shape = brush.Shape;
			if (shape == null)
			{
				return true;
			}

			var surfaces = shape.Surfaces;
			if (surfaces == null)
			{
				return true;
			}

			if (surfaceIndex < 0 || surfaceIndex >= surfaces.Length)
			{
				return true;
			}

			var texGenIndex = surfaces[surfaceIndex].TexGenIndex;
			var texGenFlags = shape.TexGenFlags;
			if (texGenFlags == null ||
				texGenIndex < 0 || texGenIndex >= texGenFlags.Length)
			{
				return true;
			}

			if (!ignoreSurfaceFlags)
			{
				var isRenderable = (texGenFlags[texGenIndex] & TexGenFlags.NoRender) != TexGenFlags.NoRender;
				if (!isRenderable && !CSGSettings.ShowHiddenSurfaces)
					return true;

				var isCollidable = (texGenFlags[texGenIndex] & TexGenFlags.NoCollision) != TexGenFlags.NoCollision;
				if (!isCollidable)
				{
					if (isTrigger)
					{
						if (!CSGSettings.ShowTriggerSurfaces)
							return true;
					} else
					{
						if (!CSGSettings.ShowColliderSurfaces)
							return true;
					}
				}
			}			
			return false;
		}
	}
}
