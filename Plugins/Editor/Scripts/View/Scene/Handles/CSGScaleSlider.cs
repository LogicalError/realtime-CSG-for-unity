using UnityEditor;
using UnityEngine;

namespace RealtimeCSG.Helpers
{
	public class CSGScaleSlider
	{
		private static float s_StartScale, s_ScaleDrawLength = 1.0f;
		private static Vector3 s_StartScale3;
		private static float s_ValueDrag;
		private static Vector2 s_StartMousePosition, s_CurrentMousePosition;

		static float SnapValue(float val, float snap, bool snapping)
		{
			if (snapping && snap > 0)
			{
				return Mathf.Round(val / snap) * snap;
			}
			return val;
		}

		public static float DoAxis(int id, float scale, Vector3 position, Vector3 direction, Quaternion rotation, float size, bool snapping, float snap, CSGHandles.InitFunction initFunction, CSGHandles.InitFunction shutdownFunction)
		{
			var evt = Event.current;
			switch (evt.GetTypeForControl(id))
			{
				case EventType.Layout:
				{ 
					HandleUtility.AddControl(id, HandleUtility.DistanceToLine(position, position + direction * size));
					HandleUtility.AddControl(id, HandleUtility.DistanceToCircle(position + direction * size, size * .2f));
					break;
				}
				case EventType.MouseDown:
				{
					if (CSGHandles.disabled)
						break;
					if (((HandleUtility.nearestControl == id && evt.button == 0) || 
					 	 (GUIUtility.keyboardControl == id && evt.button == 2)) && GUIUtility.hotControl == 0)
					{
						if (initFunction != null)
							initFunction();
						GUIUtility.hotControl = GUIUtility.keyboardControl = id;
						s_CurrentMousePosition = s_StartMousePosition = evt.mousePosition;
						s_StartScale = scale;
						evt.Use();
						EditorGUIUtility.SetWantsMouseJumping(1);
					}
					break;
				}
				case EventType.MouseDrag:
				{ 
					if (GUIUtility.hotControl == id)
					{
						s_CurrentMousePosition += evt.delta;
						float dist = 1 + HandleUtility.CalcLineTranslation(s_StartMousePosition, s_CurrentMousePosition, position, direction) / size;
						dist = SnapValue(dist, snap, snapping);
						scale = s_StartScale * dist;
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
						if (shutdownFunction != null)
							shutdownFunction();
						EditorGUIUtility.SetWantsMouseJumping(0);
					}
					break;
				}
				case EventType.Repaint:
				{
					var originalColor = Handles.color;
					if (id == GUIUtility.keyboardControl)
					{
						Handles.color = Handles.selectedColor;
					} else
					if (CSGHandles.disabled)
						Handles.color = Color.Lerp(originalColor, Handles.secondaryColor, 0.75f);

					float s = size;
					if (GUIUtility.hotControl == id)
					{
						s = size * scale / s_StartScale;
					}
#if UNITY_5_6_OR_NEWER
					Handles.CubeHandleCap (id, position + direction * s * s_ScaleDrawLength, rotation, size * .1f, EventType.Repaint);
#else
					Handles.CubeCap(id, position + direction * s * s_ScaleDrawLength, rotation, size * .1f);
#endif
					Handles.DrawLine(position, position + direction * (s * s_ScaleDrawLength - size * .05f));

					Handles.color = originalColor;
					break;
				}
			}

			return scale;
		}

		
		public static Vector3 DoCenter(int id, Vector3 value, Vector3 position, Quaternion rotation, float size, CSGHandles.CapFunction capFunction, bool snapping, float snap, CSGHandles.InitFunction initFunction, CSGHandles.InitFunction shutdownFunction)
		{
			size *= 0.5f;
			var evt = Event.current;
			switch (evt.GetTypeForControl(id))
			{
				case EventType.Layout:
				{ 
					capFunction(id, position, rotation, size, EventType.Layout);
					break;
				}
				case EventType.MouseDown:
				{
					if (CSGHandles.disabled)
						break;
					if (((HandleUtility.nearestControl == id && evt.button == 0) ||
						 (GUIUtility.keyboardControl == id && evt.button == 2)) && GUIUtility.hotControl == 0)
					{
						if (initFunction != null)
							initFunction();
						GUIUtility.hotControl = GUIUtility.keyboardControl = id;
						s_StartScale3 = value;
						s_ValueDrag = 0;
						evt.Use();
						EditorGUIUtility.SetWantsMouseJumping(1);
					}
					break;
				}
				case EventType.MouseDrag:
				{ 
					if (GUIUtility.hotControl == id)
					{
						s_ValueDrag += HandleUtility.niceMouseDelta * .01f;

						var oldScaleValue = s_StartScale3.x;
						var newScaleValue = (SnapValue(s_ValueDrag, snap, snapping) + 1.0f) * s_StartScale3.x;

						float dif = newScaleValue / oldScaleValue;
						if (!RealtimeCSG.CSGSettings.LockAxisX) value.x = newScaleValue;
						if (!RealtimeCSG.CSGSettings.LockAxisY) value.y = s_StartScale3.y * dif;
						if (!RealtimeCSG.CSGSettings.LockAxisZ) value.z = s_StartScale3.z * dif;
						
						s_ScaleDrawLength = value.x / s_StartScale3.x;
						GUI.changed = true;
						evt.Use();
					}
					break;
				}
				case EventType.KeyDown:
				{ 
					if (GUIUtility.hotControl == id)
					{
						if (evt.keyCode == KeyCode.Escape)
						{
							value = s_StartScale3;
							s_ScaleDrawLength = 1.0f;
							GUIUtility.hotControl = 0;
							GUI.changed = true;
							evt.Use();
						}
					}
					break;
				}
				case EventType.MouseUp:
				{ 
					if (GUIUtility.hotControl == id && (evt.button == 0 || evt.button == 2))
					{
						GUIUtility.hotControl = 0;
						s_ScaleDrawLength = 1.0f;
						evt.Use();
						if (shutdownFunction != null)
							shutdownFunction();
						EditorGUIUtility.SetWantsMouseJumping(0);
					}
					break;
				}
				case EventType.Repaint:
				{ 
					var originalColor = Handles.color;
					if (id == GUIUtility.keyboardControl)
						Handles.color = Handles.selectedColor;
					else
					if (CSGHandles.disabled)
						Handles.color = Color.Lerp(originalColor, Handles.secondaryColor, 0.75f);
					
					capFunction(id, position, rotation, size, EventType.Repaint);

					Handles.color = originalColor;
					break;
				}
			}

			return value;
		}
	}
}
