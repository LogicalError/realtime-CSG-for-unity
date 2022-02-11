using UnityEditor;
using UnityEngine;

namespace RealtimeCSG.Helpers
{
	public class CSGSlider2D
	{
		private static Vector2 s_CurrentMousePosition;
		private static Vector3 s_StartPosition;
		private static Vector3[] s_SnapVertices;
		private static Vector2 s_StartPlaneOffset;
		
		public static Vector3 Do(Camera camera, int id, Vector3 handlePos, Vector3 offset, Vector3 handleDir, Vector3 slideDir1, Vector3 slideDir2, float handleSize, CSGHandles.CapFunction capFunction, SnapMode snapMode, Vector3[] snapVertices, CSGHandles.InitFunction initFunction, CSGHandles.InitFunction shutdownFunction)
		{
			bool orgGuiChanged = GUI.changed;
			GUI.changed = false;

			var delta = CalcDeltaAlongDirections(id, handlePos, offset, handleDir, slideDir1, slideDir2, handleSize, capFunction, snapMode, snapVertices, initFunction, shutdownFunction);
			if (GUI.changed)
			{
				handlePos = s_StartPosition + slideDir1 * delta.x + slideDir2 * delta.y;

				if (s_SnapVertices == null)
				{
					if (snapVertices != null)
						s_SnapVertices = snapVertices;
					else
						s_SnapVertices = new Vector3[] { s_StartPosition };
				}

                switch (snapMode)
                {
                    case SnapMode.GridSnapping:
                    {
                        handlePos = RealtimeCSG.CSGGrid.SnapDeltaToGrid(camera, handlePos - s_StartPosition, s_SnapVertices, snapToGridPlane: false, snapToSelf: true) + s_StartPosition;
                        break;
                    }
                    case SnapMode.RelativeSnapping:
                    {
                        handlePos = RealtimeCSG.CSGGrid.SnapDeltaRelative(camera, handlePos - s_StartPosition, snapToGridPlane: false) + s_StartPosition;
                        break;
                    }
                    default:
                    case SnapMode.None:
                    {
                        handlePos = RealtimeCSG.CSGGrid.HandleLockedAxi(handlePos - s_StartPosition) + s_StartPosition;
                        break;
                    }
                }
			}

			GUI.changed |= orgGuiChanged;
			return handlePos;
		}
		
		private static Vector2 CalcDeltaAlongDirections(int id, Vector3 handlePos, Vector3 offset, Vector3 handleDir, Vector3 slideDir1, Vector3 slideDir2, float handleSize, CSGHandles.CapFunction capFunction, SnapMode snapMode, Vector3[] snapVertices, CSGHandles.InitFunction initFunction, CSGHandles.InitFunction shutdownFunction)
		{
			var position = handlePos + offset;
			var rotation = Quaternion.LookRotation(handleDir, slideDir1);
			var deltaDistanceAlongDirections = new Vector2(0, 0);

			var evt = Event.current;
			switch (evt.GetTypeForControl(id))
			{
				case EventType.Layout:
				{
					if (capFunction != null)
						capFunction(id, position, rotation, handleSize, EventType.Layout);
					else
						HandleUtility.AddControl(id, HandleUtility.DistanceToCircle(handlePos + offset, handleSize * .5f));
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

						var plane		= new Plane(Handles.matrix.MultiplyVector(handleDir), Handles.matrix.MultiplyPoint(handlePos));
						var mouseRay	= HandleUtility.GUIPointToWorldRay(evt.mousePosition);
						var dist		= 0.0f;
						plane.Raycast(mouseRay, out dist);

						GUIUtility.hotControl = GUIUtility.keyboardControl = id;
						s_CurrentMousePosition = evt.mousePosition;
						s_StartPosition = handlePos;

						var localMousePoint		= Handles.inverseMatrix.MultiplyPoint(mouseRay.GetPoint(dist));
						var clickOffset			= localMousePoint - handlePos;
						s_StartPlaneOffset.x = Vector3.Dot(clickOffset, slideDir1);
						s_StartPlaneOffset.y = Vector3.Dot(clickOffset, slideDir2);

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
						var worldPosition	= Handles.matrix.MultiplyPoint(handlePos);
						var worldSlideDir1	= Handles.matrix.MultiplyVector(slideDir1).normalized;
						var worldSlideDir2	= Handles.matrix.MultiplyVector(slideDir2).normalized;
						
						var mouseRay		= HandleUtility.GUIPointToWorldRay(s_CurrentMousePosition);
						var plane			= new Plane(worldPosition, worldPosition + worldSlideDir1, worldPosition + worldSlideDir2);
						var dist = 0.0f;
						if (plane.Raycast(mouseRay, out dist))
						{
							var hitpos = Handles.inverseMatrix.MultiplyPoint(mouseRay.GetPoint(dist));
							
							deltaDistanceAlongDirections.x = HandleUtility.PointOnLineParameter(hitpos, s_StartPosition, slideDir1);
							deltaDistanceAlongDirections.y = HandleUtility.PointOnLineParameter(hitpos, s_StartPosition, slideDir2);
							deltaDistanceAlongDirections -= s_StartPlaneOffset;
							
							GUI.changed = true;
						}
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
					if (capFunction == null)
						break;
					
					var originalColor = Handles.color;
					if (id == GUIUtility.keyboardControl)
					{
						Handles.color = Handles.selectedColor;
					} else
					if (CSGHandles.disabled)
						Handles.color = Color.Lerp(originalColor, Handles.secondaryColor, 0.75f);

					capFunction(id, position, rotation, handleSize, EventType.Repaint);

					Handles.color = originalColor;
					break;
				}
			}
			return deltaDistanceAlongDirections;
		}
	}
}
