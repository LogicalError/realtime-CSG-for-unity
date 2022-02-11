using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using InternalRealtimeCSG;

namespace RealtimeCSG
{
	internal sealed partial class EditModeSurfaceGUI
	{
		const int materialSize		= 100;
		const int windowWidth		= materialSize;
		const int justifyButtonSize = 22;
		const int labelWidthConst	= 56;
		const int buttonWidth		= 42;

		private static GUIContent		ContentSurfacesLabel;

		private static readonly GUIContent	ContentJustifyLabel		= new GUIContent("UV Justify");
		private static readonly GUIContent	ContentJustifyUpLeft	= new GUIContent("↖");
		private static readonly GUIContent	ContentJustifyUp		= new GUIContent("↑");
		private static readonly GUIContent	ContentJustifyUpRight	= new GUIContent("↗");
		private static readonly GUIContent	ContentJustifyLeft		= new GUIContent("←");
		private static readonly GUIContent	ContentJustifyCenter	= new GUIContent("C");
		private static readonly GUIContent	ContentJustifyRight		= new GUIContent("→");
		private static readonly GUIContent	ContentJustifyDownLeft	= new GUIContent("↙");
		private static readonly GUIContent	ContentJustifyDown		= new GUIContent("↓");
		private static readonly GUIContent	ContentJustifyDownRight	= new GUIContent("↘");
		private static readonly ToolTip		ToolTipJustifyUpLeft	= new ToolTip("Top Left Align",		"Align the texture to the top (smallest U) left (smallest V) corner of the surface");
		private static readonly ToolTip		ToolTipJustifyUp		= new ToolTip("Top Align",			"Align the texture to the top (smallest U) side of the surface");
		private static readonly ToolTip		ToolTipJustifyUpRight	= new ToolTip("Top Right Align",	"Align the texture to the top (smallest U) right (largest V) corner of the surface");
		private static readonly ToolTip		ToolTipJustifyLeft		= new ToolTip("Left Align",			"Align the texture to the left (smallest V) side of the surface");
		private static readonly ToolTip		ToolTipJustifyCenter	= new ToolTip("Center Align",		"Align the texture to the center of the surface");
		private static readonly ToolTip		ToolTipJustifyRight		= new ToolTip("Right Align",		"Align the texture to the right (largest V) side of the surface");
		private static readonly ToolTip		ToolTipJustifyDownLeft	= new ToolTip("Bottom Left Align",	"Align the texture to the bottom (largest U) left (smallest U) corner of the surface");
		private static readonly ToolTip		ToolTipJustifyDown		= new ToolTip("Bottom Align",		"Align the texture to the bottom (largest U) side of the surface");
		private static readonly ToolTip		ToolTipJustifyDownRight = new ToolTip("Bottom Right Align", "Align the texture to the bottom (largest U) right (largest U) corner of the surface");

		private static readonly GUIContent	ContentMaterial			= new GUIContent("Material");

		private static readonly GUIContent	ContentLockTexture		= new GUIContent("Lock texture to object");		
		private static readonly ToolTip		ToolTipLockTexture		= new ToolTip("Lock texture to object", "When not set, the texture will stay\n" +
																											"in the same world position when the\n" +
																											"brush is moved in the world. When \n" +
																											"set it'll move with the brush.");
		
		private static readonly GUIContent	ContentSelectAllSurfaces	= new GUIContent("Select all brush surfaces");
		private static readonly ToolTip		TooltipSelectAllSurfaces	= new ToolTip("Select all brush surfaces", "Select all the surfaces of all the\n" +
																												   "brushes that are currently selected.");
		

		private static readonly GUIContent	ContentReset			= new GUIContent("Reset");
		private static readonly GUIContent	ContentResetXY			= new GUIContent("UV");
		private static readonly GUIContent	ContentResetX			= new GUIContent("U");
		private static readonly GUIContent	ContentResetY			= new GUIContent("V");
		private static readonly ToolTip		ToolTipResetXY			= new ToolTip("Reset U & V coordinates", "Reset the texture coordinates in the U and V direction");
		private static readonly ToolTip		ToolTipResetX			= new ToolTip("Reset U coordinates", "Reset the texture coordinates in the U direction");
		private static readonly ToolTip		ToolTipResetY			= new ToolTip("Reset V coordinates", "Reset the texture coordinates in the V direction");

		private static readonly GUIContent	ContentFit				= new GUIContent("Fit");
		private static readonly GUIContent	ContentFitXY			= new GUIContent("UV");
		private static readonly GUIContent	ContentFitX				= new GUIContent("U");
		private static readonly GUIContent	ContentFitY				= new GUIContent("V");
		private static readonly ToolTip		ToolTipFitXY			= new ToolTip("Fit U & V coordinates", "Fit the texture coordinates in both the U and V direction");
		private static readonly ToolTip		ToolTipFitX				= new ToolTip("Fit U coordinates", "Fit the texture coordinates in the U direction");
		private static readonly ToolTip		ToolTipFitY				= new ToolTip("Fit V coordinates", "Fit the texture coordinates in the V direction");

