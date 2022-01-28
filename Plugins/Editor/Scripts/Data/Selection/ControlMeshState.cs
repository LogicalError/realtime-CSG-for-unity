using System;
using UnityEngine;
using InternalRealtimeCSG;
using UnityEditor;
using RealtimeCSG.Legacy;
using RealtimeCSG.Components;

namespace RealtimeCSG
{
	[Flags]
	[Serializable]
	internal enum SelectState : byte
	{
		None		= 0,
		Hovering	= 1,
		Selected	= 2
	}

	internal sealed class SceneMeshState
	{
		public Camera	    Camera;
		public bool[]       WorldPointBackfaced;
		public float[]		WorldPointSizes;                // render
		public float[]		PolygonCenterPointSizes;        // render        
		public Color[]		WorldPointColors;               // render
	}
	
	// TODO: simplify this

	[Serializable]
	internal partial class ControlMeshState
	{
		[SerializeField] public Transform		BrushTransform;
		[SerializeField] public Transform		ModelTransform;
//		[SerializeField] public Transform		ParentTransform;


		// backup stuff, used while editing

		[SerializeField] public Vector3[]		BackupPoints;
		[SerializeField] public Vector3[]       BackupPolygonCenterPoints;
		[SerializeField] public CSGPlane[]      BackupPolygonCenterPlanes;


		// ids

		[SerializeField] public int[]			PointControlId; 
		[SerializeField] public int[]			EdgeControlId;
		[SerializeField] public int[]			PolygonControlId;

		
		// geometry helpers

		[SerializeField] public Vector3[]		WorldPoints;
		
		[SerializeField] public int[]			Edges;
		[SerializeField] public int[]			EdgeStateToHalfEdge;	 
		[SerializeField] public int[]			HalfEdgeToEdgeStates;           // indicesToEdges[edge-index] = index into state 'edges'
		[SerializeField] public int[]			EdgeSurfaces;
		
		[SerializeField] public Vector3[]       PolygonCenterPoints;
		[SerializeField] public CSGPlane[]      PolygonCenterPlanes;
		[SerializeField] public int[][]         PolygonPointIndices;        


		// rendering helpers

		[SerializeField] public Color[]         EdgeColors;                     // render
		
		[SerializeField] public Color[]         PolygonColors;                  // render
		[SerializeField] public Color[]         PolygonCenterColors;            // render

		[SerializeField] public Vector3         BrushCenter;                    // render

		SceneMeshState[] CameraStates = new SceneMeshState[0];		            // render
		

		public ControlMeshState(CSGBrush brush)
		{
			UpdateTransforms(brush);

			var controlMesh	= brush.ControlMesh;
			if (controlMesh == null)
			{
				brush.ControlMesh = new ControlMesh();
				controlMesh = brush.ControlMesh;
			}

			if (controlMesh.Vertices == null) controlMesh.Vertices = new Vector3 [0];
			if (controlMesh.Edges	 == null) controlMesh.Edges    = new HalfEdge[0];
			if (controlMesh.Polygons == null) controlMesh.Polygons = new Polygon [0];

			AllocatePoints(controlMesh.Vertices.Length);
			AllocateEdges(controlMesh.Edges.Length);
			AllocatePolygons(controlMesh.Polygons.Length);
		}

		private void AllocatePoints(int pointCount)
		{
			PointControlId		= new int[pointCount];
			Selection.Points	= new SelectState[pointCount];
			WorldPoints			= new Vector3[pointCount];
		}

		private void AllocateEdges(int edgeCount)
		{
			Edges					= new int[edgeCount];
			EdgeSurfaces			= new int[edgeCount];
			HalfEdgeToEdgeStates	= new int[edgeCount];
			EdgeStateToHalfEdge		= new int[edgeCount / 2];
			EdgeColors				= new Color[edgeCount / 2];
			Selection.Edges			= new SelectState[edgeCount / 2];
			EdgeControlId			= new int[edgeCount / 2];
		}

		private void AllocatePolygons(int polygonCount)
		{
			PolygonControlId			= new int[polygonCount];
			Selection.Polygons			= new SelectState[polygonCount];
			PolygonCenterPoints			= new Vector3[polygonCount];
			PolygonColors			    = new Color[polygonCount];
			PolygonCenterColors			= new Color[polygonCount * 2];
			PolygonCenterPlanes			= new CSGPlane[polygonCount];
			PolygonPointIndices			= new int[polygonCount][];
		}

