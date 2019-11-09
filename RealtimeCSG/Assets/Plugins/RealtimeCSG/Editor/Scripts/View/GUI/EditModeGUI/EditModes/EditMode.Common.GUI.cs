using System;
using UnityEditor;
using UnityEngine;
using RealtimeCSG;
using RealtimeCSG.Legacy;
using RealtimeCSG.Components;

namespace InternalRealtimeCSG
{
	internal sealed partial class EditModeCommonGUI
	{
		public static bool IndentableButton(GUIContent content, ref Rect rect, GUIStyle style = null)
		{
			var indentRect = EditorGUI.IndentedRect(rect);
			rect.y += 20;
			return style == null ?
				GUI.Button(indentRect, content) :
				GUI.Button(indentRect, content, style);
		}

		public static bool IndentableButton(GUIContent content, GUIStyle style = null)
		{
			return style == null ?
				GUI.Button(EditorGUI.IndentedRect(EditorGUILayout.GetControlRect()), content) :
				GUI.Button(EditorGUI.IndentedRect(EditorGUILayout.GetControlRect()), content, style);
		}

		public static void UpdateLightmapUVButton(ref Rect rect)
		{
			CSG_GUIStyleUtility.InitStyles();
			if (IndentableButton(UpdateLightmapUVContent, ref rect, CSG_GUIStyleUtility.redButton))
			{
				CSGModelManager.BuildLightmapUvs();
			}
		}

		public static void UpdateLightmapUVButton()
		{
			CSG_GUIStyleUtility.InitStyles();
			if (IndentableButton(UpdateLightmapUVContent, CSG_GUIStyleUtility.redButton))
			{
				CSGModelManager.BuildLightmapUvs();
			}
		}

		public static bool NeedLightmapUVUpdate(CSGModel[] models)
		{
			bool needLightmapUVUpdate = false;
//			bool needColliderUpdate   = false;
			
			for (int m = 0; m < models.Length; m++)
			{
				if (!models[m])
					continue;

				needLightmapUVUpdate = needLightmapUVUpdate || (MeshInstanceManager.NeedToGenerateLightmapUVsForModel(models[m]) && !models[m].AutoRebuildUVs);
//				needColliderUpdate   = needColliderUpdate   || (MeshInstanceManager.NeedToGenerateCollidersForModel(models[m])   && !models[m].AutoRebuildColliders);
			}

			return needLightmapUVUpdate;
		}

		public static bool UpdateButtons(CSGModel[] models)
		{
			if (NeedLightmapUVUpdate(models))
			{
				UpdateLightmapUVButton();
				return true;
			}
			return false;
		}

		public static bool UpdateButtons(CSGModel[] models, ref Rect rect)
		{
			if (NeedLightmapUVUpdate(models))
			{
				UpdateLightmapUVButton(ref rect);
				return true;	
			}
			return false;
		}

		public class SurfaceFlagState
		{
			public bool? noRender          = false;
			public bool? noCollision       = false;
			public bool? noCastShadows     = false;
			public bool? noReceiveShadows  = false;

