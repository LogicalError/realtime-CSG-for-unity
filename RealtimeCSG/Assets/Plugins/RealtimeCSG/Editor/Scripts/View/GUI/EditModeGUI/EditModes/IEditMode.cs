using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealtimeCSG
{
	internal interface IEditMode
	{
		bool UsesUnitySelection { get; }
		bool IgnoreUnityRect	{ get; }

		void HandleEvents		(SceneView sceneView, Rect rect);
		
		Rect GetLastSceneGUIRect();
		bool OnSceneGUI			(Rect windowRect);
		void OnInspectorGUI		(EditorWindow window, float height);

		void OnDisableTool		();
		void OnEnableTool		();
		bool UndoRedoPerformed	();
		bool DeselectAll		();

		void SetTargets			(FilteredSelection filteredSelection);
	
	}
}