		public SceneMeshState GetCameraState(Camera camera, bool generate)
		{
			if (!camera)
				return null;

			if (CameraStates != null)
			{
				for (int i = CameraStates.Length - 1; i >= 0; i--)
				{
					var sceneMeshState = CameraStates[i];
					if (sceneMeshState == null ||
						System.Object.ReferenceEquals(sceneMeshState.Camera, null) ||
						!sceneMeshState.Camera)
					{
						ArrayUtility.RemoveAt(ref CameraStates, i);
						continue;
					}
					if (CameraStates[i].Camera == camera)
						return CameraStates[i];
				}
			}

			if (!generate)
				return null;

			int index;
			if (CameraStates != null)
			{
				index = CameraStates.Length;
				Array.Resize(ref CameraStates, index + 1);
			} else
			{
				CameraStates = new SceneMeshState[1];
				index = 0;
			}

			CameraStates[index] = new SceneMeshState();
			CameraStates[index].Camera				    = camera;
			CameraStates[index].WorldPointSizes			= new float[PointControlId.Length];
			CameraStates[index].WorldPointBackfaced		= new bool[PointControlId.Length];
			CameraStates[index].WorldPointColors		= new Color[PointControlId.Length * 2];
			CameraStates[index].PolygonCenterPointSizes	= new float[PolygonCenterPoints.Length];
			return CameraStates[index];
		}

		public void UpdateTransforms(CSGBrush brush)
		{
			BrushTransform	= brush.GetComponent<Transform>();
			ModelTransform	= InternalCSGModelManager.FindModelTransform(BrushTransform);
//			ParentTransform = InternalCSGModelManager.FindParentTransform(BrushTransform);
		}

		public void UpdateMesh(ControlMesh controlMesh, Vector3[] vertices = null)
		{
			if (controlMesh == null ||
				controlMesh.Vertices == null ||
				controlMesh.Edges == null ||
				controlMesh.Polygons == null ||
				PolygonPointIndices == null)
			{
				return;
			}

			if (vertices == null)
				vertices = controlMesh.Vertices;

			if (!BrushTransform)
				return;

			var pointCount = vertices.Length;
			if (WorldPoints.Length != pointCount)
				AllocatePoints(pointCount);

			var edgeCount = controlMesh.Edges.Length;
			if (Edges.Length != edgeCount)
				AllocateEdges(edgeCount);

			var polygonCount = controlMesh.Polygons.Length;
			if (PolygonControlId.Length != polygonCount)
				AllocatePolygons(polygonCount);

			var index = 0;
			for (var e = 0; e < edgeCount; e++)
			{
				if (e >= controlMesh.Edges.Length ||
					index >= Edges.Length)
					continue;

				var twin = controlMesh.Edges[e].TwinIndex;
				if (twin < e || // if it's less than e then we've already handled our twin
					twin >= controlMesh.Edges.Length)
					continue;

				var polygonIndex = controlMesh.Edges[e].PolygonIndex;
				if (polygonIndex < 0 || polygonIndex >= controlMesh.Polygons.Length)
					continue;

				var twinPolygonIndex = controlMesh.Edges[twin].PolygonIndex;
				if (twinPolygonIndex < 0 || twinPolygonIndex >= controlMesh.Polygons.Length)
					continue;

				var vertexIndex1 = controlMesh.Edges[e   ].VertexIndex;
				var vertexIndex2 = controlMesh.Edges[twin].VertexIndex;

				if (vertexIndex1 < 0 || vertexIndex1 >= Selection.Points.Length ||
					vertexIndex2 < 0 || vertexIndex2 >= Selection.Points.Length)
					continue;

				Edges[index    ] = vertexIndex1;
				Edges[index + 1] = vertexIndex2;
				EdgeStateToHalfEdge[index / 2] = e;
				HalfEdgeToEdgeStates[e] = index;
				HalfEdgeToEdgeStates[twin] = index;
				EdgeSurfaces[index    ] = polygonIndex;
				EdgeSurfaces[index + 1] = twinPolygonIndex;
				/*
				if ((Selection.Points[vertexIndex1] & SelectState.Selected) == SelectState.Selected &&
					(Selection.Points[vertexIndex2] & SelectState.Selected) == SelectState.Selected)
					Selection.Edges[index / 2] |= SelectState.Selected;
				else
					Selection.Edges[index / 2] &= ~SelectState.Selected;
				*/
				//edgeSelectState[index / 2] = SelectState.None;
				index += 2;
			}

			var polygonCountModified = false;
			while (polygonCount > PolygonPointIndices.Length)
			{
				ArrayUtility.Add(ref PolygonPointIndices, null);
				polygonCountModified = true;
			}
			while (polygonCount < PolygonPointIndices.Length)
			{
				ArrayUtility.RemoveAt(ref PolygonPointIndices, PolygonPointIndices.Length - 1);
				polygonCountModified = true;
			}

			if (polygonCountModified)
			{
				for (var i = 0; i < polygonCount; i++)
				{
					PolygonPointIndices[i] = null;
				}
			}

			UpdatePoints(controlMesh, vertices);
		}

