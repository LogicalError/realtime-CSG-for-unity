using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace RealtimeCSG.Foundation
{
	/// <summary>Describes a generated mesh, that may not already have been created.</summary>
	/// <remarks>A <see cref="RealtimeCSG.Foundation.GeneratedMeshDescription"/> is created by <see cref="RealtimeCSG.Foundation.CSGTree.GetMeshDescriptions"/> and can then be used to initialize a [UnityEngine.Mesh](https://docs.unity3d.com/ScriptReference/Mesh.html) by calling <seealso cref="RealtimeCSG.Foundation.CSGTree.GetGeneratedMesh" /> and <seealso cref="RealtimeCSG.Foundation.GeneratedMeshContents.CopyTo" />.
	/// <code>
	///	CSGTree tree = ...;
	///	MeshQuery[] meshQueries = ...;
	/// GeneratedMeshDescription[] meshDescriptions = tree.GetMeshDescriptions(meshQueries, VertexChannelFlags.All);
	/// if (meshDescriptions == null) return;
	/// foreach(var meshDescription in meshDescriptions)
	/// {
	///		GeneratedMeshContents contents = tree.GetGeneratedMesh(meshDescription);
	///		UnityEngine.Mesh unityMesh = new UnityEngine.Mesh();
	///		contents.CopyTo(unityMesh);
	///	}
	/// </code>
	/// See the [Create Unity Meshes](~/documentation/createUnityMesh.md) article for more information.
	/// <note>The hash values can be used to determine if an existing mesh has changed, and to decide if a new mesh should be created or not.</note>
	/// <note>This struct is mirrored on the native side and cannot be modified.</note>
	/// </remarks>
	/// <seealso cref="RealtimeCSG.Foundation.MeshQuery" />
	/// <seealso cref="RealtimeCSG.Foundation.CSGTree" />
	/// <seealso cref="RealtimeCSG.Foundation.CSGTree.GetMeshDescriptions" />
	/// <seealso cref="RealtimeCSG.Foundation.CSGTree.GetGeneratedMesh" />
	/// <seealso cref="RealtimeCSG.Foundation.GeneratedMeshContents"/>
	[Serializable, StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct GeneratedMeshDescription
	{
		public MeshQuery	meshQuery;

		/// <value>If requested by the <see cref="RealtimeCSG.Foundation.MeshQuery"/> this hold a surface parameter, otherwise its 0.</value>
		/// <remarks>A surface parameter can be used to, for example, differentiate between meshes that use a different [UnityEngine.Material](https://docs.unity3d.com/ScriptReference/Material.html).</remarks>
		public Int32		surfaceParameter;

		/// <value>An unique index for each found <paramref name="meshQuery"/>/<paramref name="surfaceParameter"/> pair.</value>
		public Int32		meshQueryIndex;

		/// <value>An index for each sub-mesh for each unique <paramref name="meshQuery"/>/<paramref name="surfaceParameter"/> pair.</value>
		/// <remarks>Each individual generated mesh has a 64k vertex limit. When more vertices are required, multiple sub-meshes are generated.</remarks>
		public Int32		subMeshQueryIndex;

		/// <value>Value that can be used to detect changes in vertex positions / indices.</value>
		public Int64		geometryHashValue;

		/// <value>Value that can be used to detect changes in normal, tangent or uv.</value>
		public Int64		surfaceHashValue;

		/// <value>Number of vertices of this generated mesh.</value><remarks>This can be used to pre-allocate arrays.</remarks>
		public Int32		vertexCount;

		/// <value>Number of vertices of this generated mesh.</value><remarks>This be used to pre-allocate arrays.</remarks>
		public Int32		indexCount;

		#region Comparison
		[EditorBrowsable(EditorBrowsableState.Never)]
		public override bool Equals(object obj)
		{
			if (!(obj is GeneratedMeshDescription))
			{
				return false;
			}

			var description = (GeneratedMeshDescription)obj;
			return meshQuery		 == description.meshQuery &&
				   surfaceParameter	 == description.surfaceParameter &&
				   subMeshQueryIndex == description.subMeshQueryIndex &&
				   meshQueryIndex	 == description.meshQueryIndex &&
				   geometryHashValue == description.geometryHashValue &&
				   surfaceHashValue  == description.surfaceHashValue &&
				   vertexCount       == description.vertexCount &&
				   indexCount        == description.indexCount;
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public override int GetHashCode()
		{
			var hashCode = -190551774;
			hashCode = hashCode * -1521134295;
			hashCode = hashCode * -1521134295 + meshQuery.GetHashCode();
			hashCode = hashCode * -1521134295 + (int)surfaceParameter;
			hashCode = hashCode * -1521134295 + (int)subMeshQueryIndex;
			hashCode = hashCode * -1521134295 + (int)meshQueryIndex;
			hashCode = hashCode * -1521134295 + (int)geometryHashValue;
			hashCode = hashCode * -1521134295 + (int)surfaceHashValue;
			hashCode = hashCode * -1521134295 + (int)vertexCount;
			hashCode = hashCode * -1521134295 + (int)indexCount;
			return hashCode;
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public static bool operator == (GeneratedMeshDescription left, GeneratedMeshDescription right)
		{
			return	left.meshQuery			== right.meshQuery &&
					left.surfaceParameter	== right.surfaceParameter &&
					left.subMeshQueryIndex	== right.subMeshQueryIndex &&
					left.meshQueryIndex		== right.meshQueryIndex &&
					left.geometryHashValue	== right.geometryHashValue &&
					left.surfaceHashValue	== right.surfaceHashValue &&
					left.vertexCount		== right.vertexCount &&
					left.indexCount			== right.indexCount;
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public static bool operator != (GeneratedMeshDescription left, GeneratedMeshDescription right)
		{
			return	left.meshQuery			!= right.meshQuery ||
					left.surfaceParameter	!= right.surfaceParameter ||
					left.subMeshQueryIndex	!= right.subMeshQueryIndex ||
					left.meshQueryIndex		!= right.meshQueryIndex ||
					left.geometryHashValue	!= right.geometryHashValue ||
					left.surfaceHashValue	!= right.surfaceHashValue ||
					left.vertexCount		!= right.vertexCount ||
					left.indexCount			!= right.indexCount;
		}
		#endregion
	}
}
