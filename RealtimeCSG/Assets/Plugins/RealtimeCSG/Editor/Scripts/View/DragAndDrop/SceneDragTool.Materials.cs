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
		Material[][]			previousMaterials		= null;

		List<Material>	dragMaterials		= null;
	
		bool			selectAllSurfaces	= false;
		
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

			return true;
		}
		#endregion

		
		#region ValidateDropPoint
		public bool ValidateDropPoint(SceneView sceneView)
		{
            var camera = sceneView.camera;

			GameObject foundObject;
			if (!SceneQueryUtility.FindClickWorldIntersection(camera, Event.current.mousePosition, out foundObject))
				return false;
			
			if (!foundObject.GetComponent<CSGBrush>())
				return false;
			
			return true;
		}
		#endregion

		#region Reset
		public void Reset()
		{
			hoverBrushSurfaces	= null;
			selectAllSurfaces	= false;
			previousMaterials	= null;
			hoverOnSelectedSurfaces = false;
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
		public SelectedBrushSurface[] HoverOnBrush(CSGBrush[] hoverBrushes, int surfaceIndex)
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

		#region DragUpdated
		public bool DragUpdated(Transform transformInInspector, Rect selectionRect)
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
					using (new UndoGroup(hoverBrushSurfaces, "Modified materials"))
					{
						RememberMaterials(hoverBrushSurfaces);
						ApplyMaterial(hoverBrushSurfaces);
					}
				}
			} else
			{
				if (prevSelectAllSurfaces != selectAllSurfaces)
				{
					if (hoverBrushSurfaces != null)
					{
						needUpdate = true;
						using (new UndoGroup(hoverBrushSurfaces, "Modified materials"))
						{
							ApplyMaterial(hoverBrushSurfaces);
						}
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

		public bool DragUpdated(SceneView sceneView)
		{
            var camera = sceneView.camera;
			LegacyBrushIntersection intersection;
			int		 highlight_surface	= -1;
			CSGBrush highlight_brush	= null;	
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
					using (new UndoGroup(hoverBrushSurfaces, "Modified materials"))
					{
						RememberMaterials(hoverBrushSurfaces);
						ApplyMaterial(hoverBrushSurfaces);
					}
				}
			} else
			{
				bool prevSelectAllSurfaces	= selectAllSurfaces;
				selectAllSurfaces			= Event.current.shift;

				if (prevSelectAllSurfaces != selectAllSurfaces)
				{
					if (hoverBrushSurfaces != null)
					{
						needUpdate = true;

						using (new UndoGroup(hoverBrushSurfaces, "Modified materials"))
						{
							ApplyMaterial(hoverBrushSurfaces);
						}
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

		#region RememberMaterials
		void RememberMaterials(SelectedBrushSurface[] hoverBrushSurfaces)
		{
			if (hoverBrushSurfaces == null)
			{
				previousMaterials = null;
				return;
			}
			if (previousMaterials == null ||
				previousMaterials.Length != hoverBrushSurfaces.Length)
				previousMaterials = new Material[hoverBrushSurfaces.Length][];
			for (int i = 0; i < hoverBrushSurfaces.Length; i++)
			{
				var brush			= hoverBrushSurfaces[i].brush;
				var shape			= brush.Shape;

				if (previousMaterials[i] == null ||
					previousMaterials[i].Length != shape.TexGens.Length)
					previousMaterials[i] = new Material[shape.TexGens.Length];

				for (int t = 0; t < shape.TexGens.Length; t++)
				{
					previousMaterials[i][t] = shape.TexGens[t].RenderMaterial;
				}
			}
		}
		#endregion
		
		#region UpdateBrushMeshes
		void UpdateBrushMeshes(HashSet<CSGBrush> brushes, HashSet<CSGModel> models)
		{
			foreach (var brush in brushes)
			{
				brush.EnsureInitialized();
				ShapeUtility.CheckMaterials(brush.Shape);
//				var brush_cache = InternalCSGModelManager.GetBrushCache(brush);
			}
			foreach (var brush in brushes)
			{
				InternalCSGModelManager.CheckSurfaceModifications(brush, true);
				InternalCSGModelManager.ValidateBrush(brush);
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

		#region RestoreMaterials
		void RestoreMaterials(SelectedBrushSurface[] hoverBrushSurfaces)
		{
			if (hoverBrushSurfaces == null)
				return;
			
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
					if (updateBrushes.Add(brush))
					{
						var shape = brush.Shape;
						if (shape != null &&
							shape.TexGens != null &&
							previousMaterials != null)
						{
							for (int t = 0; t < shape.TexGens.Length; t++)
							{
								shape.TexGens[t].RenderMaterial = previousMaterials[i][t];
							}
						}
					}
				}
				finally { }
			}
			UpdateBrushMeshes(updateBrushes, updateModels);
		}
		#endregion

		#region ApplyMaterial
		void ApplyMaterial(SelectedBrushSurface[] hoverBrushSurfaces)
		{
			if (hoverBrushSurfaces == null)
				return;
			var updateModels	= new HashSet<CSGModel>();
			var updateBrushes	= new HashSet<CSGBrush>();
			for (int i = 0; i < hoverBrushSurfaces.Length; i++)
			{
				var brush			= hoverBrushSurfaces[i].brush;
				if (brush.ChildData == null ||
					brush.ChildData.Model == null)
					continue;
				try
				{
					var model = brush.ChildData.Model;
					updateModels.Add(model);
					if (updateBrushes.Add(brush))
					{
						// per brush
						if (!selectAllSurfaces)
						{
							var shape = brush.Shape;
							if (previousMaterials != null)
							{
								for (int t = 0; t < shape.TexGens.Length; t++)
								{
									var material = previousMaterials[i][t];
									shape.TexGens[t].RenderMaterial = material;
								}
							}
						} else
						{
							var shape = brush.Shape;
							var surfaceTexGens = shape.TexGens.ToArray();
							if (dragMaterials.Count > 1)
							{
								for (int m = 0; m < surfaceTexGens.Length; m++)
								{
									var material = dragMaterials[Random.Range(0, dragMaterials.Count)];
									surfaceTexGens[m].RenderMaterial = material;
								}
							} else
							{
								var materialInstance = dragMaterials[0];
								for (int m = 0; m < surfaceTexGens.Length; m++)
								{
									surfaceTexGens[m].RenderMaterial = materialInstance;
								}
							}
							shape.TexGens = surfaceTexGens;
						}
					}

					// per surface
					if (!selectAllSurfaces)
					{
						var highlight_surface	= hoverBrushSurfaces[i].surfaceIndex;
						if (highlight_surface >= 0)
						{ 
							var highlight_texGen	= brush.Shape.Surfaces[highlight_surface].TexGenIndex;

							Material dragMaterial;
							if (dragMaterials.Count > 1)
								dragMaterial = dragMaterials[Random.Range(0, dragMaterials.Count)];
							else
								dragMaterial = dragMaterials[0];

							brush.Shape.TexGens[highlight_texGen].RenderMaterial = dragMaterial;
						}
					}
				}
				finally {}
			}
			UpdateBrushMeshes(updateBrushes, updateModels);
		}
		#endregion

		#region DragPerform
		public void DragPerform(SceneView sceneView)
		{
			if (hoverBrushSurfaces == null)
				return;
			
			RestoreMaterials(hoverBrushSurfaces);

			if (hoverBrushSurfaces != null)
			{
				//using (new UndoGroup(hoverBrushSurfaces, "Modified materials"))
				{
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
						if (sceneview == null)
							continue;

						if (sceneview.camera.pixelRect.Contains(Event.current.mousePosition))
							sceneview.Focus();
					}
				}
			}
			hoverBrushSurfaces = null;
			previousMaterials = null;
		}
		#endregion

		#region DragExited
		public void DragExited(SceneView sceneView)
		{
			Undo.RevertAllInCurrentGroup();
			if (hoverBrushSurfaces != null)
			{
				//using (new UndoGroup(hoverBrushSurfaces, "Modified materials"))
				{
					RestoreMaterials(hoverBrushSurfaces);
					InternalCSGModelManager.UpdateMeshes();
				}
			}
			hoverBrushSurfaces = null;
			HandleUtility.Repaint();
		}
		#endregion

		#region Paint
		public void OnPaint(Camera camera)
		{
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
