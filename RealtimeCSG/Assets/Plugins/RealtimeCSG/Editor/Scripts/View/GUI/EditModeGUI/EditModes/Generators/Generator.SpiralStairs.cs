using System;
using UnityEditor;
using UnityEngine;
using InternalRealtimeCSG;
using RealtimeCSG.Helpers;
using RealtimeCSG.Legacy;
using RealtimeCSG.Components;


/*
 
	inner radius
	step width
	step height
	step thickness
	num steps per 360
	num steps



*/



// TODO: 
//	test on non straight walls/floors
//  optional one step less in height (so that the last step is on the wall it's put against
//	make it possible to move steps in slope direction
//	update key layout on website
//	update documentation
//	create icons
//	fix snapping on edges of other brushes
//	fix shouldn't snap on backfaced edges of other brushes
//	fix remaining UNDO issues

namespace RealtimeCSG
{
	[Serializable]
	internal sealed class GeneratorSpiralStairs : GeneratorBase, IGenerator
	{
		const int	kMaxPoints		= 3;
		
		protected override IShapeSettings ShapeSettings { get { return settings; } }

		protected override bool IgnoreForcedGridForTangents { get { return true; } }
		
		[SerializeField] GeneratorSpiralStairsSettings settings = new GeneratorSpiralStairsSettings();
		


		[NonSerialized] Vector3		worldPosition;
		[NonSerialized] Vector3		prevWorldPosition;
		[NonSerialized] CSGPlane?	hoverDefaultPlane;
		
		[NonSerialized] CSGPlane[]	firstSnappedPlanes	= null;
		[NonSerialized] Vector3[]	firstSnappedEdges	= null;
		[NonSerialized] CSGBrush	firstSnappedBrush	= null;

		[SerializeField] Vector3 widthDirection;
		[SerializeField] Vector3 lengthDirection;
		[SerializeField] Vector3 heightDirection;
		

		internal static readonly int TopSlopLeftHash	    = "TopSlopLeft".GetHashCode();
		internal static readonly int TopSlopRightHash	    = "TopSlopRight".GetHashCode();
		internal static readonly int BottomSlopLeftHash	    = "BottomSlopLeft".GetHashCode();
		internal static readonly int BottomSlopRightHash	= "BottomSlopRight".GetHashCode();
		
		int topSlopeLeftID;
		int topSlopeRightID;

		int bottomSlopeLeftID;
		int bottomSlopeRightID;


		#region Settings
		public float StepDepth
		{
			get { return settings.StepDepth; }
			set
			{
				if (settings.StepDepth == value)
					return;
				
				Undo.RecordObject(this, "Modified Spiral Stairs Step Depth");
				settings.SetStepDepth(value);
				UpdateBaseShape(true);
			}
		}

		public float StepHeight
		{
			get { return settings.StepHeight; }
			set
			{
				if (settings.StepHeight == value)
					return;
				Undo.RecordObject(this, "Modified Spiral Stairs Step Height");
				settings.SetStepHeight(value);
				UpdateBaseShape(true);
			}
		}
		
		public int TotalSteps
		{
			get { return settings.TotalSteps; }
			set
			{
				if (settings.TotalSteps == value)
					return;
				Undo.RecordObject(this, "Modified Spiral Stairs Total Steps");
				settings.SetTotalSteps(value);
				UpdateBaseShape(true);
			}
		}
				
		public float	StairsWidth
		{
			get { return settings.StairsWidth; }
			set
			{
				if (settings.StairsWidth == value)
					return;
								
				Undo.RecordObject(this, "Modified Spiral Stairs Width");
				settings.SetStairsWidth(value);
				if (editMode == EditMode.ExtrudeShape || editMode == EditMode.EditShape)
				{
					var newPosition = settings.vertices[0] + (settings.StairsDepth * lengthDirection) + (value * widthDirection);
					settings.SetPoint(1, newPosition);
					UpdateBaseShape(true);
				}
			}
		}

		public float	StairsDepth
		{
			get { return settings.StairsDepth; }
			set
			{
				if (settings.StairsDepth == value)
					return;

				Undo.RecordObject(this, "Modified Spiral Stairs Depth");
				settings.SetStairsDepth(value);
				if (editMode == EditMode.ExtrudeShape || 
					editMode == EditMode.EditShape)
				{
					var newPosition = settings.vertices[0] + (value * lengthDirection) + (settings.StairsWidth * widthDirection);
					settings.SetPoint(1, newPosition);
					UpdateBaseShape(true);
				}
			}
		}		

		public float	StairsHeight
		{
			get { return settings.StairsHeight; }
			set
			{
				if (settings.StairsDepth == value)
					return;

				Undo.RecordObject(this, "Modified Spiral Stairs Height");
				settings.SetStairsHeight(value);
				if (editMode == EditMode.ExtrudeShape || editMode == EditMode.EditShape)
				{
					var newPosition = settings.vertices[1] + (value * heightDirection);
					settings.SetPoint(2, newPosition);
					UpdateBaseShape(true);
				}
			}
		}

