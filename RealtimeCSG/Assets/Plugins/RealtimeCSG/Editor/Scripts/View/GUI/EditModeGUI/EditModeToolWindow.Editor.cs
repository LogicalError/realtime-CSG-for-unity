using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RealtimeCSG.Components.CSGNode), true)]
[CanEditMultipleObjects]
public class EditModeToolWindowEditor : Editor
{
	public bool HasFrameBounds()			{ return RealtimeCSG.BoundsUtilities.HasFrameBounds(RealtimeCSG.EditModeManager.FilteredSelection); }		
	public Bounds OnGetFrameBounds()		{ return RealtimeCSG.BoundsUtilities.OnGetFrameBounds(RealtimeCSG.EditModeManager.FilteredSelection); }
	public override void OnInspectorGUI()
	{
		if (EditorApplication.isPlayingOrWillChangePlaymode)
			return;
		RealtimeCSG.EditModeSelectionGUI.OnInspectorGUI(this, this.targets);
	}
}