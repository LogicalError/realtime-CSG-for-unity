using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealtimeCSG
{
	internal sealed partial class GeneratorCylinderGUI
	{
		private static readonly GUIContent	SmoothShadingContent		= new GUIContent("Smooth shading");
		private static readonly ToolTip		SmoothShadingTooltip		= new ToolTip("Smooth shading", "Toggle if you want the sides of the cylinder have smooth lighting or have a faceted look.");
		private static readonly GUIContent	RadialCapsContent			= new GUIContent("Radial caps");
		private static readonly ToolTip		RadialCapsTooltip			= new ToolTip("Radial caps", "Toggle if you want the top and bottom of the cylinder be a single polygon, or have a triangle per side.");
		private static readonly GUIContent	AlignToSideContent			= new GUIContent("Start mid side");
		private static readonly ToolTip		AlignToSideTooltip			= new ToolTip("Start in the middle of a side", "Toggle if you want the cylinder to begin in the center of a side, or at a point.");
		private static readonly GUIContent	FitShapeContent				= new GUIContent("Fit shape");
		private static readonly ToolTip		FitShapeTooltip				= new ToolTip("Fit shape", "Toggle if you want the cylinder to be fitted to the square that encapsulates the full circle that's defined by its radius. This makes the shapes more predictable when they have only a few sides, but it may change its shape slightly.");
		private static readonly GUIContent	OffsetContent				= new GUIContent("Angle");
		private static readonly ToolTip		OffsetTooltip				= new ToolTip("Offset angle", "Set the offset angle at which the cylinder starts.");
		private static readonly GUIContent	SidesContent				= new GUIContent("Sides");
		private static readonly ToolTip		SidesTooltip				= new ToolTip("Number of sides", "Set the number of sides the cylinder has when generated.");
		private static readonly GUIContent	HeightContent				= new GUIContent("Height");
		private static readonly ToolTip		HeightTooltip				= new ToolTip("Height", "Set the height of the cylinder.");
		private static readonly GUIContent	RadiusContent				= new GUIContent("Radius");
		private static readonly ToolTip		RadiusTooltip				= new ToolTip("Radius", "Set the radius of the cylinder. The radius is half of the width of a cylinder.");
		
		private static readonly GUILayoutOption width20					= GUILayout.Width(25);
		private static readonly GUILayoutOption width65					= GUILayout.Width(65);
		private static readonly GUILayoutOption width80					= GUILayout.Width(80);
		private static readonly GUILayoutOption width110				= GUILayout.Width(110);
		private static readonly GUILayoutOption width120				= GUILayout.Width(120);
	}
}
