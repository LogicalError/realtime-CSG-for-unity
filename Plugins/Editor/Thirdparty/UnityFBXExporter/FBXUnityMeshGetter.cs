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
using System.Globalization;

namespace UnityFBXExporter
{
	public sealed class FBXUnityMeshGetter
	{

		/// <summary>
		/// Gets all the meshes and outputs to a string (even grabbing the child of each gameObject)
		/// </summary>
		/// <returns>The mesh to string.</returns>
		/// <param name="gameObj">GameObject Parent.</param>
		/// <param name="materials">Every Material in the parent that can be accessed.</param>
		/// <param name="objects">The StringBuidler to create objects for the FBX file.</param>
		/// <param name="connections">The StringBuidler to create connections for the FBX file.</param>
		/// <param name="parentObject">Parent object, if left null this is the top parent.</param>
		/// <param name="parentModelId">Parent model id, 0 if top parent.</param>
		public static long GetMeshToString(GameObject gameObj, 
										   Material colliderMaterial,
										   bool exportColliders,
		                                   ref StringBuilder objects, 
		                                   ref StringBuilder connections,
										   out bool hasNormals,
										   out bool hasTangents,
										   GameObject parentObject = null, 
		                                   long parentModelId = 0)
		{
			hasNormals = false;
			hasTangents = false;


			StringBuilder tempObjectSb = new StringBuilder();
			StringBuilder tempConnectionsSb = new StringBuilder();

			long geometryId = FBXExporter.GetRandomFBXId();
			long modelId = FBXExporter.GetRandomFBXId();

			// Sees if there is a mesh to export and add to the system
			Mesh mesh = null;
			var filter = gameObj.GetComponent<MeshFilter>();
			if (!filter)
			{
				var skinnedMeshRenderer = gameObj.GetComponent<SkinnedMeshRenderer>();
				if (!skinnedMeshRenderer)
				{
					if (exportColliders)
					{
						var meshCollider = gameObj.GetComponent<MeshCollider>();
						if (meshCollider)
							mesh = meshCollider.sharedMesh;
					} 
				} else
					mesh = skinnedMeshRenderer.sharedMesh;
			} else
				mesh = filter.sharedMesh;
			
			string meshName = gameObj.name;

			bool needObject = gameObj.transform.childCount > 0 || mesh;

			if (!needObject)
				return 0;

			// A NULL parent means that the gameObject is at the top
			string isMesh = "Null";

			if (mesh)
			{
				meshName = mesh.name;
				isMesh = "Mesh";
			}

			if (parentModelId == 0)
				tempConnectionsSb.AppendLine("\t;Model::" + meshName + ", Model::RootNode");
			else
				tempConnectionsSb.AppendLine("\t;Model::" + meshName + ", Model::USING PARENT");
			tempConnectionsSb.AppendLine("\tC: \"OO\"," + modelId + "," + parentModelId);
			tempConnectionsSb.AppendLine();
			tempObjectSb.AppendLine("\tModel: " + modelId + ", \"Model::" + gameObj.name + "\", \"" + isMesh + "\" {");
			tempObjectSb.AppendLine("\t\tVersion: 232");
			tempObjectSb.AppendLine("\t\tProperties70:  {");
			tempObjectSb.AppendLine("\t\t\tP: \"RotationOrder\", \"enum\", \"\", \"\",4");
			tempObjectSb.AppendLine("\t\t\tP: \"RotationActive\", \"bool\", \"\", \"\",1");
			tempObjectSb.AppendLine("\t\t\tP: \"InheritType\", \"enum\", \"\", \"\",1");
			tempObjectSb.AppendLine("\t\t\tP: \"ScalingMax\", \"Vector3D\", \"Vector\", \"\",0,0,0");
			tempObjectSb.AppendLine("\t\t\tP: \"DefaultAttributeIndex\", \"int\", \"Integer\", \"\",0");
			// ===== Local Translation Offset =========
			Vector3 position = gameObj.transform.localPosition;

			tempObjectSb.Append("\t\t\tP: \"Lcl Translation\", \"Lcl Translation\", \"\", \"A+\",");

			// Append the X Y Z coords to the system
			tempObjectSb.AppendFormat(CultureInfo.InvariantCulture,"{0},{1},{2}", position.x * - 1, position.y, position.z);
			tempObjectSb.AppendLine();

			// Rotates the object correctly from Unity space
			Vector3 localRotation = gameObj.transform.localEulerAngles;
			tempObjectSb.AppendFormat(CultureInfo.InvariantCulture, "\t\t\tP: \"Lcl Rotation\", \"Lcl Rotation\", \"\", \"A+\",{0},{1},{2}", localRotation.x, localRotation.y * -1, -1 * localRotation.z);
			tempObjectSb.AppendLine();

			// Adds the local scale of this object
		    Vector3 localScale = gameObj.transform.localScale;
		    tempObjectSb.AppendFormat(CultureInfo.InvariantCulture,"\t\t\tP: \"Lcl Scaling\", \"Lcl Scaling\", \"\", \"A\",{0},{1},{2}", localScale.x, localScale.y, localScale.z);
			tempObjectSb.AppendLine();

			tempObjectSb.AppendLine("\t\t\tP: \"currentUVSet\", \"KString\", \"\", \"U\", \"map1\"");
			tempObjectSb.AppendLine("\t\t}");
			tempObjectSb.AppendLine("\t\tShading: T");
			tempObjectSb.AppendLine("\t\tCulling: \"CullingOff\"");
			tempObjectSb.AppendLine("\t}");


			// Adds in geometry if it exists, if it it does not exist, this is a empty gameObject file and skips over this
			if (mesh)
			{
				// =================================
				//         General Geometry Info
				// =================================
				// Generate the geometry information for the mesh created

				tempObjectSb.AppendLine("\tGeometry: " + geometryId + ", \"Geometry::\", \"Mesh\" {");
				
				// ===== WRITE THE VERTICIES =====
				Vector3[] vertices = mesh.vertices;
				int vertCount = mesh.vertexCount * 3; // <= because the list of points is just a list of comma separated values, we need to multiply by three

				tempObjectSb.AppendLine("\t\tVertices: *" + vertCount + " {");
				tempObjectSb.Append("\t\t\ta: ");
				for(int i = 0; i < vertices.Length; i++)
				{
					if(i > 0)
						tempObjectSb.Append(",");

					// Points in the vertices. We also reverse the x value because Unity has a reverse X coordinate
					tempObjectSb.AppendFormat(CultureInfo.InvariantCulture,"{0},{1},{2}", vertices[i].x * - 1, vertices[i].y, vertices[i].z);
				}

				tempObjectSb.AppendLine();
				tempObjectSb.AppendLine("\t\t} ");
				
				// ======= WRITE THE TRIANGLES ========
				int triangleCount = mesh.triangles.Length;
				int[] triangles = mesh.triangles;

				tempObjectSb.AppendLine("\t\tPolygonVertexIndex: *" + triangleCount + " {");

				// Write triangle indexes
				tempObjectSb.Append("\t\t\ta: ");
				for(int i = 0; i < triangleCount; i += 3)
				{
					if(i > 0)
						tempObjectSb.Append(",");

					// To get the correct normals, must rewind the triangles since we flipped the x direction
					tempObjectSb.AppendFormat(CultureInfo.InvariantCulture,"{0},{1},{2}", 
					                          triangles[i],
					                          triangles[i + 2], 
					                          (triangles[i + 1] * -1) - 1); // <= Tells the poly is ended

				}

				tempObjectSb.AppendLine();

				tempObjectSb.AppendLine("\t\t} ");
				tempObjectSb.AppendLine("\t\tGeometryVersion: 124");

				tempObjectSb.AppendLine("\t\tLayerElementNormal: 0 {");
				tempObjectSb.AppendLine("\t\t\tVersion: 101");
				tempObjectSb.AppendLine("\t\t\tName: \"\"");
				tempObjectSb.AppendLine("\t\t\tMappingInformationType: \"ByPolygonVertex\"");
				tempObjectSb.AppendLine("\t\t\tReferenceInformationType: \"Direct\"");
				
				// ===== WRITE THE NORMALS ==========
				Vector3[] normals = mesh.normals;
				if (normals != null && normals.Length != 0)
				{
					hasNormals = true;

					tempObjectSb.AppendLine("\t\t\tNormals: *" + (triangleCount * 3) + " {");
					tempObjectSb.Append("\t\t\t\ta: ");

					for (int i = 0; i < triangleCount; i += 3)
					{
						if (i > 0)
							tempObjectSb.Append(",");

						// To get the correct normals, must rewind the normal triangles like the triangles above since x was flipped
						Vector3 newNormal = normals[triangles[i]];

						tempObjectSb.AppendFormat(CultureInfo.InvariantCulture,"{0},{1},{2},",
												 newNormal.x * -1, // Switch normal as is tradition
												 newNormal.y,
												 newNormal.z);

						newNormal = normals[triangles[i + 2]];

						tempObjectSb.AppendFormat(CultureInfo.InvariantCulture,"{0},{1},{2},",
												  newNormal.x * -1, // Switch normal as is tradition
												  newNormal.y,
												  newNormal.z);

						newNormal = normals[triangles[i + 1]];

						tempObjectSb.AppendFormat(CultureInfo.InvariantCulture,"{0},{1},{2}",
												  newNormal.x * -1, // Switch normal as is tradition
												  newNormal.y,
												  newNormal.z);
					}

					tempObjectSb.AppendLine();
					tempObjectSb.AppendLine("\t\t\t}");
				}
				tempObjectSb.AppendLine("\t\t}");
				
				
				Vector4[] tangents = mesh.tangents;
				if (tangents != null && tangents.Length == normals.Length && normals.Length != 0)
				{
					hasTangents = true;

					tempObjectSb.AppendLine();
					tempObjectSb.AppendLine("\t\tLayerElementTangent: 0 {");
					tempObjectSb.AppendLine("\t\t\tVersion: 101");
					tempObjectSb.AppendLine("\t\t\tName: \"\"");
					tempObjectSb.AppendLine("\t\t\tMappingInformationType: \"ByPolygonVertex\"");
					tempObjectSb.AppendLine("\t\t\tReferenceInformationType: \"Direct\"");

					// ===== WRITE THE TANGENTS ==========
					tempObjectSb.AppendLine("\t\t\tTangents: *" + (triangleCount * 3) + " {");
					tempObjectSb.Append("\t\t\t\ta: ");

					for (int i = 0; i < triangleCount; i += 3)
					{
						if (i > 0)
							tempObjectSb.Append(",");

						// To get the correct tangents, must rewind the tangent triangles like the triangles above since x was flipped
						Vector3 newTangent = tangents[triangles[i]];

						tempObjectSb.AppendFormat(CultureInfo.InvariantCulture,"{0},{1},{2},",
												 newTangent.x * -1, // Switch tangent as is tradition
												 newTangent.y,
												 newTangent.z);

						newTangent = tangents[triangles[i + 2]];

						tempObjectSb.AppendFormat(CultureInfo.InvariantCulture,"{0},{1},{2},",
												  newTangent.x * -1, // Switch tangent as is tradition
												  newTangent.y,
												  newTangent.z);

						newTangent = tangents[triangles[i + 1]];

						tempObjectSb.AppendFormat(CultureInfo.InvariantCulture,"{0},{1},{2}",
												  newTangent.x * -1, // Switch tangent as is tradition
												  newTangent.y,
												  newTangent.z);
					}

					tempObjectSb.AppendLine();
					tempObjectSb.AppendLine("\t\t\t}");
					tempObjectSb.AppendLine("\t\t}");
					
				
					tempObjectSb.AppendLine();
					tempObjectSb.AppendLine("\t\tLayerElementBinormal: 0 {");
					tempObjectSb.AppendLine("\t\t\tVersion: 101");
					tempObjectSb.AppendLine("\t\t\tName: \"\"");
					tempObjectSb.AppendLine("\t\t\tMappingInformationType: \"ByPolygonVertex\"");
					tempObjectSb.AppendLine("\t\t\tReferenceInformationType: \"Direct\"");

					// ===== WRITE THE BINORMALS ==========

					tempObjectSb.AppendLine("\t\t\tBinormals: *" + (triangleCount * 3) + " {");
					tempObjectSb.Append("\t\t\t\ta: ");

					for (int i = 0; i < triangleCount; i += 3)
					{
						if (i > 0)
							tempObjectSb.Append(",");

						// To get the correct binormals, must rewind the binormal triangles like the triangles above since x was flipped
						Vector4 packed_tangent	= tangents[triangles[i]];
						Vector3 normal			= normals[triangles[i]];
						Vector3 newBinormal		= Vector3.Cross(normal, (Vector3)packed_tangent) * packed_tangent.w;

						tempObjectSb.AppendFormat(CultureInfo.InvariantCulture,"{0},{1},{2},",
												 newBinormal.x * -1, // Switch binormal as is tradition
												 newBinormal.y,
												 newBinormal.z);

						packed_tangent	= tangents[triangles[i + 2]];
						normal			= normals[triangles[i + 2]];
						newBinormal		= Vector3.Cross(normal, (Vector3)packed_tangent) * packed_tangent.w;
					
						tempObjectSb.AppendFormat(CultureInfo.InvariantCulture,"{0},{1},{2},",
												  newBinormal.x * -1, // Switch binormal as is tradition
												  newBinormal.y,
												  newBinormal.z);

						packed_tangent	= tangents[triangles[i + 1]];
						normal			= normals[triangles[i + 1]];
						newBinormal		= Vector3.Cross(normal, (Vector3)packed_tangent) * packed_tangent.w;

						tempObjectSb.AppendFormat(CultureInfo.InvariantCulture,"{0},{1},{2}",
												  newBinormal.x * -1, // Switch binormal as is tradition
												  newBinormal.y,
												  newBinormal.z);
					}

					tempObjectSb.AppendLine();
					tempObjectSb.AppendLine("\t\t\t}");
					tempObjectSb.AppendLine("\t\t}");
				}

				// ================ UV CREATION =========================

				// -- UV 1 Creation
				Vector2[] uvs = mesh.uv;
				if (uvs != null && uvs.Length > 0)
				{
					int uvLength = uvs.Length;

					tempObjectSb.AppendLine("\t\tLayerElementUV: 0 {"); // the Zero here is for the first UV map
					tempObjectSb.AppendLine("\t\t\tVersion: 101");
					tempObjectSb.AppendLine("\t\t\tName: \"map1\"");
					tempObjectSb.AppendLine("\t\t\tMappingInformationType: \"ByPolygonVertex\"");
					tempObjectSb.AppendLine("\t\t\tReferenceInformationType: \"IndexToDirect\"");
					tempObjectSb.AppendLine("\t\t\tUV: *" + uvLength * 2 + " {");
					tempObjectSb.Append("\t\t\t\ta: ");

					for (int i = 0; i < uvLength; i++)
					{
						if (i > 0)
							tempObjectSb.Append(",");

						tempObjectSb.AppendFormat(CultureInfo.InvariantCulture,"{0},{1}", uvs[i].x, uvs[i].y);

					}
					tempObjectSb.AppendLine();

					tempObjectSb.AppendLine("\t\t\t\t}");

					// UV tile index coords
					tempObjectSb.AppendLine("\t\t\tUVIndex: *" + triangleCount + " {");
					tempObjectSb.Append("\t\t\t\ta: ");

					for (int i = 0; i < triangleCount; i += 3)
					{
						if (i > 0)
							tempObjectSb.Append(",");

						// Triangles need to be fliped for the x flip
						int index1 = triangles[i];
						int index2 = triangles[i + 2];
						int index3 = triangles[i + 1];

						tempObjectSb.AppendFormat(CultureInfo.InvariantCulture,"{0},{1},{2}", index1, index2, index3);
					}

					tempObjectSb.AppendLine();

					tempObjectSb.AppendLine("\t\t\t}");
					tempObjectSb.AppendLine("\t\t}");
				}

				// -- UV 2 Creation
				// TODO: Add UV2 Creation here

				// -- Smoothing
				// TODO: Smoothing doesn't seem to do anything when importing. This maybe should be added. -KBH

				// ============ MATERIALS =============

				tempObjectSb.AppendLine("\t\tLayerElementMaterial: 0 {");
				tempObjectSb.AppendLine("\t\t\tVersion: 101");
				tempObjectSb.AppendLine("\t\t\tName: \"\"");
				tempObjectSb.AppendLine("\t\t\tMappingInformationType: \"ByPolygon\"");
				tempObjectSb.AppendLine("\t\t\tReferenceInformationType: \"IndexToDirect\"");

				int totalFaceCount = 0;

				// So by polygon means that we need 1/3rd of how many indicies we wrote.
				int numberOfSubmeshes = mesh.subMeshCount;

				StringBuilder submeshesSb = new StringBuilder();

				// For just one submesh, we set them all to zero
				if(numberOfSubmeshes == 1)
				{
					int numFaces = triangles.Length / 3;

					for(int i = 0; i < numFaces; i++)
					{
						submeshesSb.Append("0,");
						totalFaceCount++;
					}
				}
				else
				{
					List<int[]> allSubmeshes = new List<int[]>();
					
					// Load all submeshes into a space
					for(int i = 0; i < numberOfSubmeshes; i++)
						allSubmeshes.Add(mesh.GetIndices(i));

					// TODO: Optimize this search pattern
					for(int i = 0; i < triangles.Length; i += 3)
					{
						for(int subMeshIndex = 0; subMeshIndex < allSubmeshes.Count; subMeshIndex++)
						{
							bool breaker = false;
							
							for(int n = 0; n < allSubmeshes[subMeshIndex].Length; n += 3)
							{
								if(triangles[i] == allSubmeshes[subMeshIndex][n]
								   && triangles[i + 1] == allSubmeshes[subMeshIndex][n + 1]
								   && triangles[i + 2] == allSubmeshes[subMeshIndex][n + 2])
								{
									submeshesSb.Append(subMeshIndex.ToString());
									submeshesSb.Append(",");
									totalFaceCount++;
									break;
								}
								
								if(breaker)
									break;
							}
						}
					}
				}

				tempObjectSb.AppendLine("\t\t\tMaterials: *" + totalFaceCount + " {");
				tempObjectSb.Append("\t\t\t\ta: ");
				tempObjectSb.AppendLine(submeshesSb.ToString());
				tempObjectSb.AppendLine("\t\t\t} ");
				tempObjectSb.AppendLine("\t\t}");

				// ============= INFORMS WHAT TYPE OF LATER ELEMENTS ARE IN THIS GEOMETRY =================
				tempObjectSb.AppendLine("\t\tLayer: 0 {");
				tempObjectSb.AppendLine("\t\t\tVersion: 100");
				tempObjectSb.AppendLine("\t\t\tLayerElement:  {");
				tempObjectSb.AppendLine("\t\t\t\tType: \"LayerElementNormal\"");
				tempObjectSb.AppendLine("\t\t\t\tTypedIndex: 0");
				tempObjectSb.AppendLine("\t\t\t}");
				tempObjectSb.AppendLine("\t\t\tLayerElement:  {");
				tempObjectSb.AppendLine("\t\t\t\tType: \"LayerElementTangent\"");
				tempObjectSb.AppendLine("\t\t\t\tTypedIndex: 0");
				tempObjectSb.AppendLine("\t\t\t}");
				tempObjectSb.AppendLine("\t\t\tLayerElement:  {");
				tempObjectSb.AppendLine("\t\t\t\tType: \"LayerElementBinormal\"");
				tempObjectSb.AppendLine("\t\t\t\tTypedIndex: 0");
				tempObjectSb.AppendLine("\t\t\t}");
				tempObjectSb.AppendLine("\t\t\tLayerElement:  {");
				tempObjectSb.AppendLine("\t\t\t\tType: \"LayerElementMaterial\"");
				tempObjectSb.AppendLine("\t\t\t\tTypedIndex: 0");
				tempObjectSb.AppendLine("\t\t\t}");
				tempObjectSb.AppendLine("\t\t\tLayerElement:  {");
				tempObjectSb.AppendLine("\t\t\t\tType: \"LayerElementTexture\"");
				tempObjectSb.AppendLine("\t\t\t\tTypedIndex: 0");
				tempObjectSb.AppendLine("\t\t\t}");
				tempObjectSb.AppendLine("\t\t\tLayerElement:  {");
				tempObjectSb.AppendLine("\t\t\t\tType: \"LayerElementUV\"");
				tempObjectSb.AppendLine("\t\t\t\tTypedIndex: 0");
				tempObjectSb.AppendLine("\t\t\t}");
				// TODO: Here we would add UV layer 1 for ambient occlusion UV file
	//			tempObjectSb.AppendLine("\t\t\tLayerElement:  {");
	//			tempObjectSb.AppendLine("\t\t\t\tType: \"LayerElementUV\"");
	//			tempObjectSb.AppendLine("\t\t\t\tTypedIndex: 1");
	//			tempObjectSb.AppendLine("\t\t\t}");
				tempObjectSb.AppendLine("\t\t}");
				tempObjectSb.AppendLine("\t}");

				// Add the connection for the model to the geometry so it is attached the right mesh
				tempConnectionsSb.AppendLine("\t;Geometry::, Model::" + mesh.name);
				tempConnectionsSb.AppendLine("\tC: \"OO\"," + geometryId + "," + modelId);
				tempConnectionsSb.AppendLine();

				// Add the connection of all the materials in order of submesh
				MeshRenderer meshRenderer = gameObj.GetComponent<MeshRenderer>();
				if (meshRenderer)
				{
					Material[] allMaterialsInThisMesh = meshRenderer.sharedMaterials;

					for(int i = 0; i < allMaterialsInThisMesh.Length; i++)
					{
						Material mat = allMaterialsInThisMesh[i];		
						if (mat == null)
						{
							Debug.LogError("ERROR: the game object " + gameObj.name + " has an empty material on it. This will export problematic files. Please fix and reexport");
							continue;
						}
						
						int referenceId = Mathf.Abs(mat.GetInstanceID());
						tempConnectionsSb.AppendLine("\t;Material::" + mat.name + ", Model::" + mesh.name);
						tempConnectionsSb.AppendLine("\tC: \"OO\"," + referenceId + "," + modelId);
						tempConnectionsSb.AppendLine();
					}
				} else
				{
					int referenceId = Mathf.Abs(colliderMaterial.GetInstanceID());
					tempConnectionsSb.AppendLine("\t;Material::" + colliderMaterial.name + ", Model::" + mesh.name);
					tempConnectionsSb.AppendLine("\tC: \"OO\"," + referenceId + "," + modelId);
					tempConnectionsSb.AppendLine();
				}
			}

			// Recursively add all the other objects to the string that has been built.
			for(int i = 0; i < gameObj.transform.childCount; i++)
			{
				GameObject childObject = gameObj.transform.GetChild(i).gameObject;
				bool recursiveHasNormals;
				bool recursiveHasTangents;
				FBXUnityMeshGetter.GetMeshToString(childObject, colliderMaterial, exportColliders, ref tempObjectSb, ref tempConnectionsSb, out recursiveHasNormals, out recursiveHasTangents, gameObj, modelId);
				hasNormals = recursiveHasNormals | hasNormals;
				hasTangents = recursiveHasTangents | hasTangents;
			}

			objects.Append(tempObjectSb.ToString());
			connections.Append(tempConnectionsSb.ToString());

			return modelId;
		}
	}
}
