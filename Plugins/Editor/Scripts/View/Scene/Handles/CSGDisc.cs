using UnityEditor;
using UnityEngine;

namespace RealtimeCSG.Helpers
{
	public class CSGDisc
	{
		static Vector2 s_StartMousePosition, s_CurrentMousePosition;
		static Vector3 s_StartPosition, s_StartAxis;
		static Quaternion s_StartRotation;
		static float s_RotationDist;

		static float SnapValue(float val, float snap, bool snapping)
		{
			if (snapping && snap > 0)
			{
				return Mathf.Round(val / snap) * snap;
			}
			return val;
		}

		public static Quaternion Do(Camera camera, int id, string name, Quaternion rotation, Vector3 position, Vector3 axis, float size, bool cutoffPlane, bool snapping, float snap, CSGHandles.InitFunction initFunction, CSGHandles.InitFunction shutdownFunction)
		{
            if (Mathf.Abs(Vector3.Dot(camera.transform.forward, axis)) > .999f)
				cutoffPlane = false;

			var evt = Event.current;
			switch (evt.GetTypeForControl(id))
			{
				case EventType.Layout:
				{
					float d;
					if (cutoffPlane)
					{
						var from = Vector3.Cross(axis, camera.transform.forward).normalized;
						d = HandleUtility.DistanceToArc(position, axis, from, 180, size) / 2;
					} else
					{
						d = HandleUtility.DistanceToDisc(position, axis, size) / 2;
					}

					HandleUtility.AddControl(id, d);
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
						GUIUtility.hotControl = GUIUtility.keyboardControl = id;    // Grab mouse focus
						//Tools.LockHandlePosition();
						if (cutoffPlane)
						{
							Vector3 from = Vector3.Cross(axis, camera.transform.forward).normalized;
							s_StartPosition = HandleUtility.ClosestPointToArc(position, axis, from, 180, size);
						} else
						{
							s_StartPosition = HandleUtility.ClosestPointToDisc(position, axis, size);
						}
						s_RotationDist = 0;
						s_StartRotation = rotation;
						s_StartAxis = axis;
						s_CurrentMousePosition = s_StartMousePosition = Event.current.mousePosition;
						evt.Use();
						EditorGUIUtility.SetWantsMouseJumping(1);
					}
					break;
				}
				case EventType.MouseDrag:
				{ 
					if (GUIUtility.hotControl == id)
					{
						var direction = Vector3.Cross(axis, position - s_StartPosition).normalized;
						s_CurrentMousePosition += evt.delta;
						s_RotationDist = HandleUtility.CalcLineTranslation(s_StartMousePosition, s_CurrentMousePosition, s_StartPosition, direction) / size * 30;
						s_RotationDist = SnapValue(s_RotationDist, snap, snapping);
						rotation = Quaternion.AngleAxis(s_RotationDist * -1, s_StartAxis) * s_StartRotation;
						
						GUI.changed = true;
						evt.Use();
					}
					break;
				}
				case EventType.MouseUp:
				{
					if (GUIUtility.hotControl == id && (evt.button == 0 || evt.button == 2))
					{
						//Tools.UnlockHandlePosition();
						GUIUtility.hotControl = 0;
						evt.Use();
						if (shutdownFunction != null)
							shutdownFunction();
						EditorGUIUtility.SetWantsMouseJumping(0);
					}
					break;
				}
				case EventType.KeyDown:
				{
					if (evt.keyCode == KeyCode.Escape && GUIUtility.hotControl == id)
					{
						// We do not use the event nor clear hotcontrol to ensure auto revert value kicks in from native side
						//Tools.UnlockHandlePosition();
						EditorGUIUtility.SetWantsMouseJumping(0);
					}
					break;
				}
				case EventType.Repaint:
				{
					var originalColor = Handles.color;
					if (id == GUIUtility.keyboardControl)
						Handles.color = Handles.selectedColor;
					
					// If we're dragging it, we'll go a bit further and draw a selection pie
					if (GUIUtility.hotControl == id)
					{
						Vector3 from = Vector3.Cross(axis, Vector3.Cross(axis, (s_StartPosition - position).normalized).normalized);

						float radius = size * 1.1f;
						float endAngle = s_RotationDist;

						PaintUtility.DrawRotateCircle(camera, position, axis, from, radius, 0, 0, endAngle, Handles.color, name, true);						
						PaintUtility.DrawRotateCirclePie(position, axis, from, radius, 0, 0, endAngle, Handles.color);
						/*
						Color t = Handles.color;
						Vector3 from = (s_StartPosition - position).normalized;
						Handles.color = Handles.secondaryColor;
						Handles.DrawLine(position, position + from * size * 1.1f);
						float d = Mathf.Repeat(-s_RotationDist - 180, 360) - 180;
						Vector3 to = Quaternion.AngleAxis(d, axis) * from;
						Handles.DrawLine(position, position + to * size * 1.1f);

						Handles.color = Handles.secondaryColor * new Color(1, 1, 1, .2f);
						Handles.DrawSolidArc(position, axis, from, d, size);
						Handles.color = t;
						*/
					} else
					if (CSGHandles.disabled)
						Handles.color = Color.Lerp(originalColor, Handles.secondaryColor, 0.75f);

					if (cutoffPlane)
					{
						Vector3 from = Vector3.Cross(axis, camera.transform.forward).normalized;
						Handles.DrawWireArc(position, axis, from, 180, size);
					} else
					{
						Handles.DrawWireDisc(position, axis, size);
					}

					Handles.color = originalColor;
					break;
				}
			}

			return rotation;
		}
	}
}
