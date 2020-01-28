using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using InternalRealtimeCSG;
using RealtimeCSG.Legacy;
using RealtimeCSG.Components;

namespace RealtimeCSG
{
	[Serializable]
	internal sealed class BackupBrushData
	{
		public Shape				shape;
		public CSGModel				model;
		public ControlMesh			controlMesh;
		public CSGBrush				brush;		// to ensure that we don't copy things to wrong brush
		public CSGBrush				brushCopy;
		public bool					keepCopy;
		public bool					active;
		public Matrix4x4			localToWorldMatrix;
		public GeometryWireframe	wireFrame;
		public bool					removeOnCommit;
	}
	
	internal sealed class EditModeClip : ScriptableObject, IEditMode
	{
		public bool UsesUnitySelection	{ get { return false; } }
		public bool IgnoreUnityRect		{ get { return true; } }

		[SerializeField] BackupBrushData[]	backupData = new BackupBrushData[0];

		[SerializeField] AABB			bounds;
		[SerializeField] Vector3		boundsCenter;

		[SerializeField] CSGBrush[]		clipBrushes     = new CSGBrush[0];
		[SerializeField] Vector3[]		points			= new Vector3[3];
		[SerializeField] int			pointsUsed		= 0;
		[SerializeField] int			pointsShown		= 0;
		
		[SerializeField] CSGPlane		clipPlane;
		[SerializeField] bool			clipPlaneValid;
//		[NonSerialized] Plane			prevClipPlane;
//		[NonSerialized] bool			prevClipPlaneValid;
		[SerializeField] Vector3		clipPlaneTangent;
		[SerializeField] Vector3		clipPlaneBinormal;
//		[NonSerialized] int				undoGroupIndex	= -1;

		[SerializeField] CSGPlane		movePlane;
		[NonSerialized] CSGPlane		prevMovePlane;
		
		[NonSerialized] bool			commitOnClick   = false;
		[NonSerialized] bool			mouseDragged    = false;
		[NonSerialized] int				planeCreationID = -1;

		[NonSerialized] List<Vector3>	visualSnappedLines; // used to visualize lines which we're snapping against
		
		[NonSerialized] CSGBrush		currentOnBrush;
		[NonSerialized] Vector3?		currentMousePoint;
		[NonSerialized] Vector3?		prevMousePoint;
		[NonSerialized] bool			forceWireframeUpdate = false;

//		[SerializeField] CSGBrush[]		copyTargets;

//		[NonSerialized] bool			onHandle		= false;
		[NonSerialized] bool			isEnabled		= false;
		[NonSerialized] bool			hideTool		= false;

		internal enum EditMode
		{
			None,
			CreatingPoint1,
			CreatingPoint2,
			EditPoints
		};
		
		[SerializeField] internal EditMode editMode = EditMode.None;
		[SerializeField] internal ClipMode clipMode = ClipMode.RemovePositive;
			 
		static readonly int planeCreationHash		= "planeCreation".GetHashCode();
		static readonly int clipEditBrushToolHash	= "ClipEditBrushTool".GetHashCode();

		public int ClipBrushCount { get { return clipBrushes.Length; } }

		public void SetTargets(FilteredSelection filteredSelection)
		{
			var foundBrushes = filteredSelection.GetAllContainedBrushes();
			clipBrushes = foundBrushes.ToArray();
			forceWireframeUpdate = true;
			hideTool = filteredSelection.NodeTargets.Length > 0;

			if (!isEnabled)
				return;
			
			if (backupData.Length > 0) // check if we have -any- brushes at all
			{
				// check if we already have a clip-plane, so we need to add/remove brushes
				if (clipPlaneValid)
				{
					var removedBrushes = new HashSet<int>();
					for (int i = backupData.Length - 1; i >= 0; i--)
					{
						if (foundBrushes.Contains(backupData[i].brush))
							continue;
						removedBrushes.Add(i);
					}

					if (removedBrushes.Count > 0)
					{
						RestoreTargetData(false, removedBrushes);
						foreach (var index in removedBrushes)
						{
							ArrayUtility.RemoveAt(ref backupData, index);
						}
					}

					var addedBrushes = new List<CSGBrush>();
					foreach (var foundBrush in foundBrushes)
					{
						bool found = false;
						for (int i = 0; i < backupData.Length; i++)
						{
							if (backupData[i].brush == foundBrush)
							{
								found = true;
								break;
							}
						}
						if (!found)
							addedBrushes.Add(foundBrush);
					}

					if (addedBrushes.Count > 0)
					{
						var addedBrushesArray = addedBrushes.ToArray();
						int offset = RememberTargetData(addedBrushesArray);
						UpdateTargetClipping(offset);
					}
				} else
				{
					backupData = new BackupBrushData[0];
				}
			}

			Tools.hidden = hideTool;
			UpdateBounds();

			// if we have a clip-plane but no brushes, then just start normally
			if (clipPlaneValid && backupData.Length == 0)
			{
				RememberTargetData(); // need to remember this before doing undo because unity will mess it up the undo and we'll need to fix it
				RecordAllObjects("Updated selection");
				Undo.FlushUndoRecordObjects();
				UpdateTargetClipping();
			}
			CSG_EditorGUIUtility.RepaintAll();
		}
		
		public void OnEnableTool()
		{
			isEnabled		= true;
			Tools.hidden	= hideTool;
			clipMode		= RealtimeCSG.CSGSettings.ClipMode;
			ResetTool();
			
			UpdateBounds();
			//needUpdate = true;
			forceWireframeUpdate = true;
		}

		public void OnDisableTool()
		{
			if (editMode != EditMode.None)
				Cancel();

			//needUpdate = false;
			Tools.hidden	= false;
			isEnabled		= false;
			ResetTool();
		}
				
		void ResetTool()
		{
			prevMousePoint = null;
			currentMousePoint = null;
			ResetEditMode();
			if (RealtimeCSG.CSGGrid.ForceGrid)
			{
				RealtimeCSG.CSGGrid.ForceGrid = false;
				CSG_EditorGUIUtility.RepaintAll();
			}
			forceWireframeUpdate = true;
		}
				
		void ResetEditMode()
		{
			editMode			= EditMode.None;
			clipPlaneValid		= false;
			pointsUsed			= 0;
			forceWireframeUpdate = true;
		}

		void UpdateBounds()
		{
			bounds = BoundsUtilities.GetBounds(clipBrushes);
			boundsCenter = bounds.Center;
			bounds.MinX = ((bounds.MinX - boundsCenter.x) * 4.0f) + boundsCenter.x;
			bounds.MinY = ((bounds.MinY - boundsCenter.y) * 4.0f) + boundsCenter.y;
			bounds.MinZ = ((bounds.MinZ - boundsCenter.z) * 4.0f) + boundsCenter.z;
			bounds.MaxX = ((bounds.MaxX - boundsCenter.x) * 4.0f) + boundsCenter.x;
			bounds.MaxY = ((bounds.MaxY - boundsCenter.y) * 4.0f) + boundsCenter.y;
			bounds.MaxZ = ((bounds.MaxZ - boundsCenter.z) * 4.0f) + boundsCenter.z;
		}	
		
