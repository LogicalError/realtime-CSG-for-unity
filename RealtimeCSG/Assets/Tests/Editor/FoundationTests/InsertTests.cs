using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using RealtimeCSG;
using RealtimeCSG.Foundation;
using UnityEditor.SceneManagement;

[TestFixture]
public partial class InsertTests
{
	[SetUp]
	public void Init()
	{
		CSGManager.Clear();
	}

	/*
	[Test]
	public void InvalidNode_InsertBranch_ReturnsFalse()
	{
		const int branchUserID = 10;
		var branch = CSGTreeBranch.Create(branchUserID);
		var invalidNode = CSGTreeNode.InvalidNode;
		CSGUtility.ClearDirty(branch);

		var result = invalidNode.Insert(0, branch);

		Assert.AreEqual(false, result);
		TestUtility.ExpectValidBranchWithUserID(ref branch, branchUserID);
		Assert.AreEqual(false, branch.Dirty);
		Assert.AreEqual(0, branch.Count);
		Assert.AreEqual(0, CSGUtility.TreeBrushCount);
		Assert.AreEqual(0, CSGUtility.TreeCount);
		Assert.AreEqual(1, CSGUtility.TreeBranchCount);
		Assert.AreEqual(1, CSGUtility.TreeNodeCount);
	}
	*/

	[Test]
	public void Branch_InsertInvalidNode_ReturnsFalse()
	{
		const int branchUserID = 10;
		var branch	= CSGTreeBranch.Create(branchUserID);
		CSGManager.ClearDirty(branch);

		var result		= branch.Insert(0, CSGTreeNode.InvalidNode);

		Assert.AreEqual(false, result);
		TestUtility.ExpectValidBranchWithUserID(ref branch, branchUserID);
		Assert.AreEqual(false, branch.Dirty);
		Assert.AreEqual(0, branch.Count);
		Assert.AreEqual(0, CSGManager.TreeBrushCount);
		Assert.AreEqual(0, CSGManager.TreeCount);
		Assert.AreEqual(1, CSGManager.TreeBranchCount);
		Assert.AreEqual(1, CSGManager.TreeNodeCount);
	}

	[Test]
	public void Branch_InsertSelf_ReturnsFalse()
	{
		const int branchUserID = 10;
		var branch	= CSGTreeBranch.Create(branchUserID);
		CSGManager.ClearDirty(branch);

		var result		= branch.Insert(0, branch);

		Assert.AreEqual(false, result);
		TestUtility.ExpectValidBranchWithUserID(ref branch, branchUserID);
		Assert.AreEqual(false, branch.Dirty);
		Assert.AreEqual(0, branch.Count);
		Assert.AreEqual(0, CSGManager.TreeBrushCount);
		Assert.AreEqual(0, CSGManager.TreeCount);
		Assert.AreEqual(1, CSGManager.TreeBranchCount);
		Assert.AreEqual(1, CSGManager.TreeNodeCount);
	}

	[Test]
	public void Branch_InsertTree_ReturnsFalse()
	{
		const int branchUserID = 10;
		const int treeUserID = 11;
		var tree		= CSGTree.Create(treeUserID);
		var branch	= CSGTreeBranch.Create(branchUserID);
		CSGManager.ClearDirty(tree);
		CSGManager.ClearDirty(branch);

		var result		= branch.Insert(0, tree);

		Assert.AreEqual(false, result);
		TestUtility.ExpectValidBranchWithUserID(ref branch, branchUserID);
		TestUtility.ExpectValidTreeWithUserID(ref tree, treeUserID);
		Assert.AreEqual(false, tree.Dirty);
		Assert.AreEqual(false, branch.Dirty);
		Assert.AreEqual(0, branch.Count);
		Assert.AreEqual(0, CSGManager.TreeBrushCount);
		Assert.AreEqual(1, CSGManager.TreeCount);
		Assert.AreEqual(1, CSGManager.TreeBranchCount);
		Assert.AreEqual(2, CSGManager.TreeNodeCount);
	}

