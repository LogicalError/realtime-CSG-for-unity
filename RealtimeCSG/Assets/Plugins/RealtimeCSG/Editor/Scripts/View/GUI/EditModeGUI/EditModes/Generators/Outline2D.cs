using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using InternalRealtimeCSG;
using RealtimeCSG.Legacy;
using RealtimeCSG.Components;

namespace RealtimeCSG
{
	[Serializable]
	internal struct HandlePointCurve2D
	{
		[NonSerialized]
		public int			ID;
		public SelectState	State;
		public bool			Tangent;
	}

	[Serializable]
	internal struct HandleEdgeCurve2D
	{
		[NonSerialized]
		public int			ID;
		public SelectState	State;
		public TexGen		Texgen;
	}

	[Serializable]
	internal struct HandleTangentCurve2D
	{
		[NonSerialized]
		public int				 ID;
		public SelectState		 State;
		public HandleConstraints Constraint;
	}

	[Serializable]
	internal sealed class Outline2D : IShapeSettings
	{
		public Curve2D					curve					= new Curve2D();
		public HandlePointCurve2D[]		curvePointHandles;
		public HandleTangentCurve2D[]	curveTangentHandles;
		public HandleEdgeCurve2D[]		curveEdgeHandles;

		public Curve2D		backupCurve			= new Curve2D();
				
		public TexGen		planeTexgen				= new TexGen();
				
//		public CSGPlane[]	onPlaneVertices		= new CSGPlane[0];
		public bool[]		onGeometryVertices	= new bool[0];	
		public CSGBrush[]	onBrushVertices		= new CSGBrush[0];

		
		[NonSerialized] public Vector3[]			realEdge				= null;
		[NonSerialized] public Vector3[]			realTangent				= null;
		[NonSerialized] public int[]				realTangentIDs			= null;
		[NonSerialized] public SelectState[]		realTangentSelection	= null;
		[NonSerialized] public HandleConstraints[]	realTangentState		= null;

//		[NonSerialized] public SceneView	clickSceneView;
		[NonSerialized] public int			prevHoverVertex     = -1;
		[NonSerialized] public int			prevHoverTangent    = -1;
		[NonSerialized] public int			prevHoverEdge       = -1;	
		

		public bool HaveVertices { get { return curve.Points.Length > 0; } }
		

		public void Init(Vector3[] meshVertices, int[] indices)
		{
			curve.Points		= new Vector3[indices.Length];
			curvePointHandles	= new HandlePointCurve2D[indices.Length];
			curveEdgeHandles	= new HandleEdgeCurve2D[indices.Length];

			curve.Tangents		= new TangentCurve2D[indices.Length * 2];
			curveTangentHandles = new HandleTangentCurve2D[indices.Length * 2];

			for (int i = 0, j = 0; i < indices.Length; i++, j += 2)
			{
				curve.Points[i] = meshVertices[indices[i]];

				curve.Tangents[j    ].Constraint = HandleConstraints.Straight;
				curve.Tangents[j + 1].Constraint = HandleConstraints.Straight;
				curve.Tangents[j    ].Tangent = MathConstants.leftVector3;
				curve.Tangents[j + 1].Tangent = MathConstants.rightVector3;
			}
			
			onGeometryVertices		= new bool[indices.Length];
//			onPlaneVertices			= new CSGPlane[indices.Length];
			onBrushVertices			= new CSGBrush[indices.Length];
		}

		public void Reset()
		{
			curve.Points			= new Vector3[0];
			curvePointHandles		= new HandlePointCurve2D[0];
			curveEdgeHandles		= new HandleEdgeCurve2D[0];

			curve.Tangents			= new TangentCurve2D[0];
			curveTangentHandles		= new HandleTangentCurve2D[0];

			backupCurve.Points		= new Vector3[0];
			backupCurve.Tangents	= new TangentCurve2D[0];

			onGeometryVertices		= new bool[0];
//			onPlaneVertices			= new CSGPlane[0];
			onBrushVertices			= new CSGBrush[0];
		}

		public void CalculatePlane(ref CSGPlane plane)
		{
			plane = GeometryUtility.CalcPolygonPlane(curve.Points);
		}


