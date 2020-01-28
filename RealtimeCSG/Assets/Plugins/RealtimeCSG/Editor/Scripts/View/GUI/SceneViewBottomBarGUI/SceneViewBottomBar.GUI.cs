using System.Globalization;
using UnityEditor;
using UnityEngine;
using InternalRealtimeCSG;
using System;
using System.Linq;
using System.Collections.Generic;

namespace RealtimeCSG
{
	internal sealed partial class SceneViewBottomBarGUI
	{
		static int BottomBarEditorOverlayHash	= "BottomBarEditorOverlay".GetHashCode();
		static int BottomBarGUIHash				= "BottomBarGUI".GetHashCode();

		public static void ShowGUI(SceneView sceneView, bool haveOffset = true)
		{
			InitStyles();
			CSG_GUIStyleUtility.InitStyles();
			if (sceneView != null)
			{
				float height	= sceneView.position.height;//Screen.height;
				float width		= sceneView.position.width;//Screen.width;
				Rect bottomBarRect;
				if (haveOffset)
				{
					bottomBarRect = new Rect(0, height - (CSG_GUIStyleUtility.BottomToolBarHeight + 18), 
											  width, CSG_GUIStyleUtility.BottomToolBarHeight);
				} else
					bottomBarRect = new Rect(0, height - (CSG_GUIStyleUtility.BottomToolBarHeight + 1), width, CSG_GUIStyleUtility.BottomToolBarHeight);

				try
				{ 
					Handles.BeginGUI();
					
					bool prevGUIChanged = GUI.changed;
					if (Event.current.type == EventType.Repaint)
						CSG_GUIStyleUtility.BottomToolBarStyle.Draw(bottomBarRect, false, false, false, false);
					OnBottomBarGUI(sceneView, bottomBarRect);
					GUI.changed = prevGUIChanged || GUI.changed;
					
					int controlID = GUIUtility.GetControlID(BottomBarGUIHash, FocusType.Keyboard, bottomBarRect);
					var type = Event.current.GetTypeForControl(controlID);
					switch (type)
					{
						case EventType.MouseDown: { if (bottomBarRect.Contains(Event.current.mousePosition)) { GUIUtility.hotControl = controlID; GUIUtility.keyboardControl = controlID; Event.current.Use(); } break; }
						case EventType.MouseMove: { if (bottomBarRect.Contains(Event.current.mousePosition)) { Event.current.Use(); } break; }
						case EventType.MouseUp:   { if (GUIUtility.hotControl == controlID) { GUIUtility.hotControl = 0; GUIUtility.keyboardControl = 0; Event.current.Use(); } break; }
						case EventType.MouseDrag: { if (GUIUtility.hotControl == controlID) { Event.current.Use(); } break; }
						case EventType.ScrollWheel: { if (bottomBarRect.Contains(Event.current.mousePosition)) { Event.current.Use(); } break; }
					}

					//TooltipUtility.HandleAreaOffset(new Vector2(-bottomBarRect.xMin, -bottomBarRect.yMin));
				}
				finally
				{
					Handles.EndGUI();
				}
			}
		}

		
		static Rect currentRect = new Rect();
		static void OnBottomBarGUI(SceneView sceneView, Rect barSize)
		{
			//if (Event.current.type == EventType.Layout)
			//	return;

			var snapMode        = RealtimeCSG.CSGSettings.SnapMode;
			var uniformGrid		= RealtimeCSG.CSGSettings.UniformGrid;
			var moveSnapVector  = RealtimeCSG.CSGSettings.SnapVector;
			var rotationSnap	= RealtimeCSG.CSGSettings.SnapRotation;
			var scaleSnap		= RealtimeCSG.CSGSettings.SnapScale;
			var showGrid		= RealtimeCSG.CSGSettings.GridVisible;
			var lockAxisX		= RealtimeCSG.CSGSettings.LockAxisX;
			var lockAxisY		= RealtimeCSG.CSGSettings.LockAxisY;
			var lockAxisZ		= RealtimeCSG.CSGSettings.LockAxisZ;
			var distanceUnit	= RealtimeCSG.CSGSettings.DistanceUnit;
			var helperSurfaces  = RealtimeCSG.CSGSettings.VisibleHelperSurfaces;
			var showWireframe	= RealtimeCSG.CSGSettings.IsWireframeShown(sceneView);
			var skin			= CSG_GUIStyleUtility.Skin;
			var updateSurfaces	= false;
			bool wireframeModified = false;

			var viewWidth = sceneView.position.width;

			float layoutHeight = barSize.height;
			float layoutX = 6.0f;

			bool modified = false;
			GUI.changed = false;
			{
				currentRect.width	= 27;
				currentRect.y		= 0;
				currentRect.height	= layoutHeight - currentRect.y;
				currentRect.y		+= barSize.y;
				currentRect.x		= layoutX;
				layoutX += currentRect.width;

				#region "Grid" button
				if (showGrid)
				{
					showGrid = GUI.Toggle(currentRect, showGrid, skin.gridIconOn, EditorStyles.toolbarButton);
				} else
				{
					showGrid = GUI.Toggle(currentRect, showGrid, skin.gridIcon,   EditorStyles.toolbarButton);
				}
				//(x:6.00, y:0.00, width:27.00, height:18.00)
				TooltipUtility.SetToolTip(showGridTooltip, currentRect);
				#endregion

				if (viewWidth >= 800)
					layoutX += 6; //(x:33.00, y:0.00, width:6.00, height:6.00)
					
				var prevBackgroundColor = GUI.backgroundColor;
				var lockedBackgroundColor = skin.lockedBackgroundColor;
				if (lockAxisX)
					GUI.backgroundColor = lockedBackgroundColor;

				#region "X" lock button
				currentRect.width	= 17;
				currentRect.y		= 0;
				currentRect.height	= layoutHeight - currentRect.y;
				currentRect.y		+= barSize.y;
				currentRect.x		= layoutX;
				layoutX += currentRect.width;

				lockAxisX = !GUI.Toggle(currentRect, !lockAxisX, xLabel, skin.xToolbarButton);
				//(x:39.00, y:0.00, width:17.00, height:18.00)
				if (lockAxisX)
					TooltipUtility.SetToolTip(xTooltipOn, currentRect);
				else
					TooltipUtility.SetToolTip(xTooltipOff, currentRect);
				GUI.backgroundColor = prevBackgroundColor;
				#endregion
												
				#region "Y" lock button
				currentRect.x		= layoutX;
				layoutX += currentRect.width;

				if (lockAxisY)
					GUI.backgroundColor = lockedBackgroundColor;
				lockAxisY = !GUI.Toggle(currentRect, !lockAxisY, yLabel, skin.yToolbarButton);
				//(x:56.00, y:0.00, width:17.00, height:18.00)
				if (lockAxisY)
					TooltipUtility.SetToolTip(yTooltipOn, currentRect);
				else
					TooltipUtility.SetToolTip(yTooltipOff, currentRect);
				GUI.backgroundColor = prevBackgroundColor;
				#endregion
						
				#region "Z" lock button
				currentRect.x		= layoutX;
				layoutX += currentRect.width;

				if (lockAxisZ)
					GUI.backgroundColor = lockedBackgroundColor;
				lockAxisZ = !GUI.Toggle(currentRect, !lockAxisZ, zLabel, skin.zToolbarButton);
				//(x:56.00, y:0.00, width:17.00, height:18.00)
				if (lockAxisZ)
					TooltipUtility.SetToolTip(zTooltipOn, currentRect);
				else
					TooltipUtility.SetToolTip(zTooltipOff, currentRect);
				GUI.backgroundColor = prevBackgroundColor;
				#endregion
			}
			modified = GUI.changed || modified;

			if (viewWidth >= 800)
				layoutX += 6; // (x:91.00, y:0.00, width:6.00, height:6.00)
				
			#region "SnapMode" button
			GUI.changed = false;
			{
				currentRect.width	= 27;
				currentRect.y		= 0;
				currentRect.height	= layoutHeight - currentRect.y;
				currentRect.y		+= barSize.y;
				currentRect.x		= layoutX;
				layoutX += currentRect.width;


                switch (snapMode)
                {
                    case SnapMode.GridSnapping:
                    {
                        var newValue = GUI.Toggle(currentRect, snapMode == SnapMode.GridSnapping, CSG_GUIStyleUtility.Skin.gridSnapIconOn, EditorStyles.toolbarButton);
                        if (GUI.changed)
                        {
                            snapMode = newValue ? SnapMode.GridSnapping : SnapMode.RelativeSnapping;
                        }
                        //(x:97.00, y:0.00, width:27.00, height:18.00)
                        TooltipUtility.SetToolTip(gridSnapModeTooltip, currentRect);
                        break;
                    }
                    case SnapMode.RelativeSnapping:
                    {
                        var newValue = GUI.Toggle(currentRect, snapMode == SnapMode.RelativeSnapping, CSG_GUIStyleUtility.Skin.relSnapIconOn, EditorStyles.toolbarButton);
                        if (GUI.changed)
                        {
                            snapMode = newValue ? SnapMode.RelativeSnapping : SnapMode.None;
                        }
                        //(x:97.00, y:0.00, width:27.00, height:18.00)
                        TooltipUtility.SetToolTip(relativeSnapModeTooltip, currentRect);
                        break;
                    }
                    default:
                    case SnapMode.None:
                    {
                        var newValue = GUI.Toggle(currentRect, snapMode != SnapMode.None, CSG_GUIStyleUtility.Skin.noSnapIconOn, EditorStyles.toolbarButton);
                        if (GUI.changed)
                        {
                            snapMode = newValue ? SnapMode.GridSnapping : SnapMode.None;
                        }
                        //(x:97.00, y:0.00, width:27.00, height:18.00)
                        TooltipUtility.SetToolTip(noSnappingModeTooltip, currentRect);
                        break;
                    }
                }
            }
            modified = GUI.changed || modified;
            #endregion
				
			if (viewWidth >= 460)
			{
				if (snapMode != SnapMode.None)
				{
					#region "Position" label
					if (viewWidth >= 500)
					{ 
						if (viewWidth >= 865)
						{
							currentRect.width	= 44;
							currentRect.y		= 1;
							currentRect.height	= layoutHeight - currentRect.y;
							currentRect.y		+= barSize.y;
							currentRect.x		= layoutX;
							layoutX += currentRect.width;

							uniformGrid = GUI.Toggle(currentRect, uniformGrid, positionLargeLabel, miniTextStyle);
							//(x:128.00, y:2.00, width:44.00, height:16.00)

							TooltipUtility.SetToolTip(positionTooltip, currentRect);
						} else
						{
							currentRect.width	= 22;
							currentRect.y		= 1;
							currentRect.height	= layoutHeight - currentRect.y;
							currentRect.y		+= barSize.y;
							currentRect.x		= layoutX;
							layoutX += currentRect.width;

							uniformGrid = GUI.Toggle(currentRect, uniformGrid, positionSmallLabel, miniTextStyle);
							//(x:127.00, y:2.00, width:22.00, height:16.00)

							TooltipUtility.SetToolTip(positionTooltip, currentRect);
						}
					}
					#endregion
							
					layoutX += 2;

					#region "Position" field
					if (uniformGrid || viewWidth < 515)
					{
						EditorGUI.showMixedValue = !(moveSnapVector.x == moveSnapVector.y && moveSnapVector.x == moveSnapVector.z);
						GUI.changed = false;
						{
							currentRect.width	= 70;
							currentRect.y		= 3;
							currentRect.height	= layoutHeight - (currentRect.y - 1);
							currentRect.y		+= barSize.y;
							currentRect.x		= layoutX;
							layoutX += currentRect.width;
							
							moveSnapVector.x = Units.DistanceUnitToUnity(distanceUnit, EditorGUI.DoubleField(currentRect, Units.UnityToDistanceUnit(distanceUnit, moveSnapVector.x), textInputStyle));//, MinSnapWidth, MaxSnapWidth));
							//(x:176.00, y:3.00, width:70.00, height:16.00)
						}
						if (GUI.changed)
						{
							modified = true;
							moveSnapVector.y = moveSnapVector.x;
							moveSnapVector.z = moveSnapVector.x;
						}
						EditorGUI.showMixedValue = false;
					} else
					{
						GUI.changed = false;
						{
							currentRect.width	= 70;
							currentRect.y		= 3;
							currentRect.height	= layoutHeight - (currentRect.y - 1);
							currentRect.y		+= barSize.y;
							currentRect.x		= layoutX;
							layoutX += currentRect.width;
							layoutX ++;

							moveSnapVector.x = Units.DistanceUnitToUnity(distanceUnit, EditorGUI.DoubleField(currentRect, Units.UnityToDistanceUnit(distanceUnit, moveSnapVector.x), textInputStyle));//, MinSnapWidth, MaxSnapWidth));
							//(x:175.00, y:3.00, width:70.00, height:16.00)
								

							currentRect.x		= layoutX;
							layoutX += currentRect.width;
							layoutX ++;

							moveSnapVector.y = Units.DistanceUnitToUnity(distanceUnit, EditorGUI.DoubleField(currentRect, Units.UnityToDistanceUnit(distanceUnit, moveSnapVector.y), textInputStyle));//, MinSnapWidth, MaxSnapWidth));
							//(x:247.00, y:3.00, width:70.00, height:16.00)
								

							currentRect.x		= layoutX;
							layoutX += currentRect.width;

							moveSnapVector.z = Units.DistanceUnitToUnity(distanceUnit, EditorGUI.DoubleField(currentRect, Units.UnityToDistanceUnit(distanceUnit, moveSnapVector.z), textInputStyle));//, MinSnapWidth, MaxSnapWidth));
							//(x:319.00, y:3.00, width:70.00, height:16.00)
						}
						modified = GUI.changed || modified;
					}
					#endregion

					layoutX++;

					#region "Position" Unit
					DistanceUnit nextUnit = Units.CycleToNextUnit(distanceUnit);
					GUIContent   unitText = Units.GetUnitGUIContent(distanceUnit);
						
					currentRect.width	= 22;
					currentRect.y		= 2;
					currentRect.height	= layoutHeight - currentRect.y;
					currentRect.y		+= barSize.y;
					currentRect.x		= layoutX;
					layoutX += currentRect.width;

					if (GUI.Button(currentRect, unitText, miniTextStyle))//(x:393.00, y:2.00, width:13.00, height:16.00)
					{
						distanceUnit = nextUnit;
						modified = true;
					}
					#endregion

					layoutX += 2;

					#region "Position" +/-
					if (viewWidth >= 700)
					{
						currentRect.width	= 19;
						currentRect.y		= 2;
						currentRect.height	= layoutHeight - (currentRect.y + 1);
						currentRect.y		+= barSize.y;
						currentRect.x		= layoutX;
						layoutX += currentRect.width;

						if (GUI.Button(currentRect, positionPlusLabel,  EditorStyles.miniButtonLeft))  { GridUtility.DoubleGridSize(); moveSnapVector = RealtimeCSG.CSGSettings.SnapVector; }
						//(x:410.00, y:2.00, width:19.00, height:15.00)
						TooltipUtility.SetToolTip(positionPlusTooltip, currentRect);

						currentRect.width	= 17;
						currentRect.y		= 2;
						currentRect.height	= layoutHeight - (currentRect.y + 1);
						currentRect.y		+= barSize.y;
						currentRect.x		= layoutX;
						layoutX += currentRect.width;

						if (GUI.Button(currentRect, positionMinusLabel, EditorStyles.miniButtonRight)) { GridUtility.HalfGridSize(); moveSnapVector = RealtimeCSG.CSGSettings.SnapVector; }
						//(x:429.00, y:2.00, width:17.00, height:15.00)
						TooltipUtility.SetToolTip(positionMinnusTooltip, currentRect);
					}
					#endregion

					layoutX += 2;

					#region "Angle" label
					if (viewWidth >= 750)
					{
						if (viewWidth >= 865)
						{
							currentRect.width	= 31;
							currentRect.y		= 1;
							currentRect.height	= layoutHeight - currentRect.y;
							currentRect.y		+= barSize.y;
							currentRect.x		= layoutX;
							layoutX += currentRect.width;

							GUI.Label(currentRect, angleLargeLabel, miniTextStyle);
							//(x:450.00, y:2.00, width:31.00, height:16.00)
						} else
						{
							currentRect.width	= 22;
							currentRect.y		= 1;
							currentRect.height	= layoutHeight - currentRect.y;
							currentRect.y		+= barSize.y;
							currentRect.x		= layoutX;
							layoutX += currentRect.width;

							GUI.Label(currentRect, angleSmallLabel, miniTextStyle);
							//(x:355.00, y:2.00, width:22.00, height:16.00)
						}
						TooltipUtility.SetToolTip(angleTooltip, currentRect);
					}
					#endregion
						
					layoutX += 2;

					#region "Angle" field
					GUI.changed = false;
					{
						currentRect.width	= 70;
						currentRect.y		= 3;
						currentRect.height	= layoutHeight - (currentRect.y - 1);
						currentRect.y		+= barSize.y;
						currentRect.x		= layoutX;
						layoutX += currentRect.width;

						rotationSnap = EditorGUI.FloatField(currentRect, rotationSnap, textInputStyle);//, MinSnapWidth, MaxSnapWidth);
						//(x:486.00, y:3.00, width:70.00, height:16.00)
						if (viewWidth <= 750)
							TooltipUtility.SetToolTip(angleTooltip, currentRect);
					}
					modified = GUI.changed || modified;
					#endregion

					layoutX++;

					#region "Angle" Unit
					if (viewWidth >= 370)
					{
						currentRect.width	= 14;
						currentRect.y		= 1;
						currentRect.height	= layoutHeight - currentRect.y;
						currentRect.y		+= barSize.y;
						currentRect.x		= layoutX;
						layoutX += currentRect.width;
							
						GUI.Label(currentRect, angleUnitLabel, miniTextStyle);
					}
					#endregion
						
					layoutX += 2;

					#region "Angle" +/-
					if (viewWidth >= 700)
					{
						currentRect.width	= 19;
						currentRect.y		= 1;
						currentRect.height	= layoutHeight - (currentRect.y + 1);
						currentRect.y		+= barSize.y;
						currentRect.x		= layoutX;
						layoutX += currentRect.width;

						if (GUI.Button(currentRect, anglePlusLabel, EditorStyles.miniButtonLeft)) { rotationSnap *= 2.0f; modified = true; }
						//(x:573.00, y:2.00, width:19.00, height:15.00)
						TooltipUtility.SetToolTip(anglePlusTooltip, currentRect);
							

						currentRect.width	= 17;
						currentRect.y		= 1;
						currentRect.height	= layoutHeight - (currentRect.y + 1);
						currentRect.y		+= barSize.y;
						currentRect.x		= layoutX;
						layoutX += currentRect.width;

						if (GUI.Button(currentRect, angleMinusLabel, EditorStyles.miniButtonRight)) { rotationSnap /= 2.0f; modified = true; }
						//(x:592.00, y:2.00, width:17.00, height:15.00)
						TooltipUtility.SetToolTip(angleMinnusTooltip, currentRect);
					}
					#endregion

					layoutX += 2;

					#region "Scale" label
					if (viewWidth >= 750)
					{
						if (viewWidth >= 865)
						{
							currentRect.width	= 31;
							currentRect.y		= 1;
							currentRect.height	= layoutHeight - currentRect.y;
							currentRect.y		+= barSize.y;
							currentRect.x		= layoutX;
							layoutX += currentRect.width;

							GUI.Label(currentRect, scaleLargeLabel, miniTextStyle);
							//(x:613.00, y:2.00, width:31.00, height:16.00)
						} else
						{
							currentRect.width	= 19;
							currentRect.y		= 1;
							currentRect.height	= layoutHeight - currentRect.y;
							currentRect.y		+= barSize.y;
							currentRect.x		= layoutX;
							layoutX += currentRect.width;

							GUI.Label(currentRect, scaleSmallLabel, miniTextStyle);
							//(x:495.00, y:2.00, width:19.00, height:16.00)
						}
						TooltipUtility.SetToolTip(scaleTooltip, currentRect);
					}
					#endregion
						
					layoutX += 2;

					#region "Scale" field
					GUI.changed = false;
					{
						currentRect.width	= 70;
						currentRect.y		= 3;
						currentRect.height	= layoutHeight - (currentRect.y - 1); 
						currentRect.y		+= barSize.y;
						currentRect.x		= layoutX;
						layoutX += currentRect.width;

						scaleSnap = EditorGUI.FloatField(currentRect, scaleSnap, textInputStyle);//, MinSnapWidth, MaxSnapWidth);
						//(x:648.00, y:3.00, width:70.00, height:16.00)
						if (viewWidth <= 750)
							TooltipUtility.SetToolTip(scaleTooltip, currentRect);
					}
					modified = GUI.changed || modified;
					#endregion

					layoutX ++;
						
					#region "Scale" Unit
					if (viewWidth >= 370)
					{
						currentRect.width	= 15;
						currentRect.y		= 1;
						currentRect.height	= layoutHeight - currentRect.y; 
						currentRect.y		+= barSize.y;
						currentRect.x		= layoutX;
						layoutX += currentRect.width;

						GUI.Label(currentRect, scaleUnitLabel, miniTextStyle);
						//(x:722.00, y:2.00, width:15.00, height:16.00)
					}
					#endregion
						
					layoutX += 2;

					#region "Scale" +/-
					if (viewWidth >= 700)
					{
						currentRect.width	= 19;
						currentRect.y		= 2;
						currentRect.height	= layoutHeight - (currentRect.y + 1);
						currentRect.y		+= barSize.y;
						currentRect.x		= layoutX;
						layoutX += currentRect.width;

						if (GUI.Button(currentRect, scalePlusLabel, EditorStyles.miniButtonLeft)) { scaleSnap *= 10.0f; modified = true; }
						//(x:741.00, y:2.00, width:19.00, height:15.00)
						TooltipUtility.SetToolTip(scalePlusTooltip, currentRect);
							

						currentRect.width	= 17;
						currentRect.y		= 2;
						currentRect.height	= layoutHeight - (currentRect.y + 1);
						currentRect.y		+= barSize.y;
						currentRect.x		= layoutX;
						layoutX += currentRect.width;

						if (GUI.Button(currentRect, scaleMinusLabel, EditorStyles.miniButtonRight)) { scaleSnap /= 10.0f; modified = true; }
						//(x:760.00, y:2.00, width:17.00, height:15.00)
						TooltipUtility.SetToolTip(scaleMinnusTooltip, currentRect);
					}
					#endregion
				}
			}


			var prevLayoutX = layoutX;
				
			layoutX = viewWidth;

				
			#region "Rebuild"
			currentRect.width	= 27;
			currentRect.y		= 0;
			currentRect.height	= layoutHeight - currentRect.y; 
			currentRect.y		+= barSize.y;
			layoutX -= currentRect.width;
			currentRect.x		= layoutX;

			if (GUI.Button(currentRect, CSG_GUIStyleUtility.Skin.rebuildIcon, EditorStyles.toolbarButton))
			{
				Debug.Log("Starting complete rebuild");

				var text = new System.Text.StringBuilder();

				MaterialUtility.ResetMaterialTypeLookup();

				InternalCSGModelManager.skipCheckForChanges = true;
				RealtimeCSG.CSGSettings.Reload();
				UnityCompilerDefineManager.UpdateUnityDefines();

				InternalCSGModelManager.registerTime = 0.0;
				InternalCSGModelManager.validateTime = 0.0;
				InternalCSGModelManager.hierarchyValidateTime = 0.0;
				InternalCSGModelManager.updateHierarchyTime = 0.0;

				var startTime = EditorApplication.timeSinceStartup;
				InternalCSGModelManager.ForceRebuildAll();
				InternalCSGModelManager.OnHierarchyModified();
				var hierarchy_update_endTime = EditorApplication.timeSinceStartup;
				text.AppendFormat(CultureInfo.InvariantCulture, "Full hierarchy rebuild in {0:F} ms. ", (hierarchy_update_endTime - startTime) * 1000);


				NativeMethodBindings.RebuildAll();
				var csg_endTime = EditorApplication.timeSinceStartup;
				text.AppendFormat(CultureInfo.InvariantCulture, "Full CSG rebuild done in {0:F} ms. ", (csg_endTime - hierarchy_update_endTime) * 1000);
				 
				InternalCSGModelManager.RemoveForcedUpdates(); // we already did this in rebuild all
				InternalCSGModelManager.UpdateMeshes(text, forceUpdate: true);

				updateSurfaces = true;
				UpdateLoop.ResetUpdateRoutine();
				RealtimeCSG.CSGSettings.Save();
				InternalCSGModelManager.skipCheckForChanges = false;

				var scenes = new HashSet<UnityEngine.SceneManagement.Scene>();
				foreach(var model in InternalCSGModelManager.Models)
					scenes.Add(model.gameObject.scene);

				text.AppendFormat(CultureInfo.InvariantCulture, "{0} brushes. ", Foundation.CSGManager.TreeBrushCount);
				
				Debug.Log(text.ToString());
			}
			//(x:1442.00, y:0.00, width:27.00, height:18.00)
			TooltipUtility.SetToolTip(rebuildTooltip, currentRect);
			#endregion

			if (viewWidth >= 800)
				layoutX -= 6; //(x:1436.00, y:0.00, width:6.00, height:6.00)

			#region "Helper Surface Flags" Mask
			if (viewWidth >= 250)
			{
				GUI.changed = false;
				{
					prevLayoutX += 8;  // extra space
					prevLayoutX += 26; // width of "Show wireframe" button

					currentRect.width	= Mathf.Max(20, Mathf.Min(165, (viewWidth - prevLayoutX - currentRect.width)));

					currentRect.y		= 0;
					currentRect.height	= layoutHeight - currentRect.y; 
					currentRect.y		+= barSize.y;
					layoutX -= currentRect.width;
					currentRect.x		= layoutX;

					SurfaceVisibilityPopup.Button(sceneView, currentRect);
					
					//(x:1267.00, y:2.00, width:165.00, height:16.00)
					TooltipUtility.SetToolTip(helperSurfacesTooltip, currentRect);
				}
				if (GUI.changed)
				{
					updateSurfaces = true;
					modified = true;
				}
			}
			#endregion

			#region "Show wireframe" button
			GUI.changed = false;
			currentRect.width	= 26;
			currentRect.y		= 0;
			currentRect.height	= layoutHeight - currentRect.y; 
			currentRect.y		+= barSize.y;
			layoutX -= currentRect.width;
			currentRect.x		= layoutX;

			if (showWireframe)
			{
				showWireframe = GUI.Toggle(currentRect, showWireframe, CSG_GUIStyleUtility.Skin.wireframe, EditorStyles.toolbarButton);
				//(x:1237.00, y:0.00, width:26.00, height:18.00)
			} else
			{
				showWireframe = GUI.Toggle(currentRect, showWireframe, CSG_GUIStyleUtility.Skin.wireframeOn, EditorStyles.toolbarButton);
				//(x:1237.00, y:0.00, width:26.00, height:18.00)
			}
			TooltipUtility.SetToolTip(showWireframeTooltip, currentRect);
			if (GUI.changed)
			{
				wireframeModified = true;
				modified = true;
			}
			#endregion





			#region Capture mouse clicks in empty space
			var mousePoint  = Event.current.mousePosition;
			int controlID = GUIUtility.GetControlID(BottomBarEditorOverlayHash, FocusType.Passive, barSize);
			switch (Event.current.GetTypeForControl(controlID))
			{
				case EventType.MouseDown:	{ if (barSize.Contains(mousePoint)) { GUIUtility.hotControl = controlID; GUIUtility.keyboardControl = controlID; Event.current.Use(); } break; }
				case EventType.MouseMove:	{ if (barSize.Contains(mousePoint)) { Event.current.Use(); } break; }
				case EventType.MouseUp:		{ if (GUIUtility.hotControl == controlID) { GUIUtility.hotControl = 0; GUIUtility.keyboardControl = 0; Event.current.Use(); } break; }
				case EventType.MouseDrag:	{ if (GUIUtility.hotControl == controlID) { Event.current.Use(); } break; }
				case EventType.ScrollWheel: { if (barSize.Contains(mousePoint)) { Event.current.Use(); } break; }
			}
			#endregion



			#region Store modified values
			rotationSnap = Mathf.Max(1.0f, Mathf.Abs((360 + (rotationSnap % 360))) % 360);
			moveSnapVector.x = Mathf.Max(1.0f / 1024.0f, moveSnapVector.x);
			moveSnapVector.y = Mathf.Max(1.0f / 1024.0f, moveSnapVector.y);
			moveSnapVector.z = Mathf.Max(1.0f / 1024.0f, moveSnapVector.z);
			
			scaleSnap = Mathf.Max(MathConstants.MinimumScale, scaleSnap);
						
			RealtimeCSG.CSGSettings.SnapMode				= snapMode;
			RealtimeCSG.CSGSettings.SnapVector				= moveSnapVector;
			RealtimeCSG.CSGSettings.SnapRotation			= rotationSnap;
			RealtimeCSG.CSGSettings.SnapScale				= scaleSnap;
			RealtimeCSG.CSGSettings.UniformGrid				= uniformGrid;
//			RealtimeCSG.Settings.SnapVertex					= vertexSnap;
			RealtimeCSG.CSGSettings.GridVisible				= showGrid;
			RealtimeCSG.CSGSettings.LockAxisX				= lockAxisX;
			RealtimeCSG.CSGSettings.LockAxisY				= lockAxisY;
			RealtimeCSG.CSGSettings.LockAxisZ				= lockAxisZ;
			RealtimeCSG.CSGSettings.DistanceUnit			= distanceUnit;
			RealtimeCSG.CSGSettings.VisibleHelperSurfaces	= helperSurfaces;

			if (wireframeModified)
			{
				RealtimeCSG.CSGSettings.SetWireframeShown(sceneView, showWireframe);
			}

			if (updateSurfaces)
			{
				MeshInstanceManager.UpdateHelperSurfaceVisibility(force: true);
			}
			
			if (modified)
			{
				GUI.changed = true;
				RealtimeCSG.CSGSettings.UpdateSnapSettings();
				RealtimeCSG.CSGSettings.Save();
				CSG_EditorGUIUtility.RepaintAll();
			}
			#endregion
		}
		
	}
}
