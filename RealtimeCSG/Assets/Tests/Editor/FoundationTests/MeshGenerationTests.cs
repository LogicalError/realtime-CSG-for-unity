using UnityEngine;
using NUnit.Framework;
using RealtimeCSG;
using RealtimeCSG.Legacy;
using RealtimeCSG.Foundation;

[TestFixture]
public partial class MeshGenerationTests
{
	static Material material1;
	static Material material2;
	static int		materialID1 = -1;
	static int		materialID2  = -1;

	const VertexChannelFlags invalidVertexChannels = (VertexChannelFlags)0;
	readonly static MeshQuery[] simpleMeshTypes = new MeshQuery[]
		{
			new MeshQuery(LayerUsageFlags.Collidable, vertexChannels: VertexChannelFlags.Position | VertexChannelFlags.UV0)
		};
	readonly static MeshQuery[] materialMeshTypes = new MeshQuery[]
		{
			new MeshQuery(LayerUsageFlags.Renderable, parameterIndex: LayerParameterIndex.RenderMaterial, vertexChannels: VertexChannelFlags.Position | VertexChannelFlags.UV0)
		};

	const int cubeIndexCount  = 6 * (2 * 3); // 6 sides, 2 triangles per side, 3 indices per triangle
	const int cubeVertexCount = 6 * (4    ); // 6 sides, 4 vertices per side
	

	[SetUp]
	public void Init()
	{
		material1	= MaterialUtility.FloorMaterial;
		material2	= MaterialUtility.WallMaterial;
		
		Assert.NotNull(material1); 
		Assert.NotNull(material2); 

		materialID1 = material1.GetInstanceID();
		materialID2	= material2.GetInstanceID();
		CSGManager.Clear();
	}

	#region Helpers
	static BrushMeshInstance CreateCube(Vector3 size, CSGOperationType operation = CSGOperationType.Additive, Material material = null)
	{
		if (material == null)
			material = material2;
		ControlMesh controlMesh;
		Shape shape;
		BrushFactory.CreateCubeControlMesh(out controlMesh, out shape, size, material);
		BrushMesh brushMesh = RealtimeCSG.Legacy.BrushFactory.GenerateFromControlMesh(controlMesh, shape);
		return BrushMeshInstance.Create(brushMesh);
	}

	static CSGTreeBrush CreateCubeBrush(CSGOperationType operation = CSGOperationType.Additive, Material material = null)
	{
		return CreateCubeBrush(Vector3.one, operation, material);
	}

	static CSGTreeBrush CreateCubeBrush(Vector3 size, CSGOperationType operation = CSGOperationType.Additive, Material material = null)
	{
		return CSGTreeBrush.Create(operation: operation, brushMesh: CreateCube(size, operation, material ?? material2));
	}
	
	
	static GeneratedMeshContents GeneratedMeshAndValidate(CSGTree tree, MeshQuery[] meshTypes, bool expectEmpty = false)
	{
		GeneratedMeshContents       generatedMesh       = null;
		GeneratedMeshDescription[]  meshDescriptions    = null;
		bool treeWasDirtyBefore	= false;
		bool treeIsDirtyAfter	= true; 
		
		tree.SetDirty();
		bool haveChanges			= CSGManager.Flush(); // Note: optional
		if (haveChanges)
		{ 
			treeWasDirtyBefore		= tree.Dirty; // Note: optional
			if (treeWasDirtyBefore)
			{
				meshDescriptions	= tree.GetMeshDescriptions(meshTypes);
				if (meshDescriptions != null)
				{
					var meshDescription = meshDescriptions[0];
					generatedMesh	= tree.GetGeneratedMesh(meshDescription);
				}
				treeIsDirtyAfter	= tree.Dirty; 
			}
		}		

		Assert.IsTrue(haveChanges); 
		Assert.IsTrue(treeWasDirtyBefore);
		Assert.IsFalse(treeIsDirtyAfter);
		if (expectEmpty)
		{
			Assert.Null(meshDescriptions);
			Assert.Null(generatedMesh);
		} else
		{
			Assert.NotNull(meshDescriptions);
			Assert.NotNull(generatedMesh);
			Assert.AreEqual(meshDescriptions[0].meshQuery, meshTypes[0]);
			Assert.AreEqual(simpleMeshTypes.Length, meshDescriptions.Length);
			Assert.IsTrue(generatedMesh.description.vertexCount > 0 && 
						  generatedMesh.description.indexCount > 0);
		}
		return generatedMesh;
	}

