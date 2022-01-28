using System;
using System.Runtime.InteropServices;

namespace RealtimeCSG.Foundation
{
	/// <summary>Represents the instance of a <see cref="RealtimeCSG.Foundation.BrushMesh"/>. This can be used to assign a <see cref="RealtimeCSG.Foundation.BrushMesh"/> to one or multiple <see cref="RealtimeCSG.Foundation.CSGTreeBrush"/>es.</summary>
	/// <remarks>See the [Brush Meshes](~/documentation/brushMesh.md) article for more information.
	/// <note>Be careful when keeping track of <see cref="RealtimeCSG.Foundation.BrushMeshInstance"/>s because <see cref="RealtimeCSG.Foundation.BrushMeshInstance.BrushMeshID"/>s can be recycled after being Destroyed.</note></remarks>
	/// <seealso cref="RealtimeCSG.Foundation.BrushMesh"/>
	/// <seealso cref="RealtimeCSG.Foundation.CSGTreeBrush"/>
	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public partial struct BrushMeshInstance
	{
		internal Int32			brushMeshID;

		/// <value>Is the current <see cref="RealtimeCSG.Foundation.BrushMeshInstance"/> in a correct state</value>
		public bool				Valid				{ get { return brushMeshID != BrushMeshInstance.InvalidInstanceID && IsBrushMeshIDValid(brushMeshID); } }
		
		/// <value>Returns the unique id of this <see cref="RealtimeCSG.Foundation.BrushMesh"/></value>
		public Int32			BrushMeshID			{ get { return brushMeshID; } }

		/// <value>Gets the <see cref="RealtimeCSG.Foundation.BrushMeshInstance.UserID"/> set to the <see cref="RealtimeCSG.Foundation.BrushMeshInstance"/> at creation time.</value>
		public Int32			UserID				{ get { return GetBrushMeshUserID(brushMeshID); } }
				
		/// <summary>Create a <see cref="RealtimeCSG.Foundation.BrushMeshInstance"/> from a given <see cref="RealtimeCSG.Foundation.BrushMesh"/></summary>
		/// <param name="brushMesh">The <see cref="RealtimeCSG.Foundation.BrushMesh"/> to create an instance with</param>
		/// <returns>A newly created <see cref="RealtimeCSG.Foundation.BrushMeshInstance"/> on success, or an invalid <see cref="RealtimeCSG.Foundation.BrushMeshInstance"/> on failure.</returns>
		public static BrushMeshInstance Create(BrushMesh brushMesh, Int32 userID = 0) { return new BrushMeshInstance { brushMeshID = CreateBrushMesh(userID, brushMesh) }; }
				
		/// <summary>Destroy the <see cref="RealtimeCSG.Foundation.BrushMeshInstance"/> and release the memory used by this instance.</summary>
		public void	Destroy		()						{ var prevBrushMeshID = brushMeshID; brushMeshID = BrushMeshInstance.InvalidInstanceID; DestroyBrushMesh(prevBrushMeshID); }
		
		/// <summary>Update this <see cref="RealtimeCSG.Foundation.BrushMeshInstance"/> with the given <see cref="RealtimeCSG.Foundation.BrushMesh"/>.</summary>
		/// <param name="brushMesh">The <see cref="RealtimeCSG.Foundation.BrushMesh"/> to update the <see cref="RealtimeCSG.Foundation.BrushMeshInstance"/> with</param>
		/// <returns><b>true</b> on success, <b>false</b> on failure. In case of failure the brush will keep using the previously set <see cref="RealtimeCSG.Foundation.BrushMesh"/>.</returns>
		public bool Set			(BrushMesh brushMesh)	{ return UpdateBrushMesh(brushMeshID, brushMesh); }

		/// <value>An invalid instance</value>
		public static readonly BrushMeshInstance InvalidInstance = new BrushMeshInstance { brushMeshID = BrushMeshInstance.InvalidInstanceID };
		internal const Int32 InvalidInstanceID = 0;
	}
}