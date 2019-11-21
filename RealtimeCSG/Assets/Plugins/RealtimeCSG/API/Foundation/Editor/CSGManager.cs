using System;

namespace RealtimeCSG.Foundation
{
	/// <summary>This class is manager class for all <see cref="RealtimeCSG.Foundation.CSGTreeNode"/>s.</summary>	
	public sealed partial class CSGManager
	{
		/// <summary>Destroys all <see cref="RealtimeCSG.Foundation.CSGTreeNode"/>s and all <see cref="RealtimeCSG.Foundation.BrushMesh"/>es.</summary>
		public static void	Clear	()	{ ClearAllNodes(); }

		/// <summary>Updates all pending changes to all <see cref="RealtimeCSG.Foundation.CSGTree"/>s.</summary>
		/// <returns>True if any <see cref="RealtimeCSG.Foundation.CSGTree"/>s have been updated, false if no changes have been found.</returns>
		public static bool	Flush	()	{ return UpdateAllTreeMeshes(); }

		/// <summary>Clears all caches and rebuilds all <see cref="RealtimeCSG.Foundation.CSGTree"/>s.</summary>
		public static void	Rebuild	()	{ RebuildAll(); }

		/// <summary>Destroy all <see cref="RealtimeCSG.Foundation.CSGTreeNode"/>s contained in <paramref name="nodes"/>.</summary>
		/// <param name="nodes">The <see cref="RealtimeCSG.Foundation.CSGTreeNode"/>s to destroy</param>
		/// <returns>True on success, false if there was a problem with destroying the <see cref="RealtimeCSG.Foundation.CSGTreeNode"/>s. See the log for more information.</returns>
		/// <exception cref="System.ArgumentNullException">Thrown when the <paramref name="nodes"/> is null.</exception>  
		public static bool	Destroy	(CSGTreeNode[] nodes) { if (nodes == null) throw new ArgumentNullException("nodes"); return DestroyNodes(nodes.Length, nodes); }


		/// <value>The number of <see cref="RealtimeCSG.Foundation.CSGTreeNode"/>s.</value>
		public static int	TreeNodeCount			{ get { return GetNodeCount(); } }

		/// <value>The number of <see cref="RealtimeCSG.Foundation.CSGTreeBrush"/>es.</value>
		public static int	TreeBrushCount			{ get { return GetBrushCount(); } }
		
		/// <value>The number of <see cref="RealtimeCSG.Foundation.CSGTreeBranch"/>es.</value>
		public static int	TreeBranchCount			{ get { return GetBranchCount(); } }

		/// <value>The number of <see cref="RealtimeCSG.Foundation.CSGTree"/>s.</value>
		public static int	TreeCount				{ get { return GetTreeCount(); } }

		/// <value>The number of <see cref="RealtimeCSG.Foundation.BrushMesh"/>es.</value>
		public static int	BrushMeshCount			{ get { return GetBrushMeshCount(); } }


		/// <value>All the <see cref="RealtimeCSG.Foundation.CSGTreeNode"/>s.</value>
		public static CSGTreeNode[] AllTreeNodes	{ get { return GetAllTreeNodes(); } }

		/// <value>All the <see cref="RealtimeCSG.Foundation.CSGTree"/>s.</value>
		public static CSGTree[]		AllTrees		{ get { return GetAllTrees(); } }
		
		/// <value>All the <see cref="RealtimeCSG.Foundation.BrushMeshInstance"/>s.</value>
		public static BrushMeshInstance[] AllBrushMeshInstances	{ get { return GetAllBrushMeshInstances(); } }

		// <value>Returns the version string of RealtimeCSG</value>
		public static string VersionName
		{
			get
			{
				return
					string.Format("v {0}{1}",
						Versioning.PluginVersion.Replace('_','.'),
						HasBeenCompiledInDebugMode() ? " (C++ DEBUG)" : string.Empty
						);
			}
		}
	}
}