		public void MoveShape(Vector3 offset)
		{
			for (int i = 0; i < curve.Points.Length; i++)
			{
				curve.Points[i] = 
					backupCurve.Points[i]
					+ offset;
			}
		}

		public void Negated()
		{
			Array.Reverse(curve.Points);
			Array.Reverse(curvePointHandles);
			Array.Reverse(curveEdgeHandles);

			Array.Reverse(curve.Tangents);
			Array.Reverse(curveTangentHandles);

			Array.Reverse(onGeometryVertices);
//			Array.Reverse(onPlaneVertices);
			Array.Reverse(onBrushVertices);
		}

		public void AddVertex(Vector3 position, CSGBrush brush, CSGPlane plane, bool onGeometry)
		{
			if (curve.Points.Length > 1)
			{
				if ((curve.Points[curve.Points.Length - 1] - position).sqrMagnitude < MathConstants.EqualityEpsilonSqr)
				{
					return;
				}
				if ((curve.Points[0] - position).sqrMagnitude < MathConstants.EqualityEpsilonSqr)
				{
					return;
				}
			}

			var leftTangentCurve = new TangentCurve2D()
			{
				Tangent = MathConstants.leftVector3,
				Constraint = HandleConstraints.Straight
			};

			var rightTangentCurve = new TangentCurve2D()
			{
				Tangent = MathConstants.rightVector3,
				Constraint = HandleConstraints.Straight
			};

			var handlePointCurve = new HandlePointCurve2D()
			{
				ID = -1,
				State = SelectState.None
			};

			var leftTangentCurveHandle = new HandleTangentCurve2D()
			{
				ID = -1,
				State = SelectState.None,
				Constraint = HandleConstraints.Straight
			};

			var rightTangentCurveHandle = new HandleTangentCurve2D()
			{
				ID = -1,
				State = SelectState.None,
				Constraint = HandleConstraints.Straight
			};

			var handleEdgeCurve = new HandleEdgeCurve2D()
			{
				ID = -1,
				State = SelectState.None,
				Texgen = new TexGen(CSGSettings.DefaultMaterial)
			};

			ArrayUtility.Add(ref curve.Points, position);
			ArrayUtility.Add(ref curvePointHandles, handlePointCurve);
			ArrayUtility.Add(ref curveEdgeHandles, handleEdgeCurve);

			ArrayUtility.Add(ref curve.Tangents, rightTangentCurve);
			ArrayUtility.Add(ref curve.Tangents, leftTangentCurve);
			ArrayUtility.Add(ref curveTangentHandles, rightTangentCurveHandle);
			ArrayUtility.Add(ref curveTangentHandles, leftTangentCurveHandle);

			ArrayUtility.Add(ref onGeometryVertices, onGeometry);
 //           ArrayUtility.Add(ref onPlaneVertices, plane);
			ArrayUtility.Add(ref onBrushVertices, brush);
		}

		public void InsertVertexAfter(int i, Vector3 origin)
		{
			int j = (i + 1) * 2;
			int k = (i + 1) % curve.Points.Length;
			var originalState = curveTangentHandles[(i * 2) + 1].Constraint;
			var tangent = (curve.Points[i] - curve.Points[k]).normalized;

			var leftTangentCurve = new TangentCurve2D()
			{
				Tangent = tangent,
				Constraint = originalState
			};
			var rightTangentCurve = new TangentCurve2D()
			{
				Tangent = -tangent,
				Constraint = originalState
			};

			var handlePointCurve = new HandlePointCurve2D()
			{
				ID = -1,
				Tangent = false,
				State = SelectState.Selected
			};

			var leftTangentCurveHandle = new HandleTangentCurve2D()
			{
				ID = -1,
				State = SelectState.Selected,
				Constraint = originalState
			};

			var rightTangentCurveHandle = new HandleTangentCurve2D()
			{
				ID = -1,
				State = SelectState.Selected,
				Constraint = originalState
			};

			var handleEdgeCurve = new HandleEdgeCurve2D()
			{
				ID = -1,
				State = curveEdgeHandles[i].State,
				Texgen = curveEdgeHandles[i].Texgen
			};

			ArrayUtility.Insert(ref curve.Points, i + 1, origin);
			ArrayUtility.Insert(ref curvePointHandles, i + 1, handlePointCurve);
			ArrayUtility.Insert(ref curveEdgeHandles, i + 1, handleEdgeCurve);

			ArrayUtility.Insert(ref curve.Tangents, j, rightTangentCurve);
			ArrayUtility.Insert(ref curve.Tangents, j, leftTangentCurve);
			ArrayUtility.Insert(ref curveTangentHandles, j, rightTangentCurveHandle);
			ArrayUtility.Insert(ref curveTangentHandles, j, leftTangentCurveHandle);

			ArrayUtility.Insert(ref onGeometryVertices, i + 1, onGeometryVertices[i]);
//			ArrayUtility.Insert(ref onPlaneVertices, i + 1, onPlaneVertices[i]);
			ArrayUtility.Insert(ref onBrushVertices, i + 1, onBrushVertices[i]);
		}

