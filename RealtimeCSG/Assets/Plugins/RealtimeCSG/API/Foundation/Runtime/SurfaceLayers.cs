using System;
using System.Runtime.InteropServices;

namespace RealtimeCSG.Foundation
{
	/// <summary>Define which layers of <see cref="RealtimeCSG.Foundation.BrushMesh.Polygon"/>s of <seealso cref="RealtimeCSG.Foundation.BrushMesh"/>es that should be combined to create meshes.</summary>
	/// <remarks>
	/// <note>
	/// The CSG process has no concept of, for instance, <see cref="RealtimeCSG.Foundation.LayerUsageFlags.Renderable"/> or <see cref="RealtimeCSG.Foundation.LayerUsageFlags.Collidable"/> 
	/// flags and just compares the bits set on the <see cref="RealtimeCSG.Foundation.SurfaceLayers.layerUsage"/> of the <see cref="RealtimeCSG.Foundation.BrushMesh.Polygon"/>s with the bits set in the <see cref="RealtimeCSG.Foundation.MeshQuery"/>.
	/// </note>
	/// <note>
	/// Only bits 0-23 can be used for layers, the 24th bit is used to find <see cref="RealtimeCSG.Foundation.LayerUsageFlags.Culled"/> polygons.
	/// </note>
	/// </remarks>
	/// <seealso cref="RealtimeCSG.Foundation.BrushMesh"/>
	/// <seealso cref="RealtimeCSG.Foundation.BrushMesh.Polygon"/>
	/// <seealso cref="RealtimeCSG.Foundation.MeshQuery"/>
	[Serializable, Flags]
	public enum LayerUsageFlags : int // 24 bits max
	{
		/// <summary>No layers, can be used to find <see cref="RealtimeCSG.Foundation.BrushMesh.Polygon"/>s that are not assigned to any layers.</summary>
		/// <remarks>Can be used to find all the polygons that have been set to be 'hidden'.</remarks>
		None						= 0,
		
		/// <summary>Find the polygons that: are visible</summary>
		Renderable					= (int)((uint)1 <<  0),

		/// <summary>Find the polygons that: cast shadows</summary>
		CastShadows					= (int)((uint)1 <<  1),

		/// <summary>Find the polygons that: receive shadows</summary>
		ReceiveShadows				= (int)((uint)1 <<  2),

		/// <summary>Find the polygons that: are part of a collider</summary>
		Collidable					= (int)((uint)1 <<  3),

		/// <summary>Find the polygons that: are visible and cast shadows.</summary>
		RenderCastShadows			= Renderable | CastShadows,

		/// <summary>Find the polygons that: are visible and receive shadows.</summary>
		RenderReceiveShadows		= Renderable | ReceiveShadows,

		/// <summary>
		/// Find the polygons that: are visible, cast shadows and receive shadows.
		/// </summary>
		RenderReceiveCastShadows	= Renderable | CastShadows | ReceiveShadows,

		/// <summary>Find polygons that have been removed by the CSG process, this can be used for debugging.</summary>
		Culled						= (int)((uint)1 << 23)
	};

	/// <summary>Index into one of the parameters of a SurfaceLayers</summary>
	/// <remarks>Used to generate a mesh, by querying for a specific surface layer parameter index.
	/// For example, the first layer parameter could be an index to a specific Material to render, 
	/// which could be used by all surface types that are renderable.
	/// <note>This enum is mirrored on the native side and cannot be modified. The number of LayerParameters is the same as in <see cref="RealtimeCSG.Foundation.SurfaceLayers"/>.</note>
	/// </remarks>
	/// <seealso cref="RealtimeCSG.Foundation.BrushMesh"/>
	/// <seealso cref="RealtimeCSG.Foundation.BrushMesh.Polygon"/>
	/// <seealso cref="RealtimeCSG.Foundation.SurfaceLayers"/>
	[Serializable]
	public enum LayerParameterIndex : byte
	{
		/// <summary>No parameter index is used</summary>
		None				= 0,

		/// <summary>Find polygons and create a mesh for each unique <see cref="RealtimeCSG.Foundation.SurfaceLayers.layerParameter1"/>.</summary>
		/// <seealso cref="RealtimeCSG.Foundation.SurfaceLayers.layerParameter1"/>.
		LayerParameter1 = 1,

		/// <summary>Find polygons and create a mesh for each unique <see cref="RealtimeCSG.Foundation.SurfaceLayers.layerParameter2"/>.</summary>
		/// <seealso cref="RealtimeCSG.Foundation.SurfaceLayers.layerParameter2"/>.
		LayerParameter2 = 2,