	static void ValidateIsCorrectCube(GeneratedMeshContents generatedMesh)
	{
		Assert.NotNull(generatedMesh.indices);
		Assert.NotNull(generatedMesh.positions);
		Assert.AreEqual(cubeIndexCount, generatedMesh.indices.Length);		// 6 sides, 2 triangles per side, 3 indices per triangle
		Assert.AreEqual(cubeVertexCount, generatedMesh.positions.Length);	// 6 sides, 4 vertices per side
		
		var vertexChannels = generatedMesh.description.meshQuery.UsedVertexChannels;
//		if ((vertexChannels & VertexChannelFlags.Color) == VertexChannelFlags.Color)
//		{
//			Assert.NotNull(generatedMesh.colors);
//			Assert.AreEqual(generatedMesh.positions.Length, generatedMesh.colors.Length);
//		} else
//			Assert.Null(generatedMesh.colors);

		if ((vertexChannels & VertexChannelFlags.Tangent) == VertexChannelFlags.Tangent)
		{
			Assert.NotNull(generatedMesh.tangents);
			Assert.AreEqual(generatedMesh.positions.Length, generatedMesh.tangents.Length);
		} else
			Assert.Null(generatedMesh.tangents);

		if ((vertexChannels & VertexChannelFlags.Normal) == VertexChannelFlags.Normal)
		{
			Assert.NotNull(generatedMesh.normals);
			Assert.AreEqual(generatedMesh.positions.Length, generatedMesh.normals.Length);
		} else
			Assert.Null(generatedMesh.normals);

		if ((vertexChannels & VertexChannelFlags.UV0) == VertexChannelFlags.UV0)
		{
			Assert.NotNull(generatedMesh.uv0);
			Assert.AreEqual(generatedMesh.positions.Length, generatedMesh.uv0.Length);
		} else
			Assert.Null(generatedMesh.uv0);
	}
	#endregion


	[Test]
	public void Mesh_CreateAdditiveCubeBrush_RetrieveMesh_RetrievedMeshIsACube()
	{
		var tree = CSGTree.Create(
			CreateCubeBrush(operation: CSGOperationType.Additive)
		);

		GeneratedMeshContents generatedMesh = GeneratedMeshAndValidate(tree, simpleMeshTypes, expectEmpty: false);

		ValidateIsCorrectCube(generatedMesh);
	}
	
	[Test]
	public void Mesh_CreateSubtractiveCubeBrush_RetrieveMesh_RetrievedMeshIsEmpty()
	{
		var tree = CSGTree.Create(
			CreateCubeBrush(operation: CSGOperationType.Subtractive)
		);

		GeneratedMeshContents generatedMesh = GeneratedMeshAndValidate(tree, simpleMeshTypes, expectEmpty: true);
		
		Assert.Null(generatedMesh);
	}
	
	[Test]
	public void Mesh_CreateIntersectionCubeBrush_RetrieveMesh_RetrievedMeshIsEmpty()
	{
		var tree = CSGTree.Create(
			CreateCubeBrush(operation: CSGOperationType.Intersecting)
		);

		GeneratedMeshContents generatedMesh = GeneratedMeshAndValidate(tree, simpleMeshTypes, expectEmpty: true);
		
		Assert.Null(generatedMesh);
	}
	