		public float ExtraDepth
		{
			get
			{
				var floatTotalSteps = (Mathf.Max(0, settings.StairsHeight - settings.ExtraHeight) / settings.StepHeight);
				return Mathf.Max(0, settings.StairsDepth - (floatTotalSteps * settings.StepDepth));
			}
			set
			{
				if (settings.ExtraDepth == value)
					return;

				Undo.RecordObject(this, "Modified Spiral Stairs Extra Depth");
				settings.SetExtraDepth(value);
				
				if (editMode != EditMode.CreatePlane &&
					settings.vertices.Length > 1)
					UpdateBaseShape(true);
			}
		}

		public float ExtraHeight
		{
			get { return settings.ExtraHeight; }
			set
			{
				if (settings.ExtraHeight == value)
					return;
				
				Undo.RecordObject(this, "Modified Spiral Stairs Extra Height");
				settings.SetExtraHeight(value);

				if (editMode != EditMode.CreatePlane &&
					settings.vertices.Length > 1)
					UpdateBaseShape(true);
			}
		}
		#endregion

		#region Glue
		public override void PerformDeselectAll() { Cancel(); }
		public override void PerformDelete() { Cancel(); }

		public override void Init() { base.Init(); Reset(); }

		public override void Reset() 
		{
			settings.Reset();
			base.Reset();
			hoverDefaultPlane	= null;
			firstSnappedPlanes	= null;
			firstSnappedEdges	= null;
			firstSnappedBrush	= null;
		}

		public bool HotKeyReleased()
		{
            var camera = Camera.current;
			ResetVisuals();
			RealtimeCSG.CSGGrid.SetForcedGrid(camera, new CSGPlane(fromGridQuaternion * MathConstants.upVector3, settings.bounds.Min));
			switch (editMode)
			{
				default:
				{
					return true;
				}
				case EditMode.CreateShape:
				{
					if (settings.vertices.Length == 1)
					{
						Cancel();
						return false;
					}

					if (settings.vertices.Length == 2)
					{
						settings.AddPoint(worldPosition);
					}
					
					if (settings.vertices.Length == 3)
					{
						settings.bounds = GetShapeBounds();
					}
					
					if ((settings.vertices[0] - settings.vertices[1]).sqrMagnitude <= MathConstants.EqualityEpsilon)
					{
						Cancel();
						return false;
					}
					StartEditMode(camera);
					return true;
				}
				case EditMode.CreatePlane:
				{
					Cancel();
					return false;
				}
			}
		}

		void IGenerator.OnDefaultMaterialModified()
		{
			if (generatedBrushes == null)
				return;

			var defaultMaterial = CSGSettings.DefaultMaterial;
			for (int i = 0; i < generatedBrushes.Length; i++)
			{
				var brush = generatedBrushes[i];
				if (!brush)
					continue;

				var shape = brush.Shape;
				for (var m = 0; m < shape.TexGens.Length; m++)
					shape.TexGens[m].RenderMaterial = defaultMaterial;

				if (brush.ControlMesh != null)
					brush.ControlMesh.SetDirty();
			}
		}
		#endregion

		#region Orientation, Location and Bounds
		protected override void MoveShape(Vector3 offset)
		{
			settings.MoveShape(offset);
		}
		
		public Quaternion GetPlaneRotation()
		{
			var xznormal		= buildPlane.normal;
			if (xznormal.y < 0.5f)
			{
				xznormal.y = 0;
				xznormal.Normalize();
				return Quaternion.LookRotation(xznormal, MathConstants.upVector3);
			}

			return Quaternion.identity;
		}

		public Quaternion GetWorldToLocalRotation()
		{
			var invRotation	= Quaternion.Inverse(GetPlaneRotation());
			return toGridQuaternion * invRotation;
		}

		public override AABB GetShapeBounds()
		{
			var worldToLocalRotation = GetWorldToLocalRotation();
			return GetShapeBounds(worldToLocalRotation);
		}
		
		public AABB GetShapeBounds(Quaternion rotation)
		{
			if (editMode == EditMode.ExtrudeShape ||
				 editMode == EditMode.EditShape)
				return settings.bounds;

			var bounds = ShapeSettings.CalculateBounds(rotation, gridTangent, gridBinormal);
			if (settings.vertices.Length < 3)
				bounds.Extend(rotation * worldPosition);
			return bounds;
		}
		#endregion
		

		public bool OnShowGUI(bool isSceneGUI) { return GeneratorSpiralStairsGUI.OnShowGUI(this, isSceneGUI);	}



