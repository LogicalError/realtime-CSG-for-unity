using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using InternalRealtimeCSG;
using RealtimeCSG.Legacy;
using RealtimeCSG.Components;

namespace RealtimeCSG
{
	// TODO:
	//	use CSGBounds handle to resize box in edit mode (can move all sides, not just 3 points)
    internal sealed class GeneratorBox : GeneratorExtrudedBase, IGenerator
	{
		[NonSerialized] Vector3		worldPosition;
		[NonSerialized] Vector3		prevWorldPosition;
		[NonSerialized] bool		onLastPoint			= false;
		[NonSerialized] CSGPlane	geometryPlane		= new CSGPlane(0, 1, 0, 0);
		[NonSerialized] CSGPlane?	hoverDefaultPlane;

		[NonSerialized] CSGPlane[]	firstSnappedPlanes	= null;
		[NonSerialized] Vector3[]	firstSnappedEdges	= null;
		[NonSerialized] CSGBrush	firstSnappedBrush	= null;

		[SerializeField] GeneratorBoxSettings settings			= new GeneratorBoxSettings();

		protected override IShapeSettings ShapeSettings { get { return settings; } }
		
		public float	Width
		{
			get
			{
				if (editMode == EditMode.CreatePlane ||
					settings.vertices.Length == 0)
					return 0;
				
				Vector3	corner1				= settings.vertices[0];
				Vector3	corner2				= settings.vertices.Length >= 2 ? settings.vertices[1] : worldPosition;
				Vector3 delta				= corner2 - corner1;
				Vector3 projected_width		= Vector3.Project(delta, gridTangent);
				var width = projected_width.magnitude;
				if (float.IsInfinity(width) || float.IsNaN(width))
					width = 1.0f;
				width *= Mathf.Sign(Vector3.Dot(projected_width, gridTangent));
				
				return GeometryUtility.CleanLength(width);
			}
			set
			{
				if (editMode == EditMode.CreatePlane ||
					settings.vertices.Length == 0)
					return;
				
				Vector3	corner1				= settings.vertices[0];
				Vector3	corner2				= settings.vertices.Length >= 2 ? settings.vertices[1] : worldPosition;
				Vector3 delta				= corner2 - corner1;
				Vector3 projected_length	= Vector3.Project(delta, gridBinormal);
				var width = projected_length.magnitude;
				if (float.IsInfinity(width) || float.IsNaN(width))
					width = 1.0f;
				width *= Mathf.Sign(Vector3.Dot(projected_length, gridBinormal));
				corner2 = corner1 + (width * gridBinormal) + (value * gridTangent);

				if (settings.vertices.Length == 1)
					settings.AddPoint(corner2);
				else
					settings.vertices[1] = corner2;

				if (editMode == EditMode.ExtrudeShape)
				{
					Undo.RecordObject(this, "Modified Width");
					CenterExtrusionPoints(buildPlane);
					UpdateBaseShape();
				}
			}
		}
		

		public float	Length
		{
			get
			{
				if (editMode == EditMode.CreatePlane ||
					settings.vertices.Length == 0)
					return 0;
				
				Vector3	corner1				= settings.vertices[0];
				Vector3	corner2				= settings.vertices.Length >= 2 ? settings.vertices[1] : worldPosition;
				Vector3 delta				= corner2 - corner1;
				Vector3 projected_length	= Vector3.Project(delta, gridBinormal);
				var length = projected_length.magnitude;
				if (float.IsInfinity(length) || float.IsNaN(length))
					length = 1.0f;
				length *= Mathf.Sign(Vector3.Dot(projected_length, gridBinormal));

				return GeometryUtility.CleanLength(length);
			}
			set
			{
				if (editMode == EditMode.CreatePlane ||
					settings.vertices.Length == 0)
					return;

				Vector3	corner1				= settings.vertices[0];
				Vector3	corner2				= settings.vertices.Length >= 2 ? settings.vertices[1] : worldPosition;
				Vector3 delta				= corner2 - corner1;
				Vector3 projected_width		= Vector3.Project(delta, gridTangent);
				var length = projected_width.magnitude;
				if (float.IsInfinity(length) || float.IsNaN(length))
					length = 1.0f;
				length *= Mathf.Sign(Vector3.Dot(projected_width, gridTangent));
				corner2 = corner1 + (value * gridBinormal) + (length * gridTangent);
				
				if (settings.vertices.Length == 1)
					settings.AddPoint(corner2);
				else
					settings.vertices[1] = corner2;

				if (editMode == EditMode.ExtrudeShape)
				{
					Undo.RecordObject(this, "Modified Length");
					CenterExtrusionPoints(buildPlane);
					UpdateBaseShape();
				}
			}
		}

