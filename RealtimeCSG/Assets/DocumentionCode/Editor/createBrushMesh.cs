using RealtimeCSG.Foundation;
using UnityEngine;

partial class GenerateCSGTreeExample
{
	public BrushMeshInstance CreateCube(int userID)
	{
		BrushMesh         cube             = RealtimeCSG.Legacy.BrushFactory.CreateCube(Vector3.one);
		BrushMeshInstance cubeMeshInstance = BrushMeshInstance.Create(cube, userID);

		return cubeMeshInstance;
	}
}
