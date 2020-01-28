using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using InternalRealtimeCSG;
using RealtimeCSG.Legacy;
using RealtimeCSG.Components;
using RealtimeCSG.Foundation;

namespace RealtimeCSG
{
	internal sealed class EditModeMeshEdit : ScriptableObject, IEditMode
	{
		private static readonly int RectSelectionHash			= "vertexRectSelection".GetHashCode();
		private static readonly int meshEdgeChamferHash			= "meshEdgeChamfer".GetHashCode();
		private static readonly int MeshEditBrushToolHash		= "meshEditBrushTool".GetHashCode();
		private static readonly int MeshEditBrushPointHash		= "meshEditBrushPoint".GetHashCode();
		private static readonly int MeshEditBrushEdgeHash		= "meshEditBrushEdge".GetHashCode();
		private static readonly int MeshEditBrushPolygonHash	= "meshEditBrushPolygon".GetHashCode();


		private const float HoverTextDistance = 25.0f;

		public bool	UsesUnitySelection	{ get { return true; } }
		public bool IgnoreUnityRect		{ get { return true; } }

		private bool HavePointSelection { get { for (var t = 0; t < _brushSelection.Brushes.Length; t++) if (_brushSelection.States[t].HaveSelection) return true; return false; } }
		private bool HaveEdgeSelection	{ get { for (var t = 0; t < _brushSelection.Brushes.Length; t++) if (_brushSelection.States[t].HaveEdgeSelection ) return true; return false; } }


		#region Tool edit modes
		private enum EditMode
		{
			None,
			MovingPoint,
			MovingEdge,
			MovingPolygon,

			ScalePolygon,

			RotateEdge
		};
		
		[NonSerialized] private EditMode		_editMode			= EditMode.None;
		
		[NonSerialized] private bool			_doMarquee;			//= false;
		#endregion



		[NonSerialized] private Transform		_rotateBrushParent;	//= null;
		[NonSerialized] private Vector3			_rotateStart		= MathConstants.zeroVector3;
		[NonSerialized] private Vector3			_rotateCenter		= MathConstants.zeroVector3;
		[NonSerialized] private Vector3			_rotateTangent		= MathConstants.zeroVector3;
		[NonSerialized] private Vector3			_rotateNormal		= MathConstants.zeroVector3;
		[NonSerialized] private CSGPlane		_rotatePlane;
		[NonSerialized] private float			_rotateRadius;				//= 0;
		[NonSerialized] private float			_rotateStartAngle;			//= 0; 
		[NonSerialized] private float			_rotateCurrentAngle;		//= 0;
		[NonSerialized] private float			_rotateCurrentSnappedAngle;	//= 0;
		[NonSerialized] private int				_rotationUndoGroupIndex		= -1;

		[NonSerialized] private bool			_movePlaneInNormalDirection	;//= false;
		[NonSerialized] private Vector3			_movePolygonOrigin;
		[NonSerialized] private Vector3			_movePolygonDirection;
		[NonSerialized] private Vector3			_worldDeltaMovement;
		[NonSerialized] private Vector3			_extraDeltaMovement			= MathConstants.zeroVector3;
		
		[SerializeField] private bool			_useHandleCenter;	//= false;
		[SerializeField] private Vector3		_handleCenter;
		[SerializeField] private Vector3		_startHandleCenter;
		[SerializeField] private Vector3		_startHandleDirection;
		[SerializeField] private Vector3		_handleScale		= Vector3.one;
		[SerializeField] private Vector3		_dragEdgeScale		= Vector3.one;
		[SerializeField] private Quaternion		_dragEdgeRotation;
		[SerializeField] private Vector3[]		_handleWorldPoints;	//= null;

		[NonSerialized] private int         _hoverOnEdgeIndex	    = -1;
		[NonSerialized] private int         _hoverOnPointIndex	    = -1;
		[NonSerialized] private int         _hoverOnPolygonIndex    = -1;
		[NonSerialized] private int         _hoverOnTarget		    = -1;
		
		[NonSerialized] private int	        _rectSelectionId        = -1;
		[NonSerialized] private int	        _chamferId				= -1;
		
		[NonSerialized] private bool        _nonZeroMouseIsDown;//= false;
		[NonSerialized] private bool        _mouseIsDragging;   //= false;
		[NonSerialized] private bool        _mouseIsDown;		//= false;
		[NonSerialized] private bool        _showMarquee;       //= false;
		[NonSerialized] private bool        _firstMove;		    //= false;

		[NonSerialized] private Camera		_startCamera;
		[NonSerialized] private Vector2		_startMousePoint;
		[NonSerialized] private Vector3		_originalPoint;
		[NonSerialized] private Vector2		_mousePosition;
		[NonSerialized] private CSGPlane	_movePlane;
		[NonSerialized] private bool        _usingControl;      //= false


		[SerializeField] private readonly TransformSelection    _transformSelection  = new TransformSelection();
		[SerializeField] private readonly BrushSelection        _brushSelection      = new BrushSelection();

		[SerializeField] private UnityEngine.Object[]   _undoAbleTransforms     = new UnityEngine.Object[0];
		[SerializeField] private UnityEngine.Object[]   _undoAbleBrushes		= new UnityEngine.Object[0];


		[SerializeField] private SpaceMatrices      _activeSpaceMatrices	= new SpaceMatrices();

		[NonSerialized] private bool				_isEnabled;     //= false;
		[NonSerialized] private bool				_hideTool;      //= false;

		
		public void SetTargets(FilteredSelection filteredSelection)
		{
			if (filteredSelection == null)
				return;

			var foundBrushes		= filteredSelection.GetAllContainedBrushes();
			_brushSelection.Select(foundBrushes);

			var foundTransforms = new HashSet<Transform>();
			if (filteredSelection.NodeTargets != null)
			{
				for (var i = 0; i < filteredSelection.NodeTargets.Length; i++)
				{
					if (filteredSelection.NodeTargets[i])
						foundTransforms.Add(filteredSelection.NodeTargets[i].transform);
				}
			}
			if (filteredSelection.OtherTargets != null)
			{
				for (var i = 0; i < filteredSelection.OtherTargets.Length; i++)
				{
					if (filteredSelection.OtherTargets[i])
						foundTransforms.Add(filteredSelection.OtherTargets[i]);
				}
			}
			
			_transformSelection.Select(foundTransforms.ToArray());
			var transformsAsObjects = _transformSelection.Transforms.ToList<UnityEngine.Object>();
			transformsAsObjects.Add(this);
			_undoAbleTransforms = transformsAsObjects.ToArray();

			var brushesAsObjects = _brushSelection.Brushes.ToList<UnityEngine.Object>();
			brushesAsObjects.Add(this);
			_undoAbleBrushes = brushesAsObjects.ToArray();

			_hideTool = filteredSelection.NodeTargets != null && filteredSelection.NodeTargets.Length > 0;

			if (!_isEnabled)
				return;

			ForceLineUpdate();
			_brushSelection.UpdateTargets();
			CenterPositionHandle();
			Tools.hidden = _hideTool;
		}

		public void OnEnableTool()
		{			
			_isEnabled		= true;
			_usingControl	= false;
			Tools.hidden	= _hideTool;

			ForceLineUpdate();
			_brushSelection.ResetSelection();
			_brushSelection.UpdateTargets();
			CenterPositionHandle();
			ResetTool();
		}

		public void OnDisableTool()
		{
			_isEnabled = false;
			Tools.hidden = false;
			_usingControl = false;
			ResetTool();
		}

		private void ResetTool()
		{
			_usingControl	= false;
			
			RealtimeCSG.CSGGrid.ForceGrid = false;
		}

		
		public bool UndoRedoPerformed()
		{
			_brushSelection.UpdateTargets();
			//UpdateTransformMatrices();
			//UpdateSelection(allowSubstraction: false);
			UpdateWorkControlMesh();
			//UpdateBackupPoints();
			_brushSelection.UndoRedoPerformed();

			CenterPositionHandle();
			ForceLineUpdate();
			CSG_EditorGUIUtility.RepaintAll();  
			return false;
		}


		#region Selection & Hover

		private void SelectMarquee(Camera camera, Rect rect, SelectionType selectionType)
		{
			if (rect.width <= 0 || rect.height <= 0)
				return;

			try
			{
				var frustum				= CameraUtility.GetCameraSubFrustumGUI(camera, rect);
				//var isOrthoCamera		= camera.orthographic;
				var ignoreHiddenPoints	= CSGSettings.HiddenSurfacesNotSelectable;// && (!isOrthoCamera || !CSGSettings.HiddenSurfacesOrthoSelectable);
				var selectedPoints		= SceneQueryUtility.GetPointsInFrustum(camera, frustum.Planes, _brushSelection.Brushes, _brushSelection.States, ignoreHiddenPoints);

				Undo.RecordObject(this, "Select points");
				_brushSelection.RevertSelection();
				ControlMeshState.SelectFrustumPoints(_brushSelection.States, selectedPoints, selectionType, onlyOnHover: false);
				ForceLineUpdate();
			}
			finally
			{
				CenterPositionHandle();
			}
		}

		public bool DeselectAll()
		{
			try
			{
				if (_brushSelection.States == null ||
					_brushSelection.States.Length == 0)
					return false;
				
				Undo.RecordObject(this, "Deselect All");
				if (GUIUtility.hotControl == _rectSelectionId)
				{
					GUIUtility.hotControl = 0;				
					GUIUtility.keyboardControl = 0;
					EditorGUIUtility.editingTextField = false;
					_brushSelection.RevertSelection();
					_brushSelection.DestroySelectionBackup();
					ForceLineUpdate();
					CSG_EditorGUIUtility.RepaintAll();
					return true;
				}

				if (!ControlMeshState.DeselectAll(_brushSelection.States))
				{
					Selection.activeTransform = null;
					return false;
				}
				
				ForceLineUpdate();
				CSG_EditorGUIUtility.RepaintAll();
				return true;
			}
			finally
			{
				CenterPositionHandle();
			}
		}

		public void SelectPolygon(CSGBrush brush, int polygonIndex)
		{
			UpdateTransformMatrices();
			int foundTarget = -1;
			for (int i = 0; i < _brushSelection.States.Length; i++)
			{
				if (_brushSelection.Brushes[i] == brush)
					foundTarget = i;
			}
			var hoverMeshState = _brushSelection.States[foundTarget];
			ControlMeshState.DeselectAll(_brushSelection.States);
			//hoverMeshState.SelectAll();
			hoverMeshState.Selection.Polygons[polygonIndex] |= SelectState.Selected;
			//hoverMeshState.SelectPolygon(polygonIndex, SelectionType.Replace, onlyOnHover: false);

			UpdateWorkControlMesh();
			UpdateBackupPoints();
			UpdateMeshStates();
			CenterPositionHandle();
			CSG_EditorGUIUtility.RepaintAll();
		}

		private bool UpdateSelection(bool allowSubstraction = true)
		{
			try
			{
				if (_hoverOnTarget <= -1 ||
					_hoverOnTarget >= _brushSelection.States.Length)
				{
					return false;
				}
				var hoverMeshState = _brushSelection.States[_hoverOnTarget];
				var selectionType = SelectionUtility.GetEventSelectionType();

				if (allowSubstraction == false)
				{
					selectionType = SelectionType.Replace;
					switch (_editMode)
					{
						case EditMode.MovingPoint:
						{
							if (hoverMeshState.IsPointSelectedIndirectly(_hoverOnPointIndex))
								selectionType = SelectionType.Additive;
							break;
						}
						case EditMode.RotateEdge:
						case EditMode.MovingEdge:
						{
							if (hoverMeshState.IsEdgeSelectedIndirectly(_hoverOnEdgeIndex))
								selectionType = SelectionType.Additive;
							break;
						}
						case EditMode.MovingPolygon:
						{
							if (hoverMeshState.IsPolygonSelectedIndirectly(_hoverOnPolygonIndex))
								selectionType = SelectionType.Additive;
							break;
						}
					}
				}
				
				var needRepaint = false;

				Undo.RecordObject(this, "Update selection"); 
				if (selectionType == SelectionType.Replace)
					needRepaint = ControlMeshState.DeselectAll(_brushSelection.States);


				for (var p = 0; p < hoverMeshState.Selection.Polygons.Length; p++)
					needRepaint = hoverMeshState.SelectPolygon(p, selectionType, onlyOnHover: true) || needRepaint;

				for (var e = 0; e < hoverMeshState.Selection.Edges.Length; e++)
					needRepaint = hoverMeshState.SelectEdge(e, selectionType, onlyOnHover: true) || needRepaint;

				for (var b = 0; b < _brushSelection.States.Length; b++)
				{
					var curMeshState = _brushSelection.States[b];
					for (var p = 0; p < curMeshState.WorldPoints.Length; p++)
					{
						needRepaint = curMeshState.SelectPoint(p, selectionType, onlyOnHover: true) || needRepaint;
					}
				}
				//_brushSelection.DebugLogData();


				ForceLineUpdate();
				return needRepaint;
			}
			finally
			{
				CenterPositionHandle();
			}
		}



		private MouseCursor _currentCursor = MouseCursor.Arrow;

		private EditMode SetHoverOn(EditMode editModeType, int target, int index = -1)
		{
			_hoverOnTarget = target;
			if (target <= -1 || _hoverOnTarget >= _brushSelection.States.Length)
			{
				_hoverOnEdgeIndex = -1;
				_hoverOnPolygonIndex = -1;
				_hoverOnPointIndex = -1;
				return EditMode.None;
			}

			_hoverOnEdgeIndex = -1;
			_hoverOnPolygonIndex = -1;
			_hoverOnPointIndex = -1;
			if (index == -1)
				return EditMode.None;

			var newCursor = MouseCursor.Arrow;
			switch (editModeType)
			{
				case EditMode.RotateEdge:       _hoverOnEdgeIndex    = index; newCursor = MouseCursor.RotateArrow; break;
				case EditMode.MovingEdge:       _hoverOnEdgeIndex    = index; newCursor = MouseCursor.MoveArrow; break;
				case EditMode.MovingPoint:      _hoverOnPointIndex   = index; newCursor = MouseCursor.MoveArrow; break;
				case EditMode.MovingPolygon:    _hoverOnPolygonIndex = index; newCursor = MouseCursor.MoveArrow; break; 
			}

			if (_currentCursor == MouseCursor.Arrow)
				_currentCursor = newCursor;

			return editModeType;
		}
		private void UpdateMouseCursor()
		{
			if (GUIUtility.hotControl == _rectSelectionId &&
				!_movePlaneInNormalDirection &&
				GUIUtility.hotControl != 0)
				return;

			_currentCursor = CursorUtility.GetSelectionCursor(SelectionUtility.GetEventSelectionType());
		}
		
		private EditMode HoverOnPoint(ControlMeshState meshState, int brushNodeIndex, int pointIndex)
		{
			var editMode = SetHoverOn(EditMode.MovingPoint, brushNodeIndex, pointIndex);
			meshState.Selection.Points[pointIndex] |= SelectState.Hovering;

			return editMode;
		}

