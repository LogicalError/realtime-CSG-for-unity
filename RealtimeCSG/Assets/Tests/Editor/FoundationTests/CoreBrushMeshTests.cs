using UnityEngine;
using NUnit.Framework;
using RealtimeCSG;
using RealtimeCSG.Foundation;
using RealtimeCSG.Legacy;

[TestFixture]
public partial class CoreBrushMeshTests
{
	[SetUp]
	public void Init()
	{
		CSGManager.Clear();
	}

	[Test]
	public void CreateBrushMeshFromControlMesh()
	{
		ControlMesh controlMesh;
		Shape shape;
		BrushFactory.CreateCubeControlMesh(out controlMesh, out shape, Vector3.one);

		BrushMesh brushMesh = RealtimeCSG.Legacy.BrushFactory.GenerateFromControlMesh(controlMesh, shape);
		
		Assert.AreNotEqual(null, brushMesh);
		Assert.AreNotEqual(null, brushMesh.vertices);
		Assert.AreNotEqual(null, brushMesh.halfEdges);
		Assert.AreNotEqual(null, brushMesh.polygons);
		Assert.AreEqual(8, brushMesh.vertices.Length);
		Assert.AreEqual(24, brushMesh.halfEdges.Length);
		Assert.AreEqual(6, brushMesh.polygons.Length);
	}

	[Test]
	public void CreateCoreBrushMeshFromControlMesh()
	{
		ControlMesh controlMesh;
		Shape shape;
		BrushFactory.CreateCubeControlMesh(out controlMesh, out shape, Vector3.one);
		BrushMesh brushMesh = RealtimeCSG.Legacy.BrushFactory.GenerateFromControlMesh(controlMesh, shape);

		BrushMeshInstance coreBrushMesh = BrushMeshInstance.Create(brushMesh);
		
		Assert.AreEqual(true, coreBrushMesh.Valid);
	}

	[Test]
	public void DestroyCoreBrushMesh()
	{
		ControlMesh controlMesh;
		Shape shape;
		BrushFactory.CreateCubeControlMesh(out controlMesh, out shape, Vector3.one);
		BrushMesh brushMesh = RealtimeCSG.Legacy.BrushFactory.GenerateFromControlMesh(controlMesh, shape);
		BrushMeshInstance coreBrushMesh = BrushMeshInstance.Create(brushMesh);

		coreBrushMesh.Destroy();

		Assert.AreEqual(false, coreBrushMesh.Valid);
	}


	[Test]
	public void CreateCoreBrushMesh_WithNullBrushMesh_IsNotValid()
	{
		BrushMeshInstance coreBrushMesh = BrushMeshInstance.Create(null);

		Assert.AreEqual(false, coreBrushMesh.Valid);
	}
}
