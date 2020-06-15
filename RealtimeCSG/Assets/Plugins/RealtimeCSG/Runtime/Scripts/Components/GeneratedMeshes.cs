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
	[Serializable]
	public sealed class HelperSurfaceDescription
	{
		public Mesh						SharedMesh;
		public RenderSurfaceType		RenderSurfaceType = (RenderSurfaceType)999;
		public bool						HasGeneratedNormals;
		public GeneratedMeshDescription MeshDescription;
        public GameObject   GameObject;
        public MeshFilter   MeshFilter;
        public MeshRenderer MeshRenderer; 

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

        public void Destroy()
        {
            GameObjectExtensions.Destroy(GameObject);
            GameObject = null;
            MeshFilter = null;
            MeshRenderer = null;
        }
    }
#endif

    [DisallowMultipleComponent]
	[ExecuteInEditMode]
	[SelectionBase]
	public sealed class GeneratedMeshes : MonoBehaviour
	{
		[HideInInspector] public float Version = 1.00f;
#if UNITY_EDITOR
		public CSGModel				owner;

		[NonSerialized] [HideInInspector] public Rigidbody		CachedRigidBody;

		[NonSerialized] private GeneratedMeshInstance[]         meshInstances;
		[SerializeField] private HelperSurfaceDescription[]     helperSurfaces;

        static readonly GeneratedMeshInstance[]    emptyMeshInstances   = new GeneratedMeshInstance[0];
        static readonly HelperSurfaceDescription[] emptyHelperSurfaces  = new HelperSurfaceDescription[0];


        public bool HasMeshInstances { get { return meshInstances != null && meshInstances.Length > 0; } }
        public bool HasHelperSurfaces { get { return helperSurfaces != null && helperSurfaces.Length > 0; } }

        public GeneratedMeshInstance[]      MeshInstances   {  get { if (meshInstances == null) return emptyMeshInstances; else return meshInstances; } }
        public HelperSurfaceDescription[]   HelperSurfaces  {  get { if (helperSurfaces == null) return emptyHelperSurfaces; else return helperSurfaces; } }

        public HelperSurfaceDescription GetHelperSurface(MeshInstanceKey key)
        {
            if (helperSurfaces == null)
                return null;
            for (int i = 0; i < helperSurfaces.Length; i++)
            {
                if (helperSurfaces[i].GenerateKey() == key)
                    return helperSurfaces[i];
            }
            return null;
        }

        public void AddHelperSurface(HelperSurfaceDescription instance)
        {
            if (instance != null)
                return;
            if (helperSurfaces == null)
                return;
            var key = instance.GenerateKey();
            for (int i = 0; i < helperSurfaces.Length; i++)
            {
                if (helperSurfaces[i].SharedMesh &&
                    helperSurfaces[i].GenerateKey() == key)
                {
                    helperSurfaces[i].Destroy();
                    helperSurfaces[i] = instance;
                    return;
                }
            }
            UnityEditor.ArrayUtility.Add(ref helperSurfaces, instance);
        }

        public GeneratedMeshInstance GetMeshInstance(MeshInstanceKey key)
        {
            if (meshInstances == null)
                return null;
            for (int i = 0; i < meshInstances.Length; i++)
            {
                var instanceKey = meshInstances[i].GenerateKey();
                if (instanceKey == key)
                    return meshInstances[i];
            }
            return null;
        }
         
        public void AddMeshInstance(GeneratedMeshInstance instance)
        {
            if (!instance)
                return;

            if (meshInstances == null)
            {
                meshInstances = new GeneratedMeshInstance[] { instance };
                return;
            }

            var key = instance.GenerateKey();
            for (int i = 0; i < meshInstances.Length; i++)
            {
                if (meshInstances[i].GenerateKey() == key)
                {
                    meshInstances[i] = instance;
                    return;
                }
            }
            UnityEditor.ArrayUtility.Add(ref meshInstances, instance);
        }

        public void SetHelperSurfaces(HashSet<HelperSurfaceDescription> foundInstances)
        {
            if (helperSurfaces != null &&
                foundInstances.Count == helperSurfaces.Length)
            {
                bool differenceFound = false;
                for (int i = 0; i < helperSurfaces.Length; i++)
                {
                    if (foundInstances.Contains(helperSurfaces[i]))
                    {
                        differenceFound = true;
                        break;
                    }
                }
                if (!differenceFound)
                    return;
            }
            if (helperSurfaces != null)
            {
                foreach (var helperSurface in helperSurfaces)
                    helperSurface.Destroy();
            }
            if (foundInstances.Count == 0)
            {
                helperSurfaces = null;
                return;
            }
            if (helperSurfaces == null ||
                foundInstances.Count != helperSurfaces.Length)
                helperSurfaces = new HelperSurfaceDescription[foundInstances.Count];
            {
                int i = 0;
                foreach (var item in foundInstances)
                {
                    helperSurfaces[i] = item;
                    i++;
                }
            }
        }
        public void SetMeshInstances(HashSet<GeneratedMeshInstance> foundInstances)
        {
            if (meshInstances != null &&
                foundInstances.Count == meshInstances.Length)
            {
                bool differenceFound = false;
                for (int i = 0; i < meshInstances.Length; i++)
                {
                    if (foundInstances.Contains(meshInstances[i]))
                    {
                        differenceFound = true;
                        break;
                    }
                }
                if (!differenceFound)
                    return;
            }
            if (foundInstances.Count == 0)
            {
                meshInstances = null;
                return;
            }
            if (meshInstances == null ||
                foundInstances.Count != meshInstances.Length)
                meshInstances = new GeneratedMeshInstance[foundInstances.Count];
            {
                int i = 0;
                foreach (var item in foundInstances)
                {
                    meshInstances[i] = item;
                    i++;
                }
            }
        }

        public void SetMeshInstances(List<GeneratedMeshInstance> foundInstances)
        {
            if (meshInstances != null &&
                foundInstances.Count == meshInstances.Length)
            {
                bool differenceFound = false;
                for (int i = 0; i < meshInstances.Length; i++)
                {
                    bool found = false;
                    for (int j = 0; j < foundInstances.Count; j++)
                    {
                        if (meshInstances[i] == foundInstances[j])
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        differenceFound = true;
                        break;
                    }
                }
                if (!differenceFound)
                    return;
            }
            if (foundInstances.Count == 0)
            {
                meshInstances = null;
                return;
            }
            if (meshInstances == null ||
                foundInstances.Count != meshInstances.Length)
                meshInstances = new GeneratedMeshInstance[foundInstances.Count];
            for (int i = 0; i < foundInstances.Count; i++)
                meshInstances[i] = foundInstances[i];
        }

        public void RemoveMeshInstances(MeshInstanceKey[] removeKeys, int count)
        {
            if (count == meshInstances.Length)
            {
                meshInstances = null;
                return;
            }

            var oldInstances = meshInstances.ToList();
            for (int j = oldInstances.Count - 1; j >= 0; j--)
            {
                var key = oldInstances[j].GenerateKey();
                for (int i = 0; i < removeKeys.Length; i++)
                {
                    var removeKey = removeKeys[i];
                    if (removeKey != key)
                        continue;
                    oldInstances.RemoveAt(j);
                    break;
                }
            }
            meshInstances = oldInstances.ToArray();
        }

        public bool HasMeshInstance(MeshInstanceKey key) { return GetMeshInstance(key) != null; }


        void Awake()
		{
			// cannot change visibility since this might have an effect on exporter
			this.gameObject.hideFlags = HideFlags.None;
			this.hideFlags = HideFlags.None;//HideFlags.DontSaveInBuild;
			if (CSGSceneManagerRedirector.Interface != null)
				CSGSceneManagerRedirector.Interface.OnCreated(this);
		}
        
		void OnEnable()
		{
            meshInstances = this.GetComponentsInChildren<GeneratedMeshInstance>();
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

            GameObjectExtensions.Destroy(this.gameObject);
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
