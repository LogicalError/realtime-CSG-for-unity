using RealtimeCSG.Foundation;
using UnityEngine;

partial class GenerateCSGTreeExample
{
	public MeshQuery[] GetMeshQueries()
	{
		// The mesh queries define how meshes are generated using 
		// the BrushMesh surface layer information
		MeshQuery[] meshQueries = 
		{			
			new MeshQuery(
				// Create meshes, one for each surface layer (layerParameter1) 
				// with a different type of 'Material'. 
				// Does not select surface layers with a layerParameter1 of 0.
				// Ignores all other layer parameters.
				parameterIndex: LayerParameterIndex.RenderMaterial,

				// Uses all vertex channels.
				vertexChannels: VertexChannelFlags.All,					
				
				// Find surface layers with 'Renderable', 'CastShadows' 
				// and/or 'ReceiveShadows' flags.
				mask:           LayerUsageFlags.RenderReceiveCastShadows,

				// Only select the ones that have them all set.
				query:			LayerUsageFlags.RenderReceiveCastShadows
			),


			new MeshQuery(
				// Create meshes, one for each surface layer (layerParameter1)
				// with a different type of 'Material'. 
				// Does not select surface layers with a layerParameter1 of 0.
				// Ignores all other layer parameters.
				parameterIndex: LayerParameterIndex.RenderMaterial,

				// Uses all vertex channels.
				vertexChannels: VertexChannelFlags.All,

				// Find surface layers with 'Renderable', 'CastShadows' 
				// and/or 'ReceiveShadows' flags.
				mask:           LayerUsageFlags.RenderReceiveCastShadows,

				// Only select the ones that have both 'Renderable' and 
				// 'Cast Shadows' set. Will reject any surface layer that 
				// has the 'ReceiveShadows' flag set.
				query:			LayerUsageFlags.RenderCastShadows
			),

			new MeshQuery(			
				// Create meshes, one for each surface layer (layerParameter1) 
				// with a different type of 'Material'. 
				// Does not select surface layers with a layerParameter1 of 0.
				// Ignores all other layer parameters.
				parameterIndex: LayerParameterIndex.RenderMaterial,

				// Uses all vertex channels.
				vertexChannels: VertexChannelFlags.All,

				// Find surface layers with 'Renderable', 'CastShadows' 
				// and/or 'ReceiveShadows' flags.
				mask:           LayerUsageFlags.RenderReceiveCastShadows,
				
				// Only select the ones that have both 'Renderable' and 
				// 'Receive Shadows' set. Will reject any surface layer 
				// that has the 'CastShadows' flag set.
				query:			LayerUsageFlags.RenderReceiveShadows
			),

			new MeshQuery(											
				// Create meshes, one for each surface layer (layerParameter1) 
				// with a different type of 'Material'. 
				// Does not select surface layers with a layerParameter1 of 0.
				// Ignores all other layer parameters.
				parameterIndex: LayerParameterIndex.RenderMaterial,

				// Uses all vertex channels.
				vertexChannels: VertexChannelFlags.All,

				// Find surface layers with 'Renderable', 'CastShadows' 
				// and/or 'ReceiveShadows' flags.
				mask:           LayerUsageFlags.RenderReceiveCastShadows,

				// Only select the ones that have just 'Renderable' set.
				// Will reject any surface layer that has 'CastShadows' 
				// or 'ReceiveShadows' flag set.
				query:			LayerUsageFlags.Renderable
			),

			new MeshQuery(				
				// Create meshes for this query. 
				// Ignores all layerParameters.
				parameterIndex: LayerParameterIndex.None,

				// Only uses position vertex channel.
				vertexChannels: VertexChannelFlags.Position,

				// Find surface layers with 'Renderable' and 'CastShadows' 
				mask:           LayerUsageFlags.RenderCastShadows,

				// Only select the ones that have just 'Cast Shadows' set.
				// Will reject any surface layer that has 'Renderable' set.
				query:			LayerUsageFlags.CastShadows
			),
			
			new MeshQuery(
				// Create meshes, one for each surface layer (layerParameter2) 
				// with a different type of 'PhysicMaterial'. 
				// Does not select surface layers with a layerParameter2 of 0.
				// Ignores all other layer parameters.
				parameterIndex: LayerParameterIndex.PhysicsMaterial,

				// Only uses position vertex channel.
				vertexChannels: VertexChannelFlags.Position,

				// Find surface layers with 'Collidable' flag set.
				mask:			LayerUsageFlags.Collidable,

				// Select the ones that have the 'Collidable' flag set.
				query:          LayerUsageFlags.Collidable
			),
		};

		return meshQueries;
	}
}