		public void UpdatePoints(ControlMesh controlMesh, Vector3[] vertices = null)
		{
			if (controlMesh == null ||
				controlMesh.Vertices == null ||
				controlMesh.Edges == null ||
				controlMesh.Polygons == null ||
				PolygonPointIndices == null)
			{
				return;
			}

			if (vertices == null)
				vertices = controlMesh.Vertices;

			if (!BrushTransform)
				return;
			

			var pointCount = vertices.Length;
			if (WorldPoints.Length != pointCount)
				AllocatePoints(pointCount);

			var edgeCount = controlMesh.Edges.Length;
			if (Edges.Length != edgeCount)
				AllocateEdges(edgeCount);

			var polygonCount = controlMesh.Polygons.Length;
			if (PolygonControlId.Length != polygonCount)
				AllocatePolygons(polygonCount);


			var localToWorldMatrix = BrushTransform.localToWorldMatrix;
			
			for (var p = 0; p < pointCount; p++)
			{
				var worldPoint = localToWorldMatrix.MultiplyPoint(vertices[p]);
				WorldPoints[p] = worldPoint;
			}


			var brushTotalLength = 0.0f;
			BrushCenter = MathConstants.zeroVector3;
			for (var p = 0; p < polygonCount; p++)
			{
				var localCenterPoint = MathConstants.zeroVector3;
				var totalLength = 0.0f;
				var polygon = controlMesh.Polygons[p];
				if (polygon == null)
					continue;

				var edgeIndices = polygon.EdgeIndices;
				if (edgeIndices == null ||
					edgeIndices.Length == 0)
					continue;

				var halfEdgeIndex0 = edgeIndices[edgeIndices.Length - 1];
				if (halfEdgeIndex0 < 0 || halfEdgeIndex0 >= controlMesh.Edges.Length)
					continue;

				var vertexIndex0 = controlMesh.Edges[halfEdgeIndex0].VertexIndex;
				if (vertexIndex0 < 0 || vertexIndex0 >= vertices.Length)
					continue;

				var vertex0 = vertices[vertexIndex0];

				if (PolygonPointIndices[p] == null ||
					PolygonPointIndices[p].Length != edgeIndices.Length)
					PolygonPointIndices[p] = new int[edgeIndices.Length];

				var newPointIndices = PolygonPointIndices[p];
				for (var i = 0; i < edgeIndices.Length; i++)
				{
					var halfEdgeIndex1 = edgeIndices[i];
					if (halfEdgeIndex1 < 0 ||
						halfEdgeIndex1 >= controlMesh.Edges.Length)
						continue;

					var vertexIndex1 = controlMesh.Edges[halfEdgeIndex1].VertexIndex;
					if (vertexIndex1 < 0 ||
						vertexIndex1 >= vertices.Length)
						continue;

					var vertex1 = vertices[vertexIndex1];
					newPointIndices[i] = vertexIndex1;

					var length = (vertex1 - vertex0).sqrMagnitude;
					localCenterPoint += (vertex1 + vertex0) * 0.5f * length;
					totalLength += length;
					brushTotalLength += length;

					vertex0 = vertex1;
				}

				var worldCenterPoint = Mathf.Abs(totalLength) < MathConstants.EqualityEpsilon ?
										localToWorldMatrix.MultiplyPoint(vertex0) :
										localToWorldMatrix.MultiplyPoint(localCenterPoint / totalLength);
				BrushCenter += localCenterPoint;
				PolygonCenterPoints[p] = worldCenterPoint;
				PolygonCenterPlanes[p] = GeometryUtility.CalcPolygonPlane(controlMesh, (short)p);
			}
			if (Mathf.Abs(brushTotalLength) >= MathConstants.EqualityEpsilon)
				BrushCenter /= brushTotalLength;
			BrushCenter = localToWorldMatrix.MultiplyPoint(BrushCenter);
		}

