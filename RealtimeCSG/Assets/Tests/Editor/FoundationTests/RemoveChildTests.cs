using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using RealtimeCSG;
using RealtimeCSG.Foundation;
using UnityEditor.SceneManagement;

[TestFixture]
public partial class RemoveChildTests
{
	[SetUp]
	public void Init()
	{
		CSGManager.Clear();
	}

	[Test]
	public void Tree_RemoveInvalidNode_ReturnsFalse()
	{
		const int treeUserID = 11;
		var tree = CSGTree.Create(treeUserID);
		CSGManager.ClearDirty(tree);

		var result = tree.Remove(CSGTreeNode.InvalidNode);

		Assert.AreEqual(false, result);
		Assert.AreEqual(false, tree.Dirty);
		Assert.AreEqual(0, tree.Count);
		Assert.AreEqual(0, CSGManager.TreeBranchCount);
		Assert.AreEqual(0, CSGManager.TreeBrushCount);
		Assert.AreEqual(1, CSGManager.TreeCount);
		Assert.AreEqual(1, CSGManager.TreeNodeCount);
	}

	[Test]
	public void Tree_RemoveUnknownBrush_ReturnsFalse()
	{
		const int brushUserID = 10;
		const int treeUserID = 11;
		var brush	= CSGTreeBrush.Create(userID: brushUserID);
		var tree	= CSGTree.Create(treeUserID);
		CSGManager.ClearDirty(brush);
		CSGManager.ClearDirty(tree);

		var result = tree.Remove(brush);

		Assert.AreEqual(false, result);
		TestUtility.ExpectValidBrushWithUserID(ref brush, brushUserID);
		Assert.AreEqual(false, tree.IsInTree(brush));
		Assert.AreEqual(0, tree.CountOfBrushesInTree);
		Assert.AreEqual(false, tree.Dirty);
		Assert.AreEqual(false, brush.Dirty);
		Assert.AreEqual(0, brush.Parent.NodeID);
		Assert.AreEqual(0, brush.Tree.NodeID);
		Assert.AreEqual(0, tree.Count);
		Assert.AreEqual(0, CSGManager.TreeBranchCount);
		Assert.AreEqual(1, CSGManager.TreeBrushCount);
		Assert.AreEqual(1, CSGManager.TreeCount);
		Assert.AreEqual(2, CSGManager.TreeNodeCount);
	}

	[Test]
	public void Branch_RemoveInvalidNode_ReturnsFalse()
	{
		const int branchUserID = 11;
		var branch = CSGTreeBranch.Create(branchUserID);
		CSGManager.ClearDirty(branch);

		var result = branch.Remove(CSGTreeNode.InvalidNode);

		Assert.AreEqual(false, result);
		Assert.AreEqual(false, branch.Dirty);
		Assert.AreEqual(0, branch.Parent.NodeID);
		Assert.AreEqual(0, branch.Tree.NodeID);
		Assert.AreEqual(0, branch.Count);
		Assert.AreEqual(0, CSGManager.TreeCount);
		Assert.AreEqual(0, CSGManager.TreeBrushCount);
		Assert.AreEqual(1, CSGManager.TreeBranchCount);
		Assert.AreEqual(1, CSGManager.TreeNodeCount);
	}

	[Test]
	public void Branch_RemoveUnknownBrush_ReturnsFalse()
	{
		const int brushUserID = 10;
		const int branchUserID = 11;
		var brush	= CSGTreeBrush.Create(userID: brushUserID);
		var branch	= CSGTreeBranch.Create(branchUserID);
		CSGManager.ClearDirty(brush);
		CSGManager.ClearDirty(branch);

		var result = branch.Remove(brush);

		Assert.AreEqual(false, result);
		TestUtility.ExpectValidBrushWithUserID(ref brush, brushUserID);
		Assert.AreEqual(false, brush.Dirty);
		Assert.AreEqual(false, branch.Dirty);
		Assert.AreEqual(0, brush.Parent.NodeID);
		Assert.AreEqual(0, brush.Tree.NodeID);
		Assert.AreEqual(0, branch.Parent.NodeID);
		Assert.AreEqual(0, branch.Tree.NodeID);
		Assert.AreEqual(0, branch.Count);
		Assert.AreEqual(0, CSGManager.TreeCount);
		Assert.AreEqual(1, CSGManager.TreeBrushCount);
		Assert.AreEqual(1, CSGManager.TreeBranchCount);
		Assert.AreEqual(2, CSGManager.TreeNodeCount);
	}


