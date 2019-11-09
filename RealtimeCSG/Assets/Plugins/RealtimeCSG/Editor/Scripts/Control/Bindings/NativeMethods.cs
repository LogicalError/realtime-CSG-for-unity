#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using RealtimeCSG.Foundation;
using RealtimeCSG.Legacy;
using RealtimeCSG.Components;

namespace RealtimeCSG
{
	#region delegates

	#region Tree delegates
	internal delegate bool GenerateModelDelegate			(Int32				userID,
															 out Int32			generatedModelNodeID);

	internal delegate bool SetModelDelegate					(Int32				modelNodeID,
															 bool				isEnabled);

	internal delegate bool SetDirtyDelegate					(Int32				nodeID);
	
	internal delegate bool SetModelEnabledDelegate			(Int32				modelNodeID,
															 bool				isEnabled);
	#endregion

	#region Selection delegates
	internal delegate bool RayCastMultiDelegate				(int					modelCount,
															 CSGModel[]				models,
															 Vector3				rayStart,
															 Vector3				rayEnd,
															 bool					ignoreInvisiblePolygons,
															 out LegacyBrushIntersection[] intersections,
															 CSGBrush[]				ignoreBrushes = null);

	internal delegate bool RayCastIntoModelMultiDelegate	(CSGModel				model, 
															 Vector3				rayStart,
															 Vector3				rayEnd,
															 bool					ignoreInvisiblePolygons,
															 out LegacyBrushIntersection[] intersections,
															 CSGBrush[]				ignoreBrushes = null);

	internal delegate bool RayCastIntoModelDelegate			(CSGModel				model, 
															 Vector3				rayStart,
															 Vector3				rayEnd,
															 bool					ignoreInvisiblePolygons,
															 out LegacyBrushIntersection	intersection,
															 CSGBrush[]				ignoreBrushes = null);
		
	internal delegate bool RayCastIntoBrushDelegate			(Int32					brushNodeID, 
															 Vector3				rayStart,
															 Vector3				rayEnd,
															 Matrix4x4				modelTransformation,
															 out LegacyBrushIntersection	intersection,
															 bool					ignoreInvisiblePolygons);

	internal delegate bool RayCastIntoBrushSurfaceDelegate	(Int32							brushNodeID, 
															 Int32							surfaceIndex,
															 Vector3						rayStart,
															 Vector3						rayEnd,
															 Matrix4x4						modelTransformation,
															 out LegacySurfaceIntersection	intersection);

	internal delegate bool GetItemsInFrustumDelegate		(CSGModel				model, 
															 Plane[]				planes, 
															 HashSet<GameObject>	gameObjects);
	#endregion

	
	internal delegate bool SetChildNodesDelegate			(Int32				nodeID,
															 Int32				childCount,
															 Int32[]			childrenNodeIDs);
	internal delegate bool DestroyNodeDelegate				(Int32				operationNodeID);

	internal delegate bool DestroyNodesDelegate				(Int32[]			operationNodeIDs);

	#region Operation delegates
	internal delegate bool GenerateOperationDelegate		(Int32				userID,
															 out Int32			generatedOperationNodeID);

	internal delegate bool SetOperationOperationTypeDelegate(Int32				operationNodeID,
															 CSGOperationType	operation);

	#endregion

	#region Brush delegates	
	internal delegate bool GenerateBrushDelegate			(Int32				userID,
															 out Int32			generatedBrushNodeID);

	internal delegate Int32 GetBrushMeshIDDelegate			(Int32				brushNodeID);
	internal delegate bool SetBrushMeshIDDelegate			(Int32				brushNodeID, Int32 brushMeshIndex);
	
	internal delegate CSGTreeBrushFlags GetBrushFlagsDelegate(Int32				brushNodeID);
	internal delegate bool SetBrushFlagsDelegate			(Int32				brushNodeID, CSGTreeBrushFlags flags);

	internal delegate bool SetBrushOperationTypeDelegate	(Int32				brushNodeID,
															 Foundation.CSGOperationType operation);

	internal delegate bool SetBrushToModelSpaceDelegate		(Int32				brushNodeID,
															 Matrix4x4			localToModelSpace);

	internal delegate Int32 CreateBrushMeshDelegate			(Int32				userID, Foundation.BrushMesh brushMesh);
	
	internal delegate bool UpdateBrushMeshDelegate			(Int32				brushMeshIndex,
															 Foundation.BrushMesh brushMesh);

	internal delegate bool DestroyBrushMeshDelegate			(Int32				brushMeshIndex);
	#endregion

	#region Misc
	internal delegate List<List<Vector2>> ConvexPartitionDelegate (Vector2[] points);

	#endregion

	#region TexGen manipulation delegates	

