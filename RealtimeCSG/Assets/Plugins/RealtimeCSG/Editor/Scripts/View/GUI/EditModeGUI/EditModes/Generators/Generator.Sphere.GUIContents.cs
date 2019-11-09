using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealtimeCSG
{
	internal sealed partial class GeneratorSphereGUI
	{
		private static readonly GUIContent	SmoothShadingContent		= new GUIContent("Smooth shading");
		private static readonly ToolTip		SmoothShadingTooltip		= new ToolTip("Smooth shading", "Toggle if you want the sides of the sphere have smooth lighting or have a faceted look.");
		private static readonly GUIContent	HemiSphereContent			= new GUIContent("Hemisphere");
		private static readonly ToolTip		HemiSphereTooltip			= new ToolTip("Hemisphere", "When toggled, create a hemisphere instead of a sphere.");
		private static readonly GUIContent	OffsetContent				= new GUIContent("Offset");
		private static readonly ToolTip		OffsetTooltip				= new ToolTip("Offset angle", "Set the offset angle at which the cylinder starts.");
		private static readonly GUIContent	SplitsContent				= new GUIContent("Splits");
		private static readonly ToolTip		SplitsTooltip				= new ToolTip("Number of splits", "Set the number of times the sides of the spherical cube to be split.");
		private static readonly GUIContent	RadiusContent				= new GUIContent("Radius");
		private static readonly ToolTip		RadiusTooltip				= new ToolTip("Radius", "Set the radius of the cylinder. The radius is half of the width of a cylinder.");
		
		private static readonly GUILayoutOption width25					= GUILayout.Width(25);
		private static readonly GUILayoutOption width65					= GUILayout.Width(65);
		private static readonly GUILayoutOption width120				= GUILayout.Width(120);
	}
}
