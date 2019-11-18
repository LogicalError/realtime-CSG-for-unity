using System;
using System.Runtime.InteropServices;
using Matrix4x4 = UnityEngine.Matrix4x4;
using Bounds = UnityEngine.Bounds;

namespace RealtimeCSG.Foundation
{
	/// <summary>Flags to modify default brush behavior.</summary>
	/// <remarks><note>This enum is mirrored on the native side and cannot be modified.</note></remarks>
	/// <seealso cref="RealtimeCSG.Foundation.CSGTreeBrush"/>
	/// <seealso cref="RealtimeCSG.Foundation.CSGTreeBrush.Flags"/>
	[Serializable, Flags]
	public enum CSGTreeBrushFlags
	{
		/// <summary>This brush has no special states</summary>
		Default		= 0,

		/// <summary>When set the brush is infinitely large. This can be used, for instance, to create an inverted world, by putting everything inside an infinitely large brush.</summary>
		Infinite	= 1
	}

	/// <summary>A leaf node in a CSG tree that has a shape (a <see cref="RealtimeCSG.Foundation.BrushMesh"/>) with which CSG operations can be performed.</summary>
	/// <remarks><note>This struct is a reference to node that exists on the native side, therefore its internal ID is not persistent.</note>
	/// <note>This struct can be converted into a <see cref="RealtimeCSG.Foundation.CSGTreeNode"/> and back again.</note>
	/// <note>Be careful when keeping track of <see cref="RealtimeCSG.Foundation.CSGTreeBrush"/>s because <see cref="RealtimeCSG.Foundation.BrushMeshInstance.BrushMeshID"/>s can be recycled after being Destroyed.</note>
	/// See the [CSG Trees](~/documentation/CSGTrees.md) and [Brush Meshes](~/documentation/brushMesh.md) articles for more information.</remarks>
	/// <seealso cref="RealtimeCSG.Foundation.CSGTreeNode"/>
	/// <seealso cref="RealtimeCSG.Foundation.CSGTree"/>
	/// <seealso cref="RealtimeCSG.Foundation.CSGTreeBranch"/>
	/// <seealso cref="RealtimeCSG.Foundation.BrushMesh"/>
	/// <seealso cref="RealtimeCSG.Foundation.BrushMeshInstance"/>
	[StructLayout(LayoutKind.Sequential, Pack = 4)]	
	public partial struct CSGTreeBrush 
	{
		#region Create
		/// <summary>Generates a brush on the native side and returns a <see cref="RealtimeCSG.Foundation.CSGTreeBrush"/> struct that contains a reference to it.</summary>
		/// <param name="userID">A unique id to help identify this particular brush. For instance, this could be an InstanceID to a [UnityEngine.Object](https://docs.unity3d.com/ScriptReference/Object.html)</param>
		/// <param name="localTransformation">The transformation of the brush relative to the tree root</param>
		/// <param name="brushMesh">A <see cref="RealtimeCSG.Foundation.BrushMeshInstance"/>, which is a reference to a <see cref="RealtimeCSG.Foundation.BrushMesh"/>.</param>
		/// <param name="operation">The <see cref="RealtimeCSG.Foundation.CSGOperationType"/> that needs to be performed with this <see cref="RealtimeCSG.Foundation.CSGTreeBrush"/>.</param>
		/// <param name="flags"><see cref="RealtimeCSG.Foundation.CSGTreeBrush"/> specific flags</param>
		/// <returns>A new <see cref="RealtimeCSG.Foundation.CSGTreeBrush"/>. May be an invalid node if it failed to create it.</returns>
		public static CSGTreeBrush Create(Int32 userID, Matrix4x4 localTransformation, BrushMeshInstance brushMesh = default(BrushMeshInstance), CSGOperationType operation = CSGOperationType.Additive, CSGTreeBrushFlags flags = CSGTreeBrushFlags.Default)
		{
			int brushNodeID;
			if (GenerateBrush(userID, out brushNodeID))
			{ 
				if (localTransformation != default(Matrix4x4)) SetNodeLocalTransformation(brushNodeID, ref localTransformation);
				if (operation != CSGOperationType.Additive) SetBrushOperationType(brushNodeID, operation);
				if (flags     != CSGTreeBrushFlags.Default) SetBrushFlags(brushNodeID, flags);
				if (brushMesh.Valid) SetBrushMesh(brushNodeID, brushMesh);
			} else
				brushNodeID = 0;
			return new CSGTreeBrush() { brushNodeID = brushNodeID };
		}
		