	[Test]
	public void Branch_InsertBrushTooLargeIndex_ReturnsFalse()
	{
		const int brushUserID = 10;
		const int branchUserID = 11;		
		var brush	= CSGTreeBrush.Create(userID: brushUserID);
		var branch	= CSGTreeBranch.Create(branchUserID);
		CSGManager.ClearDirty(brush);
		CSGManager.ClearDirty(branch);

		var result		= branch.Insert(1, brush);
		
		Assert.AreEqual(false, result);
		TestUtility.ExpectValidBrushWithUserID(ref brush, brushUserID);
		TestUtility.ExpectValidBranchWithUserID(ref branch, branchUserID);
		Assert.AreEqual(false, branch.Dirty);
		Assert.AreEqual(false, brush.Dirty);
		Assert.AreEqual(0, brush.Parent.NodeID);
		Assert.AreEqual(0, brush.Tree.NodeID);
		Assert.AreEqual(0, branch.Count);
		Assert.AreEqual(0, CSGManager.TreeCount);
		Assert.AreEqual(1, CSGManager.TreeBrushCount);
		Assert.AreEqual(1, CSGManager.TreeBranchCount);
		Assert.AreEqual(2, CSGManager.TreeNodeCount);
	}

	[Test]
	public void Branch_InsertBrushNegativeIndex_ReturnsFalse()
	{
		const int brushUserID = 10;
		const int branchUserID = 11;
		var brush	= CSGTreeBrush.Create(userID: brushUserID);
		var branch	= CSGTreeBranch.Create(branchUserID);
		CSGManager.ClearDirty(brush);
		CSGManager.ClearDirty(branch);

		var result = branch.Insert(-1, brush);

		Assert.AreEqual(false, result);
		TestUtility.ExpectValidBrushWithUserID(ref brush, brushUserID);
		TestUtility.ExpectValidBranchWithUserID(ref branch, branchUserID);
		Assert.AreEqual(false, brush.Dirty);
		Assert.AreEqual(false, branch.Dirty);
		Assert.AreEqual(0, brush.Parent.NodeID);
		Assert.AreEqual(0, brush.Tree.NodeID);
		Assert.AreEqual(0, branch.Count);
		Assert.AreEqual(0, CSGManager.TreeCount);
		Assert.AreEqual(1, CSGManager.TreeBrushCount);
		Assert.AreEqual(1, CSGManager.TreeBranchCount);
		Assert.AreEqual(2, CSGManager.TreeNodeCount);
	}

	[Test]
	public void Branch_InsertBrush_ContainsBrush()
	{
		const int brushUserID = 10;
		const int branchUserID = 11;		
		var brush	= CSGTreeBrush.Create(userID: brushUserID);
		var branch	= CSGTreeBranch.Create(branchUserID);
		CSGManager.ClearDirty(brush);
		CSGManager.ClearDirty(branch);

		var result		= branch.Insert(0, brush);
		
		Assert.AreEqual(true, result);
		TestUtility.ExpectValidBrushWithUserID(ref brush, brushUserID);
		TestUtility.ExpectValidBranchWithUserID(ref branch, branchUserID);
		Assert.AreEqual(true, brush.Dirty);
		Assert.AreEqual(true, branch.Dirty);
		Assert.AreEqual(branch.NodeID, brush.Parent.NodeID);
		Assert.AreEqual(0,                brush.Tree.NodeID);
		Assert.AreEqual(1, branch.Count);
		Assert.AreEqual(0, CSGManager.TreeCount);
		Assert.AreEqual(1, CSGManager.TreeBrushCount);
		Assert.AreEqual(1, CSGManager.TreeBranchCount);
		Assert.AreEqual(2, CSGManager.TreeNodeCount);
	}