		public static void GetHandleSizes(Camera cam, ref float[] sizes, Vector3[] positions)
		{
			if (sizes.Length != positions.Length)
				sizes = new float[positions.Length];

			if (!cam)
			{
				for (var p = 0; p < sizes.Length; p++)
				{
					sizes[p] = 20.0f;
				}
				return;
			}
			
			const float kHandleSize		= 80.0f / 20.0f;
			const float kHandleMaxSize	= 0.0001f / 20.0f;
			
			//position = Handles.matrix.MultiplyPoint(position);

			var tr				= cam.transform;
			var camPos			= tr.position;
			var camForward		= tr.forward;
			var camRight		= tr.right;
			var worldToClip		= cam.projectionMatrix * cam.worldToCameraMatrix;
			var width			= cam.pixelWidth  * 0.5f;
			var height			= cam.pixelHeight * 0.5f;

			var m00 = worldToClip.m00 * width;
			var m01 = worldToClip.m01 * width;
			var m02 = worldToClip.m02 * width;
			//var m03 = worldToClip.m03;

			var m10 = worldToClip.m10 * height;
			var m11 = worldToClip.m11 * height;
			var m12 = worldToClip.m12 * height;
			//var m13 = worldToClip.m13;

			var m30 = worldToClip.m30;
			var m31 = worldToClip.m31;
			var m32 = worldToClip.m32;
			var m33 = worldToClip.m33;

			//var wr = (m30 * camRight.x + m31 * camRight.y + m32 * camRight.z + m33);
			var offset = camPos + camRight;



			for (var p = 0; p < positions.Length; p++)
			{
				var distance = Vector3.Dot(positions[p] - camPos, camForward);
				var p0	= camForward * distance;
				var p2	= offset + p0;

				var w2	= (m30 * p2.x + m31 * p2.y + m32 * p2.z + m33);
				var iw2	= -1.0f / w2;
					
				var ax	= (camRight.x * iw2);
				var ay	= (camRight.y * iw2);
				var az	= (camRight.z * iw2);

				//var p1	= camPos + p0;
				//var w1	= (m30 * p1.x + m31 * p1.y + m32 * p1.z + m33);
				//var iw1	= 1.0f / w1;					
				//var t		= (iw1 + iw2);
				//ax += (p1.x * t);
				//ay += (p1.y * t);
				//az += (p1.z * t);

				var clipPointX = (m00 * ax) + (m01 * ay) + (m02 * az);// + (m03 * t);
				var clipPointY = (m10 * ax) + (m11 * ay) + (m12 * az);// + (m13 * t);

				var screenDist = Mathf.Sqrt((clipPointX * clipPointX) + (clipPointY * clipPointY));
									//new Vector3(clipPointX, clipPointY, distance - Vector3.Dot(p0 + camRight, camForward)).magnitude;
						
				sizes[p] = (kHandleSize / Mathf.Max(screenDist, kHandleMaxSize)) * EditorGUIUtility.pixelsPerPoint;
			}
		}
		

