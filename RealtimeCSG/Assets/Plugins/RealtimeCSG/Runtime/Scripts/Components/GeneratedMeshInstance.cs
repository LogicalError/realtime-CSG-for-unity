using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Serialization;
using MeshQuery = RealtimeCSG.Foundation.MeshQuery;
using GeneratedMeshDescription = RealtimeCSG.Foundation.GeneratedMeshDescription;
using RealtimeCSG.Foundation;

namespace InternalRealtimeCSG
{
#if UNITY_EDITOR
	public struct MeshInstanceKey : IEqualityComparer<MeshInstanceKey>, IEquatable<MeshInstanceKey> 
	{
		public static MeshInstanceKey GenerateKey(GeneratedMeshDescription meshDescription)
		{
			return new MeshInstanceKey(meshDescription.meshQuery, meshDescription.surfaceParameter, meshDescription.subMeshQueryIndex);
		}

		private MeshInstanceKey(MeshQuery meshType, int surfaceParameter, int subMeshIndex)
		{
			SubMeshIndex		= subMeshIndex;
			MeshType			= meshType;
			SurfaceParameter	= surfaceParameter;
		}

		public readonly int  SubMeshIndex;
		public int			 SurfaceParameter;
		public readonly MeshQuery MeshType;

        public override string ToString()
        {
            return string.Format("({0} {1} {2})", SubMeshIndex, SurfaceParameter, MeshType);
        }

        #region Comparison
        public override int GetHashCode()
		{
			var hash1 = SubMeshIndex     .GetHashCode();
			var hash2 = SurfaceParameter .GetHashCode();
			var hash3 = MeshType		 .GetHashCode();
			var hash = hash1;
			hash *= 389 + hash2;
			hash *= 397 + hash3;

			return hash + (hash1 ^ hash2 ^ hash3) + (hash1 + hash2 + hash3) + (hash1 * hash2 * hash3);
		}

		public int GetHashCode(MeshInstanceKey obj)
		{
			return obj.GetHashCode();
		}

		public bool Equals(MeshInstanceKey other)
		{
			if (System.Object.ReferenceEquals(this, other))
				return true;
			if (System.Object.ReferenceEquals(other, null))
				return false;
			return SubMeshIndex == other.SubMeshIndex &&
				   SurfaceParameter == other.SurfaceParameter &&
				   MeshType == other.MeshType;
		}

		public override bool Equals(object obj)
		{
			if (System.Object.ReferenceEquals(this, obj))
				return true;
			if (!(obj is MeshInstanceKey))
				return false;
			MeshInstanceKey other = (MeshInstanceKey)obj;
			if (System.Object.ReferenceEquals(other, null))
				return false;
			return SubMeshIndex == other.SubMeshIndex &&
				   SurfaceParameter == other.SurfaceParameter &&
				   MeshType == other.MeshType;
		}

		public bool Equals(MeshInstanceKey left, MeshInstanceKey right)
		{
			if (System.Object.ReferenceEquals(left, right))
				return true;
			if (System.Object.ReferenceEquals(left, null) ||
				System.Object.ReferenceEquals(right, null))
				return false;
			return left.SubMeshIndex == right.SubMeshIndex &&
				   left.SurfaceParameter == right.SurfaceParameter &&
				   left.MeshType == right.MeshType;
		}

		public static bool operator ==(MeshInstanceKey left, MeshInstanceKey right)
		{
			if (System.Object.ReferenceEquals(left, right))
				return true;
			if (System.Object.ReferenceEquals(left, null) ||
				System.Object.ReferenceEquals(right, null))
				return false;
			return left.SubMeshIndex == right.SubMeshIndex &&
				   left.SurfaceParameter == right.SurfaceParameter &&
				   left.MeshType == right.MeshType;
		}

		public static bool operator !=(MeshInstanceKey left, MeshInstanceKey right)
		{
			if (System.Object.ReferenceEquals(left, right))
				return false;
			if (System.Object.ReferenceEquals(left, null) ||
				System.Object.ReferenceEquals(right, null))
				return true;
			return left.SubMeshIndex != right.SubMeshIndex ||
				   left.SurfaceParameter != right.SurfaceParameter ||
				   left.MeshType != right.MeshType;
		}
		#endregion
	}
#endif

	[DisallowMultipleComponent]
	[ExecuteInEditMode]
	public sealed class GeneratedMeshInstance : MonoBehaviour
	{
		[HideInInspector] public float Version = 1.00f;
#if UNITY_EDITOR
		public Mesh					SharedMesh;
		public Material				RenderMaterial;
		public PhysicMaterial		PhysicsMaterial;
		public RenderSurfaceType	RenderSurfaceType = (RenderSurfaceType)999;

		public GeneratedMeshDescription MeshDescription;

		[HideInInspector] public bool   HasGeneratedNormals = false;
		[HideInInspector] public bool	HasUV2				= false;
        [NonSerialized]
		[HideInInspector] public float	ResetUVTime			= float.PositiveInfinity;
		[HideInInspector] public Int64	LightingHashValue;