		public void SetTangent(int index, Vector3 tangent)
		{
			if (curveTangentHandles[index].Constraint == HandleConstraints.Mirrored)
			{
				if ((index & 1) == 1)
				{
					curve.Tangents[(index + curve.Tangents.Length - 1) % curve.Tangents.Length].Tangent = -tangent;
				} else
				{
					curve.Tangents[(index + curve.Tangents.Length + 1) % curve.Tangents.Length].Tangent = -tangent;
				}
			}
			curvePointHandles[index / 2].Tangent = true;
			curve.Tangents[index].Tangent = tangent;
		}

		public void SetPosition(int index, Vector3 position)
		{
			curve.Points[index] = position;
		}
		
		public Vector3 GetPosition(int index)
		{
			return curve.Points[index];
		}
		
		public Vector3[] GetVertices()
		{
			return curve.Points;
		}

		public int VertexLength { get { return curve.Points.Length; } }

		public bool HaveSelectedEdges
		{
			get
			{
				if (curveEdgeHandles == null)
					return false;
				for (int i=0;i< curveEdgeHandles.Length;i++)
				{
					if ((curveEdgeHandles[i].State & SelectState.Selected) != 0)
						return true;
				}
				return false;
			}
		}
		public bool HaveSelectedVertices
		{
			get
			{
				if (curvePointHandles == null)
					return false;
				for (int i = 0; i < curvePointHandles.Length; i++)
				{
					if ((curvePointHandles[i].State & SelectState.Selected) != 0)
						return true;
				}
				return false;
			}
		}

		public void DeleteSelectedVertices()
		{
			for (int p = curvePointHandles.Length - 1; p >= 0; p--)
			{
				if ((curvePointHandles[p].State & SelectState.Selected) != SelectState.Selected)
					continue;

				ArrayUtility.RemoveAt(ref curve.Points, p);
				ArrayUtility.RemoveAt(ref curvePointHandles, p);
				ArrayUtility.RemoveAt(ref curveEdgeHandles, p);

				if (backupCurve != null && backupCurve.Points != null && p < backupCurve.Points.Length)
					ArrayUtility.RemoveAt(ref backupCurve.Points, p);

				int t = p * 2;
				ArrayUtility.RemoveAt(ref curve.Tangents, t + 1);
				ArrayUtility.RemoveAt(ref curve.Tangents, t + 0);

				ArrayUtility.RemoveAt(ref curveTangentHandles, t + 1);
				ArrayUtility.RemoveAt(ref curveTangentHandles, t + 0);

				if (backupCurve != null && backupCurve.Tangents != null && (t + 1) < backupCurve.Tangents.Length)
					ArrayUtility.RemoveAt(ref backupCurve.Tangents, t + 1);
				if (backupCurve != null && backupCurve.Tangents != null && (t + 0) < backupCurve.Tangents.Length)
					ArrayUtility.RemoveAt(ref backupCurve.Tangents, t + 0);

				ArrayUtility.RemoveAt(ref onGeometryVertices, p);
//				ArrayUtility.RemoveAt(ref onPlaneVertices, p);
				ArrayUtility.RemoveAt(ref onBrushVertices, p);
			}
		}

