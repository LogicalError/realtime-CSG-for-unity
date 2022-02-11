using System;
using System.Runtime.InteropServices;

namespace RealtimeCSG.Foundation
{
	/// <summary>Enum which describes the type of a node</summary>
	/// <remarks><note>This enum is mirrored on the native side and cannot be modified.</note></remarks>
	public enum CSGNodeType : byte
	{
		/// <summary>
		/// Invalid or unknown node.
		/// </summary>
		None,
		/// <summary>
		/// Node is a <see cref="RealtimeCSG.Foundation.CSGTree"/>.
		/// </summary>
		Tree,
		/// <summary>
		/// Node is a <see cref="RealtimeCSG.Foundation.CSGTreeBranch"/>.
		/// </summary>
		Branch,
		/// <summary>
		/// Node is a <see cref="RealtimeCSG.Foundation.CSGTreeBrush"/>.
		/// </summary>
		Brush
	};

	/// <summary>Represents a generic node in a CSG tree. This is used to be able to store different types of nodes together.</summary>
	/// <remarks><note>This struct is a reference to node that exists on the native side, therefore it is not persistent.</note>
	/// <note>This struct can be converted into a <see cref="RealtimeCSG.Foundation.CSGTreeBrush"/>, <see cref="RealtimeCSG.Foundation.CSGTreeBranch"/> or 
	/// <see cref="RealtimeCSG.Foundation.CSGTree"/> depending on what kind of node is stored in the <see cref="RealtimeCSG.Foundation.CSGTreeNode"/>.
	/// The type of node can be queried by using <see cref="RealtimeCSG.Foundation.CSGTreeNode.Type"/>. 
	/// If a <see cref="RealtimeCSG.Foundation.CSGTreeNode"/> is cast to the wrong kind of node, an invalid node is generated.</note>
	/// <note>Be careful when keeping track of <see cref="RealtimeCSG.Foundation.CSGTreeNode"/>s because <see cref="RealtimeCSG.Foundation.BrushMeshInstance.BrushMeshID"/>s can be recycled after being Destroyed.</note></remarks>	
	/// <seealso cref="RealtimeCSG.Foundation.CSGTree"/>
	/// <seealso cref="RealtimeCSG.Foundation.CSGTreeBranch"/>
	/// <seealso cref="RealtimeCSG.Foundation.CSGTreeBrush"/>
	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public partial struct CSGTreeNode 
	{
		#region Node
		/// <value>Returns if the current <see cref="RealtimeCSG.Foundation.CSGTreeNode"/> is valid or not.</value>
		/// <remarks><note>If <paramref name="Valid"/> is *false* that could mean that this node has been destroyed.</note></remarks>
		public bool				Valid			{ get { return nodeID != CSGTreeNode.InvalidNodeID && CSGTreeNode.IsNodeIDValid(nodeID); } }

		/// <value>Gets the <see cref="RealtimeCSG.Foundation.CSGTreeNode.NodeID"/> of the <see cref="RealtimeCSG.Foundation.CSGTreeNode"/>, which is a unique ID of this node.</value>
		/// <remarks><note>NodeIDs are eventually recycled, so be careful holding on to Nodes that have been destroyed.</note></remarks>
		public Int32			NodeID			{ get { return nodeID; } }

		/// <value>Gets the <see cref="RealtimeCSG.Foundation.CSGTreeNode.UserID"/> set to the <see cref="RealtimeCSG.Foundation.CSGTreeNode"/> at creation time.</value>
		public Int32			UserID			{ get { return CSGTreeNode.GetUserIDOfNode(nodeID); } }

		/// <value>Returns the dirty flag of the <see cref="RealtimeCSG.Foundation.CSGTreeNode"/>.</value>
		public bool				Dirty			{ get { return CSGTreeNode.IsNodeDirty(nodeID); } }
	
		/// <summary>Force set the dirty flag of the <see cref="RealtimeCSG.Foundation.CSGTreeBrush"/>.</summary>
		public void SetDirty	()				{ CSGTreeNode.SetDirty(nodeID); }
	
		/// <summary>Destroy this <see cref="RealtimeCSG.Foundation.CSGTreeNode"/>. Sets the state to invalid.</summary>
		/// <returns><b>true</b> on success, <b>false</b> on failure</returns>
		public bool	Destroy		()				{ return CSGTreeNode.DestroyNode(nodeID); }
		#endregion

		#region ChildNode
		/// <value>Returns the parent <see cref="RealtimeCSG.Foundation.CSGTreeBranch"/> this <see cref="RealtimeCSG.Foundation.CSGTreeNode"/> is a child of. Returns an invalid node if it's not a child of any <see cref="RealtimeCSG.Foundation.CSGTreeBranch"/>.</value>
		public CSGTreeBranch	Parent			{ get { return new CSGTreeBranch { branchNodeID = CSGTreeNode.GetParentOfNode(nodeID) }; } }

		/// <value>Returns tree this <see cref="RealtimeCSG.Foundation.CSGTreeNode"/> belongs to.</value>
		public CSGTree			Tree			{ get { return new CSGTree		 { treeNodeID   = CSGTreeNode.GetTreeOfNode(nodeID)  }; } }
		#endregion

		#region ChildNodeContainer
		/// <value>Gets the number of elements contained in the <see cref="RealtimeCSG.Foundation.CSGTreeBranch"/>.</value>
		public Int32			Count			{ get { return CSGTreeNode.GetChildNodeCount(nodeID); } }

		/// <summary>Gets child at the specified index.</summary>
		/// <param name="index">The zero-based index of the child to get.</param>
		/// <returns>The element at the specified index.</returns>
		public CSGTreeNode		this[int index]	{ get { return new CSGTreeNode { nodeID = CSGTreeNode.GetChildNodeAtIndex(nodeID, index) }; } }
		
		/// <summary>Copies the <see cref="RealtimeCSG.Foundation.CSGTreeNode"/>s of the <see cref="RealtimeCSG.Foundation.CSGTreeBranch"/> to a new array.</summary>
		/// <returns>An array containing the <see cref="RealtimeCSG.Foundation.CSGTreeNode"/>s of the <see cref="RealtimeCSG.Foundation.CSGTreeBranch"/>.</returns>
		public CSGTreeNode[]	ChildrenToArray() { return CSGTreeNode.GetChildNodes(nodeID); }
		#endregion

		#region TreeNode specific
		/// <value>Gets the node-type of this <see cref="RealtimeCSG.Foundation.CSGTreeNode"/>.</value>
		public CSGNodeType		Type				  { get { return CSGTreeNode.GetTypeOfNode(nodeID); } }
		
		/// <summary>Creates a <see cref="RealtimeCSG.Foundation.CSGTreeNode"/> from a given nodeID.</summary>
		/// <param name="id">ID of the node to create a <see cref="RealtimeCSG.Foundation.CSGTreeNode"/> from</param>
		/// <returns>A <see cref="RealtimeCSG.Foundation.CSGTreeNode"/> with the givenID. If the ID is not known on the native side, this will result in an invalid <see cref="RealtimeCSG.Foundation.CSGTreeNode"/>.</returns>
		public static CSGTreeNode Encapsulate(int id) { return new CSGTreeNode { nodeID = id }; }
		
		/// <summary>Operator to implicitly convert a <see cref="RealtimeCSG.Foundation.CSGTree"/> into a <see cref="RealtimeCSG.Foundation.CSGTreeNode"/>.</summary>
		/// <param name="tree">The <see cref="RealtimeCSG.Foundation.CSGTree"/> to convert into a <see cref="RealtimeCSG.Foundation.CSGTreeNode"/>.</param>
		/// <returns>A <see cref="RealtimeCSG.Foundation.CSGTreeNode"/> containing the same NodeID as <paramref name="tree"/></returns>
		/// <remarks>This can be used to build arrays of <see cref="RealtimeCSG.Foundation.CSGTreeNode"/>'s that contain a mix of type of nodes.</remarks>
		public static implicit operator CSGTreeNode   (CSGTree       tree  ) { return new CSGTreeNode { nodeID = tree.treeNodeID     }; }

		/// <summary>Operator to implicitly convert a <see cref="RealtimeCSG.Foundation.CSGTreeBranch"/> into a <see cref="RealtimeCSG.Foundation.CSGTreeNode"/>.</summary>
		/// <param name="branch">The <see cref="RealtimeCSG.Foundation.CSGTreeBranch"/> to convert into a <see cref="RealtimeCSG.Foundation.CSGTreeNode"/>.</param>
		/// <returns>A <see cref="RealtimeCSG.Foundation.CSGTreeNode"/> containing the same NodeID as <paramref name="branch"/></returns>
		/// <remarks>This can be used to build arrays of <see cref="RealtimeCSG.Foundation.CSGTreeNode"/>'s that contain a mix of type of nodes.</remarks>
		public static implicit operator CSGTreeNode   (CSGTreeBranch branch) { return new CSGTreeNode { nodeID = branch.branchNodeID }; }

		/// <summary>Operator to implicitly convert a <see cref="RealtimeCSG.Foundation.CSGTreeBrush"/> into a <see cref="RealtimeCSG.Foundation.CSGTreeNode"/>.</summary>
		/// <param name="brush">The <see cref="RealtimeCSG.Foundation.CSGTreeBrush"/> to convert into a <see cref="RealtimeCSG.Foundation.CSGTreeNode"/>.</param>
		/// <returns>A <see cref="RealtimeCSG.Foundation.CSGTreeNode"/> containing the same NodeID as <paramref name="brush"/></returns>
		/// <remarks>This can be used to build arrays of <see cref="RealtimeCSG.Foundation.CSGTreeNode"/>'s that contain a mix of type of nodes.</remarks>
		public static implicit operator CSGTreeNode   (CSGTreeBrush  brush ) { return new CSGTreeNode { nodeID = brush.brushNodeID   }; }
		
		/// <summary>Operator to allow a <see cref="RealtimeCSG.Foundation.CSGTreeNode"/> to be explicitly converted into a <see cref="RealtimeCSG.Foundation.CSGTree"/>.</summary>
		/// <param name="node">The <see cref="RealtimeCSG.Foundation.CSGTreeNode"/> to be convert into a <see cref="RealtimeCSG.Foundation.CSGTree"/></param>
		/// <returns>A valid <see cref="RealtimeCSG.Foundation.CSGTree"/> if <paramref name="node"/> actually was one, otherwise an invalid node.</returns>
		public static explicit operator CSGTree       (CSGTreeNode   node  ) { if (node.Type != CSGNodeType.Tree) return new CSGTree       { treeNodeID   = InvalidNodeID }; else return new CSGTree       { treeNodeID   = node.nodeID }; }

		/// <summary>Operator to allow a <see cref="RealtimeCSG.Foundation.CSGTreeNode"/> to be explicitly converted into a <see cref="RealtimeCSG.Foundation.CSGTreeBranch"/>.</summary>
		/// <param name="node">The <see cref="RealtimeCSG.Foundation.CSGTreeNode"/> to be convert into a <see cref="RealtimeCSG.Foundation.CSGTreeBranch"/></param>
		/// <returns>A valid <see cref="RealtimeCSG.Foundation.CSGTreeBranch"/> if <paramref name="node"/> actually was one, otherwise an invalid node.</returns>
		public static explicit operator CSGTreeBranch (CSGTreeNode   node  ) { if (node.Type != CSGNodeType.Branch) return new CSGTreeBranch { branchNodeID = InvalidNodeID }; else return new CSGTreeBranch { branchNodeID = node.nodeID }; }

		/// <summary>Operator to allow a <see cref="RealtimeCSG.Foundation.CSGTreeNode"/> to be explicitly converted into a <see cref="RealtimeCSG.Foundation.CSGTreeBrush"/>.</summary>
		/// <param name="node">The <see cref="RealtimeCSG.Foundation.CSGTreeNode"/> to be convert into a <see cref="RealtimeCSG.Foundation.CSGTreeBrush"/></param>
		/// <returns>A valid <see cref="RealtimeCSG.Foundation.CSGTreeBrush"/> if <paramref name="node"/> actually was one, otherwise an invalid node.</returns>
		public static explicit operator CSGTreeBrush  (CSGTreeNode   node  ) { if (node.Type != CSGNodeType.Brush) return new CSGTreeBrush  { brushNodeID  = InvalidNodeID }; else return new CSGTreeBrush  { brushNodeID  = node.nodeID }; }
		
		/// <value>An invalid node</value>
		public static readonly CSGTreeNode InvalidNode = new CSGTreeNode { nodeID = CSGTreeNode.InvalidNodeID };
		internal const Int32 InvalidNodeID = 0;
		#endregion

		internal Int32 nodeID;
	}
}