		private EditMode HoverOnPolygon(ControlMeshState meshState, int brushNodeIndex, int polygonIndex)
		{
			var editMode = SetHoverOn(EditMode.MovingPolygon, brushNodeIndex, polygonIndex);
			meshState.Selection.Polygons[polygonIndex] |= SelectState.Hovering;

			if (Tools.current == Tool.Scale || 
				SelectionUtility.CurrentModifiers == EventModifiers.Control)
				return editMode;
			
			var point1 = HandleUtility.WorldToGUIPoint(meshState.PolygonCenterPoints[polygonIndex]);
			var point2 = HandleUtility.WorldToGUIPoint(meshState.PolygonCenterPoints[polygonIndex] + (meshState.PolygonCenterPlanes[polygonIndex].normal * 10.0f));
			var delta = (point2 - point1).normalized;
			/*
			var brush = meshState.BrushTransform.GetComponent<CSGBrush>();
			var controlMesh = brush.ControlMesh;
			var polygon = controlMesh.Polygons[polygonIndex];
			var polygonVertices = new Vector3[meshState.PolygonPointIndices[polygonIndex].Length];
			for (int i=0;i<meshState.PolygonPointIndices[polygonIndex].Length;i++)
			{
				var pointIndex = meshState.PolygonPointIndices[polygonIndex][i];
				polygonVertices[i] = meshState.WorldPoints[pointIndex];
			}
			var calcPlane = GeometryUtility.CalcPolygonPlane(polygonVertices);
			Vector3 tangent, binormal;
			GeometryUtility.CalculateTangents(calcPlane.normal, out tangent, out binormal);
			var texGenIndex = polygon.TexGenIndex;
			var currPlane=  brush.Shape.Surfaces[texGenIndex];
			*/

			_currentCursor = CursorUtility.GetCursorForDirection(delta, 0);
			return editMode;
		}

		private EditMode HoverOnEdge(ControlMeshState meshState, int brushIndex, int edgeIndex)
		{
			var brush = _brushSelection.Brushes[brushIndex];
			var surfaces = brush.Shape.Surfaces;
			
			var vertexIndex1 = meshState.Edges[(edgeIndex * 2) + 0];
			var vertexIndex2 = meshState.Edges[(edgeIndex * 2) + 1];
			
			var surfaceIndex1 = meshState.EdgeSurfaces[(edgeIndex * 2) + 0];
			var surfaceIndex2 = meshState.EdgeSurfaces[(edgeIndex * 2) + 1];

			if (surfaceIndex1 < 0 || surfaceIndex1 >= surfaces.Length ||
				surfaceIndex2 < 0 || surfaceIndex2 >= surfaces.Length)
				return EditMode.None;

			var editMode = EditMode.None;
			if (Tools.current != Tool.Rotate)
			{
				editMode = SetHoverOn(EditMode.MovingEdge, brushIndex, edgeIndex);

				var point1 = HandleUtility.WorldToGUIPoint(meshState.WorldPoints[vertexIndex1]);
				var point2 = HandleUtility.WorldToGUIPoint(meshState.WorldPoints[vertexIndex2]);
				var delta = (point2 - point1).normalized;

				_currentCursor = CursorUtility.GetCursorForEdge(delta);
			} else
			if (Tools.current == Tool.Rotate)
				editMode = SetHoverOn(EditMode.RotateEdge, brushIndex, edgeIndex);

			meshState.Selection.Edges[edgeIndex] |= SelectState.Hovering;
			return editMode;
		}

		#endregion



		#region Actions

		public void SnapToGrid(Camera camera)
		{
			try
			{
				if (HavePointSelection)
				{
					Undo.RecordObjects(_undoAbleBrushes, "Snap points to grid");
					_brushSelection.PointSnapToGrid(camera);
					UpdateWorkControlMesh(forceUpdate: true);
				} else
				{
					Undo.IncrementCurrentGroup();
					var groupIndex = Undo.GetCurrentGroup();
					Undo.RecordObjects(_undoAbleTransforms, "Snap brushes to grid");
					Undo.RecordObjects(_undoAbleBrushes, "Snap points to grid");
					BrushOperations.SnapToGrid(camera, _brushSelection.Brushes);
					_brushSelection.CleanPoints();
					Undo.CollapseUndoOperations(groupIndex);
				}
			}
			finally
			{
				CenterPositionHandle();
			}
		}



		private void ShapeCancelled()
		{
			EditModeManager.EditMode = ToolEditMode.Edit;
			EditModeGenerate.ShapeCancelled -= ShapeCancelled;
			EditModeGenerate.ShapeCommitted -= ShapeCommitted;
		}

		private void ShapeCommitted()
		{
			EditModeManager.EditMode = ToolEditMode.Edit;
			EditModeGenerate.ShapeCancelled -= ShapeCancelled;
			EditModeGenerate.ShapeCommitted -= ShapeCommitted;
		}


		private void ExtrudeSurface(Camera camera, bool drag)
		{
			EditModeGenerate.ShapeCancelled += ShapeCancelled;
			EditModeGenerate.ShapeCommitted += ShapeCommitted;

			var targetMeshState = _brushSelection.States[_hoverOnTarget];
//			var brushLocalToWorld = targetMeshState.BrushTransform.localToWorldMatrix;

			//var polygonPlane = targetMeshState.PolygonCenterPlanes[_hoverOnPolygonIndex];
			//polygonPlane.Transform(brushLocalToWorld);

			var localNormal = targetMeshState.PolygonCenterPlanes[_hoverOnPolygonIndex].normal;
			var worldNormal = targetMeshState.BrushTransform.localToWorldMatrix.MultiplyVector(localNormal).normalized;

			if (Tools.pivotRotation == PivotRotation.Global)
				worldNormal = GeometryUtility.SnapToClosestAxis(worldNormal);

			var brush = _brushSelection.Brushes[_hoverOnTarget];
			if (brush == null)
				return;

			var controlMesh = brush.ControlMesh;
			var shape = brush.Shape;
			if (controlMesh == null || shape == null)
				return;

			var points			= targetMeshState.WorldPoints;
			var pointIndices	= targetMeshState.PolygonPointIndices[_hoverOnPolygonIndex];

			var polygonPlane = GeometryUtility.CalcPolygonPlane(points, pointIndices);

			var polygon = controlMesh.Polygons[_hoverOnPolygonIndex];
			if (polygon == null)
				return;
			
			var edgeIndices = polygon.EdgeIndices;
			if (edgeIndices == null ||
				edgeIndices.Length == 0)
				return;

			var smoothingGroups = new uint[edgeIndices.Length];
			for (int e = 0; e < edgeIndices.Length; e++)
			{
				var edgeIndex			= edgeIndices[e];
				var twinIndex			= controlMesh.Edges[edgeIndex].TwinIndex;
				var twinPolygonIndex	= controlMesh.Edges[twinIndex].PolygonIndex;
				var twinTexGenIndex		= controlMesh.Polygons[twinPolygonIndex].TexGenIndex;
				var twinSmoothingGroup	= shape.TexGens[twinTexGenIndex].SmoothingGroup;
				smoothingGroups[e] = twinSmoothingGroup;
            }

            var forceDragSource = brush.OperationType;
            EditModeManager.GenerateFromSurface(camera, brush, polygonPlane, worldNormal, points, pointIndices, smoothingGroups, drag, forceDragSource, CSGSettings.AutoCommitExtrusion);
		}

		private void MergeDuplicatePoints()
		{
			if (_editMode == EditMode.RotateEdge)
				return;

			try
			{
				Undo.RegisterCompleteObjectUndo(_undoAbleBrushes, "Merging vertices");
				ControlMeshUtility.MergeDuplicatePoints(_brushSelection.Brushes, _brushSelection.States);
				UpdateWorkControlMesh();
			}
			finally
			{
				CenterPositionHandle();
			}
		}

		private void MergeHoverEdgePoints()
		{
			try
			{
				Undo.RegisterCompleteObjectUndo(_undoAbleBrushes, "Merge edge-points");
				ControlMeshUtility.MergeHoverEdgePoints(_brushSelection.Brushes[_hoverOnTarget], _brushSelection.States[_hoverOnTarget], _hoverOnEdgeIndex);
				UpdateWorkControlMesh();
			}
			finally
			{
				CenterPositionHandle();
			}
		}

		private void MergeHoverPolygonPoints()
		{
			try
			{
				Undo.RegisterCompleteObjectUndo(_undoAbleBrushes, "Merge edge-points");
				ControlMeshUtility.MergeHoverPolygonPoints(_brushSelection.Brushes[_hoverOnTarget], _hoverOnPolygonIndex);
				UpdateWorkControlMesh();
			}
			finally
			{
				CenterPositionHandle();
			}
		}

		private void MergeSelected()
		{
			if (!HaveEdgeSelection)
			{
				if (_editMode == EditMode.MovingEdge &&
					_hoverOnTarget > -1 && _hoverOnTarget < _brushSelection.States.Length &&
					_hoverOnEdgeIndex != -1)
				{
					MergeHoverEdgePoints();
				}
				else
				if (_editMode == EditMode.MovingPolygon &&
					_hoverOnTarget > -1 && _hoverOnTarget < _brushSelection.States.Length &&
					_hoverOnPolygonIndex != -1)
				{
					MergeHoverPolygonPoints();
				}
			}

			MergeSelectedEdgePoints();
		}


		private void DoRotateBrushes(Vector3 rotationCenter, Vector3 rotationAxis, float rotationAngle)
		{
			try
			{
				Undo.RecordObjects(_undoAbleTransforms, "Transform brushes");
				_transformSelection.Rotate(rotationCenter, rotationAxis, rotationAngle);
			}
			finally
			{
				CenterPositionHandle();
			}
		}

		private void DoMoveControlPoints(Vector3 worldOffset)
		{
			try
			{
				Undo.RecordObjects(_undoAbleBrushes, "Move control-points");
				_brushSelection.TranslateControlPoints(worldOffset);
				ForceLineUpdate();
			}
			finally
			{
				CenterPositionHandle();
			}
		}

		private void DoRotateControlPoints(Vector3 center, Quaternion handleRotation, Quaternion rotationOffset)
		{
			try
			{
				Undo.RecordObjects(_undoAbleBrushes, "Rotate control-points");
				_brushSelection.RotateControlPoints(center, handleRotation, rotationOffset);
				ForceLineUpdate();
			}
			finally
			{
				CenterPositionHandle();
			}
		}

		private void DoScaleControlPoints(Quaternion rotation, Vector3 scale, Vector3 center)
		{
			try
			{
				Undo.RegisterCompleteObjectUndo(_undoAbleBrushes, "Scale control-points");
				_brushSelection.ScaleControlPoints(center, rotation, scale);
				ForceLineUpdate();
			}
			finally
			{
				CenterPositionHandle();
			}
		}

		private void DeleteSelectedPoints()
		{
			try
			{
				Undo.RegisterCompleteObjectUndo(_undoAbleBrushes, "Delete control-points");
				ControlMeshUtility.DeleteSelectedPoints(_brushSelection.Brushes, _brushSelection.States);
				UpdateWorkControlMesh();
			}
			finally
			{
				CenterPositionHandle();
			}
		}

		public void FlipX()
		{
			try
			{
				BrushOperations.FlipX(_brushSelection.Brushes);
				UpdateWorkControlMesh();
			}
			finally
			{
				CenterPositionHandle();
			}
		}

		public void FlipY()
		{
			try
			{
				BrushOperations.FlipY(_brushSelection.Brushes);
				UpdateWorkControlMesh();
			}
			finally
			{
				CenterPositionHandle();
			}
		}

		public void FlipZ()
		{
			try
			{
				BrushOperations.FlipZ(_brushSelection.Brushes);
				UpdateWorkControlMesh();
			}
			finally
			{
				CenterPositionHandle();
			}
		}

		private void MergeSelectedEdgePoints()
		{
			try
			{
				Undo.RegisterCompleteObjectUndo(_undoAbleBrushes, "Merge edge-points");
				ControlMeshUtility.MergeSelectedEdgePoints(_brushSelection.Brushes, _brushSelection.States);
				UpdateWorkControlMesh();
			}
			finally
			{
				CenterPositionHandle();
			}
		}

		#endregion





		private bool UpdateWorkControlMesh(bool forceUpdate = false)
		{
			_brushSelection.UpdateWorkControlMesh(forceUpdate);
			_transformSelection.Update();
			return true;
		}

		private void UpdateBackupPoints()
		{
			for (var t = 0; t < _brushSelection.Brushes.Length; t++)
			{
				var workControlMesh = _brushSelection.ControlMeshes[t];
				_brushSelection.States[t].BackupPoints = new Vector3[workControlMesh.Vertices.Length];
				if (workControlMesh.Vertices.Length > 0)
				{
					Array.Copy(workControlMesh.Vertices,
								_brushSelection.States[t].BackupPoints,
								workControlMesh.Vertices.Length);
				}

				_brushSelection.States[t].BackupPolygonCenterPoints = new Vector3[_brushSelection.States[t].PolygonCenterPoints.Length];
				if (_brushSelection.States[t].PolygonCenterPoints.Length > 0)
				{
					Array.Copy(_brushSelection.States[t].PolygonCenterPoints,
								_brushSelection.States[t].BackupPolygonCenterPoints,
								_brushSelection.States[t].PolygonCenterPoints.Length);
				}

				_brushSelection.States[t].BackupPolygonCenterPlanes = new CSGPlane[_brushSelection.States[t].PolygonCenterPlanes.Length];
				if (_brushSelection.States[t].PolygonCenterPlanes.Length > 0)
				{
					Array.Copy(_brushSelection.States[t].PolygonCenterPlanes,
								_brushSelection.States[t].BackupPolygonCenterPlanes,
								_brushSelection.States[t].PolygonCenterPlanes.Length);
				}
			}
		}

		private void UpdateTransformMatrices()
		{
			_activeSpaceMatrices = SpaceMatrices.Create(Selection.activeTransform);
		}

		private bool UpdateRotationCircle(SceneView sceneView)
		{
			switch (_editMode)
			{
				case EditMode.RotateEdge:
				{
					_rotateBrushParent = _brushSelection.ModelTransforms[_hoverOnTarget];
					if (_rotateBrushParent == null)
						return false;

					var meshState = _brushSelection.States[_hoverOnTarget];

					_rotateCenter = MathConstants.zeroVector3;
					for (var p = 0; p < meshState.WorldPoints.Length; p++)
					{
						_rotateCenter += meshState.WorldPoints[p];
					}
					_rotateCenter = (_rotateCenter / meshState.WorldPoints.Length);

					var pointIndex1 = meshState.Edges[(_hoverOnEdgeIndex * 2) + 0];
					var pointIndex2 = meshState.Edges[(_hoverOnEdgeIndex * 2) + 1];

					var vertex1 = meshState.WorldPoints[pointIndex1];
					var vertex2 = meshState.WorldPoints[pointIndex2];

					var camera = sceneView.camera;

                    _rotateNormal = camera.orthographic ? camera.transform.forward.normalized : (vertex2 - vertex1).normalized;

					if (Tools.pivotRotation == PivotRotation.Global)
					{
						_rotateNormal = GeometryUtility.SnapToClosestAxis(_rotateNormal);
					}

					_rotatePlane = new CSGPlane(_rotateNormal, _rotateCenter);
					_rotateStart = ((vertex2 + vertex1) * 0.5f);
					_rotateStart = GeometryUtility.ProjectPointOnPlane(_rotatePlane, _rotateStart);
					var delta = (_rotateCenter - _rotateStart);
					_rotateTangent = -delta.normalized;

					var ray			= HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
					var newMousePos = _rotatePlane.RayIntersection(ray);
					_rotateStartAngle = GeometryUtility.SignedAngle(_rotateCenter - _rotateStart, _rotateCenter - newMousePos, _rotateNormal); 

					var handleSize = HandleUtility.GetHandleSize(_rotateCenter);
					_rotateRadius = Math.Max(delta.magnitude, handleSize);

					return true;
				}
			}
			return false;
		}