		public bool DeselectAll()
		{
			if (editMode != EditMode.None)
			{
				forceWireframeUpdate = true;
				Cancel();
				return true;
			}
			
			if (clipBrushes != null &&
				clipBrushes.Length != 0)
			{
				ResetEditMode();
			}
			
			Selection.activeTransform = null;
			forceWireframeUpdate = true;
			return true;
		}

		public void Commit()
		{
			InternalCSGModelManager.skipCheckForChanges = true;
			try
			{
				Undo.FlushUndoRecordObjects();
				Undo.IncrementCurrentGroup();
				Undo.RegisterCompleteObjectUndo(this, "Finalized clipping");
				var selectedObjects = new List<UnityEngine.Object>();
				if (backupData.Length > 0)
				{
					for (int t = 0; t < backupData.Length; t++)
					{
						if (backupData[t].brush)
						{							
							backupData[t].brush.gameObject.hideFlags = HideFlags.None;
							if (backupData[t].removeOnCommit)
							{
								Undo.DestroyObjectImmediate(backupData[t].brush.gameObject);
								continue;
							} else
								selectedObjects.Add(backupData[t].brush.gameObject);
						}

						if (backupData[t].brushCopy)
						{
							backupData[t].brushCopy.gameObject.hideFlags = HideFlags.None;
							if (!backupData[t].brushCopy.isActiveAndEnabled)
							{
								Undo.DestroyObjectImmediate(backupData[t].brushCopy.gameObject);
								continue;
							} else
								selectedObjects.Add(backupData[t].brushCopy.gameObject);
						}
					}
				}

				InternalCSGModelManager.CheckForChanges();
				for (var t = 0; t < clipBrushes.Length; t++)
				{
					if (!clipBrushes[t])
						continue;

					BrushOutlineManager.ForceUpdateOutlines(clipBrushes[t].brushNodeID);
				}

				UpdateBounds();
				ResetEditMode();

				if (selectedObjects.Count > 0)
					Selection.objects = selectedObjects.ToArray();
			}
			finally
			{
				InternalCSGModelManager.skipCheckForChanges = false;
				CSG_EditorGUIUtility.RepaintAll();
				forceWireframeUpdate = true;
			}
		}

		public void Cancel()
		{
			InternalCSGModelManager.skipCheckForChanges = true;
			try
			{
				Undo.FlushUndoRecordObjects();
				Undo.IncrementCurrentGroup();
				Undo.RegisterCompleteObjectUndo(this, "Canceling clipping");

				for (int t = 0; t < backupData.Length; t++)
				{
					if (backupData[t].brush)
						backupData[t].brush.gameObject.hideFlags = HideFlags.None;

					if (backupData[t].brushCopy)
						backupData[t].brushCopy.gameObject.hideFlags = HideFlags.None;
				}

				RestoreTargetData();

				InternalCSGModelManager.CheckForChanges();
				for (var t = 0; t < clipBrushes.Length; t++)
					BrushOutlineManager.ForceUpdateOutlines(clipBrushes[t].brushNodeID);

				UpdateBounds();
				ResetEditMode();
			}
			finally
			{
				InternalCSGModelManager.skipCheckForChanges = false;
				CSG_EditorGUIUtility.RepaintAll();
				//backupData = null;
				forceWireframeUpdate = true;
			}
			
		}
		
		int RememberTargetData(CSGBrush[] addedBrushes = null)
		{
			if (addedBrushes != null)
			{
				if (addedBrushes.Length == 0)
					return 0;
			} else 
			if (clipBrushes == null || clipBrushes.Length == 0)
			{
				backupData = new BackupBrushData[0];
				return 0;
			}
			
			CSGBrush[] srcBrushes;
			int offset = 0;
			if (addedBrushes != null)
			{
				srcBrushes = addedBrushes;
				if (backupData.Length == 0)
				{
					backupData = new BackupBrushData[addedBrushes.Length];
				} else
				{
					offset = backupData.Length;
					Array.Resize(ref backupData, backupData.Length + addedBrushes.Length);
				}
			} else
			{ 
				srcBrushes = clipBrushes;
				backupData = new BackupBrushData[clipBrushes.Length];
			}
			for (int srcIndex = srcBrushes.Length - 1; srcIndex >= 0; srcIndex--)
			{
				var dstIndex = srcIndex + offset;
				var srcBrush = srcBrushes[srcIndex];
				if (!srcBrush)
				{ 
					ArrayUtility.RemoveAt(ref backupData, dstIndex);
					continue;
				}
				
				backupData[dstIndex] = new BackupBrushData();
				backupData[dstIndex].brush			= srcBrush;
				backupData[dstIndex].removeOnCommit	= false;
				backupData[dstIndex].active			= srcBrush.gameObject.activeSelf;
				backupData[dstIndex].controlMesh	= srcBrush.ControlMesh.Clone();
				backupData[dstIndex].shape			= srcBrush.Shape.Clone();
				backupData[dstIndex].wireFrame		= BrushOutlineManager.GetBrushOutline(srcBrush.brushNodeID);
				
				if (srcBrush.ChildData == null ||
					!srcBrush.ChildData.Model)
				{
					backupData[dstIndex].localToWorldMatrix	= Matrix4x4.identity;
					backupData[dstIndex].model				= null;
					continue;
				}
				
				backupData[dstIndex].localToWorldMatrix	= srcBrush.compareTransformation.localToWorldMatrix;
				backupData[dstIndex].model				= srcBrush.ChildData.Model;
			}
			return offset;
		}

		void RestoreTargetData(bool hoverMode = false, HashSet<int> selectedIndices = null)
		{
			if (backupData.Length == 0)
			{
				return;
			}
			try
			{ 
				InternalCSGModelManager.skipCheckForChanges = true;
				var uniqueModels = new HashSet<CSGModel>();
				bool need_refresh = false;
				for (int t = 0; t < backupData.Length; t++)
				{
					if (selectedIndices != null &&
						!selectedIndices.Contains(t))
						continue;

					var model = backupData[t].model;
					if (!model)
						continue;

					var brush = backupData[t].brush;
					if (!brush)
						continue;

					if (brush.gameObject.activeSelf != backupData[t].active)
					{
						brush.gameObject.SetActive(backupData[t].active);
					}
						
					if (!backupData[t].brush.IsRegistered && backupData[t].brush.isActiveAndEnabled)
					{
						InternalCSGModelManager.OnCreated(backupData[t].brush);
						need_refresh = true;
					}
				}

				if (need_refresh)
					InternalCSGModelManager.CheckForChanges(true);

				for (int t = backupData.Length - 1; t >= 0; t--)
				{
					if (selectedIndices != null &&
						!selectedIndices.Contains(t))
						continue;

					var model = backupData[t].model;
					if (!model)
						continue;

					var brush = backupData[t].brush;
					if (!brush)
						continue;
					
					if (backupData[t].brushCopy)
					{
						if (!hoverMode)
						{
							Undo.DestroyObjectImmediate(backupData[t].brushCopy.gameObject);
						} else
						{
							if (backupData[t].brushCopy.gameObject.activeSelf)
							{
								backupData[t].brushCopy.gameObject.SetActive(false);
							}
						}
					}

					//if (brush.ControlMesh.Generation != backupData[t].controlMesh.Generation)
					{
						brush.ControlMesh	= backupData[t].controlMesh.Clone();
						brush.Shape			= backupData[t].shape.Clone();
						ControlMeshUtility.RebuildShape(brush);

						if (brush.ControlMesh != null)
							brush.ControlMesh.SetDirty();
						InternalCSGModelManager.CheckSurfaceModifications(brush, true);
					}

					uniqueModels.Add(model);
				}
				
				if (!hoverMode)
				{
					InternalCSGModelManager.CheckForChanges(true);
				} else
				{
					InternalCSGModelManager.CheckTransformChanged();
					InternalCSGModelManager.OnHierarchyModified();
					InternalCSGModelManager.UpdateRemoteMeshes();
					//CSGModelManager.Refresh(true);
					//CSG_EditorGUIUtility.RepaintAll();
				}
			}
			finally
			{
				InternalCSGModelManager.skipCheckForChanges = false;
			}
		}

