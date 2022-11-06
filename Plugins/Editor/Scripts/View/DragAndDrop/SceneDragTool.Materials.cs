using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using InternalRealtimeCSG;
using System.Linq;
using RealtimeCSG.Components;

namespace RealtimeCSG
{
	internal sealed class SceneDragToolMaterials : ISceneDragTool
	{
		SelectedBrushSurface[]	hoverBrushSurfaces		= null;
		bool                    hoverOnSelectedSurfaces = false;

		List<Material>	dragMaterials		= null;
	
		bool			selectAllSurfaces	= false;
		GameObject		hoverOverObject;
		MeshRenderer	hoverOverMeshRenderer;
		int				hoverMaterialIndex;
		GameObject		prevHoverOverObject;
		
		int dragGroup = -1;
		
		#region ValidateDrop
		public bool ValidateDrop(SceneView sceneView)
		{
			Reset();
			if (DragAndDrop.objectReferences == null ||
				DragAndDrop.objectReferences.Length == 0)
			{
				dragMaterials = null; 
				return false;
			}

			dragMaterials		= new List<Material>();
			foreach (var obj in DragAndDrop.objectReferences)
			{
				var material = obj as Material;
				if (material != null)
					dragMaterials.Add(material);
			}
			if (dragMaterials.Count != 1)
			{
				dragMaterials = null;
				return false;
			}

			dragGroup = -1;
			return true;
		}
		#endregion

		
		#region ValidateDropPoint
		public bool ValidateDropPoint(SceneView sceneView)
		{
			if (!sceneView)
				return false;

			hoverOverObject = null;
			var camera = sceneView.camera;
			GameObject foundObject;
			if (!SceneQueryUtility.FindClickWorldIntersection(camera, Event.current.mousePosition, out foundObject, out hoverOverMeshRenderer, out hoverMaterialIndex, ignoreDeepClick: true))
				return false;
			
			if (foundObject.GetComponent<CSGBrush>())
				return true;
			
			hoverOverObject = foundObject;
			return true;
		}
		#endregion

		#region Reset
		public void Reset()
		{
			dragGroup = -1;
			hoverOverObject		= null;
			prevHoverOverObject = null;
			hoverBrushSurfaces	= null;
			selectAllSurfaces	= false;
			hoverOnSelectedSurfaces = false;
		}
		#endregion

		public bool DragUpdated(SceneView sceneView, Transform transformInInspector, Rect selectionRect)
		{
			if (hoverOverMeshRenderer != null)
				return DragUpdated(sceneView);
			return BrushDragUpdated(sceneView, transformInInspector, selectionRect);
		}

		void ApplyMeshMaterial()
		{
			if (hoverOverMeshRenderer != null)
			{
				var sharedMaterials = hoverOverMeshRenderer.sharedMaterials;
				var material = GetMaterial();
				if (sharedMaterials[hoverMaterialIndex] != material)
				{
					//if (UndoRevert())
					{
						InternalCSGModelManager.UpdateMeshes();
						MeshInstanceManager.UpdateHelperSurfaceVisibility();
					}
					UndoInit();

					Undo.RecordObject(hoverOverMeshRenderer, "Modified material");
					sharedMaterials[hoverMaterialIndex] = material;
					hoverOverMeshRenderer.sharedMaterials = sharedMaterials.ToArray(); // ToArray forces Undo to recognize the change
				}
			}
		}

		public bool DragUpdated(SceneView sceneView)
		{
			if (prevHoverOverObject != hoverOverObject)
			{
				BrushDragExited(sceneView);
				prevHoverOverObject = hoverOverObject;
			}
			if (hoverOverMeshRenderer != null)
			{
				ApplyMeshMaterial();
				return true;
			}

			if (hoverOverMeshRenderer == null)
				return BrushDragUpdated(sceneView);
			return true;
		}

