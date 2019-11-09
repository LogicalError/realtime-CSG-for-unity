using System;
using RealtimeCSG.Legacy;
using RealtimeCSG.Components;

namespace RealtimeCSG
{
	internal static class BrushTraits
	{
		public static bool IsSurfaceSelectable(CSGBrush brush, int surfaceIndex)
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

			if ((texGenFlags[texGenIndex] & TexGenFlags.NoRender) == TexGenFlags.NoRender)
				return !CSGSettings.ShowHiddenSurfaces;

			return false;
		}
	}
}