		public void DeleteVertex(int v)
		{
			if (v >= curve.Points.Length)
				return;

			ArrayUtility.RemoveAt(ref curve.Points, v);
			ArrayUtility.RemoveAt(ref curvePointHandles, v);
			ArrayUtility.RemoveAt(ref curveEdgeHandles, v);

			if (backupCurve != null && backupCurve.Points != null && v < backupCurve.Points.Length)
				ArrayUtility.RemoveAt(ref backupCurve.Points, v);

			int t = v * 2;
			ArrayUtility.RemoveAt(ref curve.Tangents, t + 1);
			ArrayUtility.RemoveAt(ref curve.Tangents, t + 0);

			ArrayUtility.RemoveAt(ref curveTangentHandles, t + 1);
			ArrayUtility.RemoveAt(ref curveTangentHandles, t + 0);

			if (backupCurve != null && backupCurve.Tangents != null && (t + 1) < backupCurve.Tangents.Length)
				ArrayUtility.RemoveAt(ref backupCurve.Tangents, t + 1);

			if (backupCurve != null && backupCurve.Tangents != null && (t + 0) < backupCurve.Tangents.Length)
				ArrayUtility.RemoveAt(ref backupCurve.Tangents, t + 0);

			ArrayUtility.RemoveAt(ref onGeometryVertices, v);
//			ArrayUtility.RemoveAt(ref onPlaneVertices, v);
			ArrayUtility.RemoveAt(ref onBrushVertices, v);
		}



		bool Select(ref SelectState state, SelectionType selectionType, bool onlyOnHover = true)
		{
			var old_state = state;
			if (onlyOnHover &&(old_state & SelectState.Hovering) != SelectState.Hovering)
				return false;
			var new_state = old_state;
			if		(selectionType ==   SelectionType.Subtractive) new_state &= ~SelectState.Selected;
			else if (selectionType ==   SelectionType.Toggle     ) new_state ^=  SelectState.Selected;
			else												   new_state |=  SelectState.Selected;
			if (old_state == new_state)
				return false;
			state = new_state;
			return true;
		}

		public bool SelectVertex(int index)
		{
			var changed = Select(ref curvePointHandles[index].State, SelectionType.Additive, onlyOnHover: false);
			return changed;
		}

		public bool SelectVertex(int index, SelectionType selectionType, bool onlyOnHover = true)
		{
			var changed = Select(ref curvePointHandles[index].State, selectionType, onlyOnHover);
			if (changed && !IsVertexSelected(index))
			{
				changed = Select(ref curveEdgeHandles[index].State, SelectionType.Subtractive, onlyOnHover: false) || changed;
				index = (index - 1 + curveEdgeHandles.Length) % curveEdgeHandles.Length;
				changed = Select(ref curveEdgeHandles[index].State, SelectionType.Subtractive, onlyOnHover: false) || changed;
			}
			return changed;
		}

		public bool SelectTangent(int index, SelectionType selectionType, bool onlyOnHover = true)
		{
			var changed = Select(ref curveTangentHandles[index].State, selectionType, onlyOnHover);
			return changed;
		}

		public bool SelectEdge(int index, SelectionType selectionType, bool onlyOnHover = true)
		{
			var changed = Select(ref curveEdgeHandles[index].State, selectionType, onlyOnHover);
			changed = Select(ref curvePointHandles[index].State, selectionType, onlyOnHover: false) || changed;
			index = (index + 1) % curvePointHandles.Length;
			if (selectionType == SelectionType.Toggle)
				selectionType = SelectionType.Additive;
			return Select(ref curvePointHandles[index].State, selectionType, onlyOnHover: false) || changed;
		}

		bool HoverOn(ref SelectState state)
		{
			if ((state & SelectState.Hovering) == SelectState.Hovering)
				return false;
			state |= SelectState.Hovering;
			return true;
		}

		public bool HoverOnVertex(int index)
		{
			return HoverOn(ref curvePointHandles[index].State);
		}

		public bool HoverOnTangent(int index)
		{
			return HoverOn(ref curveTangentHandles[index].State);
		}

