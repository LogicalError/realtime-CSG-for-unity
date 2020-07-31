using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using InternalRealtimeCSG;
using RealtimeCSG.Foundation;
using RealtimeCSG.Components;

namespace RealtimeCSG
{
    internal sealed partial class CSGModelComponentInspectorGUI
    {
        private class MeshData
        {
            public Int32	VertexCount;
            public Int32	TriangleCount;
            public Int64	GeometryHashValue;
            public Int64	SurfaceHashValue;
            public Mesh		Mesh;
        }
        
        #region Workarounds 
        private static Type				_probesType;
        private static System.Object	_probesInstance;
        private static MethodInfo		_probesInitializeMethod;
        private static MethodInfo		_probesOnGUIMethod;
        private static MethodInfo       _sceneViewIsUsingDeferredRenderingPath;
        private static bool				_haveReflected = false;

        private static void InitReflection() // le *sigh*
        {
            if (_haveReflected)
                return;

            _haveReflected = true;
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            var types = new List<System.Type>();
            foreach(var assembly in assemblies)
            {
                try
                {
                    types.AddRange(assembly.GetTypes());
                }
                catch { }
            }

            _probesType 		= types.FirstOrDefault(t => t.FullName == "UnityEditor.RendererEditorBase+Probes");
            _probesInstance	= Activator.CreateInstance(_probesType);

            var methods = _probesType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic);
            for (int i = 0; i < methods.Length; i++)
            {
                //internal void Initialize(SerializedObject serializedObject)
                if (methods[i].Name == "Initialize")
                {
                    if (methods[i].GetParameters().Length == 1)
                        _probesInitializeMethod = methods[i];
                } else
                    //internal void OnGUI(UnityEngine.Object[] selection, Renderer renderer, bool useMiniStyle)
                if (methods[i].Name == "OnGUI")
                {
                    if (methods[i].GetParameters().Length == 3)
                        _probesOnGUIMethod = methods[i];
                }
            }
            
