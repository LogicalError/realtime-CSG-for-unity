using System;
using UnityEngine;
using InternalRealtimeCSG;
using UnityEngine.Serialization;
using CSGOperationType = RealtimeCSG.Foundation.CSGOperationType;


namespace RealtimeCSG.Components
{

	/// <summary>Holds a CSG tree branch</summary>
	/// <remarks>The CSG branch that defines a CSGOperation is defined by its child [UnityEngine.GameObject](https://docs.unity3d.com/ScriptReference/GameObject.html)s.</remarks>
#if UNITY_EDITOR
	[AddComponentMenu("CSG/Operation")]
    [ExecuteInEditMode, DisallowMultipleComponent]
#endif
	public sealed class CSGOperation : CSGNode
	{
		public const float CurrentVersion = 1.1f;
		/// <value>The version number of this instance of a <see cref="CSGOperation" /></value>
		[HideInInspector] public float Version = CurrentVersion;

#if UNITY_EDITOR
		public bool IsRegistered { get { return operationNodeID != CSGNode.InvalidNodeID; } }
#endif


		#region Settings
#if UNITY_EDITOR

		/// <value>The CSG operation to perform with this CSG branch</value>
		public Foundation.CSGOperationType OperationType = Foundation.CSGOperationType.Additive;

        [HideInInspector][SerializeField] internal bool	passThrough		= false;
		public bool PassThrough
		{
			get { return passThrough; }
			set
			{
				if (passThrough == value)
					return;
				OnDisable(); passThrough = value; OnEnable();
				if (CSGSceneManagerRedirector.Interface != null) CSGSceneManagerRedirector.Interface.OnPassthroughChanged(this);
			}
		}
#endif
		#endregion

		#region Editor Settings
#if UNITY_EDITOR

		[FormerlySerializedAs("selectOnChild")]
        public bool	HandleAsOne		= false;
#endif
		#endregion

		#region Cached values
#if UNITY_EDITOR
		[HideInInspector][NonSerialized] public Int32				operationNodeID	= CSGNode.InvalidNodeID;
        [HideInInspector][NonSerialized] public ParentNodeData		ParentData		= new ParentNodeData();
        [HideInInspector][NonSerialized] public ChildNodeData		ChildData		= new ChildNodeData();

		// this allows us to detect if our operation has been modified
        [HideInInspector][NonSerialized] public CSGOperationType	PrevOperation	= (CSGOperationType)0xff;
		[HideInInspector][NonSerialized] public bool				PrevPassThrough = false;
		
		public void ClearCache()
		{
			ParentData.Reset();
			ChildData.Reset();
			PrevOperation	= (CSGOperationType)0xff;
			PrevPassThrough = false;
		}
#endif
		#endregion

		#region Events
#if UNITY_EDITOR
		// register ourselves with our scene manager
		internal void Awake()
		{
			// cannot change visibility since this might have an effect on exporter
			this.hideFlags |= HideFlags.DontSaveInBuild;
			ComponentUpgrader.UpgradeWhenNecessary(this); ;
			if (CSGSceneManagerRedirector.Interface != null) CSGSceneManagerRedirector.Interface.OnCreated(this);
		}
        internal void OnEnable()					{ if (CSGSceneManagerRedirector.Interface != null && !passThrough) CSGSceneManagerRedirector.Interface.OnEnabled(this); }

        // unregister ourselves from our scene manager
        internal void OnDisable()					{ if (CSGSceneManagerRedirector.Interface != null && !passThrough) CSGSceneManagerRedirector.Interface.OnDisabled(this); }
        internal void OnDestroy()					{ if (CSGSceneManagerRedirector.Interface != null) CSGSceneManagerRedirector.Interface.OnDestroyed(this); }
        
		// detect if this node has been moved within the hierarchy
		internal void OnTransformParentChanged()	{ if (CSGSceneManagerRedirector.Interface != null && !passThrough) CSGSceneManagerRedirector.Interface.OnTransformParentChanged(this); }

		// called when any value of this brush has been modified from within the inspector / or recompile
		internal void OnValidate()					{ if (CSGSceneManagerRedirector.Interface != null && !passThrough) CSGSceneManagerRedirector.Interface.OnValidate(this); }
#endif
		#endregion
		
#if UNITY_EDITOR
		public void EnsureInitialized()				{ if (CSGSceneManagerRedirector.Interface != null && !passThrough) CSGSceneManagerRedirector.Interface.EnsureInitialized(this); }
#endif
    }
}