		public void DragPerform(SceneView sceneView)
        {
			if (hoverOverMeshRenderer == null) BrushDragPerform(sceneView);
			else
				ApplyMeshMaterial();
		}
			
		public void DragExited(SceneView sceneView)
        {
			if (hoverOverMeshRenderer == null) BrushDragExited(sceneView);
		}

		#region DragUpdated
		public bool BrushDragUpdated(SceneView sceneView, Transform transformInInspector, Rect selectionRect)
		{
			var highlight_brushes = transformInInspector.GetComponentsInChildren<CSGBrush>();

			bool prevSelectAllSurfaces = selectAllSurfaces;
			selectAllSurfaces = true;
			bool modified = true;
			if (hoverBrushSurfaces != null)
			{
				if (hoverBrushSurfaces.Length != highlight_brushes.Length)
				{
					modified = false;
				} else
				{
					modified = false;
					for (int i = 0; i < highlight_brushes.Length; i++)
					{
						var find_brush = highlight_brushes[i];
						bool found = false;
						for (int j = 0; j < hoverBrushSurfaces.Length; j++)
						{
							if (hoverBrushSurfaces[j].surfaceIndex == -1 &&
								hoverBrushSurfaces[j].brush == find_brush)
							{
								found = true;
								break;
							}
						}
						if (!found)
						{
							modified = true;
							break;
						}
					}
				}
			}


			bool needUpdate = false;
			if (modified)
			{
				hoverOnSelectedSurfaces = false;
				if (hoverBrushSurfaces != null)
				{
					needUpdate = true;
					RestoreMaterials(hoverBrushSurfaces);
				}

				hoverBrushSurfaces = HoverOnBrush(highlight_brushes, -1);

				if (hoverBrushSurfaces != null)
				{
					hoverBrushSurfaces = GetCombinedBrushes(hoverBrushSurfaces);
					needUpdate = true;
					ApplyMaterial(hoverBrushSurfaces);
				}
			} else
			{
				if (prevSelectAllSurfaces != selectAllSurfaces)
				{
					if (hoverBrushSurfaces != null)
					{
						needUpdate = true;
						ApplyMaterial(hoverBrushSurfaces);
					}
				}
			}

			if (needUpdate)
			{
				InternalCSGModelManager.UpdateMeshes();
				MeshInstanceManager.UpdateHelperSurfaceVisibility();
			}
			return needUpdate;
		}

		public bool BrushDragUpdated(SceneView sceneView)
		{
            var camera = sceneView.camera;
			LegacyBrushIntersection intersection;
			
			int		 highlight_surface;
			CSGBrush highlight_brush;	
			if (!SceneQueryUtility.FindWorldIntersection(camera, Event.current.mousePosition, out intersection))
			{
				highlight_brush		= null;
				highlight_surface	= -1;
			} else
			{
				highlight_brush		= intersection.brush;
				highlight_surface	= intersection.surfaceIndex;
			}

			bool modified = true;
			if (hoverBrushSurfaces != null)
			{
				for (int i = 0; i < hoverBrushSurfaces.Length; i++)
				{
					if (hoverBrushSurfaces[i].brush == highlight_brush &&
						hoverBrushSurfaces[i].surfaceIndex == highlight_surface)
					{
						modified = false;
						break;
					}
				}
			}


			bool needUpdate = false;
			if (modified)
			{
				hoverOnSelectedSurfaces = false;
				if (hoverBrushSurfaces != null)
				{
					needUpdate = true;
					RestoreMaterials(hoverBrushSurfaces);
				}

				hoverBrushSurfaces = HoverOnBrush(new CSGBrush[1] { highlight_brush }, highlight_surface);

				if (hoverBrushSurfaces != null)
				{
					hoverBrushSurfaces = GetCombinedBrushes(hoverBrushSurfaces);
					needUpdate = true;
					ApplyMaterial(hoverBrushSurfaces);
				}
			} else
			{
				bool prevSelectAllSurfaces	= selectAllSurfaces;
				selectAllSurfaces			= Event.current.shift;

				//if (prevSelectAllSurfaces != selectAllSurfaces)
				{
					if (hoverBrushSurfaces != null)
					{
						needUpdate = (prevSelectAllSurfaces != selectAllSurfaces);

						ApplyMaterial(hoverBrushSurfaces);
					}
				}
			}
			
			if (needUpdate)
			{
				InternalCSGModelManager.UpdateMeshes();
				MeshInstanceManager.UpdateHelperSurfaceVisibility();
			}
			return needUpdate;
		}
		#endregion

