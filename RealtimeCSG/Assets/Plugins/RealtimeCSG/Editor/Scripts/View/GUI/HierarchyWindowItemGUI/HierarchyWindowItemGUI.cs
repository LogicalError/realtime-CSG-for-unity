using UnityEngine;
using UnityEditor;
using RealtimeCSG.Foundation;
using RealtimeCSG.Components;

namespace RealtimeCSG
{
	internal sealed class HierarchyWindowItemGUI
	{
		internal static void OnHierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
		{
			if (EditorApplication.isPlayingOrWillChangePlaymode)
				return;

			var o = EditorUtility.InstanceIDToObject(instanceID) as GameObject;

			if (selectionRect.Contains(Event.current.mousePosition))
			{
				Transform t = (o == null) ? null : o.transform;
				SceneDragToolManager.OnHandleDragAndDrop(sceneView: null, transformInInspector: t, selectionRect: selectionRect);
			}

			if (o == null)
				return;
			
			CSG_GUIStyleUtility.InitStyles();

			var node = o.GetComponent<CSGNode>();
			if (node == null ||
				!node.enabled || (node.hideFlags & (HideFlags.HideInHierarchy | HideFlags.HideInInspector)) != 0)
				return;

			CSGOperationType operationType = CSGOperationType.Additive;

			var brush = node as CSGBrush;
			if (brush != null)
			{
				operationType = brush.OperationType;
				var skin = CSG_GUIStyleUtility.Skin;
				GUI.Label(selectionRect, skin.hierarchyOperations[(int)operationType], CSG_GUIStyleUtility.rightAlignedLabel);
				return;
			}
			var operation = node as CSGOperation;
			if (operation != null)
			{
				var skin = CSG_GUIStyleUtility.Skin;
				if (!operation.PassThrough)
				{
					operationType = operation.OperationType;
					var operationTypeIndex = (int)operationType;
					if (operationTypeIndex >= 0 && operationTypeIndex < skin.hierarchyOperations.Length)
						GUI.Label(selectionRect, skin.hierarchyOperations[operationTypeIndex], CSG_GUIStyleUtility.rightAlignedLabel);
				} else
				{
					GUI.Label(selectionRect, skin.hierarchyPassThrough, CSG_GUIStyleUtility.rightAlignedLabel);
				}
				return;
			}
		}

	}
}