	internal delegate bool GetSurfaceMinMaxTexCoordsDelegate(Int32				brushNodeID,
															 Int32				surfaceIndex,
															 Matrix4x4			modelLocalToWorldMatrix,
															 out Vector2		minTextureCoordinate, 
															 out Vector2		maxTextureCoordinate);

	internal delegate bool GetSurfaceMinMaxWorldCoordDelegate(Int32				brushNodeID,
															 Int32				surfaceIndex, 
															 out Vector3		minWorldCoordinate, 
															 out Vector3		maxWorldCoordinate);

	internal delegate bool ConvertWorldToTextureCoordDelegate(Int32				brushNodeID,
															  Int32				surfaceIndex, 
															  Matrix4x4			modelTransformation,
															  Vector3			worldCoordinate, 
															  out Vector2		textureCoordinate);

	internal delegate bool ConvertTextureToWorldCoordDelegate(Int32				brushNodeID,
															  Int32				surfaceIndex, 
															  float				textureCoordinateU, 
															  float				textureCoordinateV,
															  // workaround for mac-osx related bug
															  ref Matrix4x4		modelTransformation,
															  ref float			worldCoordinateX, 
															  ref float			worldCoordinateY, 
															  ref float			worldCoordinateZ);


	#endregion

	#region Outlines
	internal delegate UInt64 GetBrushOutlineGenerationDelegate(Int32 brushNodeID);
	internal delegate bool GetBrushOutlineDelegate			(Int32					brushNodeID,
															 out GeometryWireframe	geometryWireframe);
	internal delegate bool GetSurfaceOutlineDelegate		(Int32					brushNodeID,
															 Int32					surfaceIndex,
															 out GeometryWireframe	outline);
	#endregion

	#region Meshes

	internal delegate void ResetCSGDelegate();

	internal delegate bool UpdateAllModelMeshesDelegate		();
	internal delegate bool ModelMeshesNeedUpdateDelegate	();
	internal delegate bool GetMeshDescriptionsDelegate		(CSGModel		model,
															 ref GeneratedMeshDescription[]	meshDescriptions);

	internal delegate GeneratedMeshContents GetModelMeshDelegate(int modelNodeID, GeneratedMeshDescription meshDescription);
	internal delegate bool GetModelMeshNoAllocDelegate(int modelNodeID, GeneratedMeshDescription meshDescription, ref GeneratedMeshContents generatedMeshData);
	
	#endregion

	#endregion
	
	internal sealed class NativeMethods
	{
		public ResetCSGDelegate				        ResetCSG;
		public ConvexPartitionDelegate              ConvexPartition;

		public SetDirtyDelegate						SetDirty;
		public SetChildNodesDelegate				SetChildNodes;
		public DestroyNodeDelegate				    DestroyNode;
		public DestroyNodesDelegate					DestroyNodes;

		public GenerateModelDelegate				GenerateModel;
		public SetModelEnabledDelegate              SetModelEnabled;

		public GenerateOperationDelegate			GenerateOperation;
		public SetOperationOperationTypeDelegate	SetOperationOperationType;
		
		public GenerateBrushDelegate				GenerateBrush;
		public GetBrushMeshIDDelegate				GetBrushMeshID;
		public SetBrushMeshIDDelegate				SetBrushMeshID;
		public GetBrushFlagsDelegate                GetBrushFlags;
		public SetBrushFlagsDelegate				SetBrushFlags;
		public SetBrushOperationTypeDelegate		SetBrushOperationType;
		public SetBrushToModelSpaceDelegate         SetBrushToModelSpace;

		
		public CreateBrushMeshDelegate				CreateBrushMesh;
		public UpdateBrushMeshDelegate				UpdateBrushMesh;
		public DestroyBrushMeshDelegate				DestroyBrushMesh;


		public UpdateAllModelMeshesDelegate         UpdateAllModelMeshes;
		public GetMeshDescriptionsDelegate          GetMeshDescriptions;

		public GetModelMeshDelegate					GetModelMesh;
		public GetModelMeshNoAllocDelegate          GetModelMeshNoAlloc;


		public RayCastMultiDelegate					RayCastMulti;
		public RayCastIntoModelMultiDelegate        RayCastIntoModelMulti;
		public RayCastIntoBrushDelegate             RayCastIntoBrush;
		public RayCastIntoBrushSurfaceDelegate      RayCastIntoBrushSurface;
		public GetItemsInFrustumDelegate            GetItemsInFrustum;
		
		public GetSurfaceMinMaxTexCoordsDelegate	GetSurfaceMinMaxTexCoords;
		public ConvertWorldToTextureCoordDelegate	ConvertModelToTextureSpace;
		public ConvertTextureToWorldCoordDelegate	ConvertTextureToModelSpace;
		
				
		public GetBrushOutlineGenerationDelegate	GetBrushOutlineGeneration;
		public GetBrushOutlineDelegate				GetBrushOutline;
		public GetSurfaceOutlineDelegate			GetSurfaceOutline;
	}
}
#endif