		enum ClipSide
		{
			Invalid,
			CompletelyInside,
			CompletelyOutside,
			Split
		};
		
		void UpdateTargetClipping(int offset = 0)
		{
			if (!clipPlaneValid ||
				backupData.Length == 0)
				return;

			CSGPlane cuttingPlane = clipPlane;
			bool needCopies = false;
			switch (clipMode)
			{
				case ClipMode.Split:		  { needCopies = true; break; }
				case ClipMode.RemovePositive: { break; }
				case ClipMode.RemoveNegative: { cuttingPlane = cuttingPlane.Negated(); break; }
			}

			var foundValidBrushes	= false;
			var clipSides			= new ClipSide[backupData.Length];
			var clipLocalPlane		= new CSGPlane[backupData.Length];
			for (int t = backupData.Length - 1; t >= offset; t--)
			{
				var model = backupData[t].model;
				if (!model)
				{
					clipSides[t] = ClipSide.Invalid;
					continue;
				}

				var brush = backupData[t].brush;
				if (!brush)
				{
					clipSides[t] = ClipSide.Invalid;
					continue;
				}

				backupData[t].removeOnCommit	= false;
				
				var brush_transform		= brush.GetComponent<Transform>();
				var matrix				= brush_transform.localToWorldMatrix;
				var localCuttingPlane	= GeometryUtility.InverseTransformPlane(matrix, cuttingPlane);

				var vertices			= backupData[t].controlMesh.Vertices;

				int inFrontCount = 0;
				int behindCount = 0;

				for (int p = 0; p < vertices.Length; p++)
				{
					if (localCuttingPlane.Distance(vertices[p]) > MathConstants.DistanceEpsilon)
						behindCount++;
					else
						inFrontCount++;
				}

				
				if		((inFrontCount != 0) && (behindCount != 0)) clipSides[t] = ClipSide.Split;
				else if ((inFrontCount == 0) && (behindCount >  0)) clipSides[t] = ClipSide.CompletelyInside;
				else											    clipSides[t] = ClipSide.CompletelyOutside;

				clipLocalPlane[t] = localCuttingPlane;
				foundValidBrushes = true;
			}

			if (!foundValidBrushes)
			{
				return;
			}

			try
			{ 
				InternalCSGModelManager.skipCheckForChanges = true;
				for (int t = backupData.Length - 1; t >= offset; t--)
				{
					if (clipSides[t] == ClipSide.Invalid)
						continue;
					
					var brush				= backupData[t].brush;
//					var model				= backupData[t].model;
					var localCuttingPlane	= clipLocalPlane[t];

					brush.ControlMesh	= backupData[t].controlMesh.Clone();
					brush.Shape			= backupData[t].shape.Clone();
					if (brush.gameObject.activeSelf != backupData[t].active)
					{
						brush.gameObject.SetActive(backupData[t].active);
					}
					
					backupData[t].keepCopy = false;

					if (clipSides[t] == ClipSide.CompletelyOutside)
						continue;

					//*
					if (clipSides[t] == ClipSide.Split)
					{
						//CSGBrush copy_brush = null;
						var selected_edges = new List<int>();
						if (ControlMeshUtility.CutMesh(brush.ControlMesh, brush.Shape, localCuttingPlane, ref selected_edges))
						{
							var edge_loop = ControlMeshUtility.FindEdgeLoop(brush.ControlMesh, ref selected_edges);
							if (edge_loop != null)
							{
								if (ControlMeshUtility.SplitEdgeLoop(brush.ControlMesh, brush.Shape, edge_loop))
								{
									Shape foundShape;
									ControlMesh foundControlMesh;
									if (ControlMeshUtility.FindAndDetachSeparatePiece(brush.ControlMesh, brush.Shape, localCuttingPlane, out foundControlMesh, out foundShape))
									{
										if (needCopies)
										{
											if (backupData[t].brushCopy == null)
												backupData[t].brushCopy = GetDeepCopy(brush);

											backupData[t].brushCopy.Shape		= brush.Shape.Clone();
											backupData[t].brushCopy.ControlMesh	= brush.ControlMesh.Clone();
											backupData[t].brushCopy.ControlMesh.SetDirty();
											backupData[t].brushCopy.gameObject.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSaveInBuild;
											backupData[t].keepCopy = true;
										}

										brush.Shape			= foundShape;
										brush.ControlMesh	= foundControlMesh;
										brush.ControlMesh.SetDirty();
									}
								}
							}
						}
						if (backupData[t].brushCopy != null)
						{
							var new_active = !needCopies ? false : (backupData[t].keepCopy ? backupData[t].active : false);
							if (backupData[t].brushCopy.gameObject.activeSelf != new_active)
							{
								backupData[t].brushCopy.gameObject.SetActive(new_active);
							}
						}
					} else
					{ 
						if (backupData[t].brushCopy != null)
						{
							if (backupData[t].brushCopy.gameObject.activeSelf != needCopies)
							{
								backupData[t].brushCopy.gameObject.SetActive(needCopies);
							}
						}
						if (clipSides[t] == ClipSide.CompletelyInside)
						{
							backupData[t].removeOnCommit = true;
							if (brush.gameObject.activeSelf)
							{
								brush.gameObject.SetActive(false);
							}
						}
					}
				}

				var uniqueModels = new HashSet<CSGModel>();
				bool have_copy = false;
				for (int t = offset; t < backupData.Length; t++)
				{
					if (clipSides[t] == ClipSide.Invalid)
						continue;
						
					if (backupData[t].brush &&
						!backupData[t].brush.IsRegistered && backupData[t].brush.isActiveAndEnabled)
					{
						InternalCSGModelManager.OnCreated(backupData[t].brush);
						have_copy = true;
					}
					if (needCopies && 
						backupData[t].brushCopy &&
						!backupData[t].brushCopy.IsRegistered && backupData[t].brushCopy.isActiveAndEnabled)
					{
						InternalCSGModelManager.OnCreated(backupData[t].brushCopy);;
						have_copy = true;
					}
				}

				if (have_copy)
					InternalCSGModelManager.CheckForChanges(true);
				for (int t = offset; t < backupData.Length; t++)
				{
					if (clipSides[t] == ClipSide.Invalid)
						continue;
					 
					uniqueModels.Add(backupData[t].model);

					if (needCopies && backupData[t].keepCopy && backupData[t].brushCopy && backupData[t].brushCopy.gameObject.activeSelf)
					{
						ControlMeshUtility.RebuildShape(backupData[t].brushCopy);
						InternalCSGModelManager.CheckSurfaceModifications(backupData[t].brushCopy, true);
						if (backupData[t].brushCopy.ControlMesh != null)
							backupData[t].brushCopy.ControlMesh.SetDirty();
						InternalCSGModelManager.CheckSurfaceModifications(backupData[t].brushCopy, true);
					}
					
					ControlMeshUtility.RebuildShape(backupData[t].brush);
					if (backupData[t].removeOnCommit)
						continue;
					if (backupData[t].brush.ControlMesh != null)
						backupData[t].brush.ControlMesh.SetDirty();
					InternalCSGModelManager.CheckSurfaceModifications(backupData[t].brush, true);
				}
				
				InternalCSGModelManager.CheckForChanges(true);
			}
			finally
			{
				InternalCSGModelManager.skipCheckForChanges = false;
			}
		}
			