		private void UpdateGrid(Camera camera)
		{
			if (_hoverOnTarget <= -1 || _hoverOnTarget >= _brushSelection.States.Length || 
				!camera)
			{
				return;
			}
			
			if (_hoverOnPolygonIndex != -1 &&
				_editMode == EditMode.MovingPolygon && 
				(SelectionUtility.CurrentModifiers & EventModifiers.Control) != EventModifiers.Control)
			{
				var targetMeshState		= _brushSelection.States[_hoverOnTarget];
				var brushLocalToWorld	= targetMeshState.BrushTransform.localToWorldMatrix;	
				var worldOrigin			= targetMeshState.PolygonCenterPoints[_hoverOnPolygonIndex];
				var worldDirection		= brushLocalToWorld.MultiplyVector(
											targetMeshState.PolygonCenterPlanes[_hoverOnPolygonIndex].normal).normalized;
				if (Tools.pivotRotation == PivotRotation.Global)
					worldDirection = GeometryUtility.SnapToClosestAxis(worldDirection);
				RealtimeCSG.CSGGrid.SetupRayWorkPlane(camera, worldOrigin, worldDirection, ref _movePlane);
							
				_movePlaneInNormalDirection = true;
				_movePolygonOrigin		= worldOrigin;
				_movePolygonDirection	= worldDirection;
			} else
			if (_hoverOnEdgeIndex != -1 &&
				_editMode == EditMode.MovingEdge && 
				(SelectionUtility.CurrentModifiers & EventModifiers.Control) != EventModifiers.Control)
			{
				var targetMeshState		= _brushSelection.States[_hoverOnTarget];
				var brushLocalToWorld	= targetMeshState.BrushTransform.localToWorldMatrix;
				var pointIndex1			= targetMeshState.Edges[(_hoverOnEdgeIndex * 2) + 0];
				var pointIndex2			= targetMeshState.Edges[(_hoverOnEdgeIndex * 2) + 1];
				var vertex1				= targetMeshState.WorldPoints[pointIndex1];
				var vertex2				= targetMeshState.WorldPoints[pointIndex2];

				var worldOrigin			= _originalPoint;
				var worldDirection		= brushLocalToWorld.MultiplyVector(vertex2 - vertex1).normalized;

				if (Tools.current == Tool.Scale)
				{
					worldDirection = camera.transform.forward;
				}

				if (Tools.pivotRotation == PivotRotation.Global)
					worldDirection = GeometryUtility.SnapToClosestAxis(worldDirection);
				RealtimeCSG.CSGGrid.SetupWorkPlane(camera, worldOrigin, worldDirection, ref _movePlane);
							
				_movePlaneInNormalDirection = true;
				_movePolygonOrigin		= worldOrigin;
				_movePolygonDirection	= worldDirection;
			} else
			{ 	
				RealtimeCSG.CSGGrid.SetupWorkPlane(camera, _originalPoint, ref _movePlane);
				
				_movePlaneInNormalDirection = false;
			}
		}


		private Vector3 SnapMovementToPlane(Vector3 offset)
		{
			if (Math.Abs(_movePlane.a) > 1 - MathConstants.NormalEpsilon) offset.x = 0.0f;
			if (Math.Abs(_movePlane.b) > 1 - MathConstants.NormalEpsilon) offset.y = 0.0f;
			if (Math.Abs(_movePlane.c) > 1 - MathConstants.NormalEpsilon) offset.z = 0.0f;
			if (float.IsNaN(offset.x) || float.IsNaN(offset.y) || float.IsNaN(offset.z))
				offset = MathConstants.zeroVector3;
			return offset;
		}






		internal class BrushOutlineInfo
		{
			public readonly BrushOutlineRenderer BrushOutlineRenderer = new BrushOutlineRenderer();
			public int LastLineMeshGeneration = -1;
			public int LastHandleGeneration = -1;
			internal void Destroy()
			{
				BrushOutlineRenderer.Destroy();
			}
		}
		
		readonly PointMeshManager pointMeshManager = new PointMeshManager();
		
		private readonly Dictionary<Camera, BrushOutlineInfo> _brushOutlineInfos = new Dictionary<Camera, BrushOutlineInfo>();


		internal BrushOutlineInfo GetBrushOutLineInfo(Camera camera)
		{
			BrushOutlineInfo brushOutlineInfo;
			if (_brushOutlineInfos.TryGetValue(camera, out brushOutlineInfo))
				return brushOutlineInfo;
			
			brushOutlineInfo = new BrushOutlineInfo();
			_brushOutlineInfos[camera] = brushOutlineInfo;
			return brushOutlineInfo;
		}

		internal void OnDestroy()
		{
			foreach (var brushOutlineInfo in _brushOutlineInfos.Values)
				brushOutlineInfo.Destroy();
			_brushOutlineInfos.Clear();
			pointMeshManager.Destroy();
		}

		private void ForceLineUpdate()
		{
			var currentMeshGeneration = InternalCSGModelManager.MeshGeneration;
			var removeKeys = new List<Camera>();
			foreach (var brushOutlineInfo in _brushOutlineInfos)
			{
				if (!brushOutlineInfo.Key)
				{
					brushOutlineInfo.Value.Destroy();
					removeKeys.Add(brushOutlineInfo.Key);
					continue;
				}
				brushOutlineInfo.Value.LastLineMeshGeneration = currentMeshGeneration - 1000;
			}
			foreach (var key in removeKeys)
				_brushOutlineInfos.Remove(key);
		}

		private void UpdateMeshStates()
		{
			for (var t = 0; t < _brushSelection.Brushes.Length; t++)
			{
				if (_brushSelection.States[t] == null)
				{
					_brushSelection.UpdateTargets();
					break;
				}

				_brushSelection.States[t].UpdateMesh(_brushSelection.BackupControlMeshes[t],
													 _brushSelection.ControlMeshes[t].Vertices);
			}
		}

		private void UpdatePointSizes(SceneView sceneView)
		{
            var camera = sceneView.camera;
            if (!camera)
				return;

			for (var t = 0; t < _brushSelection.Brushes.Length; t++)
				_brushSelection.States[t].UpdateHandles(camera, _brushSelection.BackupControlMeshes[t]);
		}
		
		[NonSerialized] private ViewTool _preViewTool = ViewTool.None;
		[NonSerialized] private Tool _prevTool = Tool.None;
		private void UpdateLineMeshes(SceneView sceneView)
		{
            var camera = sceneView.camera;
            if (!camera)
				return;

			var brushOutlineInfo = GetBrushOutLineInfo(camera);
			if (brushOutlineInfo == null)
				return;
			
			var brushOutlineRenderer = brushOutlineInfo.BrushOutlineRenderer;

			var currentMeshGeneration   = InternalCSGModelManager.MeshGeneration;
			for (var t = 0; t < _brushSelection.Brushes.Length; t++)
			{
				var brush		= _brushSelection.Brushes[t];
				var meshState	= _brushSelection.States[t];
				if (!brush ||
					meshState == null)
					continue;

				var modelTransform	= _brushSelection.ModelTransforms[t];
				if (modelTransform &&
					meshState.WorldPoints.Length != 0 &&
					meshState.Edges.Length != 0)
					continue;

				brushOutlineInfo.LastLineMeshGeneration = currentMeshGeneration - 1000;
				_brushSelection.UpdateParentModelTransforms();
				break;
			}

			if (brushOutlineInfo.LastLineMeshGeneration == currentMeshGeneration)
			{
				if (_preViewTool != Tools.viewTool ||
					_prevTool != Tools.current)
				{
					_preViewTool = Tools.viewTool;
					_prevTool = Tools.current;
					UpdatePointSizes(sceneView);
				}
				return;
			}
	
			_preViewTool = Tools.viewTool;
			_prevTool = Tools.current;

			brushOutlineInfo.LastLineMeshGeneration = currentMeshGeneration;

			UpdateMeshStates();
			UpdatePointSizes(sceneView);

			brushOutlineRenderer.Update(camera, _brushSelection.Brushes, _brushSelection.ControlMeshes, _brushSelection.States);
		}

		private void DrawPointText(SceneView sceneView, Vector3 brushCenter, Vector3 vertex, string text)
		{
			var brushCenter2D	= HandleUtility.WorldToGUIPoint(brushCenter);

			var vertex2d	= HandleUtility.WorldToGUIPoint(vertex);
			var centerDelta = brushCenter2D - vertex2d;//textCenter2D;
			
			centerDelta.Normalize();
			centerDelta *= HoverTextDistance;

			var textCenter2D = vertex2d;

			textCenter2D.x -= centerDelta.y;
			textCenter2D.y -= centerDelta.x;

			var textCenterRay = HandleUtility.GUIPointToWorldRay(textCenter2D);
			var textCenter = textCenterRay.origin + textCenterRay.direction * ((sceneView.camera.farClipPlane + sceneView.camera.nearClipPlane) * 0.5f);

			Handles.color = Color.black;
			Handles.DrawLine(vertex, textCenter);
								
			PaintUtility.DrawScreenText(textCenter2D, text);
		}

		private void DrawEdgeText(SceneView sceneView, Vector3 brushCenter, Vector3 vertexA, Vector3 vertexB, string text)
		{
			var lineCenter		= (vertexB + vertexA) * 0.5f;
			var textCenter2D	= HandleUtility.WorldToGUIPoint(lineCenter);
			var brushCenter2D	= HandleUtility.WorldToGUIPoint(brushCenter);

			var vertex2dA = HandleUtility.WorldToGUIPoint(vertexA);
			var vertex2dB = HandleUtility.WorldToGUIPoint(vertexB);
			var line2DDelta = vertex2dB - vertex2dA;
			var centerDelta = brushCenter2D - vertex2dA;//textCenter2D;

			var dot = line2DDelta.x * centerDelta.x + line2DDelta.y * centerDelta.y;
			var det = line2DDelta.x * centerDelta.y - line2DDelta.y * centerDelta.x;
			var angle = Mathf.Atan2(det, dot);

			if (Mathf.Sign(angle) < 0)
				line2DDelta = -line2DDelta;
			line2DDelta.y = -line2DDelta.y;
			line2DDelta.Normalize();
			line2DDelta *= HoverTextDistance;

			textCenter2D.x -= line2DDelta.y;
			textCenter2D.y -= line2DDelta.x;

			var textCenterRay = HandleUtility.GUIPointToWorldRay(textCenter2D);
			var textCenter = textCenterRay.origin + textCenterRay.direction * ((sceneView.camera.farClipPlane + sceneView.camera.nearClipPlane) * 0.5f);

			Handles.color = Color.black;
			Handles.DrawLine(lineCenter, textCenter);
								
			PaintUtility.DrawScreenText(textCenter2D, text);
		}

		bool ShouldDrawBrushPoints()
		{
			if (_showMarquee)
				return false;
			if (_editMode == EditMode.MovingPoint || _editMode == EditMode.MovingEdge)
			{
				if (_mouseIsDragging && _nonZeroMouseIsDown)
					return false;
				return true;
			}
			if (_mouseIsDragging || _nonZeroMouseIsDown)
				return false;
			return true;
		}

        bool ShouldDrawPolygonPoints(Camera camera)
		{
			if (_showMarquee || !camera || camera.orthographic)
				return false;
			if (_editMode == EditMode.MovingPolygon)
			{
				if (_mouseIsDragging && _nonZeroMouseIsDown) 
					return false;
				return true;
			}
			if (_mouseIsDragging || _nonZeroMouseIsDown)
				return false;
			return true;
		}
		bool ShouldDrawAnyPoints(Camera camera) { return ShouldDrawBrushPoints() || ShouldDrawPolygonPoints(camera); }
			 

