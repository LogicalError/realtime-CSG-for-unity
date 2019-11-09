using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using RealtimeCSG;
using RealtimeCSG.Foundation;
using UnityEditor.SceneManagement;

[TestFixture]
public partial class IndexOfTests
{
	[SetUp]
	public void Init()
	{
		CSGManager.Clear();
	}
	
	
	[Test]
	public void Tree_AddBrushes_HaveInOrderIndices()
	{
		const int brushUserID1 = 10;
		const int brushUserID2 = 11;
		const int brushUserID3 = 12;
		const int treeUserID = 13;
		var brush1 = CSGTreeBrush.Create(userID: brushUserID1);
		var brush2 = CSGTreeBrush.Create(userID: brushUserID2);
		var brush3 = CSGTreeBrush.Create(userID: brushUserID3);
		var tree  = CSGTree.Create(treeUserID);
		var result1 = tree.Add(brush1);
		var result2 = tree.Add(brush2);
		var result3 = tree.Add(brush3);
		CSGManager.ClearDirty(brush1);
		CSGManager.ClearDirty(brush2);
		CSGManager.ClearDirty(brush3);
		CSGManager.ClearDirty(tree);

		var index1 = tree.IndexOf(brush1);
		var index2 = tree.IndexOf(brush2);
		var index3 = tree.IndexOf(brush3);

		Assert.AreEqual(0, index1);//2
		Assert.AreEqual(1, index2);//3
		Assert.AreEqual(2, index3);//0
		Assert.AreEqual(true, result1);
		Assert.AreEqual(true, result2);
		Assert.AreEqual(true, result3);
		TestUtility.ExpectValidBrushWithUserID(ref brush1, brushUserID1);
		TestUtility.ExpectValidBrushWithUserID(ref brush2, brushUserID2);
		TestUtility.ExpectValidBrushWithUserID(ref brush3, brushUserID3);
		TestUtility.ExpectValidTreeWithUserID(ref tree, treeUserID);
		Assert.AreEqual(false, tree.Dirty);
		Assert.AreEqual(0,            brush1.Parent.NodeID);
		Assert.AreEqual(tree.NodeID, brush1.Tree.NodeID);
		Assert.AreEqual(0,            brush2.Parent.NodeID);
		Assert.AreEqual(tree.NodeID, brush2.Tree.NodeID);
		Assert.AreEqual(0,            brush3.Parent.NodeID);
		Assert.AreEqual(tree.NodeID, brush3.Tree.NodeID);
		Assert.AreEqual(3,            tree.Count);
		Assert.AreEqual(0, CSGManager.TreeBranchCount);
		Assert.AreEqual(3, CSGManager.TreeBrushCount);
		Assert.AreEqual(1, CSGManager.TreeCount);
		Assert.AreEqual(4, CSGManager.TreeNodeCount);
	}


	[Test]
	public void Branch_AddBrushes_HaveInOrderIndices()
	{
		const int brushUserID1 = 10;
		const int brushUserID2 = 11;
		const int brushUserID3 = 12;
		const int branchUserID = 13;
		var brush1 = CSGTreeBrush.Create(userID: brushUserID1);
		var brush2 = CSGTreeBrush.Create(userID: brushUserID2);
		var brush3 = CSGTreeBrush.Create(userID: brushUserID3);
		var branch = CSGTreeBranch.Create(branchUserID);
		var result1 = branch.Add(brush1);
		var result2 = branch.Add(brush2);
		var result3 = branch.Add(brush3);
		CSGManager.ClearDirty(brush1);
		CSGManager.ClearDirty(brush2);
		CSGManager.ClearDirty(brush3);
		CSGManager.ClearDirty(branch);

		var index1 = branch.IndexOf(brush1);
		var index2 = branch.IndexOf(brush2);
		var index3 = branch.IndexOf(brush3);

		Assert.AreEqual(0, index1);
		Assert.AreEqual(1, index2);
		Assert.AreEqual(2, index3);
		Assert.AreEqual(true, result1);
		Assert.AreEqual(true, result2);
		Assert.AreEqual(true, result3);
		TestUtility.ExpectValidBrushWithUserID(ref brush1, brushUserID1);
		TestUtility.ExpectValidBrushWithUserID(ref brush2, brushUserID2);
		TestUtility.ExpectValidBrushWithUserID(ref brush3, brushUserID3);
		TestUtility.ExpectValidBranchWithUserID(ref branch, branchUserID);
		Assert.AreEqual(false, branch.Dirty);;
		Assert.AreEqual(branch.NodeID, brush1.Parent.NodeID);
		Assert.AreEqual(0,				  brush1.Tree.NodeID);
		Assert.AreEqual(branch.NodeID, brush2.Parent.NodeID);
		Assert.AreEqual(0,				  brush2.Tree.NodeID);
		Assert.AreEqual(branch.NodeID, brush3.Parent.NodeID);
		Assert.AreEqual(0,                brush3.Tree.NodeID);
		Assert.AreEqual(3,				  branch.Count);
		Assert.AreEqual(0, CSGManager.TreeCount);
		Assert.AreEqual(1, CSGManager.TreeBranchCount);
		Assert.AreEqual(3, CSGManager.TreeBrushCount);
		Assert.AreEqual(4, CSGManager.TreeNodeCount);
	}

	
	[Test]
	public void Tree_IndexOfNonChildBrush_IsNegativeOne()
	{
		const int brushUserID1 = 10;
		const int brushUserID2 = 11;
		const int brushUserID3 = 12;
		const int treeUserID = 13;
		var brush1 = CSGTreeBrush.Create(userID: brushUserID1);
		var brush2 = CSGTreeBrush.Create(userID: brushUserID2);
		var brush3 = CSGTreeBrush.Create(userID: brushUserID3);
		var tree  = CSGTree.Create(treeUserID);
		tree.Add(brush1);
		tree.Add(brush2);
		CSGManager.ClearDirty(brush1);
		CSGManager.ClearDirty(brush2);
		CSGManager.ClearDirty(tree);

		var index = tree.IndexOf(brush3);

		Assert.AreEqual(-1, index);
	}

}