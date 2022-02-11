using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using InternalRealtimeCSG;

namespace RealtimeCSG
{
	internal sealed partial class EditModeClipGUI
	{
		private static GUIContent			ContentClipLabel;
		
		static readonly ClipMode[] clipModeValues = new ClipMode[]
			{
				ClipMode.RemovePositive,
				ClipMode.RemoveNegative,
				ClipMode.Split
//				,ClipEditBrushTool.ClipMode.Mirror			
			};

		private static readonly GUIContent	ContentDefaultMaterial	= new GUIContent("Default");
		private static readonly GUIContent	ContentCommit			= new GUIContent("Commit");
		private static readonly GUIContent	ContentCancel			= new GUIContent("Cancel");
		private static readonly ToolTip		CommitTooltip			= new ToolTip("Commit your changes", "Split the selected brush(es) with the current clipping plane. This makes your changes final.", Keys.PerformActionKey);
		private static readonly ToolTip		CancelTooltip			= new ToolTip("Cancel your changes", "Do not clip your selected brushes and return them to their original state.", Keys.CancelActionKey);

		private static readonly GUILayoutOption		largeLabelWidth		= GUILayout.Width(80);
		private static readonly GUILayoutOption[]	MaterialSceneWidth	= new GUILayoutOption[] { GUILayout.Width(140) };
	}
}
