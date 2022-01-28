using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using InternalRealtimeCSG;

namespace RealtimeCSG
{
	internal sealed partial class EditModeMeshModeGUI
	{
		static GUIContent			ContentMeshLabel;
//		static GUIContent			ContentBrushesLabel;
//		static GUIContent			ContentEdgesLabel;
		private static readonly GUIContent	ContentDefaultMaterial	= new GUIContent("Default");
		
		private static readonly GUILayoutOption labelWidth			= GUILayout.Width(30);
		private static readonly GUILayoutOption largeLabelWidth		= GUILayout.Width(80);
		private static readonly GUILayoutOption[] InSceneWidth		= new GUILayoutOption[] { GUILayout.Width(150) };

		/*
		static readonly CSGOperationType[] operationValues = new CSGOperationType[]
			{
				CSGOperationType.Additive,
				CSGOperationType.Subtractive,
				CSGOperationType.Intersecting
			};
		*/
		static void InitLocalStyles()
		{
			if (ContentMeshLabel != null)
				return;
			ContentMeshLabel	= new GUIContent(CSG_GUIStyleUtility.brushEditModeNames[(int)ToolEditMode.Edit]);
//			ContentBrushesLabel	= new GUIContent(GUIStyleUtility.brushEditModeNames[(int)BrushEditMode.Brushes]);
//			ContentEdgesLabel	= new GUIContent("Edges");
		}

		static readonly GUIContent	ContentFlip					= new GUIContent("Flip");
		static readonly GUIContent	ContentFlipX				= new GUIContent("X");
		static readonly ToolTip		TooltipFlipX				= new ToolTip("Flip X", "Flip the selection in the x direction", Keys.FlipSelectionX);
		static readonly GUIContent	ContentFlipY				= new GUIContent("Y");
		static readonly ToolTip		TooltipFlipY				= new ToolTip("Flip Y", "Flip the selection in the y direction", Keys.FlipSelectionY);
		static readonly GUIContent	ContentFlipZ				= new GUIContent("Z");
		static readonly ToolTip		TooltipFlipZ				= new ToolTip("Flip Z", "Flip the selection in the z direction", Keys.FlipSelectionZ);
		static readonly GUIContent	ContentSnapToGrid			= new GUIContent("Snap to grid");
		static readonly ToolTip		TooltipSnapToGrid			= new ToolTip(ContentSnapToGrid.text, "Snap the selection to the closest grid lines", Keys.SnapToGridKey);
			
		static readonly GUIContent	ContentSelection			= new GUIContent("Selection");
		static readonly GUIContent	ContentIgnoreHidden			= new GUIContent("Ignore Hidden Surfaces");
		static readonly ToolTip		TooltipIgnoreHidden			= new ToolTip("Ignore Hidden Surfaces", "When set, selecting surfaces will\n" +
																								    "only select the front most surfaces.");
		
		static readonly GUIContent	ContentVertex				= new GUIContent("Vertex");
		static readonly ToolTip		TooltipVertex				= new ToolTip("Vertex", "When set you can select vertices.");
		static readonly GUIContent	ContentEdge					= new GUIContent("Edge");
		static readonly ToolTip		TooltipEdge					= new ToolTip("Edge", "When set you can select edges.");
		static readonly GUIContent	ContentSurface				= new GUIContent("Surface");
		static readonly ToolTip		TooltipSurface				= new ToolTip("Surface", "When set you can select surfaces.");

		static readonly GUIContent  ContentAutoCommitExtrusion  = new GUIContent("Auto commit extrusion");
		static readonly ToolTip		TooltipAutoCommitExtrusion	= new ToolTip("Auto commit extrusion", "When set, when extruding a surface,\n"+
																									   "using shift drag on a polygon,\n" +
																									   "editing will automatically commit\n" +
																									   "the brush when releasing the mouse\n" +
																									   "button.");
		
	}
}