		void CreateSnappedPlanes()
		{
			firstSnappedPlanes = new CSGPlane[firstSnappedEdges.Length / 2];

			for (int i = 0; i < firstSnappedEdges.Length; i += 2)
			{
				var point0 = firstSnappedEdges[i + 0];
				var point1 = firstSnappedEdges[i + 1];

				var binormal = (point1 - point0).normalized;
				var tangent  = buildPlane.normal;
				var normal	 = Vector3.Cross(binormal, tangent);

				var worldPlane	= new CSGPlane(normal, point0);
				// note, we use 'inverse' of the worldToLocalMatrix because to transform a plane we'd need to do an inverse, 
				// and using the already inversed matrix we don't need to do a costly inverse.
				var localPlane		= GeometryUtility.InverseTransformPlane(firstSnappedBrush.transform.localToWorldMatrix, worldPlane);
				var	vertices		= firstSnappedBrush.ControlMesh.Vertices;
				var planeIsInversed	= false;
				for (int v = 0; v < vertices.Length; v++)
				{
					if (localPlane.Distance(vertices[v]) > MathConstants.DistanceEpsilon)
					{
						planeIsInversed = true;
						break;
					}
				}
				if (planeIsInversed)
					firstSnappedPlanes[i / 2] = worldPlane.Negated();
				else
					firstSnappedPlanes[i / 2] = worldPlane;
			}
		}


		#region Paint Helpers

		void PaintCircle()
		{
			if (settings.vertices.Length < 1)
				return;

			var circleCenter	= settings.vertices[0];
			var circleRadius	= settings.vertices.Length >= 2 ? settings.vertices[1] : worldPosition;
			var delta			= circleRadius - circleCenter;
			var radius			= delta.magnitude;
			
			var color = ColorSettings.MeshEdgeOutline;

			PaintUtility.DrawCircle(circleCenter, buildPlane.normal, gridTangent, radius, color);

			/*
			var projected_width  = Vector3.Project(delta, gridTangent);
			var projected_length = Vector3.Project(delta, gridBinormal);

			var point0 = circleCenter + projected_width + projected_length;
			var point1 = circleCenter + projected_width;
			var point2 = circleCenter;
			var point3 = circleCenter + projected_length;

			var points = new Vector3[] { point0, point1, point1, point2, point2, point3, point3, point0 };

			//PaintUtility.DrawPolygon(MathConstants.identityMatrix, points, color);
			*/
		}

		void PaintBounds(Camera camera)
		{
			var worldToLocalRotation	= GetWorldToLocalRotation();
			var localToWorldRotation	= Quaternion.Inverse(worldToLocalRotation);
			var localBounds				= GetShapeBounds(worldToLocalRotation);
			
			var volume = new Vector3[8];
			BoundsUtilities.GetBoundsCornerPoints(localBounds, volume);
			
			var boundOutlinesColor	= ColorSettings.MeshEdgeOutline;
			var localToWorld = Matrix4x4.TRS(Vector3.zero, localToWorldRotation, Vector3.one);
			PaintUtility.DrawDottedLines(localToWorld, volume, BoundsUtilities.AABBLineIndices, boundOutlinesColor, 4.0f);
			PaintUtility.DrawLines(localToWorld, volume, BoundsUtilities.AABBLineIndices, GUIConstants.oldLineScale, boundOutlinesColor);
			
			PaintUtility.RenderBoundsSizes(worldToLocalRotation, localToWorldRotation, camera, volume, Color.white, Color.white, Color.white, true, true, true);
		}		

		void PaintShape(SceneView sceneView, int id)
		{
            var camera      = sceneView.camera;
            var rotation	= camera.transform.rotation;
			
			var temp		= Handles.color;
			var origMatrix	= Handles.matrix;
			{
				Handles.matrix = MathConstants.identityMatrix;

				Handles.color = ColorSettings.PointInnerStateColor[3];
				{
					float handleSize = CSG_HandleUtility.GetHandleSize(worldPosition);
					float scaledHandleSize = handleSize * GUIConstants.handleScale;
					PaintUtility.SquareDotCap(id, worldPosition, rotation, scaledHandleSize);
				}

				if (settings.vertices != null && 
					settings.vertices.Length > 0)
				{
					Handles.color = ColorSettings.PointInnerStateColor[0];
					for (int i = 0; i < settings.vertices.Length; i++)
					{
						float handleSize = CSG_HandleUtility.GetHandleSize(settings.vertices[i]);
						float scaledHandleSize = handleSize * GUIConstants.handleScale;
						PaintUtility.SquareDotCap(id, settings.vertices[i], rotation, scaledHandleSize);
					}

					PaintCircle();
					PaintBounds(camera);
				}
			}

			Handles.matrix = origMatrix;
			Handles.color = temp;
		}
		#endregion


		public override bool Commit(Camera camera)
		{
			isFinished = true;
			CleanupGrid();

			if (!UpdateBaseShape(true))
			{
				Cancel();
				return false;
			}

			EndCommit();
			return true;
		}

