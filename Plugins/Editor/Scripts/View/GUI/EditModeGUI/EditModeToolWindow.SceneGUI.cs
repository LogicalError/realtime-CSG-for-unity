using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class EditModeToolWindowSceneGUI : EditorWindow
{
	private const string WindowTitle = "Realtime-CSG";
	private static List<EditModeToolWindowSceneGUI> windows = new List<EditModeToolWindowSceneGUI>();
	
	public static List<EditModeToolWindowSceneGUI> GetEditorWindows()
	{
		for (int i = windows.Count - 1; i >= 0; i--)
		{
			if (!windows[i])
				windows.Remove(windows[i]);
		}
		return windows; 
	}

	[MenuItem ("Window/Realtime-CSG window %F2")]
	public static EditModeToolWindowSceneGUI GetWindow ()
	{
		var editorWindow = EditorWindow.GetWindow<EditModeToolWindowSceneGUI>(WindowTitle, true, typeof(EditModeToolWindowEditor));
		editorWindow.minSize = new Vector2(32,  64);
		editorWindow.Show();
		return editorWindow;
	}

	public void OnGUI()
	{
		RealtimeCSG.EditModeSelectionGUI.HandleWindowGUI(this);
	}

	void Awake()
	{
		windows.Add(this);
	}

	void Update()
	{
		// apparently 'Awake' is not reliable ...
		if (windows.Contains(this))
			return;
		windows.Add(this);
	}
	
	void OnDestroy()
	{
		windows.Remove(this);
	}
}