		public bool HoverOnEdge(int index)
		{
			return HoverOn(ref curveEdgeHandles[index].State);
		}

		public void UnHoverAll()
		{
			for (int p = 0; p < curvePointHandles.Length; p++)
				curvePointHandles[p].State &= ~SelectState.Hovering;
			for (int p = 0; p < curveTangentHandles.Length; p++)
				curveTangentHandles[p].State &= ~SelectState.Hovering;
			for (int e = 0; e < curveEdgeHandles.Length; e++)
				curveEdgeHandles[e].State &= ~SelectState.Hovering;
		}

		public bool DeselectAll()
		{
			bool had_selection = false;
			for (int p = 0; p < curvePointHandles.Length; p++)
			{
				had_selection = had_selection || (curvePointHandles[p].State & SelectState.Selected) == SelectState.Selected;
				curvePointHandles[p].State &= ~SelectState.Selected;
			}
			for (int p = 0; p < curveTangentHandles.Length; p++)
			{
				had_selection = had_selection || (curveTangentHandles[p].State & SelectState.Selected) == SelectState.Selected;
				curveTangentHandles[p].State &= ~SelectState.Selected;
			}
			for (int e = 0; e < curveEdgeHandles.Length; e++)
			{
				had_selection = had_selection || (curveEdgeHandles[e].State & SelectState.Selected) == SelectState.Selected;
				curveEdgeHandles[e].State &= ~SelectState.Selected;
			}

			return had_selection;
		}

		public bool IsVertexSelected(int index)
		{
			return (curvePointHandles[index].State & SelectState.Selected) == SelectState.Selected;
		}

		public bool IsTangentSelected(int index)
		{
			return (curveTangentHandles[index].State & SelectState.Selected) == SelectState.Selected;
		}

		public bool IsEdgeSelected(int index)
		{
			return (curveEdgeHandles[index].State & SelectState.Selected) == SelectState.Selected;
		}

		public void CopyBackupVertices()
		{
			backupCurve.Points = new Vector3[curve.Points.Length];
			Array.Copy(curve.Points, backupCurve.Points, curve.Points.Length);

			backupCurve.Tangents = new TangentCurve2D[curve.Tangents.Length];
			Array.Copy(curve.Tangents, backupCurve.Tangents, curve.Tangents.Length);
		}

		public void UpdateEdgeMaterials(Vector3 extrusionDirection)
		{
			for (int i = 0; i < curve.Points.Length; i++)
				UpdateEdgeMaterial(i, extrusionDirection);
		}

		public void UpdateEdgeMaterial(int edgeIndex, Vector3 extrusionDirection)
		{
			var vertexIndex0 = ((edgeIndex + curve.Points.Length - 1) % curve.Points.Length);
			var vertexIndex1 = edgeIndex;
			
			var vertex0 = curve.Points[vertexIndex0];
			var vertex1 = curve.Points[vertexIndex1];

			var uniqueBrushes = new HashSet<CSGBrush>();
			foreach (var brush in onBrushVertices)
			{
				if (brush)
					uniqueBrushes.Add(brush);
			}
			
			var planeIndices = new int[2];
			foreach(var brush in uniqueBrushes)
			{
				var planeIndex = 0;
				var shape		= brush.Shape;
				var surfaces	= shape.Surfaces;
				var texgens		= shape.TexGens;

				var worldToLocalMatrix	= brush.transform.worldToLocalMatrix;
				var localVertex0		= worldToLocalMatrix.MultiplyPoint(vertex0);
				var localVertex1		= worldToLocalMatrix.MultiplyPoint(vertex1);
				var localDirection		= worldToLocalMatrix.MultiplyVector(extrusionDirection);
				
				for (int surfaceIndex = 0; surfaceIndex < surfaces.Length; surfaceIndex++)
				{
					var surface = surfaces[surfaceIndex];
					var dist1 = Mathf.Abs(surface.Plane.Distance(localVertex0));
					if (dist1 > MathConstants.DistanceEpsilon)
						continue;
					var dist2 = Mathf.Abs(surface.Plane.Distance(localVertex1));					
					if (dist2 > MathConstants.DistanceEpsilon)
						continue;
					planeIndices[planeIndex] = surfaceIndex;
					planeIndex++;
					if (planeIndex == 2)
					{
						float alignment1 = Mathf.Abs(Vector3.Dot(surfaces[planeIndices[0]].Plane.normal, localDirection));
						float alignment2 = Mathf.Abs(Vector3.Dot(surfaces[planeIndices[1]].Plane.normal, localDirection));
						int texGenIndex;
						if (alignment1 < alignment2)
							texGenIndex = surfaces[planeIndices[0]].TexGenIndex;
						else
							texGenIndex = surfaces[planeIndices[1]].TexGenIndex;
						
						curveEdgeHandles[vertexIndex0].Texgen		= texgens[texGenIndex];
						break;
					}
				}
			}
		}