		[NonSerialized] [HideInInspector] public bool Dirty	= true;
		[NonSerialized] [HideInInspector] public MeshCollider	CachedMeshCollider;
		[NonSerialized] [HideInInspector] public MeshFilter		CachedMeshFilter;
		[NonSerialized] [HideInInspector] public MeshRenderer	CachedMeshRenderer;
		[NonSerialized] [HideInInspector] public System.Object	CachedMeshRendererSO;

        public void Reset()
        {
		    RenderMaterial          = null;
		    PhysicsMaterial         = null;
		    RenderSurfaceType       = (RenderSurfaceType)999;
        
		    HasGeneratedNormals     = false;
		    HasUV2				    = false;
            ResetUVTime			    = float.PositiveInfinity;
		    LightingHashValue       = 0;

		    Dirty	                = true;

		    CachedMeshCollider      = null;
		    CachedMeshFilter        = null;
		    CachedMeshRenderer      = null;
		    CachedMeshRendererSO    = null;
        }

		public MeshInstanceKey GenerateKey()
		{
			return MeshInstanceKey.GenerateKey(MeshDescription);
		}

		public bool IsValid()
		{
			if ((!PhysicsMaterial || PhysicsMaterial.GetInstanceID() != 0) &&
				(!RenderMaterial  || RenderMaterial .GetInstanceID() != 0))
			{
                if (SharedMesh)
                {
                    if (SharedMesh.vertexCount < 0)
                        return false;
                } else
                if (!ReferenceEquals(SharedMesh, null) &&
                    SharedMesh.GetInstanceID() != 0)
                    return false;
				return true;
			}
			return false;
		}

        static readonly List<Vector2> sUVList = new List<Vector2>();

		internal void Awake()
		{
			// cannot change visibility since this might have an effect on exporter
			this.gameObject.hideFlags = HideFlags.DontSaveInBuild;
			this.hideFlags = HideFlags.DontSaveInBuild;


			// Unity bug workaround
			CachedMeshCollider = null;
			CachedMeshFilter = null;
			CachedMeshRenderer = null;
			CachedMeshRendererSO = null;

			// InstanceIDs are not properly remembered across domain reloads,
			//	this causes issues on, for instance, first startup of Unity. 
			//	So we need to refresh the instanceIDs
            if (RenderSurfaceType == RenderSurfaceType.Collider ||
                RenderSurfaceType == RenderSurfaceType.Trigger)
            {
                if (!ReferenceEquals(PhysicsMaterial, null)) { if (PhysicsMaterial) MeshDescription.surfaceParameter = PhysicsMaterial.GetInstanceID(); }
            } else
            { 
			    if      (!ReferenceEquals(RenderMaterial,  null)) { if (RenderMaterial)  MeshDescription.surfaceParameter = RenderMaterial .GetInstanceID(); }
			    else if (!ReferenceEquals(PhysicsMaterial, null)) { if (PhysicsMaterial) MeshDescription.surfaceParameter = PhysicsMaterial.GetInstanceID(); }
            }
		}

		internal void OnEnable()
		{
			// Workaround for when Unity gets confused with some prefab instance merging and moves 
			//	the GeneratedMeshInstances out of its GeneratedMeshes container
			if (transform.parent &&
				!transform.parent.GetComponent<GeneratedMeshes>())
			{
				this.gameObject.hideFlags = HideFlags.None;
				try
				{
					UnityEngine.Object.DestroyImmediate(gameObject);
				}
				catch
				{
					// Work-around for nested prefab instance issues ..
					if (gameObject.activeSelf)
					{
						gameObject.SetActive(false);
						{
							var childComponents = gameObject.GetComponentsInChildren<MonoBehaviour>();
							foreach (var component in childComponents)
								UnityEngine.Object.DestroyImmediate(component);
						}
						{
							var childComponents = gameObject.GetComponentsInChildren<Component>();
							foreach (var component in childComponents)
							{
								if (!(component is Transform))
									UnityEngine.Object.DestroyImmediate(component);
							}
						}
						gameObject.name = "removed";
					}
				}
			}

            if (SharedMesh)
            {
                SharedMesh.GetUVs(1, sUVList);
                HasUV2 = sUVList != null && sUVList.Count == SharedMesh.vertexCount;
            } else
                HasUV2 = false;
		}
        
        public void FindMissingSharedMesh()
        {
            MeshCollider meshCollider;
            if (this.TryGetComponent(out meshCollider))
            {
                SharedMesh = meshCollider.sharedMesh;
                return;
            }
            MeshFilter meshFilter;
            if (this.TryGetComponent(out meshFilter))
            {
                SharedMesh = meshFilter.sharedMesh;
            }
        }
#else
        void Awake() 
		{
			this.hideFlags = HideFlags.DontSaveInBuild;
		}
#endif
	}
}