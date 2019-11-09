using System;

namespace RealtimeCSG.Foundation
{
	/// <summary>Defines how the CSG operation is performed on the intersection between <see cref="RealtimeCSG.Foundation.CSGTreeBrush"/>es and/or <see cref="RealtimeCSG.Foundation.CSGTreeBranch"/>es.</summary>
	/// <remarks>See the [CSG Trees](~/documentation/CSGTrees.md) article for more information. <note>This enum is mirrored on the native side and cannot be modified.</note></remarks>
	/// <seealso cref="RealtimeCSG.Foundation.CSGTreeBrush"/>
	/// <seealso cref="RealtimeCSG.Foundation.CSGTreeBranch"/>
	[Serializable]
	public enum CSGOperationType : byte
	{
		/// <summary>The given <see cref="RealtimeCSG.Foundation.CSGTreeBrush"/> or <see cref="RealtimeCSG.Foundation.CSGTreeBranch"/> is added to the <see cref="RealtimeCSG.Foundation.CSGTree"/> and removes all the geometry inside it.</summary>
		Additive = 0,

		/// <summary>The given <see cref="RealtimeCSG.Foundation.CSGTreeBrush"/> or <see cref="RealtimeCSG.Foundation.CSGTreeBranch"/> removes all the geometry that are inside it.</summary>
		Subtractive = 1,

		/// <summary>The given <see cref="RealtimeCSG.Foundation.CSGTreeBrush"/> or <see cref="RealtimeCSG.Foundation.CSGTreeBranch"/> removes all the geometry that is outside it.</summary>
		Intersecting = 2
	}
}
