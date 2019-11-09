using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using RealtimeCSG;
using RealtimeCSG.Foundation;
using UnityEditor.SceneManagement;

[TestFixture]
public partial class RemoveRangeTests
{
	[SetUp]
	public void Init()
	{
		CSGManager.Clear();
	}


	[Test]
	public void TreeWithNoChildren_RemoveRange_ReturnsFalse()
	{
		const int treeUserID = 13;
		var tree = CSGTree.Create(treeUserID);
		CSGManager.ClearDirty(tree);

		var result = tree.RemoveRange(0, 1);

		Assert.AreEqual(false, result);
		TestUtility.ExpectValidTreeWithUserID(ref tree, treeUserID);
		Assert.AreEqual(false, tree.Dirty);
		Assert.AreEqual(0, tree.Count);
		Assert.AreEqual(0, CSGManager.TreeBranchCount);
		Assert.AreEqual(0, CSGManager.TreeBrushCount);
		Assert.AreEqual(1, CSGManager.TreeCount);
		Assert.AreEqual(1, CSGManager.TreeNodeCount);
	}


	[Test]
	public void TreeWith3Children_RemoveRangeAt0WithCount2_TreeContainsLastBrush()
	{
		const int brushUserID1 = 10;
		const int brushUserID2 = 11;
		const int brushUserID3 = 12;
		const int treeUserID = 13;
		var brush1 = CSGTreeBrush.Create(userID: brushUserID1);
		var brush2 = CSGTreeBrush.Create(userID: brushUserID2);
		var brush3 = CSGTreeBrush.Create(userID: brushUserID3);
		var tree = CSGTree.Create(treeUserID, new CSGTreeNode[] { brush1, brush2, brush3 });
		CSGManager.ClearDirty(brush1);
		CSGManager.ClearDirty(brush2);
		CSGManager.ClearDirty(brush3);
		CSGManager.ClearDirty(tree);

		var result = tree.RemoveRange(0, 2);

		Assert.AreEqual(true, result);
		TestUtility.ExpectValidBrushWithUserID(ref brush1, brushUserID1);
		TestUtility.ExpectValidBrushWithUserID(ref brush2, brushUserID2);
		TestUtility.ExpectValidBrushWithUserID(ref brush3, brushUserID3);
		TestUtility.ExpectValidTreeWithUserID(ref tree, treeUserID);
		Assert.AreEqual(true, brush1.Dirty);
		Assert.AreEqual(true, brush2.Dirty);
		Assert.AreEqual(false, brush3.Dirty);
		Assert.AreEqual(true, tree.Dirty);;
		Assert.AreEqual(0, brush1.Parent.NodeID);
		Assert.AreEqual(0, brush1.Tree.NodeID);
		Assert.AreEqual(0, brush2.Parent.NodeID);
		Assert.AreEqual(0, brush2.Tree.NodeID);
		Assert.AreEqual(0,            brush3.Parent.NodeID);
		Assert.AreEqual(tree.NodeID, brush3.Tree.NodeID);
		Assert.AreEqual(brush3.NodeID, tree[0].NodeID);
		Assert.AreEqual(1, tree.Count);
		Assert.AreEqual(0, CSGManager.TreeBranchCount);
		Assert.AreEqual(3, CSGManager.TreeBrushCount);
		Assert.AreEqual(1, CSGManager.TreeCount);
		Assert.AreEqual(4, CSGManager.TreeNodeCount);
	}
	

	[Test]
	public void TreeWith3Children_RemoveRangeAt1WithCount2_TreeContainsFirstBrush()
	{
		const int brushUserID1 = 10;
		const int brushUserID2 = 11;
		const int brushUserID3 = 12;
		const int treeUserID = 13;
		var brush1 = CSGTreeBrush.Create(userID: brushUserID1);
		var brush2 = CSGTreeBrush.Create(userID: brushUserID2);
		var brush3 = CSGTreeBrush.Create(userID: brushUserID3);
		var tree = CSGTree.Create(treeUserID, new CSGTreeNode[] { brush1, brush2, brush3 });
		CSGManager.ClearDirty(brush1);
		CSGManager.ClearDirty(brush2);
		CSGManager.ClearDirty(brush3);
		CSGManager.ClearDirty(tree);

		var result = tree.RemoveRange(1, 2);

		Assert.AreEqual(true, result);
		TestUtility.ExpectValidBrushWithUserID(ref brush1, brushUserID1);
		TestUtility.ExpectValidBrushWithUserID(ref brush2, brushUserID2);
		TestUtility.ExpectValidBrushWithUserID(ref brush3, brushUserID3);
		TestUtility.ExpectValidTreeWithUserID(ref tree, treeUserID);
		Assert.AreEqual(false, brush1.Dirty);
		Assert.AreEqual(true, brush2.Dirty);
		Assert.AreEqual(true, brush3.Dirty);
		Assert.AreEqual(true, tree.Dirty);
		Assert.AreEqual(0, brush3.Parent.NodeID);
		Assert.AreEqual(0, brush3.Tree.NodeID);
		Assert.AreEqual(0, brush2.Parent.NodeID);
		Assert.AreEqual(0, brush2.Tree.NodeID);
		Assert.AreEqual(0,            brush1.Parent.NodeID);
		Assert.AreEqual(tree.NodeID, brush1.Tree.NodeID);
		Assert.AreEqual(brush1.NodeID, tree[0].NodeID);
		Assert.AreEqual(1, tree.Count);
		Assert.AreEqual(0, CSGManager.TreeBranchCount);
		Assert.AreEqual(3, CSGManager.TreeBrushCount);
		Assert.AreEqual(1, CSGManager.TreeCount);
		Assert.AreEqual(4, CSGManager.TreeNodeCount);
	}
	

