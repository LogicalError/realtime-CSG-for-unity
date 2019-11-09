using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using RealtimeCSG;
using UnityEditor.SceneManagement;
using RealtimeCSG.Foundation;

[TestFixture]
public partial class SetGetBrushInfiniteTests
{
	[SetUp]
	public void Init()
	{
		CSGManager.Clear();
	}

	[Test]
	public void Brush_SetInfinite_GetInfiniteIsSame()
	{
		var brush1 = CSGTreeBrush.Create();
		var brush2 = CSGTreeBrush.Create();
		brush1.Flags = CSGTreeBrushFlags.Default;
		brush2.Flags = CSGTreeBrushFlags.Infinite;
		CSGManager.ClearDirty(brush1);
		CSGManager.ClearDirty(brush2);

		brush1.Flags = CSGTreeBrushFlags.Infinite;
		brush2.Flags = CSGTreeBrushFlags.Default;

		Assert.AreEqual(CSGTreeBrushFlags.Infinite, brush1.Flags);
		Assert.AreEqual(CSGTreeBrushFlags.Default, brush2.Flags);
		Assert.AreEqual(true, brush1.Dirty);
		Assert.AreEqual(true, brush2.Dirty);
	}

	[Test]
	public void Brush_SetInfiniteToTrueAndFalse_GetInfiniteIsFalse()
	{
		var brush = CSGTreeBrush.Create();
		CSGManager.ClearDirty(brush);

		brush.Flags = CSGTreeBrushFlags.Infinite;
		brush.Flags = CSGTreeBrushFlags.Default;

		Assert.AreEqual(CSGTreeBrushFlags.Default, brush.Flags);
		Assert.AreEqual(true, brush.Dirty);
	}

	[Test]
	public void Brush_SetInfiniteToFalseAndTrue_GetInfiniteIsTrue()
	{
		var brush = CSGTreeBrush.Create();
		CSGManager.ClearDirty(brush);

		brush.Flags = CSGTreeBrushFlags.Default;
		brush.Flags = CSGTreeBrushFlags.Infinite;

		Assert.AreEqual(CSGTreeBrushFlags.Infinite, brush.Flags);
	}

	[Test]
	public void Brush_DefaultInfinite_IsFalse()
	{
		var brush = CSGTreeBrush.Create();

		Assert.AreEqual(CSGTreeBrushFlags.Default, brush.Flags);
	}
}
