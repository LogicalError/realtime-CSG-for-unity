using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using InternalRealtimeCSG;

namespace RealtimeCSG
{
    public static class MaterialUtility
	{
		internal const string ShaderNameRoot						= "Hidden/CSG/internal/";
		internal const string SpecialSurfaceShaderName				= "specialSurface";
		internal const string SpecialSurfaceShaderID				= ShaderNameRoot + SpecialSurfaceShaderName;
		internal const string TransparentSpecialSurfaceShaderName	= "transparentSpecialSurface";
		internal const string TransparentSpecialSurfaceShaderID		= ShaderNameRoot + TransparentSpecialSurfaceShaderName;

		internal const string HiddenName				= "hidden";
		internal const string CulledName				= "culled";
		internal const string ColliderName				= "collider";
		internal const string TriggerName				= "trigger";
		internal const string ShadowOnlyName            = "shadowOnly";
		internal const string CastShadowsName			= "castShadows";
		internal const string ReceiveShadowsName        = "receiveShadows";

		internal const string HiddenMaterialName			= TransparentSpecialSurfaceShaderName + "_" + HiddenName;
		internal const string CulledMaterialName			= TransparentSpecialSurfaceShaderName + "_" + CulledName;
		internal const string ColliderMaterialName			= SpecialSurfaceShaderName + "_" + ColliderName;
		internal const string TriggerMaterialName			= TransparentSpecialSurfaceShaderName + "_" + TriggerName;
		internal const string ShadowOnlyMaterialName		= SpecialSurfaceShaderName + "_" + ShadowOnlyName;
		internal const string CastShadowsMaterialName		= SpecialSurfaceShaderName + "_" + CastShadowsName;
		internal const string ReceiveShadowsMaterialName	= SpecialSurfaceShaderName + "_" + ReceiveShadowsName;
		
		
		const string WallMaterialName	= "Wall";
		const string FloorMaterialName	= "Floor";
		const string WindowMaterialName	= "Window";
		const string MetalMaterialName	= "Metal";
		
		internal static Shader SpecialSurfaceShader;
		internal static Shader TransparentSpecialSurfaceShader;

		
		private static readonly Dictionary<string, Material> EditorMaterials = new Dictionary<string, Material>();

		private static bool _shadersInitialized;		//= false;
		private static int	_pixelsPerPointId			= -1;
		private static int	_lineThicknessMultiplierId	= -1; 
		private static int	_lineDashMultiplierId		= -1; 
		private static int	_lineAlphaMultiplierId		= -1;

		private static void ShaderInit()
		{
			_shadersInitialized = true;
	
			_pixelsPerPointId			= Shader.PropertyToID("_pixelsPerPoint");
			_lineThicknessMultiplierId	= Shader.PropertyToID("_thicknessMultiplier");
			_lineDashMultiplierId		= Shader.PropertyToID("_dashMultiplier");
			_lineAlphaMultiplierId		= Shader.PropertyToID("_alphaMultiplier");
			
			SpecialSurfaceShader			= Shader.Find(SpecialSurfaceShaderID);;
			TransparentSpecialSurfaceShader	= Shader.Find(TransparentSpecialSurfaceShaderID);
		}

		internal static Material GenerateEditorMaterial(string shaderName, string textureName = null, string materialName = null)
		{
			Material material;
			var name = shaderName + ":" + textureName;
			if (EditorMaterials.TryGetValue(name, out material))
			{
				// just in case one of many unity bugs destroyed the material
				if (!material)
				{
					EditorMaterials.Remove(name);
				} else
					return material;
			}

			if (materialName == null)
				materialName = name.Replace(':', '_');


			var shader = Shader.Find(ShaderNameRoot + shaderName);
			if (!shader)
			{
				Debug.LogWarning("Could not find internal shader: " + ShaderNameRoot + shaderName);
				return null;
			}

			material = new Material(shader)
			{
				name = materialName,
				hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontUnloadUnusedAsset
			};
			if (textureName != null)
			{
				string filename = "Assets/Plugins/RealtimeCSG/Editor/Resources/Textures/" + textureName + ".png";
				material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(filename);
				if (!material.mainTexture)
					Debug.LogWarning("Could not find internal texture: " + filename);
			}
			EditorMaterials.Add(name, material);
			return material;
		}