		public void UpdateHandles(Camera camera, ControlMesh controlMesh)
		{
			if (controlMesh == null ||
				controlMesh.Vertices == null ||
				controlMesh.Edges == null ||
				controlMesh.Polygons == null ||
				PolygonPointIndices == null)
			{
				return;
			}

			
			var cameraState     = GetCameraState(camera, true);

			var cameraPosition	= camera.transform.position;
			var cameraOrtho		= camera.orthographic;
			
			GetHandleSizes(camera, ref cameraState.PolygonCenterPointSizes, PolygonCenterPoints);
			GetHandleSizes(camera, ref cameraState.WorldPointSizes, WorldPoints);

			if (cameraState.WorldPointBackfaced.Length != WorldPoints.Length)
				cameraState.WorldPointBackfaced = new bool[WorldPoints.Length];

			for (var p = 0; p < cameraState.WorldPointBackfaced.Length; p++)
				cameraState.WorldPointBackfaced[p] = true;

			for (int p = 0; p < PolygonCenterPoints.Length; p++)
			{
				var handleSize = cameraState.PolygonCenterPointSizes[p];
				var delta1 = (PolygonCenterPoints[p] - BrushCenter).normalized;
				var delta2 = (PolygonCenterPoints[p] - cameraPosition).normalized;
				var dot = Vector3.Dot(delta1, delta2);

				var polygonBackfaced = Mathf.Abs(dot) > 1 - MathConstants.AngleEpsilon;
				if (cameraOrtho && polygonBackfaced)
				{
					handleSize = 0;
				} else
				if (dot > 0)
				{
					handleSize *= GUIConstants.backHandleScale;
				} else
				{
					handleSize *= GUIConstants.handleScale;
					if (PolygonPointIndices != null && 
						p < PolygonPointIndices.Length)
					{
						var indices = PolygonPointIndices[p];
						if (indices != null)
						{
							for (var i = 0; i < indices.Length; i++)
							{
								if (indices[i] >= cameraState.WorldPointBackfaced.Length)
								{
									PolygonPointIndices[p] = null;
									break;
								}
								cameraState.WorldPointBackfaced[indices[i]] = false;
							}
						}
					}
				}

				cameraState.PolygonCenterPointSizes[p] = handleSize;
			}

			for (var p = 0; p < cameraState.WorldPointSizes.Length; p++)
			{				
				var handleSize = cameraState.WorldPointSizes[p];
				if (cameraState.WorldPointBackfaced[p])
					handleSize *= GUIConstants.backHandleScale;
				else
					handleSize *= GUIConstants.handleScale;
				
				cameraState.WorldPointSizes[p] = handleSize;
			}
		}