		#region DragPerform
		public void BrushDragPerform(SceneView sceneView)
		{
			if (hoverBrushSurfaces == null)
				return;

			RestoreMaterials(hoverBrushSurfaces);

			ApplyMaterial(hoverBrushSurfaces);

			var gameObjects = new HashSet<GameObject>();
			for (int i = 0; i < hoverBrushSurfaces.Length; i++)
				gameObjects.Add(hoverBrushSurfaces[i].brush.gameObject);

			var surfaceTool = EditModeManager.ActiveTool as EditModeSurface;
			if (surfaceTool != null)
			{
				surfaceTool.SelectSurfaces(hoverBrushSurfaces, gameObjects, selectAllSurfaces);
			} else
				Selection.objects = gameObjects.ToArray();

			for (int i = 0; i < SceneView.sceneViews.Count; i++)
			{
				var sceneview = SceneView.sceneViews[i] as SceneView;
				if (sceneview == null ||
					sceneview.camera == null)
					continue;

				var rect = sceneview.camera.pixelRect;
				if (rect.Contains(Event.current.mousePosition))
				{
					sceneview.Focus();
				}
			}
			hoverBrushSurfaces = null;
		}
		#endregion

		#region DragExited
		public void BrushDragExited(SceneView sceneView)
		{
			if (hoverBrushSurfaces != null)
			{
				UndoRevert();

				var updateModels = new HashSet<CSGModel>();
				var updateBrushes = new HashSet<CSGBrush>();
				for (int i = 0; i < hoverBrushSurfaces.Length; i++)
				{
					var brush = hoverBrushSurfaces[i].brush;
					if (brush.ChildData == null ||
						brush.ChildData.Model == null)
						continue;

					try
					{
						var model = brush.ChildData.Model;
						updateModels.Add(model);
						updateBrushes.Add(brush);
					}
					finally { }
				}
				UpdateBrushMeshes(updateBrushes, updateModels);
				RestoreMaterials(hoverBrushSurfaces);

				InternalCSGModelManager.UpdateMeshes();
				MeshInstanceManager.UpdateHelperSurfaceVisibility();
				HandleUtility.Repaint();
			}
			hoverBrushSurfaces = null;
		}
		#endregion

		SelectedBrushSurface[] GetCombinedBrushes(SelectedBrushSurface[] hoverBrushSurfaces)
		{
			var highlight_surfaces = new List<SelectedBrushSurface>();
			var highlight_brushes = new HashSet<CSGBrush>();
			for (int i = 0; i < hoverBrushSurfaces.Length; i++)
			{
				highlight_surfaces.Add(hoverBrushSurfaces[i]);
			}
			for (int i = 0; i < hoverBrushSurfaces.Length; i++)
			{
				var brush = hoverBrushSurfaces[i].brush;
				var top_node = SceneQueryUtility.GetTopMostGroupForNode(brush);
				if (top_node.transform != brush.transform)
				{
					foreach (var childBrush in top_node.GetComponentsInChildren<CSGBrush>())
					{
						if (highlight_brushes.Add(childBrush))
							highlight_surfaces.Add(new SelectedBrushSurface(childBrush, -1));
					}
				}
			}
			return highlight_surfaces.ToArray();
		}