			public void Init(SelectedBrushSurface[] selectedBrushSurfaces)
			{
				if (selectedBrushSurfaces.Length > 0)
				{
					for (var i = 0; i < selectedBrushSurfaces.Length; i++)
					{
						var brush			= selectedBrushSurfaces[i].brush;
						var surfaceIndex	= selectedBrushSurfaces[i].surfaceIndex;
						if (surfaceIndex >= brush.Shape.Surfaces.Length)
						{
							Debug.LogWarning("surface_index >= brush.Shape.Surfaces.Length");
							continue; 
						}
						var texGenIndex	= brush.Shape.Surfaces[surfaceIndex].TexGenIndex;
						if (texGenIndex >= brush.Shape.TexGens.Length)
						{
							Debug.LogWarning("texGen_index >= brush.Shape.TexGens.Length");
							continue;
						}

						var texGenFlags				= brush.Shape.TexGenFlags[texGenIndex];
						var surfaceNoRender			= ((texGenFlags & TexGenFlags.NoRender)         == TexGenFlags.NoRender);
						var surfaceNoCollision		= ((texGenFlags & TexGenFlags.NoCollision)      == TexGenFlags.NoCollision);
						var surfaceNoCastShadows	= ((texGenFlags & TexGenFlags.NoCastShadows)    == TexGenFlags.NoCastShadows);
						var surfaceNoReceiveShadows	= ((texGenFlags & TexGenFlags.NoReceiveShadows) == TexGenFlags.NoReceiveShadows);

						if (i == 0)
						{
							noRender			= surfaceNoRender;
							noCollision			= surfaceNoCollision;
							noCastShadows		= surfaceNoCastShadows;
							noReceiveShadows	= surfaceNoReceiveShadows;
						} else
						{
							if (noRender		.HasValue && noRender		 .Value != surfaceNoRender		  ) noRender		 = surfaceNoRender;
							if (noCollision		.HasValue && noCollision	 .Value != surfaceNoCollision	  ) noCollision		 = surfaceNoCollision;
							if (noCastShadows   .HasValue && noCastShadows   .Value != surfaceNoCastShadows   ) noCastShadows    = surfaceNoCastShadows;
							if (noReceiveShadows.HasValue && noReceiveShadows.Value != surfaceNoReceiveShadows) noReceiveShadows = surfaceNoReceiveShadows;
						}
					}
				}
			}
		}