		internal static Material GenerateEditorColorMaterial(Color color)
		{
			var name = "Color:" + color;

			Material material;
			if (EditorMaterials.TryGetValue(name, out material))
			{
				// just in case one of many unity bugs destroyed the material
				if (!material)
				{
					EditorMaterials.Remove(name);
				} else
					return material;
			}

			var shader = Shader.Find("Unlit/Color");
			if (!shader)
				return null;

			material = new Material(shader)
			{
				name		= name.Replace(':', '_'),
				hideFlags	= HideFlags.None | HideFlags.DontUnloadUnusedAsset
			};
			material.SetColor("_Color", color);

			EditorMaterials.Add(name, material);
			return material;
		}


		internal static bool EqualInternalMaterial(Material o, Material n)
		{
			return	((bool)o == (bool)n) &&
					o.shader == n.shader && 
					o.mainTexture == n.mainTexture && 
					//o.Equals(n);
					o.name == n.name;
		}

		
		const string DefaultMaterialPath = "Assets/Plugins/RealtimeCSG/Runtime/Materials/";
		const string DefaultTexturePath = "Assets/Plugins/RealtimeCSG/Runtime/Textures/";
		
		internal static void CreateRenderPipelineVersionOfDefaultMaterial(Material defaultMaterial, string materialName)
		{
			if (!defaultMaterial)
				return;
			
			var materialPath		= string.Format("{0}{1}/", DefaultMaterialPath, defaultMaterial.shader.name);
			var materialFilename	= string.Format("{0}{1}.mat", materialPath, materialName);

			var material = AssetDatabase.LoadAssetAtPath<Material>(materialFilename);
			if (material)
				return;

			material = new Material(defaultMaterial);

			try
			{
				if (!System.IO.Directory.Exists(materialPath))
					System.IO.Directory.CreateDirectory(materialPath);
				// HDRenderPipeline will generate errors when creating it's own type of materials
				AssetDatabase.CreateAsset(material, materialFilename);
				material = AssetDatabase.LoadAssetAtPath<Material>(materialFilename);
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
			}

			try
			{
				string destTexture = null;
				if (material.HasProperty("_Diffuse")) destTexture = "_Diffuse";
				else if (material.HasProperty("_Albedo")) destTexture = "_Albedo";
				else if (material.HasProperty("_BaseColorMap")) destTexture = "_BaseColorMap";
				else if	(material.HasProperty("_MainTex")) destTexture = "_MainTex";
				if (destTexture != null)
				{
					var texturePath		= string.Format("{0}{1}.png", DefaultTexturePath, materialName);
					var defaultTexture	= AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
					if (defaultTexture)
					{
						material.SetTexture(destTexture, defaultTexture);
						material.mainTexture = defaultTexture;
					} else
					{
						var regularMaterialPath = string.Format("{0}{1}.mat", DefaultMaterialPath, materialName);
						var regularMaterial		= AssetDatabase.LoadAssetAtPath<Material>(regularMaterialPath);
						if (regularMaterial)
						{
							material.SetTexture(destTexture, regularMaterial.mainTexture);
							material.mainTexture = regularMaterial.mainTexture;
						} else
							Debug.LogWarning("couldn't find source texture for " + materialName);
					}
				}
			}
			catch(Exception ex)
			{
				Debug.LogException(ex);
			}
		}


		internal static void CreateRenderPipelineVersionOfDefaultMaterials(Material defaultMaterial)
		{
			if (!defaultMaterial)
				return;

			CreateRenderPipelineVersionOfDefaultMaterial(defaultMaterial, WallMaterialName);
			CreateRenderPipelineVersionOfDefaultMaterial(defaultMaterial, FloorMaterialName);
			CreateRenderPipelineVersionOfDefaultMaterial(defaultMaterial, WindowMaterialName);
			CreateRenderPipelineVersionOfDefaultMaterial(defaultMaterial, MetalMaterialName);
			
			CSGSettings.Reload();
			var currentMaterial = CSGSettings.DefaultMaterial;
			if (!currentMaterial)
				return;  
			
			var currentMaterialPath = AssetDatabase.GetAssetPath(currentMaterial);
			if (!currentMaterialPath.StartsWith(DefaultMaterialPath))
				return;
			
			var materialPath = string.Format("{0}{1}/", DefaultMaterialPath, defaultMaterial.shader.name);
			if (currentMaterialPath.StartsWith(materialPath))
				return;
			
			var newMaterialPath = currentMaterialPath.Replace(DefaultMaterialPath, materialPath);
			currentMaterial =  AssetDatabase.LoadAssetAtPath<Material>(newMaterialPath);
			if (currentMaterial)
				CSGSettings.DefaultMaterial = currentMaterial;
			CSGSettings.Save();
		}