		#region HoverOnBrush
		SelectedBrushSurface[] HoverOnBrush(CSGBrush[] hoverBrushes, int surfaceIndex)
		{
			hoverOnSelectedSurfaces = false;
			if (hoverBrushes == null ||
				hoverBrushes.Length == 0 ||
				hoverBrushes[0] == null)
				return null;

			var activetool = EditModeManager.ActiveTool as EditModeSurface;
			if (activetool != null)
			{
				var selectedBrushSurfaces = activetool.GetSelectedSurfaces();
				for (int i = 0; i < selectedBrushSurfaces.Length; i++)
				{
					if (selectedBrushSurfaces[i].surfaceIndex == surfaceIndex &&
						ArrayUtility.Contains(hoverBrushes, selectedBrushSurfaces[i].brush))
					{
						if (i != 0 && selectedBrushSurfaces.Length > 1)
						{
							var temp = selectedBrushSurfaces[0];
							selectedBrushSurfaces[0] = selectedBrushSurfaces[i];
							selectedBrushSurfaces[i] = temp;
						}
						hoverOnSelectedSurfaces = true;
						return selectedBrushSurfaces;
					}
				}
			}

			var surfaces = new SelectedBrushSurface[hoverBrushes.Length];
			for (int i = 0; i < hoverBrushes.Length; i++)
			{
				surfaces[i] = new SelectedBrushSurface(hoverBrushes[i], surfaceIndex);
			}
			return surfaces;
		}
		#endregion

		#region UpdateBrushMeshes
		void UpdateBrushMeshes(HashSet<CSGBrush> brushes, HashSet<CSGModel> models)
		{
			foreach (var brush in brushes)
			{
				try
				{
					brush.EnsureInitialized();
					ShapeUtility.CheckMaterials(brush.Shape);
				}
				finally { }
			}
			foreach (var brush in brushes)
			{
				try
				{
					InternalCSGModelManager.CheckSurfaceModifications(brush, true);
					InternalCSGModelManager.ValidateBrush(brush);
				}
				finally { }
			}
			MeshInstanceManager.UpdateHelperSurfaceVisibility(force: true);
		}
		#endregion

		static string MaterialToString(Material mat)
		{
			if (ReferenceEquals(mat, null))
				return "null";
			if (!mat)
				return "invalid";
			return mat.name + " " + mat.GetInstanceID().ToString();
		}

		bool UndoRevert()
        {
			if (dragGroup != -1)
			{
				Undo.RevertAllDownToGroup(dragGroup);
				dragGroup = -1;
				return true;
			}
			return false;
		}

		void UndoInit()
		{
			if (dragGroup == -1)
			{
				Undo.IncrementCurrentGroup();
				dragGroup = Undo.GetCurrentGroup();
			}
		}

		#region RestoreMaterials
		void RestoreMaterials(SelectedBrushSurface[] hoverBrushSurfaces)
		{
			if (hoverBrushSurfaces == null)
				return;

			UndoRevert();

			var updateModels	= new HashSet<CSGModel>();
			var updateBrushes	= new HashSet<CSGBrush>();
			for (int i = 0; i < hoverBrushSurfaces.Length; i++)
			{
				var brush = hoverBrushSurfaces[i].brush;
				if (brush.ChildData == null ||
					brush.ChildData.Model == null)
					continue;

				try
				{
					var model = brush.ChildData.Model;
					updateModels.Add(model);
					updateBrushes.Add(brush);
				}
				finally { }
			}
			UpdateBrushMeshes(updateBrushes, updateModels);
		}
		#endregion
		
		Material GetMaterial()
        {
			if (dragMaterials.Count > 1)
			{
				return dragMaterials[Random.Range(0, dragMaterials.Count)];
			} else
				return dragMaterials[0];
		}

