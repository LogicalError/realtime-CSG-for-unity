using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using RealtimeCSG;
using UnityEditor.SceneManagement;
using RealtimeCSG.Foundation;

[TestFixture]
public partial class AddChildTests
{
	[SetUp]
	public void Init()
	{
		CSGManager.Clear();
	}

	[Test]
	public void Branch_AddAncestor_ReturnsFalse()
	{
		const int branchUserID1 = 10;
		const int branchUserID2 = 11;
		const int branchUserID3 = 12;
		var branch1	= CSGTreeBranch.Create(branchUserID1);
		var branch2	= CSGTreeBranch.Create(branchUserID2);
		var branch3	= CSGTreeBranch.Create(branchUserID3);
		branch1.Add(branch2);
		branch2.Add(branch3);
		CSGManager.ClearDirty(branch1);
		CSGManager.ClearDirty(branch2);
		CSGManager.ClearDirty(branch3);

		var result		= branch3.Add(branch1);

		Assert.AreEqual(false, result);
		TestUtility.ExpectValidBranchWithUserID(ref branch1, branchUserID1);
		TestUtility.ExpectValidBranchWithUserID(ref branch2, branchUserID2);
		TestUtility.ExpectValidBranchWithUserID(ref branch3, branchUserID3);
		Assert.AreEqual(branch2.NodeID, branch3.Parent.NodeID);
		Assert.AreEqual(0,                 branch3.Tree.NodeID);
		Assert.AreEqual(0,                 branch3.Count);
		Assert.AreEqual(false,             branch3.Dirty);
		Assert.AreEqual(branch1.NodeID, branch2.Parent.NodeID);
		Assert.AreEqual(0,				   branch2.Tree.NodeID);
		Assert.AreEqual(1,                 branch2.Count);
		Assert.AreEqual(false,             branch2.Dirty);
		Assert.AreEqual(0,				   branch1.Parent.NodeID);
		Assert.AreEqual(0,				   branch1.Tree.NodeID);
		Assert.AreEqual(1,                 branch1.Count);
		Assert.AreEqual(false,             branch1.Dirty);
		Assert.AreEqual(0, CSGManager.TreeBrushCount);
		Assert.AreEqual(0, CSGManager.TreeCount);
		Assert.AreEqual(3, CSGManager.TreeBranchCount);
		Assert.AreEqual(3, CSGManager.TreeNodeCount);
	}

	/*
	[Test]
	public void BrushAsNode_AddInvalidNode_ReturnsFalse()
	{
		const int brushUserID = 10;
		var brush = CSGTreeBrush.Create(userID: brushUserID);
		var node = (CSGTreeNode)brush;
		CSGUtility.ClearDirty(node);

		var result = node.Add(CSGTreeNode.InvalidNode);

		Assert.AreEqual(false, result);
		TestUtility.ExpectValidBrushWithUserID(ref brush, brushUserID);
		Assert.AreEqual(false, brush.Dirty);
		Assert.AreEqual(0, node.Count);
		Assert.AreEqual(1, CSGUtility.TreeBrushCount);
		Assert.AreEqual(0, CSGUtility.TreeCount);
		Assert.AreEqual(0, CSGUtility.TreeBranchCount);
		Assert.AreEqual(1, CSGUtility.TreeNodeCount);
	}
	*/