		public bool UpdateColors(Camera camera, CSGBrush brush, ControlMesh controlMesh)
		{
			if (controlMesh == null)
				return false;
			
			var cameraState = GetCameraState(camera, false);
			if (cameraState == null)
				return false;

			var valid			= controlMesh.Valid;

			var cameraPosition	= camera.transform.position;
			var cameraOrtho		= camera.orthographic;
			

			var polygonCount = PolygonCenterPoints.Length;
			for (int j = 0, p = 0; p < polygonCount; p++, j += 2)
			{
				var state = (int)Selection.Polygons[p];
				Color color1, color2;
				if (valid)
				{
					color1 = ColorSettings.MeshEdgeOutline;
					color2 = ColorSettings.PolygonInnerStateColor[state];
				} else
				{
					color1 = ColorSettings.outerInvalidColor;
					color2 = ColorSettings.innerInvalidColor;
				}
				
				var delta1 = (PolygonCenterPoints[p] - BrushCenter).normalized;
				var delta2 = (PolygonCenterPoints[p] - cameraPosition).normalized;
				var dot = Vector3.Dot(delta1, delta2);
				if (!cameraOrtho || Mathf.Abs(dot) <= 1 - MathConstants.AngleEpsilon)
				{
					color1.a *= GUIConstants.backfaceTransparency;
					color2.a *= GUIConstants.backfaceTransparency;
				}

				if (state == (int) SelectState.None)
				{
					PolygonColors[p] = Color.clear;
				} else
				{
					var polygonColor = ColorSettings.PointInnerStateColor[state];
					polygonColor.a *= 0.3f;
					PolygonColors[p] = polygonColor;
				}
				PolygonCenterColors[j + 0] = color1;
				PolygonCenterColors[j + 1] = color2;
			}
						
			var edgeCount = Edges.Length;
			for (int j = 0, e = 0; j < edgeCount; e++, j += 2)
			{
				var state = (int)(Selection.Edges[e]
//									  | surfaceSelectState[edgeSurfaces[j    ]] 
//									  | surfaceSelectState[edgeSurfaces[j + 1]]
								);
				if (valid)
				{
					var color = ColorSettings.PointInnerStateColor[state];
					EdgeColors[e] = color;
				} else
				{
					EdgeColors[e] = ColorSettings.InvalidInnerStateColor[state];
				}
			}

			if (cameraState.WorldPointColors.Length != Selection.Points.Length * 2)
				cameraState.WorldPointColors = new Color[Selection.Points.Length * 2];
			
			for (int j = 0, p = 0; p < Selection.Points.Length; p++, j += 2)
			{
				var state = (int)Selection.Points[p];
				Color color1, color2;
				if (valid)
				{
					color1 = ColorSettings.MeshEdgeOutline;
					color2 = ColorSettings.PointInnerStateColor[state];
				} else
				{
					color1 = ColorSettings.MeshEdgeOutline;
					color2 = ColorSettings.InvalidInnerStateColor[state];
				}
				
				if (cameraState.WorldPointBackfaced[p])
				{
					color1.a *= GUIConstants.backfaceTransparency;
					color2.a *= GUIConstants.backfaceTransparency;
				} else
				{
					color2.a = 1.0f;
				}

				cameraState.WorldPointColors[j + 0] = color1;
				cameraState.WorldPointColors[j + 1] = color2;
			}
			return true;
		}

		
		public bool HaveSelection
		{
			get
			{
				for (var p = 0; p < Selection.Points.Length; p++)
				{
					if ((Selection.Points[p] & SelectState.Selected) == SelectState.Selected)
						return true;
				}
				for (var e = 0; e < Selection.Edges.Length; e++)
				{
					if ((Selection.Edges[e] & SelectState.Selected) == SelectState.Selected)
						return true;
				}
				for (var e = 0; e < Selection.Polygons.Length; e++)
				{
					if ((Selection.Polygons[e] & SelectState.Selected) == SelectState.Selected)
						return true;
				}
				return false;
			}
		}

		public bool HaveEdgeSelection
		{
			get
			{
				for (var e = 0; e < Selection.Edges.Length; e++)
				{
					if (IsEdgeSelectedIndirectly(e))
						return true;
				}
				return false;
			}
		}

		public bool SelectAll()
		{
			var hadSelection = false;
			for (var p = 0; p < Selection.Points.Length; p++)
			{
				hadSelection = hadSelection || (Selection.Points[p] & SelectState.Selected) != SelectState.Selected;
				Selection.Points[p] |= SelectState.Selected;
			}

			for (var e = 0; e < Selection.Edges.Length; e++)
			{
				hadSelection = hadSelection || (Selection.Edges[e] & SelectState.Selected) != SelectState.Selected;
				Selection.Edges[e] |= SelectState.Selected;
			}

			for (var p = 0; p < Selection.Polygons.Length; p++)
			{
				hadSelection = hadSelection || (Selection.Polygons[p] & SelectState.Selected) != SelectState.Selected;
				Selection.Polygons[p] |= SelectState.Selected;
			}
			return hadSelection;
		}

		public bool DeSelectAll()
		{
			var hadSelection = false;
			for (var p = 0; p < Selection.Points.Length; p++)
			{
				hadSelection = hadSelection || (Selection.Points[p] & SelectState.Selected) == SelectState.Selected;
				Selection.Points[p] &= ~SelectState.Selected;
			}

			for (var e = 0; e < Selection.Edges.Length; e++)
			{
				hadSelection = hadSelection || (Selection.Edges[e] & SelectState.Selected) == SelectState.Selected;
				Selection.Edges[e] &= ~SelectState.Selected;
			}

			for (var p = 0; p < Selection.Polygons.Length; p++)
			{
				hadSelection = hadSelection || (Selection.Polygons[p] & SelectState.Selected) == SelectState.Selected;
				Selection.Polygons[p] &= ~SelectState.Selected;
			}
			return hadSelection;
		}

