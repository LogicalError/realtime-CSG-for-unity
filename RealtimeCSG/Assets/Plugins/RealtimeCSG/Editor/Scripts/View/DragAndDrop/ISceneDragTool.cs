using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace RealtimeCSG
{

	internal interface ISceneDragTool
	{
		bool ValidateDrop		(bool inSceneView);
		bool ValidateDropPoint	(bool inSceneView);
		void Reset				();
		bool DragUpdated		(Transform transformInInspector, Rect selectionRect);
		bool DragUpdated		();
		void DragPerform		(bool inSceneView);
		void DragExited			(bool inSceneView);
		void OnPaint			();
	}

}
