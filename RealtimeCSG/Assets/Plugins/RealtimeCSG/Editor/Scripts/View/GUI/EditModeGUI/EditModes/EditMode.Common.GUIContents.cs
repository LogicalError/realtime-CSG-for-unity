using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using InternalRealtimeCSG;
using RealtimeCSG.Helpers;
using RealtimeCSG;

namespace InternalRealtimeCSG
{
	internal sealed partial class EditModeCommonGUI
	{
		private static GUIContent UpdateLightmapUVContent	= new GUIContent("Update lightmap UVs");
//		private static GUIContent UpdateCollidersContent	= new GUIContent("Update colliders");

		private static readonly GUIContent	ContentShadows					= new GUIContent("Shadows");
		private static readonly GUIContent	ContentCastShadowsSurfaces		= new GUIContent("Cast");
		private static readonly GUIContent	ContentReceiveShadowsSurfaces	= new GUIContent("Receive");

		private static readonly GUIContent	ContentVisibleSurfaces			= new GUIContent("Visible");
		private static readonly GUIContent	ContentCollisionSurfaces		= new GUIContent("Collision");
		
		private static readonly ToolTip		ToolTipCastShadowsSurfaces		= new ToolTip("Cast Shadows", "Toggle shadow casting for all selected surfaces. When cast is not toggled, these surfaces will not cast a shadow.");
		private static readonly ToolTip		ToolTipReceiveShadowsSurfaces	= new ToolTip("Receive Shadows", "Toggle shadow receiving for all selected surfaces. When receive is not toggled, these surfaces will not receive shadows (when visible). Note: only works with forward rendering");
		private static readonly ToolTip		ToolTipVisibleSurfaces			= new ToolTip("Visible", "Toggle visibility for all selected surfaces. When visible is not toggled, it won't be part of the rendered mesh.");
		private static readonly ToolTip		ToolTipCollisionSurfaces		= new ToolTip("Collision", "Toggle collision on/off for all selected surfaces. When collision is not toggled, it won't be part of the collision mesh.");

	}
}