		internal static Material GetRuntimeMaterial(string materialName)
		{
			Material defaultMaterial = null;
			var renderPipelineAsset = UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset;
			if (renderPipelineAsset)
				defaultMaterial = DefaultMaterial;

			if (!defaultMaterial)
			{
				var defaultFilename	= string.Format("{0}{1}.mat", DefaultMaterialPath, materialName);
				return AssetDatabase.LoadAssetAtPath<Material>(defaultFilename);
			}
			
			var materialPath		= string.Format("{0}{1}/", DefaultMaterialPath, defaultMaterial.shader.name);
			var materialFilename	= string.Format("{0}{1}.mat", materialPath, materialName);

			var material = AssetDatabase.LoadAssetAtPath<Material>(materialFilename);
			if (material)
				return material;
			
			CreateRenderPipelineVersionOfDefaultMaterials(defaultMaterial);
			
			return AssetDatabase.LoadAssetAtPath<Material>(materialFilename);
		}


        internal static PhysicMaterial GetRuntimePhysicMaterial(string materialName)
        {
            var defaultFilename = string.Format("{0}{1}.physicMaterial", DefaultMaterialPath, materialName);
            return AssetDatabase.LoadAssetAtPath<PhysicMaterial>(defaultFilename);
        }


        private static Material _defaultMaterial;
		public static Material DefaultMaterial
		{
			get
			{
				if (!_defaultMaterial)
				{
					var renderPipelineAsset = UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset;
					if (renderPipelineAsset)
					{
#if UNITY_2019_1_OR_NEWER
						_defaultMaterial = renderPipelineAsset.defaultMaterial;
						if (!_defaultMaterial)
#elif UNITY_2017_2_OR_NEWER
						_defaultMaterial = renderPipelineAsset.GetDefaultMaterial();
						if (!_defaultMaterial)
#endif
							_defaultMaterial = GetColorMaterial(Color.magenta);
					} else
						_defaultMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Diffuse.mat");
				}
				return _defaultMaterial;
			}
		}

        private static PhysicMaterial _defaultPhysicsMaterial;
        public static PhysicMaterial DefaultPhysicsMaterial
        {
            get
            {
                if (!_defaultPhysicsMaterial)
                {
                    _defaultPhysicsMaterial = GetRuntimePhysicMaterial("Default");
                    if (!_defaultPhysicsMaterial)
                        Debug.LogError("Default physics material is missing");
                }
                return _defaultPhysicsMaterial;
            }
        }
         
        private static readonly Dictionary<Color,Material> ColorMaterials = new Dictionary<Color, Material>();
		internal static Material GetColorMaterial(Color color)
		{
			Material material;
			if (ColorMaterials.TryGetValue(color, out material))
			{
				// just in case one of many unity bugs destroyed the material
				if (!material)
				{
					ColorMaterials.Remove(color);
				} else
					return material;
			}
			
			material = GenerateEditorColorMaterial(color);
			if (!material)
				return AssetDatabase.GetBuiltinExtraResource<Material>("Default-Diffuse.mat");

			ColorMaterials.Add(color, material);
			return material;
		}

		private static Material _missingMaterial;
		public static Material MissingMaterial
		{
			get
			{
				if (!_missingMaterial)
				{
					_missingMaterial = GetColorMaterial(Color.magenta);
					if (!_missingMaterial)
						return DefaultMaterial;
				}
				return _missingMaterial;
			}
		}

		private static Material _wallMaterial;
		public static Material WallMaterial
		{
			get
			{
				if (!_wallMaterial)
				{
					_wallMaterial = GetRuntimeMaterial(WallMaterialName);
					if (!_wallMaterial)
						return DefaultMaterial;
				}
				return _wallMaterial;
			}
		}

		private static Material _floorMaterial;
		public static Material FloorMaterial
		{
			get
			{
				if (!_floorMaterial)
				{
					_floorMaterial = GetRuntimeMaterial(FloorMaterialName);
					if (!_floorMaterial)
						return DefaultMaterial;
				}
				return _floorMaterial;
			}
		}

		private static Material _windowMaterial;
		public static Material WindowMaterial
		{
			get
			{
				if (!_windowMaterial)
				{
					_windowMaterial = GetRuntimeMaterial(WindowMaterialName);
					if (!_windowMaterial)
						return DefaultMaterial;
				}
				return _windowMaterial;
			}
		}