		private static readonly GUIContent	ContentFlip				= new GUIContent("Reverse");
		private static readonly GUIContent	ContentFlipXY			= new GUIContent("UV");
		private static readonly GUIContent	ContentFlipX			= new GUIContent("U");
		private static readonly GUIContent	ContentFlipY			= new GUIContent("V");
		private static readonly ToolTip		ToolTipFlipXY			= new ToolTip("Flip U & V coordinates", "Reverse the texture coordinates in both the U and V direction");
		private static readonly ToolTip		ToolTipFlipX			= new ToolTip("Flip U coordinates", "Reverse the texture coordinates in the U direction");
		private static readonly ToolTip		ToolTipFlipY			= new ToolTip("Flip V coordinates", "Reverse the texture coordinates in the V direction");

		private static readonly GUIContent	ContentUVScale			= new GUIContent("UV Scale");
		private static readonly GUIContent	ContentScale			= new GUIContent("Scale");
		private static readonly ToolTip		ToolTipScaleUV			= new ToolTip("UV Scale", "Sets the U and V scale of the texture on the surface of the brush.");
		private static readonly GUIContent	ContentDoubleScale		= new GUIContent("x2");
		private static readonly GUIContent	ContentHalfScale		= new GUIContent("/2");
		private static readonly ToolTip		ToolTipDoubleScale		= new ToolTip("Double the scale", "Make the texturing bigger by doubling the scale. This will decrease the tiling (if any).");
		private static readonly ToolTip		ToolTipHalfScale		= new ToolTip("Half the scale", "Make the texturing smaller by halving the scale. This will increase the tiling (if any).");

		private static readonly GUIContent	ContentUSymbol			= new GUIContent("U");
		private static readonly GUIContent	ContentVSymbol			= new GUIContent("V");
		private static readonly GUIContent	ContentOffset			= new GUIContent("UV Offset");
		private static readonly ToolTip		ToolTipOffsetUV			= new ToolTip("UV Offset", "Sets the U and V position of the texture on the surface of the brush.");
		private static readonly GUIContent	ContentRotate			= new GUIContent("Rotation");
		private static readonly ToolTip		ToolTipRotation			= new ToolTip("Rotation", "Sets the rotation the texture on the surface of the brush.");
		private static readonly GUIContent	ContentAngleSymbol		= new GUIContent("°");
		private static readonly GUIContent	ContentRotate90Negative	= new GUIContent("-90°");
		private static readonly GUIContent	ContentRotate90Positive	= new GUIContent("+90°");
		private static readonly ToolTip		ToolTipRotate90Negative = new ToolTip("Rotate texture -90°", "Subtracts 90 from the rotation of the texture");
		private static readonly ToolTip		ToolTipRotate90Positive = new ToolTip("Rotate texture +90°", "Adds 90 to the rotation of the texture");
				
		private static readonly GUIContent	ContentUnSmoothSurfaces		= new GUIContent("Un-smooth");
		private static readonly GUIContent	ContentSmoothSurfaces		= new GUIContent("Smooth");
		private static readonly ToolTip		ToolTipUnSmoothSurfaces		= new ToolTip("Un-smooth", "Remove all selected smoothed surfaces from smoothing groups");
		private static readonly ToolTip		ToolTipSmoothSurfaces		= new ToolTip("Smooth", "Put all selected surfaces in same smoothing group");


		private static readonly GUILayoutOption		minFloatFieldWidth  = GUILayout.MinWidth(20);
//		private static readonly GUILayoutOption		labelWidth          = GUILayout.Width(labelWidthConst);
//		private static readonly GUILayoutOption		angleButtonWidth    = GUILayout.Width(buttonWidth);
		public static readonly GUILayoutOption		smallLabelWidth		= GUILayout.Width(45);
		public static readonly GUILayoutOption		largeLabelWidth		= GUILayout.Width(80);
		private static readonly GUILayoutOption		unitWidth			= GUILayout.Width(14);

		private static readonly GUILayoutOption		justifyButtonWidth	= GUILayout.Width(justifyButtonSize);
		private static readonly GUILayoutOption		justifyButtonHeight	= GUILayout.Height(justifyButtonSize);
		private static readonly GUILayoutOption[]	justifyButtonLayout = new GUILayoutOption[] { justifyButtonWidth, justifyButtonHeight };

		private static readonly GUILayoutOption		materialWidth		= GUILayout.Width(materialSize);
		private static readonly GUILayoutOption		materialHeight		= GUILayout.Height(materialSize);
//		private static readonly GUILayoutOption[]	materialDoubleWidth	= new GUILayoutOption[] { GUILayout.Width(windowWidth) };

		static void InitLocalStyles()
		{
			if (ContentSurfacesLabel != null)
				return;
			ContentSurfacesLabel = new GUIContent(CSG_GUIStyleUtility.brushEditModeNames[(int)ToolEditMode.Surfaces]);
		}
	}
}