	[Test]
	public void TreeWith3Children_RemoveRangeAt0WithCount3_TreeIsEmpty()
	{
		const int brushUserID1 = 10;
		const int brushUserID2 = 11;
		const int brushUserID3 = 12;
		const int treeUserID = 13;
		var brush1 = CSGTreeBrush.Create(userID: brushUserID1);
		var brush2 = CSGTreeBrush.Create(userID: brushUserID2);
		var brush3 = CSGTreeBrush.Create(userID: brushUserID3);
		var tree = CSGTree.Create(treeUserID, new CSGTreeNode[] { brush1, brush2, brush3 });
		CSGManager.ClearDirty(brush1);
		CSGManager.ClearDirty(brush2);
		CSGManager.ClearDirty(brush3);
		CSGManager.ClearDirty(tree);

		var result = tree.RemoveRange(0, 3);

		Assert.AreEqual(true, result);
		TestUtility.ExpectValidBrushWithUserID(ref brush1, brushUserID1);
		TestUtility.ExpectValidBrushWithUserID(ref brush2, brushUserID2);
		TestUtility.ExpectValidBrushWithUserID(ref brush3, brushUserID3);
		TestUtility.ExpectValidTreeWithUserID(ref tree, treeUserID);
		Assert.AreEqual(true, brush1.Dirty);
		Assert.AreEqual(true, brush2.Dirty);
		Assert.AreEqual(true, brush3.Dirty);
		Assert.AreEqual(true, tree.Dirty);
		Assert.AreEqual(0, brush1.Parent.NodeID);
		Assert.AreEqual(0, brush1.Tree.NodeID);
		Assert.AreEqual(0, brush2.Parent.NodeID);
		Assert.AreEqual(0, brush2.Tree.NodeID);
		Assert.AreEqual(0, brush3.Parent.NodeID);
		Assert.AreEqual(0, brush3.Tree.NodeID);
		Assert.AreEqual(0, tree.Count);
		Assert.AreEqual(0, CSGManager.TreeBranchCount);
		Assert.AreEqual(3, CSGManager.TreeBrushCount);
		Assert.AreEqual(1, CSGManager.TreeCount);
		Assert.AreEqual(4, CSGManager.TreeNodeCount);
	}


	[Test]
	public void TreeWith3Children_RemoveRangeAtWithNegativeIndex_ReturnsFalse()
	{
		const int brushUserID1 = 10;
		const int brushUserID2 = 11;
		const int brushUserID3 = 12;
		const int treeUserID = 13;
		var brush1 = CSGTreeBrush.Create(userID: brushUserID1);
		var brush2 = CSGTreeBrush.Create(userID: brushUserID2);
		var brush3 = CSGTreeBrush.Create(userID: brushUserID3);
		var tree = CSGTree.Create(treeUserID, new CSGTreeNode[] { brush1, brush2, brush3 });
		CSGManager.ClearDirty(brush1);
		CSGManager.ClearDirty(brush2);
		CSGManager.ClearDirty(brush3);
		CSGManager.ClearDirty(tree);

		var result = tree.RemoveRange(-1, 3);

		Assert.AreEqual(false, result);
		TestUtility.ExpectValidBrushWithUserID(ref brush1, brushUserID1);
		TestUtility.ExpectValidBrushWithUserID(ref brush2, brushUserID2);
		TestUtility.ExpectValidBrushWithUserID(ref brush3, brushUserID3);
		TestUtility.ExpectValidTreeWithUserID(ref tree, treeUserID);
		Assert.AreEqual(false, brush1.Dirty);
		Assert.AreEqual(false, brush2.Dirty);
		Assert.AreEqual(false, brush3.Dirty);
		Assert.AreEqual(false, tree.Dirty);
		Assert.AreEqual(0,				brush1.Parent.NodeID);
		Assert.AreEqual(tree.NodeID,	brush1.Tree.NodeID);
		Assert.AreEqual(0,				brush2.Parent.NodeID);
		Assert.AreEqual(tree.NodeID,	brush2.Tree.NodeID);
		Assert.AreEqual(0,				brush3.Parent.NodeID);
		Assert.AreEqual(tree.NodeID,	brush3.Tree.NodeID);
		Assert.AreEqual(3,				tree.Count);
		Assert.AreEqual(0, CSGManager.TreeBranchCount);
		Assert.AreEqual(3, CSGManager.TreeBrushCount);
		Assert.AreEqual(1, CSGManager.TreeCount);
		Assert.AreEqual(4, CSGManager.TreeNodeCount);
	}