		static CSGBrush GetDeepCopy(CSGBrush brush)
		{
			var srcGameObject = brush.gameObject;
			var srcTransform  = brush.transform;
			var dstGameObject = UnityEngine.Object.Instantiate(srcGameObject);
			dstGameObject.transform.SetParent(srcTransform.parent, false);
			dstGameObject.transform.SetSiblingIndex(srcTransform.GetSiblingIndex() + 1);
			dstGameObject.transform.localPosition	= srcTransform.localPosition;
			dstGameObject.transform.localRotation	= srcTransform.localRotation;
			dstGameObject.transform.localScale		= srcTransform.localScale;
			dstGameObject.name = GameObjectUtility.GetUniqueNameForSibling(srcTransform.parent, srcGameObject.name);
			
			var copy = dstGameObject.GetComponent<CSGBrush>();

			Undo.RegisterCreatedObjectUndo(dstGameObject, "Created copy");
			return copy;
		}		
		
		void RecordAllObjects(string text)
		{
			Undo.RegisterCompleteObjectUndo(this, text);
		}

		public void SetClipMode(ClipMode newClipMode)
		{
			if (clipMode == newClipMode)
				return;
			Undo.RegisterCompleteObjectUndo(this, "Updated clip mode");
			try
			{
				clipMode = newClipMode;
				RealtimeCSG.CSGSettings.ClipMode = clipMode;
				RealtimeCSG.CSGSettings.Save();
				if (clipPlaneValid)
					UpdateTargetClipping();
				else
					RestoreTargetData();
				Undo.FlushUndoRecordObjects();
			}
			finally
			{
				CSG_EditorGUIUtility.RepaintAll();
			}
		}
		
		public bool UndoRedoPerformed()
		{
			try
			{
				if (clipPlaneValid)
					UpdateTargetClipping();
				else
					RestoreTargetData();
				forceWireframeUpdate = true;
			}
			finally
			{
				CSG_EditorGUIUtility.RepaintAll();
			}
			return true;
		}

		void UpdateClipPlane()
		{
			if (!CreateClipPlane())
			{
				points[2] = points[1] - (movePlane.normal * 2.0f);
				CreateClipPlane();
			}

			if (!clipPlaneValid)
			{
//				if (prevClipPlaneValid)
				{
//					prevClipPlane = clipPlane;
//					prevClipPlaneValid = clipPlaneValid;
					RestoreTargetData();
					CSG_EditorGUIUtility.RepaintAll();
				}
			} else
			//if (!prevClipPlaneValid ||
			//	prevClipPlane != clipPlane)
			{
//				prevClipPlane = clipPlane;
//				prevClipPlaneValid = clipPlaneValid;
				UpdateTargetClipping();
				CSG_EditorGUIUtility.RepaintAll();
			}			
		}


		bool CreateClipPlane()
		{
			if (Mathf.Abs(movePlane.Distance(points[0])) < MathConstants.DistanceEpsilon &&
				Mathf.Abs(movePlane.Distance(points[1])) < MathConstants.DistanceEpsilon &&
				Mathf.Abs(movePlane.Distance(points[2])) < MathConstants.DistanceEpsilon)
				return false;
			var newClipPlane = new CSGPlane(points[1], points[0], points[2]);
			if (float.IsNaN(newClipPlane.a) || float.IsInfinity(newClipPlane.a) ||
				float.IsNaN(newClipPlane.b) || float.IsInfinity(newClipPlane.b) ||
				float.IsNaN(newClipPlane.c) || float.IsInfinity(newClipPlane.c) ||
				float.IsNaN(newClipPlane.d) || float.IsInfinity(newClipPlane.d) ||
				newClipPlane.normal.magnitude < 0.75f)
			{
				clipPlaneValid = false;
				return false;
			} else
			{
				clipPlane = newClipPlane;
				clipPlaneValid = true;
			}
			
			GeometryUtility.CalculateTangents(clipPlane.normal, out clipPlaneTangent, out clipPlaneBinormal);
			return true;
		}

		void UpdateMouseIntersection(SceneView sceneView)
		{
			var camera = sceneView.camera;
            var assume2DView = CSGSettings.Assume2DView(camera);

			var world_ray	= HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
			LegacyBrushIntersection intersection;
			currentOnBrush = null;
			if (SceneQueryUtility.FindWorldIntersection(camera, world_ray, out intersection))
			{
				movePlane = intersection.worldPlane;
				currentOnBrush = intersection.brush;
				currentMousePoint = intersection.worldIntersection;
				RealtimeCSG.CSGGrid.SetForcedGrid(camera, movePlane);
			} else
			{
				RealtimeCSG.CSGGrid.ForceGrid = false;

				if (pointsUsed > 0 && camera != null && assume2DView)
				{
					movePlane = new CSGPlane(camera.transform.forward, points[0]);
				} else
					movePlane = RealtimeCSG.CSGGrid.CurrentGridPlane;					
				
				Vector3 intersectionPoint;
				if (movePlane.TryRayIntersection(world_ray, out intersectionPoint))
					currentMousePoint = intersectionPoint;
				else
					currentMousePoint = null;
				//var activeTransform = Selection.activeTransform;
			}
			
			visualSnappedLines = null;
			if (currentMousePoint.HasValue)
			{
				CSGBrush snapOnBrush;
				currentMousePoint = EditModeManager.SnapPointToGrid(camera, currentMousePoint.Value, movePlane, ref visualSnappedLines, out snapOnBrush, null);
				if (snapOnBrush != null)
					currentOnBrush = snapOnBrush;
			}			
//			onHandle = false;
			if (prevMovePlane != movePlane || currentMousePoint != prevMousePoint)
			{
				prevMovePlane = movePlane;
				CSG_EditorGUIUtility.RepaintAll();
				prevMousePoint = currentMousePoint;
			}

		}