		private void OnPaint(SceneView sceneView)
		{
            var camera = sceneView.camera;
            if (!camera)
				return;

			var brushOutlineInfo = GetBrushOutLineInfo(camera);
			if (brushOutlineInfo == null)
				return;

			if (_movePlaneInNormalDirection &&
				_hoverOnTarget > -1 && _hoverOnTarget < _brushSelection.States.Length &&
				_hoverOnPolygonIndex != -1)
			{
				_currentCursor = CursorUtility.GetCursorForDirection(_movePolygonDirection, 90);
			}

            {
                if (sceneView != null)
                {
                    var windowRect = new Rect(0, 0, sceneView.position.width, sceneView.position.height - CSG_GUIStyleUtility.BottomToolBarHeight);
                    EditorGUIUtility.AddCursorRect(windowRect, _currentCursor);
                }
            }

			var origMatrix = Handles.matrix;
			Handles.matrix = MathConstants.identityMatrix;

			var currentTool = Tools.current;

			{
				brushOutlineInfo.BrushOutlineRenderer.RenderOutlines();

				if (ShouldDrawAnyPoints(camera))
				{ 
					pointMeshManager.Begin();				
					if (ShouldDrawBrushPoints() && CSGSettings.SelectionVertex)
					{
						for (var t = 0; t < _brushSelection.Brushes.Length; t++)
						{
							var meshState		= _brushSelection.States[t];
							var modelTransform	= _brushSelection.ModelTransforms[t];
							if (modelTransform == null ||
								meshState == null ||
								meshState.WorldPoints == null)
								continue;
						
							var CameraState = _brushSelection.States[t].GetCameraState(camera, false);
							if (CameraState == null)
								continue;
						
							pointMeshManager.DrawPoints(camera, meshState.WorldPoints, 
														CameraState.WorldPointSizes, 
														CameraState.WorldPointColors);
                            //PaintUtility.DrawDoubleDots(meshState.WorldPoints, 
                            //							cameraState.WorldPointSizes, 
                            //							cameraState.WorldPointColors, 
                            //							meshState.WorldPoints.Length);
                        }
                    }

					if (ShouldDrawPolygonPoints(camera) && CSGSettings.SelectionSurface)
					{
						for (var t = 0; t < _brushSelection.Brushes.Length; t++)
						{
							var meshState		= _brushSelection.States[t];
							var modelTransform  = _brushSelection.ModelTransforms[t];
							if (modelTransform == null)
								continue;
						
							var cameraState = _brushSelection.States[t].GetCameraState(camera, false);
							if (cameraState == null)
								continue;
						
							pointMeshManager.DrawPoints(camera, meshState.PolygonCenterPoints, 
														cameraState.PolygonCenterPointSizes, 
														meshState.PolygonCenterColors);
                            //PaintUtility.DrawDoubleDots(meshState.PolygonCenterPoints, 
                            //							cameraState.PolygonCenterPointSizes, 
                            //							meshState.PolygonCenterColors, 
                            //							meshState.PolygonCenterPoints.Length);
                        }
                    }
					pointMeshManager.End();
					pointMeshManager.Render(PaintUtility.CustomDotMaterial, PaintUtility.CustomSurfaceNoDepthMaterial);
				}

				if (currentTool == Tool.Rotate && _editMode == EditMode.RotateEdge)
				{
					//if (rotateBrushParent != null)
					{
						if (_mouseIsDragging)
						{
							PaintUtility.DrawRotateCircle(camera, _rotateCenter, _rotateNormal, _rotateTangent, _rotateRadius, 0, _rotateStartAngle, _rotateCurrentSnappedAngle, 
															ColorSettings.RotateCircleOutline);
							PaintUtility.DrawRotateCirclePie(_rotateCenter, _rotateNormal, _rotateTangent, _rotateRadius, 0, _rotateStartAngle, _rotateCurrentSnappedAngle, 
															ColorSettings.RotateCircleOutline);
						} else
						{
							var inCamera = camera.pixelRect.Contains(Event.current.mousePosition);
							if (inCamera && UpdateRotationCircle(sceneView))
							{
								PaintUtility.DrawRotateCircle(camera, _rotateCenter, _rotateNormal, _rotateTangent, _rotateRadius, 0, _rotateStartAngle, _rotateStartAngle, 
																ColorSettings.RotateCircleOutline);
							}
						}
					}
				}

				if ((Tools.current != Tool.Scale && 
					 Tools.current != Tool.Rotate && 

					(SelectionUtility.CurrentModifiers == EventModifiers.Shift && SelectionUtility.CurrentModifiers != EventModifiers.Control)) 
					&& _hoverOnTarget > -1 && _hoverOnPolygonIndex != -1
					)
				{
					var t = _hoverOnTarget;				
					var p = _hoverOnPolygonIndex;
						
					if (t > -1 && t < _brushSelection.States.Length)
					{
						var targetMeshState = _brushSelection.States[t];
						var modelTransform = _brushSelection.ModelTransforms[t];
						if (modelTransform != null)
						{
							if (_hoverOnTarget == t &&
								p == _hoverOnPolygonIndex)
								Handles.color = ColorSettings.PolygonInnerStateColor[(int)(SelectState.Selected | SelectState.Hovering)];

							if (p < targetMeshState.PolygonCenterPoints.Length)
							{
								var origin = targetMeshState.PolygonCenterPoints[p];

								var localNormal = targetMeshState.PolygonCenterPlanes[p].normal;
								var worldNormal = targetMeshState.BrushTransform.localToWorldMatrix.MultiplyVector(localNormal).normalized;

								Handles.matrix = MathConstants.identityMatrix;

								if (Tools.pivotRotation == PivotRotation.Global)
									worldNormal = GeometryUtility.SnapToClosestAxis(worldNormal);

								PaintUtility.DrawArrowCap(origin, worldNormal, HandleUtility.GetHandleSize(origin));
								Handles.matrix = MathConstants.identityMatrix;
							}
						}
					}
				}


				if ((SelectionUtility.CurrentModifiers == EventModifiers.Shift) &&
					camera.pixelRect.Contains(Event.current.mousePosition) && 
					_hoverOnTarget > -1 && _hoverOnTarget < _brushSelection.States.Length)
				{
					var meshState		= _brushSelection.States[_hoverOnTarget];
					var modelTransform	= _brushSelection.ModelTransforms[_hoverOnTarget];
					if (modelTransform != null)
					{
						if (_hoverOnPolygonIndex != -1)
						{
							var origin = meshState.PolygonCenterPoints[_hoverOnPolygonIndex];

							var textCenter2D = HandleUtility.WorldToGUIPoint(origin);
							textCenter2D.y += HoverTextDistance * 2;

							var textCenterRay = HandleUtility.GUIPointToWorldRay(textCenter2D);
							var textCenter = textCenterRay.origin + textCenterRay.direction * ((camera.farClipPlane + camera.nearClipPlane) * 0.5f);

							Handles.color = Color.black;
								
							Handles.DrawLine(origin, textCenter);
							if (Tools.pivotRotation == PivotRotation.Local)
								PaintUtility.DrawScreenText(textCenter2D, "Normal Aligned Extrude [local]");
							else 
								PaintUtility.DrawScreenText(textCenter2D, "Axis Aligned Extrude [global]");
						} else
						if (_hoverOnEdgeIndex != -1 &&
							((_hoverOnEdgeIndex * 2) + 1) < meshState.Edges.Length)
						{
							var pointIndex1 = meshState.Edges[(_hoverOnEdgeIndex * 2) + 0];
							var pointIndex2 = meshState.Edges[(_hoverOnEdgeIndex * 2) + 1];
							var vertexA = meshState.WorldPoints[pointIndex1];
							var vertexB = meshState.WorldPoints[pointIndex2];

							DrawEdgeText(sceneView, meshState.BrushCenter, vertexA, vertexB, "Chamfer");
						} else
						if (_hoverOnPointIndex != -1)
						{
							var vertex = meshState.WorldPoints[_hoverOnPointIndex];

							DrawPointText(sceneView, meshState.BrushCenter, vertex, "Chamfer");
						}
					}
				}

				if (SelectionUtility.CurrentModifiers != EventModifiers.Shift &&
					currentTool != Tool.Rotate && currentTool != Tool.Scale)
				{
					if (_hoverOnEdgeIndex != -1 &&
						_hoverOnTarget > -1 && _hoverOnTarget < _brushSelection.States.Length)
					{
						var meshState = _brushSelection.States[_hoverOnTarget];
						if (((_hoverOnEdgeIndex * 2) + 1) < meshState.Edges.Length)
						{
							Handles.matrix = origMatrix;
							var pointIndex1 = meshState.Edges[(_hoverOnEdgeIndex * 2) + 0];
							var pointIndex2 = meshState.Edges[(_hoverOnEdgeIndex * 2) + 1];
							var vertexA = meshState.WorldPoints[pointIndex1];
							var vertexB = meshState.WorldPoints[pointIndex2];

							var lineDelta = (vertexB - vertexA);
							var length = lineDelta.magnitude;

							DrawEdgeText(sceneView, meshState.BrushCenter, vertexA, vertexB, Units.ToRoundedDistanceString(length));

							Handles.matrix = MathConstants.identityMatrix;
						}
					}
				}
			}

			Handles.matrix = origMatrix;
		}

		private void RenderOffsetText()
		{
			var delta = _worldDeltaMovement + _extraDeltaMovement;
			if (Tools.pivotRotation == PivotRotation.Local)
			{
				if (_activeSpaceMatrices == null)
					_activeSpaceMatrices = SpaceMatrices.Create(Selection.activeTransform);

				delta = GridUtility.CleanPosition(_activeSpaceMatrices.activeLocalToWorld.MultiplyVector(delta).normalized);
			}
							
			var textCenter2D = Event.current.mousePosition;
			textCenter2D.y += HoverTextDistance * 2;
							
			var lockX	= (Mathf.Abs(delta.x) < MathConstants.ConsideredZero) && (CSGSettings.LockAxisX || (Mathf.Abs(_movePlane.a) >= 1 - MathConstants.EqualityEpsilon));
			var lockY	= (Mathf.Abs(delta.y) < MathConstants.ConsideredZero) && (CSGSettings.LockAxisY || (Mathf.Abs(_movePlane.b) >= 1 - MathConstants.EqualityEpsilon));
			var lockZ	= (Mathf.Abs(delta.z) < MathConstants.ConsideredZero) && (CSGSettings.LockAxisZ || (Mathf.Abs(_movePlane.c) >= 1 - MathConstants.EqualityEpsilon));
					
			var text	= Units.ToRoundedDistanceString(delta, lockX, lockY, lockZ);
			PaintUtility.DrawScreenText(textCenter2D, text);
		}


		private int _meshEditBrushToolId;
		private void CreateControlIDs()
		{
			_meshEditBrushToolId = GUIUtility.GetControlID(MeshEditBrushToolHash, FocusType.Keyboard);
			HandleUtility.AddDefaultControl(_meshEditBrushToolId);
			
			_rectSelectionId = GUIUtility.GetControlID(RectSelectionHash, FocusType.Keyboard);
			_chamferId = GUIUtility.GetControlID(meshEdgeChamferHash, FocusType.Keyboard);
			if (_brushSelection.States == null)
				return;

			for (var t = 0; t < _brushSelection.States.Length; t++)
			{
				var meshState = _brushSelection.States[t];
				if (meshState == null)
					continue;
				
				for (var p = 0; p < meshState.WorldPoints.Length; p++)
					meshState.PointControlId[p] = GUIUtility.GetControlID(MeshEditBrushPointHash, FocusType.Keyboard);
					
				for (var e = 0; e < meshState.Edges.Length / 2; e++)
					meshState.EdgeControlId[e] = GUIUtility.GetControlID(MeshEditBrushEdgeHash, FocusType.Keyboard);
					
				for (var p = 0; p < meshState.PolygonCenterPoints.Length; p++)
					meshState.PolygonControlId[p] = GUIUtility.GetControlID(MeshEditBrushPolygonHash, FocusType.Keyboard);
			}
		}

		private void CenterPositionHandle()
		{
			if (_brushSelection.Brushes.Length <= 0)
			{
				_useHandleCenter = false;
				return;
			}

			var bounds = _brushSelection.GetSelectionBounds();
			if (!bounds.Valid)
			{
				_useHandleCenter = false;
				return;
			}
			
			_handleCenter = bounds.Center;
			_handleScale = Vector3.one;
			_useHandleCenter = true;
		}
		

		private Quaternion GetRealHandleRotation()
		{
			var rotation = Tools.handleRotation;
			if (Tools.pivotRotation == PivotRotation.Local)
			{
				var polygonSelectedCount = 0;
				for (var t = 0; t < _brushSelection.States.Length; t++)
				{
					var targetMeshState = _brushSelection.States[t];
					for (var p = 0; p < targetMeshState.PolygonCount; p++)
					{
						if (!targetMeshState.IsPolygonSelected(p))
							continue;

						polygonSelectedCount++;
						var localNormal		= targetMeshState.PolygonCenterPlanes[p].normal;
						var worldNormal		= targetMeshState.BrushTransform.localToWorldMatrix.MultiplyVector(localNormal).normalized;
						if (worldNormal.sqrMagnitude < MathConstants.EqualityEpsilonSqr)
							continue;
						rotation = Quaternion.LookRotation(worldNormal);

						if (Vector3.Dot(rotation * MathConstants.forwardVector3, worldNormal) < 0)
							rotation = Quaternion.Inverse(rotation);

						if (polygonSelectedCount > 1)
							break;
					}
					if (polygonSelectedCount > 1)
						break;
				}
				if (polygonSelectedCount != 1)
					rotation = Tools.handleRotation;
			}
			if (rotation.x <= MathConstants.EqualityEpsilon &&
				rotation.y <= MathConstants.EqualityEpsilon &&
				rotation.z <= MathConstants.EqualityEpsilon &&
				rotation.w <= MathConstants.EqualityEpsilon)
				rotation = Quaternion.identity;

			return rotation;
		}

		private void DrawScaleBounds(Camera camera, Quaternion rotation, Vector3 scale, Vector3 center, Vector3[] worldPoints)
		{
			var lockX	= ((Mathf.Abs(scale.x) - 1) < MathConstants.ConsideredZero) && (CSGSettings.LockAxisX || (Mathf.Abs(_movePlane.a) >= 1 - MathConstants.EqualityEpsilon));
			var lockY	= ((Mathf.Abs(scale.y) - 1) < MathConstants.ConsideredZero) && (CSGSettings.LockAxisY || (Mathf.Abs(_movePlane.b) >= 1 - MathConstants.EqualityEpsilon));
			var lockZ	= ((Mathf.Abs(scale.z) - 1) < MathConstants.ConsideredZero) && (CSGSettings.LockAxisZ || (Mathf.Abs(_movePlane.c) >= 1 - MathConstants.EqualityEpsilon));
			
			var text	= Units.ToRoundedScaleString(scale, lockX, lockY, lockZ);
			PaintUtility.DrawScreenText(camera, _handleCenter, HoverTextDistance * 3, text);

			var bounds = BoundsUtilities.GetBounds(worldPoints, rotation, scale, center);
			var outputVertices = new Vector3[8];
			BoundsUtilities.GetBoundsCornerPoints(bounds, outputVertices);
			PaintUtility.RenderBoundsSizes(Quaternion.Inverse(rotation), rotation, camera, outputVertices, Color.white, Color.white, Color.white, true, true, true);
		}

		CSGPlane[]		chamferPlanes				= new CSGPlane[2];
		Vector3[]		chamferEdgePoints			= new Vector3[2];
		Vector3			chamferDirection			= Vector3.zero;
		Vector3			chamferIntersectionPoint	= Vector3.zero;
		bool[]			chamferBrushModified        = new bool[0];
		Shape[]			chamferShapes               = new Shape[0];
		ControlMesh[]	chamferControlMesh			= new ControlMesh[0];
		ControlMeshSelection[] chamferSelection     = new ControlMeshSelection[0];
		Vector3			chamferVertex				= Vector3.zero;
		ChamferMode		chamferMode					= ChamferMode.Point;

		enum ChamferMode
		{
			Point,
			Edge
		}

		//Vector3[]	chamferTangents		= new Vector3[2];
		//Vector3[]	chamferLines		= new Vector3[0];

		bool StartChamfer(SceneView sceneView)
		{
			if (_hoverOnTarget <= -1 ||
				_hoverOnTarget >= _brushSelection.States.Length)
				return false;

			UpdateBackupPoints();


			var hoverBrush			= _brushSelection.Brushes[_hoverOnTarget];
			var hoverMeshState		= _brushSelection.States[_hoverOnTarget];
			var transform			= hoverBrush.transform;
			var controlMesh			= hoverBrush.ControlMesh;
			var shape				= hoverBrush.Shape;
			var localToWorldMatrix	= transform.localToWorldMatrix;

			if (_hoverOnEdgeIndex != -1)
			{
				chamferMode	= ChamferMode.Edge;
				var halfEdgEIndex = hoverMeshState.EdgeStateToHalfEdge[_hoverOnEdgeIndex];
				var twinIndex = controlMesh.Edges[halfEdgEIndex].TwinIndex;
				var polygonIndex1 = controlMesh.Edges[halfEdgEIndex].PolygonIndex;
				var polygonIndex2 = controlMesh.Edges[twinIndex].PolygonIndex;
				var texGenIndex1 = controlMesh.Polygons[polygonIndex1].TexGenIndex;
				var texGenIndex2 = controlMesh.Polygons[polygonIndex2].TexGenIndex;
				chamferPlanes = new CSGPlane[2];
				chamferPlanes[0] = shape.Surfaces[texGenIndex1].Plane;
				chamferPlanes[1] = shape.Surfaces[texGenIndex2].Plane;
				
				for (int p = 0; p < chamferPlanes.Length; p++)
					chamferPlanes[p].Transform(localToWorldMatrix);

				var pointIndex1 = hoverMeshState.Edges[(_hoverOnEdgeIndex * 2) + 0];
				var pointIndex2 = hoverMeshState.Edges[(_hoverOnEdgeIndex * 2) + 1];

				chamferEdgePoints[0] = hoverMeshState.WorldPoints[pointIndex1];
				chamferEdgePoints[1] = hoverMeshState.WorldPoints[pointIndex2];

				chamferDirection = (chamferEdgePoints[1] - chamferEdgePoints[0]).normalized;
			} else
			if (_hoverOnPointIndex != -1)
			{
				chamferVertex = hoverMeshState.WorldPoints[_hoverOnPointIndex];
				chamferMode	= ChamferMode.Point;
				var foundPlanes = new List<CSGPlane>();
				for (int e = 0; e < controlMesh.Edges.Length; e++)
				{
					if (controlMesh.Edges[e].VertexIndex != _hoverOnPointIndex)
						continue;
					
					var polygonIndex	= controlMesh.Edges[e].PolygonIndex;
					var texGenIndex		= controlMesh.Polygons[polygonIndex].TexGenIndex;
					var plane			= shape.Surfaces[texGenIndex].Plane;
					plane.Transform(localToWorldMatrix);
					foundPlanes.Add(plane);
				}
				chamferPlanes = foundPlanes.ToArray();
			} else
				return false;
			

			GUIUtility.hotControl = _chamferId;
			GUIUtility.keyboardControl = _chamferId;
			EditorGUIUtility.editingTextField = false;
			Event.current.Use();


			chamferBrushModified	= new bool[_brushSelection.Brushes.Length];
			chamferShapes			= new Shape[_brushSelection.Brushes.Length];
			chamferControlMesh		= new ControlMesh[_brushSelection.Brushes.Length];
			chamferSelection		= new ControlMeshSelection[_brushSelection.Brushes.Length];
			for (int b = 0; b < _brushSelection.Brushes.Length; b++)
			{
				chamferShapes	  [b] = _brushSelection.Brushes[b].Shape.Clone();
				chamferControlMesh[b] = _brushSelection.Brushes[b].ControlMesh.Clone();
				chamferSelection  [b] = _brushSelection.States[b].Selection.Clone();
			}

			UpdateChamferIntersectionPoint(sceneView);
			return true;
		}
		
