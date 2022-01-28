using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using RealtimeCSG.Foundation;

namespace RealtimeCSG
{
	internal static class Keys
	{

		[KeyDescription("Generate/Free draw")]
		public static readonly KeyEvent FreeBuilderMode					= new KeyEvent(KeyCode.N, hold: true);
		[KeyDescription("Generate/Cylinder")]
		public static readonly KeyEvent CylinderBuilderMode				= new KeyEvent(KeyCode.C, hold: true);
		[KeyDescription("Generate/Box")]
		public static readonly KeyEvent BoxBuilderMode					= new KeyEvent(KeyCode.B, hold: true);
		[KeyDescription("Generate/Sphere")]
		public static readonly KeyEvent SphereBuilderMode				= new KeyEvent(KeyCode.K, hold: true);
		[KeyDescription("Generate/Linear Stairs")]
		public static readonly KeyEvent LinearStairsBuilderMode			= new KeyEvent(KeyCode.L, hold: true);
		[KeyDescription("Generate/Spiral Stairs")]
		public static readonly KeyEvent SpiralStairsBuilderMode			= new KeyEvent(KeyCode.P, hold: true);
		
		[KeyDescription("Tools/Switch to Object mode")]
		public static readonly KeyEvent SwitchToObjectEditMode			= new KeyEvent(KeyCode.F1, EventModifiers.Alt);
		[KeyDescription("Tools/Switch to Generate mode")]
		public static readonly KeyEvent SwitchToGenerateEditMode		= new KeyEvent(KeyCode.F2, EventModifiers.Alt);
		[KeyDescription("Tools/Switch to Mesh mode")]
		public static readonly KeyEvent SwitchToMeshEditMode			= new KeyEvent(KeyCode.F3, EventModifiers.Alt);
		[KeyDescription("Tools/Switch to Clip mode")]
		public static readonly KeyEvent SwitchToClipEditMode			= new KeyEvent(KeyCode.F4, EventModifiers.Alt);
		[KeyDescription("Tools/Switch to Surface mode")]
		public static readonly KeyEvent SwitchToSurfaceEditMode			= new KeyEvent(KeyCode.F5, EventModifiers.Alt);

		[KeyDescription("Object mode/Center pivot")]
		public static readonly KeyEvent CenterPivot						= new KeyEvent(KeyCode.R, EventModifiers.Control);

		[KeyDescription("Mesh mode/Merge edge points")]
		public static readonly KeyEvent MergeEdgePoints					= new KeyEvent(KeyCode.M);

		[KeyDescription("Surface mode/Smear or copy material")]
		public static readonly KeyEvent CopyMaterialTexGen				= new KeyEvent(KeyCode.G, hold: true);

		[KeyDescription("Clip mode/Next clip mode")]
		public static readonly KeyEvent CycleClipModes					= new KeyEvent(KeyCode.Tab);

		[KeyDescription("Free draw/Insert point")]
		public static readonly KeyEvent InsertPoint						= new KeyEvent(KeyCode.I);

		[KeyDescription("Selection/Delete")]
		public static readonly KeyEvent DeleteSelectionKey				= new KeyEvent(KeyCode.Delete, KeyCode.Backspace);
		[KeyDescription("Selection/Clone Drag")]
		public static readonly KeyEvent CloneDragActivate				= new KeyEvent(new KeyCodeWithModifier(KeyCode.D, EventModifiers.None, _hold: true),
																					   new KeyCodeWithModifier(KeyCode.D, EventModifiers.Shift, _hold: true),
																					   new KeyCodeWithModifier(KeyCode.D, EventModifiers.Control | EventModifiers.Shift, _hold: true));

//		[KeyDescription("Selection/MoveVertical")]
//		public static readonly KeyEvent VerticalMoveMode				= new KeyEvent(KeyCode.Z, hold: true);

		[KeyDescription("Selection/SnapToGrid")]
		public static readonly KeyEvent SnapToGridKey					= new KeyEvent(KeyCode.End, EventModifiers.Control);

		[KeyDescription("Selection/QuickHide")]
		public static readonly KeyEvent QuickHideSelectedObjectsKey			= new KeyEvent(KeyCode.H, EventModifiers.None);
		[KeyDescription("Selection/QuickUnhide")]
		public static readonly KeyEvent QuickHideUnselectedObjectsKey		= new KeyEvent(KeyCode.H, EventModifiers.Control);
		[KeyDescription("Selection/ToggleVisibility")]
		public static readonly KeyEvent ToggleSelectedObjectVisibilityKey	= new KeyEvent(KeyCode.H, EventModifiers.Shift);
		[KeyDescription("Selection/UnhideAll")]
		public static readonly KeyEvent UnHideAllObjectsKey					= new KeyEvent(KeyCode.U, EventModifiers.None);

		[KeyDescription("Selection/Rotate clockwise")]
		public static readonly KeyEvent RotateSelectionLeft				= new KeyEvent(KeyCode.Comma, EventModifiers.Control);

