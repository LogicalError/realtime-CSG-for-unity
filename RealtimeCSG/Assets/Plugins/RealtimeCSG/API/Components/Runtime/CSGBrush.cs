using System;
using UnityEngine;
using RealtimeCSG.Legacy;
using InternalRealtimeCSG;
using CSGOperationType = RealtimeCSG.Foundation.CSGOperationType;

namespace RealtimeCSG.Components
{
	[System.Serializable]
	public enum BrushFlags
	{
		None = 0,
		InfiniteBrush = 1 // used to create inverted world
	}

	/// <summary>Holds a CSG tree brush</summary>
#if UNITY_EDITOR
	[AddComponentMenu("CSG/Brush")]
	[ExecuteInEditMode]
#endif
	public sealed partial class CSGBrush : CSGNode
	{
		public const float CurrentVersion = 2.1f;
		/// <value>The version number of this instance of a <see cref="CSGBrush" /></value>
		[HideInInspector] public float Version = CurrentVersion;

#if UNITY_EDITOR
		public bool IsRegistered { get { return brushNodeID != CSGNode.InvalidNodeID; } }
#endif

		#region Settings
#if UNITY_EDITOR

		/// <value>The CSG operation to perform with this brush</value>
		public CSGOperationType		OperationType	= CSGOperationType.Additive;
		
		[UnityEngine.Serialization.FormerlySerializedAs("flags")]
		public BrushFlags			Flags			= BrushFlags.None;

		/// <value>The <see cref="Shape"/> that defines the shape by this brush together with its <see cref="ControlMesh"/>.</value>
		/// <remarks><note>This will be replaced by <see cref="RealtimeCSG.Foundation.BrushMesh"/> eventually</note></remarks>
		public Shape				Shape;
		
		/// <value>The <see cref="ControlMesh"/> that defines the shape by this brush together with its <see cref="Shape"/>.</value>
		/// <remarks><note>This will be replaced by <see cref="RealtimeCSG.Foundation.BrushMesh"/> eventually</note></remarks>
		public ControlMesh			ControlMesh;

#endif
		#endregion

		#region Cached values
#if UNITY_EDITOR
		[HideInInspector][NonSerialized] public Int32		brushNodeID  = CSGNode.InvalidNodeID;
		[HideInInspector][NonSerialized] public Color?		outlineColor;
        [HideInInspector][NonSerialized] public readonly ChildNodeData			ChildData				= new ChildNodeData();
		[HideInInspector][NonSerialized] public readonly HierarchyItem			hierarchyItem			= new HierarchyItem();
		[HideInInspector][NonSerialized] public GeometryWireframe				outline					= null;
        [HideInInspector][NonSerialized] public Foundation.CSGOperationType		prevOperation			= Foundation.CSGOperationType.Additive;
		[HideInInspector][NonSerialized] public int								controlMeshGeneration	= 0;

		// this allows us to determine if our brush has changed it's transformation
		[HideInInspector][NonSerialized] public readonly CompareTransformation	compareTransformation	= new CompareTransformation();

		// this allows us to determine if our brush has any of it's surfaces changed
		[HideInInspector][NonSerialized] public readonly CompareShape			compareShape			= new CompareShape();
		
		public void ClearCache()
		{
			ChildData    .Reset();
			hierarchyItem.Reset();
			compareShape.Reset();
			compareTransformation.Reset();
			outline					= null;
			prevOperation			= Foundation.CSGOperationType.Additive;
			controlMeshGeneration	= 0;
		}
#endif
		#endregion

		#region Events
#if UNITY_EDITOR
		// register ourselves with our scene manager
		void Awake()
		{
			// cannot change visibility since this might have an effect on exporter
			this.hideFlags |= HideFlags.DontSaveInBuild;
			this.brushNodeID = CSGNode.InvalidNodeID;
			ComponentUpgrader.UpgradeWhenNecessary(this);
			if (CSGSceneManagerRedirector.Interface != null)
				CSGSceneManagerRedirector.Interface.OnCreated(this);
		}

		internal void OnEnable()					{ if (CSGSceneManagerRedirector.Interface != null) CSGSceneManagerRedirector.Interface.OnEnabled(this); }

		// unregister ourselves from our scene manager
		internal void OnDisable()					{ if (CSGSceneManagerRedirector.Interface != null) CSGSceneManagerRedirector.Interface.OnDisabled(this); }
		internal void OnDestroy()					{ if (CSGSceneManagerRedirector.Interface != null) CSGSceneManagerRedirector.Interface.OnDestroyed(this); }

		// detect if this node has been moved within the hierarchy
		internal void OnTransformParentChanged()	{ if (CSGSceneManagerRedirector.Interface != null) CSGSceneManagerRedirector.Interface.OnTransformParentChanged(this); }

		// called when any value of this brush has been modified from within the inspector
		internal void OnValidate()					{ if (CSGSceneManagerRedirector.Interface != null) CSGSceneManagerRedirector.Interface.OnValidate(this); }
#endif
		#endregion

#if UNITY_EDITOR
		public void EnsureInitialized()				{ if (CSGSceneManagerRedirector.Interface != null) CSGSceneManagerRedirector.Interface.EnsureInitialized(this); }
#endif
	}
}