		void UpdateChamferIntersectionPoint(SceneView sceneView)
		{
            var camera = sceneView.camera;
			var mouseRay		= HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
			
			var intersection	= Vector3.zero;
			var found			= false;
			for (int p0 = 0; p0 < chamferPlanes.Length; p0++)
			{
				var tryIntersection = chamferPlanes[p0].RayIntersection(mouseRay);
				bool tryFound = true;
				for (int p1 = 0; p1 < chamferPlanes.Length; p1++)
				{
					if (p0 == p1)
						continue;
					if (chamferPlanes[p1].Distance(tryIntersection) >= MathConstants.DistanceEpsilon)
					{
						tryFound = false;
						break;
					}
				}

				if (!tryFound)
					continue;

				RealtimeCSG.CSGGrid.SetForcedGrid(camera, chamferPlanes[p0]);
				intersection = tryIntersection;
				found = true;
				break;
			}

			if (!found)
				return;

			var activeSnapMode = RealtimeCSG.CSGSettings.ActiveSnappingMode;
			Vector3 projectedPoint;
			switch (chamferMode)
			{
				default:
				{
					return;
				}
				case ChamferMode.Point:
				{
					projectedPoint = chamferVertex;

                    switch (activeSnapMode)
                    {
                        case SnapMode.GridSnapping:
                        {
                            chamferIntersectionPoint = intersection + RealtimeCSG.CSGGrid.SnapDeltaToGrid(camera, intersection - projectedPoint, new Vector3[] { intersection });//, snapToSelf: true);
                            break;
                        }
                        case SnapMode.RelativeSnapping:
                        {
                            chamferIntersectionPoint = intersection + RealtimeCSG.CSGGrid.SnapDeltaRelative(camera, intersection - projectedPoint);
                            break;
                        }
                        default:
                        case SnapMode.None:
                        {
                            chamferIntersectionPoint = intersection + RealtimeCSG.CSGGrid.HandleLockedAxi(intersection - projectedPoint);
                            break;
                        }
                    }
					break;
				}
				case ChamferMode.Edge:
				{
					projectedPoint = GeometryUtility.ProjectPointOnInfiniteLine(intersection, chamferEdgePoints[0], chamferDirection);

                    switch (activeSnapMode)
                    {
                        case SnapMode.GridSnapping:
                        {
                            chamferIntersectionPoint = intersection + RealtimeCSG.CSGGrid.SnapDeltaToGrid(camera, intersection - projectedPoint, new Vector3[] { intersection });//, snapToSelf: true);
                            break;
                        }
                        case SnapMode.RelativeSnapping:
                        {
                            chamferIntersectionPoint = intersection + RealtimeCSG.CSGGrid.SnapDeltaRelative(camera, intersection - projectedPoint);
                            break;
                        }
                        default:
                        case SnapMode.None:
                        {
                            chamferIntersectionPoint = intersection + RealtimeCSG.CSGGrid.HandleLockedAxi(intersection - projectedPoint);
                            break;
                        }
                    }

					projectedPoint = GeometryUtility.ProjectPointOnInfiniteLine(chamferIntersectionPoint, chamferEdgePoints[0], chamferDirection);
					break;
				}
			}
			
			var distance	= (chamferIntersectionPoint - projectedPoint).magnitude;
			
			Undo.RecordObjects(_undoAbleBrushes, "Chamfer brushes");
			
			for (var t = 0; t < _brushSelection.Brushes.Length; t++)
			{
				_brushSelection.States[t].UpdateMesh(chamferControlMesh[t]);
				chamferSelection[t].CopyTo(_brushSelection.States[t].Selection);
			}

			var anyBrushModified = false;
			//var render_lines = new List<Vector3>();
			var selected_edges		= new List<int>();
			var planes				= new HashSet<CSGPlane>();
			var found_points		= new List<Vector3>();
			for (int b = 0; b < _brushSelection.Brushes.Length; b++)
			{
				var brushModified = false;
				var brush		= _brushSelection.Brushes[b];
				var controlMesh = chamferControlMesh[b].Clone();
				var shape		= chamferShapes[b].Clone();
				var meshState	= _brushSelection.States[b];
				
				var transform	= brush.transform;

				planes.Clear();

				if (Math.Abs(distance) > MathConstants.EqualityEpsilon)
				{
					//if (chamferMode == ChamferMode.Edge)
					{
						for (int e = 0; e < meshState.Selection.Edges.Length; e++)
						{
							var halfEdgeIndex = meshState.EdgeStateToHalfEdge[e];
							var twinIndex = controlMesh.Edges[halfEdgeIndex].TwinIndex;
							var polygonIndex1 = controlMesh.Edges[halfEdgeIndex].PolygonIndex;
							var polygonIndex2 = controlMesh.Edges[twinIndex].PolygonIndex;


							if ((meshState.Selection.Edges[e] & (SelectState.Selected | SelectState.Hovering)) == SelectState.None &&
								(meshState.Selection.Polygons[polygonIndex1] & SelectState.Selected) != SelectState.Selected &&
								(meshState.Selection.Polygons[polygonIndex2] & SelectState.Selected) != SelectState.Selected)
							{
								continue;
							}

							var pointIndex1 = meshState.Edges[(e * 2) + 0];
							var pointIndex2 = meshState.Edges[(e * 2) + 1];

							var texGenIndex1 = controlMesh.Polygons[polygonIndex1].TexGenIndex;
							var texGenIndex2 = controlMesh.Polygons[polygonIndex2].TexGenIndex;
							var chamferPlanes0 = shape.Surfaces[texGenIndex1].Plane;
							var chamferPlanes1 = shape.Surfaces[texGenIndex2].Plane;

							var localToWorldMatrix = transform.localToWorldMatrix;
							chamferPlanes0.Transform(localToWorldMatrix);
							chamferPlanes1.Transform(localToWorldMatrix);

							var chamferEdgePoints0 = meshState.WorldPoints[pointIndex1];
							var chamferEdgePoints1 = meshState.WorldPoints[pointIndex2];

							var chamferEdge = (chamferEdgePoints1 - chamferEdgePoints0).normalized;

							var chamferTangents0 = Vector3.Cross(chamferPlanes0.normal, chamferEdge);
							var chamferTangents1 = Vector3.Cross(chamferPlanes1.normal, chamferEdge);

							if (chamferPlanes1.Distance(chamferEdgePoints0 + chamferTangents0) > 0)
								chamferTangents0 = -chamferTangents0;
							if (chamferPlanes0.Distance(chamferEdgePoints0 + chamferTangents1) > 0)
								chamferTangents1 = -chamferTangents1;

							var delta = chamferTangents0 * distance;
							var chamferPoints0 = chamferEdgePoints0 + delta;
							var chamferPoints1 = chamferEdgePoints1 + delta;

							delta = chamferTangents1 * distance;
							var chamferPoints2 = chamferEdgePoints0 + delta;
							//var chamferPoints3 = chamferEdgePoints1 + delta;

							//render_lines.Add(chamferPoints0);
							//render_lines.Add(chamferPoints1);
							//render_lines.Add(chamferPoints2);
							//render_lines.Add(chamferPoints3);

							var chamferPlane = new CSGPlane(chamferPoints0, chamferPoints1, chamferPoints2);

							var localCuttingPlane = GeometryUtility.InverseTransformPlane(brush.transform.localToWorldMatrix, chamferPlane);

							planes.Add(localCuttingPlane);
						}
					}

					//if (chamferMode == ChamferMode.Point)
					{
						var indirectPoints = new HashSet<int>();
						for (int p = 0; p < meshState.Selection.Polygons.Length; p++)
						{
							if ((meshState.Selection.Polygons[p] & SelectState.Selected) != SelectState.Selected)
								continue;

							var pointIndices = meshState.PolygonPointIndices[p];
							for (var i = 0; i < pointIndices.Length; i++)
							{
								indirectPoints.Add((short)pointIndices[i]);
							}
						}
						for (int p = 0; p < meshState.Selection.Points.Length; p++)
						{
							if ((meshState.Selection.Points[p] & (SelectState.Selected | SelectState.Hovering)) == SelectState.None &&
								!indirectPoints.Contains(p))
								continue;

							var localToWorldMatrix = brush.transform.localToWorldMatrix;

							var currPointIndex = p;
							var currPoint = localToWorldMatrix.MultiplyPoint(controlMesh.Vertices[currPointIndex]);

							found_points.Clear();
							for (int e = 0; e < controlMesh.Edges.Length; e++)
							{
								if (controlMesh.Edges[e].VertexIndex != p)
									continue;

								var twinIndex = controlMesh.Edges[e].TwinIndex;
								var twinPointIndex = controlMesh.Edges[twinIndex].VertexIndex;
								var twinPoint = localToWorldMatrix.MultiplyPoint(controlMesh.Vertices[twinPointIndex]);

								var direction = (twinPoint - currPoint).normalized;

								found_points.Add(currPoint + (direction * distance));
							}

							var chamferPlane = GeometryUtility.CalcPolygonPlane(found_points.ToArray());

							if (chamferPlane.Distance(currPoint) < 0)
								chamferPlane = chamferPlane.Negated();

							var localCuttingPlane = GeometryUtility.InverseTransformPlane(brush.transform.localToWorldMatrix, chamferPlane);

							planes.Add(localCuttingPlane);
						}
					}

					foreach(var plane in planes)
					{
						selected_edges.Clear();
						if (ControlMeshUtility.CutMesh(controlMesh, shape, plane, ref selected_edges))
						{
							var edge_loop = ControlMeshUtility.FindEdgeLoop(controlMesh, ref selected_edges);
							if (edge_loop != null)
							{
								if (ControlMeshUtility.SplitEdgeLoop(controlMesh, shape, edge_loop))
								{
									Shape foundShape;
									ControlMesh foundControlMesh;
									if (ControlMeshUtility.FindAndDetachSeparatePiece(controlMesh, shape, plane, out foundControlMesh, out foundShape))
									{
										controlMesh = foundControlMesh;
										shape = foundShape;
										brushModified = true;
									}
								}
							}
						}
					}
				}

				if (brushModified || chamferBrushModified[b])
				{
					anyBrushModified = true;
					chamferBrushModified[b] = brushModified;
					brush.ControlMesh = controlMesh;
					brush.Shape = shape;
					brush.ControlMesh.SetDirty();
					ControlMeshUtility.RebuildShape(brush);
					InternalCSGModelManager.CheckSurfaceModifications(brush, true);
					UpdateWorkControlMesh();
				}
			}
			
			for (var t = 0; t < _brushSelection.Brushes.Length; t++)
			{
				_brushSelection.States[t].UpdateMesh(chamferControlMesh[t]);
				_brushSelection.States[t].Selection.DeselectAll();
			}

			//chamferLines = render_lines.ToArray();

			if (anyBrushModified)
			{
				UpdateLineMeshes(sceneView);
				InternalCSGModelManager.CheckForChanges(true);
			}
		}

		void HandleChamfering(SceneView sceneView)
		{
			var type = Event.current.GetTypeForControl(_chamferId);
			switch (type)
			{
				case EventType.MouseMove:
				case EventType.MouseUp:
				{
					_brushSelection.UpdateTargets();
					UpdateWorkControlMesh();

                    var camera = sceneView.camera;
                    if (camera)
					{
						var brushOutlineInfo = GetBrushOutLineInfo(camera);
						if (brushOutlineInfo != null)
						{
							brushOutlineInfo.LastLineMeshGeneration--;
						}
					}

					UpdateLineMeshes(sceneView);
					RealtimeCSG.CSGGrid.ForceGrid = false;
					GUIUtility.hotControl = 0;
					GUIUtility.keyboardControl = 0;
					EditorGUIUtility.editingTextField = false;
					Event.current.Use();
					break;
				}
				case EventType.MouseDrag:
				{
					UpdateChamferIntersectionPoint(sceneView);

					Event.current.Use();
					break;
				}
				case EventType.Repaint:
				{
					Handles.color = ColorSettings.HoverOutlines;
					
					var camera = sceneView.camera;
                    if (camera)
					{
						var brushOutlineInfo = GetBrushOutLineInfo(camera);
						if (brushOutlineInfo != null)
						{
							brushOutlineInfo.BrushOutlineRenderer.RenderOutlines();
						}
					}

					PaintUtility.SquareDotCap(-1, chamferIntersectionPoint, MathConstants.identityQuaternion, CSG_HandleUtility.GetHandleSize(chamferIntersectionPoint) * GUIConstants.handleScale);
					break; 
				}
			}
		}


		[NonSerialized] private Vector2 _prevMousePos;