		public void TryFindPlaneMaterial(CSGPlane buildPlane)
		{
			var uniqueBrushes = new HashSet<CSGBrush>();
			foreach (var brush in onBrushVertices)
			{
				if (brush)
					uniqueBrushes.Add(brush);
			}
			
			foreach(var brush in uniqueBrushes)
			{
				var shape		= brush.Shape;
				var surfaces	= shape.Surfaces;
				var texgens		= shape.TexGens;

				var worldToLocalMatrix	= brush.transform.worldToLocalMatrix;
				var localPosition		= worldToLocalMatrix.MultiplyPoint(buildPlane.pointOnPlane);
				var localDirection		= worldToLocalMatrix.MultiplyVector(buildPlane.normal);
				
				for (int surfaceIndex = 0; surfaceIndex < surfaces.Length; surfaceIndex++)
				{
					var surface		= surfaces[surfaceIndex];
					var dist		= Mathf.Abs(surface.Plane.Distance(localPosition));
					if (dist > MathConstants.DistanceEpsilon)
						continue;
					var alignment = Mathf.Abs(Vector3.Dot(surface.Plane.normal, localDirection));
					if (alignment < 1 - MathConstants.NormalEpsilon)
						continue;

					var texGenIndex = surface.TexGenIndex;
					planeTexgen				= texgens[texGenIndex];
					break;
				}
			}
		}
		
		Vector2 Centroid(Vector2[] vertices)
		{
			Vector2 centroid = Vector2.zero;
			var currentVertex = Vector2.zero; 
			var nextVertex = Vector2.zero; 
			var signedArea = 0.0f;
			var partialSignedArea = 0.0f;

			// For all vertices except last
			for (int j = vertices.Length - 1, i = 0; i < vertices.Length; j = i, ++i)
			{
				currentVertex = vertices[j];
				nextVertex = vertices[i];

				partialSignedArea = currentVertex.x * nextVertex.y - nextVertex.x * currentVertex.y;

				signedArea += partialSignedArea;
				centroid.x += (currentVertex.x + nextVertex.x) * partialSignedArea;
				centroid.y += (currentVertex.y + nextVertex.y) * partialSignedArea;
			}

			signedArea *= 0.5f;
			centroid.x /= (6.0f * signedArea);
			centroid.y /= (6.0f * signedArea);

			return centroid;
		}
		

		public Vector3 GetCenter(CSGPlane plane)
		{
			var vertices2d = GeometryUtility.RotatePlaneTo2D(curve.Points, plane);
			var centroid2d = Centroid(vertices2d);

			return plane.Project(GeometryUtility.Rotate2DToPlane(new Vector2[] { centroid2d }, plane)[0]);
		}

		public RealtimeCSG.AABB CalculateBounds(Quaternion rotation, Vector3 gridTangent, Vector3 gridBinormal)
		{
			var bounds = new RealtimeCSG.AABB();
			bounds.Reset();
			var min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
			var max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
			for (int i = 0; i < this.VertexLength; i++)
			{
				var pos = rotation * curve.Points[i];

				min.x = Mathf.Min(min.x, pos.x);
				min.y = Mathf.Min(min.y, pos.y);
				min.z = Mathf.Min(min.z, pos.z);
				
				max.x = Mathf.Max(max.x, pos.x);
				max.y = Mathf.Max(max.y, pos.y);
				max.z = Mathf.Max(max.z, pos.z);
			}
			bounds.Min = min; 
			bounds.Max = max; 
			return bounds;
		}