	[Test]
	public void Branch_InsertBrushInMiddle_HasBrushInMiddle()
	{
		const int brushUserID1 = 10;
		const int brushUserID2 = 11;
		const int brushUserID3 = 12;
		const int branchUserID = 13;		
		var brush1	= CSGTreeBrush.Create(userID: brushUserID1);
		var brush2	= CSGTreeBrush.Create(userID: brushUserID2);
		var brush3	= CSGTreeBrush.Create(userID: brushUserID3);
		var branch	= CSGTreeBranch.Create(branchUserID, new CSGTreeNode[] { brush1, brush2 });
		CSGManager.ClearDirty(brush1);
		CSGManager.ClearDirty(brush2);
		CSGManager.ClearDirty(brush3);
		CSGManager.ClearDirty(branch);

		var result		= branch.Insert(1, brush3);
		
		Assert.AreEqual(true, result);
		TestUtility.ExpectValidBrushWithUserID(ref brush1, brushUserID1);
		TestUtility.ExpectValidBrushWithUserID(ref brush2, brushUserID2);
		TestUtility.ExpectValidBrushWithUserID(ref brush3, brushUserID3);
		TestUtility.ExpectValidBranchWithUserID(ref branch, branchUserID);
		Assert.AreEqual(true, branch.Dirty);
		Assert.AreEqual(false, brush1.Dirty);
		Assert.AreEqual(false, brush2.Dirty);
		Assert.AreEqual(true, brush3.Dirty);
		Assert.AreEqual(branch.NodeID, brush1.Parent.NodeID);
		Assert.AreEqual(0,                brush1.Tree.NodeID);
		Assert.AreEqual(branch.NodeID, brush2.Parent.NodeID);
		Assert.AreEqual(0,                brush2.Tree.NodeID);
		Assert.AreEqual(branch.NodeID, brush3.Parent.NodeID);
		Assert.AreEqual(0,                brush3.Tree.NodeID);
		Assert.AreEqual(3,                branch.Count);
		Assert.AreEqual(brush1.NodeID,    branch[0].NodeID);
		Assert.AreEqual(brush3.NodeID,    branch[1].NodeID);
		Assert.AreEqual(brush2.NodeID,    branch[2].NodeID);
		Assert.AreEqual(0, CSGManager.TreeCount);
		Assert.AreEqual(3, CSGManager.TreeBrushCount);
		Assert.AreEqual(1, CSGManager.TreeBranchCount);
		Assert.AreEqual(4, CSGManager.TreeNodeCount);
	}

	[Test]
	public void Branch_InsertBrushAtZero_HasBrushInFront()
	{
		const int brushUserID1 = 10;
		const int brushUserID2 = 11;
		const int brushUserID3 = 12;
		const int branchUserID = 13;		
		var brush1	= CSGTreeBrush.Create(userID: brushUserID1);
		var brush2	= CSGTreeBrush.Create(userID: brushUserID2);
		var brush3	= CSGTreeBrush.Create(userID: brushUserID3);
		var branch	= CSGTreeBranch.Create(branchUserID, new CSGTreeNode[] { brush1, brush2 });
		CSGManager.ClearDirty(brush1);
		CSGManager.ClearDirty(brush2);
		CSGManager.ClearDirty(brush3);
		CSGManager.ClearDirty(branch);

		var result		= branch.Insert(0, brush3);
		
		Assert.AreEqual(true, result);
		TestUtility.ExpectValidBrushWithUserID(ref brush1, brushUserID1);
		TestUtility.ExpectValidBrushWithUserID(ref brush2, brushUserID2);
		TestUtility.ExpectValidBrushWithUserID(ref brush3, brushUserID3);
		TestUtility.ExpectValidBranchWithUserID(ref branch, branchUserID);
		Assert.AreEqual(true, branch.Dirty);
		Assert.AreEqual(false, brush1.Dirty);
		Assert.AreEqual(false, brush2.Dirty);
		Assert.AreEqual(true, brush3.Dirty);
		Assert.AreEqual(branch.NodeID, brush1.Parent.NodeID);
		Assert.AreEqual(0,                brush1.Tree.NodeID);
		Assert.AreEqual(branch.NodeID, brush2.Parent.NodeID);
		Assert.AreEqual(0,                brush2.Tree.NodeID);
		Assert.AreEqual(branch.NodeID, brush3.Parent.NodeID);
		Assert.AreEqual(0,                brush3.Tree.NodeID);
		Assert.AreEqual(3,                branch.Count);
		Assert.AreEqual(brush3.NodeID,    branch[0].NodeID);
		Assert.AreEqual(brush1.NodeID,    branch[1].NodeID);
		Assert.AreEqual(brush2.NodeID,    branch[2].NodeID);
		Assert.AreEqual(0, CSGManager.TreeCount);
		Assert.AreEqual(3, CSGManager.TreeBrushCount);
		Assert.AreEqual(1, CSGManager.TreeBranchCount);
		Assert.AreEqual(4, CSGManager.TreeNodeCount);
	}

