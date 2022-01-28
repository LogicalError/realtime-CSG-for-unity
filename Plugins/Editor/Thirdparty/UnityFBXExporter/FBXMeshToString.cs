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

		public static string MeshToString (GameObject gameObj, string newPath, Material colliderMaterial, bool exportColliders, out bool hasNormals, out bool hasTangents, out bool hasMaterials, bool copyMaterials = false, bool copyTextures = false)
		{
			StringBuilder sb = new StringBuilder();
			
			StringBuilder objectProps = new StringBuilder();
			objectProps.AppendLine("; Object properties");
			objectProps.AppendLine(";------------------------------------------------------------------");
			objectProps.AppendLine("");
			objectProps.AppendLine("Objects:  {");
			
			StringBuilder objectConnections = new StringBuilder();
			objectConnections.AppendLine("; Object connections");
			objectConnections.AppendLine(";------------------------------------------------------------------");
			objectConnections.AppendLine("");
			objectConnections.AppendLine("Connections:  {");
			objectConnections.AppendLine("\t");

			Material[] materials = new Material[0];

			// First finds all unique materials and compiles them (and writes to the object connections) for funzies
			string materialsObjectSerialized = "";
			string materialConnectionsSerialized = "";

			FBXUnityMaterialGetter.GetAllMaterialsToString(gameObj, newPath, colliderMaterial, exportColliders, copyMaterials, copyTextures, out materials, out materialsObjectSerialized, out materialConnectionsSerialized);

			// Run recursive FBX Mesh grab over the entire gameobject
			FBXUnityMeshGetter.GetMeshToString(gameObj, colliderMaterial, exportColliders, ref objectProps, ref objectConnections, out hasNormals, out hasTangents);

			hasMaterials = materials.Length > 0;

			// write the materials to the objectProps here. Should not do it in the above as it recursive.

			objectProps.Append(materialsObjectSerialized);
			objectConnections.Append(materialConnectionsSerialized);

			// Close up both builders;
			objectProps.AppendLine("}");
			objectConnections.AppendLine("}");

			
			// ========= Create header ========
			
			// Intro
			sb.AppendLine("; FBX 7.3.0 project file");
			sb.AppendLine("; Copyright (C) 1997-2010 Autodesk Inc. and/or its licensors.");
			sb.AppendLine("; All rights reserved.");
			sb.AppendLine("; ----------------------------------------------------");
			sb.AppendLine();
			
			// The header
			sb.AppendLine("FBXHeaderExtension:  {");
			sb.AppendLine("\tFBXHeaderVersion: 1003");
			sb.AppendLine("\tFBXVersion: 7300");

			// Creationg Date Stamp
			System.DateTime currentDate = System.DateTime.Now;
			sb.AppendLine("\tCreationTimeStamp:  {");
			sb.AppendLine("\t\tVersion: 1000");
			sb.AppendLine("\t\tYear: " + currentDate.Year);
			sb.AppendLine("\t\tMonth: " + currentDate.Month);
			sb.AppendLine("\t\tDay: " + currentDate.Day);
			sb.AppendLine("\t\tHour: " + currentDate.Hour);
			sb.AppendLine("\t\tMinute: " + currentDate.Minute);
			sb.AppendLine("\t\tSecond: " + currentDate.Second);
			sb.AppendLine("\t\tMillisecond: " + currentDate.Millisecond);
			sb.AppendLine("\t}");
			
			// Info on the Creator
			sb.AppendLine("\tCreator: \"" + VersionInformation + "\"");
			sb.AppendLine("\tSceneInfo: \"SceneInfo::GlobalInfo\", \"UserData\" {");
			sb.AppendLine("\t\tType: \"UserData\"");
			sb.AppendLine("\t\tVersion: 100");
			sb.AppendLine("\t\tMetaData:  {");
			sb.AppendLine("\t\t\tVersion: 100");
			sb.AppendLine("\t\t\tTitle: \"\"");
			sb.AppendLine("\t\t\tSubject: \"\"");
			sb.AppendLine("\t\t\tAuthor: \"\"");
			sb.AppendLine("\t\t\tKeywords: \"\"");
			sb.AppendLine("\t\t\tRevision: \"\"");
			sb.AppendLine("\t\t\tComment: \"\"");
			sb.AppendLine("\t\t}");
			sb.AppendLine("\t\tProperties70:  {");

			// Information on how this item was originally generated
			string documentInfoPaths = Application.dataPath + newPath + ".fbx";
			sb.AppendLine("\t\t\tP: \"DocumentUrl\", \"KString\", \"Url\", \"\", \"" + documentInfoPaths + "\"");
			sb.AppendLine("\t\t\tP: \"SrcDocumentUrl\", \"KString\", \"Url\", \"\", \"" + documentInfoPaths + "\"");
			sb.AppendLine("\t\t\tP: \"Original\", \"Compound\", \"\", \"\"");
			sb.AppendLine("\t\t\tP: \"Original|ApplicationVendor\", \"KString\", \"\", \"\", \"\"");
			sb.AppendLine("\t\t\tP: \"Original|ApplicationName\", \"KString\", \"\", \"\", \"\"");
			sb.AppendLine("\t\t\tP: \"Original|ApplicationVersion\", \"KString\", \"\", \"\", \"\"");
			sb.AppendLine("\t\t\tP: \"Original|DateTime_GMT\", \"DateTime\", \"\", \"\", \"\"");
			sb.AppendLine("\t\t\tP: \"Original|FileName\", \"KString\", \"\", \"\", \"\"");
			sb.AppendLine("\t\t\tP: \"LastSaved\", \"Compound\", \"\", \"\"");
			sb.AppendLine("\t\t\tP: \"LastSaved|ApplicationVendor\", \"KString\", \"\", \"\", \"\"");
			sb.AppendLine("\t\t\tP: \"LastSaved|ApplicationName\", \"KString\", \"\", \"\", \"\"");
			sb.AppendLine("\t\t\tP: \"LastSaved|ApplicationVersion\", \"KString\", \"\", \"\", \"\"");
			sb.AppendLine("\t\t\tP: \"LastSaved|DateTime_GMT\", \"DateTime\", \"\", \"\", \"\"");
			sb.AppendLine("\t\t}");
			sb.AppendLine("\t}");
			sb.AppendLine("}");
			
			// The Global information
			sb.AppendLine("GlobalSettings:  {");
			sb.AppendLine("\tVersion: 1000");
			sb.AppendLine("\tProperties70:  {");
			sb.AppendLine("\t\tP: \"UpAxis\", \"int\", \"Integer\", \"\",1");
			sb.AppendLine("\t\tP: \"UpAxisSign\", \"int\", \"Integer\", \"\",1");
			sb.AppendLine("\t\tP: \"FrontAxis\", \"int\", \"Integer\", \"\",2");
			sb.AppendLine("\t\tP: \"FrontAxisSign\", \"int\", \"Integer\", \"\",1");
			sb.AppendLine("\t\tP: \"CoordAxis\", \"int\", \"Integer\", \"\",0");
			sb.AppendLine("\t\tP: \"CoordAxisSign\", \"int\", \"Integer\", \"\",1");
			sb.AppendLine("\t\tP: \"OriginalUpAxis\", \"int\", \"Integer\", \"\",-1");
			sb.AppendLine("\t\tP: \"OriginalUpAxisSign\", \"int\", \"Integer\", \"\",1");
			sb.AppendLine("\t\tP: \"UnitScaleFactor\", \"double\", \"Number\", \"\",100"); // NOTE: This sets the resize scale upon import
			sb.AppendLine("\t\tP: \"OriginalUnitScaleFactor\", \"double\", \"Number\", \"\",100");
			sb.AppendLine("\t\tP: \"AmbientColor\", \"ColorRGB\", \"Color\", \"\",0,0,0");
			sb.AppendLine("\t\tP: \"DefaultCamera\", \"KString\", \"\", \"\", \"Producer Perspective\"");
			sb.AppendLine("\t\tP: \"TimeMode\", \"enum\", \"\", \"\",11");
			sb.AppendLine("\t\tP: \"TimeSpanStart\", \"KTime\", \"Time\", \"\",0");
			sb.AppendLine("\t\tP: \"TimeSpanStop\", \"KTime\", \"Time\", \"\",479181389250");
			sb.AppendLine("\t\tP: \"CustomFrameRate\", \"double\", \"Number\", \"\",-1");
			sb.AppendLine("\t}");
			sb.AppendLine("}");
			
			// The Object definations
			sb.AppendLine("; Object definitions");
			sb.AppendLine(";------------------------------------------------------------------");
			sb.AppendLine("");
			sb.AppendLine("Definitions:  {");
			sb.AppendLine("\tVersion: 100");
			sb.AppendLine("\tCount: 4");

			sb.AppendLine("\tObjectType: \"GlobalSettings\" {");
			sb.AppendLine("\t\tCount: 1");
			sb.AppendLine("\t}");


			sb.AppendLine("\tObjectType: \"Model\" {");
			sb.AppendLine("\t\tCount: 1"); // TODO figure out if this count matters
			sb.AppendLine("\t\tPropertyTemplate: \"FbxNode\" {");
			sb.AppendLine("\t\t\tProperties70:  {");
			sb.AppendLine("\t\t\t\tP: \"QuaternionInterpolate\", \"enum\", \"\", \"\",0");
			sb.AppendLine("\t\t\t\tP: \"RotationOffset\", \"Vector3D\", \"Vector\", \"\",0,0,0");
			sb.AppendLine("\t\t\t\tP: \"RotationPivot\", \"Vector3D\", \"Vector\", \"\",0,0,0");
			sb.AppendLine("\t\t\t\tP: \"ScalingOffset\", \"Vector3D\", \"Vector\", \"\",0,0,0");
			sb.AppendLine("\t\t\t\tP: \"ScalingPivot\", \"Vector3D\", \"Vector\", \"\",0,0,0");
			sb.AppendLine("\t\t\t\tP: \"TranslationActive\", \"bool\", \"\", \"\",0");
			sb.AppendLine("\t\t\t\tP: \"TranslationMin\", \"Vector3D\", \"Vector\", \"\",0,0,0");
			sb.AppendLine("\t\t\t\tP: \"TranslationMax\", \"Vector3D\", \"Vector\", \"\",0,0,0");
			sb.AppendLine("\t\t\t\tP: \"TranslationMinX\", \"bool\", \"\", \"\",0");
			sb.AppendLine("\t\t\t\tP: \"TranslationMinY\", \"bool\", \"\", \"\",0");
			sb.AppendLine("\t\t\t\tP: \"TranslationMinZ\", \"bool\", \"\", \"\",0");
			sb.AppendLine("\t\t\t\tP: \"TranslationMaxX\", \"bool\", \"\", \"\",0");
			sb.AppendLine("\t\t\t\tP: \"TranslationMaxY\", \"bool\", \"\", \"\",0");
			sb.AppendLine("\t\t\t\tP: \"TranslationMaxZ\", \"bool\", \"\", \"\",0");
			sb.AppendLine("\t\t\t\tP: \"RotationOrder\", \"enum\", \"\", \"\",0");
			sb.AppendLine("\t\t\t\tP: \"RotationSpaceForLimitOnly\", \"bool\", \"\", \"\",0");
			sb.AppendLine("\t\t\t\tP: \"RotationStiffnessX\", \"double\", \"Number\", \"\",0");
			sb.AppendLine("\t\t\t\tP: \"RotationStiffnessY\", \"double\", \"Number\", \"\",0");
			sb.AppendLine("\t\t\t\tP: \"RotationStiffnessZ\", \"double\", \"Number\", \"\",0");
			sb.AppendLine("\t\t\t\tP: \"AxisLen\", \"double\", \"Number\", \"\",10");
			sb.AppendLine("\t\t\t\tP: \"PreRotation\", \"Vector3D\", \"Vector\", \"\",0,0,0");
			sb.AppendLine("\t\t\t\tP: \"PostRotation\", \"Vector3D\", \"Vector\", \"\",0,0,0");
			sb.AppendLine("\t\t\t\tP: \"RotationActive\", \"bool\", \"\", \"\",0");
			sb.AppendLine("\t\t\t\tP: \"RotationMin\", \"Vector3D\", \"Vector\", \"\",0,0,0");
			sb.AppendLine("\t\t\t\tP: \"RotationMax\", \"Vector3D\", \"Vector\", \"\",0,0,0");
			sb.AppendLine("\t\t\t\tP: \"RotationMinX\", \"bool\", \"\", \"\",0");
			sb.AppendLine("\t\t\t\tP: \"RotationMinY\", \"bool\", \"\", \"\",0");
			sb.AppendLine("\t\t\t\tP: \"RotationMinZ\", \"bool\", \"\", \"\",0");
			sb.AppendLine("\t\t\t\tP: \"RotationMaxX\", \"bool\", \"\", \"\",0");
			sb.AppendLine("\t\t\t\tP: \"RotationMaxY\", \"bool\", \"\", \"\",0");
			sb.AppendLine("\t\t\t\tP: \"RotationMaxZ\", \"bool\", \"\", \"\",0");
			sb.AppendLine("\t\t\t\tP: \"InheritType\", \"enum\", \"\", \"\",0");
			sb.AppendLine("\t\t\t\tP: \"ScalingActive\", \"bool\", \"\", \"\",0");
			sb.AppendLine("\t\t\t\tP: \"ScalingMin\", \"Vector3D\", \"Vector\", \"\",0,0,0");
			sb.AppendLine("\t\t\t\tP: \"ScalingMax\", \"Vector3D\", \"Vector\", \"\",1,1,1");
			sb.AppendLine("\t\t\t\tP: \"ScalingMinX\", \"bool\", \"\", \"\",0");
			sb.AppendLine("\t\t\t\tP: \"ScalingMinY\", \"bool\", \"\", \"\",0");
			sb.AppendLine("\t\t\t\tP: \"ScalingMinZ\", \"bool\", \"\", \"\",0");
			sb.AppendLine("\t\t\t\tP: \"ScalingMaxX\", \"bool\", \"\", \"\",0");
			sb.AppendLine("\t\t\t\tP: \"ScalingMaxY\", \"bool\", \"\", \"\",0");
			sb.AppendLine("\t\t\t\tP: \"ScalingMaxZ\", \"bool\", \"\", \"\",0");
			sb.AppendLine("\t\t\t\tP: \"GeometricTranslation\", \"Vector3D\", \"Vector\", \"\",0,0,0");
			sb.AppendLine("\t\t\t\tP: \"GeometricRotation\", \"Vector3D\", \"Vector\", \"\",0,0,0");
			sb.AppendLine("\t\t\t\tP: \"GeometricScaling\", \"Vector3D\", \"Vector\", \"\",1,1,1");
			sb.AppendLine("\t\t\t\tP: \"MinDampRangeX\", \"double\", \"Number\", \"\",0");
			sb.AppendLine("\t\t\t\tP: \"MinDampRangeY\", \"double\", \"Number\", \"\",0");
			sb.AppendLine("\t\t\t\tP: \"MinDampRangeZ\", \"double\", \"Number\", \"\",0");
			sb.AppendLine("\t\t\t\tP: \"MaxDampRangeX\", \"double\", \"Number\", \"\",0");
			sb.AppendLine("\t\t\t\tP: \"MaxDampRangeY\", \"double\", \"Number\", \"\",0");
			sb.AppendLine("\t\t\t\tP: \"MaxDampRangeZ\", \"double\", \"Number\", \"\",0");
			sb.AppendLine("\t\t\t\tP: \"MinDampStrengthX\", \"double\", \"Number\", \"\",0");
			sb.AppendLine("\t\t\t\tP: \"MinDampStrengthY\", \"double\", \"Number\", \"\",0");
			sb.AppendLine("\t\t\t\tP: \"MinDampStrengthZ\", \"double\", \"Number\", \"\",0");
			sb.AppendLine("\t\t\t\tP: \"MaxDampStrengthX\", \"double\", \"Number\", \"\",0");
			sb.AppendLine("\t\t\t\tP: \"MaxDampStrengthY\", \"double\", \"Number\", \"\",0");
			sb.AppendLine("\t\t\t\tP: \"MaxDampStrengthZ\", \"double\", \"Number\", \"\",0");
			sb.AppendLine("\t\t\t\tP: \"PreferedAngleX\", \"double\", \"Number\", \"\",0");
			sb.AppendLine("\t\t\t\tP: \"PreferedAngleY\", \"double\", \"Number\", \"\",0");
			sb.AppendLine("\t\t\t\tP: \"PreferedAngleZ\", \"double\", \"Number\", \"\",0");
			sb.AppendLine("\t\t\t\tP: \"LookAtProperty\", \"object\", \"\", \"\"");
			sb.AppendLine("\t\t\t\tP: \"UpVectorProperty\", \"object\", \"\", \"\"");
			sb.AppendLine("\t\t\t\tP: \"Show\", \"bool\", \"\", \"\",1");
			sb.AppendLine("\t\t\t\tP: \"NegativePercentShapeSupport\", \"bool\", \"\", \"\",1");
			sb.AppendLine("\t\t\t\tP: \"DefaultAttributeIndex\", \"int\", \"Integer\", \"\",-1");
			sb.AppendLine("\t\t\t\tP: \"Freeze\", \"bool\", \"\", \"\",0");
			sb.AppendLine("\t\t\t\tP: \"LODBox\", \"bool\", \"\", \"\",0");
			sb.AppendLine("\t\t\t\tP: \"Lcl Translation\", \"Lcl Translation\", \"\", \"A\",0,0,0");
			sb.AppendLine("\t\t\t\tP: \"Lcl Rotation\", \"Lcl Rotation\", \"\", \"A\",0,0,0");
			sb.AppendLine("\t\t\t\tP: \"Lcl Scaling\", \"Lcl Scaling\", \"\", \"A\",1,1,1");
			sb.AppendLine("\t\t\t\tP: \"Visibility\", \"Visibility\", \"\", \"A\",1");
			sb.AppendLine("\t\t\t\tP: \"Visibility Inheritance\", \"Visibility Inheritance\", \"\", \"\",1");
			sb.AppendLine("\t\t\t}");
			sb.AppendLine("\t\t}");
			sb.AppendLine("\t}");
			
			// The geometry, this is IMPORTANT
			sb.AppendLine("\tObjectType: \"Geometry\" {");
			sb.AppendLine("\t\tCount: 1"); // TODO - this must be set by the number of items being placed.
			sb.AppendLine("\t\tPropertyTemplate: \"FbxMesh\" {");
			sb.AppendLine("\t\t\tProperties70:  {");
			sb.AppendLine("\t\t\t\tP: \"Color\", \"ColorRGB\", \"Color\", \"\",0.8,0.8,0.8");
			sb.AppendLine("\t\t\t\tP: \"BBoxMin\", \"Vector3D\", \"Vector\", \"\",0,0,0");
			sb.AppendLine("\t\t\t\tP: \"BBoxMax\", \"Vector3D\", \"Vector\", \"\",0,0,0");
			sb.AppendLine("\t\t\t\tP: \"Primary Visibility\", \"bool\", \"\", \"\",1");
			sb.AppendLine("\t\t\t\tP: \"Casts Shadows\", \"bool\", \"\", \"\",1");
			sb.AppendLine("\t\t\t\tP: \"Receive Shadows\", \"bool\", \"\", \"\",1");
			sb.AppendLine("\t\t\t}");
			sb.AppendLine("\t\t}");
			sb.AppendLine("\t}");
			
			// The materials that are being placed. Has to be simple I think
			sb.AppendLine("\tObjectType: \"Material\" {");
			sb.AppendLine("\t\tCount: 1");
			sb.AppendLine("\t\tPropertyTemplate: \"FbxSurfacePhong\" {");
			sb.AppendLine("\t\t\tProperties70:  {");
			sb.AppendLine("\t\t\t\tP: \"ShadingModel\", \"KString\", \"\", \"\", \"Phong\"");
			sb.AppendLine("\t\t\t\tP: \"MultiLayer\", \"bool\", \"\", \"\",0");
			sb.AppendLine("\t\t\t\tP: \"EmissiveColor\", \"Color\", \"\", \"A\",0,0,0");
			sb.AppendLine("\t\t\t\tP: \"EmissiveFactor\", \"Number\", \"\", \"A\",1");
			sb.AppendLine("\t\t\t\tP: \"AmbientColor\", \"Color\", \"\", \"A\",0.2,0.2,0.2");
			sb.AppendLine("\t\t\t\tP: \"AmbientFactor\", \"Number\", \"\", \"A\",1");
			sb.AppendLine("\t\t\t\tP: \"DiffuseColor\", \"Color\", \"\", \"A\",0.8,0.8,0.8");
			sb.AppendLine("\t\t\t\tP: \"DiffuseFactor\", \"Number\", \"\", \"A\",1");
			sb.AppendLine("\t\t\t\tP: \"Bump\", \"Vector3D\", \"Vector\", \"\",0,0,0");
			sb.AppendLine("\t\t\t\tP: \"NormalMap\", \"Vector3D\", \"Vector\", \"\",0,0,0");
			sb.AppendLine("\t\t\t\tP: \"BumpFactor\", \"double\", \"Number\", \"\",1");
			sb.AppendLine("\t\t\t\tP: \"TransparentColor\", \"Color\", \"\", \"A\",0,0,0");
			sb.AppendLine("\t\t\t\tP: \"TransparencyFactor\", \"Number\", \"\", \"A\",0");
			sb.AppendLine("\t\t\t\tP: \"DisplacementColor\", \"ColorRGB\", \"Color\", \"\",0,0,0");
			sb.AppendLine("\t\t\t\tP: \"DisplacementFactor\", \"double\", \"Number\", \"\",1");
			sb.AppendLine("\t\t\t\tP: \"VectorDisplacementColor\", \"ColorRGB\", \"Color\", \"\",0,0,0");
			sb.AppendLine("\t\t\t\tP: \"VectorDisplacementFactor\", \"double\", \"Number\", \"\",1");
			sb.AppendLine("\t\t\t\tP: \"SpecularColor\", \"Color\", \"\", \"A\",0.2,0.2,0.2");
			sb.AppendLine("\t\t\t\tP: \"SpecularFactor\", \"Number\", \"\", \"A\",1");
			sb.AppendLine("\t\t\t\tP: \"ShininessExponent\", \"Number\", \"\", \"A\",20");
			sb.AppendLine("\t\t\t\tP: \"ReflectionColor\", \"Color\", \"\", \"A\",0,0,0");
			sb.AppendLine("\t\t\t\tP: \"ReflectionFactor\", \"Number\", \"\", \"A\",1");
			sb.AppendLine("\t\t\t}");
			sb.AppendLine("\t\t}");
			sb.AppendLine("\t}");

			// Explanation of how textures work
			sb.AppendLine("\tObjectType: \"Texture\" {");
			sb.AppendLine("\t\tCount: 2"); // TODO - figure out if this texture number is important
			sb.AppendLine("\t\tPropertyTemplate: \"FbxFileTexture\" {");
			sb.AppendLine("\t\t\tProperties70:  {");
			sb.AppendLine("\t\t\t\tP: \"TextureTypeUse\", \"enum\", \"\", \"\",0");
			sb.AppendLine("\t\t\t\tP: \"Texture alpha\", \"Number\", \"\", \"A\",1");
			sb.AppendLine("\t\t\t\tP: \"CurrentMappingType\", \"enum\", \"\", \"\",0");
			sb.AppendLine("\t\t\t\tP: \"WrapModeU\", \"enum\", \"\", \"\",0");
			sb.AppendLine("\t\t\t\tP: \"WrapModeV\", \"enum\", \"\", \"\",0");
			sb.AppendLine("\t\t\t\tP: \"UVSwap\", \"bool\", \"\", \"\",0");
			sb.AppendLine("\t\t\t\tP: \"PremultiplyAlpha\", \"bool\", \"\", \"\",1");
			sb.AppendLine("\t\t\t\tP: \"Translation\", \"Vector\", \"\", \"A\",0,0,0");
			sb.AppendLine("\t\t\t\tP: \"Rotation\", \"Vector\", \"\", \"A\",0,0,0");
			sb.AppendLine("\t\t\t\tP: \"Scaling\", \"Vector\", \"\", \"A\",1,1,1");
			sb.AppendLine("\t\t\t\tP: \"TextureRotationPivot\", \"Vector3D\", \"Vector\", \"\",0,0,0");
			sb.AppendLine("\t\t\t\tP: \"TextureScalingPivot\", \"Vector3D\", \"Vector\", \"\",0,0,0");
			sb.AppendLine("\t\t\t\tP: \"CurrentTextureBlendMode\", \"enum\", \"\", \"\",1");
			sb.AppendLine("\t\t\t\tP: \"UVSet\", \"KString\", \"\", \"\", \"default\"");
			sb.AppendLine("\t\t\t\tP: \"UseMaterial\", \"bool\", \"\", \"\",0");
			sb.AppendLine("\t\t\t\tP: \"UseMipMap\", \"bool\", \"\", \"\",0");
			sb.AppendLine("\t\t\t}");
			sb.AppendLine("\t\t}");
			sb.AppendLine("\t}");

			sb.AppendLine("}");
			sb.AppendLine("");

			sb.Append(objectProps.ToString());
			sb.Append(objectConnections.ToString());

			return sb.ToString();
		}

	}
}