		public void SetPointConstraint(CSGPlane buildPlane, int pointIndex, HandleConstraints state)
		{
			SetPointConstaintSide(buildPlane, pointIndex, 0, state);
			SetPointConstaintSide(buildPlane, pointIndex, 1, state);
		}

		public void SetPointConstaintSide(CSGPlane buildPlane, int pointIndex, int side, HandleConstraints state)
		{
			if (curveTangentHandles[(pointIndex * 2) + side].Constraint == state)
				return;
			
			curveTangentHandles[(pointIndex * 2) + side].Constraint = state;
			if (state != HandleConstraints.Straight &&
				curveTangentHandles[(pointIndex * 2) + (1 - side)].Constraint != HandleConstraints.Straight)
			{
				if (state == HandleConstraints.Broken &&
					curveTangentHandles[(pointIndex * 2) + (1 - side)].Constraint == HandleConstraints.Mirrored)
					curveTangentHandles[(pointIndex * 2) + (1 - side)].Constraint = HandleConstraints.Broken;

				curvePointHandles[pointIndex].Tangent = true;
				curve.Tangents[(pointIndex * 2) + side].Tangent = -curve.Tangents[(pointIndex * 2) + (1 - side)].Tangent;
				return;
			}

			switch (state)
			{
				case HandleConstraints.Broken:
				case HandleConstraints.Mirrored:
				{
					if (curvePointHandles[pointIndex].Tangent)
						break;
					var count = curve.Points.Length;
					var prev = (pointIndex + count - 1) % count;
					var curr = pointIndex;
					var next = (pointIndex + count + 1) % count;
					var vertex0 = curve.Points[prev];
					var vertex1 = curve.Points[curr];
					var vertex2 = curve.Points[next];

					var centerA = (vertex0 + vertex1 + vertex2) / 3;

					var deltaA = (centerA - vertex1);

					var tangentA = Vector3.Cross(buildPlane.normal, deltaA);
					if (side == 0)
						tangentA = -tangentA;

					curvePointHandles[pointIndex].Tangent = true;
					curve.Tangents[(pointIndex * 2) + side].Tangent = tangentA;
					break;
				}
			}
		}

		static Vector3 PointOnBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
		{
			return (1 - t) * (1 - t) * (1 - t) * p0 + 3 * t * (1 - t) * (1 - t) * p1 + 3 * t * t * (1 - t) * p2 + t * t * t * p3;
		}

		Vector3[] CurvedEdges(uint curveSides, out int[][] curvedEdges)
		{
			var newPoints	= new List<Vector3>();
			var points		= curve.Points;
			var tangents	= curve.Tangents;
			var length		= points.Length;
			curvedEdges = new int[length][];

			for (int i = 0; i < points.Length; i++)
			{
				var index1 = i;
				var p1 = points[index1];
				var index2 = (i + 1) % points.Length;
				var p2 = points[index2];
				var tangentIndex1 = (index1 * 2) + 1;
				var tangentIndex2 = (index2 * 2) + 0;

				if (curveSides == 0 ||
					tangentIndex1 >= curveTangentHandles.Length ||
					(curveTangentHandles[tangentIndex1].Constraint == HandleConstraints.Straight &&
					 curveTangentHandles[tangentIndex2].Constraint == HandleConstraints.Straight))
				{
					curvedEdges[i] = new int[] { newPoints.Count, newPoints.Count + 1 };
					newPoints.Add(p1);
					continue;
				}

				Vector3 p0, p3;

				if (curveTangentHandles[tangentIndex1].Constraint != HandleConstraints.Straight)
					p0 = p1 - tangents[tangentIndex1].Tangent;
				else
					p0 = p1;
				if (curveTangentHandles[tangentIndex2].Constraint != HandleConstraints.Straight)
					p3 = p2 - tangents[tangentIndex2].Tangent;
				else
					p3 = p2;

				int first_index = newPoints.Count;
				newPoints.Add(p1);
				for (int n = 1; n < curveSides; n++)
				{
					newPoints.Add(PointOnBezier(p1, p0, p3, p2, n / (float)curveSides));
				}

				var pointCount = newPoints.Count - first_index + 1;
				curvedEdges[i] = new int[pointCount];
				for (int j = 0; j < pointCount; j++)
				{
					curvedEdges[i][j] = j + first_index;
				}
			}
			var last_indices = curvedEdges[curvedEdges.Length - 1];
			if (last_indices.Length > 0)
				last_indices[last_indices.Length - 1] = 0;
			return newPoints.ToArray();
		}