		private static Material _metalMaterial;
		public static Material MetalMaterial
		{
			get
			{
				if (!_metalMaterial)
				{
					_metalMaterial = GetRuntimeMaterial(MetalMaterialName);
					if (!_metalMaterial)
						return DefaultMaterial;
				}
				return _metalMaterial;
			}
		}

		private static float _lineThicknessMultiplier = 1.0f;
		internal static float LineThicknessMultiplier
		{
			get { return _lineThicknessMultiplier; }
			set
			{
				if (Math.Abs(_lineThicknessMultiplier - value) < MathConstants.EqualityEpsilon)
					return;
				_lineThicknessMultiplier = value;
			}
		}

		private static float _lineDashMultiplier = 1.0f;
		internal static float LineDashMultiplier
		{
			get { return _lineDashMultiplier; }
			set
			{
				if (Math.Abs(_lineDashMultiplier - value) < MathConstants.EqualityEpsilon)
					return;
				_lineDashMultiplier = value;
			}
		}

		private static float _lineAlphaMultiplier = 1.0f;
		internal static float LineAlphaMultiplier
		{
			get { return _lineAlphaMultiplier; }
			set
			{
				if (Math.Abs(_lineAlphaMultiplier - value) < MathConstants.EqualityEpsilon)
					return;
				_lineAlphaMultiplier = value;
			}
		}


		private static Material _zTestGenericLine;
		internal static Material ZTestGenericLine
		{
			get
			{
				if (!_zTestGenericLine)
					_zTestGenericLine = GenerateEditorMaterial("ZTestGenericLine");
				return _zTestGenericLine;
			}
		}

		internal static void InitGenericLineMaterial(Material genericLineMaterial)
		{
			if (!genericLineMaterial)
				return;
			
			if (!_shadersInitialized) ShaderInit();
			if (_pixelsPerPointId != -1)
			{
				genericLineMaterial.SetFloat(_pixelsPerPointId, EditorGUIUtility.pixelsPerPoint);
			}
			if (_lineThicknessMultiplierId != -1) genericLineMaterial.SetFloat(_lineThicknessMultiplierId, _lineThicknessMultiplier * EditorGUIUtility.pixelsPerPoint);
			if (_lineDashMultiplierId      != -1) genericLineMaterial.SetFloat(_lineDashMultiplierId,      _lineDashMultiplier);
			if (_lineAlphaMultiplierId	   != -1) genericLineMaterial.SetFloat(_lineAlphaMultiplierId,     _lineAlphaMultiplier);
		}

		private static Material _noZTestGenericLine;
		internal static Material NoZTestGenericLine
		{
			get
			{
				if (!_noZTestGenericLine)
				{
					_noZTestGenericLine = GenerateEditorMaterial("NoZTestGenericLine");
				}
				return _noZTestGenericLine;
			}
		}

		private static Material _coloredPolygonMaterial;
		internal static Material ColoredPolygonMaterial
		{
			get
			{
				if (!_coloredPolygonMaterial)
				{
					_coloredPolygonMaterial = GenerateEditorMaterial("customSurface");
				}
				return _coloredPolygonMaterial;
			}
		}

		private static Material _hiddenMaterial;
		internal static Material HiddenMaterial		{ get { if (!_hiddenMaterial) _hiddenMaterial = GenerateEditorMaterial(TransparentSpecialSurfaceShaderName, HiddenName, HiddenMaterialName); return _hiddenMaterial; } }

		private static Material _culledMaterial;
		internal static Material CulledMaterial		{ get { if (!_culledMaterial) _culledMaterial = GenerateEditorMaterial(TransparentSpecialSurfaceShaderName, CulledName, CulledMaterialName); return _culledMaterial; } }

		private static Material _colliderMaterial;
		internal static Material ColliderMaterial	{ get { if (!_colliderMaterial) _colliderMaterial = GenerateEditorMaterial(SpecialSurfaceShaderName, ColliderName, ColliderMaterialName); return _colliderMaterial; } }
		 
		private static Material _triggerMaterial;
		internal static Material TriggerMaterial	{ get { if (!_triggerMaterial) _triggerMaterial = GenerateEditorMaterial(TransparentSpecialSurfaceShaderName, TriggerName, TriggerMaterialName); return _triggerMaterial; } }

		private static Material _shadowOnlyMaterial;
		internal static Material ShadowOnlyMaterial { get { if (!_shadowOnlyMaterial) _shadowOnlyMaterial = GenerateEditorMaterial(SpecialSurfaceShaderName, ShadowOnlyName, ShadowOnlyMaterialName); return _shadowOnlyMaterial; } }

