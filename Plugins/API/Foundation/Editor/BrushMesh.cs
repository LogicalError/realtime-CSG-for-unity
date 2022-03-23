using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Vector3 = UnityEngine.Vector3;
using Vector4 = UnityEngine.Vector4;

namespace RealtimeCSG.Foundation
{
	/// <summary>Flags that define how surfaces in a <see cref="RealtimeCSG.Foundation.BrushMesh"/> behave.</summary>
	/// <remarks><note>This enum is mirrored on the native side and cannot be modified.</note></remarks>
	/// <seealso cref="RealtimeCSG.Foundation.BrushMesh.Polygon"/>
	/// <seealso cref="RealtimeCSG.Foundation.BrushMesh"/>	
	[Serializable, Flags]
	public enum SurfaceFlags : int
	{
		/// <summary>The surface has no flags set</summary>
		None = 0,

		/// <summary>When set, the surface texture coordinates are calculated in world-space instead of brush-space</summary>
		TextureIsInWorldSpace = 1
	}

	/// <summary>A 2x4 matrix to calculate the UV coordinates for the vertices of a <see cref="RealtimeCSG.Foundation.BrushMesh"/>.</summary>
	/// <remarks><note>This struct is mirrored on the native side and cannot be modified.</note></remarks>
	/// <seealso cref="RealtimeCSG.Foundation.BrushMesh.Polygon"/>
	/// <seealso cref="RealtimeCSG.Foundation.BrushMesh"/>
	[Serializable, StructLayout(LayoutKind.Sequential, Pack = 4)]
	[DebuggerDisplay("U={U}, V={V}")]
	public struct UVMatrix
	{
		/// <summary>Used to convert a vertex coordinate to a U texture coordinate</summary>
		public Vector4 U;

		/// <value>Used to convert a vertex coordinate to a V texture coordinate</value>
		public Vector4 V;
	}

	/// <summary>Describes how the texture coordinates and normals are generated and if a surface is, for example, <see cref="RealtimeCSG.Foundation.LayerUsageFlags.Renderable"/> and/or <see cref="RealtimeCSG.Foundation.LayerUsageFlags.Collidable" /> etc.</summary>
	/// <remarks><note>This struct is mirrored on the native side and cannot be modified.</note></remarks>
	/// <seealso cref="RealtimeCSG.Foundation.BrushMesh.Polygon"/>
	/// <seealso cref="RealtimeCSG.Foundation.BrushMesh"/>
	[Serializable, StructLayout(LayoutKind.Sequential, Pack = 4)]
	[DebuggerDisplay("SurfaceFlags={surfaceFlags}, SmoothingGroup={smoothingGroup}, UV0={UV0}")]
	public struct SurfaceDescription
	{
		/// <value>The current normal smoothing group, 0 means that the surface doesn't do any smoothing</value>
		/// <remarks><note>This is only used when normals are set to be generated using the <see cref="RealtimeCSG.Foundation.VertexChannelFlags"/>.</note></remarks>
		public UInt32           smoothingGroup;

		/// <value>Surface specific flags</value>
		public SurfaceFlags     surfaceFlags;

		/// <value>2x4 matrix to calculate UV0 coordinates from vertex positions.</value>
		/// <remarks><note>This is only used when uv0 channels are set to be generated using the <see cref="RealtimeCSG.Foundation.VertexChannelFlags"/>.</note></remarks>
		public UVMatrix         UV0;


		// .. more UVMatrices can be added when more UV channels are supported
	}

	/// <summary>Contains a shape that can be used to initialize and update a <see cref="RealtimeCSG.Foundation.CSGTreeBrush"/>.</summary>
	/// <remarks>See the [Brush Meshes](~/documentation/brushMesh.md) article for more information.
	/// <note>This struct is safe to serialize.</note>
	/// <note>This struct is mirrored on the native side and cannot be modified.</note></remarks>
	/// <seealso cref="RealtimeCSG.Foundation.BrushMeshInstance"/>
	/// <seealso cref="RealtimeCSG.Foundation.BrushMeshInstance.Create"/>
	/// <seealso cref="RealtimeCSG.Foundation.BrushMeshInstance.Set"/>
	/// <seealso cref="RealtimeCSG.Foundation.CSGTreeBrush"/>
	[Serializable]
	[DebuggerDisplay("VertexCount={vertices.Length}, HalfEdgeCount={halfEdges.Length}, PolygonCount={polygons.Length}")]
	public sealed class BrushMesh
	{
		/// <summary>Defines the polygon of a <see cref="RealtimeCSG.Foundation.BrushMesh"/>.</summary>
		/// <remarks><note>This struct is mirrored on the native side and cannot be modified.</note></remarks>
		/// <seealso cref="RealtimeCSG.Foundation.BrushMesh"/>
		[Serializable, StructLayout(LayoutKind.Sequential, Pack = 4)]
		[DebuggerDisplay("PolygonID={polygonID}, EdgeCount={edgeCount}, FirstEdge={firstEdge}")]
		public struct Polygon
		{
			/// <value>The index to the first half edge that forms this <see cref="RealtimeCSG.Foundation.BrushMesh.Polygon"/>.</value>
			public Int32 firstEdge;
			
			/// <value>The number or edges of this <see cref="RealtimeCSG.Foundation.BrushMesh.Polygon"/>.</value>
			public Int32 edgeCount;
			
			/// <value>An ID that can be used to identify the <see cref="RealtimeCSG.Foundation.BrushMesh.Polygon"/>.</value>
			public Int32 polygonID;
			
			/// <value>Describes how normals and texture coordinates are created.</value>
			public SurfaceDescription surface;

			/// <value>Describes the surface layers that this <see cref="RealtimeCSG.Foundation.BrushMesh.Polygon"/> is part of, and, for example, what Materials it uses.</value>
			/// <seealso cref="RealtimeCSG.Foundation.MeshQuery"/>
			public SurfaceLayers layers;
		}

		/// <summary>Defines a half edge of a <see cref="RealtimeCSG.Foundation.BrushMesh"/>.</summary>
		/// <remarks><note>This struct is mirrored on the native side and cannot be modified.</note></remarks>
		/// <seealso cref="RealtimeCSG.Foundation.BrushMesh"/>
		[Serializable, StructLayout(LayoutKind.Sequential, Pack = 4)]
		[DebuggerDisplay("VertexIndex={vertexIndex}, Twin={twinIndex}")]
		public struct HalfEdge
		{
			/// <value>The index to the vertex of this <seealso cref="RealtimeCSG.Foundation.BrushMesh.HalfEdge"/>.</value>
			public Int32 vertexIndex;

			/// <value>The index to the twin <seealso cref="RealtimeCSG.Foundation.BrushMesh.HalfEdge"/> of this <seealso cref="RealtimeCSG.Foundation.BrushMesh.HalfEdge"/>.</value>
			public Int32 twinIndex;
		}
		
		/// <value>The vertices of this <see cref="RealtimeCSG.Foundation.BrushMesh"/>.</value> 
		public Vector3[]	vertices;

		/// <value>An array of <see cref="RealtimeCSG.Foundation.BrushMesh.HalfEdge"/> that define the edges of a <see cref="RealtimeCSG.Foundation.BrushMesh"/>.</value>
		public HalfEdge[]	halfEdges;
		
		/// <value>An array of <see cref="RealtimeCSG.Foundation.BrushMesh.Polygon"/> that define the polygons of a <see cref="RealtimeCSG.Foundation.BrushMesh"/>.</value>
		public Polygon[]	polygons;
	}
}