		public Vector3[] GetCurvedVertices(CSGPlane buildPlane, uint curveSides, out int[][] curvedEdges)
		{
			if (curve.Points.Length < 3)
			{
				curvedEdges = null;
				return GetVertices();
			}
			var vertices3d = CurvedEdges(curveSides, out curvedEdges);
			var vertices2d = GeometryUtility.RotatePlaneTo2D(vertices3d, buildPlane);
			if ((vertices2d[0] - vertices2d[vertices2d.Length - 1]).sqrMagnitude < MathConstants.EqualityEpsilonSqr)
			{
				if (vertices2d.Length == 3)
				{
					curvedEdges = null;
					return GetVertices();
				}
			}
			/*
			Vector2 intersection;
			for (int a1i = vertices2d.Length - 1, a2i = 0; a1i >= 0; a2i = a1i, a1i--)
			{
				TryAgain:
				var A1 = vertices2d[a1i];
				var A2 = vertices2d[a2i];
				for (int b1i = a1i - 1, b2i = a1i; b1i >= 0; b2i = b1i, b1i--)
				{
					var B1 = vertices2d[b1i];
					var B2 = vertices2d[b2i];
					if (GeometryUtility.TryIntersection(A1, A2, 
														B1, B2, 
														out intersection))
					{
						ArrayUtility.Insert(ref vertices2d, a2i, intersection);
						ArrayUtility.Insert(ref vertices2d, b2i, intersection);
						a1i++;
						a2i++;
						goto TryAgain;
					}
				}
			}
			*/

			return GeometryUtility.Rotate2DToPlane(vertices2d, buildPlane);
		}

		public Vector3[] GetCurvedVertices(CSGPlane buildPlane, uint curveSides)
		{
			int[][] curvedEdges = null;
			return GetCurvedVertices(buildPlane, curveSides, out curvedEdges);
		}

		public void UpdateSmoothingGroups(HashSet<uint> usedSmoothingGroupIndices)
		{
			for (int i = 0; i < curveEdgeHandles.Length; i++)
			{
				curveEdgeHandles[i].Texgen.SmoothingGroup = 0;
			}

			for (int i = 0; i < curveEdgeHandles.Length; i++)
			{
				if (curveEdgeHandles[i].Texgen.SmoothingGroup != 0)
					continue;

				var smoothingGroup = SurfaceUtility.FindUnusedSmoothingGroupIndex(usedSmoothingGroupIndices);
				usedSmoothingGroupIndices.Add(smoothingGroup);
				curveEdgeHandles[i].Texgen.SmoothingGroup = smoothingGroup;

				if (i == 0 &&
					curveTangentHandles[(i * 2) + 0].Constraint == HandleConstraints.Mirrored ||
					curveTangentHandles[(i * 2) + 1].Constraint == HandleConstraints.Mirrored)
				{
					var last = curveEdgeHandles.Length - 1;
					curveEdgeHandles[last].Texgen.SmoothingGroup = smoothingGroup;
				}
				if (i < (curveEdgeHandles.Length - 1) &&
					i < (curve.Points.Length - 1) &&
					(curveTangentHandles[((i + 1) * 2) + 0].Constraint == HandleConstraints.Mirrored ||
					 curveTangentHandles[((i + 1) * 2) + 1].Constraint == HandleConstraints.Mirrored))
				{
					curveEdgeHandles[i + 1].Texgen.SmoothingGroup = smoothingGroup;
				}
			}
		}
	}
}