		public void UnHoverAll()
		{
			for (var p = 0; p < Selection.Points.Length; p++)
				Selection.Points[p] &= ~SelectState.Hovering;

			for (var e = 0; e < Selection.Edges.Length; e++)
				Selection.Edges[e] &= ~SelectState.Hovering;

			for (var p = 0; p < Selection.Polygons.Length; p++)
				Selection.Polygons[p] &= ~SelectState.Hovering;
		}

		public bool IsPointSelected(int pointIndex)
		{
			var newState = Selection.Points[pointIndex];
			return ((newState & SelectState.Selected) == SelectState.Selected);
		}

		public bool IsEdgeSelected(int edgeIndex)
		{
			var newState = Selection.Edges[edgeIndex];
			return ((newState & SelectState.Selected) == SelectState.Selected);
		}

		public bool IsPolygonSelected(int polygonIndex)
		{
			var newState = Selection.Polygons[polygonIndex];
			return ((newState & SelectState.Selected) == SelectState.Selected);
		}

		public bool IsPointSelectedIndirectly(int pointIndex)
		{
			if ((Selection.Points[pointIndex] & SelectState.Selected) == SelectState.Selected)
				return true;

			for (int p = 0; p < PolygonPointIndices.Length; p++)
			{
				if ((Selection.Polygons[p] & SelectState.Selected) != SelectState.Selected)
					continue;

				var indices = PolygonPointIndices[p];
				for (int i = 0; i < indices.Length; i++)
				{
					if (indices[i] == pointIndex)
						return true;
				}
			}

			for (int e = 0, e2 = 0; e < Selection.Edges.Length; e++, e2+=2)
			{
				if ((Selection.Edges[e] & SelectState.Selected) != SelectState.Selected)
					continue;

				var pointIndex1 = Edges[e2 + 0];
				var pointIndex2 = Edges[e2 + 1];

				if (pointIndex1 == pointIndex ||
					pointIndex2 == pointIndex)
					return true;
			}
			return false;
		}

		public bool IsEdgeSelectedIndirectly(int edgeIndex)
		{
			if ((Selection.Edges[edgeIndex] & SelectState.Selected) == SelectState.Selected)
				return true;
			
			var pointIndex1 = Edges[(edgeIndex * 2) + 0];
			var pointIndex2 = Edges[(edgeIndex * 2) + 1];
			if ((Selection.Points[pointIndex1] & SelectState.Selected) == SelectState.Selected ||
				(Selection.Points[pointIndex2] & SelectState.Selected) == SelectState.Selected)
				return true;

			var polygonIndex1 = EdgeSurfaces[(edgeIndex * 2) + 0];
			var polygonIndex2 = EdgeSurfaces[(edgeIndex * 2) + 1];
			if ((Selection.Polygons[polygonIndex1] & SelectState.Selected) == SelectState.Selected ||
				(Selection.Polygons[polygonIndex2] & SelectState.Selected) == SelectState.Selected)
				return true;

			return false;
		}

		public bool IsPolygonSelectedIndirectly(int polygonIndex)
		{
			if ((Selection.Polygons[polygonIndex] & SelectState.Selected) == SelectState.Selected)
				return true;
			
			return false;
		}

		public int PolygonCount
		{
			get
			{
				return Selection.Polygons.Length;
			}
		}

		private static bool Select(ref SelectState state, SelectionType selectionType, bool onlyOnHover = true)
		{
			var oldState = state;
			if (onlyOnHover && (oldState & SelectState.Hovering) != SelectState.Hovering)
				return false;

			var newState = oldState;
			switch (selectionType)
			{
				case SelectionType.Subtractive:		newState &= ~SelectState.Selected; break;
				case SelectionType.Toggle:			newState ^=  SelectState.Selected; break;
				case SelectionType.Replace:
				case SelectionType.Additive:		newState |=  SelectState.Selected; break;

				default:
					throw new ArgumentOutOfRangeException("selectionType", selectionType, null);
			}

			if (oldState == newState)
				return false;

			state = newState;
			return true;
		}

		public bool SelectPoint(int pointIndex, SelectionType selectionType, bool onlyOnHover = true)
		{
			if (pointIndex >= Selection.Points.Length)
				return false;
			if (!Select(ref Selection.Points[pointIndex], selectionType, onlyOnHover))
				return false;
			return true;
		}