		/// <summary>Find polygons and create a mesh for each unique <see cref="RealtimeCSG.Foundation.SurfaceLayers.layerParameter3"/>.</summary>
		/// <seealso cref="RealtimeCSG.Foundation.SurfaceLayers.layerParameter3"/>.
		LayerParameter3 = 3,


		// Human readable versions of the above categories


		/// <summary>Find polygons and create a mesh for each unique Material</summary>
		/// <remarks>alias of <see cref="RealtimeCSG.Foundation.LayerParameterIndex.LayerParameter1"/>.</remarks>
		/// <seealso cref="RealtimeCSG.Foundation.SurfaceLayers.layerParameter1"/>.
		RenderMaterial = LayerParameter1,

		/// <summary>Find polygons and create a mesh for each unique PhysicMaterial</summary>
		/// <remarks>alias of <see cref="RealtimeCSG.Foundation.LayerParameterIndex.LayerParameter2"/>.</remarks>
		/// <seealso cref="RealtimeCSG.Foundation.SurfaceLayers.layerParameter2"/>.
		PhysicsMaterial = LayerParameter2
	};

	/// <summary>This struct describes what layers a surface is part of, and user set layer indices</summary>
	/// <remarks>Setting layer indices can be used to, for example, assign things like [Material](https://docs.unity3d.com/ScriptReference/Material.html)s and [PhysicMaterial](https://docs.unity3d.com/ScriptReference/PhysicMaterial.html)s to a surface.
	/// Currently only 3 layer indices are supported, more might be added in the future.
	/// See the [Create Unity Meshes](~/documentation/createUnityMesh.md) article for more information.
	/// <note>This struct is mirrored on the native side and cannot be modified. The number of LayerParameters is the same as in <see cref="RealtimeCSG.Foundation.LayerParameterIndex"/>.</note>
	/// </remarks>
	/// <seealso cref="RealtimeCSG.Foundation.BrushMesh"/>
	/// <seealso cref="RealtimeCSG.Foundation.BrushMesh.Polygon"/>
	/// <seealso cref="RealtimeCSG.Foundation.MeshQuery"/>
	/// <seealso cref="RealtimeCSG.Foundation.LayerUsageFlags"/>
	/// <seealso cref="RealtimeCSG.Foundation.LayerParameterIndex"/>
	[Serializable, StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct SurfaceLayers
	{
		/// <value>Describe to what layers this surface belongs.</value>
		/// <remarks>Can be used to define if the surface is, for example, <see cref="RealtimeCSG.Foundation.LayerUsageFlags.Renderable"/> and/or <see cref="RealtimeCSG.Foundation.LayerUsageFlags.Collidable" /> etc.</remarks>
		public LayerUsageFlags	layerUsage;

		/// <value>First layer-parameter.</value>
		/// <remarks>Could be, for instance, an instanceID to a [Material](https://docs.unity3d.com/ScriptReference/Material.html), which can then be found using [EditorUtility.InstanceIDToObject](https://docs.unity3d.com/ScriptReference/EditorUtility.InstanceIDToObject.html)
		/// A value of 0 means that it's not set.
		/// <code>
		///	mySurfaceLayer.<paramref name="layerParameter1"/> = myMaterial.GetInstanceID();
		///	... generate your mesh ...
		///	Material myMaterial = EditorUtility.InstanceIDToObject(myGeneratedMeshContents.surfaceParameter);
		/// </code>
		/// </remarks>
		/// <seealso cref="RealtimeCSG.Foundation.LayerParameterIndex.LayerParameter1"/>.
		public Int32			layerParameter1;

		/// <value>Second layer-parameter.</value>
		/// <remarks>Could be, for instance, an instanceID to a [PhysicMaterial](https://docs.unity3d.com/ScriptReference/PhysicMaterial.html), which can then be found using [EditorUtility.InstanceIDToObject](https://docs.unity3d.com/ScriptReference/EditorUtility.InstanceIDToObject.html)
		/// A value of 0 means that it's not set.
		/// <code>
		///	mySurfaceLayer.<paramref name="layerParameter2"/> = myPhysicMaterial.GetInstanceID();
		///	... generate your mesh ...
		///	PhysicMaterial myMaterial = EditorUtility.InstanceIDToObject(myGeneratedMeshContents.surfaceParameter);
		/// </code>
		/// </remarks>
		/// <seealso cref="RealtimeCSG.Foundation.LayerParameterIndex.LayerParameter2"/>.
		public Int32			layerParameter2;

		/// <value>Third layer-parameter.</value>
		/// <remarks>A value of 0 means that it's not set.</remarks>
		public Int32			layerParameter3;

		// .. this could be extended in the future, when necessary
	}
}
