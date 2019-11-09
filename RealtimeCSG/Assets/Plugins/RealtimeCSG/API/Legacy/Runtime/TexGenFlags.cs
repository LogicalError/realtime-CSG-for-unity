using System;

namespace RealtimeCSG.Legacy
{
	/// <summary>Flags to specify in what meshes a surface is part of</summary>
	/// <remarks><note>This code is legacy and will be removed eventually.</note></remarks>
	/// <seealso cref="RealtimeCSG.Legacy.Shape"/>
	/// <seealso cref="RealtimeCSG.Legacy.ControlMesh"/>
	/// <seealso cref="RealtimeCSG.Legacy.Polygon"/>
	/// <seealso cref="RealtimeCSG.Legacy.TexGen"/>
	[Flags]
	public enum TexGenFlags : int //32 bits
	{
		/// <summary>Surface uses default settings</summary>
		None				= 0,

		/// <summary>UV coordinates are calculated in brush space instead of world space.</summary>
		WorldSpaceTexture	= 1,
		
		/// <summary>Surface is not rendered.</summary>
		NoRender			= 2,		// do not render
		
		/// <summary>Surface does not cast shadows.</summary>
		NoCastShadows		= 4,

		/// <summary>Surface does not receive shadows.</summary>
		NoReceiveShadows	= 8,
		
		/// <summary>Surface is not part of the collision mesh.</summary>
		NoCollision			= 16		// do not add surface to collider
	};
}
