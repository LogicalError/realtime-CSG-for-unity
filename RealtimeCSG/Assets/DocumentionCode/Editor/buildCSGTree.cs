using RealtimeCSG.Foundation;
using UnityEngine;

partial class GenerateCSGTreeExample
{
	public CSGTree CreateTree()
	{
		// Every node can have its own unique ID, this can be used to identify nodes
		const int treeUserID		= 1;
		const int branchUserID		= 2;
		const int brushAUserID		= 3;
		const int brushBUserID		= 4;
		const int brushMeshUserID	= 5;

		// Create a cube BrushMesh that we can use for our brushes
		BrushMeshInstance	cube	= CreateCube(userID: brushMeshUserID);

		// Create a matrix for each brush, each with a different position
		Matrix4x4 brushPositionA = Matrix4x4.TRS(new Vector3(-0.5f, 0, 0), Quaternion.identity, Vector3.one);
		Matrix4x4 brushPositionB = Matrix4x4.TRS(new Vector3( 0.5f, 0, 0), Quaternion.identity, Vector3.one);
		
		// Create two brushes
		CSGTreeBrush brushA, brushB;
		brushA = CSGTreeBrush.Create(userID:				brushAUserID,
									 localTransformation:	brushPositionA,
									 brushMesh:				cube,
									 operation:				CSGOperationType.Additive,
									 flags:					CSGTreeBrushFlags.Default);

		brushB = CSGTreeBrush.Create(userID:				brushBUserID,
									 localTransformation:	brushPositionB,
									 brushMesh:				cube,
									 operation:				CSGOperationType.Subtractive,
									 flags:					CSGTreeBrushFlags.Default);

		// Create a branch that contains both brushes
		CSGTreeBranch branch;
		branch = CSGTreeBranch.Create(userID:			branchUserID,
									  operation:		CSGOperationType.Additive,
									  // children of this branch:
									  children:			new CSGTreeNode[] { brushA, brushB });

		// Create a tree that contains the branch
		CSGTree tree;
		tree = CSGTree.Create(userID:		treeUserID,
							  children:		new CSGTreeNode[] { branch });
		return tree;
	}
}