		/// <summary>Generates a brush on the native side and returns a <see cref="RealtimeCSG.Foundation.CSGTreeBrush"/> struct that contains a reference to it.</summary>
		/// <param name="localTransformation">The transformation of the brush relative to the tree root</param>
		/// <param name="brushMesh">A <see cref="RealtimeCSG.Foundation.BrushMeshInstance"/>, which is a reference to a <see cref="RealtimeCSG.Foundation.BrushMesh"/>.</param>
		/// <param name="operation">The <see cref="RealtimeCSG.Foundation.CSGOperationType"/> that needs to be performed with this <see cref="RealtimeCSG.Foundation.CSGTreeBrush"/>.</param>
		/// <param name="flags"><see cref="RealtimeCSG.Foundation.CSGTreeBrush"/> specific flags</param>
		/// <returns>A new <see cref="RealtimeCSG.Foundation.CSGTreeBrush"/>. May be an invalid node if it failed to create it.</returns>
		public static CSGTreeBrush Create(Matrix4x4 localTransformation, BrushMeshInstance brushMesh = default(BrushMeshInstance), CSGOperationType operation = CSGOperationType.Additive, CSGTreeBrushFlags flags = CSGTreeBrushFlags.Default)
		{
			return Create(0, localTransformation, brushMesh, operation, flags);
		}
		
		/// <summary>Generates a brush on the native side and returns a <see cref="RealtimeCSG.Foundation.CSGTreeBrush"/> struct that contains a reference to it.</summary>
		/// <param name="userID">A unique id to help identify this particular brush. For instance, this could be an InstanceID to a [UnityEngine.Object](https://docs.unity3d.com/ScriptReference/Object.html)</param>
		/// <param name="brushMesh">A <see cref="RealtimeCSG.Foundation.BrushMeshInstance"/>, which is a reference to a <see cref="RealtimeCSG.Foundation.BrushMesh"/>.</param>
		/// <param name="operation">The <see cref="RealtimeCSG.Foundation.CSGOperationType"/> that needs to be performed with this <see cref="RealtimeCSG.Foundation.CSGTreeBrush"/>.</param>
		/// <param name="flags"><see cref="RealtimeCSG.Foundation.CSGTreeBrush"/> specific flags</param>
		/// <returns>A new <see cref="RealtimeCSG.Foundation.CSGTreeBrush"/>. May be an invalid node if it failed to create it.</returns>
		public static CSGTreeBrush Create(Int32 userID = 0, BrushMeshInstance brushMesh = default(BrushMeshInstance), CSGOperationType operation = CSGOperationType.Additive, CSGTreeBrushFlags flags = CSGTreeBrushFlags.Default)
		{
			return Create(userID, default(Matrix4x4), brushMesh, operation, flags);
		}
		#endregion


		#region Node
		/// <value>Returns if the current <see cref="RealtimeCSG.Foundation.CSGTreeBrush"/> is valid or not.</value>
		/// <remarks><note>If <paramref name="Valid"/> is <b>false</b> that could mean that this node has been destroyed.</note></remarks>
		public bool				Valid			{ get { return brushNodeID != CSGTreeNode.InvalidNodeID && CSGTreeNode.IsNodeIDValid(brushNodeID); } }

		/// <value>Gets the <see cref="RealtimeCSG.Foundation.CSGTreeBrush.NodeID"/> of the <see cref="RealtimeCSG.Foundation.CSGTreeBrush"/>, which is a unique ID of this node.</value>
		/// <remarks><note>NodeIDs are eventually recycled, so be careful holding on to Nodes that have been destroyed.</note></remarks>
		public Int32			NodeID			{ get { return brushNodeID; } }