		MouseCursor currentCursor = MouseCursor.Arrow;
//		MouseCursor lastUsedCursor = MouseCursor.Arrow;
		void UpdateMouseCursor()
		{
			if (GUIUtility.hotControl != 0)
				return;
			
			switch (SelectionUtility.GetEventSelectionType())
			{
				case SelectionType.Additive:	currentCursor = MouseCursor.ArrowPlus; break;
				case SelectionType.Subtractive: currentCursor = MouseCursor.ArrowMinus; break;
				case SelectionType.Toggle:		currentCursor = MouseCursor.Arrow; break;
				default:						currentCursor = MouseCursor.Arrow; break;
			}
		}

		
		Vector3 HandleMovement(SceneView sceneView, Vector2 startMousePosition, Vector2 currentMousePosition)
		{
			//ResetTargets(true);
			RestoreTargetData(hoverMode: true);
			UpdateMouseIntersection(sceneView);
			if (currentMousePoint.HasValue)
			{
				return currentMousePoint.Value;
			} else
				return MathConstants.zeroVector3;
		}
		
		void CreateFirstPoint(SceneView sceneView)
		{
			if (!currentMousePoint.HasValue)
				return;


			var camera = sceneView.camera;
            var assume2DView = CSGSettings.Assume2DView(camera);


			var intersectionPoint = currentMousePoint.Value;
			
			points[0] =  
			points[1] = 
			points[2] = intersectionPoint;

			var normal = movePlane.normal;
			if (camera != null && assume2DView)
				normal = -camera.transform.forward;
			
			if (currentOnBrush != null)
			{
				if (currentOnBrush.ChildData != null &&
					currentOnBrush.ChildData.ModelTransform != null)
				{
					var ray_start	= intersectionPoint - (normal * camera.farClipPlane);
					var ray_end		= intersectionPoint + (normal * camera.farClipPlane);
					
					LegacyBrushIntersection intersectionFar;
					if (SceneQueryUtility.FindBrushIntersection(currentOnBrush, currentOnBrush.ChildData.ModelTransform.localToWorldMatrix, ray_start, ray_end, out intersectionFar))
					{
						points[2] = intersectionFar.worldIntersection;
					}
				}
			}

			if ((clipBrushes == null || clipBrushes.Length == 0) && currentOnBrush)
			{
				Selection.objects = new UnityEngine.Object[1] { currentOnBrush.gameObject };
				EditModeManager.UpdateSelection();
			}

			RememberTargetData(); // need to remember this before doing undo because unity will mess it up the undo and we'll need to fix it

			RecordAllObjects("Created clip plane");

			if ((points[2] - points[1]).sqrMagnitude < 0.1f)
			{
				var direction = -normal;
				var snapVector = RealtimeCSG.CSGGrid.CurrentGridSnapVector;
				direction.x *= Mathf.Max(1.0f, snapVector.x);
				direction.y *= Mathf.Max(1.0f, snapVector.y);
				direction.z *= Mathf.Max(1.0f, snapVector.z);
				points[2] = points[1] + direction;
			}

			pointsUsed = 1;
			pointsShown = 1;
			editMode = EditMode.CreatingPoint1;
		}

		void CreateControlIDs()
		{
			int meshEditBrushToolID = GUIUtility.GetControlID(clipEditBrushToolHash, FocusType.Passive);
			HandleUtility.AddDefaultControl(meshEditBrushToolID);
			planeCreationID = GUIUtility.GetControlID(planeCreationHash, FocusType.Keyboard);
		}
		
		public void HandleEvents(SceneView sceneView, Rect sceneRect)
		{
			var originalEventType = Event.current.type;
			if      (originalEventType == EventType.MouseDown ||
					 originalEventType == EventType.MouseMove) { mouseDragged = false; }
			else if (originalEventType == EventType.MouseDrag) { mouseDragged = true; }


			//if (Event.current.type == EventType.Layout)
				CreateControlIDs();
			
			switch (editMode)
			{
				case EditMode.None:
				case EditMode.CreatingPoint1:
				case EditMode.CreatingPoint2:
				{
					HandleCreatePointEvents(sceneView, sceneRect);
					break;
				}
				case EditMode.EditPoints:					
				{
					HandleEditPointEvents(sceneView, sceneRect);
					break;
				}
			}
			
			if (originalEventType == EventType.MouseUp) { mouseDragged = false; }
		}
		
		[NonSerialized] LineMeshManager zTestLineMeshManager2	= new LineMeshManager();
		[NonSerialized] LineMeshManager noZTestLineMeshManager2	= new LineMeshManager(); 
		[NonSerialized] LineMeshManager zTestLineMeshManager1	= new LineMeshManager();
		[NonSerialized] LineMeshManager noZTestLineMeshManager1	= new LineMeshManager(); 
		[NonSerialized] int				lastLineMeshGeneration	= -1;
		
		void OnDestroy()
		{
			zTestLineMeshManager1.Destroy();
			noZTestLineMeshManager1.Destroy();
			zTestLineMeshManager2.Destroy();
			noZTestLineMeshManager2.Destroy();
		}


