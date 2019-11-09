using System;
using UnityEngine;
using System.Collections.Generic;
using RealtimeCSG.Components;
using System.Linq;
using UnityEngine.Serialization;
using RealtimeCSG.Foundation;

namespace InternalRealtimeCSG
{
	[Serializable]
	public enum RenderSurfaceType
	{
		Normal,
		[FormerlySerializedAs("Discarded")] Hidden,	// manually hidden by user
		[FormerlySerializedAs("Invisible")] Culled, // removed by CSG process
		ShadowOnly,									// surface that casts shadows
		Collider,
		Trigger,

		CastShadows,								// surface that casts shadows
		ReceiveShadows								// surface that receive shadows
	}

#if UNITY_EDITOR
	[Serializable, System.Reflection.Obfuscation(Exclude = true)]
	public sealed class HelperSurfaceDescription
	{
		public Mesh						SharedMesh;
		public RenderSurfaceType		RenderSurfaceType = (RenderSurfaceType)999;
		public GeneratedMeshContents	GeneratedMeshContents;
		public bool						HasGeneratedNormals;
		public GeneratedMeshDescription MeshDescription;

		public MeshInstanceKey GenerateKey()
		{
			return MeshInstanceKey.GenerateKey(MeshDescription);
		}

		public bool IsValid()
		{
			if (SharedMesh)
			{
				if (SharedMesh.vertexCount < 0)
					return false;
			} else
			if (SharedMesh.GetInstanceID() != 0)
				return false;
			return true;
		}
	}
#endif

	[DisallowMultipleComponent]
	[ExecuteInEditMode]
	[SelectionBase]
	[System.Reflection.Obfuscation(Exclude = true)]
	public sealed class GeneratedMeshes : MonoBehaviour
#if UNITY_EDITOR
		, ISerializationCallbackReceiver
#endif
	{
		[HideInInspector] public float Version = 1.00f;
#if UNITY_EDITOR
		public CSGModel				owner;

		[NonSerialized] [HideInInspector] public Rigidbody		CachedRigidBody;

		[SerializeField] private GeneratedMeshInstance[] meshInstances;
		[NonSerialized] public readonly Dictionary<MeshInstanceKey, GeneratedMeshInstance> meshInstanceLookup = new Dictionary<MeshInstanceKey, GeneratedMeshInstance>();
		
		[SerializeField] private HelperSurfaceDescription[] helperSurfaces;
		[NonSerialized] public readonly Dictionary<MeshInstanceKey, HelperSurfaceDescription> helperSurfaceLookup = new Dictionary<MeshInstanceKey, HelperSurfaceDescription>();

		public void SetHelperSurfaces(HelperSurfaceDescription[] descriptions)
		{
			helperSurfaceLookup.Clear();
			if (descriptions != null)
			{
				foreach (var helperSurface in descriptions)
				{
					var key = helperSurface.GenerateKey();
					helperSurfaceLookup[key] = helperSurface;
				}
			}
			helperSurfaces = descriptions;
		}

		public void SetMeshInstances(GeneratedMeshInstance[] instances)
		{
			meshInstanceLookup.Clear();
			if (instances != null)
			{
				foreach (var instance in instances)
				{
					if (!instance)
						continue;
					var key = instance.GenerateKey();
					meshInstanceLookup[key] = instance;
				}
			}
			meshInstances = meshInstanceLookup.Values.ToArray();
		}

		public void OnAfterDeserialize()
		{
			SetHelperSurfaces(helperSurfaces);
			SetMeshInstances(meshInstances);
		}

		public void OnBeforeSerialize()
		{
			helperSurfaces = helperSurfaceLookup.Values.ToArray();
			meshInstances = meshInstanceLookup.Values.ToArray();
		}


		void Awake()
		{
			// cannot change visibility since this might have an effect on exporter
			this.gameObject.hideFlags = HideFlags.None;
			this.hideFlags = HideFlags.DontSaveInBuild;
			if (CSGSceneManagerRedirector.Interface != null)
				CSGSceneManagerRedirector.Interface.OnCreated(this);
		} 

		void OnDestroy()
		{
			if (CSGSceneManagerRedirector.Interface != null)
				CSGSceneManagerRedirector.Interface.OnDestroyed(this);
		}

		// Unity bug workaround
		private void Update()
		{
			// we need to kill a dangling "generated-meshes" when deleting prefab instance in scene
			if (owner)
				return;
			
			UnityEngine.Object.DestroyImmediate(this.gameObject);
		}
#else
			void Awake()
		{
			//this.hideFlags = HideFlags.DontSaveInBuild;
			this.gameObject.tag = "EditorOnly"; 
		}
#endif
	}
}