		public void HandleEvents(SceneView sceneView, Rect sceneRect)
		{
			var originalEventType = Event.current.type;
			switch (originalEventType)
			{
				case EventType.MouseMove:
				{
					_mouseIsDragging = false;
					_mouseIsDown = false;
					_nonZeroMouseIsDown = false;
					break;
				}
				case EventType.MouseDown:
				{
					_mouseIsDragging = false;
					_mouseIsDown = true;
					if (Event.current.button != 0)
						_nonZeroMouseIsDown = true;
					_prevMousePos = Event.current.mousePosition;
					break;
				}
				case EventType.MouseDrag:
				{
					if (Event.current.button != 0)
						_nonZeroMouseIsDown = true;
					if (!_mouseIsDragging && (_prevMousePos - Event.current.mousePosition).sqrMagnitude > 4.0f)
						_mouseIsDragging = true;
					break;
				}
				case EventType.MouseUp:
				{
					_mouseIsDown = false;
					_nonZeroMouseIsDown = false;
					break;
				}
			}
			
			if (Event.current.type == EventType.Repaint)
				UpdateLineMeshes(sceneView);
			
			var currentHotControl = GUIUtility.hotControl;

			if (currentHotControl != _chamferId &&
				!SceneDragToolManager.IsDraggingObjectInScene &&
				Event.current.GetTypeForControl(_meshEditBrushToolId) == EventType.Repaint)
			{
				OnPaint(sceneView);
			}
			
			//if (Event.current.type == EventType.Layout)
				CreateControlIDs();


			var camera = sceneView.camera;

            if (SelectionUtility.CurrentModifiers == EventModifiers.None &&
				currentHotControl != _chamferId &&
				_useHandleCenter)
			{
				RealtimeCSG.Helpers.CSGHandles.InitFunction init = delegate
				{
					UpdateTransformMatrices();
					UpdateSelection(allowSubstraction: false);
					UpdateWorkControlMesh();
					UpdateBackupPoints();
					UpdateGrid(_startCamera);
					_handleWorldPoints = _brushSelection.GetSelectedWorldPoints(useBackupPoints: true);
					CenterPositionHandle();
					_startHandleCenter = _handleCenter;
					_usingControl = true;
				};

				if (GUIUtility.hotControl == 0)
				{
					_handleWorldPoints = null;
					_handleScale = Vector3.one;
				}

				switch (Tools.current)
				{
					case Tool.None:
					case Tool.Rect: break;
					case Tool.Rotate:
					{
						RealtimeCSG.Helpers.CSGHandles.InitFunction shutdown = delegate
						{
							MergeDuplicatePoints();
							if (UpdateWorkControlMesh(forceUpdate: true))
								UpdateBackupPoints();
							else
								Undo.PerformUndo();
							InternalCSGModelManager.CheckSurfaceModifications(_brushSelection.Brushes, true);
							_usingControl = false;
						};
						
						var handleRotation = GetRealHandleRotation();
						var newRotation = PaintUtility.HandleRotation(camera, _handleCenter, handleRotation, init, shutdown);
						if (GUI.changed)
						{
							ForceLineUpdate();
							DoRotateControlPoints(_handleCenter, handleRotation, Quaternion.Inverse(handleRotation) * newRotation);
							ControlMeshUtility.MergeDuplicatePoints(_brushSelection.Brushes, _brushSelection.States);
							GUI.changed = false;
						}
						break;
					}
					case Tool.Scale:
					{
						var rotation = GetRealHandleRotation();
						RealtimeCSG.Helpers.CSGHandles.InitFunction shutdown = delegate
						{
							MergeDuplicatePoints();
							if (UpdateWorkControlMesh())
							{
								if (_editMode == EditMode.ScalePolygon)
								{
									_brushSelection.Brushes = null;
									SetTargets(EditModeManager.FilteredSelection);
								}
								else
									UpdateBackupPoints();
							} else
								Undo.PerformUndo();
							_usingControl = false;
						};

						var newHandleScale = PaintUtility.HandleScale(_handleScale, _handleCenter, rotation, init, shutdown);
						if (GUI.changed)
						{
							var newScale = newHandleScale;
							if (float.IsInfinity(newScale.x) || float.IsNaN(newScale.x) ||
								float.IsInfinity(newScale.y) || float.IsNaN(newScale.y) ||
								float.IsInfinity(newScale.z) || float.IsNaN(newScale.z)) newScale = Vector3.zero;

							if (newScale.x <= MathConstants.EqualityEpsilon) { newScale.x = 0.0f; }
							if (newScale.y <= MathConstants.EqualityEpsilon) { newScale.y = 0.0f; }
							if (newScale.z <= MathConstants.EqualityEpsilon) { newScale.z = 0.0f; }
							
							DoScaleControlPoints(rotation, newScale, _startHandleCenter);
							ControlMeshUtility.MergeDuplicatePoints(_brushSelection.Brushes, _brushSelection.States);
							_handleScale = newScale;
							GUI.changed = false;
						}
						if (_usingControl)
						{
							DrawScaleBounds(camera, rotation, _handleScale, _startHandleCenter, _handleWorldPoints);
						}
						break;
					}
					case Tool.Move:
					{
						var rotation = GetRealHandleRotation();
						RealtimeCSG.Helpers.CSGHandles.InitFunction shutdown = delegate
						{
							MergeDuplicatePoints();
							if (UpdateWorkControlMesh(forceUpdate: true))
								UpdateBackupPoints();
							else
								Undo.PerformUndo();
							InternalCSGModelManager.CheckSurfaceModifications(_brushSelection.Brushes, true);
							_usingControl = false;
						};
						var newHandleCenter = PaintUtility.HandlePosition(camera, _handleCenter, rotation, _handleWorldPoints, init, shutdown);
						if (GUI.changed)
						{
							ForceLineUpdate();
							var offset = newHandleCenter - _handleCenter;
							_worldDeltaMovement += offset;
							DoMoveControlPoints(_worldDeltaMovement);
							ControlMeshUtility.MergeDuplicatePoints(_brushSelection.Brushes, _brushSelection.States);
							_handleCenter = _startHandleCenter + _worldDeltaMovement;
							GUI.changed = false;
						}
						if (_usingControl)
						{
							var lockX	= (Mathf.Abs(_worldDeltaMovement.x) < MathConstants.ConsideredZero) && (CSGSettings.LockAxisX || (Mathf.Abs(_movePlane.a) >= 1 - MathConstants.EqualityEpsilon));
							var lockY	= (Mathf.Abs(_worldDeltaMovement.y) < MathConstants.ConsideredZero) && (CSGSettings.LockAxisY || (Mathf.Abs(_movePlane.b) >= 1 - MathConstants.EqualityEpsilon));
							var lockZ	= (Mathf.Abs(_worldDeltaMovement.z) < MathConstants.ConsideredZero) && (CSGSettings.LockAxisZ || (Mathf.Abs(_movePlane.c) >= 1 - MathConstants.EqualityEpsilon));
					
							var text	= Units.ToRoundedDistanceString(_worldDeltaMovement, lockX, lockY, lockZ);
							PaintUtility.DrawScreenText(camera, _handleCenter, HoverTextDistance * 3, text);
						}
						break;
					}
				}

			}


			
			if (Event.current.type == EventType.Repaint)
			{
				if (_showMarquee &&
					GUIUtility.hotControl == _rectSelectionId && 
					camera.pixelRect.Contains(_startMousePoint))
				{
					PaintUtility.DrawSelectionRectangle(CameraUtility.PointsToRect(_startMousePoint, Event.current.mousePosition));
				}
			}

			try
			{
				switch (Event.current.type)
				{
					case EventType.MouseDown:
					{
						if (!sceneRect.Contains(Event.current.mousePosition))
							break;

						if (GUIUtility.hotControl != 0 ||
							Event.current.button != 0 ||
							(SelectionUtility.CurrentModifiers & EventModifiers.Alt) == EventModifiers.Alt)
							break;

						if (SelectionUtility.CurrentModifiers == EventModifiers.Shift)
						{
							if (_editMode == EditMode.MovingPolygon ||
								_editMode == EditMode.MovingPoint ||
								_editMode == EditMode.MovingEdge)
							{
								_doMarquee = false;
								_showMarquee = false;
								Event.current.Use();
								break;
							}
						}

						_doMarquee = false;
						_showMarquee = false;
						_firstMove = true;
						_extraDeltaMovement = MathConstants.zeroVector3;
						_worldDeltaMovement = MathConstants.zeroVector3;

						var newControlId = -1;
						if (_hoverOnTarget > -1 && _hoverOnTarget < _brushSelection.States.Length)
						{
							UpdateWorkControlMesh();
							switch (_editMode)
							{
								case EditMode.RotateEdge:
								{
									newControlId = _brushSelection.States[_hoverOnTarget].EdgeControlId[_hoverOnEdgeIndex];
									if (!UpdateRotationCircle(sceneView))
									{
										break;
									}

									_rotateCurrentAngle = _rotateStartAngle;
									_rotateCurrentSnappedAngle = _rotateStartAngle;
											
									Undo.IncrementCurrentGroup();
									_rotationUndoGroupIndex = Undo.GetCurrentGroup();
									break;
								}
								case EditMode.MovingEdge:
								{
									newControlId = _brushSelection.States[_hoverOnTarget].EdgeControlId[_hoverOnEdgeIndex];
									break;
								}
								case EditMode.MovingPoint:
								{
									newControlId = _brushSelection.States[_hoverOnTarget].PointControlId[_hoverOnPointIndex];
									break;
								}
								case EditMode.MovingPolygon:
								{
									newControlId = _brushSelection.States[_hoverOnTarget].PolygonControlId[_hoverOnPolygonIndex];
									if (Tools.current == Tool.Scale)
									{
										_editMode = EditMode.ScalePolygon;
									}
									break;
								}
							}
									
						}
						
						if (newControlId != -1)
						{
							GUIUtility.hotControl				= newControlId;
							GUIUtility.keyboardControl			= newControlId;
							EditorGUIUtility.editingTextField	= false;
							Event.current.Use();

						} else
						//if (!doCloneDragging)
						{
							_doMarquee		= true;
							_startMousePoint = Event.current.mousePosition;
								
							CSG_EditorGUIUtility.RepaintAll();
						}
						break;
					}

					case EventType.MouseDrag:
					{
						if (_doMarquee)
						{
							if (GUIUtility.hotControl == 0)
							{
								_doMarquee = true;
								GUIUtility.hotControl = _rectSelectionId;
								GUIUtility.keyboardControl = _rectSelectionId;
								EditorGUIUtility.editingTextField = false;
							} else
								_doMarquee = false;
						}
                        if (Event.current.button != 0)
                            break;
                        if (SelectionUtility.CurrentModifiers != EventModifiers.Shift)
							break;

						switch (_editMode)
						{
							case EditMode.MovingPolygon:
							{
								ExtrudeSurface(camera, drag: true);
								Event.current.Use();
								break;
							}
							case EditMode.MovingPoint:
							case EditMode.MovingEdge:
							{
								if (GUIUtility.hotControl != 0)
									break;
								if (!StartChamfer(sceneView))
								{
									GUIUtility.hotControl = 0;
									GUIUtility.keyboardControl = 0;
									EditorGUIUtility.editingTextField = false;
								}								
								Event.current.Use();;
								break;
							} 
						}
						break;
					}

					case EventType.MouseUp:
					{
						if (_mouseIsDragging || _showMarquee)
							break;
						/*
						if (_editMode == EditMode.MovingPolygon)
						{
							if (SelectionUtility.CurrentModifiers == EventModifiers.Shift)
							{
								if (Tools.current == Tool.Move)
								{
									ExtrudeSurface(drag: false);
									Event.current.Use();
									break;
								}
							}
						}*/
						if (!UpdateSelection())
						{
							if (_brushSelection.HaveSelection)
							{
								DeselectAll();
							} else
								SelectionUtility.DoSelectionClick(sceneView);
						} else
							CSG_EditorGUIUtility.RepaintAll();
						break;
					}

					case EventType.KeyDown:
					{
						if (GUIUtility.hotControl != 0)
							break;
						if (HavePointSelection && Keys.DeleteSelectionKey.IsKeyPressed()) { Event.current.Use(); break; }
						if (Keys.CancelActionKey.IsKeyPressed()) { Event.current.Use(); break; }
						if (Keys.SnapToGridKey.IsKeyPressed()) { Event.current.Use(); break; }
						if (Keys.MergeEdgePoints.IsKeyPressed() && HaveEdgeSelection) { Event.current.Use(); break; }
						if (Keys.HandleSceneKeyDown(EditModeManager.CurrentTool, false)) { Event.current.Use(); break; }
						if (Keys.FlipSelectionX.IsKeyPressed()) { Event.current.Use(); break; }
						if (Keys.FlipSelectionY.IsKeyPressed()) { Event.current.Use(); break; }
						if (Keys.FlipSelectionZ.IsKeyPressed()) { Event.current.Use(); break; }
						else break;
					}

					case EventType.KeyUp:
					{
						if (GUIUtility.hotControl != 0)
							break;
						if (HavePointSelection && Keys.DeleteSelectionKey.IsKeyPressed()) { Event.current.Use(); DeleteSelectedPoints(); break; }
						if (Keys.SnapToGridKey.IsKeyPressed()) { SnapToGrid(camera); Event.current.Use(); break; }
						if (Keys.MergeEdgePoints.IsKeyPressed() && HaveEdgeSelection) { MergeSelected(); Event.current.Use(); break; }
						if (Keys.HandleSceneKeyUp(EditModeManager.CurrentTool, false)) { Event.current.Use(); break; }
						if (Keys.FlipSelectionX.IsKeyPressed()) { FlipX(); Event.current.Use(); break; }
						if (Keys.FlipSelectionY.IsKeyPressed()) { FlipY(); Event.current.Use(); break; }
						if (Keys.FlipSelectionZ.IsKeyPressed()) { FlipZ(); Event.current.Use(); break; }
						else break;
					}

					case EventType.ValidateCommand:
					{
						if (GUIUtility.hotControl != 0)
							break;
						if (HavePointSelection && (Event.current.commandName == "SoftDelete" || Event.current.commandName == "Delete")) { Event.current.Use(); break; }
						if (Keys.CancelActionKey.IsKeyPressed()) { Event.current.Use(); break; }
						if (Keys.SnapToGridKey.IsKeyPressed()) { Event.current.Use(); break; }
						if (Keys.MergeEdgePoints.IsKeyPressed() && HaveEdgeSelection) { Event.current.Use(); break; }
						if (Keys.HandleSceneValidate(EditModeManager.CurrentTool, false)) { Event.current.Use(); break; }
						if (Keys.FlipSelectionX.IsKeyPressed()) { Event.current.Use(); break; }
						if (Keys.FlipSelectionY.IsKeyPressed()) { Event.current.Use(); break; }
						if (Keys.FlipSelectionZ.IsKeyPressed()) { Event.current.Use(); break; }
						else break;
					}

					case EventType.ExecuteCommand:
					{
						if (GUIUtility.hotControl != 0)
							break;
						if (HavePointSelection &&
							(Event.current.commandName == "SoftDelete" || 
							 Event.current.commandName == "Delete"))
						{
							DeleteSelectedPoints();
							Event.current.Use();
							break;
						}
						break;
					}
				
					case EventType.Layout:
					{
						UpdateMouseCursor();

						if (_brushSelection.Brushes == null)
						{
							break;
						}
						if (_brushSelection.States.Length != _brushSelection.Brushes.Length)
						{
							break;
						}

						Matrix4x4 origMatrix = Handles.matrix;
						Handles.matrix = MathConstants.identityMatrix;
						try
						{
							var currentTool = Tools.current;
						
							var inCamera = camera && camera.pixelRect.Contains(Event.current.mousePosition);
							
							if (!inCamera || _mouseIsDragging || GUIUtility.hotControl != 0)
								break;

							if (_mouseIsDown || !ShouldDrawAnyPoints(camera))
								break;
							 
							_hoverOnEdgeIndex		= -1;
							_hoverOnPointIndex		= -1;
							_hoverOnPolygonIndex	= -1;
							_hoverOnTarget			= -1;
						
							var cameraPlane = CSG_HandleUtility.GetNearPlane(camera);

							var hoverControl = 0;
							var hotControl = GUIUtility.hotControl;
								
							for (int t = 0; t < _brushSelection.Brushes.Length; t++)
							{
								//var brush = _brushSelection.Brushes[t];
								var meshState = _brushSelection.States[t];
								if (meshState == null)
									continue;

								for (int j = 0, e = 0; j < meshState.Edges.Length; e++, j += 2)
								{
									var newControlId = meshState.EdgeControlId[e];
									if (hotControl == newControlId) hoverControl = newControlId;
									var distance = meshState.GetClosestEdgeDistance(cameraPlane, meshState.Edges[j + 0], meshState.Edges[j + 1]);
									HandleUtility.AddControl(newControlId, distance);
								}
								
								var cameraState = _brushSelection.States[t].GetCameraState(camera, false);
								if (cameraState == null)
									continue;

								for (var p = 0; p < meshState.PolygonCenterPoints.Length; p++)
								{
									var newControlId = meshState.PolygonControlId[p];
									if (hotControl == newControlId) hoverControl = newControlId;
									if (camera.orthographic || p >= cameraState.PolygonCenterPointSizes.Length ||
										cameraState.PolygonCenterPointSizes[p] <= 0)
										continue;

									var center = meshState.PolygonCenterPoints[p];
									if (_useHandleCenter &&
											(_handleCenter - center).sqrMagnitude <= MathConstants.EqualityEpsilonSqr)
										continue;
										
									var radius = cameraState.PolygonCenterPointSizes[p] * 1.2f;
									var centerDistance = CameraUtility.DistanceToCircle(center, radius);
									HandleUtility.AddControl(newControlId, centerDistance);
								}

								for (var p = 0; p < meshState.WorldPoints.Length; p++)
								{
									var newControlId = meshState.PointControlId[p];
									if (hotControl == newControlId) hoverControl = newControlId;
									
									var center = meshState.WorldPoints[p];
									if (_useHandleCenter &&
											(_handleCenter - center).sqrMagnitude <= MathConstants.EqualityEpsilonSqr || 
											p >= cameraState.WorldPointSizes.Length)
										continue;
									
									var radius = cameraState.WorldPointSizes[p] * 1.2f;
									var distance = CameraUtility.DistanceToCircle(center, radius);
									HandleUtility.AddControl(newControlId, distance);
								}
							}

							try
							{
								var closestBrushIndex = -1;
								var closestSurfaceIndex = -1;
								_brushSelection.FindClosestIntersection(sceneView, out closestBrushIndex, out closestSurfaceIndex);
								if (closestBrushIndex != -1)
								{
									var meshState	 = _brushSelection.States[closestBrushIndex];
									var newControlId = meshState.PolygonControlId[closestSurfaceIndex];
									HandleUtility.AddControl(newControlId, 5.0f);
								}
							}
							catch
							{}
							
							var nearestControl = HandleUtility.nearestControl;
							if (nearestControl == _meshEditBrushToolId) nearestControl = 0; // liar

							if (hoverControl != 0) nearestControl = hoverControl;
							else if (hotControl != 0) nearestControl = 0;
							
							var doRepaint = false;
							if (nearestControl == 0)
							{
								if (hotControl == 0)
								{
									_brushSelection.BackupSelection();
									_brushSelection.UnHoverAll();
									doRepaint = _brushSelection.HasSelectionChanged();
								}
								_editMode = EditMode.None;
							} else
							{
								var newEditMode = EditMode.None;
								_brushSelection.BackupSelection();
								_brushSelection.UnHoverAll();
								for (var t = 0; t < _brushSelection.Brushes.Length; t++)
								{
									var meshState = _brushSelection.States[t];
									if (newEditMode == EditMode.None)
									{
										if (newEditMode == EditMode.None && 
											currentTool != Tool.Rotate && 
											CSGSettings.SelectionVertex)
										{
											for (var p = 0; p < meshState.WorldPoints.Length; p++)
											{
												if (meshState.PointControlId[p] != nearestControl)
													continue;

												var worldPoint = meshState.WorldPoints[p];
												for (var t2 = 0; t2 < _brushSelection.Brushes.Length; t2++)
												{
													if (t2 == t)
														continue;

													var meshState2 = _brushSelection.States[t2];
													for (var p2 = 0; p2 < meshState2.WorldPoints.Length; p2++)
													{
														var worldPoint2 = meshState2.WorldPoints[p2];
														if ((worldPoint- worldPoint2).sqrMagnitude < MathConstants.EqualityEpsilonSqr)
														{
															meshState2.Selection.Points[p2] |= SelectState.Hovering;
															break;
														}
													}
												}
												newEditMode = HoverOnPoint(meshState, t, p);
												break;
											}
										}

										if (newEditMode == EditMode.None && CSGSettings.SelectionSurface)
										{
											for (var p = 0; p < meshState.PolygonCenterPoints.Length; p++)
											{
												if (meshState.PolygonControlId[p] != nearestControl)
													continue;
												
												newEditMode = HoverOnPolygon(meshState, t, p);
												break;
											}
										}

										if (newEditMode == EditMode.None &&
											CSGSettings.SelectionEdge)
										{
											for (var e = 0; e < meshState.EdgeControlId.Length; e++)
											{
												if (meshState.EdgeControlId[e] != nearestControl)
													continue;
												
												newEditMode = HoverOnEdge(meshState, t, e);
												break;
											}
										}
									}
								}
								doRepaint = _brushSelection.HasSelectionChanged();
								_editMode = newEditMode;
							}

							if (doRepaint)
							{
								ForceLineUpdate();
								CSG_EditorGUIUtility.RepaintAll();
							}
						}
						finally
						{
							Handles.matrix = origMatrix;
						}
						break;
					}
				}
				 
				if (currentHotControl == _rectSelectionId)
				{
					var type = Event.current.GetTypeForControl(_rectSelectionId);
					switch (type)
					{
						case EventType.MouseDrag:
						{
							if (Event.current.button != 0)
								break;
							
							if (!_showMarquee)
							{
								if ((_startMousePoint - Event.current.mousePosition).sqrMagnitude >
										(MathConstants.MinimumMouseMovement * MathConstants.MinimumMouseMovement))
								{
									_brushSelection.BackupSelection();
									_showMarquee = true;
								}
								break;
							}

							Event.current.Use();

							if (_brushSelection != null &&
								_brushSelection.States.Length == _brushSelection.Brushes.Length)
							{
								var selectionType = SelectionUtility.GetEventSelectionType();
								SelectMarquee(camera, CameraUtility.PointsToRect(_startMousePoint, Event.current.mousePosition), selectionType);
							}
							CSG_EditorGUIUtility.RepaintAll();
							break;
						}
						case EventType.MouseUp:
						{
							_movePlaneInNormalDirection = false;
							_doMarquee = false;
							_showMarquee = false;
							
							_startCamera = null;
							RealtimeCSG.CSGGrid.ForceGrid = false;

							GUIUtility.hotControl = 0;
							GUIUtility.keyboardControl = 0;
							EditorGUIUtility.editingTextField = false;
							Event.current.Use();

							if (!_mouseIsDragging || !_showMarquee)
							{
								break;
							}

							if (_brushSelection != null &&
								_brushSelection.States.Length == _brushSelection.Brushes.Length)
							{
								var selectionType = SelectionUtility.GetEventSelectionType();
								SelectMarquee(camera, CameraUtility.PointsToRect(_startMousePoint, Event.current.mousePosition), selectionType);
							}
							break;
						}
					}
				} else
				if (currentHotControl == _chamferId)
				{
					HandleChamfering(sceneView);
				} else
				if (_brushSelection.States != null)
				{
					for (var t = 0; t < _brushSelection.States.Length; t++)
					{
						var meshState = _brushSelection.States[t];
						if (meshState == null)
							continue;

						for (int p = 0; p < meshState.WorldPoints.Length; p++)
						{
							if (currentHotControl == meshState.PointControlId[p])
							{
								var type = Event.current.GetTypeForControl(meshState.PointControlId[p]);
								switch (type)
								{
									case EventType.KeyUp:
									{
										if (HavePointSelection && Keys.DeleteSelectionKey.IsKeyPressed()) { Event.current.Use(); DeleteSelectedPoints(); break; }
										if (Keys.MergeEdgePoints.IsKeyPressed()) { MergeSelected(); Event.current.Use(); break; }
										break;
									}
									case EventType.KeyDown:
									{
										if (HavePointSelection && Keys.DeleteSelectionKey.IsKeyPressed()) { Event.current.Use(); break; }
										if (Keys.MergeEdgePoints.IsKeyPressed() && HaveEdgeSelection) { Event.current.Use(); break; }
										break;
									}

									case EventType.ValidateCommand:
									{
										if ((Event.current.commandName == "SoftDelete" || Event.current.commandName == "Delete") && HavePointSelection) { Event.current.Use(); break; }
										if (Keys.MergeEdgePoints.IsKeyPressed()) { Event.current.Use(); break; }
										break;
									}

									case EventType.ExecuteCommand:
									{
										if (HavePointSelection &&
											(Event.current.commandName == "SoftDelete" || Event.current.commandName == "Delete"))
										{
											DeleteSelectedPoints(); Event.current.Use();
											break;
										}
										break;
									}

									case EventType.MouseDrag:
									{
										if (Event.current.button != 0)
											break;
										
										if (Tools.current == Tool.Scale)
											break;

										Event.current.Use();
										if (_firstMove)
										{
											_extraDeltaMovement = MathConstants.zeroVector3;
											_worldDeltaMovement = MathConstants.zeroVector3;
											_startCamera = camera;
											UpdateTransformMatrices();
											UpdateSelection(allowSubstraction: false);
										}
			
										if (//_prevYMode != RealtimeCSG.Grid.YMoveModeActive || 
												_firstMove)
										{
											//_prevYMode = RealtimeCSG.Grid.YMoveModeActive;
											if (_firstMove)
												_originalPoint = meshState.WorldPoints[p];
											UpdateWorkControlMesh();
											UpdateBackupPoints();
											UpdateGrid(_startCamera);
											_firstMove = true;
											_extraDeltaMovement += _worldDeltaMovement;
										}
																		
										var lockX = (CSGSettings.LockAxisX || (Mathf.Abs(_movePlane.a) >= 1 - MathConstants.EqualityEpsilon));
										var lockY = (CSGSettings.LockAxisY || (Mathf.Abs(_movePlane.b) >= 1 - MathConstants.EqualityEpsilon));
										var lockZ = (CSGSettings.LockAxisZ || (Mathf.Abs(_movePlane.c) >= 1 - MathConstants.EqualityEpsilon));					
			
										if (CSGSettings.SnapVector == MathConstants.zeroVector3)
										{
											EditModeManager.ShowMessage("Positional snapping is set to zero, cannot move.");
											break;
										} else
										if (lockX && lockY && lockZ)
										{
											EditModeManager.ShowMessage("All axi are disabled (X Y Z), cannot move.");
											break;
										}
										EditModeManager.ResetMessage();	

										var mouseRay		= HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
										var intersection	= _movePlane.RayIntersection(mouseRay);
										if (float.IsNaN(intersection.x) || float.IsNaN(intersection.y) || float.IsNaN(intersection.z))
											break;

										intersection			= GeometryUtility.ProjectPointOnPlane(_movePlane, intersection);
			
										if (_firstMove)
										{
											_originalPoint = intersection;
											_worldDeltaMovement = MathConstants.zeroVector3;
											_firstMove = false;
										} else
										{
											_worldDeltaMovement = SnapMovementToPlane(intersection - _originalPoint);
										}
						
										// try to snap selected points against non-selected points
										var activeSnappingMode = RealtimeCSG.CSGSettings.ActiveSnappingMode;
                                        switch (activeSnappingMode)
                                        {
                                            case SnapMode.GridSnapping:
                                            {
                                                var worldPoints = _brushSelection.GetSelectedWorldPoints(useBackupPoints: true);
                                                //for (int i = 0; i < worldPoints.Length; i++)
                                                //	worldPoints[i] = GeometryUtility.ProjectPointOnPlane(movePlane, worldPoints[i]);// - center));
                                                _worldDeltaMovement = RealtimeCSG.CSGGrid.SnapDeltaToGrid(camera, _worldDeltaMovement, worldPoints, snapToSelf: true);
                                                break;
                                            }
                                            case SnapMode.RelativeSnapping:
                                            {
                                                _worldDeltaMovement = RealtimeCSG.CSGGrid.SnapDeltaRelative(camera, _worldDeltaMovement);
                                                break;
                                            }
                                            default:
                                            case SnapMode.None:
                                            {
                                                _worldDeltaMovement = RealtimeCSG.CSGGrid.HandleLockedAxi(_worldDeltaMovement);
                                                break;
                                            }
                                        }

										DoMoveControlPoints(_worldDeltaMovement);
										CenterPositionHandle();
										CSG_EditorGUIUtility.RepaintAll();
										break;
									}
									case EventType.MouseUp:
									{
										_movePlaneInNormalDirection = false;
									
										_startCamera = null;
										RealtimeCSG.CSGGrid.ForceGrid = false;

										GUIUtility.hotControl = 0;
										GUIUtility.keyboardControl = 0;
										EditorGUIUtility.editingTextField = false;
										Event.current.Use();

										if (!_mouseIsDragging)
											break;

										MergeDuplicatePoints();
										if (!UpdateWorkControlMesh())
										{				
											Undo.PerformUndo();
										} else
										{
											UpdateBackupPoints();
										}
										break;
									}
									case EventType.Repaint:
									{
										if (Tools.current == Tool.Scale)
											break;
										if (_editMode != EditMode.ScalePolygon)
											RenderOffsetText();
										break;
									}
								}
								break;
							}
						}

						for (int e = 0; e < meshState.Edges.Length / 2; e++)
						{
							if (currentHotControl == meshState.EdgeControlId[e])
							{
								var type = Event.current.GetTypeForControl(meshState.EdgeControlId[e]);
								switch (type)
								{
									case EventType.KeyDown:
									{
										if (HavePointSelection && Keys.DeleteSelectionKey.IsKeyPressed()) { Event.current.Use(); break; }
										if (Keys.MergeEdgePoints.IsKeyPressed() && HaveEdgeSelection) { Event.current.Use(); break; }
										break;
									}

									case EventType.ValidateCommand:
									{
										if ((Event.current.commandName == "SoftDelete" || Event.current.commandName == "Delete") && HavePointSelection) { Event.current.Use(); break; }
										if (Keys.MergeEdgePoints.IsKeyPressed()) { Event.current.Use(); break; }
										break;
									}

									case EventType.KeyUp:
									{
										if (HavePointSelection && Keys.DeleteSelectionKey.IsKeyPressed()) { Event.current.Use(); DeleteSelectedPoints(); break; }
										if (Keys.MergeEdgePoints.IsKeyPressed()) { MergeSelected(); Event.current.Use(); break; }
										break;
									}
									
									case EventType.ExecuteCommand:
									{
										if (HavePointSelection &&
											(Event.current.commandName == "SoftDelete" || Event.current.commandName == "Delete"))
										{
											DeleteSelectedPoints(); Event.current.Use();
											break;
										}
										break;
									}

									case EventType.MouseDrag:
									{
										if (Event.current.button != 0)
											break;

										Event.current.Use();
										if (_editMode == EditMode.RotateEdge)
										{
											if (_rotateBrushParent == null)
												break;

											if ((CSGSettings.SnapRotation % 360) <= 0)
											{
												EditModeManager.ShowMessage("Rotational snapping is set to zero, cannot rotate.");
												break;
											}
											EditModeManager.ResetMessage();

											var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
											var newMousePos = _rotatePlane.RayIntersection(ray);

											_rotateCurrentAngle = GeometryUtility.SignedAngle(_rotateCenter - _rotateStart, _rotateCenter - newMousePos, _rotateNormal);
											_rotateCurrentSnappedAngle = GridUtility.SnappedAngle(_rotateCurrentAngle - _rotateStartAngle) + _rotateStartAngle;
												
											DoRotateBrushes(_rotateCenter, _rotateNormal, _rotateCurrentSnappedAngle - _rotateStartAngle);
											CSG_EditorGUIUtility.RepaintAll();
											break;
										}

										if (Tools.current != Tool.Move &&
											Tools.current != Tool.Scale)
											break;

										if (_firstMove)
										{
											_extraDeltaMovement = MathConstants.zeroVector3;
											_worldDeltaMovement = MathConstants.zeroVector3;
											_startCamera = camera;
											UpdateTransformMatrices();
											UpdateSelection(allowSubstraction: false);
										}
			
										if (//_prevYMode != RealtimeCSG.Grid.YMoveModeActive || 
											_firstMove)
										{
											//_prevYMode = RealtimeCSG.Grid.YMoveModeActive;
											//if (_firstMove)
											{
												var originalVertexIndex1 = meshState.Edges[(e * 2) + 0];
												var originalVertexIndex2 = meshState.Edges[(e * 2) + 1];
												var originalVertex1 = meshState.WorldPoints[originalVertexIndex1];
												var originalVertex2 = meshState.WorldPoints[originalVertexIndex2];

												var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

												float squaredDist, s;
												Vector3 closestRay;
												_originalPoint = MathUtils.ClosestPtSegmentRay(originalVertex1, originalVertex2, ray, out squaredDist, out s, out closestRay);
											}
											UpdateWorkControlMesh();
											UpdateBackupPoints();
											UpdateGrid(_startCamera);
											_firstMove = true;
											_extraDeltaMovement += _worldDeltaMovement;
										}

										var lockX = (CSGSettings.LockAxisX || (Mathf.Abs(_movePlane.a) >= 1 - MathConstants.EqualityEpsilon));
										var lockY = (CSGSettings.LockAxisY || (Mathf.Abs(_movePlane.b) >= 1 - MathConstants.EqualityEpsilon));
										var lockZ = (CSGSettings.LockAxisZ || (Mathf.Abs(_movePlane.c) >= 1 - MathConstants.EqualityEpsilon));					
			
										if (CSGSettings.SnapVector == MathConstants.zeroVector3)
										{
											EditModeManager.ShowMessage("Positional snapping is set to zero, cannot move.");
											break;
										} else
										if (lockX && lockY && lockZ)
										{
											EditModeManager.ShowMessage("All axi are disabled (X Y Z), cannot move.");
											break;
										}
										EditModeManager.ResetMessage();

										var mouseRay		= HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
										var intersection	= _movePlane.RayIntersection(mouseRay);
										if (float.IsNaN(intersection.x) || float.IsNaN(intersection.y) || float.IsNaN(intersection.z))
											break;

										intersection			= GeometryUtility.ProjectPointOnPlane(_movePlane, intersection);
											
										if (_firstMove)
										{
											_originalPoint = intersection;
											_worldDeltaMovement = MathConstants.zeroVector3;
											

											_handleWorldPoints = _brushSelection.GetSelectedWorldPoints(useBackupPoints: true);
											_dragEdgeScale = Vector3.one;
											_dragEdgeRotation = GetRealHandleRotation();

											var rotation			= _dragEdgeRotation;
											var inverseRotation		= Quaternion.Inverse(rotation);

											var delta		= (_originalPoint - _handleCenter);
											var distance	= delta.magnitude;
											_startHandleDirection = (delta / distance);
											_startHandleDirection = GeometryUtility.SnapToClosestAxis(inverseRotation * _startHandleDirection);
											_startHandleDirection = rotation * _startHandleDirection;
											
											_startHandleCenter = _handleCenter;

											_firstMove = false;
										} else
										{
											_worldDeltaMovement = SnapMovementToPlane(intersection - _originalPoint);
										}
			
										// try to snap selected points against non-selected points
										var activeSnappingMode = RealtimeCSG.CSGSettings.ActiveSnappingMode;
                                        switch (activeSnappingMode)
                                        {
                                            case SnapMode.GridSnapping:
                                            {
                                                var worldPoints = _handleWorldPoints;
                                                //for (int i = 0; i < worldPoints.Length; i++)
                                                //	worldPoints[i] = GeometryUtility.ProjectPointOnPlane(movePlane, worldPoints[i]);// - center));
                                                _worldDeltaMovement = RealtimeCSG.CSGGrid.SnapDeltaToGrid(camera, _worldDeltaMovement, worldPoints, snapToSelf: true);
                                                break;
                                            }
                                            case SnapMode.RelativeSnapping:
                                            {
                                                _worldDeltaMovement = RealtimeCSG.CSGGrid.SnapDeltaRelative(camera, _worldDeltaMovement);
                                                break;
                                            }
                                            default:
                                            case SnapMode.None:
                                            {
                                                _worldDeltaMovement = RealtimeCSG.CSGGrid.HandleLockedAxi(_worldDeltaMovement);
                                                break;
                                            }
                                        }

										if (Tools.current == Tool.Move)
										{
											DoMoveControlPoints(_worldDeltaMovement);
										}
										if (Tools.current == Tool.Scale)
										{
											var rotation			= _dragEdgeRotation;
											var inverseRotation		= Quaternion.Inverse(rotation);
											
											var start	= GeometryUtility.ProjectPointOnInfiniteLine(_originalPoint, _startHandleCenter, _startHandleDirection);
											var end		= GeometryUtility.ProjectPointOnInfiniteLine(intersection, _startHandleCenter, _startHandleDirection);
											
												
											var oldDistance	= inverseRotation * (start - _startHandleCenter);
											var newDistance	= inverseRotation * (end - _startHandleCenter);
											if (Mathf.Abs(oldDistance.x) > MathConstants.DistanceEpsilon) _dragEdgeScale.x = newDistance.x / oldDistance.x;
											if (Mathf.Abs(oldDistance.y) > MathConstants.DistanceEpsilon) _dragEdgeScale.y = newDistance.y / oldDistance.y;
											if (Mathf.Abs(oldDistance.z) > MathConstants.DistanceEpsilon) _dragEdgeScale.z = newDistance.z / oldDistance.z;
											
											if (float.IsNaN(_dragEdgeScale.x) || float.IsInfinity(_dragEdgeScale.x)) _dragEdgeScale.x = 1.0f;
											if (float.IsNaN(_dragEdgeScale.y) || float.IsInfinity(_dragEdgeScale.y)) _dragEdgeScale.y = 1.0f;
											if (float.IsNaN(_dragEdgeScale.z) || float.IsInfinity(_dragEdgeScale.z)) _dragEdgeScale.z = 1.0f;
											
											_dragEdgeScale.x = Mathf.Round(_dragEdgeScale.x / CSGSettings.SnapScale) * CSGSettings.SnapScale;
											_dragEdgeScale.y = Mathf.Round(_dragEdgeScale.y / CSGSettings.SnapScale) * CSGSettings.SnapScale;
											_dragEdgeScale.z = Mathf.Round(_dragEdgeScale.z / CSGSettings.SnapScale) * CSGSettings.SnapScale;

											DoScaleControlPoints(rotation, _dragEdgeScale, _startHandleCenter);
										}
										CenterPositionHandle();
										CSG_EditorGUIUtility.RepaintAll();
										break;
									}
									case EventType.MouseUp:
									{
										_movePlaneInNormalDirection = false;
			 
										_startCamera = null;
										RealtimeCSG.CSGGrid.ForceGrid = false;
										EditorGUIUtility.SetWantsMouseJumping(0);
										GUIUtility.hotControl = 0;
										GUIUtility.keyboardControl = 0;
										EditorGUIUtility.editingTextField = false;
										Event.current.Use();

										if (!_mouseIsDragging)
											break;

										if (_editMode == EditMode.RotateEdge)
										{
											Undo.CollapseUndoOperations(_rotationUndoGroupIndex);
											UpdateWorkControlMesh();
										}

										MergeDuplicatePoints();
										if (!UpdateWorkControlMesh(forceUpdate: true))
										{				
											Undo.PerformUndo();
										} else
										{
											UpdateBackupPoints();
										}
										InternalCSGModelManager.CheckSurfaceModifications(_brushSelection.Brushes, true);
										break;
									}
									case EventType.Repaint:
									{
										if (Tools.current == Tool.Move)
										{
											if (_editMode != EditMode.RotateEdge)
												RenderOffsetText();
										} else
										if (Tools.current == Tool.Scale)
										{
											if (_handleWorldPoints == null)
												return;

											var realScale = _dragEdgeScale;
											if (realScale.x < 0) realScale.x = 0;
											if (realScale.y < 0) realScale.y = 0;
											if (realScale.z < 0) realScale.z = 0;

											DrawScaleBounds(camera, _dragEdgeRotation, realScale, _startHandleCenter, _handleWorldPoints);
										}
										break;
									}
								}
								break;
							}
						}

						for (int p = 0; p < meshState.PolygonCenterPoints.Length; p++)
						{
							if (currentHotControl == meshState.PolygonControlId[p])
							{
								var type = Event.current.GetTypeForControl(meshState.PolygonControlId[p]);
								switch (type)
								{
									case EventType.KeyDown:
									{
										if (HavePointSelection && Keys.DeleteSelectionKey.IsKeyPressed()) { Event.current.Use(); break; }
										if (Keys.MergeEdgePoints.IsKeyPressed() && HaveEdgeSelection) { Event.current.Use(); break; }
										break;
									}

									case EventType.ValidateCommand:
									{
										if ((Event.current.commandName == "SoftDelete" || Event.current.commandName == "Delete") && HavePointSelection) { Event.current.Use(); break; }
										if (Keys.MergeEdgePoints.IsKeyPressed()) { Event.current.Use(); break; }
										break;
									}

									case EventType.KeyUp:
									{
										if (HavePointSelection && Keys.DeleteSelectionKey.IsKeyPressed()) { Event.current.Use(); DeleteSelectedPoints(); break; }
										if (Keys.MergeEdgePoints.IsKeyPressed()) { MergeSelected(); Event.current.Use(); break; }
										break;
									}
									
									case EventType.ExecuteCommand:
									{
										if (HavePointSelection &&
											(Event.current.commandName == "SoftDelete" || Event.current.commandName == "Delete"))
										{
											DeleteSelectedPoints(); Event.current.Use();
											break;
										}
										break;
									}

									case EventType.MouseDrag:
									{					
										if (Event.current.button != 0)
											break;

										Event.current.Use();
										
										if (_firstMove)
										{
											EditorGUIUtility.SetWantsMouseJumping(1);
											_mousePosition = Event.current.mousePosition;
											_extraDeltaMovement = MathConstants.zeroVector3;
											_worldDeltaMovement = MathConstants.zeroVector3;
											_startCamera = camera;
											UpdateTransformMatrices();
											UpdateSelection(allowSubstraction: false);
										} else
										{
											_mousePosition += Event.current.delta;
										}
			
										if (//_prevYMode != RealtimeCSG.Grid.YMoveModeActive || 
												_firstMove)
										{
											//_prevYMode = RealtimeCSG.Grid.YMoveModeActive;
											if (_firstMove)
											{
												_originalPoint = meshState.PolygonCenterPoints[p];
												//UpdateWorkControlMesh();
											}
											UpdateBackupPoints();
											UpdateGrid(_startCamera);
											_firstMove = true;
											_extraDeltaMovement += _worldDeltaMovement;
										}
			
										var lockX = (CSGSettings.LockAxisX || (Mathf.Abs(_movePlane.a) >= 1 - MathConstants.EqualityEpsilon));
										var lockY = (CSGSettings.LockAxisY || (Mathf.Abs(_movePlane.b) >= 1 - MathConstants.EqualityEpsilon));
										var lockZ = (CSGSettings.LockAxisZ || (Mathf.Abs(_movePlane.c) >= 1 - MathConstants.EqualityEpsilon));					
			
										if (CSGSettings.SnapVector == MathConstants.zeroVector3)
										{
											EditModeManager.ShowMessage("Positional snapping is set to zero, cannot move.");
											break;
										} else
										if (lockX && lockY && lockZ)
										{
											EditModeManager.ShowMessage("All axi are disabled (X Y Z), cannot move.");
											break;
										}
										EditModeManager.ResetMessage();

										var mouseRay		= HandleUtility.GUIPointToWorldRay(_mousePosition);
										var intersection	= _movePlane.RayIntersection(mouseRay);
										if (float.IsNaN(intersection.x) || float.IsNaN(intersection.y) || float.IsNaN(intersection.z))
											break;
										
										if (_movePlaneInNormalDirection && _editMode != EditMode.ScalePolygon)
										{
											intersection	= GridUtility.CleanPosition(intersection);
											intersection	= GeometryUtility.ProjectPointOnInfiniteLine(intersection, _movePolygonOrigin, _movePolygonDirection);
										} else
										{
											intersection	= GeometryUtility.ProjectPointOnPlane(_movePlane, intersection);
										}

										if (_firstMove)
										{
											_originalPoint = intersection;
											_worldDeltaMovement = MathConstants.zeroVector3;
											_firstMove = false;
										} else
										{
											_worldDeltaMovement = SnapMovementToPlane(intersection - _originalPoint);
										}

										// try to snap selected points against non-selected points
										var activeSnappingMode = RealtimeCSG.CSGSettings.ActiveSnappingMode;
                                        switch (activeSnappingMode)
                                        {
                                            case SnapMode.GridSnapping:
                                            {
                                                var worldPoints = _brushSelection.GetSelectedWorldPoints(useBackupPoints: true);
                                                if (_movePlaneInNormalDirection && _editMode != EditMode.ScalePolygon)
                                                {
                                                    var worldLineOrg = _movePolygonOrigin;
                                                    var worldLineDir = _movePolygonDirection;
                                                    _worldDeltaMovement = RealtimeCSG.CSGGrid.SnapDeltaToRayGrid(camera, new Ray(worldLineOrg, worldLineDir), _worldDeltaMovement, worldPoints);
                                                } else
                                                {
                                                    //for (int i = 0; i < worldPoints.Length; i++)
                                                    //	worldPoints[i] = GeometryUtility.ProjectPointOnPlane(movePlane, worldPoints[i]);// - center));
                                                    _worldDeltaMovement = RealtimeCSG.CSGGrid.SnapDeltaToGrid(camera, _worldDeltaMovement, worldPoints, snapToSelf: true);
                                                }
                                                break;
                                            }
                                            case SnapMode.RelativeSnapping:
                                            {
                                                if (_movePlaneInNormalDirection && _editMode != EditMode.ScalePolygon)
                                                {
                                                    var worldLineOrg = _movePolygonOrigin;
                                                    var worldLineDir = _movePolygonDirection;
                                                    _worldDeltaMovement = RealtimeCSG.CSGGrid.SnapDeltaToRayRelative(camera, new Ray(worldLineOrg, worldLineDir), _worldDeltaMovement);
                                                } else
                                                {
                                                    _worldDeltaMovement = RealtimeCSG.CSGGrid.SnapDeltaRelative(camera, _worldDeltaMovement);
                                                }
                                                break;
                                            }
                                            default:
                                            case SnapMode.None:
                                            {
                                                _worldDeltaMovement = RealtimeCSG.CSGGrid.HandleLockedAxi(_worldDeltaMovement);
                                                break;
                                            }
                                        }

										switch (_editMode)
										{
											case EditMode.MovingPolygon: DoMoveControlPoints(_worldDeltaMovement); break;
										//	case EditMode.ScalePolygon:  DoScaleControlPoints(worldDeltaMovement, meshState.polygonCenterPoints[p]); break;
										}
										CenterPositionHandle();
										CSG_EditorGUIUtility.RepaintAll();
										break;
									}
									case EventType.MouseUp:
									{
										_movePlaneInNormalDirection = false;
									
										_startCamera = null;
										RealtimeCSG.CSGGrid.ForceGrid = false;

										EditorGUIUtility.SetWantsMouseJumping(0);
										GUIUtility.hotControl = 0;
										GUIUtility.keyboardControl = 0;
										EditorGUIUtility.editingTextField = false;
										Event.current.Use();

										if (!_mouseIsDragging)
											break;

										MergeDuplicatePoints();
										if (!UpdateWorkControlMesh())
										{				
											Undo.PerformUndo();
										} else
										{
											if (_editMode == EditMode.ScalePolygon)
											{
												_brushSelection.Brushes = null;
												SetTargets(EditModeManager.FilteredSelection); 
											} else
												UpdateBackupPoints();
										}
										break;
									}
									case EventType.Repaint:
									{
										if (_editMode != EditMode.ScalePolygon)
											RenderOffsetText();
										break;
									}
								}
								break;
							}
						}
					}
				}
			}
			finally
			{ 
				if (originalEventType == EventType.MouseUp) { _mouseIsDragging = false; }
			}
		}


































		
		static Vector2 scrollPos;
		public void OnInspectorGUI(EditorWindow window, float height)
		{
			scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
			{
				EditModeMeshModeGUI.OnInspectorGUI(window, height);
			}
			EditorGUILayout.EndScrollView();
		}
		
		public Rect GetLastSceneGUIRect()
		{
			return EditModeMeshModeGUI.GetLastSceneGUIRect(this);
		}

		public bool OnSceneGUI(Rect windowRect)
		{
			if (_brushSelection.Brushes == null)
				return false;

			EditModeMeshModeGUI.OnSceneGUI(windowRect, this);
			return true;
		}
	}
}
