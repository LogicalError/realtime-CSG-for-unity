using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using RealtimeCSG;
using RealtimeCSG.Foundation;
using UnityEditor.SceneManagement;

[TestFixture]
public partial class ClearTests
{
	[SetUp]
	public void Init()
	{
		CSGManager.Clear();
	}

	/*
	[Test]
	public void InvalidNode_Clear_ReturnsFalse()
	{
		var invalidNode = CSGTreeNode.InvalidNode;

		var result = invalidNode.Clear();

		Assert.AreEqual(false, result);
		Assert.AreEqual(0, CSGUtility.TreeBrushCount);
		Assert.AreEqual(0, CSGUtility.TreeCount);
		Assert.AreEqual(0, CSGUtility.TreeBranchCount);
		Assert.AreEqual(0, CSGUtility.TreeNodeCount);
	}
	*/


	[Test]
	public void TreeWithoutChildren_Clear_TreeIsEmpty()
	{
		const int treeUserID = 11;
		var tree	= CSGTree.Create(treeUserID);
		CSGManager.ClearDirty(tree);

		tree.Clear();
		
		Assert.AreEqual(false, tree.Dirty);
		Assert.AreEqual(0, tree.Count);
		Assert.AreEqual(0, CSGManager.TreeBrushCount);
		Assert.AreEqual(0, CSGManager.TreeBranchCount);
		Assert.AreEqual(1, CSGManager.TreeCount);
		Assert.AreEqual(1, CSGManager.TreeNodeCount);
	}
	

	[Test]
	public void BranchWithoutChildren_Clear_BranchIsEmpty()
	{
		const int branchUserID = 10;
		var branch	= CSGTreeBranch.Create(branchUserID);
		CSGManager.ClearDirty(branch);

		branch.Clear();
		
		Assert.AreEqual(false, branch.Dirty);
		Assert.AreEqual(0, branch.Parent.NodeID);
		Assert.AreEqual(0, branch.Tree.NodeID);
		Assert.AreEqual(0, branch.Count);
		Assert.AreEqual(0, CSGManager.TreeBrushCount);
		Assert.AreEqual(0, CSGManager.TreeCount);
		Assert.AreEqual(1, CSGManager.TreeBranchCount);
		Assert.AreEqual(1, CSGManager.TreeNodeCount);
	}

	[Test]
	public void TreeWithChildBranch_Clear_TreeIsEmpty()
	{
		const int branchUserID = 10;
		const int treeUserID = 11;
		var branch	= CSGTreeBranch.Create(branchUserID);
		var tree		= CSGTree.Create(treeUserID, new CSGTreeNode[] { branch });
		CSGManager.ClearDirty(branch);
		CSGManager.ClearDirty(tree);

		tree.Clear();
		
		TestUtility.ExpectValidBranchWithUserID(ref branch, branchUserID);
		Assert.AreEqual(true, branch.Dirty);
		Assert.AreEqual(true, tree.Dirty);
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
	public void TreeWithChildBrush_Clear_TreeIsEmpty()
	{
		const int brushUserID = 10;
		const int treeUserID = 11;
		var brush	= CSGTreeBrush.Create(userID: brushUserID);
		var tree	= CSGTree.Create(treeUserID, new CSGTreeNode[] { brush });
		CSGManager.ClearDirty(brush);
		CSGManager.ClearDirty(tree);

		tree.Clear();
		
		TestUtility.ExpectValidBrushWithUserID(ref brush, brushUserID);
		Assert.AreEqual(true, tree.Dirty);
		Assert.AreEqual(true, brush.Dirty);
		Assert.AreEqual(0, brush.Parent.NodeID);
		Assert.AreEqual(0, brush.Tree.NodeID);
		Assert.AreEqual(0, tree.Count);
		Assert.AreEqual(0, CSGManager.TreeBranchCount);
		Assert.AreEqual(1, CSGManager.TreeBrushCount);
		Assert.AreEqual(1, CSGManager.TreeCount);
		Assert.AreEqual(2, CSGManager.TreeNodeCount);
	}


	[Test]
	public void BranchWithChildBranch_Clear_BranchIsEmpty()
	{
		const int branchUserID1 = 10;
		const int branchUserID2 = 11;
		var branch1	= CSGTreeBranch.Create(branchUserID1);
		var branch2	= CSGTreeBranch.Create(branchUserID2, new CSGTreeNode[] { branch1 });
		CSGManager.ClearDirty(branch1);
		CSGManager.ClearDirty(branch2);

		branch2.Clear();
		
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


	[Test]
	public void BranchWithChildBrush_Clear_BranchIsEmpty()
	{
		const int brushUserID = 10;
		const int branchUserID = 11;
		var brush	= CSGTreeBrush.Create(userID: brushUserID);
		var branch	= CSGTreeBranch.Create(branchUserID, new CSGTreeNode[] { brush });
		CSGManager.ClearDirty(brush);
		CSGManager.ClearDirty(branch);

		branch.Clear();
		
		TestUtility.ExpectValidBrushWithUserID(ref brush, brushUserID);
		Assert.AreEqual(true, branch.Dirty);
		Assert.AreEqual(true, brush.Dirty);
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
	


}