		#region Events
				
		protected override void CreateControlIDs()
		{
			base.CreateControlIDs();

			topSlopeLeftID = GUIUtility.GetControlID(TopSlopLeftHash, FocusType.Keyboard);
			topSlopeRightID = GUIUtility.GetControlID(TopSlopRightHash, FocusType.Keyboard);

			bottomSlopeLeftID = GUIUtility.GetControlID(BottomSlopLeftHash, FocusType.Keyboard);
			bottomSlopeRightID = GUIUtility.GetControlID(BottomSlopRightHash, FocusType.Keyboard);
			
			if (settings.vertices.Length > 0)
			{
				if (settings.vertexIDs == null ||
					settings.vertexIDs.Length != settings.vertices.Length)
					settings.vertexIDs = new int[settings.vertices.Length];
				for (int i = 0; i < settings.vertices.Length; i++)
				{
					settings.vertexIDs[i] = GUIUtility.GetControlID(ShapeBuilderPointHash, FocusType.Passive);
				}
			}
		}
		
		public override void HandleEvents(SceneView sceneView, Rect sceneRect)
		{
			base.HandleEvents(sceneView, sceneRect);

			if (Event.current.type == EventType.MouseUp)
			{
				if (GUIUtility.hotControl == 0 &&
					Event.current.modifiers == EventModifiers.None &&
					!mouseIsDragging && Event.current.button == 0 &&
					(Tools.viewTool == ViewTool.None || Tools.viewTool == ViewTool.Pan))
				{
					ResetVisuals();
					Event.current.Use();
					Commit(sceneView.camera);
				}
			}
		}
		

		protected override void HandleEditShapeEvents(SceneView sceneView, Rect sceneRect)
		{
            var camera      = sceneView.camera;
			var origMatrix  = Handles.matrix;
			var origColor   = Handles.color;
			Handles.matrix  = Matrix4x4.identity;

			if (settings.vertices.Length < kMaxPoints)
			{
				if (editMode == EditMode.ExtrudeShape ||
					editMode == EditMode.EditShape)
					editMode = EditMode.CreatePlane;
			}
			EditorGUI.BeginChangeCheck();
			{
				var worldToLocalRotation = GetWorldToLocalRotation();
				var localToWorldRotation = Quaternion.Inverse(worldToLocalRotation);
				var newBounds = settings.bounds;
								
				widthDirection	= fromGridQuaternion * MathConstants.rightVector3;
				heightDirection	= fromGridQuaternion * MathConstants.upVector3;
				lengthDirection	= fromGridQuaternion * MathConstants.forwardVector3;
			
				var lengthOffset = ExtraDepth;
				var heightOffset = ExtraHeight;
				
				EditorGUI.BeginChangeCheck();
				{
					newBounds = CSGHandles.Box(camera, newBounds, worldToLocalRotation, showEdgePoints: false);
				}
				if (EditorGUI.EndChangeCheck())
				{
					Undo.RecordObject(this, "Modified Bounds"); 
					settings.SetBounds(newBounds);
				}

				var width	= settings.StairsWidth;
				var height	= settings.StairsHeight;
				var length	= settings.StairsDepth;
				
				brushRotation = GetPlaneRotation();
				brushPosition = localToWorldRotation * settings.bounds.Min;


				
				var activeSnappingMode = RealtimeCSG.CSGSettings.ActiveSnappingMode;


				var nearestControl = HandleUtility.nearestControl;
				var hotControl = GUIUtility.hotControl;

				var localToWorld = Matrix4x4.TRS(brushPosition, localToWorldRotation, Vector3.one);
				var worldToLocal = Matrix4x4.Inverse(localToWorld);
				
				var topFrom		= new Vector3(             0, height, lengthOffset);
				var topTo		= new Vector3( width        , height, lengthOffset);
								
				var bottomFrom	= new Vector3(             0, heightOffset, length);
				var bottomTo	= new Vector3( width        , heightOffset, length);


				var topSelectState	= ((hotControl     == topSlopeLeftID) || (hotControl     == topSlopeRightID)) ? SelectState.Selected :
									  ((nearestControl == topSlopeLeftID) || (nearestControl == topSlopeRightID)) ? SelectState.Hovering : SelectState.None;
				var topColor		= ColorSettings.PointInnerStateColor[(int)topSelectState];

				PaintUtility.DrawLine(localToWorld, topFrom, topTo, GUIConstants.oldThickLineScale, topColor);
				
				var bottomSelectState = ((hotControl     == bottomSlopeLeftID) || (hotControl     == bottomSlopeRightID)) ? SelectState.Selected :
										((nearestControl == bottomSlopeLeftID) || (nearestControl == bottomSlopeRightID)) ? SelectState.Hovering : SelectState.None;
				var bottomColor		= ColorSettings.PointInnerStateColor[(int)bottomSelectState];
				PaintUtility.DrawLine(localToWorld, bottomFrom, bottomTo, GUIConstants.oldThickLineScale, bottomColor);
				
				
				PaintUtility.DrawLine(localToWorld, topFrom, bottomFrom, GUIConstants.oldThickLineScale, ColorSettings.MeshEdgeOutline);
				PaintUtility.DrawLine(localToWorld, topTo  , bottomTo,   GUIConstants.oldThickLineScale, ColorSettings.MeshEdgeOutline);
				PaintUtility.DrawDottedLine(localToWorld, topFrom, bottomFrom, ColorSettings.MeshEdgeOutline, 4.0f);
				PaintUtility.DrawDottedLine(localToWorld, topTo, bottomTo, ColorSettings.MeshEdgeOutline, 4.0f);

				float kSlopeDotSize = 0.25f;
				
				settings.CalcTotalSteps();
				{ 
					var topDirection	= localToWorld.MultiplyVector( MathConstants.forwardVector3 );
					Handles.color = topColor;

					EditorGUI.BeginChangeCheck();
					{
						var point	= localToWorld.MultiplyPoint(new Vector3( width * 0.25f, height, lengthOffset));
						var size	= HandleUtility.GetHandleSize(point) * kSlopeDotSize;
						var temp	= CSGSlider1D.Do(camera, topSlopeLeftID, point, topDirection, size, CSGHandles.CircleDotCap, activeSnappingMode, null, null, null);
						lengthOffset = worldToLocal.MultiplyPoint(temp).z;
					}
					if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(this, "Modified Length"); settings.SetExtraDepth(lengthOffset); }
								
					EditorGUI.BeginChangeCheck();
					{
						var point	= localToWorld.MultiplyPoint(new Vector3( width * 0.75f, height, lengthOffset));
						var size	= HandleUtility.GetHandleSize(point) * kSlopeDotSize;
						var temp	= CSGSlider1D.Do(camera, topSlopeRightID, point, topDirection, size, CSGHandles.CircleDotCap, activeSnappingMode, null, null, null);
						lengthOffset = worldToLocal.MultiplyPoint(temp).z;
					}
					if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(this, "Modified Length"); settings.SetExtraDepth(lengthOffset); }
				}