	[Test]
	public void Branch_InsertBrushAtEnd_HasBrushInBack()
	{
		const int brushUserID1 = 10;
		const int brushUserID2 = 11;
		const int brushUserID3 = 12;
		const int branchUserID = 13;
		const int treeUserID = 14;
		var brush1	= CSGTreeBrush.Create(userID: brushUserID1);
		var brush2	= CSGTreeBrush.Create(userID: brushUserID2);
		var brush3	= CSGTreeBrush.Create(userID: brushUserID3);
		var branch	= CSGTreeBranch.Create(branchUserID, new CSGTreeNode[] { brush1, brush2 });
		var tree	= CSGTree.Create(treeUserID, new CSGTreeNode[] { branch });
		CSGManager.ClearDirty(brush1);
		CSGManager.ClearDirty(brush2);
		CSGManager.ClearDirty(brush3);
		CSGManager.ClearDirty(branch);
		CSGManager.ClearDirty(tree);

		var result		= branch.Insert(2, brush3);
		
		Assert.AreEqual(true, result);
		TestUtility.ExpectValidBrushWithUserID(ref brush1, brushUserID1);
		TestUtility.ExpectValidBrushWithUserID(ref brush2, brushUserID2);
		TestUtility.ExpectValidBrushWithUserID(ref brush3, brushUserID3);
		TestUtility.ExpectValidBranchWithUserID(ref branch, branchUserID);
		Assert.AreEqual(true, tree.Dirty);
		Assert.AreEqual(true, branch.Dirty);
		Assert.AreEqual(false, brush1.Dirty);
		Assert.AreEqual(false, brush2.Dirty);
		Assert.AreEqual(true, brush3.Dirty);
		Assert.AreEqual(true, tree.IsInTree(brush1));
		Assert.AreEqual(true, tree.IsInTree(brush2));
		Assert.AreEqual(true, tree.IsInTree(brush3));
		Assert.AreEqual(3, tree.CountOfBrushesInTree);
		Assert.AreEqual(branch.NodeID, brush1.Parent.NodeID);
		Assert.AreEqual(tree.NodeID,     brush1.Tree.NodeID);
		Assert.AreEqual(branch.NodeID, brush2.Parent.NodeID);
		Assert.AreEqual(tree.NodeID,     brush2.Tree.NodeID);
		Assert.AreEqual(branch.NodeID, brush3.Parent.NodeID);
		Assert.AreEqual(tree.NodeID,     brush3.Tree.NodeID); 
		Assert.AreEqual(3,                branch.Count);
		Assert.AreEqual(brush1.NodeID,    branch[0].NodeID);
		Assert.AreEqual(brush2.NodeID,    branch[1].NodeID);
		Assert.AreEqual(brush3.NodeID,    branch[2].NodeID);
		Assert.AreEqual(1, CSGManager.TreeCount);
		Assert.AreEqual(3, CSGManager.TreeBrushCount);
		Assert.AreEqual(1, CSGManager.TreeBranchCount);
		Assert.AreEqual(5, CSGManager.TreeNodeCount);
	}


