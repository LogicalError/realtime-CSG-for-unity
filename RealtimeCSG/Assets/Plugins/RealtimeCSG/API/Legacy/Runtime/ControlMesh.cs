using InternalRealtimeCSG;
using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace RealtimeCSG.Legacy
{
	/// <summary>A <see cref="RealtimeCSG.Legacy.HalfEdge"/> of a <see cref="RealtimeCSG.Legacy.ControlMesh"/>.</summary>
	/// <remarks><note>This code will be replaced by pure <see cref="RealtimeCSG.Foundation.BrushMesh"/> code eventually.</note>
	/// <note>This class can be safely serialized.</note></remarks>
	/// <seealso cref="RealtimeCSG.Legacy.ControlMesh"/>
	/// <seealso cref="RealtimeCSG.Legacy.Polygon"/>
	/// <seealso cref="RealtimeCSG.Legacy.Shape"/>
	[Serializable]
	public struct HalfEdge
	{
		/// <summary>Creates a HalfEdge for a <see cref="RealtimeCSG.Legacy.ControlMesh"/></summary>
		/// <param name="polygonIndex">The index to the <see cref="RealtimeCSG.Legacy.Polygon"/> in the <see cref="RealtimeCSG.Legacy.ControlMesh"/> this <see cref="RealtimeCSG.Legacy.HalfEdge"/> belongs to.</param>
		/// <param name="twinIndex">The index to the twin edge <see cref="RealtimeCSG.Legacy.HalfEdge"/> in the <see cref="RealtimeCSG.Legacy.ControlMesh"/> this <see cref="RealtimeCSG.Legacy.HalfEdge"/> belongs to.</param>
		/// <param name="vertexIndex">The index to a vertex in the <see cref="RealtimeCSG.Legacy.ControlMesh"/> this <see cref="RealtimeCSG.Legacy.HalfEdge"/> belongs to.</param>
		public HalfEdge(short polygonIndex, int twinIndex, short vertexIndex, bool hardEdge = true)
		{
			TwinIndex		= twinIndex;
			PolygonIndex	= polygonIndex;
			HardEdge		= hardEdge;
			VertexIndex		= vertexIndex;
		}

		/// <value>Index to the twin edge <see cref="RealtimeCSG.Legacy.HalfEdge"/> in the <see cref="RealtimeCSG.Legacy.ControlMesh"/> this <see cref="RealtimeCSG.Legacy.HalfEdge"/> belongs to.</value>
		[FormerlySerializedAs("twinIndex"   )] public int		TwinIndex;

		/// <value>Index to the <see cref="RealtimeCSG.Legacy.Polygon"/> in the <see cref="RealtimeCSG.Legacy.ControlMesh"/> this <see cref="RealtimeCSG.Legacy.HalfEdge"/> belongs to.</value>
		[FormerlySerializedAs("polygonIndex")] public short		PolygonIndex;
		
		/// <value>Is this edge considered an internal or external (hard) edge</value>
		[FormerlySerializedAs("hardEdge"    )] public bool		HardEdge;
		
		/// <value>Index to a vertex in the <see cref="RealtimeCSG.Legacy.ControlMesh"/> this <see cref="RealtimeCSG.Legacy.HalfEdge"/> belongs to.</value>
		[FormerlySerializedAs("vertexIndex" )] public short		VertexIndex;
	}
	
	/// <summary>A <see cref="RealtimeCSG.Legacy.Polygon"/> of a <see cref="RealtimeCSG.Legacy.ControlMesh"/>.</summary>
	/// <remarks><note>This code will be replaced by pure <see cref="RealtimeCSG.Foundation.BrushMesh"/> code eventually.</note>
	/// <note>This class can be safely serialized.</note></remarks>
	/// <seealso cref="RealtimeCSG.Legacy.ControlMesh"/>
	/// <seealso cref="RealtimeCSG.Legacy.HalfEdge"/>
	/// <seealso cref="RealtimeCSG.Legacy.TexGen"/>
	/// <seealso cref="RealtimeCSG.Legacy.TexGenFlags"/>
	/// <seealso cref="RealtimeCSG.Legacy.Shape"/>
	[Serializable]
	public sealed class Polygon
	{
		/// <summary>Creates a Polygon for a <see cref="RealtimeCSG.Legacy.ControlMesh"/></summary>
		/// <param name="edges">A list of indices to <see cref="RealtimeCSG.Legacy.HalfEdge"/>s in the <see cref="RealtimeCSG.Legacy.ControlMesh"/> this <see cref="RealtimeCSG.Legacy.Polygon"/> belongs to, that form this <see cref="RealtimeCSG.Legacy.Polygon"/>.</param>
		/// <param name="texGenIndex">An index to the <see cref="RealtimeCSG.Legacy.TexGen"/> in the <see cref="RealtimeCSG.Legacy.ControlMesh"/> that this <see cref="RealtimeCSG.Legacy.Polygon"/> uses.</param>
		public Polygon(int[] edges, int texGenIndex) { EdgeIndices = edges; TexGenIndex = texGenIndex; }

		/// <value>indices to <see cref="RealtimeCSG.Legacy.HalfEdge"/>s in the <see cref="RealtimeCSG.Legacy.ControlMesh"/> this <see cref="RealtimeCSG.Legacy.Polygon"/> belongs to.</value>
		[FormerlySerializedAs("edgeIndices")]
		public int[] EdgeIndices;

		/// <value>Index to the <see cref="RealtimeCSG.Legacy.TexGen"/> in the <see cref="RealtimeCSG.Legacy.ControlMesh"/> that this <see cref="RealtimeCSG.Legacy.Polygon"/> uses.</value>
		[FormerlySerializedAs("surfaceIndex"),FormerlySerializedAs("texGenIndex")]
		public int TexGenIndex;
	}

	
	/// <summary>Defines the shape of a convex brush that can be used in CSG operations.</summary>
	/// <remarks><note>This code will be replaced by pure <see cref="RealtimeCSG.Foundation.BrushMesh"/> code eventually.</note>
	/// <note>This class can be safely serialized.</note></remarks>
	/// <seealso cref="RealtimeCSG.Legacy.Polygon"/>
	/// <seealso cref="RealtimeCSG.Legacy.HalfEdge"/>
	/// <seealso cref="RealtimeCSG.Legacy.TexGen"/>
	/// <seealso cref="RealtimeCSG.Legacy.TexGenFlags"/>
	/// <seealso cref="RealtimeCSG.Legacy.Shape"/>
	[Serializable]
	public sealed class ControlMesh
	{
		/// <value>Vertices that are used by this <see cref="RealtimeCSG.Legacy.ControlMesh"/></value>
		[FormerlySerializedAs("points")]
		[FormerlySerializedAs("vertices")] public Vector3[]		Vertices;
		
		/// <value><see cref="RealtimeCSG.Legacy.HalfEdge"/>s that are used by this <see cref="RealtimeCSG.Legacy.ControlMesh"/></value>
		[FormerlySerializedAs("edges")]    public HalfEdge[]	Edges;
		
		/// <value><see cref="RealtimeCSG.Legacy.Polygon"/>s that are used by this <see cref="RealtimeCSG.Legacy.ControlMesh"/></value>
		[FormerlySerializedAs("polygons")] public Polygon[]		Polygons;

		/// <value><b>true</b> if valid, <b>false</b> if not.</value>
		public bool			Valid { get { return valid; } set { valid = value; } }
		internal bool		valid;
				
		/// <value>Every time this ControlMesh changes, this value can be increased to easily detect modifications over time.</value>
		public int			Generation =  0;


		/// <summary>Create an uninitialized <see cref="RealtimeCSG.Legacy.ControlMesh"/></summary>
		public ControlMesh() { }

		/// <summary>Create new <see cref="RealtimeCSG.Legacy.ControlMesh"/> by copying an existing <see cref="RealtimeCSG.Legacy.ControlMesh"/>.</summary>
		/// <param name="other">The <see cref="RealtimeCSG.Legacy.ControlMesh"/> to copy from.</param>
		public ControlMesh(ControlMesh other) { CopyFrom(other); }
		

		/// <summary>Increase the <see cref="RealtimeCSG.Legacy.ControlMesh.Generation"/> of this <see cref="RealtimeCSG.Legacy.ControlMesh"/> so that we can detect that its been changed.</summary>
		public void SetDirty() { Generation++; }

		/// <summary>Clear the contents of this <see cref="RealtimeCSG.Legacy.ControlMesh"/>.</summary>
		public void Reset()
		{
			Vertices = null;
			Edges = null;
			Polygons = null;
			valid = false;
			Generation =  0;
		}

		/// <summary>Copy the contents of another <see cref="RealtimeCSG.Legacy.ControlMesh"/> to this <see cref="RealtimeCSG.Legacy.ControlMesh"/>.</summary>
		/// <param name="other">The <see cref="RealtimeCSG.Legacy.ControlMesh"/> to copy from.</param>
		public void CopyFrom(ControlMesh other)
		{
			if (other == null)
			{
				Reset();
				return;
			}
			if (other.Vertices != null)
			{ 
				if (Vertices == null || Vertices.Length != other.Vertices.Length)
					Vertices		= new Vector3[other.Vertices.Length];
				Array.Copy(other.Vertices, Vertices, other.Vertices.Length);
			} else
				Vertices = null;
			
			if (other.Edges != null)
			{ 
				if (Edges == null || Edges.Length != other.Edges.Length)
					Edges		= new HalfEdge[other.Edges.Length];
				Array.Copy(other.Edges, Edges, other.Edges.Length);
			} else
				Edges = null;

			if (other.Polygons != null)
			{ 
				if (Polygons == null || Polygons.Length != other.Polygons.Length)
					Polygons = new Polygon[other.Polygons.Length];
				for (var p = 0; p < other.Polygons.Length; p++)
				{
					if (other.Polygons[p].EdgeIndices == null ||
						other.Polygons[p].EdgeIndices.Length == 0)
						continue;
					var newEdges = new int[other.Polygons[p].EdgeIndices.Length];
					Array.Copy(other.Polygons[p].EdgeIndices, newEdges, other.Polygons[p].EdgeIndices.Length);
					Polygons[p] = new Polygon(newEdges, other.Polygons[p].TexGenIndex);
				}
			} else
				Polygons = null;

			valid = other.valid;
			Generation = other.Generation;
		}

		/// <summary>Return a copy of this <see cref="RealtimeCSG.Legacy.ControlMesh"/>.</summary>
		/// <returns>The copy of this <see cref="RealtimeCSG.Legacy.ControlMesh"/>.</returns>
		public ControlMesh Clone() { return new ControlMesh(this); }

		/// <summary>For an index to a <see cref="RealtimeCSG.Legacy.HalfEdge"/>, return the vertex associated with it.</summary>
		/// <param name="halfEdgeIndex">The index to the <see cref="RealtimeCSG.Legacy.HalfEdge"/></param>
		/// <returns>A vertex that belongs to a <see cref="RealtimeCSG.Legacy.HalfEdge"/>. If the <paramref name="halfEdgeIndex"/> was invalid it returns a zero vertex.</returns>
		public Vector3	GetVertex				(int halfEdgeIndex)
		{
			if (halfEdgeIndex < 0 || halfEdgeIndex >= Edges.Length)
				return MathConstants.zeroVector3;
			var vertexIndex = Edges[halfEdgeIndex].VertexIndex;
			if (vertexIndex < 0 || vertexIndex >= Vertices.Length)
				return MathConstants.zeroVector3;
			return Vertices[vertexIndex];
		}

		/// <summary>Returns the vertices for the given array of indices to <see cref="RealtimeCSG.Legacy.HalfEdge"/>s.</summary>
		/// <param name="halfEdgeIndices">Indices to <see cref="RealtimeCSG.Legacy.HalfEdge"/>s.</param>
		/// <returns>An array of vertices</returns>
		public Vector3[]	GetVertices			(int[] halfEdgeIndices)
		{
			var vertices = new Vector3[halfEdgeIndices.Length];
			for (var i = 0; i < halfEdgeIndices.Length; i++)
			{
				var halfEdgeIndex = halfEdgeIndices[i];
				if (halfEdgeIndex < 0 || halfEdgeIndex >= Edges.Length)
				{
					vertices[i] = MathConstants.zeroVector3;
					continue;
				}
				var vertexIndex = Edges[halfEdgeIndex].VertexIndex;
				if (vertexIndex < 0 || vertexIndex >= Vertices.Length)
				{
					vertices[i] = MathConstants.zeroVector3;
					continue;
				}
				vertices[i] = Vertices[vertexIndex];
			}
			return vertices;
		}

		/// <summary>Get the vertex that is associated with the given <see cref="RealtimeCSG.Legacy.HalfEdge"/></summary>
		/// <param name="halfEdge">The <see cref="RealtimeCSG.Legacy.HalfEdge"/> to get a vertex for</param>
		/// <returns>A vertex</returns>
		public Vector3	GetVertex				(ref HalfEdge halfEdge)		{ return Vertices[halfEdge.VertexIndex]; }
		
		/// <summary>Get the vertex index that is associated with the given <see cref="RealtimeCSG.Legacy.HalfEdge"/> that is found using <paramref name="halfEdgeIndex"/></summary>
		/// <param name="halfEdgeIndex">Index to a <see cref="RealtimeCSG.Legacy.HalfEdge"/></param>
		/// <returns>A index to a vertex in this <see cref="RealtimeCSG.Legacy.ControlMesh"/></returns>
		public short	GetVertexIndex			(int halfEdgeIndex)			{ return Edges[halfEdgeIndex].VertexIndex; }
		
		/// <summary>Get the vertex index that is associated with the given <see cref="RealtimeCSG.Legacy.HalfEdge"/></summary>
		/// <param name="halfEdge">The <see cref="RealtimeCSG.Legacy.HalfEdge"/> to get a vertex for</param>
		/// <returns>A index to a vertex in this <see cref="RealtimeCSG.Legacy.ControlMesh"/></returns>
		public short	GetVertexIndex			(ref HalfEdge halfEdge)		{ return halfEdge.VertexIndex; }
		
		/// <summary>Get the vertex of the twin of the given <see cref="RealtimeCSG.Legacy.HalfEdge"/></summary>
		/// <param name="halfEdge">The <see cref="RealtimeCSG.Legacy.HalfEdge"/> to get a vertex for</param>
		/// <returns>A vertex in this <see cref="RealtimeCSG.Legacy.ControlMesh"/></returns>
		public Vector3	GetTwinEdgeVertex		(ref HalfEdge halfEdge)		{ return Vertices[Edges[halfEdge.TwinIndex].VertexIndex]; }

		/// <summary>Get the vertex of the twin of the given <see cref="RealtimeCSG.Legacy.HalfEdge"/> that is found using <paramref name="halfEdgeIndex"/></summary>
		/// <param name="halfEdgeIndex">Index to a <see cref="RealtimeCSG.Legacy.HalfEdge"/></param>
		/// <returns>A vertex in this <see cref="RealtimeCSG.Legacy.ControlMesh"/></returns>
		public Vector3	GetTwinEdgeVertex		(int halfEdgeIndex)			{ return Vertices[Edges[Edges[halfEdgeIndex].TwinIndex].VertexIndex]; }
		
		/// <summary>Get the index to the vertex of the twin of the given <see cref="RealtimeCSG.Legacy.HalfEdge"/></summary>
		/// <param name="halfEdge">The <see cref="RealtimeCSG.Legacy.HalfEdge"/> to get a vertex for</param>
		/// <returns>A index to a vertex in this <see cref="RealtimeCSG.Legacy.ControlMesh"/></returns>
		public short	GetTwinEdgeVertexIndex	(ref HalfEdge halfEdge)		{ return Edges[halfEdge.TwinIndex].VertexIndex; }
		
		/// <summary>Get the index to the vertex of the twin of the given <see cref="RealtimeCSG.Legacy.HalfEdge"/> that is found using <paramref name="halfEdgeIndex"/></summary>
		/// <param name="halfEdgeIndex">Index to a <see cref="RealtimeCSG.Legacy.HalfEdge"/></param>
		/// <returns>A index to a vertex in this <see cref="RealtimeCSG.Legacy.ControlMesh"/></returns>
		public short	GetTwinEdgeVertexIndex	(int halfEdgeIndex)			{ return Edges[Edges[halfEdgeIndex].TwinIndex].VertexIndex; }
		
		/// <summary>Get the index to twin <see cref="RealtimeCSG.Legacy.HalfEdge"/> of the given <see cref="RealtimeCSG.Legacy.HalfEdge"/></summary>
		/// <param name="halfEdge">The <see cref="RealtimeCSG.Legacy.HalfEdge"/> to get a vertex for</param>
		/// <returns>A index to a <see cref="RealtimeCSG.Legacy.HalfEdge"/> in this <see cref="RealtimeCSG.Legacy.ControlMesh"/></returns>
		public int		GetTwinEdgeIndex		(ref HalfEdge halfEdge)		{ return halfEdge.TwinIndex; }
		
		/// <summary>Get the index to twin <see cref="RealtimeCSG.Legacy.HalfEdge"/> of the given <see cref="RealtimeCSG.Legacy.HalfEdge"/> that is found using <paramref name="halfEdgeIndex"/></summary>
		/// <param name="halfEdgeIndex">Index to a <see cref="RealtimeCSG.Legacy.HalfEdge"/></param>
		/// <returns>A index to a <see cref="RealtimeCSG.Legacy.HalfEdge"/> in this <see cref="RealtimeCSG.Legacy.ControlMesh"/></returns>
		public int		GetTwinEdgeIndex		(int halfEdgeIndex)			{ return Edges[halfEdgeIndex].TwinIndex; }
		
		/// <summary>Get the index to the polygon of the twin <see cref="RealtimeCSG.Legacy.HalfEdge"/> of the given <see cref="RealtimeCSG.Legacy.HalfEdge"/> that is found using <paramref name="halfEdgeIndex"/></summary>
		/// <param name="halfEdgeIndex">Index to a <see cref="RealtimeCSG.Legacy.HalfEdge"/></param>
		/// <returns>A index to a <see cref="RealtimeCSG.Legacy.Polygon"/> in this <see cref="RealtimeCSG.Legacy.ControlMesh"/></returns>
		public short	GetTwinEdgePolygonIndex	(int halfEdgeIndex)			{ return Edges[Edges[halfEdgeIndex].TwinIndex].PolygonIndex; }
		
		/// <summary>Get the index to the polygon of the given <see cref="RealtimeCSG.Legacy.HalfEdge"/> that is found using <paramref name="halfEdgeIndex"/></summary>
		/// <param name="halfEdgeIndex">Index to a <see cref="RealtimeCSG.Legacy.HalfEdge"/></param>
		/// <returns>A index to a <see cref="RealtimeCSG.Legacy.Polygon"/> in this <see cref="RealtimeCSG.Legacy.ControlMesh"/></returns>
		public short	GetEdgePolygonIndex		(int halfEdgeIndex)			{ return Edges[halfEdgeIndex].PolygonIndex; }
		
		/// <summary>Get the next index to a <see cref="RealtimeCSG.Legacy.HalfEdge"/> around the vertex associated with the <see cref="RealtimeCSG.Legacy.HalfEdge"/> found with the given <paramref name="halfEdgeIndex"/></summary>
		/// <param name="halfEdgeIndex">Index to a <see cref="RealtimeCSG.Legacy.HalfEdge"/></param>
		/// <returns>A index to a <see cref="RealtimeCSG.Legacy.HalfEdge"/> in this <see cref="RealtimeCSG.Legacy.ControlMesh"/></returns>
		public int		GetNextEdgeIndexAroundVertex	(int halfEdgeIndex) { return GetTwinEdgeIndex(GetNextEdgeIndex(halfEdgeIndex)); }
		
		/// <summary>Get the previous index to a <see cref="RealtimeCSG.Legacy.HalfEdge"/> of the <see cref="RealtimeCSG.Legacy.Polygon"/> associated with the <see cref="RealtimeCSG.Legacy.HalfEdge"/> found with the given <paramref name="halfEdgeIndex"/></summary>
		/// <param name="halfEdgeIndex">Index to a <see cref="RealtimeCSG.Legacy.HalfEdge"/></param>
		/// <returns>A index to a <see cref="RealtimeCSG.Legacy.HalfEdge"/> in this <see cref="RealtimeCSG.Legacy.ControlMesh"/></returns>
		public int		GetPrevEdgeIndex	(int halfEdgeIndex)
		{
			var edge	= Edges[halfEdgeIndex];
			var polygonIndex = edge.PolygonIndex;
			if (polygonIndex < 0 || polygonIndex >= Polygons.Length)
				return -1;

			var edgeIndices = Polygons[polygonIndex].EdgeIndices;
			for (int i = 1; i < edgeIndices.Length; i++)
			{
				if (edgeIndices[i] == halfEdgeIndex)
					return edgeIndices[i - 1];
			}
			return edgeIndices[edgeIndices.Length - 1];
		}
		
		/// <summary>Get the next index to a <see cref="RealtimeCSG.Legacy.HalfEdge"/> of the <see cref="RealtimeCSG.Legacy.Polygon"/> associated with the <see cref="RealtimeCSG.Legacy.HalfEdge"/> found with the given <paramref name="halfEdgeIndex"/></summary>
		/// <param name="halfEdgeIndex">Index to a <see cref="RealtimeCSG.Legacy.HalfEdge"/></param>
		/// <returns>A index to a <see cref="RealtimeCSG.Legacy.HalfEdge"/> in this <see cref="RealtimeCSG.Legacy.ControlMesh"/></returns>
		public int		GetNextEdgeIndex	(int halfEdgeIndex)
		{
			var edge	= Edges[halfEdgeIndex];
			var polygonIndex = edge.PolygonIndex;
			if (polygonIndex < 0 || polygonIndex >= Polygons.Length)
				return -1;

			var edgeIndices = Polygons[polygonIndex].EdgeIndices;
			for (int i = 0; i < edgeIndices.Length - 1; i++)
			{
				if (edgeIndices[i] == halfEdgeIndex)
					return edgeIndices[i + 1];
			}
			return edgeIndices[0];
		}
	}
}