		public bool SelectEdge(int edgeIndex, SelectionType selectionType, bool onlyOnHover = true)
		{
			return Select(ref Selection.Edges[edgeIndex], selectionType, onlyOnHover);
		}

		public bool SelectPolygon(int polygonIndex, SelectionType selectionType, bool onlyOnHover = true)
		{
			var pointIndices    = PolygonPointIndices[polygonIndex];
			if (pointIndices == null)
				return false;

			if (!Select(ref Selection.Polygons[polygonIndex], selectionType, onlyOnHover))
				return false;
			/*
			if ((Selection.Polygons[polygonIndex] & SelectState.Selected) > 0)
			{
				for (var p = 0; p < pointIndices.Length; p++)
				{
					var pointIndex = pointIndices[p];
					Selection.Points[pointIndex] |= SelectState.Selected;
				}
			} else
			{
				for (var p = 0; p < pointIndices.Length; p++)
				{
					var pointIndex = pointIndices[p];
					Selection.Points[pointIndex] &= ~SelectState.Selected;
				}
			}
			*/
			return true;
		}

		public static bool DeselectAll(ControlMeshState[] controlMeshStates)
		{
			var hadSelection = false;
			for (var t = 0; t < controlMeshStates.Length; t++)
			{
				hadSelection = controlMeshStates[t].DeSelectAll() || hadSelection;
			}
			return hadSelection;
		}
		

		public static bool SelectAll(ControlMeshState[] controlMeshStates)
		{
			var hadSelection = false;
			for (var t = 0; t < controlMeshStates.Length; t++)
			{
				hadSelection = controlMeshStates[t].SelectAll() || hadSelection;
			}
			return hadSelection;
		}

		public static void SelectFrustumPoints(ControlMeshState[] controlMeshStates, PointSelection[] selectedPoints, SelectionType selectionType, bool onlyOnHover = true)
		{
			if (selectionType == SelectionType.Replace)
			{
				for (var t = 0; t < controlMeshStates.Length; t++)
				{
					var meshState = controlMeshStates[t];
					if (meshState == null)
						return;
					meshState.DeSelectAll();
				}
			}

			int maxPoints = 0;
			for (var t = 0; t < controlMeshStates.Length; t++)
			{
				var meshState = controlMeshStates[t];
				if (meshState == null)
					return;
				maxPoints = Mathf.Max(maxPoints, meshState.WorldPoints.Length);
			}
			
			var foundPoints = new byte[maxPoints];
			for (var t = 0; t < controlMeshStates.Length; t++)
			{
				var meshState = controlMeshStates[t];
				if (meshState == null)
					return;

				Array.Clear(foundPoints, 0, foundPoints.Length);				
				for (var i = 0; i < selectedPoints.Length; i++)
				{
					var brushNodeID = selectedPoints[i].BrushNodeID;
					if (brushNodeID != t)
						continue;

					var pointIndex = selectedPoints[i].PointIndex;
					foundPoints[pointIndex] = 1;
					//foundPoints.Add(pointIndex);

					if (CSGSettings.SelectionVertex)
						meshState.SelectPoint(pointIndex, selectionType, onlyOnHover);
				}

				if (CSGSettings.SelectionEdge)
				{
					for (int e = 0; e < meshState.Edges.Length; e += 2)
					{
						var pointIndex1 = meshState.Edges[e + 0];
						var pointIndex2 = meshState.Edges[e + 1];
						if (foundPoints[pointIndex1] == 1 &&
							foundPoints[pointIndex2] == 1)
						//if (foundPoints.Contains(pointIndex1) &&
						//	foundPoints.Contains(pointIndex2))
						{
							meshState.SelectEdge(e / 2, selectionType, onlyOnHover);
						}
					}
				}

				if (CSGSettings.SelectionSurface)
				{
					for (int p = 0; p < meshState.PolygonCount; p++)
					{
						var indices = meshState.PolygonPointIndices[p];
						for (int i = 0; i < indices.Length; i++)
						{
							if (foundPoints[indices[i]] != 1)
								//if (!foundPoints.Contains(indices[i]))
								goto SkipPolygon;
						}

						meshState.SelectPolygon(p, selectionType, onlyOnHover);
					SkipPolygon:
						;
					}
				}
			}
		}
	}
}
