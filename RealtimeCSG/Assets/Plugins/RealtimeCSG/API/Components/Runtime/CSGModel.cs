using System;
using UnityEngine;
using InternalRealtimeCSG;

namespace RealtimeCSG.Components
{
	[Flags, Serializable]
	public enum ModelSettingsFlags
	{
//		ShadowCastingModeFlags	= 1|2|4,
//		DoNotReceiveShadows		= 8,
		DoNotRender				= 16,
		NoCollider				= 32,
		IsTrigger				= 64,
		InvertedWorld			= 128,
		SetColliderConvex		= 256,
		AutoUpdateRigidBody		= 512,
		PreserveUVs             = 1024,
		AutoRebuildUVs			= 2048,
		StitchLightmapSeams		= 4096,
		IgnoreNormals			= 8192,
		TwoSidedShadows			= 16384,
	}

	[Serializable]
	public enum ExportType
	{
		FBX,
		UnityMesh
	}

	[Serializable]
	public enum OriginType
	{
		ModelCenter,
		ModelPivot,
		WorldSpace
	}
	
	/// <summary>Holds a CSG tree and generates meshes for that CSG tree</summary>
	/// <remarks>The CSG tree that defines a model is defined by its child [UnityEngine.GameObject](https://docs.unity3d.com/ScriptReference/GameObject.html)s.</remarks>
#if UNITY_EDITOR
	[AddComponentMenu("CSG/Model")]
	[DisallowMultipleComponent, ExecuteInEditMode]
#endif
	public sealed class CSGModel : CSGNode
	{
		public const float CurrentVersion = 1.1f;

        public static ModelSettingsFlags DefaultSettings = ((ModelSettingsFlags)UnityEngine.Rendering.ShadowCastingMode.On) | ModelSettingsFlags.PreserveUVs;

        /// <value>The version number of this instance of a <see cref="CSGModel" /></value>
        [HideInInspector] public float Version = CurrentVersion;

		public bool	IsRenderable			{ get { return (Settings & ModelSettingsFlags.DoNotRender) == (ModelSettingsFlags)0; } }
		public bool	IsTwoSidedShadows		{ get { return (Settings & ModelSettingsFlags.TwoSidedShadows) != (ModelSettingsFlags)0; } }
		public bool	HaveCollider			{ get { return (Settings & ModelSettingsFlags.NoCollider) == (ModelSettingsFlags)0; } }
		public bool	IsTrigger				{ get { return (Settings & ModelSettingsFlags.IsTrigger) != (ModelSettingsFlags)0; } }
		public bool	InvertedWorld			{ get { return (Settings & ModelSettingsFlags.InvertedWorld) != (ModelSettingsFlags)0; } }
		public bool	SetColliderConvex		{ get { return (Settings & ModelSettingsFlags.SetColliderConvex) != (ModelSettingsFlags)0; } }
		public bool	NeedAutoUpdateRigidBody	{ get { return (Settings & ModelSettingsFlags.AutoUpdateRigidBody) == (ModelSettingsFlags)0; } }
		public bool	PreserveUVs         	{ get { return (Settings & ModelSettingsFlags.PreserveUVs) != (ModelSettingsFlags)0; } }
		public bool StitchLightmapSeams		{ get { return (Settings & ModelSettingsFlags.StitchLightmapSeams) != (ModelSettingsFlags)0; } }		
		public bool	AutoRebuildUVs         	{ get { return (Settings & ModelSettingsFlags.AutoRebuildUVs) != (ModelSettingsFlags)0; } }
		public bool	IgnoreNormals  			{ get { return (Settings & ModelSettingsFlags.IgnoreNormals) != (ModelSettingsFlags)0; } }

#if UNITY_EDITOR
		public bool IsRegistered			{ get { return modelNodeID != CSGNode.InvalidNodeID; } }
#endif


