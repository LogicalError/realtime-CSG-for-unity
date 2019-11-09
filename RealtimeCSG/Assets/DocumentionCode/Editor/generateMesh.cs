using RealtimeCSG.Foundation;
using UnityEngine;

partial class GenerateCSGTreeExample
{
	public void GenerateMesh()
	{
		// The mesh queries define how meshes are generated using the BrushMesh surface layer information
		MeshQuery[] meshQueries = GetMeshQueries();


		// Create a tree
		// Note: this can be cached and updated separately.
		CSGTree tree = CreateTree();

		
		// At this point, when you have a tree that you update frequently, you can also potentially check if it's Dirty or not.
		// If it is, it means it has been modified since the last time tree.GetMeshDescriptions has been called.


		// Find all the potential meshes that could be generated with our queries.
		// There is no guarantee that these meshes will exist. It might also be split into multiple meshes when it's too large
		GeneratedMeshDescription[] meshDescriptions = tree.GetMeshDescriptions(meshQueries, VertexChannelFlags.All);

		if (meshDescriptions != null)
		{
			// Iterate through all the meshes that we've found
			foreach (var meshDescription in meshDescriptions)
			{
				// At this point, it's possible to check the Hash values stored in meshDescription, 
				// to check if this particular type of mesh has already been generated before (perhaps in a previous iteration)
				// You could then potentially early out and avoid retrieving the generated mesh and converting it into a new UnityEngine.Mesh.

				// Actually generate the mesh on the native side, potentially generate smooth normals etc. and retrieve the mesh.
				GeneratedMeshContents contents = tree.GetGeneratedMesh(meshDescription);

				// Note: It's possible to re-use this UnityEngine.Mesh
				UnityEngine.Mesh unityMesh = new UnityEngine.Mesh();

				// Copy the generated mesh to the UnityEngine.Mesh
				contents.CopyTo(unityMesh);

				// ... assign it to a MeshRenderer/MeshCollider etc.
			}
		}
	}
}