				{ 		
					var bottomDirection	= localToWorld.MultiplyVector( MathConstants.upVector3 );

					Handles.color = bottomColor;
					EditorGUI.BeginChangeCheck();
					{
						var point	= localToWorld.MultiplyPoint(new Vector3( width * 0.25f, heightOffset, length));
						var size	= HandleUtility.GetHandleSize(point) * kSlopeDotSize;
						var temp	= CSGSlider1D.Do(camera, bottomSlopeLeftID, point, bottomDirection, size, CSGHandles.CircleDotCap, activeSnappingMode, null, null, null);
						heightOffset = worldToLocal.MultiplyPoint(temp).y;
					}
					if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(this, "Modified Height"); settings.SetExtraHeight(heightOffset); }

					EditorGUI.BeginChangeCheck();
					{
						var point	= localToWorld.MultiplyPoint(new Vector3( width * 0.75f, heightOffset, length));
						var size	= HandleUtility.GetHandleSize(point) * kSlopeDotSize;
						var temp	= CSGSlider1D.Do(camera, bottomSlopeRightID, point, bottomDirection, size, CSGHandles.CircleDotCap, activeSnappingMode, null, null, null);
						heightOffset = worldToLocal.MultiplyPoint(temp).y;
					}
					if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(this, "Modified Height"); settings.SetExtraHeight(heightOffset); }
				}
			}
			if (EditorGUI.EndChangeCheck())
			{
				UpdateBaseShape(true);
			}
			
			Handles.matrix = origMatrix;
			Handles.color = origColor;
		}

		void UpdateSizes()
		{
			if (settings.vertices.Length == 0)
				return;
			
			widthDirection	= fromGridQuaternion * MathConstants.rightVector3;
			heightDirection	= fromGridQuaternion * MathConstants.upVector3;
			lengthDirection	= fromGridQuaternion * MathConstants.forwardVector3;
			
			var worldToLocalRotation	= GetWorldToLocalRotation();
			var localToWorldRotation	= Quaternion.Inverse(worldToLocalRotation);
			var localBounds				= GetShapeBounds(worldToLocalRotation);

			settings.SetBounds(localBounds);

			brushRotation = GetPlaneRotation();
			brushPosition = localToWorldRotation * localBounds.Min;
		}


		protected override void HandleCreateShapeEvents(SceneView sceneView, Rect sceneRect)
		{
			bool		pointOnEdge			= false;
			bool		havePlane			= false;
			bool		vertexOnGeometry	= false;
			CSGBrush	vertexOnBrush		= null;
			
			CSGPlane	hoverBuildPlane		= buildPlane;
            var camera = sceneView.camera;
			if (camera != null &&
				camera.pixelRect.Contains(Event.current.mousePosition))
			{
				if (!hoverDefaultPlane.HasValue ||
					settings.vertices.Length == 0)
				{
					bool forceGrid = RealtimeCSG.CSGGrid.ForceGrid;
					RealtimeCSG.CSGGrid.ForceGrid = false;
					hoverDefaultPlane = RealtimeCSG.CSGGrid.CurrentGridPlane;
					RealtimeCSG.CSGGrid.ForceGrid = forceGrid;
					firstSnappedEdges = null;
					firstSnappedBrush = null;
					firstSnappedPlanes = null;
					base.geometryModel = null;
				}
				if (editMode == EditMode.CreatePlane)
				{
					LegacyBrushIntersection intersection;
					if (!IgnoreDepthForRayCasts(camera) && !havePlane &&
                        EditorWindow.mouseOverWindow == sceneView &&
                        SceneQueryUtility.FindWorldIntersection(camera, Event.current.mousePosition, out intersection))
					{
						worldPosition = intersection.worldIntersection;
						hoverBuildPlane = intersection.worldPlane;
						vertexOnBrush = intersection.brush;

						vertexOnGeometry = true;
					} else
					{
						hoverBuildPlane = hoverDefaultPlane.Value;
						vertexOnBrush = null;

						var mouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
						worldPosition = hoverBuildPlane.RayIntersection(mouseRay);
						vertexOnGeometry = false;
					}
					ResetVisuals();
					if (snapFunction != null)
					{
						CSGBrush snappedOnBrush;
						worldPosition = snapFunction(camera, worldPosition, hoverBuildPlane, ref visualSnappedEdges, out snappedOnBrush, generatedBrushes, ignoreAllBrushes: true);
						if (snappedOnBrush != null)
						{
							pointOnEdge = (visualSnappedEdges != null &&
									  visualSnappedEdges.Count > 0);
							vertexOnBrush = snappedOnBrush;
							vertexOnGeometry = true;
						}
					}

					if (settings.vertices.Length == 1)
					{
						if (hoverBuildPlane.normal != MathConstants.zeroVector3)
						{
							editMode = EditMode.CreateShape;
							havePlane = true;
						}
					}
				} else
				{
					var mouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
					if (settings.vertices.Length == 2)
					{
						var startPoint = settings.vertices[1];

						var forward = camera.transform.forward;
						if (Vector3.Dot(forward, gridBinormal) < Vector3.Dot(forward, gridTangent))
							hoverBuildPlane = new CSGPlane(gridBinormal, startPoint);
						else
							hoverBuildPlane = new CSGPlane(gridTangent, startPoint);
						worldPosition = hoverBuildPlane.RayIntersection(mouseRay);

						// the third point is always straight up from the second point
						//worldPosition = startPoint + (Vector3.Dot(worldPosition - startPoint, gridNormal) * gridNormal);
						
						RealtimeCSG.CSGGrid.SetForcedGrid(camera, hoverBuildPlane);
						ResetVisuals();
						if (raySnapFunction != null)
						{
							Ray ray = new Ray(startPoint, gridNormal);
							CSGBrush snappedOnBrush;
							worldPosition = raySnapFunction(camera, worldPosition, ray, ref visualSnappedEdges, out snappedOnBrush);
							if (snappedOnBrush != null)
							{
								pointOnEdge = (visualSnappedEdges != null &&
												visualSnappedEdges.Count > 0);
								vertexOnBrush = snappedOnBrush;
							}
						}

						worldPosition = GeometryUtility.ProjectPointOnInfiniteLine(worldPosition, startPoint, gridNormal);
					} else
					{
						worldPosition = hoverBuildPlane.RayIntersection(mouseRay);
						
						RealtimeCSG.CSGGrid.SetForcedGrid(camera, hoverBuildPlane);
						ResetVisuals();
						if (snapFunction != null)
						{
							CSGBrush snappedOnBrush;
							worldPosition = snapFunction(camera, worldPosition, hoverBuildPlane, ref visualSnappedEdges, out snappedOnBrush, generatedBrushes, ignoreAllBrushes: true);
							if (snappedOnBrush != null)
							{
								pointOnEdge = (visualSnappedEdges != null &&
												visualSnappedEdges.Count > 0);
								vertexOnBrush = snappedOnBrush;
							}
						}
					}
				}

				if (geometryModel == null && vertexOnBrush != null)
				{
					if (vertexOnBrush.ChildData != null && vertexOnBrush.ChildData.Model)
						geometryModel = vertexOnBrush.ChildData.Model;
				}

				if (worldPosition != prevWorldPosition)
				{
					prevWorldPosition = worldPosition;
					if (settings.vertices.Length > 0)
					{
						if ((settings.vertices[0] - worldPosition).sqrMagnitude > MathConstants.EqualityEpsilon)
						{
							UpdateSizes();
							UpdateBaseShape(true);
						}
					}
					if (Event.current.type != EventType.Repaint)
						CSG_EditorGUIUtility.RepaintAll();
				}
				
				visualSnappedGrid = RealtimeCSG.CSGGrid.FindAllGridEdgesThatTouchPoint(camera, worldPosition);
				visualSnappedBrush = vertexOnBrush;
			}
			RealtimeCSG.CSGGrid.SetForcedGrid(camera, hoverBuildPlane);
			
			

			if (!SceneDragToolManager.IsDraggingObjectInScene &&
				Event.current.type == EventType.Repaint)
			{
				PaintSnapVisualisation();
				PaintShape(sceneView, base.shapeId);
			}


			var type = Event.current.GetTypeForControl(base.shapeId);
			switch (type)
			{
				case EventType.Layout:
				{
					return;
				}

				case EventType.ValidateCommand:
				case EventType.KeyDown:
				{
					if (GUIUtility.hotControl == base.shapeId)
					{
						if (Keys.PerformActionKey.IsKeyPressed() ||
							Keys.DeleteSelectionKey.IsKeyPressed() ||
							Keys.CancelActionKey.IsKeyPressed())
						{
							Event.current.Use();
						}
					}
					return;
				}
				case EventType.KeyUp:
				{
					if (GUIUtility.hotControl == base.shapeId)
					{
						if (Keys.CylinderBuilderMode.IsKeyPressed() ||
							Keys.PerformActionKey.IsKeyPressed())
						{
							HotKeyReleased(); 
							Event.current.Use();
							return;
						}
						if (Keys.DeleteSelectionKey.IsKeyPressed() ||
							Keys.CancelActionKey.IsKeyPressed())
						{
							Cancel();
							Event.current.Use();
							return;
						}
					}
					return;
				}

				case EventType.MouseDown:
				{
					if (!sceneRect.Contains(Event.current.mousePosition))
						break;
					if (Tools.viewTool != ViewTool.None && Tools.viewTool != ViewTool.Pan)
						return;
					if ((GUIUtility.hotControl != 0 && GUIUtility.hotControl != shapeEditId && GUIUtility.hotControl != base.shapeId) ||
						Event.current.button != 0)
						return;
					
					Event.current.Use();
					if (settings.vertices.Length == 0)
					{
						if ((GUIUtility.hotControl == 0 ||
							GUIUtility.hotControl == base.shapeEditId) && base.shapeId != -1)
						{
							base.CalculateWorldSpaceTangents(camera);
							GUIUtility.hotControl = base.shapeId;
							GUIUtility.keyboardControl = base.shapeId;
							EditorGUIUtility.editingTextField = false; 
						}
					}

					if (GUIUtility.hotControl == base.shapeId && 
						settings.vertices.Length < kMaxPoints)
					{
						if (!float.IsNaN(worldPosition.x) && !float.IsInfinity(worldPosition.x) &&
							!float.IsNaN(worldPosition.y) && !float.IsInfinity(worldPosition.y) &&
							!float.IsNaN(worldPosition.z) && !float.IsInfinity(worldPosition.z))
						{
							if (settings.vertices.Length < 2 &&
								hoverBuildPlane.normal.sqrMagnitude != 0)
								buildPlane = hoverBuildPlane;
							CalculateWorldSpaceTangents(camera);

							if (settings.vertices.Length == 0)
							{
								if (pointOnEdge)
								{
									firstSnappedEdges = visualSnappedEdges.ToArray();
									firstSnappedBrush = visualSnappedBrush;
									firstSnappedPlanes = null;
								} else
								{
									firstSnappedBrush = null;
									firstSnappedEdges = null;
									firstSnappedPlanes = null;
								}
								planeOnGeometry = vertexOnGeometry;
							} else
							{
								if (firstSnappedEdges != null)
								{
									if (firstSnappedPlanes == null)
										CreateSnappedPlanes();

									bool outside = true;
									for (int i = 0; i < firstSnappedPlanes.Length; i++)
									{
										if (firstSnappedPlanes[i].Distance(worldPosition) <= MathConstants.DistanceEpsilon)
										{
											outside = false;
											break;
										}
									}

									planeOnGeometry = !outside;
								}

								if (vertexOnGeometry)
								{
									var plane = hoverDefaultPlane.Value;
									var distance = plane.Distance(worldPosition);
									plane.d += distance;
									hoverDefaultPlane = plane;

									for (int i = 0; i < settings.vertices.Length; i++)
									{
										if (!settings.onGeometryVertices[i])
										{
											settings.SetPoint(i, GeometryUtility.ProjectPointOnPlane(plane, settings.vertices[i]));
											settings.onGeometryVertices[i] = true;
										}
									}
								}
							}
							ArrayUtility.Add(ref settings.onGeometryVertices, vertexOnGeometry);
							settings.AddPoint(worldPosition);
														
							UpdateSizes();
							CSG_EditorGUIUtility.RepaintAll();
							if (settings.vertices.Length == kMaxPoints)
							{
								HotKeyReleased();
							}
						}
					}
					return;
				}
				case EventType.MouseDrag:
				{
					if (Tools.viewTool != ViewTool.None && Tools.viewTool != ViewTool.Pan)
						break;
					if (GUIUtility.hotControl == base.shapeId && Event.current.button == 0)
					{
						Event.current.Use();
					}
					return;
				}
				case EventType.MouseUp:
				{
					if (GUIUtility.hotControl != base.shapeId)
						return;
					if (Tools.viewTool != ViewTool.None && Tools.viewTool != ViewTool.Pan)
						return;
					if (Event.current.button == 0)
					{
						Event.current.Use(); 
						
						ResetVisuals();
						if (settings.vertices.Length == kMaxPoints)
						{
							GUIUtility.hotControl = 0;
							GUIUtility.keyboardControl = 0;
							EditorGUIUtility.editingTextField = false;

							editMode = EditMode.CreateShape;
							HotKeyReleased();
						}
					}
					return;
				}
			}
		}
		#endregion


		#region Create Stairs
		internal override bool UpdateBaseShape(bool registerUndo = true)
		{
			if (editMode == EditMode.CreatePlane)
				return false;
						
			//var length = StairsDepth;
			//if (length == 0)
			{
				InternalCSGModelManager.skipCheckForChanges = false;
				HideGenerateBrushes();
				return false;
			}
			/*
			var worldToLocalRotation = GetWorldToLocalRotation();
			var localToWorldRotation = Quaternion.Inverse(worldToLocalRotation);
			brushPosition = localToWorldRotation * settings.bounds.Min;

			var totalSteps = TotalSteps;
			if (!GenerateBrushObjects(totalSteps))
			{
				InternalCSGModelManager.skipRefresh = false;
				HideGenerateBrushes();
				return false;
			}
			
			if (registerUndo)
				Undo.RecordObjects(generatedBrushes, "Created Spiral Stairs");
			
			if (!GenerateStairs(generatedBrushes, totalSteps, StepDepth, StepHeight, StairsDepth, StairsWidth, StairsHeight, ExtraDepth, ExtraHeight))
			{
				InternalCSGModelManager.skipRefresh = false;
				HideGenerateBrushes();
				return false;
			}
			
			if (registerUndo)
				MarkAllBrushesDirty();
			

			try
			{
				InternalCSGModelManager.skipRefresh = true;
				if (registerUndo)
					EditorUtility.SetDirty(this);
				InternalCSGModelManager.UpdateMaterialCount(parentModel);
				InternalCSGModelManager.External.SetDirty(parentModel.modelNodeID);
				InternalCSGModelManager.Refresh(forceHierarchyUpdate: true);
				//DebugEditorWindow.PrintDebugInfo();
			}
			finally
			{
				InternalCSGModelManager.skipRefresh = false;
			}
			return true;*/
		}

		private bool GenerateStairs(CSGBrush[] stepBrushes, int totalSteps, float stepLength, float stepHeight, float stairsDepth, float stairsWidth, float stairsHeight, float extraDepth, float extraHeight)
		{
			bool success = true;
			for (int stepIndex = 0; stepIndex < totalSteps; stepIndex++)
			{
				var brush = stepBrushes[stepIndex];
				if (!brush)
					continue;

				var curStepHeight	= Mathf.Min(stairsHeight, (stepIndex == 0) ? (extraHeight + stepHeight) : stepHeight);
				var curStepY		= (stepIndex == 0) ? (stepHeight  * stepIndex ) : (extraHeight + (stepHeight * stepIndex));
				
				var extraLength = lengthDirection * (stepLength * stepIndex);
				var heightPos	= heightDirection * curStepY;

				var widthSize	= (widthDirection  * stairsWidth);
				var lengthSize	= (lengthDirection * stairsDepth) - extraLength;
				var heightSize	= (heightDirection * curStepHeight);
								
				var size		= widthSize + heightSize + lengthSize;
				var position	= (totalSteps == 1) ? (heightPos + brushPosition) : heightPos;
				
				ControlMesh newControlMesh;
				Shape		newShape;
				if (!BrushFactory.CreateCubeControlMesh(out newControlMesh, out newShape, Vector3.zero, size))
				{
					success = false;
					if (brush.gameObject.activeSelf)
						brush.gameObject.SetActive(false);
					continue;
				}				

				if (!brush.gameObject.activeSelf)
					brush.gameObject.SetActive(true);
				
				brush.Shape			= newShape;
				brush.ControlMesh	= newControlMesh;
				brush.transform.localPosition = position;
				SurfaceUtility.TranslateSurfacesInWorldSpace(brush, -position);
			}
			return success;
		}
		#endregion
	}
}
