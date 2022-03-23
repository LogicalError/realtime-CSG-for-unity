using InternalRealtimeCSG;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealtimeCSG
{
	internal sealed partial class EditModeGenerateGUI
	{
		static GUIContent			ContentTitleLabel;
		
		private static readonly GUIContent	ContentDefaultMaterial	= new GUIContent("Default");
		private static readonly GUIContent	CreateContent			= new GUIContent("Create");
		private static readonly GUIContent	CancelContent			= new GUIContent("Cancel");
		private static readonly ToolTip		CreateTooltip			= new ToolTip("Create your brush", "Create the brush from the current box shape.", Keys.PerformActionKey);
		private static readonly ToolTip		CancelTooltip			= new ToolTip("Cancel brush creation", "Do not generate the brush.", Keys.CancelActionKey);

		private static readonly GUILayoutOption largeLabelWidth = GUILayout.Width(80);
		private static readonly GUILayoutOption Width100		= GUILayout.Width(100);

		static void InitLocalStyles()
		{
			if (ContentTitleLabel != null)
				return;
			ContentTitleLabel	= new GUIContent(CSG_GUIStyleUtility.brushEditModeNames[(int)ToolEditMode.Generate]);
		}
	}
}