	[Test]
	public void Branch_AddInvalidNode_ReturnsFalse()
	{
		const int branchUserID = 10;
		var branch	= CSGTreeBranch.Create(branchUserID);
		CSGManager.ClearDirty(branch);

		var result	= branch.Add(CSGTreeNode.InvalidNode);

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
	public void Tree_AddInvalidNode_ReturnsFalse()
	{
		const int treeUserID = 10;
		var tree	= CSGTree.Create(treeUserID);
		CSGManager.ClearDirty(tree);

		var result	= tree.Add(CSGTreeNode.InvalidNode);

		Assert.AreEqual(false, result);
		TestUtility.ExpectValidTreeWithUserID(ref tree, treeUserID);
		Assert.AreEqual(0, tree.CountOfBrushesInTree);
		Assert.AreEqual(false, tree.Dirty);
		Assert.AreEqual(0, tree.Count);
		Assert.AreEqual(0, CSGManager.TreeBrushCount);
		Assert.AreEqual(0, CSGManager.TreeBranchCount);
		Assert.AreEqual(1, CSGManager.TreeCount);
		Assert.AreEqual(1, CSGManager.TreeNodeCount);
	}

	/*
	[Test]
	public void BrushAsNode_AddSelf_ReturnsFalse()
	{
		const int brushUserID = 10;
		var brush = CSGTreeBrush.Create(userID: brushUserID);
		var node = (CSGTreeNode)brush;
		CSGUtility.ClearDirty(node);

		var result = node.Add(brush);

		Assert.AreEqual(false, result);
		TestUtility.ExpectValidBrushWithUserID(ref brush, brushUserID);
		Assert.AreEqual(false, brush.Dirty);
		Assert.AreEqual(0, node.Count);
		Assert.AreEqual(0, CSGUtility.TreeCount);
		Assert.AreEqual(0, CSGUtility.TreeBranchCount);
		Assert.AreEqual(1, CSGUtility.TreeBrushCount);
		Assert.AreEqual(1, CSGUtility.TreeNodeCount);
	}
	*/

	[Test]
	public void Branch_AddSelf_ReturnsFalse()
	{
		const int branchUserID = 10;
		var branch = CSGTreeBranch.Create(branchUserID);
		CSGManager.ClearDirty(branch);

		var result = branch.Add(branch);

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
	public void Tree_AddSelf_ReturnsFalse()
	{
		const int treeUserID = 10;
		var tree = CSGTree.Create(treeUserID);
		CSGManager.ClearDirty(tree);

		var result = tree.Add(tree);

		Assert.AreEqual(false, result);
		TestUtility.ExpectValidTreeWithUserID(ref tree, treeUserID);
		Assert.AreEqual(0, tree.CountOfBrushesInTree);
		Assert.AreEqual(false, tree.Dirty);
		Assert.AreEqual(0, tree.Count);
		Assert.AreEqual(0, CSGManager.TreeBrushCount);
		Assert.AreEqual(0, CSGManager.TreeBranchCount);
		Assert.AreEqual(1, CSGManager.TreeCount);
		Assert.AreEqual(1, CSGManager.TreeNodeCount);
	}

	/*
	[Test]
	public void BrushAsNode_AddTree_ReturnsFalse()
	{
		const int brushUserID = 10;
		const int treeUserID = 11;
		var tree = CSGTree.Create(treeUserID);
		var brush = CSGTreeBrush.Create(userID: brushUserID);
		var node = (CSGTreeNode)brush;
		CSGUtility.ClearDirty(node);
		CSGUtility.ClearDirty(tree);

		var result = node.Add(tree);

		Assert.AreEqual(false, result);
		TestUtility.ExpectValidBrushWithUserID(ref brush, brushUserID);
		TestUtility.ExpectValidTreeWithUserID(ref tree, treeUserID);
		Assert.AreEqual(0, tree.AllBrushCount);
		Assert.AreEqual(false, tree.Dirty);
		Assert.AreEqual(false, brush.Dirty);
		Assert.AreEqual(0, node.Count);
		Assert.AreEqual(0, CSGUtility.TreeBranchCount);
		Assert.AreEqual(1, CSGUtility.TreeBrushCount);
		Assert.AreEqual(1, CSGUtility.TreeCount);
		Assert.AreEqual(2, CSGUtility.TreeNodeCount);
	}
	*/

	[Test]
	public void Branch_AddTree_ReturnsFalse()
	{
		const int branchUserID = 10;
		const int treeUserID = 11;
		var tree		= CSGTree.Create(treeUserID);
		var branch	= CSGTreeBranch.Create(branchUserID);
		CSGManager.ClearDirty(branch);
		CSGManager.ClearDirty(tree);

		var result		= branch.Add(tree);

		Assert.AreEqual(false, result);
		TestUtility.ExpectValidBranchWithUserID(ref branch, branchUserID);
		TestUtility.ExpectValidTreeWithUserID(ref tree, treeUserID);
		Assert.AreEqual(0, tree.CountOfBrushesInTree);
		Assert.AreEqual(false, tree.Dirty);
		Assert.AreEqual(false, branch.Dirty);
		Assert.AreEqual(0, branch.Count);
		Assert.AreEqual(0, CSGManager.TreeBrushCount);
		Assert.AreEqual(1, CSGManager.TreeCount);
		Assert.AreEqual(1, CSGManager.TreeBranchCount);
		Assert.AreEqual(2, CSGManager.TreeNodeCount);
	}

	[Test]
	public void Tree_AddTree_ReturnsFalse()
	{
		const int treeUserID1 = 10;
		const int treeUserID2 = 11;
		var tree1	= CSGTree.Create(treeUserID1);
		var tree2	= CSGTree.Create(treeUserID2);
		CSGManager.ClearDirty(tree1);
		CSGManager.ClearDirty(tree2);

		var result	= tree1.Add(tree2);

		Assert.AreEqual(false, result);
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


	/*
	[Test]
	public void BrushAsNode_AddBrush_ReturnsFalse()
	{
		const int brushUserID1 = 10;
		const int brushUserID2 = 11;
		var brush1 = CSGTreeBrush.Create(userID: brushUserID1);
		var brush2 = CSGTreeBrush.Create(userID: brushUserID2);
		var node = (CSGTreeNode)brush1;
		CSGUtility.ClearDirty(brush1);
		CSGUtility.ClearDirty(brush2);

		var result = node.Add(brush2);

		Assert.AreEqual(false, result);
		TestUtility.ExpectValidBrushWithUserID(ref brush1, brushUserID1);
		TestUtility.ExpectValidBrushWithUserID(ref brush2, brushUserID2);
		Assert.AreEqual(false, brush1.Dirty);
		Assert.AreEqual(false, brush2.Dirty);
		Assert.AreEqual(0, node.Count);
		Assert.AreEqual(0, CSGUtility.TreeCount);
		Assert.AreEqual(0, CSGUtility.TreeBranchCount);
		Assert.AreEqual(2, CSGUtility.TreeBrushCount);
		Assert.AreEqual(2, CSGUtility.TreeNodeCount);
	}
	*/

	[Test]
	public void Branch_AddBrush_ContainsBrush()
	{
		const int brushUserID = 10;
		const int branchUserID = 11;		
		var brush	= CSGTreeBrush.Create(userID: brushUserID);
		var branch	= CSGTreeBranch.Create(userID: branchUserID);
		CSGManager.ClearDirty(brush);
		CSGManager.ClearDirty(branch);

		var result		= branch.Add(brush);
		
		Assert.AreEqual(true, result);
		TestUtility.ExpectValidBrushWithUserID(ref brush, brushUserID);
		TestUtility.ExpectValidBranchWithUserID(ref branch, branchUserID);
		Assert.AreEqual(true, branch.Dirty);
		Assert.AreEqual(true, brush.Dirty);
		Assert.AreEqual(branch.NodeID, brush.Parent.NodeID);
		Assert.AreEqual(0,                brush.Tree.NodeID);
		Assert.AreEqual(1, branch.Count);
		Assert.AreEqual(0, CSGManager.TreeCount);
		Assert.AreEqual(1, CSGManager.TreeBrushCount);
		Assert.AreEqual(1, CSGManager.TreeBranchCount);
		Assert.AreEqual(2, CSGManager.TreeNodeCount);
	}
	
	[Test]
	public void Tree_AddBrush_ContainsBrush()
	{
		const int brushUserID = 10;
		const int treeUserID = 11;
		var brush	= CSGTreeBrush.Create(userID: brushUserID);
		var tree	= CSGTree.Create(treeUserID);
		CSGManager.ClearDirty(brush);
		CSGManager.ClearDirty(tree);

		var result = tree.Add(brush);

		Assert.AreEqual(true, result);
		TestUtility.ExpectValidBrushWithUserID(ref brush, brushUserID);
		TestUtility.ExpectValidTreeWithUserID(ref tree, treeUserID);
		Assert.AreEqual(true, tree.Dirty);
		Assert.AreEqual(true, brush.Dirty);
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
	public void Branch_AddChildBrushOfOtherBranch_MovesBrushToBranch()
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

		var result = branch2.Add(brush);

		Assert.AreEqual(true, result);
		TestUtility.ExpectValidBrushWithUserID(ref brush, brushUserID);
		TestUtility.ExpectValidBranchWithUserID(ref branch1, branchUserID1);
		TestUtility.ExpectValidBranchWithUserID(ref branch2, branchUserID2);
		TestUtility.ExpectValidTreeWithUserID(ref tree1, treeUserID1);
		TestUtility.ExpectValidTreeWithUserID(ref tree2, treeUserID2);
		Assert.AreEqual(true, branch1.Dirty);
		Assert.AreEqual(true, tree1.Dirty);
		Assert.AreEqual(true, branch2.Dirty);
		Assert.AreEqual(true, tree2.Dirty);
		Assert.AreEqual(true, brush.Dirty);
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
	public void Tree_AddChildBrushOfOtherTree_MovesBrushToTree()
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
		var result = tree2.Add(brush);

		Assert.AreEqual(true, result); 
		TestUtility.ExpectValidBrushWithUserID(ref brush, brushUserID);
		TestUtility.ExpectValidTreeWithUserID(ref tree1, treeUserID1);
		TestUtility.ExpectValidTreeWithUserID(ref tree2, treeUserID2);
		Assert.AreEqual(true, tree1.Dirty);
		Assert.AreEqual(true, tree2.Dirty);
		Assert.AreEqual(true, brush.Dirty);
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
	public void Branch_AddChildBrushOfOtherTree_MovesBrushToTree()
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
		var result = tree2.Add(brush);

		Assert.AreEqual(true, result);
		TestUtility.ExpectValidBrushWithUserID(ref brush, brushUserID);
		TestUtility.ExpectValidBranchWithUserID(ref branch1, branchUserID1);
		TestUtility.ExpectValidTreeWithUserID(ref tree1, treeUserID1);
		TestUtility.ExpectValidTreeWithUserID(ref tree2, treeUserID2);
		Assert.AreEqual(true, branch1.Dirty);
		Assert.AreEqual(true, tree1.Dirty);
		Assert.AreEqual(true, tree2.Dirty);
		Assert.AreEqual(true, brush.Dirty);
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
	public void Tree_AddChildBrushOfOtherBranch_MovesBrushToBranch()
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

		var result = branch2.Add(brush);

		Assert.AreEqual(true, result);
		TestUtility.ExpectValidBrushWithUserID(ref brush, brushUserID);
		TestUtility.ExpectValidBranchWithUserID(ref branch2, branchUserID2);
		TestUtility.ExpectValidTreeWithUserID(ref tree1, treeUserID1);
		TestUtility.ExpectValidTreeWithUserID(ref tree2, treeUserID2);
		Assert.AreEqual(true, tree1.Dirty);
		Assert.AreEqual(true, branch2.Dirty);
		Assert.AreEqual(true, tree2.Dirty);
		Assert.AreEqual(true, brush.Dirty);
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


	[Test]
	public void Branch_AddSameBrushTwice_ReturnsFalse()
	{
		const int brushUserID = 10;
		const int branchUserID = 11;
		var brush	= CSGTreeBrush.Create(userID: brushUserID);
		var branch	= CSGTreeBranch.Create(branchUserID);
		CSGManager.ClearDirty(brush);
		CSGManager.ClearDirty(branch);

		var result1 = branch.Add(brush);
		var result2 = branch.Add(brush);

		Assert.AreEqual(true, result1);
		Assert.AreEqual(false, result2);
		TestUtility.ExpectValidBrushWithUserID(ref brush, brushUserID);
		TestUtility.ExpectValidBranchWithUserID(ref branch, branchUserID);
		Assert.AreEqual(true, branch.Dirty);
		Assert.AreEqual(true, brush.Dirty);
		Assert.AreEqual(0,                brush.Tree.NodeID);
		Assert.AreEqual(branch.NodeID, brush.Parent.NodeID);
		Assert.AreEqual(0, brush.Tree.NodeID);
		Assert.AreEqual(1, branch.Count);
		Assert.AreEqual(0, CSGManager.TreeCount);
		Assert.AreEqual(1, CSGManager.TreeBrushCount);
		Assert.AreEqual(1, CSGManager.TreeBranchCount);
		Assert.AreEqual(2, CSGManager.TreeNodeCount);
	}

	[Test]
	public void Tree_AddSameBrushTwice_ReturnsFalse()
	{
		const int brushUserID = 10;
		const int treeUserID = 11;
		var brush = CSGTreeBrush.Create(userID: brushUserID);
		var tree  = CSGTree.Create(treeUserID);
		CSGManager.ClearDirty(brush);
		CSGManager.ClearDirty(tree);

		var result1 = tree.Add(brush);
		var result2 = tree.Add(brush);

		Assert.AreEqual(true, result1);
		Assert.AreEqual(false, result2);
		TestUtility.ExpectValidBrushWithUserID(ref brush, brushUserID);
		TestUtility.ExpectValidTreeWithUserID(ref tree, treeUserID);
		Assert.AreEqual(true, tree.IsInTree(brush));
		Assert.AreEqual(1, tree.CountOfBrushesInTree);
		Assert.AreEqual(true, tree.Dirty);
		Assert.AreEqual(true, brush.Dirty);
		Assert.AreEqual(0,            brush.Parent.NodeID);
		Assert.AreEqual(tree.NodeID, brush.Tree.NodeID);
		Assert.AreEqual(1, tree.Count);
		Assert.AreEqual(0, CSGManager.TreeBranchCount);
		Assert.AreEqual(1, CSGManager.TreeBrushCount);
		Assert.AreEqual(1, CSGManager.TreeCount);
		Assert.AreEqual(2, CSGManager.TreeNodeCount);
	}

	/*
	[Test]
	public void BrushAsNode_AddBranch_ReturnsFalse()
	{
		const int branchUserID = 10;
		const int brushUserID = 11;
		var branch	= CSGTreeBranch.Create(branchUserID);
		var brush	= CSGTreeBrush.Create(userID: brushUserID);
		var node	= (CSGTreeNode)brush;
		CSGUtility.ClearDirty(brush);
		CSGUtility.ClearDirty(branch);

		var result = node.Add(branch);

		Assert.AreEqual(false, result);
		TestUtility.ExpectValidBrushWithUserID(ref brush, brushUserID);
		TestUtility.ExpectValidBranchWithUserID(ref branch, branchUserID);
		Assert.AreEqual(false, branch.Dirty);
		Assert.AreEqual(false, brush.Dirty);
		Assert.AreEqual(0, branch.Parent.NodeID);
		Assert.AreEqual(0, branch.Tree.NodeID);
		Assert.AreEqual(0, branch.Count);
		Assert.AreEqual(0, brush.Parent.NodeID);
		Assert.AreEqual(0, brush.Tree.NodeID);
		Assert.AreEqual(0, node.Count);
		Assert.AreEqual(1, CSGUtility.TreeBrushCount);
		Assert.AreEqual(0, CSGUtility.TreeCount);
		Assert.AreEqual(1, CSGUtility.TreeBranchCount);
		Assert.AreEqual(2, CSGUtility.TreeNodeCount);
	}
	*/

	[Test]
	public void Branch_AddBranch_ContainsBranch()
	{
		const int branchUserID1 = 10;
		const int branchUserID2 = 11;
		var branch1 = CSGTreeBranch.Create(branchUserID1);
		var branch2 = CSGTreeBranch.Create(branchUserID2);
		CSGManager.ClearDirty(branch1);
		CSGManager.ClearDirty(branch2);

		var result		= branch2.Add(branch1);

		Assert.AreEqual(true, result);
		TestUtility.ExpectValidBranchWithUserID(ref branch1, branchUserID1);
		TestUtility.ExpectValidBranchWithUserID(ref branch2, branchUserID2);
		Assert.AreEqual(true, branch1.Dirty);
		Assert.AreEqual(true, branch2.Dirty);
		Assert.AreEqual(branch2.NodeID, branch1.Parent.NodeID);
		Assert.AreEqual(0, branch1.Tree.NodeID);
		Assert.AreEqual(0, branch1.Count);
		Assert.AreEqual(0, branch2.Parent.NodeID);
		Assert.AreEqual(0, branch2.Tree.NodeID);
		Assert.AreEqual(1, branch2.Count);
		Assert.AreEqual(0, CSGManager.TreeBrushCount);
		Assert.AreEqual(0, CSGManager.TreeCount);
		Assert.AreEqual(2, CSGManager.TreeBranchCount);
		Assert.AreEqual(2, CSGManager.TreeNodeCount);
	}

	[Test]
	public void Tree_AddBranch_ContainsBranch()
	{
		const int branchUserID = 10;
		const int treeUserID = 11;
		var branch	= CSGTreeBranch.Create(branchUserID);
		var tree	= CSGTree.Create(treeUserID);
		CSGManager.ClearDirty(branch);
		CSGManager.ClearDirty(tree);

		var result		= tree.Add(branch);

		Assert.AreEqual(true, result);
		TestUtility.ExpectValidBranchWithUserID(ref branch, branchUserID);
		TestUtility.ExpectValidTreeWithUserID(ref tree, treeUserID);
		Assert.AreEqual(true, branch.Dirty);
		Assert.AreEqual(true, tree.Dirty);
		Assert.AreEqual(0, tree.CountOfBrushesInTree);
		Assert.AreEqual(0,            branch.Parent.NodeID);
		Assert.AreEqual(tree.NodeID, branch.Tree.NodeID);
		Assert.AreEqual(0, branch.Count);
		Assert.AreEqual(1, tree.Count);
		Assert.AreEqual(0, CSGManager.TreeBrushCount);
		Assert.AreEqual(1, CSGManager.TreeCount);
		Assert.AreEqual(1, CSGManager.TreeBranchCount);
		Assert.AreEqual(2, CSGManager.TreeNodeCount);
	}
	

	[Test]
	public void Branch_AddSameBranchTwice_ReturnsFalse()
	{
		const int branchUserID1 = 10;
		const int branchUserID2 = 11;
		var branch1 = CSGTreeBranch.Create(branchUserID1);
		var branch2 = CSGTreeBranch.Create(branchUserID2);
		CSGManager.ClearDirty(branch1);
		CSGManager.ClearDirty(branch2);

		var result1		= branch2.Add(branch1);
		var result2		= branch2.Add(branch1);

		Assert.AreEqual(true, result1);
		Assert.AreEqual(false, result2);
		TestUtility.ExpectValidBranchWithUserID(ref branch1, branchUserID1);
		TestUtility.ExpectValidBranchWithUserID(ref branch2, branchUserID2);
		Assert.AreEqual(true, branch1.Dirty);
		Assert.AreEqual(true, branch2.Dirty);
		Assert.AreEqual(branch2.NodeID, branch1.Parent.NodeID);
		Assert.AreEqual(0, branch1.Tree.NodeID);
		Assert.AreEqual(0, branch1.Count);
		Assert.AreEqual(0, branch2.Parent.NodeID);
		Assert.AreEqual(0, branch2.Tree.NodeID);
		Assert.AreEqual(1, branch2.Count);
		Assert.AreEqual(0, CSGManager.TreeBrushCount);
		Assert.AreEqual(0, CSGManager.TreeCount);
		Assert.AreEqual(2, CSGManager.TreeBranchCount);
		Assert.AreEqual(2, CSGManager.TreeNodeCount);
	}

	[Test]
	public void Tree_AddSameBranchTwice_ReturnsFalse()
	{
		const int branchUserID = 10;
		const int treeUserID = 11;
		var branch	= CSGTreeBranch.Create(branchUserID);
		var tree		= CSGTree.Create(treeUserID);
		CSGManager.ClearDirty(branch);
		CSGManager.ClearDirty(tree);

		var result1		= tree.Add(branch);
		var result2		= tree.Add(branch);

		Assert.AreEqual(true, result1);
		Assert.AreEqual(false, result2);
		TestUtility.ExpectValidBranchWithUserID(ref branch, branchUserID);
		TestUtility.ExpectValidTreeWithUserID(ref tree, treeUserID);
		Assert.AreEqual(true, branch.Dirty);
		Assert.AreEqual(true, tree.Dirty);
		Assert.AreEqual(0, tree.CountOfBrushesInTree);
		Assert.AreEqual(0, branch.Parent.NodeID);
		Assert.AreEqual(tree.NodeID, branch.Tree.NodeID);
		Assert.AreEqual(0, branch.Count);
		Assert.AreEqual(1, tree.Count);
		Assert.AreEqual(0, CSGManager.TreeBrushCount);
		Assert.AreEqual(1, CSGManager.TreeCount);
		Assert.AreEqual(1, CSGManager.TreeBranchCount);
		Assert.AreEqual(2, CSGManager.TreeNodeCount);
	}
	

	[Test]
	public void Tree_AddBranchAddBrush_ContainsBranchThatContainsBrush()
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

		var result1		= branch.Add(brush);
		var result2		= tree    .Add(branch);

		Assert.AreEqual(true, result1);
		Assert.AreEqual(true, result2);
		TestUtility.ExpectValidBrushWithUserID(ref brush, brushUserID);
		TestUtility.ExpectValidBranchWithUserID(ref branch, branchUserID);
		TestUtility.ExpectValidTreeWithUserID(ref tree, treeUserID);
		Assert.AreEqual(true, branch.Dirty);
		Assert.AreEqual(true, tree.Dirty);
		Assert.AreEqual(true, brush.Dirty);
		Assert.AreEqual(true, tree.IsInTree(brush));
		Assert.AreEqual(1, tree.CountOfBrushesInTree);
		Assert.AreEqual(branch.NodeID,	brush.Parent.NodeID);
		Assert.AreEqual(tree.NodeID,	brush.Tree.NodeID);
		Assert.AreEqual(0,				branch.Parent.NodeID);
		Assert.AreEqual(tree.NodeID,	branch.Tree.NodeID);
		Assert.AreEqual(1,				branch.Count);
		Assert.AreEqual(1,				tree.Count);
		Assert.AreEqual(1, CSGManager.TreeBrushCount);
		Assert.AreEqual(1, CSGManager.TreeCount);
		Assert.AreEqual(1, CSGManager.TreeBranchCount);
		Assert.AreEqual(3, CSGManager.TreeNodeCount);
	}

	[Test]
	public void Tree_AddBranchAddBrushReversed_ContainsBranchThatContainsBrush()
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

		var result1		= tree  .Add(branch);
		var result2		= branch.Add(brush);

		Assert.AreEqual(true, result1);
		Assert.AreEqual(true, result2);
		TestUtility.ExpectValidBrushWithUserID(ref brush, brushUserID);
		TestUtility.ExpectValidBranchWithUserID(ref branch, branchUserID);
		TestUtility.ExpectValidTreeWithUserID(ref tree, treeUserID);
		Assert.AreEqual(true, branch.Dirty);
		Assert.AreEqual(true, tree.Dirty);
		Assert.AreEqual(true, brush.Dirty);
		Assert.AreEqual(true, tree.IsInTree(brush));
		Assert.AreEqual(1, tree.CountOfBrushesInTree);
		Assert.AreEqual(branch.NodeID,	brush.Parent.NodeID);
		Assert.AreEqual(tree.NodeID,	brush.Tree.NodeID);
		Assert.AreEqual(0,				branch.Parent.NodeID);
		Assert.AreEqual(tree.NodeID,	branch.Tree.NodeID);
		Assert.AreEqual(1,				branch.Count);
		Assert.AreEqual(1,				tree.Count);
		Assert.AreEqual(1, CSGManager.TreeBranchCount);
		Assert.AreEqual(1, CSGManager.TreeBrushCount);
		Assert.AreEqual(1, CSGManager.TreeCount);
		Assert.AreEqual(3, CSGManager.TreeNodeCount);
	}
}