	[Test]
	public void TreeWith3Children_RemoveRangeAt0WithTooLargeCount_ReturnsFalse()
	{
		const int brushUserID1 = 10;
		const int brushUserID2 = 11;
		const int brushUserID3 = 12;
		const int treeUserID = 13;
		var brush1 = CSGTreeBrush.Create(userID: brushUserID1);
		var brush2 = CSGTreeBrush.Create(userID: brushUserID2);
		var brush3 = CSGTreeBrush.Create(userID: brushUserID3);
		var tree = CSGTree.Create(treeUserID, new CSGTreeNode[] { brush1, brush2, brush3 });
		CSGManager.ClearDirty(brush1);
		CSGManager.ClearDirty(brush2);
		CSGManager.ClearDirty(brush3);
		CSGManager.ClearDirty(tree);

		var result = tree.RemoveRange(0, 4);

		Assert.AreEqual(false, result);
		TestUtility.ExpectValidBrushWithUserID(ref brush1, brushUserID1);
		TestUtility.ExpectValidBrushWithUserID(ref brush2, brushUserID2);
		TestUtility.ExpectValidBrushWithUserID(ref brush3, brushUserID3);
		TestUtility.ExpectValidTreeWithUserID(ref tree, treeUserID);
		Assert.AreEqual(0,				brush1.Parent.NodeID);
		Assert.AreEqual(tree.NodeID,	brush1.Tree.NodeID);
		Assert.AreEqual(0,				brush2.Parent.NodeID);
		Assert.AreEqual(tree.NodeID,	brush2.Tree.NodeID);
		Assert.AreEqual(0,				brush3.Parent.NodeID);
		Assert.AreEqual(tree.NodeID,	brush3.Tree.NodeID);
		Assert.AreEqual(3,				tree.Count);
		Assert.AreEqual(0, CSGManager.TreeBranchCount);
		Assert.AreEqual(3, CSGManager.TreeBrushCount);
		Assert.AreEqual(1, CSGManager.TreeCount);
		Assert.AreEqual(4, CSGManager.TreeNodeCount);
	}


	[Test]
	public void TreeWith3Children_RemoveRangeAt1WithTooLargeCount_ReturnsFalse()
	{
		const int brushUserID1 = 10;
		const int brushUserID2 = 11;
		const int brushUserID3 = 12;
		const int treeUserID = 13;
		var brush1 = CSGTreeBrush.Create(userID: brushUserID1);
		var brush2 = CSGTreeBrush.Create(userID: brushUserID2);
		var brush3 = CSGTreeBrush.Create(userID: brushUserID3);
		var tree = CSGTree.Create(treeUserID, new CSGTreeNode[] { brush1, brush2, brush3 });
		CSGManager.ClearDirty(brush1);
		CSGManager.ClearDirty(brush2);
		CSGManager.ClearDirty(brush3);
		CSGManager.ClearDirty(tree);

		var result = tree.RemoveRange(1, 3);

		Assert.AreEqual(false, result);
		TestUtility.ExpectValidBrushWithUserID(ref brush1, brushUserID1);
		TestUtility.ExpectValidBrushWithUserID(ref brush2, brushUserID2);
		TestUtility.ExpectValidBrushWithUserID(ref brush3, brushUserID3);
		TestUtility.ExpectValidTreeWithUserID(ref tree, treeUserID);
		Assert.AreEqual(0,				brush1.Parent.NodeID);
		Assert.AreEqual(tree.NodeID,	brush1.Tree.NodeID);
		Assert.AreEqual(0,				brush2.Parent.NodeID);
		Assert.AreEqual(tree.NodeID,	brush2.Tree.NodeID);
		Assert.AreEqual(0,				brush3.Parent.NodeID);
		Assert.AreEqual(tree.NodeID,	brush3.Tree.NodeID);
		Assert.AreEqual(3,				tree.Count);
		Assert.AreEqual(0, CSGManager.TreeBranchCount);
		Assert.AreEqual(3, CSGManager.TreeBrushCount);
		Assert.AreEqual(1, CSGManager.TreeCount);
		Assert.AreEqual(4, CSGManager.TreeNodeCount);
	}