		private static Material _castShadowsMaterial;
		internal static Material CastShadowsMaterial { get { if (!_castShadowsMaterial) _castShadowsMaterial = GenerateEditorMaterial(SpecialSurfaceShaderName, CastShadowsName, CastShadowsMaterialName); return _castShadowsMaterial; } }

		private static Material _receiveShadowsMaterial;
		internal static Material ReceiveShadowsMaterial { get { if (!_receiveShadowsMaterial) _receiveShadowsMaterial = GenerateEditorMaterial(SpecialSurfaceShaderName, ReceiveShadowsName, ReceiveShadowsMaterialName); return _receiveShadowsMaterial; } }


		internal static Texture2D CreateSolidColorTexture(int width, int height, Color color)
		{
			var pixels = new Color[width * height];
			for (var i = 0; i < pixels.Length; i++)
				pixels[i] = color;
			var newTexture = new Texture2D(width, height);
            newTexture.hideFlags = HideFlags.DontUnloadUnusedAsset;
			newTexture.SetPixels(pixels);
			newTexture.Apply();
			return newTexture;
		}

		static Dictionary<Material, RenderSurfaceType> materialTypeLookup = new Dictionary<Material, RenderSurfaceType>();

		internal static void ResetMaterialTypeLookup()
		{
			materialTypeLookup.Clear();
		}

		internal static RenderSurfaceType GetMaterialSurfaceType(Material material)
		{
			if (object.ReferenceEquals(material, null))
				return RenderSurfaceType.Normal;

			RenderSurfaceType surfaceType;
			if (materialTypeLookup.TryGetValue(material, out surfaceType))
				return surfaceType;
			
			if (!material)
				return RenderSurfaceType.Normal;
			
			if (!_shadersInitialized) ShaderInit();

			var shader = material.shader;
			if (shader != SpecialSurfaceShader &&
				shader != TransparentSpecialSurfaceShader)
			{
				materialTypeLookup[material] = RenderSurfaceType.Normal;
				return RenderSurfaceType.Normal;
			}

			var shaderName = shader.name;
			if (shaderName != SpecialSurfaceShaderID &&
				shaderName != TransparentSpecialSurfaceShaderID)
			{
				materialTypeLookup[material] = RenderSurfaceType.Normal;
				return RenderSurfaceType.Normal;
			}

			switch (material.name)
			{
				case HiddenMaterialName:			{ surfaceType = RenderSurfaceType.Hidden;         materialTypeLookup[material] = surfaceType; return surfaceType; }
				case CulledMaterialName:			{ surfaceType = RenderSurfaceType.Culled;         materialTypeLookup[material] = surfaceType; return surfaceType; }
				case ColliderMaterialName:			{ surfaceType = RenderSurfaceType.Collider;       materialTypeLookup[material] = surfaceType; return surfaceType; }
				case TriggerMaterialName:			{ surfaceType = RenderSurfaceType.Trigger;        materialTypeLookup[material] = surfaceType; return surfaceType; }
				case ShadowOnlyMaterialName:		{ surfaceType = RenderSurfaceType.ShadowOnly;     materialTypeLookup[material] = surfaceType; return surfaceType; }
				case CastShadowsMaterialName:		{ surfaceType = RenderSurfaceType.CastShadows;    materialTypeLookup[material] = surfaceType; return surfaceType; }
				case ReceiveShadowsMaterialName:	{ surfaceType = RenderSurfaceType.ReceiveShadows; materialTypeLookup[material] = surfaceType; return surfaceType; }
			}
			
			materialTypeLookup[material] = RenderSurfaceType.Normal;
			return RenderSurfaceType.Normal;
		}

		internal static Material GetSurfaceMaterial(RenderSurfaceType renderSurfaceType)
		{
			switch (renderSurfaceType)
			{
				case RenderSurfaceType.Hidden:			return HiddenMaterial; 
				case RenderSurfaceType.Culled:			return CulledMaterial; 
				case RenderSurfaceType.Collider:		return ColliderMaterial; 
				case RenderSurfaceType.Trigger:			return TriggerMaterial; 
				case RenderSurfaceType.ShadowOnly:		return ShadowOnlyMaterial;
				case RenderSurfaceType.CastShadows:		return CastShadowsMaterial;
				case RenderSurfaceType.ReceiveShadows:	return ReceiveShadowsMaterial;
				case RenderSurfaceType.Normal:			return null;
				default:								return null;
			}
		}
	
	}
}