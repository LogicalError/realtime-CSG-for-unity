using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RealtimeCSG.Helpers
{
	public static class CSGSlider1D
	{
		private static Vector2 s_StartMousePosition, s_CurrentMousePosition;
		private static Vector3 s_StartPosition;
		private static Vector3[] s_SnapVertices;

		internal static Vector3 Do(Camera camera, int id, Vector3 position, Vector3 direction, float size, CSGHandles.CapFunction capFunction, SnapMode snapMode, Vector3[] snapVertices, CSGHandles.InitFunction initFunction = null, CSGHandles.InitFunction shutdownFunction = null)
		{
			var evt = Event.current;
			switch (evt.GetTypeForControl(id))
			{
				case EventType.Layout:
				{
					if (capFunction != null) 
						capFunction(id, position, Quaternion.LookRotation(direction), size, EventType.Layout);
					else
						HandleUtility.AddControl(id, HandleUtility.DistanceToCircle(position, size * .2f));
					break;
				}
				case EventType.MouseDown:
				{
					if (CSGHandles.disabled)
						break;
					if (((HandleUtility.nearestControl == id && evt.button == 0) || 
						 (GUIUtility.keyboardControl == id && evt.button == 2)) && GUIUtility.hotControl == 0)
					{
						s_SnapVertices = null;
						if (initFunction != null)
							initFunction();
						GUIUtility.hotControl = GUIUtility.keyboardControl = id;
						s_CurrentMousePosition = s_StartMousePosition = evt.mousePosition;
						s_StartPosition = position;
						evt.Use();
						EditorGUIUtility.SetWantsMouseJumping(1);
					}

					break;
				}
				case EventType.MouseDrag:
				{
					if (GUIUtility.hotControl == id)
					{
						if (s_SnapVertices == null)
						{ 
							if (snapVertices != null)
								s_SnapVertices = snapVertices.ToArray();
							else
								s_SnapVertices = new Vector3[] { s_StartPosition };
						}
						s_CurrentMousePosition += evt.delta;
						var dist			= HandleUtility.CalcLineTranslation(s_StartMousePosition, s_CurrentMousePosition, s_StartPosition, direction);
						var worldDirection	= Handles.matrix.MultiplyVector(direction);
						var worldPosition	= Handles.matrix.MultiplyPoint(s_StartPosition) + (worldDirection * dist);
						position			= Handles.inverseMatrix.MultiplyPoint(worldPosition);

                        switch(snapMode)
                        {
                            case SnapMode.GridSnapping:
                            {
                                var delta = RealtimeCSG.CSGGrid.SnapDeltaToGrid(camera, position - s_StartPosition, s_SnapVertices, snapToGridPlane: false, snapToSelf: true);
                                position = delta + s_StartPosition;
                                break;
                            }
                            case SnapMode.RelativeSnapping:
                            {
                                var delta = RealtimeCSG.CSGGrid.SnapDeltaRelative(camera, position - s_StartPosition, snapToGridPlane: false);
                                position = delta + s_StartPosition;
                                break;
                            }
                            default:
                            case SnapMode.None:
                            {
                                position = RealtimeCSG.CSGGrid.HandleLockedAxi(position - s_StartPosition) + s_StartPosition;
                                break;
                            }
                        }
						
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
						if (shutdownFunction != null)
							shutdownFunction();
						evt.Use();
						EditorGUIUtility.SetWantsMouseJumping(0);
					}
					break;
				}
				case EventType.Repaint:
				{
					var originalColor = Handles.color;
                    Handles.color = CSGHandles.StateColor(originalColor, CSGHandles.disabled, CSGHandles.FocusControl == id);
                    if (capFunction != null)
						capFunction(id, position, Quaternion.LookRotation(direction), size, EventType.Repaint);

					Handles.color = originalColor;
					break;
				}
			}
			return position;
		}

	}
}