	[Test]
	public void TreeWithChildBrush_RemoveBrush_IsEmpty()
	{
		const int brushUserID = 10;
		const int treeUserID = 11;
		var brush	= CSGTreeBrush.Create(userID: brushUserID);
		var tree	= CSGTree.Create(treeUserID, brush);
		CSGManager.ClearDirty(brush);
		CSGManager.ClearDirty(tree);

		var result	= tree.Remove(brush);

		Assert.AreEqual(true, result);
		TestUtility.ExpectValidBrushWithUserID(ref brush, brushUserID);
		Assert.AreEqual(true, brush.Dirty);
		Assert.AreEqual(true, tree.Dirty);
		Assert.AreEqual(false, tree.IsInTree(brush));
		Assert.AreEqual(0, tree.CountOfBrushesInTree);
		Assert.AreEqual(0, brush.Parent.NodeID);
		Assert.AreEqual(0, brush.Tree.NodeID);
		Assert.AreEqual(0, tree.Count);
		Assert.AreEqual(0, CSGManager.TreeBranchCount);
		Assert.AreEqual(1, CSGManager.TreeBrushCount);
		Assert.AreEqual(1, CSGManager.TreeCount);
		Assert.AreEqual(2, CSGManager.TreeNodeCount);
	}


	[Test]
	public void TreeWithChildBrush_RemoveSameBrushTwice_ReturnsFalse()
	{
		const int brushUserID = 10;
		const int treeUserID = 11;
		var brush	= CSGTreeBrush.Create(userID: brushUserID);
		var tree	= CSGTree.Create(treeUserID, new CSGTreeNode[] { brush });
		CSGManager.ClearDirty(brush);
		CSGManager.ClearDirty(tree);

		var result1 = tree.Remove(brush);
		var result2 = tree.Remove(brush);

		Assert.AreEqual(true, result1);
		Assert.AreEqual(false, result2);
		TestUtility.ExpectValidBrushWithUserID(ref brush, brushUserID);
		Assert.AreEqual(true, brush.Dirty);
		Assert.AreEqual(true, tree.Dirty);
		Assert.AreEqual(false, tree.IsInTree(brush));
		Assert.AreEqual(0, tree.CountOfBrushesInTree);
		Assert.AreEqual(0, brush.Parent.NodeID);
		Assert.AreEqual(0, brush.Tree.NodeID);
		Assert.AreEqual(0, tree.Count);
		Assert.AreEqual(0, CSGManager.TreeBranchCount);
		Assert.AreEqual(1, CSGManager.TreeBrushCount);
		Assert.AreEqual(1, CSGManager.TreeCount);
		Assert.AreEqual(2, CSGManager.TreeNodeCount);
	}


	[Test]
	public void TreeWithChildBranch_RemoveBranch_IsEmpty()
	{
		const int branchUserID	= 10;
		const int treeUserID		= 11;
		var branch	= CSGTreeBranch.Create(branchUserID);
		var tree		= CSGTree.Create(treeUserID, new CSGTreeNode[] { branch });
		CSGManager.ClearDirty(branch);
		CSGManager.ClearDirty(tree);

		var result		= tree.Remove(branch);

		Assert.AreEqual(true, result);
		TestUtility.ExpectValidBranchWithUserID(ref branch, branchUserID);
		Assert.AreEqual(true, tree.Dirty);
		Assert.AreEqual(true, branch.Dirty);
		Assert.AreEqual(0, branch.Parent.NodeID);
		Assert.AreEqual(0, branch.Tree.NodeID);
		Assert.AreEqual(0, branch.Count);
		Assert.AreEqual(0, tree.Count);
		Assert.AreEqual(0, CSGManager.TreeBrushCount);
		Assert.AreEqual(1, CSGManager.TreeBranchCount);
		Assert.AreEqual(1, CSGManager.TreeCount);
		Assert.AreEqual(2, CSGManager.TreeNodeCount);
	}
	
	[Test]
	public void BranchWithChildBrush_RemoveBrush_IsEmpty()
	{
		const int brushUserID = 10;
		const int branchUserID = 11;
		var brush	= CSGTreeBrush.Create(userID: brushUserID);
		var branch	= CSGTreeBranch.Create(branchUserID, new CSGTreeNode[] { brush });
		CSGManager.ClearDirty(brush);
		CSGManager.ClearDirty(branch);

		var result		= branch.Remove(brush);

		Assert.AreEqual(true, result);
		TestUtility.ExpectValidBrushWithUserID(ref brush, brushUserID);
		Assert.AreEqual(true, brush.Dirty);
		Assert.AreEqual(true, branch.Dirty);
		Assert.AreEqual(0, brush.Parent.NodeID);
		Assert.AreEqual(0, brush.Tree.NodeID);
		Assert.AreEqual(0, branch.Parent.NodeID);
		Assert.AreEqual(0, branch.Tree.NodeID);
		Assert.AreEqual(0, branch.Count);
		Assert.AreEqual(0, CSGManager.TreeCount);
		Assert.AreEqual(1, CSGManager.TreeBrushCount);
		Assert.AreEqual(1, CSGManager.TreeBranchCount);
		Assert.AreEqual(2, CSGManager.TreeNodeCount);
	}

