#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using RealtimeCSG.Legacy;
using RealtimeCSG.Foundation;
using RealtimeCSG.Components;

namespace RealtimeCSG
{
	/// <summary>
	/// This class defines an intersection into a specific surface of a brush
	/// </summary>
	[Serializable, StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct CSGSurfaceIntersection
	{
		public Plane		localPlane;
		public Plane		modelPlane;
		public Plane		worldPlane;

		public Vector3      worldIntersection;
		public Vector2      surfaceIntersection;

		public float        distance;

        public readonly static CSGSurfaceIntersection None = new CSGSurfaceIntersection()
        {
            localPlane          = new Plane(Vector3.zero, 0),
            modelPlane          = new Plane(Vector3.zero, 0),
            worldPlane          = new Plane(Vector3.zero, 0),
            worldIntersection   = Vector3.zero,
            surfaceIntersection = Vector2.zero,
            distance            = float.PositiveInfinity
        };
    };

	/// <summary>
	/// This class defines an intersection into a specific brush
	/// </summary>
	[Serializable, StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct CSGTreeBrushIntersection
	{
		public CSGTree		tree;
		public CSGTreeBrush	brush;
		public Int32        surfaceID;
		public Int32        brushUserID;

		public CSGSurfaceIntersection intersection;

        public readonly static CSGTreeBrushIntersection None = new CSGTreeBrushIntersection()
        {
            tree			= (CSGTree)CSGTreeNode.InvalidNode,
            brush			= (CSGTreeBrush)CSGTreeNode.InvalidNode,
            brushUserID		= 0,
            surfaceID	    = -1,
            intersection    = CSGSurfaceIntersection.None
        };
	};

	internal static class NativeMethodBindings
	{
		const string NativePluginName =  CSGManager.NativePluginName;

		#region Functionality to allow C# methods to be called from C++
		public delegate float   GetFloatAction();
		public delegate Int32   GetInt32Action();
		public delegate void	StringLog([MarshalAs(UnmanagedType.LPStr)] string text, int uniqueObjectID);
		[return: MarshalAs(UnmanagedType.LPStr)] public delegate string	ReturnStringMethod(int uniqueObjectID);

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
		struct UnityMethods
		{
			public StringLog DebugLog;
			public StringLog DebugLogError;
			public StringLog DebugLogWarning;
			public ReturnStringMethod NameForUserID;
		}

		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern void RegisterMethods([In] ref UnityMethods unityMethods);

		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern void ClearMethods();
		
		public static void RegisterUnityMethods()
		{
			UnityMethods unityMethods;
			
			unityMethods.DebugLog           = delegate (string message, int uniqueObjectID)
			{
				Debug.Log(message, (uniqueObjectID != 0) ? EditorUtility.InstanceIDToObject(uniqueObjectID) : null);
			};
			unityMethods.DebugLogError      = delegate (string message, int uniqueObjectID) 
			{
				Debug.LogError(message, (uniqueObjectID != 0) ? EditorUtility.InstanceIDToObject(uniqueObjectID) : null);
			};
			unityMethods.DebugLogWarning    = delegate (string message, int uniqueObjectID) 
			{
				Debug.LogWarning(message, (uniqueObjectID != 0) ? EditorUtility.InstanceIDToObject(uniqueObjectID) : null);
			};
			unityMethods.NameForUserID		= delegate (int uniqueObjectID)
			{
				var obj = (uniqueObjectID != 0) ? EditorUtility.InstanceIDToObject(uniqueObjectID) : null;
				if (obj == null)
					return "<unknown>";
				else
					return obj.name;
			};
			
			RegisterMethods(ref unityMethods);
		}

		public static void ClearUnityMethods()
		{
			ClearMethods();
			ClearAllNodes();
		}
		#endregion

		#region C++ Registration/Update functions

		#region Diagnostics
		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		public static extern void	LogDiagnostics();
		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		public static extern void	RebuildAll();
		#endregion

		#region Scene event functions

		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern void	ClearAllNodes();

		#endregion

		#region Polygon Convex Decomposition
		
		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool DecomposeStart(Int32			vertexCount,
												  [In] IntPtr	vertices,		
												  out Int32		polygonCount);
		
		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool DecomposeGetSizes(Int32			polygonCount,
													 [Out] IntPtr	polygonSizes);

		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool DecomposeGetPolygon(Int32		polygonIndex,
													   Int32		vertexSize,
													   [Out] IntPtr	vertices);

		private static List<List<Vector2>> ConvexPartition(Vector2[] points)
		{
			Int32 polygonCount = 0;
			GCHandle	pointsHandle = GCHandle.Alloc(points, GCHandleType.Pinned);
			IntPtr		pointsPtr = pointsHandle.AddrOfPinnedObject();
			var result = DecomposeStart(points.Length, pointsPtr, out polygonCount);
			pointsHandle.Free();
			if (!result)
				return null;

			if (polygonCount == 0)
				return null;

			var polygonSizes = new Int32[polygonCount];
			GCHandle	polygonSizesHandle	= GCHandle.Alloc(polygonSizes, GCHandleType.Pinned);
			IntPtr		polygonSizesPtr		= polygonSizesHandle.AddrOfPinnedObject();
			result = DecomposeGetSizes(polygonCount, polygonSizesPtr);
			polygonSizesHandle.Free();
			if (!result)
				return null;

			var polygons = new List<List<Vector2>>();
			for (int i = 0; i < polygonCount; i++)
			{
				var vertexCount = polygonSizes[i];
				var vertices	= new Vector2[vertexCount];
				GCHandle	verticesHandle	= GCHandle.Alloc(vertices, GCHandleType.Pinned);
				IntPtr		verticesPtr		= verticesHandle.AddrOfPinnedObject();
				result = DecomposeGetPolygon(i, vertexCount, verticesPtr);
				verticesHandle.Free();
				if (!result)
					return null;
				polygons.Add(new List<Vector2>(vertices));
			}

			return polygons;
		}

		#endregion

		
		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool SetDirty			(Int32				nodeID);

		#region Models C++ functions
		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool GenerateTree		(Int32				userID,
													 out Int32			generatedTreeNodeID);

		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool SetTreeEnabled	(Int32				modelNodeID, 
												     bool				isEnabled);



		[DllImport(NativePluginName, CallingConvention = CallingConvention.Cdecl)]
		private static extern bool RayCastMultiGet	(int				objectCount,
												     [Out] IntPtr		outputBrushIntersection);



		static int					__prevIntersectionCount = -1;
		static CSGTreeBrushIntersection[]	__outputIntersections;


		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern Int32 RayCastIntoTreeMultiCount(Int32				modelNodeID,
															   [In] ref Vector3		worldRayStart,
															   [In] ref Vector3		worldRayEnd,
															   [In] ref Matrix4x4	modelLocalToWorldMatrix,
															   int					in_filterLayerParameter0,
															   bool					ignoreInvisiblePolygons,
															   [In] IntPtr			ignoreNodeIDs,
															   Int32				ignoreNodeIDCount);
		private static bool RayCastIntoModelMulti(CSGModel					model,
												  Vector3					worldRayStart,
												  Vector3					worldRayEnd,
												  bool						ignoreInvisiblePolygons,
												  out LegacyBrushIntersection[]	intersections,
												  CSGBrush[]				ignoreBrushes = null)
		{
			var visibleLayers = Tools.visibleLayers;
            if (!ModelTraits.IsModelSelectable(model))
			{
				intersections = null;
				return false;
			}

			IntPtr		ignoreNodeIDsPtr	= IntPtr.Zero;
			GCHandle	ignoreNodeIDsHandle	= new GCHandle();
			
			if (ignoreBrushes != null)
			{
				var ignoreNodeIDsList = new List<Int32>(ignoreBrushes.Length);
				for (int i = ignoreBrushes.Length - 1; i >= 0; i--)
				{
					if (!ignoreBrushes[i])
						continue;
					if (ignoreBrushes[i].brushNodeID == CSGNode.InvalidNodeID)
						continue;
					ignoreNodeIDsList.Add(ignoreBrushes[i].brushNodeID);
				}
				
				var ignoreNodeIDs	= ignoreNodeIDsList.ToArray();
				ignoreNodeIDsHandle	= GCHandle.Alloc(ignoreNodeIDs, GCHandleType.Pinned);
				ignoreNodeIDsPtr	= ignoreNodeIDsHandle.AddrOfPinnedObject();
			}

			var modelLocalToWorldMatrix = model.transform.localToWorldMatrix;
			Int32 intersectionCount = RayCastIntoTreeMultiCount(model.modelNodeID, 
																 ref worldRayStart,
																 ref worldRayEnd,
																 ref modelLocalToWorldMatrix,
																 0,
																 ignoreInvisiblePolygons,
																 ignoreNodeIDsPtr,
																 (ignoreBrushes == null) ? 0 : ignoreBrushes.Length);

			if (ignoreNodeIDsHandle.IsAllocated)
				ignoreNodeIDsHandle.Free();

			if (intersectionCount == 0)
			{
				intersections = null;
				return false;
			}

			if (__prevIntersectionCount < intersectionCount)
			{
				__outputIntersections = new CSGTreeBrushIntersection[intersectionCount];
				__prevIntersectionCount = intersectionCount;
			}
			
			GCHandle	outputIntersectionsHandle	= GCHandle.Alloc(__outputIntersections, GCHandleType.Pinned);
			IntPtr		outputIntersectionsPtr		= outputIntersectionsHandle.AddrOfPinnedObject();

			var result = RayCastMultiGet(intersectionCount,
										 outputIntersectionsPtr);

			outputIntersectionsHandle.Free();

			if (!result)
			{
				intersections = null;
				return false;
			}
			
			var intersectionList = new List<LegacyBrushIntersection>();			
			for (int i = 0, t = 0; i < intersectionCount; i++, t+=3)
			{
				var obj					= EditorUtility.InstanceIDToObject(__outputIntersections[i].brushUserID);
				var monoBehaviour = obj as MonoBehaviour;
				if (monoBehaviour == null || !monoBehaviour)
					continue;

				var gameObject = monoBehaviour.gameObject;
				if (((1 << gameObject.layer) & visibleLayers) == 0)
					continue;

				intersectionList.Add(new LegacyBrushIntersection
				{
					gameObject			= gameObject,
					model				= model,
					brushNodeID			= __outputIntersections[i].brush.NodeID,
					surfaceIndex		= __outputIntersections[i].surfaceID,
					
					localPlane			= new CSGPlane(__outputIntersections[i].intersection.localPlane),
					modelPlane			= new CSGPlane(__outputIntersections[i].intersection.modelPlane),
					worldPlane			= new CSGPlane(__outputIntersections[i].intersection.worldPlane),
					worldIntersection	= __outputIntersections[i].intersection.worldIntersection,
					surfaceIntersection = __outputIntersections[i].intersection.surfaceIntersection,
					distance			= __outputIntersections[i].intersection.distance
				});
			}

			if (intersectionList.Count == 0)
			{
				intersections = null;
				return true;
			}

			intersections = intersectionList.ToArray();
			return true;
		}
		
		static readonly List<LegacyBrushIntersection> __intersectionList = new List<LegacyBrushIntersection>();
		private static bool RayCastMulti(int						modelCount,
										 CSGModel[]					models,
										 Vector3					rayStart,
										 Vector3					rayEnd,
										 bool						ignoreInvisiblePolygons,
										 out LegacyBrushIntersection[]	intersections,
										 CSGBrush[]					ignoreBrushes = null)
		{
			if (modelCount == 0)
			{
				intersections = null;
				return false;
			}

			IntPtr ignoreNodeIDsPtr = IntPtr.Zero;
			GCHandle ignoreNodeIDsHandle = new GCHandle();
			
			if (ignoreBrushes != null)
			{
				var ignoreNodeIDsList = new List<Int32>(ignoreBrushes.Length);
				for (int i = ignoreBrushes.Length - 1; i >= 0; i--)
				{
					if (!ignoreBrushes[i])
						continue;
					if (ignoreBrushes[i].brushNodeID == CSGNode.InvalidNodeID)
						continue;
					ignoreNodeIDsList.Add(ignoreBrushes[i].brushNodeID);
				}
				
				var ignoreNodeIDs	= ignoreNodeIDsList.ToArray();
				ignoreNodeIDsHandle = GCHandle.Alloc(ignoreNodeIDs, GCHandleType.Pinned);
				ignoreNodeIDsPtr	= ignoreNodeIDsHandle.AddrOfPinnedObject();
			}
			
			GCHandle outputIntersectionsHandle	= GCHandle.Alloc(__outputIntersections, GCHandleType.Pinned);

			var visibleLayers	 = Tools.visibleLayers;
			__intersectionList.Clear();
			for (int m = 0; m < modelCount; m++)
			{
				var model					= models[m];
				var modelLocalToWorldMatrix	= model.transform.localToWorldMatrix;
				Int32 intersectionCount		= RayCastIntoTreeMultiCount(model.modelNodeID, 
																		 ref rayStart,
																		 ref rayEnd,
																		 ref modelLocalToWorldMatrix,
																		 0,
																		 ignoreInvisiblePolygons,
																		 ignoreNodeIDsPtr,
																		 (ignoreBrushes == null) ? 0 : ignoreBrushes.Length);

				if (intersectionCount == 0)
					continue;

				if (__prevIntersectionCount < intersectionCount)
				{
					if (outputIntersectionsHandle.IsAllocated) 
						outputIntersectionsHandle.Free();
					__outputIntersections		= new CSGTreeBrushIntersection[intersectionCount];
					__prevIntersectionCount		= intersectionCount;
					outputIntersectionsHandle	= GCHandle.Alloc(__outputIntersections, GCHandleType.Pinned);
				}
			
				IntPtr	outputIntersectionsPtr = outputIntersectionsHandle.AddrOfPinnedObject();

				var result = RayCastMultiGet(intersectionCount,
											 outputIntersectionsPtr);
								
				if (!result)
					continue;
				
				for (int i = 0, t = 0; i < intersectionCount; i++, t+=3)
				{
					var obj					= EditorUtility.InstanceIDToObject(__outputIntersections[i].brushUserID);
					var monoBehaviour = obj as MonoBehaviour;
					if (monoBehaviour == null || !monoBehaviour)
						continue;

					var gameObject = monoBehaviour.gameObject;
					if (((1 << gameObject.layer) & visibleLayers) == 0)
						continue;
					
					__intersectionList.Add(new LegacyBrushIntersection
					{
						gameObject			= gameObject,
						model				= model,
						brushNodeID			= __outputIntersections[i].brush.NodeID,
						surfaceIndex		= __outputIntersections[i].surfaceID,
						
						localPlane			= new CSGPlane(__outputIntersections[i].intersection.localPlane),
						modelPlane			= new CSGPlane(__outputIntersections[i].intersection.modelPlane),
						worldPlane			= new CSGPlane(__outputIntersections[i].intersection.worldPlane),
						worldIntersection	= __outputIntersections[i].intersection.worldIntersection,
						surfaceIntersection = __outputIntersections[i].intersection.surfaceIntersection,
						distance			= __outputIntersections[i].intersection.distance,						
					});
				}
			}

			if (outputIntersectionsHandle.IsAllocated)
				outputIntersectionsHandle.Free();
			
			if (ignoreNodeIDsHandle.IsAllocated)
				ignoreNodeIDsHandle.Free();

			if (__intersectionList.Count == 0)
			{
				intersections = null;
				return false;
			}

			__intersectionList.Sort(delegate(LegacyBrushIntersection x, LegacyBrushIntersection y)
				{
					return (int)Mathf.Sign(y.distance - x.distance);
				}
			);

			intersections = __intersectionList.ToArray();
			return true;
		}
		

		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool RayCastIntoBrush(Int32				brushNodeID,
													[In]ref Vector3		rayStart,
													[In]ref Vector3		rayEnd,
													[In] ref Matrix4x4	modelLocalToWorldMatrix,
													bool				ignoreInvisiblePolygons,
													out CSGTreeBrushIntersection	outputBrushIntersection);

		private static bool RayCastIntoBrush(Int32					brushNodeID, 
											 Vector3				rayStart,
											 Vector3				rayEnd,
											 Matrix4x4				modelLocalToWorldMatrix,
											 out LegacyBrushIntersection	intersection,
											 bool					ignoreInvisiblePolygons)
		{
			if (brushNodeID == CSGNode.InvalidNodeID)
			{
				intersection = null;
				return false;
			}

			CSGTreeBrushIntersection outputBrushIntersection;
			if (!RayCastIntoBrush(brushNodeID,
								  ref rayStart,
								  ref rayEnd,
								  ref modelLocalToWorldMatrix,
								  ignoreInvisiblePolygons,
								  out outputBrushIntersection))
			{
				intersection = null;
				return false;
			}

			intersection = new LegacyBrushIntersection
			{
				gameObject			= null,
				model				= null,
				brushNodeID			= brushNodeID,
				surfaceIndex		= outputBrushIntersection.surfaceID,
				
				localPlane			= new CSGPlane(outputBrushIntersection.intersection.localPlane),
				modelPlane			= new CSGPlane(outputBrushIntersection.intersection.modelPlane),
				worldPlane			= new CSGPlane(outputBrushIntersection.intersection.worldPlane),
				worldIntersection	= outputBrushIntersection.intersection.worldIntersection,
				surfaceIntersection = outputBrushIntersection.intersection.surfaceIntersection				
			};
			return true;
		}




		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool RayCastIntoBrushSurface(Int32				brushNodeID, 
														   Int32				surfaceID,
														   [In]ref Vector3		rayStart,
														   [In]ref Vector3		rayEnd,
														   [In]ref Matrix4x4	modelLocalToWorldMatrix,
														   out  CSGSurfaceIntersection outputSurfaceIntersection);

		private static bool RayCastIntoBrushSurface(Int32							brushNodeID, 
													Int32							surfaceID, 
													Vector3							rayStart,
													Vector3							rayEnd,
													Matrix4x4						modelLocalToWorldMatrix,
													out LegacySurfaceIntersection	intersection)
		{
			if (brushNodeID == CSGNode.InvalidNodeID)
			{
				intersection = null;
				return false;
			}

			CSGSurfaceIntersection outputSurfaceIntersection;
			if (!RayCastIntoBrushSurface(brushNodeID,
										 surfaceID,
										 ref rayStart,
										 ref rayEnd,
										 ref modelLocalToWorldMatrix,
										 out outputSurfaceIntersection))
			{
				intersection = null;
				return false;
			}

			intersection = new LegacySurfaceIntersection
			{
				localPlane			= new CSGPlane(outputSurfaceIntersection.localPlane),
				modelPlane			= new CSGPlane(outputSurfaceIntersection.modelPlane),
				worldPlane			= new CSGPlane(outputSurfaceIntersection.worldPlane),
				surfaceIntersection = outputSurfaceIntersection.surfaceIntersection,
				worldIntersection	= outputSurfaceIntersection.worldIntersection,
				distance			= outputSurfaceIntersection.distance
			};
			return true;
		}
		
			
		
		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern int FindNodesInFrustum(Int32			modelNodeID, 
													 Int32			planeCount,
													 [In] IntPtr	planes);
		
		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool RetrieveUserIDsInFrustum(Int32			objectIDCount,
															[Out] IntPtr	objectIDs);

		private static bool GetItemsInFrustum(CSGModel				model, 
											  Plane[]				planes, 
											  HashSet<GameObject>	gameObjects)
		{
			var visibleLayers = Tools.visibleLayers;
            if (!ModelTraits.IsModelSelectable(model))
			{
				return false;
			}
						
			if (planes == null ||
				planes.Length != 6)
			{
				return false;
			}

			// TODO: should be full transformation
			var translated_planes = new Plane[planes.Length];
			for (int i = 0; i < planes.Length; i++)
			{
				var plane = planes[i];
				var normal = plane.normal;
				var d = plane.distance;
				var translation = model.transform.position;
				d += (normal.x * translation.x) +
					 (normal.y * translation.y) +
					 (normal.z * translation.z);
				translated_planes[i] = new Plane(normal, d);
			}

			GCHandle	planesHandle	= GCHandle.Alloc(translated_planes, GCHandleType.Pinned);
			IntPtr		planesPtr		= planesHandle.AddrOfPinnedObject();
			var itemCount = FindNodesInFrustum(model.modelNodeID, translated_planes.Length, planesPtr);
			planesHandle.Free();
			if (itemCount == 0)
			{
				return false;
			}

			var ids = new int[itemCount];
			GCHandle	idsHandle	= GCHandle.Alloc(ids, GCHandleType.Pinned);
			IntPtr		idsPtr		= idsHandle.AddrOfPinnedObject();
			var result = RetrieveUserIDsInFrustum(ids.Length, idsPtr);
			idsHandle.Free();
			if (!result)
			{
				return false;
			}

			bool found = false;
			for (int i = ids.Length - 1; i >= 0; i--)
			{
				var obj			= EditorUtility.InstanceIDToObject(ids[i]);
				var brush		= obj as MonoBehaviour;
				var gameObject	= (brush != null) ? brush.gameObject : null;
				if (gameObject == null)
					continue;
				
				gameObjects.Add(gameObject);
				found = true;
			}

			return found;
		}
		#endregion


		[DllImport(NativePluginName, CallingConvention = CallingConvention.Cdecl)]
		private static extern bool SetChildNodes(Int32 nodeID, Int32 childCount, [In] IntPtr childrenNodeIDs);

		private static bool SetChildNodes(Int32 nodeID, Int32 childCount, Int32[] childrenNodeIDs)
		{
			GCHandle childrenNodeIDsHandle = GCHandle.Alloc(childrenNodeIDs, GCHandleType.Pinned);
			IntPtr childrenNodeIDsPtr = childrenNodeIDsHandle.AddrOfPinnedObject();
			var result = SetChildNodes(nodeID, childCount, childrenNodeIDsPtr);
			childrenNodeIDsHandle.Free();
			return result;
		}


				
		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool DestroyNode(Int32			nodeID);
		
		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool DestroyNodes(Int32			nodeCount,
												 [In] IntPtr	nodeIDs);
		private static bool DestroyNodes(Int32 nodeCount,
										   Int32[] nodeIDs)
		{
			GCHandle	nodeIDsHandle	= GCHandle.Alloc(nodeIDs, GCHandleType.Pinned);
			IntPtr		nodeIDsPtr		= nodeIDsHandle.AddrOfPinnedObject();

			var result = DestroyNodes(nodeCount, nodeIDsPtr);

			nodeIDsHandle.Free();
			return result;
		}


		#region Operation C++ functions
		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool GenerateBranch(Int32		userID,
												  out Int32	generatedOperationNodeID);

		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool SetBranchOperationType(Int32				operationNodeID, 
															 CSGOperationType	operation);


		#endregion


		#region Brush C++ functions
		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool GenerateBrush(Int32		userID,
												 out Int32	generatedBrushNodeID);
		
		[DllImport(NativePluginName, CallingConvention = CallingConvention.Cdecl)]
		private static extern Int32 GetBrushMeshID(Int32 brushNodeID);

		[DllImport(NativePluginName, CallingConvention = CallingConvention.Cdecl)]
		private static extern bool SetBrushMeshID(Int32 brushNodeID, Int32 brushMeshIndex);


		[DllImport(NativePluginName, CallingConvention = CallingConvention.Cdecl)]
		private static extern CSGTreeBrushFlags GetBrushFlags(Int32 brushNodeID);

		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool  SetBrushFlags(Int32 brushNodeID, CSGTreeBrushFlags flags);

		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool SetBrushOperationType(Int32				brushNodeID,
														 CSGOperationType	operation);
		
		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern Int32 CreateBrushMesh(Int32		userID,
													Int32		vertexCount,
													[In] IntPtr	vertices,
													Int32		halfEdgeCount,
													[In] IntPtr	halfEdges,
													Int32		polygonCount,
													[In] IntPtr	polygons);

		private static Int32 CreateBrushMesh(Int32 userID, BrushMesh	brushMesh)
		{
			if (brushMesh == null) throw new ArgumentNullException("brushMesh");
			if (brushMesh.vertices == null || brushMesh.halfEdges == null || brushMesh.polygons == null) return CSGNode.InvalidNodeID;
			if (brushMesh.vertices.Length < 5 || brushMesh.halfEdges.Length < 16 || brushMesh.polygons.Length < 5) return CSGNode.InvalidNodeID;

			GCHandle	verticesHandle	= GCHandle.Alloc(brushMesh.vertices, GCHandleType.Pinned);
			GCHandle	halfEdgesHandle	= GCHandle.Alloc(brushMesh.halfEdges, GCHandleType.Pinned);
			GCHandle	polygonsHandle	= GCHandle.Alloc(brushMesh.polygons, GCHandleType.Pinned);
			IntPtr		verticesPtr		= verticesHandle.AddrOfPinnedObject();
			IntPtr		halfEdgesPtr	= halfEdgesHandle.AddrOfPinnedObject();
			IntPtr		polygonsPtr		= polygonsHandle.AddrOfPinnedObject();
			
			var result = CreateBrushMesh(userID,
										 brushMesh.vertices.Length,	 verticesPtr,
										 brushMesh.halfEdges.Length, halfEdgesPtr,
										 brushMesh.polygons.Length,	 polygonsPtr);
			
			polygonsHandle.Free();
			halfEdgesHandle.Free();
			verticesHandle.Free();
			return result;
		}

		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool UpdateBrushMesh(Int32		brushMeshIndex,
													Int32		vertexCount,
													[In] IntPtr	vertices,
													Int32		halfEdgeCount,
													[In] IntPtr	halfEdges,
													Int32		polygonCount,
													[In] IntPtr	polygons);
		private static bool UpdateBrushMesh(Int32		brushMeshIndex,
											BrushMesh	brushMesh)
		{
			if (brushMesh == null) throw new ArgumentNullException("brushMesh");
			if (brushMesh.vertices.Length < 5 || brushMesh.halfEdges.Length < 16 || brushMesh.polygons.Length < 5) return false;

			GCHandle	verticesHandle	= GCHandle.Alloc(brushMesh.vertices, GCHandleType.Pinned);
			GCHandle	halfEdgesHandle	= GCHandle.Alloc(brushMesh.halfEdges, GCHandleType.Pinned);
			GCHandle	polygonsHandle	= GCHandle.Alloc(brushMesh.polygons, GCHandleType.Pinned);
			IntPtr		verticesPtr		= verticesHandle.AddrOfPinnedObject();
			IntPtr		halfEdgesPtr	= halfEdgesHandle.AddrOfPinnedObject();
			IntPtr		polygonsPtr		= polygonsHandle.AddrOfPinnedObject();
			
			var result = UpdateBrushMesh(brushMeshIndex, 
										 brushMesh.vertices.Length,	 verticesPtr,
										 brushMesh.halfEdges.Length, halfEdgesPtr,
										 brushMesh.polygons.Length,	 polygonsPtr);
			
			polygonsHandle.Free();
			halfEdgesHandle.Free();
			verticesHandle.Free();
			return result;
		}
		
		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool DestroyBrushMesh(Int32	brushMeshIndex);

		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		public static extern bool GetBrushBounds   (Int32	 brushNodeID,
													out AABB bounds);

		[DllImport(NativePluginName, CallingConvention = CallingConvention.Cdecl)]
		private static extern bool SetNodeLocalTransformation(Int32 brushNodeID,
														[In]ref Matrix4x4 localToModelSpace);
		
		private static bool SetBrushToModelSpace(Int32 brushNodeID, 
												 Matrix4x4 localToModelSpace)
		{
			return SetNodeLocalTransformation(brushNodeID, ref localToModelSpace);
		}
		#endregion

		#region TexGen manipulation C++ functions

		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool GetSurfaceMinMaxTexCoords (Int32			brushNodeID,
															  Int32			surfaceID, 
															  Matrix4x4		modelLocalToWorldMatrix,
															  out Vector2	minTextureCoordinate, 		
															  out Vector2	maxTextureCoordinate);


		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool ConvertWorldToTextureCoord(Int32			brushNodeID,
															  Int32			surfaceID,
															  Matrix4x4		modelLocalToWorldMatrix,
															  Vector3		worldCoordinate, 
															  out Vector2	textureCoordinate);
		
		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool ConvertTextureToWorldCoord(Int32			brushNodeID,
															  Int32			surfaceID, 
															  float			textureCoordinateU, 
															  float			textureCoordinateV,
															  // workaround for mac-osx related bug
															  ref Matrix4x4 modelLocalToWorldMatrix,
															  ref float		worldCoordinateX, 
															  ref float		worldCoordinateY, 
															  ref float		worldCoordinateZ);
		#endregion

		#region C++ Mesh functions
		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool UpdateAllTreeMeshes();

		[DllImport(NativePluginName, CallingConvention = CallingConvention.Cdecl)]
		private static extern bool GenerateMeshDescriptions(Int32				modelNodeID, 
														    Int32				meshTypeCount,
														    [In]IntPtr			meshTypes,
														    VertexChannelFlags	vertexChannelMask,
														    out Int32			meshDescriptionCount);

		[DllImport(NativePluginName, CallingConvention = CallingConvention.Cdecl)]
		private static extern bool GetMeshDescriptions(Int32		modelNodeID, 
													   Int32        meshDescriptionCount,
													   [Out] IntPtr meshDescriptions);

		private static bool GetMeshDescriptions(Int32					modelNodeID, 	
												Foundation.MeshQuery[]	meshTypes,
												VertexChannelFlags		vertexChannelMask, 
												ref Foundation.GeneratedMeshDescription[] meshDescriptions)
		{
			Int32 meshDescriptionCount = 0;
		
			GCHandle	nativeMeshTypeHandle	= GCHandle.Alloc(meshTypes, GCHandleType.Pinned);
			IntPtr		nativeMeshTypePtr		= nativeMeshTypeHandle.AddrOfPinnedObject();
			
			var result = GenerateMeshDescriptions(modelNodeID, meshTypes.Length, nativeMeshTypePtr, vertexChannelMask, out meshDescriptionCount);
			
			nativeMeshTypeHandle.Free();
			if (!result)
				return false;
			
			if (meshDescriptionCount != meshDescriptions.Length)
				meshDescriptions = new Foundation.GeneratedMeshDescription[meshDescriptionCount];

            if (meshDescriptionCount == 0)
                return true;

			var meshDescriptionsHandle	= GCHandle.Alloc(meshDescriptions, GCHandleType.Pinned);
			var meshDescriptionsPtr		= meshDescriptionsHandle.AddrOfPinnedObject();
			
			result = GetMeshDescriptions(modelNodeID, meshDescriptionCount, meshDescriptionsPtr);
			
			meshDescriptionsHandle.Free();
			return result;
		}
		
		private static bool GetMeshDescriptions(CSGModel model, ref Foundation.GeneratedMeshDescription[] meshDescriptions)
		{
			return GetMeshDescriptions(model.modelNodeID, InternalCSGModelManager.GetMeshTypesForModel(model), model.VertexChannels, ref meshDescriptions);
		}
		
		[DllImport(NativePluginName, CallingConvention = CallingConvention.Cdecl)]
		private static extern bool GetGeneratedMesh(Int32			modelNodeID, 
													Int32			meshIndex,
													Int32			subMeshIndex,

													Int32			indexCount,
													[Out] IntPtr	indices,

													Int32			vertexCount,
													[Out] IntPtr	positions,
													[Out] IntPtr	tangents,
													[Out] IntPtr	normals,
//												    [Out] IntPtr	colors,
													[Out] IntPtr	uvs,
													out Vector3		boundsCenter,
													out Vector3		boundsSize);	// singular value
		
		private static bool GetModelMesh(int		modelNodeID,
										 int		meshIndex,
										 int		subMeshIndex,
										 
										 int		indexCount,
										 Int32[]	indices,

										 int		vertexCount,
										 Vector3[]	positions,
										 Vector4[]	tangents,
										 Vector3[]	normals,
//										 Color[]	colors,
										 Vector2[]	uv0,
										 out Vector3 boundsCenter,
										 out Vector3 boundsSize)
		{
			if ((indices   == null || indexCount  > indices  .Length) || // may not be null
				(positions == null || vertexCount > positions.Length) || // may not be null
				(tangents  != null && vertexCount > tangents .Length) || // may be null
				(normals   != null && vertexCount > normals  .Length) || // may be null
//				(colors    != null && vertexCount > colors   .Length) || // may be null
				(uv0       != null && vertexCount > uv0      .Length))   // may be null
			{
				boundsCenter = Vector3.zero;
				boundsSize = Vector3.zero;
				return false;
			}
			
			GCHandle indicesHandle		= GCHandle.Alloc(indices, GCHandleType.Pinned);
			GCHandle positionHandle		= GCHandle.Alloc(positions, GCHandleType.Pinned);
			GCHandle tangentHandle		= new GCHandle();
			GCHandle normalHandle		= new GCHandle();
//			GCHandle colorHandle		= new GCHandle();
			GCHandle uv0Handle			= new GCHandle();
			
			IntPtr	indicesPtr	= indicesHandle.AddrOfPinnedObject();
			IntPtr	positionPtr	= positionHandle.AddrOfPinnedObject();
			IntPtr	tangentPtr	= IntPtr.Zero;
			IntPtr	normalPtr	= IntPtr.Zero;
//			IntPtr	colorPtr	= IntPtr.Zero;
			IntPtr	uv0Ptr		= IntPtr.Zero;

			if (tangents	!= null) { tangentHandle	= GCHandle.Alloc(tangents,	GCHandleType.Pinned); tangentPtr  = tangentHandle.AddrOfPinnedObject(); }
			if (normals		!= null) { normalHandle		= GCHandle.Alloc(normals,	GCHandleType.Pinned); normalPtr   = normalHandle.AddrOfPinnedObject(); }
//			if (colors		!= null) { colorHandle		= GCHandle.Alloc(colors,	GCHandleType.Pinned); colorPtr	  = colorHandle.AddrOfPinnedObject(); }
			if (uv0			!= null) { uv0Handle		= GCHandle.Alloc(uv0,		GCHandleType.Pinned); uv0Ptr	  = uv0Handle.AddrOfPinnedObject(); }
			
			var result = GetGeneratedMesh((Int32)modelNodeID,
									  (Int32)meshIndex,
									  (Int32)subMeshIndex,
									  (Int32)indexCount,
									  indicesPtr,
									  (Int32)vertexCount,
									  positionPtr,
									  tangentPtr,
									  normalPtr,
									  //colorPtr,
									  uv0Ptr,
									  out boundsCenter,
									  out boundsSize);
			
			if (uv0			!= null) { uv0Handle	 .Free(); }
//			if (colors		!= null) { colorHandle	 .Free(); }
			if (normals		!= null) { normalHandle	 .Free(); }
			if (tangents	!= null) { tangentHandle .Free(); }
			positionHandle.Free(); 
			indicesHandle.Free();
			return result;
		}
		
		
		static GeneratedMeshContents GetModelMesh(int modelNodeID, GeneratedMeshDescription	meshDescription)
		{
			var generatedMesh = new GeneratedMeshContents();
			// create our arrays on the C# side with the correct size
			var usedVertexChannels	= meshDescription.meshQuery.UsedVertexChannels;
			var vertexCount			= meshDescription.vertexCount;
			var indexCount			= meshDescription.indexCount;
			var meshIndex			= meshDescription.meshQueryIndex;
			var subMeshIndex		= meshDescription.subMeshQueryIndex;

			generatedMesh.description	= meshDescription;
			generatedMesh.tangents		= ((usedVertexChannels & VertexChannelFlags.Tangent) != 0) ? new Vector4[vertexCount] : null;
			generatedMesh.normals		= ((usedVertexChannels & VertexChannelFlags.Normal ) != 0) ? new Vector3[vertexCount] : null;
			generatedMesh.uv0			= ((usedVertexChannels & VertexChannelFlags.UV0    ) != 0) ? new Vector2[vertexCount] : null;
//			generatedMesh.colors		= ((usedVertexChannels & VertexChannelFlags.Color  ) != 0) ? new Color  [vertexCount] : null;
			generatedMesh.positions		= new Vector3[vertexCount];
			generatedMesh.indices		= new int[indexCount];

			var boundsCenter = Vector3.zero;
			var boundsSize = Vector3.zero;
			if (!GetModelMesh(modelNodeID,
							  meshIndex,
							  subMeshIndex,

							  indexCount,
							  generatedMesh.indices,

							  vertexCount,
							  generatedMesh.positions,
							  generatedMesh.tangents,
							  generatedMesh.normals,
//							  generatedMesh.colors,
							  generatedMesh.uv0,
							  out boundsCenter,
							  out boundsSize))
				return null;
			generatedMesh.bounds = new Bounds(boundsCenter, boundsSize);
			return generatedMesh;
		}

		static bool GetModelMeshNoAlloc(int modelNodeID, GeneratedMeshDescription	meshDescription, ref GeneratedMeshContents generatedMesh)
		{
			// create our arrays on the C# side with the correct size
			var usedVertexChannels	= meshDescription.meshQuery.UsedVertexChannels;
			var vertexCount			= meshDescription.vertexCount;
			var indexCount			= meshDescription.indexCount;
			var meshIndex			= meshDescription.meshQueryIndex;
			var subMeshIndex		= meshDescription.subMeshQueryIndex;
			
			var tangents		= ((usedVertexChannels & VertexChannelFlags.Tangent) != 0) ? generatedMesh.tangents : null;
			var normals			= ((usedVertexChannels & VertexChannelFlags.Normal ) != 0) ? generatedMesh.normals  : null;
			var uv0				= ((usedVertexChannels & VertexChannelFlags.UV0    ) != 0) ? generatedMesh.uv0      : null;
//			var colors			= ((usedVertexChannels & VertexChannelFlags.Color  ) != 0) ? generatedMesh.colors   : null;
			var positions		= generatedMesh.positions;
			var indices			= generatedMesh.indices;
			
			var boundsCenter	= Vector3.zero;
			var boundsSize		= Vector3.zero;
			
			if (!GetModelMesh(modelNodeID,
							  meshIndex,
							  subMeshIndex,

							  indexCount,
							  indices,

							  vertexCount,
							  positions,
							  tangents,
							  normals,
//							  colors,
							  uv0,
							  out boundsCenter,
							  out boundsSize))
				return false;
			generatedMesh.description	= meshDescription;
			generatedMesh.bounds		= new Bounds(boundsCenter, boundsSize);
			return true;
		}

		#endregion



		#region Outline C++ functions
		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern UInt64 GetBrushOutlineGeneration(Int32 brushNodeID);

		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool GetBrushOutlineSizes(Int32		brushNodeID,
														out Int32	vertexCount,
														out Int32	visibleOuterLineCount,
														out Int32	visibleInnerLineCount,
														out Int32	invisibleOuterLineCount,
														out Int32	invisibleInnerLineCount,
														out Int32	invalidLineCount);

		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool GetBrushOutlineValues(Int32			brushNodeID,
														 Int32			vertexCount,
														 [Out] IntPtr	vertices,
														 Int32			visibleOuterLineCount,
														 [Out] IntPtr	visibleOuterLines,
														 Int32			visibleInnerLineCount,
														 [Out] IntPtr	visibleInnerLines,
														 Int32			invisibleOuterLineCount,
														 [Out] IntPtr	invisibleOuteLines,
														 Int32			invisibleInnerLineCount,
														 [Out] IntPtr	invisibleInnerLines,
														 Int32			invalidLineCount,
														 [Out] IntPtr	invalidLines);
		
		private static bool GetBrushOutline(Int32					brushNodeID,
											out GeometryWireframe	outline)
		{
			int vertexCount = 0;
			int visibleOuterLineCount = 0;
			int visibleInnerLineCount = 0;
			int invisibleOuterLineCount = 0;
			int invisibleInnerLineCount = 0;
			int invalidLineCount = 0;
			if (!GetBrushOutlineSizes(brushNodeID,
									  out vertexCount,
									  out visibleOuterLineCount,
									  out visibleInnerLineCount,
									  out invisibleOuterLineCount,
									  out invisibleInnerLineCount,
									  out invalidLineCount))
			{
				outline = null;
				return false;
			}
			
			if (vertexCount == 0 ||
				(visibleOuterLineCount == 0 && invisibleOuterLineCount == 0 && 
				 visibleInnerLineCount == 0 && invisibleInnerLineCount == 0 && 
				 invalidLineCount == 0))
			{
				outline = null;
				return false;
			}
			
			outline = new GeometryWireframe();
			outline.vertices = null;
			outline.visibleOuterLines = null;
			outline.visibleInnerLines = null;
			outline.invisibleOuterLines = null;
			outline.invisibleInnerLines = null;
			outline.invalidLines = null;

			if (outline.vertices == null || outline.vertices.Length != vertexCount)
			{
				outline.vertices = new Vector3[vertexCount];
			}

			if (visibleOuterLineCount > 0 &&
				(outline.visibleOuterLines == null || outline.visibleOuterLines.Length != visibleOuterLineCount))
			{
				outline.visibleOuterLines = new Int32[visibleOuterLineCount];
			}

			if (visibleInnerLineCount > 0 &&
				(outline.visibleInnerLines == null || outline.visibleInnerLines.Length != visibleInnerLineCount))
			{
				outline.visibleInnerLines = new Int32[visibleInnerLineCount];
			}

			if (invisibleOuterLineCount > 0 &&
				(outline.invisibleOuterLines == null || outline.invisibleOuterLines.Length != invisibleOuterLineCount))
			{
				outline.invisibleOuterLines = new Int32[invisibleOuterLineCount];
			}

			if (invisibleInnerLineCount > 0 &&
				(outline.invisibleInnerLines == null || outline.invisibleInnerLines.Length != invisibleInnerLineCount))
			{
				outline.invisibleInnerLines = new Int32[invisibleInnerLineCount];
			}

			if (invalidLineCount > 0 &&
				(outline.invalidLines == null || outline.invalidLines.Length != invalidLineCount))
			{
			   outline.invalidLines = new Int32[invalidLineCount];
			}

			GCHandle verticesHandle				= GCHandle.Alloc(outline.vertices, GCHandleType.Pinned);
			GCHandle visibleOuterLinesHandle	= GCHandle.Alloc(outline.visibleOuterLines, GCHandleType.Pinned);
			GCHandle visibleInnerLinesHandle	= GCHandle.Alloc(outline.visibleInnerLines, GCHandleType.Pinned);
			GCHandle invisibleOuterLinesHandle	= GCHandle.Alloc(outline.invisibleOuterLines, GCHandleType.Pinned);
			GCHandle invisibleInnerLinesHandle	= GCHandle.Alloc(outline.invisibleInnerLines, GCHandleType.Pinned);
			GCHandle invalidLinesHandle			= GCHandle.Alloc(outline.invalidLines, GCHandleType.Pinned);

			IntPtr verticesPtr				= verticesHandle.AddrOfPinnedObject();
			IntPtr visibleOuterLinesPtr		= visibleOuterLinesHandle.AddrOfPinnedObject();
			IntPtr visibleInnerLinesPtr		= visibleInnerLinesHandle.AddrOfPinnedObject();
			IntPtr invisibleOuterLinesPtr	= invisibleOuterLinesHandle.AddrOfPinnedObject();
			IntPtr invisibleInnerLinesPtr	= invisibleInnerLinesHandle.AddrOfPinnedObject();
			IntPtr invalidLinesPtr			= invalidLinesHandle.AddrOfPinnedObject();
			
			if (!GetBrushOutlineValues(brushNodeID,
									   vertexCount,
									   verticesPtr,
									   visibleOuterLineCount,
									   visibleOuterLinesPtr,
									   visibleInnerLineCount,
									   visibleInnerLinesPtr,
									   invisibleOuterLineCount,
									   invisibleOuterLinesPtr,
									   invisibleInnerLineCount,
									   invisibleInnerLinesPtr,
									   invalidLineCount,
									   invalidLinesPtr))
				return false;

			verticesHandle.Free();
			visibleOuterLinesHandle.Free();
			visibleInnerLinesHandle.Free();
			invisibleOuterLinesHandle.Free();
			invisibleInnerLinesHandle.Free();
			invalidLinesHandle.Free();
			return true;
		}


		
		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool GetSurfaceOutlineSizes(Int32			brushNodeID,
														  Int32			surfaceID,
														  out Int32		vertexCount,
														  out Int32		visibleOuterLineCount,
														  out Int32		visibleInnerLineCount,
														  out Int32		visibleTriangleCount,
														  out Int32		invisibleOuterLineCount,
														  out Int32		invisibleInnerLineCount,
														  out Int32		invalidLineCount);
		[DllImport(NativePluginName, CallingConvention=CallingConvention.Cdecl)]
		private static extern bool GetSurfaceOutlineValues(Int32		brushNodeID,
														  Int32			surfaceID,
														  Int32			vertexCount,
														  [Out] IntPtr	vertices,
														  Int32			visibleOuterLineCount,
														  [Out] IntPtr	visibleOuterLines,
														  Int32			visibleInnerLineCount,
														  [Out] IntPtr	visibleInnerLines,
														  Int32			visibleTriangleCount,
														  [Out] IntPtr	visibleTriangles,
														  Int32			invisibleOuterLineCount,
														  [Out] IntPtr	invisibleOuterLines,
														  Int32			invisibleInnerLineCount,
														  [Out] IntPtr	invisibleInnerLines,
														  Int32			invalidLineCount,
														  [Out] IntPtr	invalidLines);
		
		private static bool GetSurfaceOutline(Int32			brushNodeID,
											  Int32			surfaceID,
											  out GeometryWireframe outline)
		{
			int vertexCount = 0;
			int visibleOuterLineCount = 0;
			int visibleInnerLineCount = 0;
			int visibleTriangleCount = 0;
			int invisibleOuterLineCount = 0;
			int invisibleInnerLineCount = 0;
			int invalidLineCount = 0;
			if (!GetSurfaceOutlineSizes(brushNodeID,
										surfaceID,
										out vertexCount,
										out visibleOuterLineCount,
										out visibleInnerLineCount,
										out visibleTriangleCount,
										out invisibleOuterLineCount,
										out invisibleInnerLineCount,
										out invalidLineCount))
			{
				outline = null;
				return false;
			}
			
			if (vertexCount == 0 ||
				(visibleOuterLineCount == 0 && invisibleOuterLineCount == 0 && 
				 visibleInnerLineCount == 0 && invisibleInnerLineCount == 0 &&
				 visibleTriangleCount == 0 &&
				invalidLineCount == 0))
			{
				vertexCount = 0;
				visibleOuterLineCount = 0;
				visibleInnerLineCount = 0;
				visibleTriangleCount = 0;
				invisibleOuterLineCount = 0;
				invisibleInnerLineCount = 0;
				invalidLineCount = 0;
				outline = null;
				return false;
			}
			
			outline = new GeometryWireframe();
			if (outline.vertices == null || outline.vertices.Length != vertexCount)
			{
				outline.vertices = new Vector3[vertexCount];
			}
			

			if (visibleOuterLineCount > 0 &&
				(outline.visibleOuterLines == null || outline.visibleOuterLines.Length != visibleOuterLineCount))
			{
				outline.visibleOuterLines = new Int32[visibleOuterLineCount];
			}

			if (visibleInnerLineCount > 0 &&
				(outline.visibleInnerLines == null || outline.visibleInnerLines.Length != visibleInnerLineCount))
			{
				outline.visibleInnerLines = new Int32[visibleInnerLineCount];
			}

			if (visibleTriangleCount > 0 &&
				(outline.visibleTriangles == null || outline.visibleTriangles.Length != visibleTriangleCount))
			{
				outline.visibleTriangles = new Int32[visibleTriangleCount];
			}

			if (invisibleOuterLineCount > 0 &&
				(outline.invisibleOuterLines == null || outline.invisibleOuterLines.Length != invisibleOuterLineCount))
			{
				outline.invisibleOuterLines = new Int32[invisibleOuterLineCount];
			}

			if (invisibleInnerLineCount > 0 &&
				(outline.invisibleInnerLines == null || outline.invisibleInnerLines.Length != invisibleInnerLineCount))
			{
				outline.invisibleInnerLines = new Int32[invisibleInnerLineCount];
			}

			if (invalidLineCount > 0 &&
				(outline.invalidLines == null || outline.invalidLines.Length != invalidLineCount))
			{
			   outline.invalidLines = new Int32[invalidLineCount];
			}

			GCHandle verticesHandle				= GCHandle.Alloc(outline.vertices, GCHandleType.Pinned);
			GCHandle visibleOuterLinesHandle	= GCHandle.Alloc(outline.visibleOuterLines, GCHandleType.Pinned);
			GCHandle visibleInnerLinesHandle	= GCHandle.Alloc(outline.visibleInnerLines, GCHandleType.Pinned);
			GCHandle visibleTrianglesHandle		= GCHandle.Alloc(outline.visibleTriangles, GCHandleType.Pinned);
			GCHandle invisibleOuterLinesHandle	= GCHandle.Alloc(outline.invisibleOuterLines, GCHandleType.Pinned);
			GCHandle invisibleInnerLinesHandle	= GCHandle.Alloc(outline.invisibleInnerLines, GCHandleType.Pinned);
			GCHandle invalidLinesHandle			= GCHandle.Alloc(outline.invalidLines, GCHandleType.Pinned);

			IntPtr verticesPtr				= verticesHandle.AddrOfPinnedObject();
			IntPtr visibleOuterLinesPtr		= visibleOuterLinesHandle.AddrOfPinnedObject();
			IntPtr visibleInnerLinesPtr		= visibleInnerLinesHandle.AddrOfPinnedObject();
			IntPtr visibleTrianglesPtr		= visibleTrianglesHandle.AddrOfPinnedObject();
			IntPtr invisibleOuterLinesPtr	= invisibleOuterLinesHandle.AddrOfPinnedObject();
			IntPtr invisibleInnerLinesPtr	= invisibleInnerLinesHandle.AddrOfPinnedObject();
			IntPtr invalidLinesPtr			= invalidLinesHandle.AddrOfPinnedObject();

			if (!GetSurfaceOutlineValues(brushNodeID,
										 surfaceID,
										 vertexCount,
										 verticesPtr,
										 visibleOuterLineCount,
										 visibleOuterLinesPtr,
										 visibleInnerLineCount,
										 visibleInnerLinesPtr,
										 visibleTriangleCount,
										 visibleTrianglesPtr,
										 invisibleOuterLineCount,
										 invisibleOuterLinesPtr,
										 invisibleInnerLineCount,
										 invisibleInnerLinesPtr,
										 invalidLineCount,
										 invalidLinesPtr))
			{
				return false;
			}

			verticesHandle.Free();
			visibleOuterLinesHandle.Free();
			visibleInnerLinesHandle.Free();
			visibleTrianglesHandle.Free();
			invisibleOuterLinesHandle.Free();
			invisibleInnerLinesHandle.Free();
			invalidLinesHandle.Free();
			return true;
		}
#endregion


		public static void RegisterExternalMethods()
		{
			var methods = new NativeMethods();
			methods.ResetCSG    				= ClearAllNodes;

			methods.ConvexPartition				= ConvexPartition;

			methods.SetDirty					= SetDirty;
			methods.SetChildNodes				= SetChildNodes;
			methods.DestroyNode					= DestroyNode;
			methods.DestroyNodes				= delegate (Int32[] nodeIDs) { return DestroyNodes(nodeIDs.Length, nodeIDs); };

			methods.GenerateModel				= GenerateTree;
			methods.SetModelEnabled				= SetTreeEnabled;

			methods.RayCastMulti				= RayCastMulti;
			methods.RayCastIntoModelMulti		= RayCastIntoModelMulti;
			methods.RayCastIntoBrush			= RayCastIntoBrush;
			methods.RayCastIntoBrushSurface		= RayCastIntoBrushSurface;
			methods.GetItemsInFrustum			= GetItemsInFrustum;
			

			methods.GenerateOperation			= GenerateBranch;
			methods.SetOperationOperationType	= SetBranchOperationType;
			
			methods.GenerateBrush				= GenerateBrush;
			methods.GetBrushMeshID				= GetBrushMeshID;
			methods.SetBrushMeshID				= SetBrushMeshID;
			methods.GetBrushFlags				= GetBrushFlags;
			methods.SetBrushFlags				= SetBrushFlags;
			methods.SetBrushOperationType		= SetBrushOperationType;
			methods.SetBrushToModelSpace		= SetBrushToModelSpace;

		
			methods.CreateBrushMesh				= CreateBrushMesh;
			methods.UpdateBrushMesh				= UpdateBrushMesh;
			methods.DestroyBrushMesh			= DestroyBrushMesh;

			
//			methods.FitSurface					= FitSurface;
//			methods.FitSurfaceX					= FitSurfaceX;
//			methods.FitSurfaceY					= FitSurfaceY;
			methods.GetSurfaceMinMaxTexCoords	= GetSurfaceMinMaxTexCoords;
			methods.ConvertModelToTextureSpace	= ConvertWorldToTextureCoord;
			methods.ConvertTextureToModelSpace	= ConvertTextureToWorldCoord;

			methods.GetBrushOutlineGeneration	= GetBrushOutlineGeneration;
			methods.GetBrushOutline				= GetBrushOutline;
			methods.GetSurfaceOutline			= GetSurfaceOutline;

			methods.UpdateAllModelMeshes		= UpdateAllTreeMeshes;
			methods.GetMeshDescriptions			= GetMeshDescriptions;
			
			methods.GetModelMesh				= GetModelMesh;
			methods.GetModelMeshNoAlloc			= GetModelMeshNoAlloc;

			InternalCSGModelManager.External = methods;
		}

		public static void ClearExternalMethods()
		{
			InternalCSGModelManager.External = null;
		}
#endregion
	}
}
#endif