	[Test]
	public void Branch_InsertChildBrushOfOtherBranch_MovesBrushToBranch()
	{
		const int brushUserID = 10;
		const int treeUserID1 = 11;
		const int treeUserID2 = 12;
		const int branchUserID1 = 13;
		const int branchUserID2 = 14;
		var brush	= CSGTreeBrush.Create(userID: brushUserID);
		var branch2	= CSGTreeBranch.Create(branchUserID2);
		var tree2	= CSGTree.Create(treeUserID2, new CSGTreeNode[] { branch2 });
		var branch1	= CSGTreeBranch.Create(branchUserID1, new CSGTreeNode[] { brush });
		var tree1	= CSGTree.Create(treeUserID1, new CSGTreeNode[] { branch1 });
		CSGManager.ClearDirty(brush);
		CSGManager.ClearDirty(branch1);
		CSGManager.ClearDirty(branch2);
		CSGManager.ClearDirty(tree1);
		CSGManager.ClearDirty(tree2);

		var result = branch2.Insert(0, brush);

		Assert.AreEqual(true, result);
		TestUtility.ExpectValidBrushWithUserID(ref brush, brushUserID);
		TestUtility.ExpectValidBranchWithUserID(ref branch1, branchUserID1);
		TestUtility.ExpectValidBranchWithUserID(ref branch2, branchUserID2);
		TestUtility.ExpectValidTreeWithUserID(ref tree1, treeUserID1);
		TestUtility.ExpectValidTreeWithUserID(ref tree2, treeUserID2);
		Assert.AreEqual(true, brush.Dirty);
		Assert.AreEqual(true, branch1.Dirty);
		Assert.AreEqual(true, branch2.Dirty);
		Assert.AreEqual(true, tree1.Dirty);
		Assert.AreEqual(true, tree2.Dirty);
		Assert.AreEqual(false, tree1.IsInTree(brush));
		Assert.AreEqual(true, tree2.IsInTree(brush));
		Assert.AreEqual(0, tree1.CountOfBrushesInTree);
		Assert.AreEqual(1, tree2.CountOfBrushesInTree);
		Assert.AreEqual(tree2.NodeID,     brush.Tree.NodeID);
		Assert.AreEqual(branch2.NodeID, brush.Parent.NodeID);
		Assert.AreEqual(tree1.NodeID,     branch1.Tree.NodeID);
		Assert.AreEqual(0,                 branch1.Parent.NodeID);
		Assert.AreEqual(0,                 branch1.Count);
		Assert.AreEqual(tree2.NodeID,     branch2.Tree.NodeID);
		Assert.AreEqual(0,                 branch2.Parent.NodeID);
		Assert.AreEqual(1,                 branch2.Count);
		Assert.AreEqual(2, CSGManager.TreeCount);
		Assert.AreEqual(1, CSGManager.TreeBrushCount);
		Assert.AreEqual(2, CSGManager.TreeBranchCount);
		Assert.AreEqual(5, CSGManager.TreeNodeCount);
	} 


	[Test]
	public void Tree_InsertChildBrushOfOtherTree_MovesBrushToTree()
	{
		const int brushUserID = 10;
		const int treeUserID1 = 11;
		const int treeUserID2 = 12;
		var brush		= CSGTreeBrush.Create(userID: brushUserID);
		var tree2		= CSGTree.Create(treeUserID2);
		var tree1		= CSGTree.Create(treeUserID1, new CSGTreeNode[] { brush });
		CSGManager.ClearDirty(brush);
		CSGManager.ClearDirty(tree1);
		CSGManager.ClearDirty(tree2);

		var result = tree2.Insert(0, brush);

		Assert.AreEqual(true, result); 
		TestUtility.ExpectValidBrushWithUserID(ref brush, brushUserID);
		TestUtility.ExpectValidTreeWithUserID(ref tree1, treeUserID1);
		TestUtility.ExpectValidTreeWithUserID(ref tree2, treeUserID2);
		Assert.AreEqual(true, brush.Dirty);
		Assert.AreEqual(true, tree1.Dirty);
		Assert.AreEqual(true, tree2.Dirty);
		Assert.AreEqual(false, tree1.IsInTree(brush));
		Assert.AreEqual(true, tree2.IsInTree(brush));
		Assert.AreEqual(0, tree1.CountOfBrushesInTree);
		Assert.AreEqual(1, tree2.CountOfBrushesInTree);
		Assert.AreEqual(tree2.NodeID,     brush.Tree.NodeID);
		Assert.AreEqual(0,                 brush.Parent.NodeID);
		Assert.AreEqual(0,                 tree1.Count);
		Assert.AreEqual(1,                 tree2.Count);
		Assert.AreEqual(2, CSGManager.TreeCount);
		Assert.AreEqual(1, CSGManager.TreeBrushCount);
		Assert.AreEqual(0, CSGManager.TreeBranchCount);
		Assert.AreEqual(3, CSGManager.TreeNodeCount);
	} 
	

