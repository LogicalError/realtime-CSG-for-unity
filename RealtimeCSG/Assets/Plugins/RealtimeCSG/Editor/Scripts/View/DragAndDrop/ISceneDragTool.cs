using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace RealtimeCSG
{

	internal interface ISceneDragTool
	{
		bool ValidateDrop		(SceneView sceneView);
		bool ValidateDropPoint	(SceneView sceneView);
		void Reset				();
		bool DragUpdated		(Transform transformInInspector, Rect selectionRect);
		bool DragUpdated		(SceneView sceneView);
		void DragPerform		(SceneView sceneView);
		void DragExited			(SceneView sceneView);
		void OnPaint			(Camera camera);
	}

}
