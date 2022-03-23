// ===============================================================================================
//	The MIT License (MIT) for UnityFBXExporter
//
//  UnityFBXExporter was created for Building Crafter (http://u3d.as/ovC) a tool to rapidly 
//	create high quality buildings right in Unity with no need to use 3D modeling programs.
//
//  Copyright (c) 2016 | 8Bit Goose Games, Inc.
//		
//	Permission is hereby granted, free of charge, to any person obtaining a copy 
//	of this software and associated documentation files (the "Software"), to deal 
//	in the Software without restriction, including without limitation the rights 
//	to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies 
//	of the Software, and to permit persons to whom the Software is furnished to do so, 
//	subject to the following conditions:
//		
//	The above copyright notice and this permission notice shall be included in all 
//	copies or substantial portions of the Software.
//		
//	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
//	INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
//	PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
//	HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
//	OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE 
//	OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
// ===============================================================================================

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
	public sealed partial class FBXExporter
	{
		public static bool ExportGameObjToFBX(GameObject gameObj, string filename, bool exportColliders = false, bool copyMaterials = false, bool copyTextures = false)
		{
			// Check to see if the extension is right
			if (filename.Remove(0, filename.LastIndexOf('.')).ToLower() != ".fbx")
			{
				Debug.LogError("The end of the path wasn't \".fbx\"");
				return false;
			}

			bool hasNormals		= false;
			bool hasTangents	= false;
			bool hasMaterials	= false;

			var colliderMaterial = RealtimeCSG.MaterialUtility.ColliderMaterial;

			string buildMesh = MeshToString(gameObj, filename, colliderMaterial, exportColliders, out hasNormals, out hasTangents, out hasMaterials, copyMaterials, copyTextures);

			if (hasMaterials && copyMaterials)
				CopyComplexMaterialsToPath(gameObj, filename, copyTextures);

			var path = System.IO.Path.GetDirectoryName(filename);
			if (System.IO.File.Exists(filename)) System.IO.File.Delete(filename);
			else if (!System.IO.Directory.Exists(path)) System.IO.Directory.CreateDirectory(path);

			
			System.IO.File.WriteAllText(filename, buildMesh);

#if UNITY_EDITOR
			FBXModelPostProcessor.modify = true;
			try
			{
				FBXModelPostProcessor.copyMaterials = copyMaterials;
				FBXModelPostProcessor.hasNormals	= hasNormals;
				FBXModelPostProcessor.hasTangents	= hasTangents;
				FBXModelPostProcessor.hasMaterials	= hasMaterials;
				var stringLocalPath = filename.Remove(0, filename.LastIndexOf("/Assets") + 1);
				AssetDatabase.ImportAsset(stringLocalPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate | ImportAssetOptions.ImportRecursive);
			}
			finally
			{
				FBXModelPostProcessor.modify = false;
			}
#endif
			return true;
		}

		public static string VersionInformation
		{
			get { return "FBX Unity Export version 1.1.1 (Originally created for the Unity Asset, Building Crafter)"; }
		}

		public static long GetRandomFBXId()
		{
			return System.BitConverter.ToInt64(System.Guid.NewGuid().ToByteArray(), 0);
		}
		public static void CopyComplexMaterialsToPath(GameObject gameObj, string path, bool copyTextures, string texturesFolder = "/Textures", string materialsFolder = "/Materials")
		{
#if UNITY_EDITOR
			int folderIndex = path.LastIndexOf('/');
			path = path.Remove(folderIndex, path.Length - folderIndex);

			// 1. First create the directories that are needed
			string texturesPath = path + texturesFolder;
			string materialsPath = path + materialsFolder;
			
			if(Directory.Exists(path) == false)
				Directory.CreateDirectory(path);
			if(Directory.Exists(materialsPath) == false)
				Directory.CreateDirectory(materialsPath);
				

			// 2. Copy every distinct Material into the Materials folder
			Renderer[] renderers = gameObj.GetComponentsInChildren<Renderer>();
			List<Material> everyMaterial = new List<Material>();

			for(int i = 0; i < renderers.Length; i++)
			{
				for(int n = 0; n < renderers[i].sharedMaterials.Length; n++)
				{
					everyMaterial.Add(renderers[i].sharedMaterials[n]);
				}
			}

			Material[] everyDistinctMaterial = everyMaterial.Distinct().ToArray<Material>();
			everyDistinctMaterial = everyDistinctMaterial.OrderBy(o => o.name).ToArray<Material>();

			// Log warning if there are multiple assets with the same name
			for(int i = 0; i < everyDistinctMaterial.Length; i++)
			{
				for(int n = 0; n < everyDistinctMaterial.Length; n++)
				{
					if(i == n)
						continue;

					if(everyDistinctMaterial[i].name == everyDistinctMaterial[n].name)
					{
						Debug.LogErrorFormat("Two distinct materials {0} and {1} have the same name, this will not work with the FBX Exporter", everyDistinctMaterial[i], everyDistinctMaterial[n]);
						return;
					}
				}
			}

			List<string> everyMaterialName = new List<string>();
			// Structure of materials naming, is used when packaging up the package
			// PARENTNAME_ORIGINALMATNAME.mat
			for(int i = 0; i < everyDistinctMaterial.Length; i++)
			{
				string newName = gameObj.name + "_" + everyDistinctMaterial[i].name;
				string fullPath = materialsPath + "/" + newName + ".mat";

				if(File.Exists(fullPath))
					File.Delete(fullPath);

				if(CopyAndRenameAsset(everyDistinctMaterial[i], newName, materialsPath))
					everyMaterialName.Add(newName);
			}

			// 3. Go through newly moved materials and copy every texture and update the material
			AssetDatabase.Refresh();

			List<Material> allNewMaterials = new List<Material>();

			for (int i = 0; i < everyMaterialName.Count; i++) 
			{
				string assetPath = materialsPath;
				if(assetPath[assetPath.Length - 1] != '/')
					assetPath += "/";

				assetPath += everyMaterialName[i] + ".mat";

				Material sourceMat = (Material)AssetDatabase.LoadAssetAtPath(assetPath, typeof(Material));

				if(sourceMat != null)
					allNewMaterials.Add(sourceMat);
			}

			// Get all the textures from the mesh renderer

			if(copyTextures)
			{
				if(Directory.Exists(texturesPath) == false)
					Directory.CreateDirectory(texturesPath);

				AssetDatabase.Refresh();

				for(int i = 0; i < allNewMaterials.Count; i++)
				{
					allNewMaterials[i] = CopyTexturesAndAssignCopiesToMaterial(allNewMaterials[i], texturesPath);
				}
			}

			AssetDatabase.Refresh();
#endif
		}

		public static bool CopyAndRenameAsset(Object obj, string newName, string newFolderPath)
		{
#if UNITY_EDITOR
			string path = newFolderPath;
			
			if(path[path.Length - 1] != '/')
				path += "/";
//			string testPath = path.Remove(path.Length - 1);

//			if(AssetDatabase.IsValidFolder(testPath) == false)
//			{
//				Debug.LogError("This folder does not exist " + testPath);
//				return false;
//			}

			string assetPath =  AssetDatabase.GetAssetPath(obj);
			string fileName = GetFileName(assetPath);
 			if (string.IsNullOrEmpty(fileName)) 
 				return false;

			string extension = fileName.Remove(0, fileName.LastIndexOf('.'));

			string newFileName = path + newName + extension;

			if(System.IO.File.Exists(newFileName))
				return false;

			return AssetDatabase.CopyAsset(assetPath, newFileName);
#else
			return false;

#endif
		}

		/// <summary>
		/// Strips the full path of a file
		/// </summary>
		/// <returns>The file name.</returns>
		/// <param name="path">Path.</param>
		private static string GetFileName(string path)
		{
			string fileName = path.ToString();
			fileName = fileName.Remove(0, fileName.LastIndexOf('/') + 1);

			return fileName;
		}

		private static Material CopyTexturesAndAssignCopiesToMaterial(Material material, string newPath)
		{
			if(material.shader.name == "Standard" || material.shader.name == "Standard (Specular setup)")
			{
				GetTextureUpdateMaterialWithPath(material, "_MainTex", newPath);

				if(material.shader.name == "Standard")
					GetTextureUpdateMaterialWithPath(material, "_MetallicGlossMap", newPath);

				if(material.shader.name == "Standard (Specular setup)")
					GetTextureUpdateMaterialWithPath(material, "_SpecGlossMap", newPath);

				GetTextureUpdateMaterialWithPath(material, "_BumpMap", newPath);
				GetTextureUpdateMaterialWithPath(material, "_BumpMap", newPath);
				GetTextureUpdateMaterialWithPath(material, "_BumpMap", newPath);
				GetTextureUpdateMaterialWithPath(material, "_BumpMap", newPath);
				GetTextureUpdateMaterialWithPath(material, "_ParallaxMap", newPath);
				GetTextureUpdateMaterialWithPath(material, "_OcclusionMap", newPath);
				GetTextureUpdateMaterialWithPath(material, "_EmissionMap", newPath);
				GetTextureUpdateMaterialWithPath(material, "_DetailMask", newPath);
				GetTextureUpdateMaterialWithPath(material, "_DetailAlbedoMap", newPath);
				GetTextureUpdateMaterialWithPath(material, "_DetailNormalMap", newPath);

			}
			else
				Debug.LogError("WARNING: " + material.name + " is not a physically based shader, may not export to package correctly");

			return material;
		}

		/// <summary>
		/// Copies and renames the texture and assigns it to the material provided.
		/// NAME FORMAT: Material.name + textureShaderName
		/// </summary>
		/// <param name="material">Material.</param>
		/// <param name="textureShaderName">Texture shader name.</param>
		/// <param name="newPath">New path.</param>
		private static void GetTextureUpdateMaterialWithPath(Material material, string textureShaderName, string newPath)
		{
			Texture textureInQ = material.GetTexture(textureShaderName);
			if(textureInQ != null)
			{
				string name = material.name + textureShaderName;
				
				Texture newTexture = (Texture)CopyAndRenameAssetReturnObject(textureInQ, name, newPath);
				if(newTexture != null)
					material.SetTexture(textureShaderName, newTexture);
			}
		}

		public static Object CopyAndRenameAssetReturnObject(Object obj, string newName, string newFolderPath)
		{
			#if UNITY_EDITOR
			string path = newFolderPath;
			
			if(path[path.Length - 1] != '/')
				path += "/";
			string testPath = path.Remove(path.Length - 1);
			
			if(System.IO.Directory.Exists(testPath) == false)
			{
				Debug.LogError("This folder does not exist " + testPath);
				return null;
			}
			
			string assetPath	= AssetDatabase.GetAssetPath(obj);
			string fileName		= GetFileName(assetPath);
			if (string.IsNullOrEmpty(fileName)) 
 				return null;

			string extension = fileName.Remove(0, fileName.LastIndexOf('.'));
			
			string newFullPathName = path + newName + extension;
			
			if(AssetDatabase.CopyAsset(assetPath, newFullPathName) == false)
				return null;
			
			AssetDatabase.Refresh();
			
			return AssetDatabase.LoadAssetAtPath(newFullPathName, typeof(Texture));
			#else
			return null;
			#endif
		}
	}
}