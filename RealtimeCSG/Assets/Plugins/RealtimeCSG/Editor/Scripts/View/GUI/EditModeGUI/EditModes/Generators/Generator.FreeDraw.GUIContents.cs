using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealtimeCSG
{
	internal sealed partial class GeneratorFreeDrawGUI
	{
//		private static readonly GUIContent	CommitContent			= new GUIContent("Commit");
//		private static readonly GUIContent	CancelContent			= new GUIContent("Cancel");
//		private static readonly ToolTip		CommitTooltip			= new ToolTip("Generate your brush", "Create the brush from the current shape.", Keys.PerformActionKey);
//		private static readonly ToolTip		CancelTooltip			= new ToolTip("Cancel brush creation", "Do not generate the brush.", Keys.CancelActionKey);

		private static readonly GUIContent	HeightContent			= new GUIContent("Height");
		private static readonly ToolTip		HeightTooltip			= new ToolTip("Height", "Set the height of the shape.");
		private static readonly GUIContent	CurveSidesContent		= new GUIContent("Splits");
		private static readonly ToolTip		CurveSidesTooltip		= new ToolTip("Curve Splits", "How many sides should each edge that's turned into a curve have.");
		private static readonly GUIContent	EdgeTypeContent			= new GUIContent("Edge");
		private static readonly ToolTip		EdgeTypeTooltip			= new ToolTip("Edge Curve Type", "Set the current selected edges to be Straight, Broken Curve or an Aligned Curve.");
		private static readonly GUIContent	SplitSelectedContent	= new GUIContent("Insert point");
		private static readonly ToolTip		SplitSelectedTooltip	= new ToolTip("Insert point", "Insert a new point in the middle of each selected edge.", Keys.InsertPoint);
		
		
		private static readonly GUILayoutOption width20				= GUILayout.Width(25);
		private static readonly GUILayoutOption width65				= GUILayout.Width(65);
	}
}
