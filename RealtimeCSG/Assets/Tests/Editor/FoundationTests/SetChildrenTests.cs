using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using RealtimeCSG;
using RealtimeCSG.Foundation;
using UnityEditor.SceneManagement;

[TestFixture]
public partial class SetChildrenTests
{
	[SetUp]
	public void Init()
	{
		CSGManager.Clear();
	}

	[Test]
	public void Branch_SetChildrenWithAncestore_DoesNotContainAncestor()
	{
		const int branchUserID1 = 10;
		const int branchUserID2 = 11;
		const int branchUserID3 = 12;
		var branch1 = CSGTreeBranch.Create(branchUserID1);
		var branch2 = CSGTreeBranch.Create(branchUserID2);
		var branch3 = CSGTreeBranch.Create(branchUserID3);
		branch1.Add(branch2);
		branch2.Add(branch3);
		CSGManager.ClearDirty(branch1);
		CSGManager.ClearDirty(branch2);
		CSGManager.ClearDirty(branch3);

		branch3.InsertRange(0, new CSGTreeNode[] { branch1 });

		TestUtility.ExpectValidBranchWithUserID(ref branch1, branchUserID1);
		TestUtility.ExpectValidBranchWithUserID(ref branch2, branchUserID2);
		TestUtility.ExpectValidBranchWithUserID(ref branch3, branchUserID3);
		Assert.AreEqual(false, branch1.Dirty);
		Assert.AreEqual(false, branch2.Dirty);
		Assert.AreEqual(false, branch3.Dirty);
		Assert.AreEqual(branch2.NodeID, branch3.Parent.NodeID);
		Assert.AreEqual(0,                 branch3.Tree.NodeID);
		Assert.AreEqual(0,                 branch3.Count);
		Assert.AreEqual(branch1.NodeID, branch2.Parent.NodeID);
		Assert.AreEqual(0,				   branch2.Tree.NodeID);
		Assert.AreEqual(1,                 branch2.Count);
		Assert.AreEqual(0,				   branch1.Parent.NodeID);
		Assert.AreEqual(0,				   branch1.Tree.NodeID);
		Assert.AreEqual(1,                 branch1.Count);
		Assert.AreEqual(0, CSGManager.TreeBrushCount);
		Assert.AreEqual(0, CSGManager.TreeCount);
		Assert.AreEqual(3, CSGManager.TreeBranchCount);
		Assert.AreEqual(3, CSGManager.TreeNodeCount);
	}