		void OnPaint(SceneView sceneView)
		{					
			if (sceneView != null)
			{
				Rect windowRect = new Rect(0, 0, sceneView.position.width, sceneView.position.height - CSG_GUIStyleUtility.BottomToolBarHeight);
				if (currentCursor != MouseCursor.Arrow)
					EditorGUIUtility.AddCursorRect(windowRect, currentCursor);
			}
			
			if (lastLineMeshGeneration != InternalCSGModelManager.MeshGeneration)
			{
				forceWireframeUpdate = true;
				lastLineMeshGeneration = InternalCSGModelManager.MeshGeneration;
			}

			if (forceWireframeUpdate)
			{
				zTestLineMeshManager2.Clear();
				noZTestLineMeshManager2.Clear();
				if (clipPlaneValid && backupData.Length > 0)
				{
					var wireframes		= new List<GeometryWireframe>(backupData.Length);
					var transformations	= new List<Matrix4x4>(backupData.Length);
					for (int i = 0; i < backupData.Length; i++)
					{
						var model = backupData[i].model;
						if (!model)
							continue;

						var brush = backupData[i].brush;
						if (!brush)
							continue;

						var brush_transformation = backupData[i].localToWorldMatrix;
						wireframes.Add(backupData[i].wireFrame);
						transformations.Add(brush_transformation);
					}
					if (wireframes.Count > 0)
					{
						CSGRenderer.DrawSelectedBrushes(zTestLineMeshManager2, noZTestLineMeshManager2,
							wireframes.ToArray(), transformations.ToArray(),
							ColorSettings.SelectedOutlines, GUIConstants.thickLineScale);
					}
				}
				zTestLineMeshManager1.Clear();
				noZTestLineMeshManager1.Clear();
				if (clipBrushes != null)
				{
					var ids = new List<int>(clipBrushes.Length);
					var transformations = new List<Matrix4x4>(clipBrushes.Length);
					for (int i = 0; i < clipBrushes.Length; i++)
					{
						var brush = clipBrushes[i];
						if (!brush)
							continue;

						if (brush.ChildData == null ||
							brush.ChildData.ModelTransform == null)
							continue;

						var brush_transformation = brush.compareTransformation.localToWorldMatrix;
						ids.Add(brush.brushNodeID);
						transformations.Add(brush_transformation);
						//CSGRenderer.DrawSelectedBrush(brush.brushNodeID, brush_translation, ColorSettings.SelectedOutlines, GUIConstants.thickLineScale);
					}
					if (ids.Count > 0)
					{
						CSGRenderer.DrawSelectedBrushes(zTestLineMeshManager1, noZTestLineMeshManager1,
							ids.ToArray(), transformations.ToArray(),
							ColorSettings.SelectedOutlines, GUIConstants.thickLineScale);
					}
				}
			}
			
			forceWireframeUpdate = false;
			MaterialUtility.LineAlphaMultiplier = 1.0f;
			MaterialUtility.LineDashMultiplier = 2.0f;
			MaterialUtility.LineThicknessMultiplier = 2.0f;
			noZTestLineMeshManager2.Render(MaterialUtility.NoZTestGenericLine);
			noZTestLineMeshManager1.Render(MaterialUtility.NoZTestGenericLine);
			MaterialUtility.LineThicknessMultiplier = 1.0f;
			zTestLineMeshManager2.Render(MaterialUtility.ZTestGenericLine);
			zTestLineMeshManager1.Render(MaterialUtility.ZTestGenericLine);

			if (clipPlaneValid && pointsUsed == 3)
			{
				Vector3 pop;
				var x_dir	= clipPlaneTangent  * 1.5f;
				var y_dir	= clipPlaneBinormal * 1.5f;
						
				if (backupData.Length > 0)
				{
					pop	= GeometryUtility.ProjectPointOnPlane(clipPlane, boundsCenter);// clipPlane.pointOnPlane;
					float size_x, size_y;
					switch (GeometryUtility.GetPrincipleAxis(x_dir))
					{
						default:
						case PrincipleAxis.X: size_x = (bounds.MaxX - bounds.MinX); break;
						case PrincipleAxis.Y: size_x = (bounds.MaxY - bounds.MinY); break;
						case PrincipleAxis.Z: size_x = (bounds.MaxZ - bounds.MinZ); break;
					}
					x_dir *= Mathf.Max(100, size_x);
				
					switch (GeometryUtility.GetPrincipleAxis(y_dir))
					{
						default:
						case PrincipleAxis.X: size_y = (bounds.MaxX - bounds.MinX); break;
						case PrincipleAxis.Y: size_y = (bounds.MaxY - bounds.MinY); break;
						case PrincipleAxis.Z: size_y = (bounds.MaxZ - bounds.MinZ); break;
					}
					y_dir *= Mathf.Max(100, size_y);
				} else
				{
					pop = points[0];
					x_dir *= 100.0f;
					y_dir *= 100.0f;
				}
				
				var polygon_points = new Vector3[]{
						pop - x_dir - y_dir,
						pop + x_dir - y_dir,
						pop + x_dir + y_dir,
						pop - x_dir + y_dir
					};
				PaintUtility.DrawPolygon(polygon_points, ColorSettings.ClipPlaneFill);
				PaintUtility.DrawWirePolygon(polygon_points, ColorSettings.ClipPlaneOutline);
			}
		}
		