	[Test]
	public void Branch_InsertChildBrushOfOtherTree_MovesBrushToTree()
	{
		const int brushUserID = 10;
		const int treeUserID1 = 11;
		const int treeUserID2 = 12;
		const int branchUserID1 = 13;
		var brush	= CSGTreeBrush.Create(userID: brushUserID);
		var tree2	= CSGTree.Create(treeUserID2);
		var branch1	= CSGTreeBranch.Create(branchUserID1, new CSGTreeNode[] { brush });
		var tree1	= CSGTree.Create(treeUserID1, new CSGTreeNode[] { branch1 });
		CSGManager.ClearDirty(brush);
		CSGManager.ClearDirty(branch1);
		CSGManager.ClearDirty(tree1);
		CSGManager.ClearDirty(tree2);

		var result = tree2.Insert(0, brush);

		Assert.AreEqual(true, result);
		TestUtility.ExpectValidBrushWithUserID(ref brush, brushUserID);
		TestUtility.ExpectValidBranchWithUserID(ref branch1, branchUserID1);
		TestUtility.ExpectValidTreeWithUserID(ref tree1, treeUserID1);
		TestUtility.ExpectValidTreeWithUserID(ref tree2, treeUserID2);
		Assert.AreEqual(true, brush.Dirty);
		Assert.AreEqual(true, branch1.Dirty);
		Assert.AreEqual(true, tree1.Dirty);
		Assert.AreEqual(true, tree2.Dirty);
		Assert.AreEqual(false, tree1.IsInTree(brush));
		Assert.AreEqual(true, tree2.IsInTree(brush));
		Assert.AreEqual(0, tree1.CountOfBrushesInTree);
		Assert.AreEqual(1, tree2.CountOfBrushesInTree);
		Assert.AreEqual(tree2.NodeID,     brush.Tree.NodeID);
		Assert.AreEqual(0,                 brush.Parent.NodeID);
		Assert.AreEqual(tree1.NodeID,     branch1.Tree.NodeID);
		Assert.AreEqual(0,                 branch1.Parent.NodeID);
		Assert.AreEqual(0,                 branch1.Count);
		Assert.AreEqual(1,                 tree2.Count);
		Assert.AreEqual(2, CSGManager.TreeCount);
		Assert.AreEqual(1, CSGManager.TreeBrushCount);
		Assert.AreEqual(1, CSGManager.TreeBranchCount);
		Assert.AreEqual(4, CSGManager.TreeNodeCount);
	} 


	[Test]
	public void Tree_InsertChildBrushOfOtherBranch_MovesBrushToBranch()
	{
		const int brushUserID = 10;
		const int treeUserID1 = 11;
		const int treeUserID2 = 12;
		const int branchUserID2 = 14;
		var brush	= CSGTreeBrush.Create(userID: brushUserID);
		var branch2	= CSGTreeBranch.Create(branchUserID2);
		var tree2	= CSGTree.Create(treeUserID2, new CSGTreeNode[] { branch2 });
		var tree1	= CSGTree.Create(treeUserID1, new CSGTreeNode[] { brush });
		CSGManager.ClearDirty(brush);
		CSGManager.ClearDirty(branch2);
		CSGManager.ClearDirty(tree1);
		CSGManager.ClearDirty(tree2);

		var result = branch2.Insert(0, brush);

		Assert.AreEqual(true, result);
		TestUtility.ExpectValidBrushWithUserID(ref brush, brushUserID);
		TestUtility.ExpectValidBranchWithUserID(ref branch2, branchUserID2);
		TestUtility.ExpectValidTreeWithUserID(ref tree1, treeUserID1);
		TestUtility.ExpectValidTreeWithUserID(ref tree2, treeUserID2);
		Assert.AreEqual(true, brush.Dirty);
		Assert.AreEqual(true, branch2.Dirty);
		Assert.AreEqual(true, tree1.Dirty);
		Assert.AreEqual(true, tree2.Dirty);
		Assert.AreEqual(false, tree1.IsInTree(brush));
		Assert.AreEqual(true, tree2.IsInTree(brush));
		Assert.AreEqual(0, tree1.CountOfBrushesInTree);
		Assert.AreEqual(1, tree2.CountOfBrushesInTree);
		Assert.AreEqual(tree2.NodeID,     brush.Tree.NodeID);
		Assert.AreEqual(branch2.NodeID, brush.Parent.NodeID);
		Assert.AreEqual(0,                 tree1.Count);
		Assert.AreEqual(tree2.NodeID,     branch2.Tree.NodeID);
		Assert.AreEqual(0,                 branch2.Parent.NodeID);
		Assert.AreEqual(1,                 branch2.Count);
		Assert.AreEqual(2, CSGManager.TreeCount);
		Assert.AreEqual(1, CSGManager.TreeBrushCount);
		Assert.AreEqual(1, CSGManager.TreeBranchCount);
		Assert.AreEqual(4, CSGManager.TreeNodeCount);
	} 

}