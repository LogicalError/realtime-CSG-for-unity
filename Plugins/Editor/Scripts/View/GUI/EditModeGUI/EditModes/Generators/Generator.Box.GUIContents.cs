using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealtimeCSG
{
	internal sealed partial class GeneratorBoxGUI
	{
		private static readonly GUIContent	LengthContent		= new GUIContent("Length (X)");
		private static readonly ToolTip		LengthTooltip		= new ToolTip("Length (X)", "Set the length of the box, this is in the X direction.");
		private static readonly GUIContent	HeightContent		= new GUIContent("Height (Y)");
		private static readonly ToolTip		HeightTooltip		= new ToolTip("Height (Y)", "Set the height of the box, this is in the Y direction.");
		private static readonly GUIContent	WidthContent		= new GUIContent("Width (Z)");
		private static readonly ToolTip		WidthTooltip		= new ToolTip("Width (Z)", "Set the width of the box, this is in the Z direction.");
		
		private static readonly GUILayoutOption Width25			= GUILayout.Width(25);
		private static readonly GUILayoutOption Width65			= GUILayout.Width(65);
	}
}