		[KeyDescription("Selection/Rotate anti-clockwise")]
		public static readonly KeyEvent RotateSelectionRight			= new KeyEvent(KeyCode.Period, EventModifiers.Control);

		[KeyDescription("Selection/Move left")]
		public static readonly KeyEvent MoveSelectionLeft				= new KeyEvent(KeyCode.LeftArrow, EventModifiers.Control | EventModifiers.Shift);
		[KeyDescription("Selection/Move right")]
		public static readonly KeyEvent MoveSelectionRight				= new KeyEvent(KeyCode.RightArrow, EventModifiers.Control | EventModifiers.Shift);
		[KeyDescription("Selection/Move back")]
		public static readonly KeyEvent MoveSelectionBack				= new KeyEvent(KeyCode.DownArrow, EventModifiers.Control | EventModifiers.Shift);
		[KeyDescription("Selection/Move forward")]
		public static readonly KeyEvent MoveSelectionForward			= new KeyEvent(KeyCode.UpArrow, EventModifiers.Control | EventModifiers.Shift);
		[KeyDescription("Selection/Move down")]
		public static readonly KeyEvent MoveSelectionDown               = new KeyEvent(KeyCode.PageDown, EventModifiers.Control | EventModifiers.Shift);
		[KeyDescription("Selection/Move up")]
		public static readonly KeyEvent MoveSelectionUp					= new KeyEvent(KeyCode.PageUp, EventModifiers.Control | EventModifiers.Shift);

		[KeyDescription("Selection/Flip on X axis")]
		public static readonly KeyEvent FlipSelectionX					= new KeyEvent(KeyCode.X, EventModifiers.Control | EventModifiers.Shift);
		[KeyDescription("Selection/Flip on Y axis")]
		public static readonly KeyEvent FlipSelectionY					= new KeyEvent(KeyCode.Y, EventModifiers.Control | EventModifiers.Shift);
		[KeyDescription("Selection/Flip on Z axis")]
		public static readonly KeyEvent FlipSelectionZ					= new KeyEvent(KeyCode.Z, EventModifiers.Control | EventModifiers.Shift);

		[KeyDescription("Selection/Set as PassThrough")]
		public static readonly KeyEvent MakeSelectedPassThroughKey      = new KeyEvent(KeyCode.Question);
		[KeyDescription("Selection/Set as Additive")]
		public static readonly KeyEvent MakeSelectedAdditiveKey			= new KeyEvent(KeyCode.Equals, KeyCode.KeypadPlus);
		[KeyDescription("Selection/Set as Subtractive")]
		public static readonly KeyEvent MakeSelectedSubtractiveKey		= new KeyEvent(KeyCode.Minus, KeyCode.KeypadMinus);
		[KeyDescription("Selection/Set as Intersecting")]
		public static readonly KeyEvent MakeSelectedIntersectingKey		= new KeyEvent(KeyCode.Backslash, KeyCode.KeypadDivide);

		//	public static readonly KeyEvent GroupSelectionKey				= new KeyEvent(KeyCode.G, EventModifiers.Control); // see OperationsUtility / GroupSelectionInOperation

		[KeyDescription("Grid/Half grid size")]
		public static readonly KeyEvent HalfGridSizeKey					= new KeyEvent(KeyCode.LeftBracket);
		[KeyDescription("Grid/Double grid size")]
		public static readonly KeyEvent DoubleGridSizeKey				= new KeyEvent(KeyCode.RightBracket);
		[KeyDescription("Grid/Toggle grid rendering")]
		public static readonly KeyEvent ToggleShowGridKey				= new KeyEvent(KeyCode.G, EventModifiers.Shift);
		[KeyDescription("Grid/Toggle snapping")]
		public static readonly KeyEvent ToggleSnappingKey			    = new KeyEvent(KeyCode.T, EventModifiers.Shift);

		[KeyDescription("Action/Cancel or Deselect")]
		public static readonly KeyEvent CancelActionKey					= new KeyEvent(KeyCode.Escape);

		[KeyDescription("Action/Perform")]
		public static readonly KeyEvent PerformActionKey				= new KeyEvent(KeyCode.Return, KeyCode.KeypadEnter);
		

		public static bool HandleSceneValidate(IEditMode tool, bool checkForTextEditing = true)
		{
			if (EditorGUIUtility.editingTextField && checkForTextEditing)
			{
				return false;
			}
			
			if (Keys.MakeSelectedPassThroughKey .IsKeyPressed() && tool.UsesUnitySelection) { return true; }
			if (Keys.MakeSelectedAdditiveKey    .IsKeyPressed() && tool.UsesUnitySelection) { return true; }
			if (Keys.MakeSelectedSubtractiveKey .IsKeyPressed() && tool.UsesUnitySelection) { return true; }
			if (Keys.MakeSelectedIntersectingKey.IsKeyPressed() && tool.UsesUnitySelection) { return true; }
			
			if (Keys.ToggleSelectedObjectVisibilityKey	.IsKeyPressed() && tool.UsesUnitySelection) { return true; }
			if (Keys.QuickHideSelectedObjectsKey		.IsKeyPressed() && tool.UsesUnitySelection) { return true; }
			if (Keys.QuickHideUnselectedObjectsKey		.IsKeyPressed() && tool.UsesUnitySelection) { return true; }
			if (Keys.UnHideAllObjectsKey				.IsKeyPressed() && tool.UsesUnitySelection) { return true; }
			if (Keys.CancelActionKey					.IsKeyPressed()) { return true; }

			if (Keys.HalfGridSizeKey			.IsKeyPressed()) { return true; }
			if (Keys.DoubleGridSizeKey			.IsKeyPressed()) { return true; }
			if (Keys.ToggleShowGridKey			.IsKeyPressed()) { return true; }
			if (Keys.ToggleSnappingKey			.IsKeyPressed()) { return true; }
			
			return false;
		}