		public static void OnSurfaceFlagButtons(SurfaceFlagState state, SelectedBrushSurface[] selectedBrushSurfaces, bool isSceneGUI = false)
		{
			var leftStyle		= isSceneGUI ? EditorStyles.miniButtonLeft  : GUI.skin.button;
			//var middleStyle	= isSceneGUI ? EditorStyles.miniButtonMid   : GUI.skin.button;
			var rightStyle		= isSceneGUI ? EditorStyles.miniButtonRight : GUI.skin.button;
			
			GUILayout.BeginHorizontal(CSG_GUIStyleUtility.ContentEmpty);
			{
				if (!isSceneGUI)
					GUILayout.Label(ContentShadows, EditModeSurfaceGUI.largeLabelWidth);
				else
					GUILayout.Label(ContentShadows, EditorStyles.miniLabel, EditModeSurfaceGUI.smallLabelWidth);

				EditorGUI.BeginChangeCheck();
				{
					// TODO: implement support
					EditorGUI.showMixedValue = !state.noReceiveShadows.HasValue;
					state.noReceiveShadows = !GUILayout.Toggle(!(state.noReceiveShadows ?? (state.noRender ?? true)), ContentReceiveShadowsSurfaces, leftStyle);
					TooltipUtility.SetToolTip(ToolTipReceiveShadowsSurfaces);
				}
				if (EditorGUI.EndChangeCheck())
					SurfaceUtility.SetSurfaceTexGenFlags(selectedBrushSurfaces, TexGenFlags.NoReceiveShadows, state.noReceiveShadows.Value);
				EditorGUI.BeginChangeCheck();
				{
					// TODO: implement support
					EditorGUI.showMixedValue = !state.noCastShadows.HasValue;
					state.noCastShadows = !GUILayout.Toggle(!(state.noCastShadows ?? true), ContentCastShadowsSurfaces, rightStyle);
					TooltipUtility.SetToolTip(ToolTipCastShadowsSurfaces);
				}
				if (EditorGUI.EndChangeCheck())
					SurfaceUtility.SetSurfaceTexGenFlags(selectedBrushSurfaces, TexGenFlags.NoCastShadows, state.noCastShadows.Value);
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal(CSG_GUIStyleUtility.ContentEmpty);
			{
				EditorGUI.BeginChangeCheck();
				{
					EditorGUI.showMixedValue = !state.noRender.HasValue;
					state.noRender = !GUILayout.Toggle(!(state.noRender ?? true), ContentVisibleSurfaces, leftStyle);
					TooltipUtility.SetToolTip(ToolTipVisibleSurfaces);
				}
				if (EditorGUI.EndChangeCheck())
					SurfaceUtility.SetSurfaceTexGenFlags(selectedBrushSurfaces, TexGenFlags.NoRender, state.noRender.Value);
				EditorGUI.BeginChangeCheck();
				{
					EditorGUI.showMixedValue = !state.noCollision.HasValue;
					state.noCollision = !GUILayout.Toggle(!(state.noCollision ?? true), ContentCollisionSurfaces, rightStyle);
					TooltipUtility.SetToolTip(ToolTipCollisionSurfaces);
				}
				if (EditorGUI.EndChangeCheck())
					SurfaceUtility.SetSurfaceTexGenFlags(selectedBrushSurfaces, TexGenFlags.NoCollision, state.noCollision.Value);
			}
			GUILayout.EndHorizontal();
			EditorGUI.showMixedValue = false;
		}
		
		public static void OnSurfaceFlagButtons(Rect rect, SurfaceFlagState state, SelectedBrushSurface[] selectedBrushSurfaces)
		{
			var leftStyle		= EditorStyles.miniButtonLeft;
			var rightStyle		= EditorStyles.miniButtonRight;

			var tempRect = rect;

			{
				tempRect.Set(rect.x + 4, rect.y + 1, 45, 16);
				GUI.Label(tempRect, ContentShadows, EditorStyles.miniLabel);
		
				EditorGUI.BeginChangeCheck();
				{
					var mixed = !state.noReceiveShadows.HasValue;
					var enabled = !(state.noReceiveShadows ?? (state.noRender ?? true));
					EditorGUI.showMixedValue = mixed;
					tempRect.Set(rect.x + 53, rect.y + 1, 90 - 4, 15);
					state.noReceiveShadows = !GUI.Toggle(tempRect, enabled, ContentReceiveShadowsSurfaces, leftStyle);
					TooltipUtility.SetToolTip(ToolTipReceiveShadowsSurfaces, tempRect);
				}
				if (EditorGUI.EndChangeCheck())
					SurfaceUtility.SetSurfaceTexGenFlags(selectedBrushSurfaces, TexGenFlags.NoReceiveShadows, state.noReceiveShadows.Value);
				EditorGUI.BeginChangeCheck();
				{
					var mixed = !state.noCastShadows.HasValue;
					var enabled = !(state.noCastShadows ?? true);
					EditorGUI.showMixedValue = mixed;
					tempRect.Set(rect.x + 143 - 4, rect.y + 1, 74 - 4, 15);
					state.noCastShadows = !GUI.Toggle(tempRect, enabled, ContentCastShadowsSurfaces, rightStyle);
					TooltipUtility.SetToolTip(ToolTipCastShadowsSurfaces, tempRect);
				}
				if (EditorGUI.EndChangeCheck())
					SurfaceUtility.SetSurfaceTexGenFlags(selectedBrushSurfaces, TexGenFlags.NoCastShadows, state.noCastShadows.Value);
			}
			{
				EditorGUI.BeginChangeCheck();
				{
					var mixed = !state.noRender.HasValue;
					var enabled = !(state.noRender ?? true);
					EditorGUI.showMixedValue = mixed;
					tempRect.Set(rect.x + 4, rect.y + 18, 94, 15);
					state.noRender = !GUI.Toggle(tempRect, enabled, ContentVisibleSurfaces, leftStyle);
					TooltipUtility.SetToolTip(ToolTipVisibleSurfaces, tempRect);
				}
				if (EditorGUI.EndChangeCheck())
					SurfaceUtility.SetSurfaceTexGenFlags(selectedBrushSurfaces, TexGenFlags.NoRender, state.noRender.Value);
				EditorGUI.BeginChangeCheck();
				{
					var mixed = !state.noCollision.HasValue;
					var enabled = !(state.noCollision ?? true);
					EditorGUI.showMixedValue = mixed;
					tempRect.Set(rect.x + 98, rect.y + 18, 112, 15);
					state.noCollision = !GUI.Toggle(tempRect, enabled, ContentCollisionSurfaces, rightStyle);
					TooltipUtility.SetToolTip(ToolTipCollisionSurfaces, tempRect);
				}
				if (EditorGUI.EndChangeCheck())
					SurfaceUtility.SetSurfaceTexGenFlags(selectedBrushSurfaces, TexGenFlags.NoCollision, state.noCollision.Value);
			}
			EditorGUI.showMixedValue = false;
		}


		public static TexGenFlags OnSurfaceFlagButtons(TexGenFlags texGenFlags, bool isSceneGUI = false)
		{
			var leftStyle	= EditorStyles.miniButtonLeft;
			//var middleStyle = EditorStyles.miniButtonMid;
			var rightStyle	= EditorStyles.miniButtonRight;

			var	noRender			= (texGenFlags & TexGenFlags.NoRender) == TexGenFlags.NoRender;
			var noCollision			= (texGenFlags & TexGenFlags.NoCollision) == TexGenFlags.NoCollision;
			var noCastShadows		= (texGenFlags & TexGenFlags.NoCastShadows) == TexGenFlags.NoCastShadows;
			var noReceiveShadows	= noRender || (texGenFlags & TexGenFlags.NoReceiveShadows) == TexGenFlags.NoReceiveShadows;

			GUILayout.BeginVertical(CSG_GUIStyleUtility.ContentEmpty);
			{
				GUILayout.BeginHorizontal(CSG_GUIStyleUtility.ContentEmpty);
				{
					GUILayout.Label(ContentShadows, EditorStyles.miniLabel, EditModeSurfaceGUI.smallLabelWidth);
					EditorGUI.BeginChangeCheck();
					{
						// TODO: implement support
						noReceiveShadows = !GUILayout.Toggle(!noReceiveShadows, ContentReceiveShadowsSurfaces, leftStyle);
						TooltipUtility.SetToolTip(ToolTipReceiveShadowsSurfaces);
					}
					if (EditorGUI.EndChangeCheck())
					{
						if (noReceiveShadows) texGenFlags |=  TexGenFlags.NoReceiveShadows; 
						else				  texGenFlags &= ~TexGenFlags.NoReceiveShadows;
						GUI.changed = true;
					}
					EditorGUI.BeginChangeCheck();
					{
						// TODO: implement support
						noCastShadows = !GUILayout.Toggle(!noCastShadows, ContentCastShadowsSurfaces, rightStyle);
						TooltipUtility.SetToolTip(ToolTipCastShadowsSurfaces);
					}
					if (EditorGUI.EndChangeCheck())
					{
						if (noCastShadows) texGenFlags |=  TexGenFlags.NoCastShadows; 
						else			   texGenFlags &= ~TexGenFlags.NoCastShadows;
						GUI.changed = true;
					}
				}
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal(CSG_GUIStyleUtility.ContentEmpty);
				{
					EditorGUI.BeginChangeCheck();
					{
						noRender = !GUILayout.Toggle(!noRender, ContentVisibleSurfaces, leftStyle);
						TooltipUtility.SetToolTip(ToolTipVisibleSurfaces);
					}
					if (EditorGUI.EndChangeCheck())
					{
						if (noRender) texGenFlags |=  TexGenFlags.NoRender; 
						else		  texGenFlags &= ~TexGenFlags.NoRender;
						GUI.changed = true;
					}
					EditorGUI.BeginChangeCheck();
					{
						noCollision = !GUILayout.Toggle(!noCollision, ContentCollisionSurfaces, rightStyle);
						TooltipUtility.SetToolTip(ToolTipCollisionSurfaces);
					}
					if (EditorGUI.EndChangeCheck())
					{
						if (noCollision) texGenFlags |=  TexGenFlags.NoCollision; 
						else		     texGenFlags &= ~TexGenFlags.NoCollision;
						GUI.changed = true;
					}
				}
				GUILayout.EndHorizontal();
			}
			GUILayout.EndVertical();
			return texGenFlags;
		}


		public static void StartToolGUI()
		{
			if (UpdateButtons(InternalCSGModelManager.Models))
				GUILayout.Space(10);
		}


		public static Rect StartToolGUI(Rect rect)
		{
			if (UpdateButtons(InternalCSGModelManager.Models, ref rect))
			{
				rect.y += 10;
			}
			return rect;
		}
	}
}
