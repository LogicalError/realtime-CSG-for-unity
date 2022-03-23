using System;
using UnityEngine;
using RealtimeCSG.Legacy;
using InternalRealtimeCSG;

namespace RealtimeCSG
{
	// this allows us to determine if our brush has any of it's surfaces changed
#if UNITY_EDITOR
	[Serializable]
	public sealed class CompareShape
	{
		public Surface[]		prevSurfaces	= new Surface[0];
		public TexGen[]			prevTexGens		= new TexGen[0];
		public TexGenFlags[]	prevTexGenFlags = new TexGenFlags[0];

		public void EnsureInitialized(Shape shape)
		{
			if (prevTexGens == null ||
				prevTexGens.Length != shape.TexGens.Length)
			{
				prevTexGens = new TexGen[shape.TexGens.Length];
				prevTexGenFlags = new TexGenFlags[shape.TexGens.Length];
			}
			Array.Copy(shape.TexGens, prevTexGens, shape.TexGens.Length);
			Array.Copy(shape.TexGenFlags, prevTexGenFlags, shape.TexGenFlags.Length);

			if (prevSurfaces == null ||
				prevSurfaces.Length != shape.Surfaces.Length)
			{
				prevSurfaces = new Surface[shape.Surfaces.Length];
			}
			Array.Copy(shape.Surfaces, prevSurfaces, shape.Surfaces.Length);
		}

		public void Reset()
		{
			prevSurfaces	= new Surface[0];
			prevTexGens		= new TexGen[0];
			prevTexGenFlags = new TexGenFlags[0];
		}
	}
#endif
}