		#region ApplyMaterial
		void ApplyMaterial(SelectedBrushSurface[] hoverBrushSurfaces)
		{
			if (hoverBrushSurfaces == null)
				return;

			var updateModels = new HashSet<CSGModel>();
			var updateBrushes = new HashSet<CSGBrush>();
			try
			{
				for (int i = 0; i < hoverBrushSurfaces.Length; i++)
				{
					var brush = hoverBrushSurfaces[i].brush;
					if (brush.ChildData == null ||
						brush.ChildData.Model == null)
						continue;

					try
					{
						var shape = brush.Shape;
						var model = brush.ChildData.Model;

						var texGens = shape.TexGens;

						var modified = false;

						if (selectAllSurfaces)
						{
							// assign material to all surfaces of selected brushes
							if (!updateBrushes.Contains(brush)) // but don't duplicate any work if multiple surfaces of same brush are selected
							{ 
								for (int m = 0; m < texGens.Length; m++)
								{
									var material = GetMaterial();
									if (texGens[m].RenderMaterial != material)
									{
										if (modified)
										{
											UndoInit();
											Undo.RecordObject(brush, "Modifying material");
										}
										modified = true;
										texGens[m].RenderMaterial = material;
									}
								}
							}
						} else
						// per surface
						{
							// assign material to selected surface
							var highlight_surface = hoverBrushSurfaces[i].surfaceIndex;
							if (highlight_surface >= 0)
							{
								var material = GetMaterial();
								var highlight_texGen = shape.Surfaces[highlight_surface].TexGenIndex;
								if (texGens[highlight_texGen].RenderMaterial != material)
								{
									if (!modified)
									{
										UndoInit();
										Undo.RecordObject(brush, "Modifying material");
									}
									modified = true;
									texGens[highlight_texGen].RenderMaterial = material;
								}
							}
						}

						if (modified)
						{
							shape.TexGens = texGens.ToArray();
							updateModels.Add(model);
							updateBrushes.Add(brush);
						}
					}
					finally { }
				}
			}
			finally
			{
				UpdateBrushMeshes(updateBrushes, updateModels);
			}
		}
		#endregion


		#region Paint
		public void OnPaint(Camera camera)
		{
			if (hoverOverObject != null)
				return;

			if (!hoverOnSelectedSurfaces)
			{ 
				var activetool = EditModeManager.ActiveTool as EditModeSurface;
				if (activetool != null)
				{
					var selectedBrushSurfaces = activetool.GetSelectedSurfaces();
					for (int i = 0; i < selectedBrushSurfaces.Length; i++)
					{
						var brush		= selectedBrushSurfaces[i].brush;

						var highlight_surface		= selectedBrushSurfaces[i].surfaceIndex;
						var brush_transformation	= brush.compareTransformation.localToWorldMatrix;
						
						CSGRenderer.DrawSurfaceOutlines(brush.brushNodeID, brush.Shape,
													   brush_transformation, highlight_surface,
													   ColorSettings.SurfaceInnerStateColor[2],
													   ColorSettings.SurfaceOuterStateColor[2],
													   GUIConstants.oldThinLineScale);
					}
				}
			}

			if (hoverBrushSurfaces != null)
			{ 			
				for (int i = 0; i < hoverBrushSurfaces.Length; i++)
				{
					var brush = hoverBrushSurfaces[i].brush;
					
					var brush_transformation = brush.compareTransformation.localToWorldMatrix;
				
					var highlight_surface	= hoverBrushSurfaces[i].surfaceIndex;
					if (highlight_surface == -1 || selectAllSurfaces)
					{
						CSGRenderer.DrawSelectedBrush(brush.brushNodeID, brush.Shape, brush_transformation, ColorSettings.WireframeOutline, 0, selectAllSurfaces, GUIConstants.oldLineScale);
					} else
					{
						CSGRenderer.DrawSelectedBrush(brush.brushNodeID, brush.Shape, brush_transformation, ColorSettings.WireframeOutline, highlight_surface, selectAllSurfaces, GUIConstants.oldLineScale);
					}
				}
			}
		}
		#endregion
	}
}
