using UnityEngine;
using System.Collections;
using System.Text;
using System.Collections.Generic;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Linq;

namespace UnityFBXExporter
{
#if UNITY_EDITOR
	internal class FBXModelPostProcessor : AssetPostprocessor
	{
        public override uint GetVersion() { return 1; }


        public static bool modify           = false;
		public static bool hasMaterials     = false;
		public static bool copyMaterials    = false;
		public static bool hasNormals       = false;
		public static bool hasTangents      = false;

		public void OnPreprocessModel()
		{
			if (!modify)
				return;
			var modelImporter = assetImporter as ModelImporter;
			modelImporter.materialName = ModelImporterMaterialName.BasedOnMaterialName;
			if (hasNormals)
				modelImporter.importNormals = ModelImporterNormals.Import;
			else
				modelImporter.importNormals = ModelImporterNormals.None;

			if (hasTangents)
				modelImporter.importTangents = ModelImporterTangents.Import;
			else
				modelImporter.importTangents = ModelImporterTangents.None;

			modelImporter.generateSecondaryUV = true;
			modelImporter.addCollider = false;
#if UNITY_2019_3_OR_NEWER
            modelImporter.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
#else
            modelImporter.importMaterials = hasMaterials;
#endif
			if (hasMaterials && !copyMaterials)
				modelImporter.materialSearch = ModelImporterMaterialSearch.Everywhere;
		}
	}
#endif
 }
