using UnityEditor;
using UnityEngine;

namespace RealtimeCSG.Helpers
{
	public class CSGFreeMove
	{
		private static Vector2 s_StartMousePosition, s_CurrentMousePosition;
		private static Vector3 s_StartPosition;
		private static Vector3[] s_SnapVertices;

		public static Vector3 Do(Camera camera, int id, Vector3 position, Quaternion rotation, float size, CSGHandles.CapFunction handleFunction, bool snapping, Vector3[] snapVertices)
		{
            var worldPosition	= Handles.matrix.MultiplyPoint(position);
			var origMatrix		= Handles.matrix;
			
			var evt = Event.current;
			switch (evt.GetTypeForControl(id))
			{
				case EventType.Layout:
				{
					Handles.matrix = Matrix4x4.identity;
					handleFunction(id, position, rotation, size, EventType.Layout);
					Handles.matrix = origMatrix;
					break;
				}
				case EventType.MouseDown:
				{
					if ((HandleUtility.nearestControl == id && evt.button == 0) || 
						(GUIUtility.keyboardControl == id && evt.button == 2))
					{
						GUIUtility.hotControl = GUIUtility.keyboardControl = id;
						s_CurrentMousePosition = s_StartMousePosition = evt.mousePosition;
						s_StartPosition = position;
						if (snapVertices != null)
							s_SnapVertices = snapVertices;
						else
							s_SnapVertices = new Vector3[] { s_StartPosition };
						evt.Use();
						EditorGUIUtility.SetWantsMouseJumping(1);
					}
					break;
				}
				case EventType.MouseDrag:
				{
					if (GUIUtility.hotControl == id)
					{
						s_CurrentMousePosition += new Vector2(evt.delta.x, -evt.delta.y);
						var screenPos = camera.WorldToScreenPoint(Handles.matrix.MultiplyPoint(s_StartPosition));
						screenPos += (Vector3)(s_CurrentMousePosition - s_StartMousePosition);
						position = Handles.inverseMatrix.MultiplyPoint(camera.ScreenToWorldPoint(screenPos));

						if (snapping)
							position = RealtimeCSG.CSGGrid.SnapDeltaToGrid(camera, position - s_StartPosition, s_SnapVertices, snapToGridPlane: false, snapToSelf: true) + s_StartPosition; 
						else
							position = RealtimeCSG.CSGGrid.HandleLockedAxi(position - s_StartPosition) + s_StartPosition;
						GUI.changed = true;
						evt.Use();
					}
					break;
				}
				case EventType.MouseUp:
				{
					if (GUIUtility.hotControl == id && (evt.button == 0 || evt.button == 2))
					{
						GUIUtility.hotControl = 0;
						evt.Use();
						EditorGUIUtility.SetWantsMouseJumping(0);
					}
					break;
				}
				case EventType.Repaint:
				{
					var temp = Color.white;
					if (id == GUIUtility.keyboardControl)
					{
						temp = Handles.color;
						Handles.color = Handles.selectedColor;
					}
					
					Handles.matrix = Matrix4x4.identity;
					handleFunction(id, worldPosition, camera.transform.rotation, size, EventType.Repaint);
					Handles.matrix = origMatrix;

					if (id == GUIUtility.keyboardControl)
						Handles.color = temp;
					break;
				}
			}
			return position;
		}
	}
}