		/// <value>Gets the <see cref="RealtimeCSG.Foundation.CSGTreeBrush.UserID"/> set to the <see cref="RealtimeCSG.Foundation.CSGTreeBrush"/> at creation time.</value>
		public Int32			UserID			{ get { return CSGTreeNode.GetUserIDOfNode(brushNodeID); } }

		/// <value>Returns the dirty flag of the <see cref="RealtimeCSG.Foundation.CSGTreeBrush"/>. When the it's dirty, then it means (some of) its generated meshes have been modified.</value>
		public bool				Dirty			{ get { return CSGTreeNode.IsNodeDirty(brushNodeID); } }

		/// <summary>Force set the dirty flag of the <see cref="RealtimeCSG.Foundation.CSGTreeBrush"/>.</summary>
		public void SetDirty	()				{ CSGTreeNode.SetDirty(brushNodeID); }

		/// <summary>Destroy this <see cref="RealtimeCSG.Foundation.CSGTreeBrush"/>. Sets the state to invalid.</summary>
		/// <returns><b>true</b> on success, <b>false</b> on failure</returns>
		public bool Destroy		()				{ return CSGTreeNode.DestroyNode(brushNodeID); }
		#endregion

		#region ChildNode
		/// <value>Returns the parent <see cref="RealtimeCSG.Foundation.CSGTreeBranch"/> this <see cref="RealtimeCSG.Foundation.CSGTreeBrush"/> is a child of. Returns an invalid node if it's not a child of any <see cref="RealtimeCSG.Foundation.CSGTreeBranch"/>.</value>
		public CSGTreeBranch	Parent				{ get { return new CSGTreeBranch { branchNodeID = CSGTreeNode.GetParentOfNode(brushNodeID) }; } }
		
		/// <value>Returns tree this <see cref="RealtimeCSG.Foundation.CSGTreeBrush"/> belongs to.</value>
		public CSGTree			Tree				{ get { return new CSGTree       { treeNodeID   = CSGTreeNode.GetTreeOfNode(brushNodeID) }; } }

		/// <value>The CSG operation that this <see cref="RealtimeCSG.Foundation.CSGTreeBrush"/> will use.</value>
		public CSGOperationType Operation			{ get { return (CSGOperationType)GetBrushOperationType(brushNodeID); } set { SetBrushOperationType(brushNodeID, value); } }
		#endregion

		#region TreeBrush specific
		/// <value>Gets or sets <see cref="RealtimeCSG.Foundation.CSGTreeBrush"/> specific flags.</value>
		public CSGTreeBrushFlags Flags				{ get { return GetBrushFlags(brushNodeID); } set	{ SetBrushFlags(brushNodeID, value); } }

		/// <value>Sets or gets a <see cref="RealtimeCSG.Foundation.BrushMeshInstance"/></value>
		/// <remarks>By modifying the <see cref="RealtimeCSG.Foundation.BrushMeshInstance"/> you can change the shape of the <see cref="RealtimeCSG.Foundation.CSGTreeBrush"/>
		/// <note><see cref="RealtimeCSG.Foundation.BrushMeshInstance"/>s can be shared between <see cref="RealtimeCSG.Foundation.CSGTreeBrush"/>es.</note></remarks>
		/// <seealso cref="RealtimeCSG.Foundation.BrushMesh" />
		public BrushMeshInstance BrushMesh			{ set { SetBrushMesh(brushNodeID, value); } get { return GetBrushMesh(brushNodeID); } }

		/// <value>Gets or sets the transformation of the brush relative to the tree root.</value>
		public Matrix4x4		LocalTransformation	{ get { return GetNodeLocalTransformation(brushNodeID); } set { SetNodeLocalTransformation(brushNodeID, ref value); } }

		/// <value>Gets the bounds of this <see cref="RealtimeCSG.Foundation.CSGTreeBrush"/>.</value>
		public Bounds			Bounds				{ get { return GetBrushBounds(brushNodeID); } }
		#endregion


		internal Int32 brushNodeID;
	}
}