	[Test]
	public void Mesh_AdditiveCubeBrushWithSubtractiveCubeBrush_RetrievedMeshIsEmpty()
	{
		var tree = CSGTree.Create(
			CreateCubeBrush(operation: CSGOperationType.Additive),
			CreateCubeBrush(operation: CSGOperationType.Subtractive)
		);
		
		GeneratedMeshContents generatedMesh = GeneratedMeshAndValidate(tree, simpleMeshTypes, expectEmpty: true);

		Assert.Null(generatedMesh);
	}
	
	[Test]
	public void Mesh_SubtractiveCubeBrushWithAdditiveCubeBrush_RetrievedMeshIsCube()
	{
		var tree = CSGTree.Create(
			CreateCubeBrush(operation: CSGOperationType.Subtractive, material: material2),
			CreateCubeBrush(operation: CSGOperationType.Additive,    material: material1)
		);
		
		GeneratedMeshContents generatedMesh = GeneratedMeshAndValidate(tree, materialMeshTypes, expectEmpty: false);
		
		ValidateIsCorrectCube(generatedMesh);
		Assert.AreEqual(materialID1, generatedMesh.description.surfaceParameter);
	}
	
	[Test]
	public void Mesh_IntersectingCubeBrushWithAdditiveCubeBrush_RetrievedMeshIsEmpty()
	{
		var tree = CSGTree.Create(
			CreateCubeBrush(operation: CSGOperationType.Intersecting, material: material1),
			CreateCubeBrush(operation: CSGOperationType.Additive,     material: material2)
		);
		
		GeneratedMeshContents generatedMesh = GeneratedMeshAndValidate(tree, materialMeshTypes, expectEmpty: false);
		
		ValidateIsCorrectCube(generatedMesh);
		Assert.AreEqual(materialID2, generatedMesh.description.surfaceParameter);
	}
	
	[Test]
	public void Mesh_AdditiveCubeBrushWithAdditiveCubeBrush_RetrievedMeshHasLastBrushMaterial()
	{
		var tree = CSGTree.Create(
			CreateCubeBrush(operation: CSGOperationType.Additive, material: material2),
			CreateCubeBrush(operation: CSGOperationType.Additive, material: material1)
		);
		
		GeneratedMeshContents generatedMesh = GeneratedMeshAndValidate(tree, materialMeshTypes, expectEmpty: false);
		 
		ValidateIsCorrectCube(generatedMesh);
		Assert.AreEqual(materialID1, generatedMesh.description.surfaceParameter);
	}
	
	[Test]
	public void Mesh_AdditiveCubeBrushOverlapsWithIntersectingCubeBrush_RetrievedMeshHasIntersectingCubeMaterial()
	{
		var tree = CSGTree.Create(
			CreateCubeBrush(operation: CSGOperationType.Additive,     material: material2),
			CreateCubeBrush(operation: CSGOperationType.Intersecting, material: material1)
		);
				
		GeneratedMeshContents generatedMesh = GeneratedMeshAndValidate(tree, materialMeshTypes, expectEmpty: false);
		 
		ValidateIsCorrectCube(generatedMesh);
		Assert.AreEqual(materialID2, generatedMesh.description.surfaceParameter);
	}
	
	[Test]
	public void Mesh_LargeAdditiveCubeBrushWithIntersectingCubeBrush_RetrievedMeshHasIntersectingCubeMaterial()
	{
		var tree = CSGTree.Create(
			CreateCubeBrush(operation: CSGOperationType.Additive,     material: material2, size: Vector3.one*2),
			CreateCubeBrush(operation: CSGOperationType.Intersecting, material: material1)
		);
		
		GeneratedMeshContents generatedMesh = GeneratedMeshAndValidate(tree, materialMeshTypes, expectEmpty: false);
		
		ValidateIsCorrectCube(generatedMesh);
		Assert.AreEqual(materialID1, generatedMesh.description.surfaceParameter);
	}
}