            methods = typeof(SceneView).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static);
            for (int i = 0; i < methods.Length; i++)
            {
                if (methods[i].Name == "IsUsingDeferredRenderingPath")
                {
                    _sceneViewIsUsingDeferredRenderingPath = methods[i];
                }
            }
        }



        internal static Camera GetMainCamera()
        {
            // main camera, if we have any
            var mainCamera = Camera.main;
            if (mainCamera != null)
                return mainCamera;

            // if we have one camera, return it
            Camera[] allCameras = Camera.allCameras;
            if (allCameras != null && allCameras.Length == 1)
                return allCameras[0];

            // otherwise no "main" camera
            return null;
        }

        internal static RenderingPath GetSceneViewRenderingPath()
        {
            var mainCamera = GetMainCamera ();
            if (mainCamera != null)
                return mainCamera.renderingPath;
            return RenderingPath.UsePlayerSettings;
        }

        internal static bool IsUsingDeferredRenderingPath()
        {
            if (_sceneViewIsUsingDeferredRenderingPath != null)
            {
                var ret = _sceneViewIsUsingDeferredRenderingPath.Invoke(null, null);
                return (bool) ret;
            } else
                return false;
        }
        #endregion

        private static bool					_probesInitialized = false;
        private static UnityEngine.Object[] _probesTargets = null;
        private static SerializedObject		_probesSerializedObject;
        
        static void UpdateTargets(CSGModel[] models)
        {
            _probesInitialized = false;
            _probesTargets = null;
            _probesSerializedObject = null;
            if (models.Length == 0)
                return;
            
            _probesTargets = MeshInstanceManager.FindRenderers(models);
            if (_probesTargets.Length == 0 || _probesTargets == null)
                return; 

            _probesSerializedObject = new SerializedObject(_probesTargets);

            InitReflection();
            if (_probesInstance != null &&
                _probesInitializeMethod != null && 
                _probesOnGUIMethod != null)
            {
                if (_probesTargets.Length > 0)
                {
                    _probesInitializeMethod.Invoke(_probesInstance, new System.Object[] { _probesSerializedObject });
                    _probesInitialized = true;
                }
            }
        }

        static GUIStyle popupStyle;

        static Material dummyMaterial;
        static bool localStyles = false;
        
        public static void OnInspectorGUI(UnityEngine.Object[] targets)
        {
            InitReflection();
            if (!localStyles)
            {
                popupStyle = new GUIStyle(EditorStyles.popup);
                //popupStyle.padding.top += 2;
                popupStyle.margin.top += 2;
                localStyles = true;
            }


            bool updateMeshes = false;

            var models = new CSGModel[targets.Length];

            for (int i = targets.Length - 1; i >= 0; i--)
            {
                models[i] = targets[i] as CSGModel;
                if (!models[i])
                {
                    ArrayUtility.RemoveAt(ref models, i);
                }
            }

            if (models.Length == 0)
                return;

            
            float? AngleError	= models[0].angleError;
            float? AreaError	= models[0].areaError;
            float? HardAngle	= models[0].hardAngle;
            float? PackMargin	= models[0].packMargin;
            
            var		StaticFlags				= GameObjectUtility.GetStaticEditorFlags(models[0].gameObject);
#if UNITY_2019_2_OR_NEWER 
            bool?	GenerateLightMaps		= (StaticFlags & StaticEditorFlags.ContributeGI) == StaticEditorFlags.ContributeGI;
#else
            bool? GenerateLightMaps         = (StaticFlags & StaticEditorFlags.LightmapStatic) == StaticEditorFlags.LightmapStatic;            
#endif
            var		Settings				= models[0].Settings;
            var		VertexChannels			= models[0].VertexChannels;
#if UNITY_2017_3_OR_NEWER
            var     CookingOptions          = models[0].MeshColliderCookingOptions;
#endif
            ExportType? ExportType			= models[0].exportType;
            OriginType? OriginType			= models[0].originType;
#if UNITY_2019_2_OR_NEWER
            ReceiveGI?  MeshReceiveGI		= models[0].ReceiveGI;
#endif
            bool?	ExportColliders			= models[0].exportColliders;
//			bool?	VertexChannelColor		= (vertexChannels & VertexChannelFlags.Color) == VertexChannelFlags.Color;
            bool?	VertexChannelTangent	= (VertexChannels & VertexChannelFlags.Tangent) == VertexChannelFlags.Tangent;
            bool?	VertexChannelNormal		= (VertexChannels & VertexChannelFlags.Normal) == VertexChannelFlags.Normal;
            bool?	VertexChannelUV0		= (VertexChannels & VertexChannelFlags.UV0) == VertexChannelFlags.UV0;
            bool?	InvertedWorld			= (Settings & ModelSettingsFlags.InvertedWorld) == ModelSettingsFlags.InvertedWorld;
            bool?	NoCollider				= (Settings & ModelSettingsFlags.NoCollider) == ModelSettingsFlags.NoCollider;
            bool?	IsTrigger				= (Settings & ModelSettingsFlags.IsTrigger) == ModelSettingsFlags.IsTrigger;
            bool?	SetToConvex				= (Settings & ModelSettingsFlags.SetColliderConvex) == ModelSettingsFlags.SetColliderConvex;
            bool?	AutoGenerateRigidBody	= (Settings & ModelSettingsFlags.AutoUpdateRigidBody) == ModelSettingsFlags.AutoUpdateRigidBody;
            bool?	DoNotRender				= (Settings & ModelSettingsFlags.DoNotRender) == ModelSettingsFlags.DoNotRender;
            bool?	TwoSidedShadows			= (Settings & ModelSettingsFlags.TwoSidedShadows) == ModelSettingsFlags.TwoSidedShadows;
//			bool?	ReceiveShadows			= !((settings & ModelSettingsFlags.DoNotReceiveShadows) == ModelSettingsFlags.DoNotReceiveShadows);
            bool?	AutoRebuildUVs          = (Settings & ModelSettingsFlags.AutoRebuildUVs) == ModelSettingsFlags.AutoRebuildUVs;
            bool?	PreserveUVs             = (Settings & ModelSettingsFlags.PreserveUVs) == ModelSettingsFlags.PreserveUVs;
            bool?	StitchLightmapSeams     = (Settings & ModelSettingsFlags.StitchLightmapSeams) == ModelSettingsFlags.StitchLightmapSeams;
            bool?	IgnoreNormals			= (Settings & ModelSettingsFlags.IgnoreNormals) == ModelSettingsFlags.IgnoreNormals;
#if UNITY_2017_3_OR_NEWER
            bool?   CookForFasterSimulation = (CookingOptions & MeshColliderCookingOptions.CookForFasterSimulation) == MeshColliderCookingOptions.CookForFasterSimulation;
            bool?   EnableMeshCleaning      = (CookingOptions & MeshColliderCookingOptions.EnableMeshCleaning) == MeshColliderCookingOptions.EnableMeshCleaning;
            bool?   WeldColocatedVertices   = (CookingOptions & MeshColliderCookingOptions.WeldColocatedVertices) == MeshColliderCookingOptions.WeldColocatedVertices;
#endif
            int?	MinimumChartSize		= models[0].minimumChartSize;
            float?	AutoUVMaxDistance		= models[0].autoUVMaxDistance;
            float?	AutoUVMaxAngle			= models[0].autoUVMaxAngle;

            float?	ScaleInLightmap			= models[0].scaleInLightmap;
            bool?	ShowGeneratedMeshes		= models[0].ShowGeneratedMeshes;
//			ShadowCastingMode? ShadowCastingMode = (ShadowCastingMode)(settings & ModelSettingsFlags.ShadowCastingModeFlags);
            var	defaultPhysicsMaterial		= models[0].DefaultPhysicsMaterial;
            var	defaultPhysicsMaterialMixed = false;
            
            for (int i = 1; i< models.Length; i++)
            {
                Settings		= models[i].Settings;
                VertexChannels	= models[i].VertexChannels;
#if UNITY_2017_3_OR_NEWER
                CookingOptions  = models[i].MeshColliderCookingOptions;
#endif
                var		currStaticFlags				= GameObjectUtility.GetStaticEditorFlags(models[i].gameObject);
#if UNITY_2019_2_OR_NEWER
                var		currGenerateLightMaps		= (currStaticFlags & StaticEditorFlags.ContributeGI) == StaticEditorFlags.ContributeGI;
#else
                var currGenerateLightMaps           = (currStaticFlags & StaticEditorFlags.LightmapStatic) == StaticEditorFlags.LightmapStatic;
#endif
                ExportType currExportType			= models[i].exportType;
                OriginType  currOriginType			= models[i].originType;
#if UNITY_2019_2_OR_NEWER
                ReceiveGI   currReceiveGI           = models[i].ReceiveGI;
#endif
                bool currExportColliders			= models[i].exportColliders;
                float	currAngleError				= models[i].angleError;
                float	currAreaError				= models[i].areaError;
                float	currHardAngle				= models[i].hardAngle;
                float	currPackMargin				= models[i].packMargin;
//				bool	currVertexChannelColor		= (vertexChannels & VertexChannelFlags.Color) == VertexChannelFlags.Color;
                bool	currVertexChannelTangent	= (VertexChannels & VertexChannelFlags.Tangent) == VertexChannelFlags.Tangent;
                bool	currVertexChannelNormal		= (VertexChannels & VertexChannelFlags.Normal) == VertexChannelFlags.Normal;
                bool	currVertexChannelUV0		= (VertexChannels & VertexChannelFlags.UV0) == VertexChannelFlags.UV0;
                bool	currInvertedWorld			= (Settings & ModelSettingsFlags.InvertedWorld) == ModelSettingsFlags.InvertedWorld;
                bool	currNoCollider				= (Settings & ModelSettingsFlags.NoCollider) == ModelSettingsFlags.NoCollider;
                bool	currIsTrigger				= (Settings & ModelSettingsFlags.IsTrigger) == ModelSettingsFlags.IsTrigger;
                bool	currSetToConvex				= (Settings & ModelSettingsFlags.SetColliderConvex) == ModelSettingsFlags.SetColliderConvex;
                bool	currAutoGenerateRigidBody	= (Settings & ModelSettingsFlags.AutoUpdateRigidBody) == ModelSettingsFlags.AutoUpdateRigidBody;
                bool	currDoNotRender				= (Settings & ModelSettingsFlags.DoNotRender) == ModelSettingsFlags.DoNotRender;
                bool	currTwoSidedShadows			= (Settings & ModelSettingsFlags.TwoSidedShadows) == ModelSettingsFlags.TwoSidedShadows;
//				bool	currReceiveShadows			= !((settings & ModelSettingsFlags.DoNotReceiveShadows) == ModelSettingsFlags.DoNotReceiveShadows);
                bool	currAutoRebuildUVs			= (Settings & ModelSettingsFlags.AutoRebuildUVs) == ModelSettingsFlags.AutoRebuildUVs;
                bool	currPreserveUVs				= (Settings & ModelSettingsFlags.PreserveUVs) == ModelSettingsFlags.PreserveUVs;
                bool	currStitchLightmapSeams		= (Settings & ModelSettingsFlags.StitchLightmapSeams) == ModelSettingsFlags.StitchLightmapSeams;
#if UNITY_2017_3_OR_NEWER
                bool    currCookForFasterSimulation = (CookingOptions & MeshColliderCookingOptions.CookForFasterSimulation) == MeshColliderCookingOptions.CookForFasterSimulation;
                bool    currEnableMeshCleaning      = (CookingOptions & MeshColliderCookingOptions.EnableMeshCleaning) == MeshColliderCookingOptions.EnableMeshCleaning;
                bool    currWeldColocatedVertices   = (CookingOptions & MeshColliderCookingOptions.WeldColocatedVertices) == MeshColliderCookingOptions.WeldColocatedVertices;
#endif
                int		currMinimumChartSize		= models[i].minimumChartSize;
                float	currAutoUVMaxDistance		= models[i].autoUVMaxDistance;
                float	currAutoUVMaxAngle			= models[i].autoUVMaxAngle;
                bool	currShowGeneratedMeshes		= models[i].ShowGeneratedMeshes;
                float	currScaleInLightmap			= models[i].scaleInLightmap;
                var		currdefaultPhysicsMaterial	= models[i].DefaultPhysicsMaterial;
//				ShadowCastingMode currShadowCastingMode = (ShadowCastingMode)(settings & ModelSettingsFlags.ShadowCastingModeFlags);

//				if (VertexChannelColor		.HasValue && VertexChannelColor		.Value != currVertexChannelColor	) VertexChannelColor = null;
                if (VertexChannelTangent	.HasValue && VertexChannelTangent	.Value != currVertexChannelTangent	) VertexChannelTangent = null;
                if (VertexChannelNormal		.HasValue && VertexChannelNormal	.Value != currVertexChannelNormal	) VertexChannelNormal = null;
                if (VertexChannelUV0	    .HasValue && VertexChannelUV0		.Value != currVertexChannelUV0		) VertexChannelUV0 = null;
                
                if (ExportType				.HasValue && ExportType				.Value != currExportType			) ExportType = null;
                if (OriginType				.HasValue && OriginType				.Value != currOriginType			) OriginType = null;
#if UNITY_2019_2_OR_NEWER
                if (MeshReceiveGI           .HasValue && MeshReceiveGI          .Value != currReceiveGI             ) MeshReceiveGI = null;
#endif
                if (ExportColliders			.HasValue && ExportColliders		.Value != currExportColliders		) ExportColliders = null;
                
                if (InvertedWorld			.HasValue && InvertedWorld			.Value != currInvertedWorld			) InvertedWorld = null;
                if (NoCollider				.HasValue && NoCollider				.Value != currNoCollider			) NoCollider = null;
                if (IsTrigger				.HasValue && IsTrigger				.Value != currIsTrigger				) IsTrigger = null;
                if (SetToConvex				.HasValue && SetToConvex		    .Value != currSetToConvex			) SetToConvex = null;
                if (AutoGenerateRigidBody	.HasValue && AutoGenerateRigidBody	.Value != currAutoGenerateRigidBody ) AutoGenerateRigidBody = null;
                if (DoNotRender				.HasValue && DoNotRender		    .Value != currDoNotRender			) DoNotRender = null;
                if (TwoSidedShadows			.HasValue && TwoSidedShadows		.Value != currTwoSidedShadows		) TwoSidedShadows = null;
//				if (ReceiveShadows			.HasValue && ReceiveShadows			.Value != currReceiveShadows		) ReceiveShadows = null;
//				if (ShadowCastingMode		.HasValue && ShadowCastingMode		.Value != currShadowCastingMode		) ShadowCastingMode = null;
                if (AutoRebuildUVs     		.HasValue && AutoRebuildUVs     	.Value != currAutoRebuildUVs		) AutoRebuildUVs = null;
                if (PreserveUVs     		.HasValue && PreserveUVs     		.Value != currPreserveUVs	    	) PreserveUVs = null;
                if (StitchLightmapSeams		.HasValue && StitchLightmapSeams	.Value != currStitchLightmapSeams	) StitchLightmapSeams = null;

#if UNITY_2017_3_OR_NEWER
                if (CookForFasterSimulation	.HasValue && CookForFasterSimulation.Value != currCookForFasterSimulation) CookForFasterSimulation = null;
                if (EnableMeshCleaning		.HasValue && EnableMeshCleaning	    .Value != currEnableMeshCleaning	 ) EnableMeshCleaning = null;
                if (WeldColocatedVertices	.HasValue && WeldColocatedVertices	.Value != currWeldColocatedVertices	 ) WeldColocatedVertices = null;
#endif
                
                if (ScaleInLightmap    		.HasValue && ScaleInLightmap     	.Value != currScaleInLightmap	  	) ScaleInLightmap = null;
                if (ShowGeneratedMeshes		.HasValue && ShowGeneratedMeshes	.Value != currShowGeneratedMeshes	) ShowGeneratedMeshes = null;
            
                if (MinimumChartSize    	.HasValue && MinimumChartSize     	.Value != currMinimumChartSize	  	) MinimumChartSize = null;
                if (AutoUVMaxDistance    	.HasValue && AutoUVMaxDistance     	.Value != currAutoUVMaxDistance	  	) AutoUVMaxDistance = null;
                if (AutoUVMaxAngle			.HasValue && AutoUVMaxAngle			.Value != currAutoUVMaxAngle		) AutoUVMaxAngle = null;

                if (AngleError				.HasValue && AngleError           	.Value != currAngleError			) AngleError = null;
                if (AreaError				.HasValue && AreaError           	.Value != currAreaError				) AreaError = null;
                if (HardAngle				.HasValue && HardAngle           	.Value != currHardAngle				) HardAngle = null;
                if (PackMargin				.HasValue && PackMargin           	.Value != currPackMargin			) PackMargin = null;
                if (GenerateLightMaps		.HasValue && GenerateLightMaps    	.Value != currGenerateLightMaps		) GenerateLightMaps = null;

                
                
                if (defaultPhysicsMaterial != currdefaultPhysicsMaterial) defaultPhysicsMaterialMixed = true;
            }

            var behaviourVisible    = SessionState.GetBool("CSGModel.Behaviour", true);
            var exportVisible       = SessionState.GetBool("CSGModel.Export", true);
            var physicsVisible      = SessionState.GetBool("CSGModel.Physics", true);
            var renderingVisible    = SessionState.GetBool("CSGModel.Rendering", true);
            var UVSettingsVisible   = SessionState.GetBool("CSGModel.UVSettings", true);
            var meshAdvancedVisible = SessionState.GetBool("CSGModel.MeshAdvanced", true);
            var statisticsVisible   = SessionState.GetBool("CSGModel.Statistics", true);

            GUILayout.BeginVertical(GUI.skin.box);
            {
                EditorGUI.BeginChangeCheck();
                behaviourVisible = EditorGUILayout.Foldout(behaviourVisible, "Behaviour");
                if (EditorGUI.EndChangeCheck())
                    SessionState.SetBool("CSGModel.Behaviour", behaviourVisible);
                if (behaviourVisible)
                { 
                    EditorGUI.indentLevel++;
                    {
                        bool inverted_world = InvertedWorld.HasValue ? InvertedWorld.Value : false;
                        EditorGUI.BeginChangeCheck();
                        {
                            EditorGUI.showMixedValue = !InvertedWorld.HasValue;
                            inverted_world = EditorGUILayout.Toggle(InvertedWorldContent, inverted_world);
                        }
                        if (EditorGUI.EndChangeCheck())
                        {
                            for (int i = 0; i < models.Length; i++)
                            {
                                var model = models[i];
                                if (inverted_world)	model.Settings |=  ModelSettingsFlags.InvertedWorld;
                                else				model.Settings &= ~ModelSettingsFlags.InvertedWorld;
                            }

                            GUI.changed = true;
                            InvertedWorld = inverted_world;
                        }
                    }
                    EditorGUI.indentLevel--;
                }
            }
            GUILayout.EndVertical();
            if (behaviourVisible)
                GUILayout.Space(10);
            if (models != null && models.Length == 1)
            {
                GUILayout.BeginVertical(GUI.skin.box);
                {
                    EditorGUI.BeginChangeCheck();
                    exportVisible = EditorGUILayout.Foldout(exportVisible, ExportLabel);
                    if (EditorGUI.EndChangeCheck())
                        SessionState.SetBool("CSGModel.Export", exportVisible);
                    if (exportVisible)
                    {
                        {
                            GUILayout.BeginHorizontal();
                            {
                                GUILayout.Space(20);
                                EditorGUI.BeginChangeCheck();
                                {
                                    EditorGUI.showMixedValue = !OriginType.HasValue;
                                    OriginType = (OriginType)EditorGUILayout.EnumPopup(ExportOriginLabel, OriginType ?? Components.OriginType.ModelCenter, popupStyle);
                                    EditorGUI.showMixedValue = false;
                                }
                                if (EditorGUI.EndChangeCheck() && OriginType.HasValue)
                                {
                                    for (int i = 0; i < models.Length; i++)
                                    {
                                        models[i].originType = OriginType.Value;
                                    }
                                }
                            }
                            GUILayout.EndHorizontal();
                            GUILayout.BeginHorizontal();
                            {
                                GUILayout.Space(20);
                                var exportColliderToggle = ExportColliders ?? true;
                                EditorGUI.BeginChangeCheck();
                                {
                                    EditorGUI.showMixedValue = !OriginType.HasValue;
                                    exportColliderToggle = EditorGUILayout.Toggle(ExportColliderLabel, exportColliderToggle);
                                    EditorGUI.showMixedValue = false;
                                }
                                if (EditorGUI.EndChangeCheck() && OriginType.HasValue)
                                {
                                    for (int i = 0; i < models.Length; i++)
                                    {
                                        models[i].exportColliders = exportColliderToggle;
                                    }
                                    ExportColliders = exportColliderToggle;
                                }
                            }
                            GUILayout.EndHorizontal();
                            GUILayout.BeginHorizontal();
                            {
                                GUILayout.Space(20);
                                EditorGUI.BeginDisabledGroup(!ExportType.HasValue);
                                {
                                    if (EditModeCommonGUI.IndentableButton(ExportToButtonLabel) && ExportType.HasValue)
                                    {
                                        MeshInstanceManager.Export(models[0], ExportType.Value, ExportColliders ?? true);
                                    }
                                }
                                EditorGUI.EndDisabledGroup();
                                EditorGUI.BeginChangeCheck();
                                {
                                    EditorGUI.showMixedValue = !ExportType.HasValue;
                                    ExportType = (ExportType)EditorGUILayout.EnumPopup(ExportType ?? Components.ExportType.FBX, popupStyle);
                                    EditorGUI.showMixedValue = false;
                                }
                                if (EditorGUI.EndChangeCheck() && ExportType.HasValue)
                                {
                                    for (int i = 0; i < models.Length; i++)
                                    {
                                        models[i].exportType = ExportType.Value;
                                    }
                                }
                            }
                            GUILayout.EndHorizontal();
                        }
                    }
                }
                GUILayout.EndVertical();
                if (exportVisible)
                    GUILayout.Space(10);
            }
            GUILayout.BeginVertical(GUI.skin.box);
            {
                EditorGUI.BeginChangeCheck();
                physicsVisible = EditorGUILayout.Foldout(physicsVisible, "Physics");
                if (EditorGUI.EndChangeCheck())
                    SessionState.SetBool("CSGModel.Physics", physicsVisible);
                if (physicsVisible)
                {
                    EditorGUI.indentLevel++;
                    { 
                        bool collider_value = NoCollider.HasValue ? NoCollider.Value : false;
                        EditorGUI.BeginChangeCheck();
                        {
                            EditorGUI.showMixedValue = !NoCollider.HasValue;
                            collider_value = !EditorGUILayout.Toggle(GenerateColliderContent, !collider_value);
                        }
                        if (EditorGUI.EndChangeCheck())
                        {
                            for (int i = 0; i < models.Length; i++)
                            {
                                var model = models[i];
                                if (collider_value)	model.Settings |=  ModelSettingsFlags.NoCollider;
                                else				model.Settings &= ~ModelSettingsFlags.NoCollider;
                            }
                            GUI.changed = true;
                            NoCollider = collider_value;
                            updateMeshes = true;
                        }
                    }
                    var have_no_collider = NoCollider.HasValue && NoCollider.Value;
                    EditorGUI.BeginDisabledGroup(have_no_collider);
                    {
                        bool trigger_value_mixed = have_no_collider ? true : !IsTrigger.HasValue;
                        bool trigger_value = IsTrigger.HasValue ? IsTrigger.Value : false;
                        {
                            EditorGUI.BeginChangeCheck();
                            {
                                EditorGUI.showMixedValue = trigger_value_mixed;
                                trigger_value = EditorGUILayout.Toggle(ModelIsTriggerContent, trigger_value);
                            }
                            if (EditorGUI.EndChangeCheck())
                            {
                                for (int i = 0; i < models.Length; i++)
                                {
                                    var model = models[i];
                                    if (trigger_value)	model.Settings |= ModelSettingsFlags.IsTrigger;
                                    else				model.Settings &= ~ModelSettingsFlags.IsTrigger;
                                }
                                GUI.changed = true;
                                IsTrigger = trigger_value;
                                updateMeshes = true;
                            }
                        }
                        bool set_convex_value_mixed = have_no_collider ? true : !SetToConvex.HasValue;
                        bool set_convex_value = have_no_collider ? false : (SetToConvex.HasValue ? SetToConvex.Value : false);
                        { 
                            EditorGUI.BeginChangeCheck();
                            {
                                EditorGUI.showMixedValue = set_convex_value_mixed;
                                var prevColor = GUI.color;
                                if (!set_convex_value && trigger_value)
                                {
                                    var color = new Color(1, 0.25f, 0.25f);
                                    GUI.color = color;
                                }
                                set_convex_value = EditorGUILayout.Toggle(ColliderSetToConvexContent, set_convex_value);
                                GUI.color = prevColor;
                            }
                            if (EditorGUI.EndChangeCheck())
                            {
                                for (int i = 0; i < models.Length; i++)
                                {
                                    if (set_convex_value) models[i].Settings |=  ModelSettingsFlags.SetColliderConvex;
                                    else				  models[i].Settings &= ~ModelSettingsFlags.SetColliderConvex;
                                }
                                GUI.changed = true;
                                SetToConvex = set_convex_value;
                                updateMeshes = true;
                            }
                        }
                        {
                            EditorGUI.BeginChangeCheck();
                            {
                                EditorGUI.showMixedValue = defaultPhysicsMaterialMixed;
                                GUILayout.BeginHorizontal();
                                EditorGUILayout.PrefixLabel(DefaultPhysicsMaterialContent);
                                defaultPhysicsMaterial = EditorGUILayout.ObjectField(defaultPhysicsMaterial, typeof(PhysicMaterial), true) as PhysicMaterial;
                                GUILayout.EndHorizontal();
                            }
                            if (EditorGUI.EndChangeCheck())
                            {
                                if (!defaultPhysicsMaterial)
                                    defaultPhysicsMaterial = MaterialUtility.DefaultPhysicsMaterial;
                                for (int i = 0; i < models.Length; i++)
                                {
                                    models[i].DefaultPhysicsMaterial = defaultPhysicsMaterial;
                                }
                                GUI.changed = true;
                                //MeshInstanceManager.Clear();
                                updateMeshes = true;
                            }
                        }
                        if (!have_no_collider && !set_convex_value && trigger_value)
                        {
                            var prevColor = GUI.color;
                            var color = new Color(1, 0.25f, 0.25f);
                            GUI.color = color;
                            GUILayout.Label("Warning:\r\nFor performance reasons colliders need to\r\nbe convex!");
                    
                            GUI.color = prevColor;
                        }
                    }
                    EditorGUI.EndDisabledGroup();
                    {
                        bool autoRigidbody = (AutoGenerateRigidBody.HasValue ? AutoGenerateRigidBody.Value : false);
                        EditorGUI.BeginChangeCheck();
                        {
                            EditorGUI.showMixedValue = !AutoGenerateRigidBody.HasValue;
                            autoRigidbody = !EditorGUILayout.Toggle(ColliderAutoRigidBodyContent, !autoRigidbody);
                        }
                        if (EditorGUI.EndChangeCheck())
                        {
                            for (int i = 0; i < models.Length; i++)
                            {
                                if (autoRigidbody) models[i].Settings |= ModelSettingsFlags.AutoUpdateRigidBody;
                                else models[i].Settings &= ~ModelSettingsFlags.AutoUpdateRigidBody;
                            }
                            GUI.changed = true;
                            AutoGenerateRigidBody = autoRigidbody;
                        }
                    }
                    EditorGUI.indentLevel--;
                }
            }
            GUILayout.EndVertical();
            if (physicsVisible)
                GUILayout.Space(10);
            GUILayout.BeginVertical(GUI.skin.box);
            {
                EditorGUI.BeginChangeCheck();
                renderingVisible = EditorGUILayout.Foldout(renderingVisible, "Rendering");
                if (EditorGUI.EndChangeCheck())
                    SessionState.SetBool("CSGModel.Rendering", renderingVisible);
                if (renderingVisible)
                {
                    //ShadowCastingMode shadowcastingValue = ShadowCastingMode.HasValue ? ShadowCastingMode.Value : UnityEngine.Rendering.ShadowCastingMode.On;
                    //var castOnlyShadow = (shadowcastingValue == UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly);
                    EditorGUI.indentLevel++;
                    //EditorGUI.BeginDisabledGroup(castOnlyShadow);
                    {
                        bool donotrender_value = //castOnlyShadow ? true : 
                                                    (DoNotRender.HasValue ? DoNotRender.Value : false);
                        EditorGUI.BeginChangeCheck();
                        {
                            EditorGUI.showMixedValue = //castOnlyShadow ? true : 
                                                        !DoNotRender.HasValue;
                            donotrender_value = EditorGUILayout.Toggle(DoNotRenderContent, donotrender_value);
                        }
                        if (EditorGUI.EndChangeCheck())
                        {
                            for (int i = 0; i < models.Length; i++)
                            {
                                var model = models[i];
                                if (donotrender_value) model.Settings |= ModelSettingsFlags.DoNotRender;
                                else model.Settings &= ~ModelSettingsFlags.DoNotRender;
                            }
                            GUI.changed = true;
                            DoNotRender = donotrender_value;
                            updateMeshes = true;
                        }
                    }
                    //EditorGUI.EndDisabledGroup();
                    {
                        bool two_sided_shadows_value = //castOnlyShadow ? true : 
                                                        (TwoSidedShadows.HasValue ? TwoSidedShadows.Value : false);
                        EditorGUI.BeginChangeCheck();
                        {
                            EditorGUI.showMixedValue = //castOnlyShadow ? true : 
                                                        !TwoSidedShadows.HasValue;
                            two_sided_shadows_value = EditorGUILayout.Toggle(TwoSidedShadowsContent, two_sided_shadows_value);
                        }
                        if (EditorGUI.EndChangeCheck())
                        {
                            for (int i = 0; i < models.Length; i++)
                            {
                                var model = models[i];
                                if (two_sided_shadows_value) model.Settings |= ModelSettingsFlags.TwoSidedShadows;
                                else model.Settings &= ~ModelSettingsFlags.TwoSidedShadows;
                            }
                            GUI.changed = true;
                            TwoSidedShadows = two_sided_shadows_value;
                            updateMeshes = true;
                        }
                    }

                    GUILayout.Space(10);
                    /*
                    EditorGUI.BeginDisabledGroup(DoNotRender.HasValue && DoNotRender.Value);
                    {
                        EditorGUI.BeginChangeCheck();
                        {
                            EditorGUI.showMixedValue = !ShadowCastingMode.HasValue;						
                            shadowcastingValue = (ShadowCastingMode)EditorGUILayout.EnumPopup(CastShadows, shadowcastingValue);
                        }
                        if (EditorGUI.EndChangeCheck())
                        {
                            for (int i = 0; i < models.Length; i++)
                            {
                                settings = models[i].Settings;
                                settings &= ~ModelSettingsFlags.ShadowCastingModeFlags;
                                settings |= (ModelSettingsFlags)(((int)shadowcastingValue) & (int)ModelSettingsFlags.ShadowCastingModeFlags);
                                models[i].Settings = settings;
                            }
                            GUI.changed = true;
                            ShadowCastingMode = shadowcastingValue;
                            updateMeshes = true;
                        }

                        var isUsingDeferredRenderingPath = false;//IsUsingDeferredRenderingPath();
                        EditorGUI.BeginDisabledGroup(castOnlyShadow || isUsingDeferredRenderingPath);
                        {
                            var receiveshadowsValue = !castOnlyShadow && (isUsingDeferredRenderingPath || (ReceiveShadows ?? false));
                            EditorGUI.BeginChangeCheck();
                            {
                                EditorGUI.showMixedValue = (castOnlyShadow || !ReceiveShadows.HasValue) && !isUsingDeferredRenderingPath;
                                receiveshadowsValue = EditorGUILayout.Toggle(CSGModelComponentInspectorGUI.ReceiveShadowsContent, receiveshadowsValue || isUsingDeferredRenderingPath);
                            }
                            if (EditorGUI.EndChangeCheck())
                            {
                                for (int i = 0; i < models.Length; i++)
                                {
                                    if (receiveshadowsValue) models[i].Settings &= ~ModelSettingsFlags.DoNotReceiveShadows;
                                    else                     models[i].Settings |=  ModelSettingsFlags.DoNotReceiveShadows;	
                                }
                                GUI.changed = true;
                                ReceiveShadows = receiveshadowsValue;
                            }
                        }
                        EditorGUI.EndDisabledGroup();
                    }
                    EditorGUI.EndDisabledGroup();*/

                    //EditorGUI.BeginDisabledGroup(castOnlyShadow);
                    EditorGUI.showMixedValue = false;
                    UpdateTargets(models);
                    if (_probesInstance != null &&
                        _probesOnGUIMethod != null &&
                        _probesTargets != null &&
                        _probesSerializedObject != null &&
                        _probesInitialized)
                    {
                        GUILayout.Space(10);
                        try
                        {
#if UNITY_5_6_OR_NEWER
                            _probesSerializedObject.UpdateIfRequiredOrScript();
#else
                            _probesSerializedObject.UpdateIfDirtyOrScript();
#endif
                            _probesOnGUIMethod.Invoke(_probesInstance, new System.Object[] { _probesTargets, (Renderer)_probesTargets[0], false });
                            _probesSerializedObject.ApplyModifiedProperties();
                        }
                        catch { }
                    }
                    //EditorGUI.EndDisabledGroup();
                    EditorGUI.indentLevel--;
                }
            }
            GUILayout.EndVertical();
            if (renderingVisible)
                GUILayout.Space(10);
            GUILayout.BeginVertical(GUI.skin.box);
            {
                EditorGUI.BeginChangeCheck();
                UVSettingsVisible = EditorGUILayout.Foldout(UVSettingsVisible, "Lighting");
                if (EditorGUI.EndChangeCheck())
                    SessionState.SetBool("CSGModel.UVSettings", UVSettingsVisible);
                if (UVSettingsVisible)
                {
                    EditorGUI.indentLevel++;
#if UNITY_2019_2_OR_NEWER
                    {
                        EditorGUI.BeginChangeCheck();
                        {
                            EditorGUI.showMixedValue = !MeshReceiveGI.HasValue;
                            MeshReceiveGI = (ReceiveGI)EditorGUILayout.EnumPopup(ReceiveGIContent, MeshReceiveGI ?? ReceiveGI.LightProbes, popupStyle);
                        }
                        if (EditorGUI.EndChangeCheck() && MeshReceiveGI.HasValue)
                        {
                            for (int i = 0; i < models.Length; i++)
                            {
                                var model = models[i];
                                model.ReceiveGI = MeshReceiveGI.Value;
                            }
                            GUI.changed = true;
                            updateMeshes = true;
                        }
                    }
#endif
                    {
                        if (DoNotRender.HasValue && DoNotRender.Value)
                        {
                            var prevColor = GUI.color;
                            var color = new Color(1, 0.25f, 0.25f);
                            GUI.color = color;
                            EditorGUILayout.LabelField("Rendering is disabled");
                            GUI.color = prevColor;
                        } else
                        {	
                            bool enable = !GenerateLightMaps.HasValue || (GenerateLightMaps.HasValue && !GenerateLightMaps.Value);
                            var enableOrDisableButtonText = !GenerateLightMaps.HasValue ?
                                                                EnableLightmapsForAllContent : 
                                                                (!GenerateLightMaps.Value ? EnableLightmapsContent : DisableLightmapsContent);
                            if (EditModeCommonGUI.IndentableButton(enableOrDisableButtonText))
                            {
                                for (int i = 0; i < models.Length; i++)
                                {
                                    var	oldStaticFlags	= GameObjectUtility.GetStaticEditorFlags(models[i].gameObject);
                                    StaticEditorFlags newStaticFlags;
#if UNITY_2019_2_OR_NEWER
                                    if (enable)
                                        newStaticFlags = oldStaticFlags | StaticEditorFlags.ContributeGI;
                                    else
                                        newStaticFlags = oldStaticFlags & ~StaticEditorFlags.ContributeGI;
#else
                                    if (enable)
                                        newStaticFlags = oldStaticFlags | StaticEditorFlags.LightmapStatic;
                                    else
                                        newStaticFlags = oldStaticFlags & ~StaticEditorFlags.LightmapStatic;
#endif
                                    if (oldStaticFlags != newStaticFlags)
                                    {
                                        GameObjectUtility.SetStaticEditorFlags(models[i].gameObject, newStaticFlags);
                                        MeshInstanceManager.ClearUVs(models[i]);
                                    }
                                }
                            }
                                            
                            if (!GenerateLightMaps.HasValue || (GenerateLightMaps.HasValue && GenerateLightMaps.Value))
                            {
                                if (!DoNotRender.HasValue)
                                {
                                    var prevColor = GUI.color;
                                    var color = new Color(1, 0.25f, 0.25f);
                                    GUI.color = color;
                                    EditorGUILayout.LabelField("Not all models have their rendering enabled");
                                    GUI.color = prevColor;
                                }
                            
                                EditorGUILayout.LabelField("Unity UV Generation");
                                EditorGUI.indentLevel++;
                                {
                                    {
                                        var angleError = AngleError ?? 0;
                                        EditorGUI.BeginChangeCheck();
                                        {
                                            EditorGUI.showMixedValue = !AngleError.HasValue;
                                            angleError = EditorGUILayout.Slider(AngleErrorContent, angleError, CSGModel.MinAngleError, CSGModel.MaxAngleError);
                                        }
                                        if (EditorGUI.EndChangeCheck())
                                        {
                                            for (int i = 0; i < models.Length; i++)
                                            {
                                                models[i].angleError = angleError;
                                                MeshInstanceManager.Refresh(models[i], onlyFastRefreshes: false);
                                            }
                                            GUI.changed = true;
                                            updateMeshes = true;
                                        }
                                    }
                                    {
                                        var areaError = AreaError ?? 0;
                                        EditorGUI.BeginChangeCheck();
                                        {
                                            EditorGUI.showMixedValue = !AreaError.HasValue;
                                            areaError = EditorGUILayout.Slider(AreaErrorContent, areaError, CSGModel.MinAreaError, CSGModel.MaxAreaError);
                                        }
                                        if (EditorGUI.EndChangeCheck())
                                        {
                                            for (int i = 0; i < models.Length; i++)
                                            {
                                                models[i].areaError = areaError;
                                                MeshInstanceManager.Refresh(models[i], onlyFastRefreshes: false);
                                            }
                                            GUI.changed = true;
                                            updateMeshes = true;
                                        }
                                    }
                                    {
                                        var hardAngle = HardAngle ?? 0;
                                        EditorGUI.BeginChangeCheck();
                                        {
                                            EditorGUI.showMixedValue = !HardAngle.HasValue;
                                            hardAngle = EditorGUILayout.Slider(HardAngleContent, hardAngle, 0, 360);
                                        }
                                        if (EditorGUI.EndChangeCheck())
                                        {
                                            for (int i = 0; i < models.Length; i++)
                                            {
                                                models[i].hardAngle = hardAngle;
                                                MeshInstanceManager.Refresh(models[i], onlyFastRefreshes: false);
                                            }
                                            GUI.changed = true;
                                            updateMeshes = true;
                                        }
                                    }
                                    {
                                        var packMargin = PackMargin ?? 0;
                                        EditorGUI.BeginChangeCheck();
                                        {
                                            EditorGUI.showMixedValue = !PackMargin.HasValue;
                                            packMargin = EditorGUILayout.FloatField(PackMarginContent, (int)(packMargin * 8192.0f) / 8192.0f);
                                        }
                                        if (EditorGUI.EndChangeCheck())
                                        {
                                            for (int i = 0; i < models.Length; i++)
                                            {
                                                models[i].packMargin = packMargin;
                                                MeshInstanceManager.Refresh(models[i], onlyFastRefreshes: false);
                                            }
                                            GUI.changed = true;
                                            updateMeshes = true;
                                        }
                                    }
                                    UnityEditor.UnwrapParam uvGenerationSettings;
                                    UnityEditor.UnwrapParam.SetDefaults(out uvGenerationSettings);

                                    if (!AngleError.HasValue || AngleError.Value	!= uvGenerationSettings.angleError ||
                                        !AreaError .HasValue || AreaError .Value	!= uvGenerationSettings.areaError ||
                                        !HardAngle .HasValue || HardAngle .Value	!= uvGenerationSettings.hardAngle ||
                                        !PackMargin.HasValue || PackMargin.Value	!= uvGenerationSettings.packMargin)
                                    { 
                                        if (EditModeCommonGUI.IndentableButton(ResetContent))
                                        {
                                            for (int i = 0; i < models.Length; i++)
                                            {
                                                models[i].angleError	= uvGenerationSettings.angleError;
                                                models[i].areaError		= uvGenerationSettings.areaError;
                                                models[i].hardAngle		= uvGenerationSettings.hardAngle;
                                                models[i].packMargin	= uvGenerationSettings.packMargin;
                                                MeshInstanceManager.Refresh(models[i], onlyFastRefreshes: false);
                                            }
                                            GUI.changed = true;
                                            updateMeshes = true;
                                        }
                                    }
                                }
                                {
                                    var autoRebuildUvs = AutoRebuildUVs ?? false;
                                    EditorGUI.BeginChangeCheck();
                                    {
                                        EditorGUI.showMixedValue = !AutoRebuildUVs.HasValue;
                                        autoRebuildUvs = EditorGUILayout.Toggle(AutoRebuildUVsContent, autoRebuildUvs);
                                    }
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        for (int i = 0; i < models.Length; i++)
                                        {
                                            if (autoRebuildUvs)
                                                models[i].Settings |= ModelSettingsFlags.AutoRebuildUVs;
                                            else
                                                models[i].Settings &= ~ModelSettingsFlags.AutoRebuildUVs;
                                            MeshInstanceManager.Refresh(models[i], onlyFastRefreshes: false);
                                        }
                                        GUI.changed = true;
                                        AutoRebuildUVs = autoRebuildUvs;
                                    }
                                }
                                EditModeCommonGUI.UpdateButtons(models);
                                EditorGUI.indentLevel--;
                                GUILayout.Space(10);
                                EditorGUILayout.LabelField("Unity UV Charting Control");
                                EditorGUI.indentLevel++;
                                {
                                    {
                                        var preserveUVs = PreserveUVs ?? false;
                                        EditorGUI.BeginChangeCheck();
                                        {
                                            EditorGUI.showMixedValue = !PreserveUVs.HasValue;
                                            preserveUVs = EditorGUILayout.Toggle(PreserveUVsContent, preserveUVs);
                                        }
                                        if (EditorGUI.EndChangeCheck())
                                        {
                                            for (int i = 0; i < models.Length; i++)
                                            {
                                                if (preserveUVs)
                                                    models[i].Settings |= ModelSettingsFlags.PreserveUVs;
                                                else
                                                    models[i].Settings &= ~ModelSettingsFlags.PreserveUVs;
                                                MeshInstanceManager.Refresh(models[i], onlyFastRefreshes: false);
                                            }
                                            GUI.changed = true;
                                            PreserveUVs = preserveUVs;
                                            updateMeshes = true;
                                        }
                                    }
                                    EditorGUI.indentLevel++;

                                    bool disabledAutoUVs = (PreserveUVs ?? false);
                                    using (new EditorGUI.DisabledScope(disabledAutoUVs))
                                    {
                                        {
                                            var autoUVMaxDistance = AutoUVMaxDistance ?? 0.0f;
                                            EditorGUI.BeginChangeCheck();
                                            {
                                                EditorGUI.showMixedValue = !AutoUVMaxDistance.HasValue;
                                                autoUVMaxDistance = EditorGUILayout.FloatField(AutoUVMaxDistanceContent, autoUVMaxDistance);
                                                if (autoUVMaxDistance < 0.0f)
                                                    autoUVMaxDistance = 0.0f;
                                            }
                                            if (EditorGUI.EndChangeCheck())
                                            {
                                                for (int i = 0; i < models.Length; i++)
                                                {
                                                    models[i].autoUVMaxDistance = autoUVMaxDistance;
                                                    MeshInstanceManager.Refresh(models[i], onlyFastRefreshes: false);
                                                }
                                                GUI.changed = true;
                                                updateMeshes = true;
                                            }
                                        }
                                        {
                                            var autoUVMaxAngle = AutoUVMaxAngle ?? 0.0f;
                                            EditorGUI.BeginChangeCheck();
                                            {
                                                EditorGUI.showMixedValue = !AutoUVMaxAngle.HasValue;
                                                autoUVMaxAngle = EditorGUILayout.Slider(AutoUVMaxAngleContent, autoUVMaxAngle, 0, 180);
                                            }
                                            if (EditorGUI.EndChangeCheck())
                                            {
                                                for (int i = 0; i < models.Length; i++)
                                                {
                                                    models[i].autoUVMaxAngle = autoUVMaxAngle;
                                                    MeshInstanceManager.Refresh(models[i], onlyFastRefreshes: false);
                                                }
                                                GUI.changed = true;
                                                updateMeshes = true;
                                            }
                                        }

                                    }
                                    EditorGUI.indentLevel--;
                                    {
                                        var ignoreNormals = IgnoreNormals ?? false;
                                        EditorGUI.BeginChangeCheck();
                                        {
                                            EditorGUI.showMixedValue = !IgnoreNormals.HasValue;
                                            ignoreNormals = EditorGUILayout.Toggle(IgnoreNormalsContent, ignoreNormals);
                                        }
                                        if (EditorGUI.EndChangeCheck())
                                        {
                                            for (int i = 0; i < models.Length; i++)
                                            {
                                                if (ignoreNormals)
                                                    models[i].Settings |= ModelSettingsFlags.IgnoreNormals;
                                                else
                                                    models[i].Settings &= ~ModelSettingsFlags.IgnoreNormals;
                                                MeshInstanceManager.Refresh(models[i], onlyFastRefreshes: false);
                                            }
                                            GUI.changed = true;
                                            updateMeshes = true;
                                        }
                                    }
                                    {
                                        var minimumChartSize = MinimumChartSize ?? 4;
                                        EditorGUI.BeginChangeCheck();
                                        {
                                            EditorGUI.showMixedValue = !MinimumChartSize.HasValue;
                                            minimumChartSize = EditorGUILayout.IntPopup(MinimumChartSizeContent, minimumChartSize, MinimumChartSizeStrings, MinimumChartSizeValues);
                                        }
                                        if (EditorGUI.EndChangeCheck())
                                        {
                                            for (int i = 0; i < models.Length; i++)
                                            {
                                                models[i].minimumChartSize = minimumChartSize;
                                                MeshInstanceManager.Refresh(models[i], onlyFastRefreshes: false);
                                            }
                                            GUI.changed = true;
                                            updateMeshes = true;
                                        }
                                    }
                                }
                                EditorGUI.indentLevel--;
                                GUILayout.Space(10);
                                EditorGUILayout.LabelField("Lightmap Settings");
                                EditorGUI.indentLevel++;
                                { 
                                    {
                                        var scaleInLightmap = ScaleInLightmap ?? 1.0f;
                                        EditorGUI.BeginChangeCheck();
                                        {
                                            EditorGUI.showMixedValue = !ScaleInLightmap.HasValue;
                                            scaleInLightmap = EditorGUILayout.FloatField(ScaleInLightmapContent, scaleInLightmap);
                                        }
                                        if (EditorGUI.EndChangeCheck())
                                        {
                                            for (int i = 0; i < models.Length; i++)
                                            {
                                                models[i].scaleInLightmap = scaleInLightmap;
                                                MeshInstanceManager.Refresh(models[i], onlyFastRefreshes: false);
                                            }
                                            GUI.changed = true;
                                            ScaleInLightmap = scaleInLightmap;
                                            updateMeshes = true;
                                        }
                                    }

#if UNITY_2017_2_OR_NEWER
                                    {
                                        var stitchLightmapSeams = StitchLightmapSeams ?? false;
                                        EditorGUI.BeginChangeCheck();
                                        {
                                            EditorGUI.showMixedValue = !StitchLightmapSeams.HasValue;
                                            stitchLightmapSeams = EditorGUILayout.Toggle(StitchLightmapSeamsContent, stitchLightmapSeams);
                                        }
                                        if (EditorGUI.EndChangeCheck())
                                        {
                                            for (int i = 0; i < models.Length; i++)
                                            {
                                                if (stitchLightmapSeams)
                                                    models[i].Settings |= ModelSettingsFlags.StitchLightmapSeams;
                                                else
                                                    models[i].Settings &= ~ModelSettingsFlags.StitchLightmapSeams;
                                                MeshInstanceManager.Refresh(models[i], onlyFastRefreshes: false);
                                            }
                                            GUI.changed = true;
                                            StitchLightmapSeams = stitchLightmapSeams;
                                            updateMeshes = true;
                                        }
                                    }
#endif
                                
                                }
                                EditorGUI.indentLevel--;
                                EditorGUI.indentLevel--;

                                EditorGUI.indentLevel++;
                            }
                        }
                    }
                    EditorGUI.indentLevel--;
                }
            }
            GUILayout.EndVertical();
            if (UVSettingsVisible)
                GUILayout.Space(10);
            GUILayout.BeginVertical(GUI.skin.box);
            {
                EditorGUI.BeginChangeCheck();
                meshAdvancedVisible = EditorGUILayout.Foldout(meshAdvancedVisible, "Mesh (advanced)");
                if (EditorGUI.EndChangeCheck())
                    SessionState.SetBool("CSGModel.MeshAdvanced", meshAdvancedVisible);
                if (meshAdvancedVisible)
                {
                    EditorGUI.indentLevel++;
                    {
                        var showGeneratedMeshes = ShowGeneratedMeshes ?? false;
                        EditorGUI.BeginChangeCheck();
                        {
                            EditorGUI.showMixedValue = !ShowGeneratedMeshes.HasValue;
                            showGeneratedMeshes = EditorGUILayout.Toggle(ShowGeneratedMeshesContent, showGeneratedMeshes);
                        }
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObjects(models, "Setting ShowGeneratedMeshes to " + showGeneratedMeshes);
                            for (int i = 0; i < models.Length; i++)
                            {
                                models[i].ShowGeneratedMeshes = showGeneratedMeshes;
                                MeshInstanceManager.UpdateGeneratedMeshesVisibility(models[i]);
                            }
                            // Workaround for unity not refreshing the hierarchy when changing hideflags
                            EditorApplication.RepaintHierarchyWindow();
                            EditorApplication.DirtyHierarchyWindowSorting();
                        }

#if UNITY_2017_3_OR_NEWER
                        GUILayout.Space(10);
                    
                        EditorGUI.BeginDisabledGroup(NoCollider??true);
                        EditorGUILayout.LabelField(MeshColliderCookingContent);
                        EditorGUI.indentLevel++;
                        { 
                            var cookForFasterSimulation	    = CookForFasterSimulation   ?? false;
                            var enableMeshCleaning	        = EnableMeshCleaning        ?? false;
                            var weldColocatedVertices		= WeldColocatedVertices     ?? false;
                            EditorGUI.BeginChangeCheck();
                            {

                                EditorGUI.showMixedValue = !VertexChannelTangent.HasValue;
                                cookForFasterSimulation  = EditorGUILayout.Toggle(CookForFasterSimulationContent, cookForFasterSimulation);
                        
                                EditorGUI.showMixedValue = !VertexChannelNormal.HasValue;
                                enableMeshCleaning       = EditorGUILayout.Toggle(EnableMeshCleaningContent, enableMeshCleaning);
                        
                                EditorGUI.showMixedValue = !VertexChannelUV0.HasValue;
                                weldColocatedVertices    = EditorGUILayout.Toggle(WeldColocatedVerticesContent, weldColocatedVertices);
                            }
                            if (EditorGUI.EndChangeCheck())
                            {
                                for (int i = 0; i < models.Length; i++)
                                {
                                    var meshColliderCookingOptions = models[i].MeshColliderCookingOptions;
                                    meshColliderCookingOptions &= ~(MeshColliderCookingOptions.CookForFasterSimulation |
                                                                    MeshColliderCookingOptions.EnableMeshCleaning |
                                                                    MeshColliderCookingOptions.WeldColocatedVertices);
                            
                                    if (cookForFasterSimulation)	meshColliderCookingOptions |= MeshColliderCookingOptions.CookForFasterSimulation;
                                    if (enableMeshCleaning)	        meshColliderCookingOptions |= MeshColliderCookingOptions.EnableMeshCleaning;
                                    if (weldColocatedVertices)		meshColliderCookingOptions |= MeshColliderCookingOptions.WeldColocatedVertices;
                                    models[i].MeshColliderCookingOptions = meshColliderCookingOptions;
                                }
                                GUI.changed = true;
                            }
                        }
                        EditorGUI.EndDisabledGroup();
                        EditorGUI.indentLevel--;
#endif
                        GUILayout.Space(10);
                    
                        EditorGUILayout.LabelField("Used Vertex Channels");
                        EditorGUI.indentLevel++;
                        {
                            if (DoNotRender.HasValue && DoNotRender.Value)
                            {
                                var prevColor = GUI.color;
                                var color = new Color(1, 0.25f, 0.25f);
                                GUI.color = color;
                                EditorGUILayout.LabelField("Rendering is disabled");
                                GUI.color = prevColor;
                            } else
                            {
                                if (!DoNotRender.HasValue)
                                {
                                    var prevColor = GUI.color;
                                    var color = new Color(1, 0.25f, 0.25f);
                                    GUI.color = color;
                                    EditorGUILayout.LabelField("Not all models have their rendering enabled");
                                    GUI.color = prevColor;
                                }
        //						var vertex_channel_color	= VertexChannelColor ?? false;
                                var vertex_channel_tangent	= VertexChannelTangent ?? false;
                                var vertex_channel_normal	= VertexChannelNormal ?? false;
                                var vertex_channel_UV0		= VertexChannelUV0 ?? false;
                                EditorGUI.BeginChangeCheck();
                                {
        //							EditorGUI.showMixedValue = !VertexChannelColor.HasValue;
        //							vertex_channel_color = EditorGUILayout.Toggle(VertexChannelColorContent, vertex_channel_color);
                        
                                    EditorGUI.showMixedValue = !VertexChannelTangent.HasValue;
                                    vertex_channel_tangent = EditorGUILayout.Toggle(VertexChannelTangentContent, vertex_channel_tangent);
                        
                                    EditorGUI.showMixedValue = !VertexChannelNormal.HasValue;
                                    vertex_channel_normal = EditorGUILayout.Toggle(VertexChannelNormalContent, vertex_channel_normal);
                        
                                    EditorGUI.showMixedValue = !VertexChannelUV0.HasValue;
                                    vertex_channel_UV0 = EditorGUILayout.Toggle(VertexChannelUV1Content, vertex_channel_UV0);
                                }
                                if (EditorGUI.EndChangeCheck())
                                {
                                    for (int i = 0; i < models.Length; i++)
                                    {
                                        var vertexChannel = models[i].VertexChannels;
                                        vertexChannel &= ~(//VertexChannelFlags.Color |
                                                           VertexChannelFlags.Tangent |
                                                           VertexChannelFlags.Normal |
                                                           VertexChannelFlags.UV0);

                                        //if (vertex_channel_color)	vertexChannel |= VertexChannelFlags.Color;
                                        if (vertex_channel_tangent)	vertexChannel |= VertexChannelFlags.Tangent;
                                        if (vertex_channel_normal)	vertexChannel |= VertexChannelFlags.Normal;
                                        if (vertex_channel_UV0)		vertexChannel |= VertexChannelFlags.UV0;
                                        models[i].VertexChannels = vertexChannel;
                                    }
                                    GUI.changed = true;
                                }
                            }
                        }
                        EditorGUI.indentLevel--;
                    }
                    EditorGUI.indentLevel--;
                }
                GUILayout.EndVertical();
                if (models != null && models.Length == 1)
                {
                    if (meshAdvancedVisible)
                        GUILayout.Space(10);

                    GUILayout.BeginVertical(GUI.skin.box);
                    EditorGUI.BeginChangeCheck();
                    statisticsVisible = EditorGUILayout.Foldout(statisticsVisible, "Statistics");
                    if (EditorGUI.EndChangeCheck())
                        SessionState.SetBool("CSGModel.Statistics", statisticsVisible);
                    if (statisticsVisible)
                    {
                        if (models[0].generatedMeshes == null || 
                            !models[0].generatedMeshes)
                        {
                            GUILayout.Label("Could not find model cache for this model.");
                        } else
                        {
                            var meshContainer = models[0].generatedMeshes;


                            var totalTriangles = 0;
                            var totalVertices = 0;
                            var totalMeshes = 0;
                        

                            var materialMeshes = new Dictionary<Material, List<MeshData>>();
                            foreach (var instance in meshContainer.MeshInstances)
                            {
                                var mesh				= instance.SharedMesh;
                                if (!mesh || !MeshInstanceManager.HasVisibleMeshRenderer(instance))
                                    continue;

                                if (!instance.RenderMaterial)
                                {
                                    var meshDescription = instance.MeshDescription;
                                    if (meshDescription.surfaceParameter > 0)
                                    {
                                        instance.RenderMaterial		= null;
                                        instance.PhysicsMaterial	= null;
                                        var obj = EditorUtility.InstanceIDToObject(meshDescription.surfaceParameter);
                                        if (obj)
                                        { 
                                            switch (meshDescription.meshQuery.LayerParameterIndex)
                                            {
                                                case LayerParameterIndex.LayerParameter1: { instance.RenderMaterial	 = obj as Material;       break; }
                                                case LayerParameterIndex.LayerParameter2: { instance.PhysicsMaterial = obj as PhysicMaterial; break; }
                                            }
                                        }
                                    }
                                    if (!instance.RenderMaterial)
                                    {
                                        if (!dummyMaterial)
                                            dummyMaterial = new Material(MaterialUtility.FloorMaterial);
                                    
                                        instance.RenderMaterial = dummyMaterial;
                                    }
                                }

                                List<MeshData> meshes;
                                if (!materialMeshes.TryGetValue(instance.RenderMaterial, out meshes))
                                {
                                    meshes = new List<MeshData>();
                                    materialMeshes[instance.RenderMaterial] = meshes;
                                }

                                var meshData = new MeshData();
                                meshData.Mesh				= mesh;
                                meshData.VertexCount		= mesh.vertexCount;
                                meshData.TriangleCount		= mesh.triangles.Length / 3;
                                meshData.GeometryHashValue	= instance.MeshDescription.geometryHashValue;
                                meshData.SurfaceHashValue	= instance.MeshDescription.surfaceHashValue;
                                meshes.Add(meshData);
                            
                                totalVertices += meshData.VertexCount;
                                totalTriangles += meshData.TriangleCount;
                                totalMeshes++;
                            }
                            EditorGUI.indentLevel++;
                            EditorGUILayout.Space();
                            EditorGUILayout.LabelField("total:");
                            EditorGUILayout.LabelField("vertices: " + totalVertices + "  triangles: " + totalTriangles + "  materials: " + materialMeshes.Count + "  meshes: " + totalMeshes);
                            GUILayout.Space(10);
                            EditorGUILayout.LabelField("meshes:");
                            foreach(var item in materialMeshes)
                            {
                                var material = item.Key;
                                var meshes = item.Value;

                                if (material == dummyMaterial)
                                    material = null;

                                GUILayout.BeginHorizontal();
                                {
                                    EditorGUI.BeginDisabledGroup(true);
                                    {
                                        EditorGUILayout.ObjectField(material, typeof(Material), true);
                                    }								
                                    GUILayout.BeginVertical();
                                    {
                                        for (int i = 0; i < meshes.Count; i++)
                                        {
                                            EditorGUILayout.ObjectField(meshes[i].Mesh, typeof(Mesh), true);
                                            EditorGUILayout.LabelField("geometryHash " + meshes[i].GeometryHashValue.ToString("X"));
                                            EditorGUILayout.LabelField("surfaceHash " + meshes[i].SurfaceHashValue.ToString("X"));
                                            EditorGUILayout.LabelField("vertices " + meshes[i].VertexCount + "  triangles " + meshes[i].TriangleCount);
                                        }
                                    }
                                    GUILayout.EndVertical();
                                    EditorGUI.EndDisabledGroup();
                                }
                                GUILayout.EndHorizontal();
                                EditorGUILayout.Space();
                            }
                            EditorGUI.indentLevel--;
                        }
                    }
                    GUILayout.EndVertical();
                }
            }
            EditorGUI.showMixedValue = false;
            if (updateMeshes)
            {
                InternalCSGModelManager.DoForcedMeshUpdate();
                InternalCSGModelManager.UpdateMeshes();
                UpdateLoop.ResetUpdateRoutine();

                for (int i = 0; i < models.Length; i++)
                {
                    MeshInstanceManager.ClearUVs(models[i]);
                    (CSGTreeNode.Encapsulate(models[i].modelNodeID)).SetDirty();
                }
            }
        }
    }
}
 