	[Test]
	public void Branch_SetChildrenWithBrush_ContainsBrush()
	{
		const int brushUserID = 10;
		const int branchUserID = 11;		
		var brush	= CSGTreeBrush.Create(userID: brushUserID);
		var branch	= CSGTreeBranch.Create(branchUserID);
		CSGManager.ClearDirty(brush);
		CSGManager.ClearDirty(branch);

		branch.InsertRange(0, new CSGTreeNode[] { brush });
		
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
	public void Branch_SetChildrenWithTree_DoesNotContainTree()
	{
		const int treeUserID = 10;
		const int branchUserID = 11;
		var tree      = CSGTree.Create(treeUserID);
		var branch = CSGTreeBranch.Create(branchUserID);
		CSGManager.ClearDirty(branch);
		CSGManager.ClearDirty(tree);

		branch.InsertRange(0, new CSGTreeNode[] { tree });

		TestUtility.ExpectValidTreeWithUserID(ref tree, treeUserID);
		TestUtility.ExpectValidBranchWithUserID(ref branch, branchUserID);
		Assert.AreEqual(false, branch.Dirty);
		Assert.AreEqual(false, tree.Dirty);
		Assert.AreEqual(0, tree.Count);
		Assert.AreEqual(0, branch.Count);
		Assert.AreEqual(0, CSGManager.TreeBrushCount);
		Assert.AreEqual(1, CSGManager.TreeCount);
		Assert.AreEqual(1, CSGManager.TreeBranchCount);
		Assert.AreEqual(2, CSGManager.TreeNodeCount);
	}


	[Test]
	public void Tree_SetChildrenWithTree_DoesNotContainTree()
	{
		const int treeUserID1 = 10;
		const int treeUserID2 = 11;
		var tree1 = CSGTree.Create(treeUserID1);
		var tree2 = CSGTree.Create(treeUserID2);
		CSGManager.ClearDirty(tree1);
		CSGManager.ClearDirty(tree2);

		tree2.InsertRange(0, new CSGTreeNode[] { tree1 });

		TestUtility.ExpectValidTreeWithUserID(ref tree1, treeUserID1);
		TestUtility.ExpectValidTreeWithUserID(ref tree2, treeUserID2);
		Assert.AreEqual(false, tree1.Dirty);
		Assert.AreEqual(false, tree2.Dirty);
		Assert.AreEqual(0, tree1.Count);
		Assert.AreEqual(0, tree2.Count);
		Assert.AreEqual(0, CSGManager.TreeBrushCount);
		Assert.AreEqual(0, CSGManager.TreeBranchCount);
		Assert.AreEqual(2, CSGManager.TreeCount);
		Assert.AreEqual(2, CSGManager.TreeNodeCount);
	}

	[Test]
	public void Branch_SetChildrenWithSelf_DoesNotContainSelf()
	{
		const int branchUserID = 11;
		var branch = CSGTreeBranch.Create(branchUserID);
		CSGManager.ClearDirty(branch);

		branch.InsertRange(0, new CSGTreeNode[] { branch });
		
		TestUtility.ExpectValidBranchWithUserID(ref branch, branchUserID);
		Assert.AreEqual(false, branch.Dirty);
		Assert.AreEqual(0, branch.Count);
		Assert.AreEqual(0, CSGManager.TreeBrushCount);
		Assert.AreEqual(0, CSGManager.TreeCount);
		Assert.AreEqual(1, CSGManager.TreeBranchCount);
		Assert.AreEqual(1, CSGManager.TreeNodeCount);
	}


	[Test]
	public void Tree_SetChildrenWithBrush_ContainsBrush()
	{
		const int brushUserID = 10;
		const int treeUserID = 11;
		var brush = CSGTreeBrush.Create(userID: brushUserID);
		var tree = CSGTree.Create(treeUserID);
		CSGManager.ClearDirty(brush);
		CSGManager.ClearDirty(tree);

		tree.InsertRange(0, new CSGTreeNode[] { brush });

		TestUtility.ExpectValidBrushWithUserID(ref brush, brushUserID);
		TestUtility.ExpectValidTreeWithUserID(ref tree, treeUserID);
		Assert.AreEqual(true, brush.Dirty);
		Assert.AreEqual(true, tree.Dirty);
		Assert.AreEqual(true, tree.IsInTree(brush));
		Assert.AreEqual(1, tree.CountOfBrushesInTree);
		Assert.AreEqual(0, brush.Parent.NodeID);
		Assert.AreEqual(tree.NodeID, brush.Tree.NodeID);
		Assert.AreEqual(1, tree.Count);
		Assert.AreEqual(0, CSGManager.TreeBranchCount);
		Assert.AreEqual(1, CSGManager.TreeBrushCount);
		Assert.AreEqual(1, CSGManager.TreeCount);
		Assert.AreEqual(2, CSGManager.TreeNodeCount);
	}


	[Test]
	public void Tree_SetChildrenWithSelf_DoesNotContainsSelf()
	{
		const int treeUserID = 11;
		var tree = CSGTree.Create(treeUserID);
		CSGManager.ClearDirty(tree);

		tree.InsertRange(0, new CSGTreeNode[] { tree });
		
		TestUtility.ExpectValidTreeWithUserID(ref tree, treeUserID);
		Assert.AreEqual(false, tree.Dirty);
		Assert.AreEqual(0, tree.Count);
		Assert.AreEqual(0, CSGManager.TreeBranchCount);
		Assert.AreEqual(0, CSGManager.TreeBrushCount);
		Assert.AreEqual(1, CSGManager.TreeCount);
		Assert.AreEqual(1, CSGManager.TreeNodeCount);
	}

	[Test]
	public void Branch_SetChildrenWithBranch_ContainsBranch()
	{
		const int branchUserID1 = 10;
		const int branchUserID2 = 11;		
		var branch1	= CSGTreeBranch.Create(branchUserID1);
		var branch2	= CSGTreeBranch.Create(branchUserID2);
		CSGManager.ClearDirty(branch1);
		CSGManager.ClearDirty(branch2);

		branch2.InsertRange(0, new CSGTreeNode[] { branch1 });
		
		TestUtility.ExpectValidBranchWithUserID(ref branch1, branchUserID1);
		TestUtility.ExpectValidBranchWithUserID(ref branch2, branchUserID2);
		Assert.AreEqual(true, branch1.Dirty);
		Assert.AreEqual(true, branch2.Dirty);
		Assert.AreEqual(branch2.NodeID, branch1.Parent.NodeID);
		Assert.AreEqual(0,                 branch1.Tree.NodeID);
		Assert.AreEqual(0,                 branch1.Count);
		Assert.AreEqual(0,                 branch2.Parent.NodeID);
		Assert.AreEqual(0,                 branch2.Tree.NodeID);
		Assert.AreEqual(1,                 branch2.Count);
		Assert.AreEqual(0, CSGManager.TreeBrushCount);
		Assert.AreEqual(0, CSGManager.TreeCount);
		Assert.AreEqual(2, CSGManager.TreeBranchCount);
		Assert.AreEqual(2, CSGManager.TreeNodeCount);
	}
	
	[Test]
	public void Tree_SetChildrenWithBranch_ContainsBranch()
	{
		const int branchUserID = 10;
		const int treeUserID = 11;
		var branch	= CSGTreeBranch.Create(branchUserID);
		var tree		= CSGTree.Create(treeUserID);
		CSGManager.ClearDirty(branch);
		CSGManager.ClearDirty(tree);

		tree.InsertRange(0, new CSGTreeNode[] { branch });

		TestUtility.ExpectValidBranchWithUserID(ref branch, branchUserID);
		TestUtility.ExpectValidTreeWithUserID(ref tree, treeUserID);
		Assert.AreEqual(true, branch.Dirty);
		Assert.AreEqual(true, tree.Dirty);
		Assert.AreEqual(0,            branch.Parent.NodeID);
		Assert.AreEqual(tree.NodeID, branch.Tree.NodeID);
		Assert.AreEqual(0,            branch.Count);
		Assert.AreEqual(1,            tree.Count);
		Assert.AreEqual(0, CSGManager.TreeBrushCount);
		Assert.AreEqual(1, CSGManager.TreeCount);
		Assert.AreEqual(1, CSGManager.TreeBranchCount);
		Assert.AreEqual(2, CSGManager.TreeNodeCount);
	}


	[Test]
	public void Tree_SetChildrenWithBranchWithBrush_ContainsBranchThatContainsBrush()
	{
		const int brushUserID		= 10;
		const int branchUserID	= 11;
		const int treeUserID		= 12;
		var brush	= CSGTreeBrush.Create(userID: brushUserID);
		var branch	= CSGTreeBranch.Create(branchUserID);
		var tree	= CSGTree.Create(treeUserID);
		CSGManager.ClearDirty(brush);
		CSGManager.ClearDirty(branch);
		CSGManager.ClearDirty(tree);

		branch	.InsertRange(0, new CSGTreeNode[] { brush });
		tree    .InsertRange(0, new CSGTreeNode[] { branch });

		TestUtility.ExpectValidBrushWithUserID(ref brush, brushUserID);
		TestUtility.ExpectValidBranchWithUserID(ref branch, branchUserID);
		TestUtility.ExpectValidTreeWithUserID(ref tree, treeUserID);
		Assert.AreEqual(true, brush.Dirty);
		Assert.AreEqual(true, branch.Dirty);
		Assert.AreEqual(true, tree.Dirty);
		Assert.AreEqual(true, tree.IsInTree(brush));
		Assert.AreEqual(1, tree.CountOfBrushesInTree);
		Assert.AreEqual(branch.NodeID, brush.Parent.NodeID);
		Assert.AreEqual(tree.NodeID,     brush.Tree.NodeID);
		Assert.AreEqual(0,                branch.Parent.NodeID);
		Assert.AreEqual(tree.NodeID,     branch.Tree.NodeID);
		Assert.AreEqual(1,                branch.Count);
		Assert.AreEqual(1,                tree.Count);
		Assert.AreEqual(1, CSGManager.TreeBrushCount);
		Assert.AreEqual(1, CSGManager.TreeCount);
		Assert.AreEqual(1, CSGManager.TreeBranchCount);
		Assert.AreEqual(3, CSGManager.TreeNodeCount);
	}

	[Test]
	public void Tree_SetChildrenWithBranchWithBrushReversed_ContainsBranchThatContainsBrush()
	{
		const int brushUserID = 10;
		const int branchUserID = 11;
		const int treeUserID = 12;
		var brush	= CSGTreeBrush.Create(userID: brushUserID);
		var branch	= CSGTreeBranch.Create(branchUserID);
		var tree	= CSGTree.Create(treeUserID);
		CSGManager.ClearDirty(brush);
		CSGManager.ClearDirty(branch);
		CSGManager.ClearDirty(tree);

		tree    .InsertRange(0, new CSGTreeNode[] { branch });
		branch	.InsertRange(0, new CSGTreeNode[] { brush });

		TestUtility.ExpectValidBrushWithUserID(ref brush, brushUserID);
		TestUtility.ExpectValidBranchWithUserID(ref branch, branchUserID);
		TestUtility.ExpectValidTreeWithUserID(ref tree, treeUserID);
		Assert.AreEqual(true, brush.Dirty);
		Assert.AreEqual(true, branch.Dirty);
		Assert.AreEqual(true, tree.Dirty);
		Assert.AreEqual(true, tree.IsInTree(brush));
		Assert.AreEqual(1, tree.CountOfBrushesInTree);
		Assert.AreEqual(branch.NodeID,	brush.Parent.NodeID);
		Assert.AreEqual(tree.NodeID,    brush.Tree.NodeID);
		Assert.AreEqual(0,              branch.Parent.NodeID);
		Assert.AreEqual(tree.NodeID,    branch.Tree.NodeID);
		Assert.AreEqual(1,              branch.Count);
		Assert.AreEqual(1,              tree.Count);
		Assert.AreEqual(1, CSGManager.TreeBranchCount);
		Assert.AreEqual(1, CSGManager.TreeBrushCount);
		Assert.AreEqual(1, CSGManager.TreeCount);
		Assert.AreEqual(3, CSGManager.TreeNodeCount);
	}
}