		#region Cached values	
#if UNITY_EDITOR
		[HideInInspector][NonSerialized] public Int32					modelNodeID				= CSGNode.InvalidNodeID;
		[HideInInspector][NonSerialized] public readonly ParentNodeData	parentData				= new ParentNodeData();
		[HideInInspector][NonSerialized] public GeneratedMeshes			generatedMeshes;
		[HideInInspector][NonSerialized] public Transform				cachedTransform			= null;
		[HideInInspector][SerializeField] public CSGBrush				infiniteBrush			= null;
		[HideInInspector][NonSerialized] public bool					forceUpdate				= false;
		[HideInInspector][NonSerialized] public bool					isActive				= false; // this allows us to detect if we're enabled/disabled
		
		
		public void ClearCache()
		{
			parentData.Reset();
			generatedMeshes		= null;
			forceUpdate			= false;
			isActive			= false;
		}
#endif
		#endregion

		#region Settings

		[EnumAsFlags] public ModelSettingsFlags				Settings		= DefaultSettings;
		[EnumAsFlags] public Foundation.VertexChannelFlags	VertexChannels	= Foundation.VertexChannelFlags.All;
#if UNITY_2019_2_OR_NEWER
        [EnumAsFlags] public ReceiveGI                      ReceiveGI       = ReceiveGI.LightProbes;
#endif

#if UNITY_2017_3_OR_NEWER
        [EnumAsFlags] public MeshColliderCookingOptions MeshColliderCookingOptions = MeshColliderCookingOptions.CookForFasterSimulation |
                                                                                     MeshColliderCookingOptions.EnableMeshCleaning |
                                                                                     MeshColliderCookingOptions.WeldColocatedVertices;
        #endif

		#endregion
		
		#region Editor Settings
        #if UNITY_EDITOR
        
        public bool             ShowGeneratedMeshes     = false;
		public PhysicMaterial   DefaultPhysicsMaterial  = null;

        #region Export settings
		public ExportType		exportType				= ExportType.FBX;
		public OriginType		originType				= OriginType.ModelCenter;
		public bool				exportColliders			= false;
		public string			exportPath				= null;
        #endregion

        #endif

        #region Lightmap settings
        public const float	MinAngleError    = 0.001f;
		public const float	MinAreaError     = 0.001f;
		public const float	MaxAngleError    = 1.000f;
		public const float	MaxAreaError     = 1.000f;

		// Note: contents of UnityEditor.UnwrapParam, which is not marked serializable :(
		public float			angleError				= 0.08f;
		public float			areaError				= 0.15f;
		public float			hardAngle				= 88.0f;
		public float			packMargin				= 4.0f / 1024.0f;
		
		public float			scaleInLightmap			= 1.0f;
		public float			autoUVMaxDistance		= 0.5f;
		public float			autoUVMaxAngle			= 89.0f;
		public int				minimumChartSize		= 4;
		#endregion

		#endregion


		#region Events
#if UNITY_EDITOR
		// register ourselves with our scene manager
		internal void Awake()
		{
			// cannot change visibility since this might have an effect on exporter
			this.hideFlags |= HideFlags.DontSaveInBuild;
			ComponentUpgrader.UpgradeWhenNecessary(this);
			if (CSGSceneManagerRedirector.Interface != null) CSGSceneManagerRedirector.Interface.OnCreated(this);
		}

		internal void OnEnable()					{ if (CSGSceneManagerRedirector.Interface != null) CSGSceneManagerRedirector.Interface.OnEnabled(this); }
		internal void OnDisable()					{ if (CSGSceneManagerRedirector.Interface != null) CSGSceneManagerRedirector.Interface.OnDisabled(this); }
		internal void OnDestroy()					{ if (CSGSceneManagerRedirector.Interface != null) CSGSceneManagerRedirector.Interface.OnDestroyed(this); }
		internal void OnTransformChildrenChanged()	{ if (CSGSceneManagerRedirector.Interface != null) CSGSceneManagerRedirector.Interface.OnTransformChildrenChanged(this); }
		 
		
		// called when any value of this brush has been modified from within the inspector / or recompile
		// on recompile causes our data to be forgotten, yet awake isn't called
		internal void OnValidate()					{ if (CSGSceneManagerRedirector.Interface != null) CSGSceneManagerRedirector.Interface.OnValidate(this); }

		internal void Update()						{ if (CSGSceneManagerRedirector.Interface != null && !IsRegistered) CSGSceneManagerRedirector.Interface.OnUpdate(this); }
#endif
		#endregion

#if UNITY_EDITOR
		public void EnsureInitialized()				{ CSGSceneManagerRedirector.Interface.EnsureInitialized(this); }
#endif
	}
}