	[Test]
	public void TreeWith3Children_RemoveRangeWithCount0AndValidIndex_ReturnsTrueButRemovesNothing()
	{
		const int brushUserID1 = 10;
		const int brushUserID2 = 11;
		const int brushUserID3 = 12;
		const int treeUserID = 13;
		var brush1 = CSGTreeBrush.Create(userID: brushUserID1);
		var brush2 = CSGTreeBrush.Create(userID: brushUserID2);
		var brush3 = CSGTreeBrush.Create(userID: brushUserID3);
		var tree = CSGTree.Create(treeUserID, new CSGTreeNode[] { brush1, brush2, brush3 });
		CSGManager.ClearDirty(brush1);
		CSGManager.ClearDirty(brush2);
		CSGManager.ClearDirty(brush3);
		CSGManager.ClearDirty(tree);

		var result = tree.RemoveRange(0, 0);

		Assert.AreEqual(true, result);
		TestUtility.ExpectValidBrushWithUserID(ref brush1, brushUserID1);
		TestUtility.ExpectValidBrushWithUserID(ref brush2, brushUserID2);
		TestUtility.ExpectValidBrushWithUserID(ref brush3, brushUserID3);
		TestUtility.ExpectValidTreeWithUserID(ref tree, treeUserID);
		Assert.AreEqual(0,				brush1.Parent.NodeID);
		Assert.AreEqual(tree.NodeID,	brush1.Tree.NodeID);
		Assert.AreEqual(0,				brush2.Parent.NodeID);
		Assert.AreEqual(tree.NodeID,	brush2.Tree.NodeID);
		Assert.AreEqual(0,				brush3.Parent.NodeID);
		Assert.AreEqual(tree.NodeID,	brush3.Tree.NodeID);
		Assert.AreEqual(3,				tree.Count);
		Assert.AreEqual(0, CSGManager.TreeBranchCount);
		Assert.AreEqual(3, CSGManager.TreeBrushCount);
		Assert.AreEqual(1, CSGManager.TreeCount);
		Assert.AreEqual(4, CSGManager.TreeNodeCount);
	}


	[Test]
	public void TreeWith3Children_RemoveRangeWithCount0AndInvalidIndex_ReturnsFalse()
	{
		const int brushUserID1 = 10;
		const int brushUserID2 = 11;
		const int brushUserID3 = 12;
		const int treeUserID = 13;
		var brush1 = CSGTreeBrush.Create(userID: brushUserID1);
		var brush2 = CSGTreeBrush.Create(userID: brushUserID2);
		var brush3 = CSGTreeBrush.Create(userID: brushUserID3);
		var tree = CSGTree.Create(treeUserID, new CSGTreeNode[] { brush1, brush2, brush3 });
		CSGManager.ClearDirty(brush1);
		CSGManager.ClearDirty(brush2);
		CSGManager.ClearDirty(brush3);
		CSGManager.ClearDirty(tree);

		var result = tree.RemoveRange(3, 0);

		Assert.AreEqual(false, result);
		TestUtility.ExpectValidBrushWithUserID(ref brush1, brushUserID1);
		TestUtility.ExpectValidBrushWithUserID(ref brush2, brushUserID2);
		TestUtility.ExpectValidBrushWithUserID(ref brush3, brushUserID3);
		TestUtility.ExpectValidTreeWithUserID(ref tree, treeUserID);
		Assert.AreEqual(0,				brush1.Parent.NodeID);
		Assert.AreEqual(tree.NodeID,	brush1.Tree.NodeID);
		Assert.AreEqual(0,				brush2.Parent.NodeID);
		Assert.AreEqual(tree.NodeID,	brush2.Tree.NodeID);
		Assert.AreEqual(0,				brush3.Parent.NodeID);
		Assert.AreEqual(tree.NodeID,	brush3.Tree.NodeID);
		Assert.AreEqual(3,				tree.Count);
		Assert.AreEqual(0, CSGManager.TreeBranchCount);
		Assert.AreEqual(3, CSGManager.TreeBrushCount);
		Assert.AreEqual(1, CSGManager.TreeCount);
		Assert.AreEqual(4, CSGManager.TreeNodeCount);
	}
}