		public override void PerformDeselectAll() { Cancel(); }
		public override void PerformDelete() { Cancel(); }

		public override void Reset() 
		{
            settings.Reset();
			base.Reset();
			onLastPoint = false;
			hoverDefaultPlane	= null;
			firstSnappedPlanes	= null;
			firstSnappedEdges	= null;
			firstSnappedBrush	= null;
		}

		public bool HotKeyReleased()
		{
            var camera = Camera.current;

            ResetVisuals();
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
						settings.AddPoint(worldPosition);
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

		protected override void MoveShape(Vector3 offset)
		{
			settings.MoveShape(offset);
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

		internal override bool UpdateBaseShape(bool registerUndo = true)
		{
			List<ShapePolygon> newPolygons = null;
			var outlineVertices = settings.GetVertices(buildPlane, worldPosition, base.gridTangent, base.gridBinormal, out shapeIsValid);
			if (shapeIsValid)
			{
				newPolygons = ShapePolygonUtility.CreateCleanPolygonsFromVertices(outlineVertices, brushPosition, buildPlane);
				shapeIsValid = newPolygons != null;
			}
			//CSGBrushEditorManager.ResetMessage();

			if (shapeIsValid && newPolygons != null)
				UpdatePolygons(outlineVertices, newPolygons.ToArray());

			if (editMode != EditMode.EditShape &&
				editMode != EditMode.ExtrudeShape)
				return false;

			if (!shapeIsValid || !HaveExtrusion)
				return true;

			GenerateBrushesFromPolygons(inGridSpace: false);
			UpdateExtrudedShape();
			return true;
		}

		bool BuildPlaneIsReversed
		{
			get
			{
				var realPlane = geometryPlane;
				//if (!planeOnGeometry)
				//	realPlane = RealtimeCSG.Grid.CurrentGridPlane;

				if (Vector3.Dot(realPlane.normal, buildPlane.normal) < 0)
					return true;
				return false;
			}
		}

		internal override bool StartExtrudeMode(Camera camera, bool showErrorMessage = true)
        {
            // reverse buildPlane if it's different
            if (BuildPlaneIsReversed)
			{
				buildPlane = buildPlane.Negated();
				CalculateWorldSpaceTangents(camera);
			}
			
			var outlineVertices	= settings.GetVertices(buildPlane, worldPosition, base.gridTangent, base.gridBinormal, out shapeIsValid);
			if (!shapeIsValid)
			{
				ClearPolygons();
				//if (showErrorMessage)
				//	CSGBrushEditorManager.ShowMessage("Could not create brush from given 2D shape");
				HideGenerateBrushes();
				return false;
			}

			var newPolygons		= ShapePolygonUtility.CreateCleanPolygonsFromVertices(outlineVertices, brushPosition, buildPlane);
			if (newPolygons == null)
			{
				shapeIsValid = false;
				ClearPolygons();
				//if (showErrorMessage)
				//	CSGBrushEditorManager.ShowMessage("Could not create brush from given 2D shape");
				HideGenerateBrushes();
				return false;
			}
			EditModeManager.ResetMessage();
			shapeIsValid = true;

			UpdatePolygons(outlineVertices, newPolygons.ToArray());
			GenerateBrushesFromPolygons(inGridSpace: false);
			return true;
		}
		
		internal override bool CreateControlMeshForBrushIndex(CSGModel parentModel, CSGBrush brush, ShapePolygon polygon, Matrix4x4 localToWorld, float height, out ControlMesh newControlMesh, out Shape newShape)
		{
			var direction = haveForcedDirection ? forcedDirection : buildPlane.normal;
            if (!ShapePolygonUtility.GenerateControlMeshFromVertices(polygon,
																	 localToWorld,
																	 GeometryUtility.RotatePointIntoPlaneSpace(buildPlane, direction),
																	 height,
																	
																	 new TexGen(),
																			   
																	 false, 
																	 true,
																	 out newControlMesh,
																	 out newShape))
			{
				return false;
			}
						
			brush.Shape = newShape;
			brush.ControlMesh = newControlMesh;
			InternalCSGModelManager.ValidateBrush(brush, true);
			ControlMeshUtility.RebuildShape(brush);
			return true;
		}

		void PaintSquare()
		{
			//var wireframeColor = ColorSettings.BoundsOutlines;

			if (settings.vertices.Length < 1)
				return;

			Vector3 corner1 = settings.vertices[0];
			Vector3 corner2 = settings.vertices.Length >= 2 ? settings.vertices[1] : worldPosition;
			//Vector3 center				= (corner2 + corner1) * 0.5f;
			Vector3 delta = corner2 - corner1;
			Vector3 projected_width = Vector3.Project(delta, gridTangent);
			Vector3 projected_length = Vector3.Project(delta, gridBinormal);
			
			var point0 = corner1 + projected_width + projected_length;
			var point1 = corner1 + projected_width;
			var point2 = corner1;
			var point3 = corner1 + projected_length;

			var points = new Vector3[] { point0, point1, point1, point2, point2, point3, point3, point0 };

			var color = ColorSettings.ShapeDrawingFill;
			PaintUtility.DrawPolygon(MathConstants.identityMatrix, points, color);
		}

		void PaintBounds(Camera camera)
		{
			if (HaveHeight)
			{
				var localBounds = GetShapeBounds(toGridQuaternion);

				var volume = new Vector3[8];
				BoundsUtilities.GetBoundsCornerPoints(localBounds, volume);

				PaintUtility.RenderBoundsSizes(toGridQuaternion, fromGridQuaternion, camera, volume, Color.white, Color.white, Color.white, true, true, true);
			}
		}

		void PaintShape(SceneView sceneView, int id)
		{
            var camera      = sceneView.camera;
            var temp		= Handles.color;
			var origMatrix	= Handles.matrix;
					
			Handles.matrix = MathConstants.identityMatrix;
			var rotation = camera.transform.rotation;

			bool isValid;
			var realVertices = settings.GetVertices(buildPlane, worldPosition, gridTangent, gridBinormal, out isValid);
			if (editMode == EditMode.EditShape)
				shapeIsValid = isValid;
			if (realVertices != null && realVertices.Length >= 3)
			{
				var wireframeColor = ColorSettings.WireframeOutline;
				if (!shapeIsValid || !isValid)
					wireframeColor = Color.red;
				for (int i = 1; i < realVertices.Length; i++)
				{
					PaintUtility.DrawLine(realVertices[i - 1], realVertices[i], GUIConstants.oldLineScale, wireframeColor);
					PaintUtility.DrawDottedLine(realVertices[i - 1], realVertices[i], wireframeColor, 4.0f);
				}

				PaintUtility.DrawLine(realVertices[realVertices.Length - 1], realVertices[0], GUIConstants.oldLineScale, wireframeColor);
				PaintUtility.DrawDottedLine(realVertices[realVertices.Length - 1], realVertices[0], wireframeColor, 4.0f);
				
				//var color = ColorSettings.ShapeDrawingFill;
				//PaintUtility.DrawPolygon(MathConstants.identityMatrix, realVertices, color);
			}



			if (settings.vertices != null && settings.vertices.Length > 0)
			{
				Handles.color = ColorSettings.PointInnerStateColor[0];
				for (int i = 0; i < settings.vertices.Length; i++)
				{
					float handleSize = CSG_HandleUtility.GetHandleSize(settings.vertices[i]);
					float scaledHandleSize = handleSize * GUIConstants.handleScale;
					PaintUtility.SquareDotCap(id, settings.vertices[i], rotation, scaledHandleSize);
				}
				PaintSquare();
				PaintBounds(camera);
			}
						
			Handles.color = ColorSettings.PointInnerStateColor[3];
			{
				float handleSize = CSG_HandleUtility.GetHandleSize(worldPosition);
				float scaledHandleSize = handleSize * GUIConstants.handleScale;
				PaintUtility.SquareDotCap(id, worldPosition, rotation, scaledHandleSize);
			}

			Handles.matrix = origMatrix;
			Handles.color = temp;
		}

		protected override void CreateControlIDs()
		{
			base.CreateControlIDs();

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

				var transform		= firstSnappedBrush.transform.localToWorldMatrix;

				var localPlane		= GeometryUtility.InverseTransformPlane(transform, worldPlane);
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
		
		protected override void HandleCreateShapeEvents(SceneView sceneView, Rect sceneRect)
		{
            if (settings.vertices.Length < 2)
			{
				if (editMode == EditMode.ExtrudeShape ||
					editMode == EditMode.EditShape)
					editMode = EditMode.CreatePlane;
			}

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
						worldPosition	= intersection.worldIntersection;
						hoverBuildPlane = intersection.worldPlane;
						vertexOnBrush	= intersection.brush;
						
						vertexOnGeometry = true;
					} else
					{
						hoverBuildPlane = hoverDefaultPlane.Value;
						vertexOnBrush	= null; 
						
						var mouseRay	= HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
						worldPosition	= hoverBuildPlane.RayIntersection(mouseRay);
						vertexOnGeometry = false;
					}

					ResetVisuals();
					if (snapFunction != null)
					{
						CSGBrush snappedOnBrush;
						worldPosition = snapFunction(camera, worldPosition, hoverBuildPlane, ref visualSnappedEdges, out snappedOnBrush, generatedBrushes);
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
					} else
					if (settings.vertices.Length == 2)
					{
						onLastPoint = true;
					}
				} else
				{
					var mouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
					worldPosition = hoverBuildPlane.RayIntersection(mouseRay);

					ResetVisuals();
					if (snapFunction != null)
					{
						CSGBrush snappedOnBrush;
						worldPosition = snapFunction(camera, worldPosition, hoverBuildPlane, ref visualSnappedEdges, out snappedOnBrush, generatedBrushes);
						if (snappedOnBrush != null)
						{
							pointOnEdge = (visualSnappedEdges != null &&
											visualSnappedEdges.Count > 0);
							vertexOnBrush = snappedOnBrush;
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
						if (Keys.BoxBuilderMode.IsKeyPressed() ||
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

					if (GUIUtility.hotControl == base.shapeId && settings.vertices.Length < 2)
					{
						if (!float.IsNaN(worldPosition.x) && !float.IsInfinity(worldPosition.x) &&
							!float.IsNaN(worldPosition.y) && !float.IsInfinity(worldPosition.y) &&
							!float.IsNaN(worldPosition.z) && !float.IsInfinity(worldPosition.z))
						{
							if (hoverBuildPlane.normal.sqrMagnitude != 0)
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
								geometryPlane	= buildPlane;
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
									if (float.IsInfinity(distance) || float.IsNaN(distance))
										distance = 1.0f;
									plane.d += distance;
									hoverDefaultPlane = plane;

									for (int i = 0; i < settings.vertices.Length; i++)
									{
										if (!settings.onGeometryVertices[i])
										{
											settings.vertices[i] = GeometryUtility.ProjectPointOnPlane(plane, settings.vertices[i]);
											settings.onGeometryVertices[i] = true;
										}
									}
								}
							}
							ArrayUtility.Add(ref settings.onGeometryVertices, vertexOnGeometry);
							settings.AddPoint(worldPosition);
							CSG_EditorGUIUtility.RepaintAll();
							if (settings.vertices.Length == 2)
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
						if (onLastPoint)
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
			return;
		}


		internal override void BeginExtrusion()
		{
			settings.CopyBackupVertices();
		}

		internal override void EndExtrusion()
		{

		}

		protected override void HandleEditShapeEvents(SceneView sceneView, Rect sceneRect)
		{
            var camera = sceneView.camera;

			if (settings.vertices.Length < 2)
			{
				if (editMode == EditMode.ExtrudeShape ||
					editMode == EditMode.EditShape)
					editMode = EditMode.CreatePlane;
			}

			if (!SceneDragToolManager.IsDraggingObjectInScene &&
				Event.current.type == EventType.Repaint)
			{			
				if (visualSnappedEdges != null)
					PaintUtility.DrawLines(visualSnappedEdges.ToArray(), GUIConstants.oldThickLineScale, ColorSettings.SnappedEdges);
				
				if (visualSnappedGrid != null)
				{
					var _origMatrix = Handles.matrix;
					Handles.matrix = MathConstants.identityMatrix;
					PaintUtility.DrawDottedLines(visualSnappedGrid.ToArray(), ColorSettings.SnappedEdges);
					Handles.matrix = _origMatrix;
				}
					
				if (visualSnappedBrush != null)
				{
					if (visualSnappedBrush.compareTransformation != null &&
						visualSnappedBrush.ChildData != null &&
						visualSnappedBrush.ChildData.ModelTransform)
					{
						var brush_transformation = visualSnappedBrush.compareTransformation.localToWorldMatrix;
						CSGRenderer.DrawOutlines(visualSnappedBrush.brushNodeID, brush_transformation, ColorSettings.SelectedOutlines, ColorSettings.SelectedOutlines, ColorSettings.SelectedOutlines, ColorSettings.SelectedOutlines);
					}						
				}

				var origMatrix = Handles.matrix;
				Handles.matrix = MathConstants.identityMatrix;
				PaintBounds(camera);
				Handles.matrix = origMatrix;
			}
			
			HandleHeightHandles(sceneView, sceneRect, false);

			for (int i = 0; i < settings.vertices.Length; i++)
			{
				var id = settings.vertexIDs[i];
				var point_type = Event.current.GetTypeForControl(id);
				switch (point_type)
				{
					case EventType.Repaint:
					{
						if (SceneDragToolManager.IsDraggingObjectInScene)
							break;

						bool isSelected = id == GUIUtility.keyboardControl;
						var temp		= Handles.color;
						var origMatrix	= Handles.matrix;
					
						Handles.matrix = MathConstants.identityMatrix;
						var rotation = camera.transform.rotation;


						if (isSelected)
						{
							Handles.color = ColorSettings.PointInnerStateColor[3];
						} else
						if (HandleUtility.nearestControl == id)
						{
							Handles.color = ColorSettings.PointInnerStateColor[1];
						} else						
						{
							Handles.color = ColorSettings.PointInnerStateColor[0];
						}

						float handleSize = CSG_HandleUtility.GetHandleSize(settings.vertices[i]);
						float scaledHandleSize = handleSize * GUIConstants.handleScale;
						PaintUtility.SquareDotCap(id, settings.vertices[i], rotation, scaledHandleSize);
						
						Handles.matrix = origMatrix;
						Handles.color = temp;
						break;
					}

					case EventType.Layout:
					{
						if ((Tools.viewTool != ViewTool.None && Tools.viewTool != ViewTool.Pan))
							break;

						var origMatrix = Handles.matrix;
						Handles.matrix = MathConstants.identityMatrix;
						float handleSize = CSG_HandleUtility.GetHandleSize(settings.vertices[i]);
						float scaledHandleSize = handleSize * GUIConstants.handleScale;
						HandleUtility.AddControl(id, HandleUtility.DistanceToCircle(settings.vertices[i], scaledHandleSize));
						Handles.matrix = origMatrix;						
					
						break;
					}

					case EventType.ValidateCommand:
					case EventType.KeyDown:
					{
						if (GUIUtility.hotControl == id)
						{
							if (Keys.CancelActionKey.IsKeyPressed())
							{
								Event.current.Use(); 
								break;
							}
						}
						break;
					}
					case EventType.KeyUp:
					{
						if (GUIUtility.hotControl == id)
						{
							if (Keys.CancelActionKey.IsKeyPressed())
                            {
                                GUIUtility.hotControl = 0;
								GUIUtility.keyboardControl = 0;
								EditorGUIUtility.editingTextField = false;
								Event.current.Use(); 
								break;
							}
						}
						break;
					}

					case EventType.MouseDown:
					{
						if (!sceneRect.Contains(Event.current.mousePosition))
							break;
						if (Tools.viewTool != ViewTool.None && Tools.viewTool != ViewTool.Pan)
							break;
						if (GUIUtility.hotControl == 0 && HandleUtility.nearestControl == id && Event.current.button == 0)
						{
                            GUIUtility.hotControl = id;
							GUIUtility.keyboardControl = id;
							EditorGUIUtility.editingTextField = false; 
							EditorGUIUtility.SetWantsMouseJumping(1);
							Event.current.Use(); 
							break;
						}
						break;
					}
					case EventType.MouseDrag:
					{
						if (Tools.viewTool != ViewTool.None && Tools.viewTool != ViewTool.Pan)
							break;
						if (GUIUtility.hotControl == id && Event.current.button == 0)
						{
							Undo.RecordObject(this, "Modify shape");

							var mouseRay		= HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
							RealtimeCSG.CSGGrid.SetForcedGrid(camera, buildPlane);
							var alignedPlane	= new CSGPlane(RealtimeCSG.CSGGrid.CurrentWorkGridPlane.normal, settings.vertices[0]);
							var worldPosition	= buildPlane.Project(alignedPlane.RayIntersection(mouseRay));
							if (float.IsInfinity(worldPosition.x) || float.IsNaN(worldPosition.x) ||
								float.IsInfinity(worldPosition.y) || float.IsNaN(worldPosition.y) ||
								float.IsInfinity(worldPosition.z) || float.IsNaN(worldPosition.z))
								worldPosition = settings.vertices[i];

							ResetVisuals();
							if (snapFunction != null)
							{
								CSGBrush snappedOnBrush;
								worldPosition = snapFunction(camera, worldPosition, buildPlane, ref base.visualSnappedEdges, out snappedOnBrush, generatedBrushes);
							}
								
							base.visualSnappedGrid = RealtimeCSG.CSGGrid.FindAllGridEdgesThatTouchPoint(camera, worldPosition);

							settings.vertices[i] = worldPosition;
							
							CenterExtrusionPoints(buildPlane);
							UpdateBaseShape();

							if (editMode == EditMode.ExtrudeShape)
							{
								StartExtrudeMode(camera);
								UpdateBaseShape();
							}
							UpdateExtrudedShape();

							GUI.changed = true;
							Event.current.Use(); 
							break;
						}
						break;
					}
					case EventType.MouseUp:
					{
						if (GUIUtility.hotControl != id)
							break;
						if (Tools.viewTool != ViewTool.None && Tools.viewTool != ViewTool.Pan)
							break;
						if (Event.current.button == 0)
                        {
							GUIUtility.hotControl = 0;
							GUIUtility.keyboardControl = 0;
							EditorGUIUtility.editingTextField = false;
							EditorGUIUtility.SetWantsMouseJumping(0);
							Event.current.Use(); 

							ResetVisuals();
							//if (Length == 0 || Width == 0)
							//{
							//	Cancel();
							//}
							break;
						}
						break;
					}
				}
				
			}
		}
		
		public bool OnShowGUI(bool isSceneGUI)
		{
			return GeneratorBoxGUI.OnShowGUI(this, isSceneGUI);
		}
	}
}