		public void HandleCreatePointEvents(SceneView sceneView, Rect sceneRect)
		{
//			onHandle = false;
			switch (Event.current.type)
			{
				case EventType.MouseDrag:
				//case EventType.MouseDown:
				{
					if (!sceneRect.Contains(Event.current.mousePosition))
						break;
					if (GUIUtility.hotControl == 0 && Event.current.button == 0)
					{
						if (Event.current.modifiers == EventModifiers.None)
						{
							RememberTargetData();
							GUIUtility.hotControl = planeCreationID;
							GUIUtility.keyboardControl = planeCreationID;
							EditorGUIUtility.editingTextField = false;

							visualSnappedLines	= null;
							commitOnClick		= false;
						
							editMode = EditMode.None;					
							pointsUsed = 0;
							pointsShown = 0;

							if (pointsUsed != 3)
							{
								ResetEditMode();
							}
					
							RestoreTargetData(hoverMode: true);
							UpdateMouseIntersection(sceneView);
						}
					}
					break;
				}
				case EventType.MouseUp:
				{
					if (GUIUtility.hotControl == 0 && Event.current.button == 0)
					{
						Event.current.Use();

						if (!mouseDragged)
						{
							SelectionUtility.DoSelectionClick(sceneView);
						}
					}
					break;
				}
				case EventType.Repaint:
				{
					if (SceneDragToolManager.IsDraggingObjectInScene)
						break;
					
					OnPaint(sceneView);
					
					if (currentMousePoint.HasValue)
					{ 
						//var world_ray	= HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
						Vector3 intersectionPoint = currentMousePoint.Value;
						//if (TryFindIntersection(world_ray, out intersectionPoint))
						{
							var prevMatrix = Handles.matrix;
							Handles.matrix = MathConstants.identityMatrix;
							float handleSize	= CSG_HandleUtility.GetHandleSize(intersectionPoint);
							Handles.color = ColorSettings.PointInnerStateColor[3];
							if (this.currentOnBrush == null)
								PaintUtility.DiamondDotCap(-1, intersectionPoint, MathConstants.identityQuaternion, handleSize * 1.2f * GUIConstants.handleScale);
							else
								PaintUtility.SquareDotCap(-1, intersectionPoint, MathConstants.identityQuaternion, handleSize * GUIConstants.handleScale);
							Handles.matrix = prevMatrix;
						}// else visualSnappedLines = null;
					}

					if (visualSnappedLines != null)
						PaintUtility.DrawLines(visualSnappedLines.ToArray(), GUIConstants.oldThinLineScale * 2.0f, ColorSettings.SnappedEdges);
					break;
				}
				case EventType.MouseMove:
				{
					var prevCursor = currentCursor;
					UpdateMouseCursor();
					UpdateMouseIntersection(sceneView);
					
					if (currentCursor != MouseCursor.ArrowPlus && 
						clipBrushes != null && 
						currentOnBrush && !ArrayUtility.Contains(clipBrushes, currentOnBrush))
					{
						currentCursor = MouseCursor.ArrowPlus;
					}
					if (prevCursor != currentCursor)
						CSG_EditorGUIUtility.RepaintAll();
					break;
				}
				case EventType.ValidateCommand:
				case EventType.KeyDown:
				{
					if (GUIUtility.hotControl == 0)
					{
						if (Keys.CycleClipModes.IsKeyPressed()) { Event.current.Use(); break; }
						if (Keys.HandleSceneKeyDown(EditModeManager.CurrentTool, false)) { Event.current.Use(); break; }
					}
					break;
				}

				case EventType.KeyUp:
				{
					if (GUIUtility.hotControl == 0)
					{
						if (Keys.CycleClipModes.IsKeyPressed()) { CycleClipMode(); Event.current.Use(); break; }
						if (Keys.HandleSceneKeyUp(EditModeManager.CurrentTool, false)) { Event.current.Use(); break; }
					}
					break;
				}
			}

			var type = Event.current.GetTypeForControl(planeCreationID);
			switch (type)
			{
				case EventType.MouseUp:
				{
					if (GUIUtility.hotControl == planeCreationID && Event.current.button == 0)
					{
						GUIUtility.hotControl = 0;
						GUIUtility.keyboardControl = 0;
						EditorGUIUtility.editingTextField = false;
						Event.current.Use();

						visualSnappedLines = null;

						if (!mouseDragged)
						{
							SelectionUtility.DoSelectionClick(sceneView);
							break;
						}

						if (pointsUsed == 3 && currentMousePoint.HasValue)
						{
							editMode = EditMode.EditPoints;
						} else
							ResetEditMode();
					}
					break;
				}

				case EventType.MouseDrag:
				{
					if (GUIUtility.hotControl == planeCreationID)
					{
						if (commitOnClick)
						{
							ResetEditMode();
							commitOnClick = false;
						}

						Event.current.Use();

						if (pointsUsed == 0)
							CreateFirstPoint(sceneView);

						if (pointsUsed != 0)
							RestoreTargetData(hoverMode: true);
						UpdateMouseIntersection(sceneView);

						if (currentMousePoint.HasValue)
						{
							points[0] = currentMousePoint.Value;
							if (editMode == EditMode.CreatingPoint2)
							{
								pointsUsed = 3;
								pointsShown = 2;
							} else
							{
								pointsUsed = 1;
								pointsShown = 1;
							}
						} else
						{
							if (editMode == EditMode.CreatingPoint2)
							{
								pointsUsed = 2;
								pointsShown = 2;
							} else
							{
								pointsUsed = 0;
								pointsShown = 0;
							}
						}
						if (editMode == EditMode.CreatingPoint1)
						{
							float handleSize = HandleUtility.GetHandleSize(points[0]);
							var magnitude = (points[0] - points[1]).magnitude;
							if (magnitude > handleSize * (GUIConstants.handleScale / 10.0f))
							{
								editMode = EditMode.CreatingPoint2;
								pointsUsed = 3;
								pointsShown = 2;
							}
						}

						if (pointsUsed == 3)
							UpdateClipPlane();

						CSG_EditorGUIUtility.RepaintAll();
					}
					break;
				}

				case EventType.ValidateCommand:
				case EventType.KeyDown:
				{
					if (GUIUtility.hotControl == planeCreationID)
					{ 
						if (Keys.CancelActionKey.IsKeyPressed()) { Event.current.Use(); break; }
						if (Keys.PerformActionKey.IsKeyPressed()) { Event.current.Use(); break; }
						if (Keys.CycleClipModes.IsKeyPressed()) { Event.current.Use(); break; }
						if (Keys.HandleSceneKeyDown(EditModeManager.CurrentTool, false)) { Event.current.Use(); break; }
					}
					break;
				}

				case EventType.KeyUp:
				{
					if (GUIUtility.hotControl == planeCreationID)
					{ 
						if (Keys.CancelActionKey.IsKeyPressed())
						{
							GUIUtility.hotControl = 0;
							GUIUtility.keyboardControl = 0;
							EditorGUIUtility.editingTextField = false;
							Event.current.Use();				
							SelectionUtility.DeselectAll();
							break;
						}
						if (Keys.PerformActionKey.IsKeyPressed())
						{
							GUIUtility.hotControl = 0;
							GUIUtility.keyboardControl = 0;
							EditorGUIUtility.editingTextField = false;
							Event.current.Use();
							Cancel();
							break;
						}
						if (Keys.CycleClipModes.IsKeyPressed()) { CycleClipMode(); Event.current.Use(); break; }
						if (Keys.HandleSceneKeyUp(EditModeManager.CurrentTool, false))
						{
							Event.current.Use();
							break;
						}
					}
					break;
				}
				case EventType.Repaint:
				{
					if (SceneDragToolManager.IsDraggingObjectInScene)
						break;

					if (GUIUtility.hotControl == planeCreationID)
					{ 
						if (visualSnappedLines != null)
							PaintUtility.DrawLines(visualSnappedLines.ToArray(), GUIConstants.oldThinLineScale * 2.0f, ColorSettings.SnappedEdges);					
						
						if (editMode != EditMode.CreatingPoint1)
						{ 
							var prevMatrix = Handles.matrix;
							Handles.matrix = MathConstants.identityMatrix;
							for (int i = 0; i < pointsShown; i++)
							{
								float handleSize	= CSG_HandleUtility.GetHandleSize(points[i]);
								Handles.color = (i==0) ? ColorSettings.PointInnerStateColor[3] : ColorSettings.PointInnerStateColor[0];
								PaintUtility.SquareDotCap(-1, points[i], MathConstants.identityQuaternion, handleSize * GUIConstants.handleScale);
							}
							Handles.matrix = prevMatrix;
						}

						if (visualSnappedLines != null)
							PaintUtility.DrawLines(visualSnappedLines.ToArray(), GUIConstants.oldThinLineScale * 2.0f, ColorSettings.SnappedEdges);
					}
					break;
				}
			}
		}

		private void CycleClipMode()
		{
			var newClipMode = (ClipMode)((int)this.clipMode + 1);
			if (newClipMode > ClipMode.Split)
				newClipMode = ClipMode.RemovePositive;
			SetClipMode(newClipMode);
		}
		
		Vector2 startMousePosition;	
		
