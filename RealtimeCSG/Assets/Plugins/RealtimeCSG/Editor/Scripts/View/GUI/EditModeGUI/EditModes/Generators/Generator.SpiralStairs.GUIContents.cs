using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealtimeCSG
{
	internal sealed partial class GeneratorSpiralStairsGUI
	{
		private static readonly GUIContent	StepDepthContent			= new GUIContent("Step Depth");
		private static readonly ToolTip		StepDepthTooltip			= new ToolTip("Step Depth", "Set how deep the steps of the stairs are");
		
		private static readonly GUIContent	StepHeightContent			= new GUIContent("Step Height");
		private static readonly ToolTip		StepHeightTooltip			= new ToolTip("Step Height", "Set how high the steps of the stairs are");
		
		private static readonly GUIContent	StairsWidthContent			= new GUIContent("Stairs Width");
		private static readonly ToolTip		StairsWidthTooltip			= new ToolTip("Stairs Width", "Set how wide the entire staircase is");

		private static readonly GUIContent	StairsHeightContent			= new GUIContent("Stairs Height");
		private static readonly ToolTip		StairsHeightTooltip			= new ToolTip("Stairs Height", "Set how high the entire staircase is");
		
		private static readonly GUIContent	StairsDepthContent			= new GUIContent("Stairs Depth");
		private static readonly ToolTip		StairsDepthTooltip			= new ToolTip("Stairs Depth", "Set how deep the entire staircase is");
		
		private static readonly GUIContent	TotalStepsContent			= new GUIContent("Total Steps");
		private static readonly ToolTip		TotalStepsTooltip			= new ToolTip("Total Steps", "Set the total number of steps in this staircase");
		
		private static readonly GUIContent	ExtraDepthContent			= new GUIContent("Extra Depth");
		private static readonly ToolTip		ExtraDepthTooltip			= new ToolTip("Extra Depth", "Add an additional space before the steps start at the top");
		
		private static readonly GUIContent	ExtraHeightContent			= new GUIContent("Extra Height");
		private static readonly ToolTip		ExtraHeightTooltip			= new ToolTip("Extra Height", "Add additional height to the step at the bottom of the staircase");
		
		

		private static readonly GUILayoutOption width25					= GUILayout.Width(25);
		private static readonly GUILayoutOption width65					= GUILayout.Width(65);
		private static readonly GUILayoutOption width80					= GUILayout.Width(80);
	}
}