		public static bool HandleSceneKeyDown(IEditMode tool, bool checkForTextEditing = true)
		{
			if (EditorGUIUtility.editingTextField && checkForTextEditing)
			{
				return false;
			}
			
			if (Keys.MakeSelectedPassThroughKey .IsKeyPressed() && tool.UsesUnitySelection) { return true; }
			if (Keys.MakeSelectedAdditiveKey    .IsKeyPressed() && tool.UsesUnitySelection) { return true; }
			if (Keys.MakeSelectedSubtractiveKey .IsKeyPressed() && tool.UsesUnitySelection) { return true; }
			if (Keys.MakeSelectedIntersectingKey.IsKeyPressed() && tool.UsesUnitySelection) { return true; }
			
			if (Keys.ToggleSelectedObjectVisibilityKey	.IsKeyPressed() && tool.UsesUnitySelection) { return true; }
			if (Keys.QuickHideSelectedObjectsKey		.IsKeyPressed() && tool.UsesUnitySelection) { return true; }
			if (Keys.QuickHideUnselectedObjectsKey		.IsKeyPressed() && tool.UsesUnitySelection) { return true; }
			if (Keys.UnHideAllObjectsKey				.IsKeyPressed() && tool.UsesUnitySelection) { return true; }
			if (Keys.CancelActionKey					.IsKeyPressed()) { return true; }

			if (Keys.HalfGridSizeKey			.IsKeyPressed()) { return true; }
			if (Keys.DoubleGridSizeKey			.IsKeyPressed()) { return true; }
			if (Keys.ToggleShowGridKey			.IsKeyPressed()) { return true; }
			if (Keys.ToggleSnappingKey			.IsKeyPressed()) { return true; }
			return false;
		}

		public static bool HandleSceneKeyUp(IEditMode tool, bool checkForTextEditing = true)
		{
			if (EditorGUIUtility.editingTextField && checkForTextEditing)
			{
				return false;
			}
			
			if (Keys.MakeSelectedPassThroughKey .IsKeyPressed() && tool.UsesUnitySelection) { OperationsUtility.SetPassThroughOnSelected(); return true; }
			if (Keys.MakeSelectedAdditiveKey    .IsKeyPressed() && tool.UsesUnitySelection) { OperationsUtility.ModifyOperationsOnSelected(CSGOperationType.Additive); return true; }
			if (Keys.MakeSelectedSubtractiveKey .IsKeyPressed() && tool.UsesUnitySelection) { OperationsUtility.ModifyOperationsOnSelected(CSGOperationType.Subtractive); return true; }
			if (Keys.MakeSelectedIntersectingKey.IsKeyPressed() && tool.UsesUnitySelection) { OperationsUtility.ModifyOperationsOnSelected(CSGOperationType.Intersecting); return true; }
			
			if (Keys.ToggleSelectedObjectVisibilityKey	.IsKeyPressed() && tool.UsesUnitySelection) { SelectionUtility.ToggleSelectedObjectVisibility(); return true; }
			if (Keys.QuickHideSelectedObjectsKey		.IsKeyPressed() && tool.UsesUnitySelection) { SelectionUtility.HideSelectedObjects(); return true; }
			if (Keys.QuickHideUnselectedObjectsKey		.IsKeyPressed() && tool.UsesUnitySelection) { SelectionUtility.HideUnselectedObjects(); return true; }
			if (Keys.UnHideAllObjectsKey				.IsKeyPressed() && tool.UsesUnitySelection) { SelectionUtility.UnHideAll(); return true; }
			if (Keys.CancelActionKey					.IsKeyPressed()) { SelectionUtility.DeselectAll(); return true; }

			if (Keys.HalfGridSizeKey	.IsKeyPressed()) { GridUtility.HalfGridSize(); return true; }
			if (Keys.DoubleGridSizeKey	.IsKeyPressed()) { GridUtility.DoubleGridSize(); return true; }
			if (Keys.ToggleShowGridKey	.IsKeyPressed()) { GridUtility.ToggleShowGrid(); return true; }
			if (Keys.ToggleSnappingKey	.IsKeyPressed()) { GridUtility.ToggleSnapToGrid(); return true; }
			return false;
		}
	}
}