		public void HandleEditPointEvents(SceneView sceneView, Rect sceneRect)
		{
			var camera = sceneView.camera;
            var assume2DView = CSGSettings.Assume2DView(camera);

			int[] ids = new int[3];
			float[] sizes = new float[3];
			for (int i = 0; i < 3; i++)
			{
				sizes[i] = CSG_HandleUtility.GetHandleSize(points[i]) * GUIConstants.handleScale;
				ids[i] = GUIUtility.GetControlID(SnappedPoint.SnappedPointHash, FocusType.Keyboard);
			}
			
			
			var prevMatrix = Handles.matrix;
			Handles.matrix = MathConstants.identityMatrix;
			try
			{
				switch (Event.current.type)
				{
					case EventType.Layout:
					{
						for (int i = 0; i < pointsShown; i++)
						{
							HandleUtility.AddControl(ids[i], HandleUtility.DistanceToCircle(points[i], sizes[i] * 1.2f));
						}
						break;
					}
					case EventType.MouseDown:
					{
						if (!sceneRect.Contains(Event.current.mousePosition))
							break;
						if (GUIUtility.hotControl == 0 && Event.current.button == 0)
						{
							visualSnappedLines = null;
							for (int i = 0; i < pointsShown; i++)
							{
								if (HandleUtility.nearestControl == ids[i])
								{
									Undo.RegisterCompleteObjectUndo(this, "Updated clip plane");

									movePlane = RealtimeCSG.CSGGrid.CurrentGridPlane;
									movePlane = new CSGPlane(movePlane.normal, points[i]);

									startMousePosition = Event.current.mousePosition;

									GUIUtility.hotControl = ids[i];
									GUIUtility.keyboardControl = ids[i];
									EditorGUIUtility.editingTextField = false;
									EditorGUIUtility.SetWantsMouseJumping(1);
									Event.current.Use();
									break;
								}
							}
						}
						break;
					}
					case EventType.MouseUp:
					{
						if (GUIUtility.hotControl == 0 && Event.current.button == 0)
						{
							if ((camera == null || !assume2DView) &&
								pointsShown == 2 && currentMousePoint.HasValue &&
								//(currentOnBrush == null || Selection.activeGameObject != null) &&
								Event.current.modifiers == EventModifiers.None)
							{
								pointsShown = 3;
								var intersectionPoint = currentMousePoint.Value;
								if (camera != null && assume2DView)
								{
									intersectionPoint = new CSGPlane(camera.transform.forward, points[0]).Project(intersectionPoint);
								}
								points[2] = intersectionPoint;
							} else
							if (!mouseDragged)
							{
								SelectionUtility.DoSelectionClick(sceneView);
							}
						}
						break;
					}
					case EventType.MouseDrag:
					{
						if (GUIUtility.hotControl == 0 && Event.current.button == 0)
						{
							ResetEditMode();
							Cancel();
						}
						break;
					}
					case EventType.MouseMove:
					{
						UpdateMouseCursor();
						visualSnappedLines = null;
						if (GUIUtility.hotControl == 0)
						{
							RestoreTargetData(hoverMode: true);
							UpdateMouseIntersection(sceneView);
							UpdateClipPlane();
						} else
							UpdateMouseIntersection(sceneView);
						break;
					}
					case EventType.ValidateCommand:
					case EventType.KeyDown:
					{
						if (GUIUtility.hotControl == 0)
						{
							if (Keys.CancelActionKey.IsKeyPressed()) { Event.current.Use(); break; }
							if (Keys.PerformActionKey.IsKeyPressed()) { Event.current.Use(); break; }
							if (Keys.CycleClipModes.IsKeyPressed()) { Event.current.Use(); break; }
							if (Keys.HandleSceneKeyDown(EditModeManager.CurrentTool, false)) { Event.current.Use(); break; }
						}
						break;
					}
					case EventType.KeyUp:
					{
						if (GUIUtility.hotControl == 0)
						{
							if (Keys.CancelActionKey.IsKeyPressed()) { SelectionUtility.DeselectAll(); Event.current.Use(); break; }
							if (Keys.PerformActionKey.IsKeyPressed()) { Commit(); Event.current.Use(); break; }
							if (Keys.CycleClipModes.IsKeyPressed()) { CycleClipMode(); Event.current.Use(); break; }
							if (Keys.HandleSceneKeyUp(EditModeManager.CurrentTool, false)) { Event.current.Use(); break; }
						}
						break;
					}
					case EventType.Repaint:
					{
						if (SceneDragToolManager.IsDraggingObjectInScene)
							break;

						OnPaint(sceneView);

						Color temp = Handles.color;
						for (int i = 0; i < pointsShown; i++)
						{
							bool isSelected = (ids[i] == GUIUtility.keyboardControl) || (ids[i] == GUIUtility.hotControl);

							Handles.color = isSelected ? Handles.selectedColor : Color.gray;
							PaintUtility.SquareDotCap(ids[i], points[i], camera.transform.rotation, sizes[i]);
						}
						Handles.color = temp;

						if ((camera == null || !assume2DView) &&
							pointsShown == 2 && GUIUtility.hotControl == 0)
						{
							Vector3 intersectionPoint = currentMousePoint.Value;
							if (camera != null && assume2DView)
								intersectionPoint = new CSGPlane(camera.transform.forward, points[0]).Project(intersectionPoint);
							var tempMatrix = Handles.matrix;
							Handles.matrix = MathConstants.identityMatrix;
							float handleSize	= CSG_HandleUtility.GetHandleSize(intersectionPoint);
							Handles.color = ColorSettings.PointInnerStateColor[3];
							PaintUtility.SquareDotCap(-1, intersectionPoint, MathConstants.identityQuaternion, handleSize * GUIConstants.handleScale);
							Handles.matrix = tempMatrix;
						}

						if (visualSnappedLines != null)
							PaintUtility.DrawLines(visualSnappedLines.ToArray(), GUIConstants.oldThinLineScale * 2.0f, ColorSettings.SnappedEdges);
						break;
					}
				}
				for (int i = 0; i < pointsShown; i++)
				{
					var type = Event.current.GetTypeForControl(ids[i]);
					switch (type)
					{
						case EventType.MouseUp:
						{
							if (GUIUtility.hotControl == ids[i] && Event.current.button == 0)
							{
								GUIUtility.hotControl = 0;
								GUIUtility.keyboardControl = 0;
								EditorGUIUtility.editingTextField = false;
								Event.current.Use();
								EditorGUIUtility.SetWantsMouseJumping(0);

								visualSnappedLines = null;
							}
							break;
						}

						case EventType.MouseDrag:
						{
							if (GUIUtility.hotControl == ids[i])
							{
								var newPoint = HandleMovement(sceneView, startMousePosition, Event.current.mousePosition);
								if (camera != null && assume2DView)
									newPoint = new CSGPlane(camera.transform.forward, points[i]).Project(newPoint);
								var delta = newPoint - points[i];
								if (pointsShown != 3 && i == 1)
								{
									points[2] += delta;
								}

								points[i] += delta;

								GUI.changed = true;
								Event.current.Use();
								UpdateClipPlane();
							}
							break;
						}
					}
				}
			}
			finally
			{				
				Handles.matrix = prevMatrix;
			}
		}
		
		static Vector2 scrollPos;
		public void OnInspectorGUI(EditorWindow window, float height)
		{
			scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
			{
				EditModeClipGUI.OnInspectorGUI(window, height);
			}
			EditorGUILayout.EndScrollView();
		}
		
		public Rect GetLastSceneGUIRect()
		{
			return EditModeClipGUI.GetLastSceneGUIRect(this);
		}
		
		public bool OnSceneGUI(Rect windowRect)
		{
			//if (clipBrushes == null ||
			//	clipBrushes.Length == 0)
				//return false;

			EditModeClipGUI.OnSceneGUI(windowRect, this);
			return true;
		}
	}
}