	[Test]
	public void BranchWithChildBrush_RemoveSameBrushTwice_ReturnsFalse()
	{
		const int brushUserID = 10;
		const int branchUserID = 11;
		var brush	= CSGTreeBrush.Create(userID: brushUserID);
		var branch	= CSGTreeBranch.Create(branchUserID, new CSGTreeNode[] { brush });
		CSGManager.ClearDirty(brush);
		CSGManager.ClearDirty(branch);

		var result1		= branch.Remove(brush);
		var result2		= branch.Remove(brush);

		Assert.AreEqual(true, result1);
		Assert.AreEqual(false, result2);
		TestUtility.ExpectValidBrushWithUserID(ref brush, brushUserID);
		Assert.AreEqual(true, brush.Dirty);
		Assert.AreEqual(true, branch.Dirty);
		Assert.AreEqual(0, brush.Parent.NodeID);
		Assert.AreEqual(0, brush.Tree.NodeID);
		Assert.AreEqual(0, branch.Parent.NodeID);
		Assert.AreEqual(0, branch.Tree.NodeID);
		Assert.AreEqual(0, branch.Count);
		Assert.AreEqual(0, CSGManager.TreeCount);
		Assert.AreEqual(1, CSGManager.TreeBrushCount);
		Assert.AreEqual(1, CSGManager.TreeBranchCount);
		Assert.AreEqual(2, CSGManager.TreeNodeCount);
	}

	[Test]
	public void BranchWithChildBrushes_RemoveMiddleBrush_OtherBrushesRemain()
	{
		const int brushUserID1 = 10;
		const int brushUserID2 = 11;
		const int brushUserID3 = 12;
		const int branchUserID = 13;
		var brush1	= CSGTreeBrush.Create(userID: brushUserID1);
		var brush2	= CSGTreeBrush.Create(userID: brushUserID2);
		var brush3	= CSGTreeBrush.Create(userID: brushUserID3);
		var branch	= CSGTreeBranch.Create(branchUserID, new CSGTreeNode[] { brush1, brush2, brush3 });
		CSGManager.ClearDirty(brush1);
		CSGManager.ClearDirty(brush2);
		CSGManager.ClearDirty(brush3);
		CSGManager.ClearDirty(branch);

		var result		= branch.Remove(brush2);

		Assert.AreEqual(true, result);
		TestUtility.ExpectValidBrushWithUserID(ref brush1, brushUserID1);
		TestUtility.ExpectValidBrushWithUserID(ref brush2, brushUserID2);
		TestUtility.ExpectValidBrushWithUserID(ref brush3, brushUserID3);
		Assert.AreEqual(false, brush1.Dirty);
		Assert.AreEqual(true, brush2.Dirty);
		Assert.AreEqual(false, brush3.Dirty);
		Assert.AreEqual(true, branch.Dirty);
		Assert.AreEqual(branch.NodeID, brush1.Parent.NodeID);
		Assert.AreEqual(0,                brush1.Tree.NodeID);
		Assert.AreEqual(branch.NodeID, brush3.Parent.NodeID);
		Assert.AreEqual(0,                brush3.Tree.NodeID);
		Assert.AreEqual(0,                brush2.Parent.NodeID);
		Assert.AreEqual(0,                brush2.Tree.NodeID);
		Assert.AreEqual(0,                branch.Parent.NodeID);
		Assert.AreEqual(0,                branch.Tree.NodeID);
		Assert.AreEqual(2,                branch.Count);
		Assert.AreEqual(brush1.NodeID,    branch[0].NodeID);
		Assert.AreEqual(brush3.NodeID,    branch[1].NodeID);
		Assert.AreEqual(0, CSGManager.TreeCount);
		Assert.AreEqual(3, CSGManager.TreeBrushCount);
		Assert.AreEqual(1, CSGManager.TreeBranchCount);
		Assert.AreEqual(4, CSGManager.TreeNodeCount);
	}
	
	[Test]
	public void BranchWithChildBranch_RemoveBranch_IsEmpty()
	{
		const int branchUserID1 = 10;
		const int branchUserID2 = 11;
		var branch1	= CSGTreeBranch.Create(branchUserID1);
		var branch2	= CSGTreeBranch.Create(branchUserID2, new CSGTreeNode[] { branch1 });
		CSGManager.ClearDirty(branch1);
		CSGManager.ClearDirty(branch2);

		var result		= branch2.Remove(branch1);

		Assert.AreEqual(true, result);
		TestUtility.ExpectValidBranchWithUserID(ref branch1, branchUserID1);
		Assert.AreEqual(true, branch1.Dirty);
		Assert.AreEqual(true, branch2.Dirty);
		Assert.AreEqual(0, branch1.Parent.NodeID);
		Assert.AreEqual(0, branch1.Tree.NodeID);
		Assert.AreEqual(0, branch1.Count);
		Assert.AreEqual(0, branch2.Parent.NodeID);
		Assert.AreEqual(0, branch2.Tree.NodeID);
		Assert.AreEqual(0, branch2.Count);
		Assert.AreEqual(0, CSGManager.TreeBrushCount);
		Assert.AreEqual(0, CSGManager.TreeCount);
		Assert.AreEqual(2, CSGManager.TreeBranchCount);
		Assert.AreEqual(2, CSGManager.TreeNodeCount);
	}
}
