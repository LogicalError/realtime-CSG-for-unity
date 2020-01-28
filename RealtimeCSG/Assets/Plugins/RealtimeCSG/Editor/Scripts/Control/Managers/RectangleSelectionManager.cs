using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using InternalRealtimeCSG;
using RealtimeCSG.Components;

namespace RealtimeCSG
{
	internal sealed class RectangleSelectionManager
	{
		static Type			UnitySceneViewType;
		static Type			UnityRectSelectionType;
		static Type			UnityEnumSelectionType;

		static object		SelectionType_Additive;
		static object		SelectionType_Subtractive;
		static object		SelectionType_Normal;
			
		static FieldInfo	m_RectSelection_field;
		static FieldInfo	m_RectSelecting_field;
		static FieldInfo	s_RectSelectionID_field;
		static FieldInfo	m_SelectStartPoint_field;
		static FieldInfo	m_SelectMousePoint_field;
		static FieldInfo	m_SelectionStart_field;
		static FieldInfo	m_LastSelection_field;
		static FieldInfo	m_CurrentSelection_field;
		static MethodInfo	UpdateSelection_method;

		static bool         initialized			= false;
		static bool			reflectionSucceeded = false;

		static void InitReflectedData()
		{
			if (initialized)
				return;

			initialized			= true;
			reflectionSucceeded	= false;
			UnitySceneViewType	= typeof(SceneView);
			
			var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
			var types = new List<System.Type>();
			foreach(var assembly in assemblies)
			{
				try
				{
					types.AddRange(assembly.GetTypes());
				}
				catch { }
			}
			UnityRectSelectionType		= types.FirstOrDefault(t => t.FullName == "UnityEditor.RectSelection");
			UnityEnumSelectionType 		= types.FirstOrDefault(t => t.FullName == "UnityEditor.RectSelection+SelectionType");
			

			if (UnitySceneViewType != null)
			{
				m_RectSelection_field		= UnitySceneViewType.GetField("m_RectSelection", BindingFlags.NonPublic | BindingFlags.Instance);
			} else
			{
				m_RectSelection_field = null;
			}

			if (UnityRectSelectionType != null) 
			{
				m_RectSelecting_field		= UnityRectSelectionType.GetField("m_RectSelecting",	BindingFlags.NonPublic | BindingFlags.Instance);
				s_RectSelectionID_field		= UnityRectSelectionType.GetField("s_RectSelectionID",	BindingFlags.NonPublic | BindingFlags.Static);
				m_SelectStartPoint_field	= UnityRectSelectionType.GetField("m_SelectStartPoint",	BindingFlags.NonPublic | BindingFlags.Instance);
				m_SelectionStart_field		= UnityRectSelectionType.GetField("m_SelectionStart",	BindingFlags.NonPublic | BindingFlags.Instance);
				m_LastSelection_field		= UnityRectSelectionType.GetField("m_LastSelection",	BindingFlags.NonPublic | BindingFlags.Instance);
				m_CurrentSelection_field	= UnityRectSelectionType.GetField("m_CurrentSelection",	BindingFlags.NonPublic | BindingFlags.Instance);
				m_SelectMousePoint_field	= UnityRectSelectionType.GetField("m_SelectMousePoint",	BindingFlags.NonPublic | BindingFlags.Instance);
				
				if (UnityEnumSelectionType != null)
				{
					SelectionType_Additive		= Enum.Parse(UnityEnumSelectionType, "Additive");
					SelectionType_Subtractive	= Enum.Parse(UnityEnumSelectionType, "Subtractive");
					SelectionType_Normal		= Enum.Parse(UnityEnumSelectionType, "Normal");
			
					UpdateSelection_method		= UnityRectSelectionType.GetMethod("UpdateSelection", BindingFlags.NonPublic | BindingFlags.Static,
																					null,
																					new Type[] {
																						typeof(UnityEngine.Object[]),
																						typeof(UnityEngine.Object[]),
																						UnityEnumSelectionType,
																						typeof(bool)
																					},
																					null);
				}
			}

			reflectionSucceeded =	s_RectSelectionID_field  != null &&
									m_RectSelection_field    != null &&
									m_RectSelecting_field    != null &&
									m_SelectStartPoint_field != null &&
									m_SelectMousePoint_field != null &&
									UpdateSelection_method   != null;
		}

		
		static HashSet<GameObject>	rectFoundGameObjects = new HashSet<GameObject>();
		static Vector2				prevStartGUIPoint;
		static Vector2				prevMouseGUIPoint;
		static Vector2				prevStartScreenPoint;
		static Vector2				prevMouseScreenPoint;
		

		static bool		rectClickDown       = false;
		static bool		mouseDragged        = false;
		static Vector2	clickMousePosition  = MathConstants.zeroVector2;

		// Update rectangle selection using reflection
		// This is hacky & dangerous 
		// LOOK AWAY NOW!
		internal static void Update(SceneView sceneView)
		{
			InitReflectedData();
			if (!reflectionSucceeded)
			{
				prevStartGUIPoint = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
				prevMouseGUIPoint = prevStartGUIPoint;
				prevStartScreenPoint = MathConstants.zeroVector2;
				prevMouseScreenPoint = MathConstants.zeroVector2;
				rectFoundGameObjects.Clear();
				return;
			}

			var s_RectSelectionID_instance = (int)s_RectSelectionID_field.GetValue(null);

			// check if we're rect-selecting
			if (GUIUtility.hotControl == s_RectSelectionID_instance)
			{
				var typeForControl	= Event.current.GetTypeForControl(s_RectSelectionID_instance);
				if (typeForControl == EventType.Used ||
					Event.current.commandName == "ModifierKeysChanged")
				{
					// m_RectSelection field of SceneView
					var m_RectSelection_instance = m_RectSelection_field.GetValue(sceneView);

					// m_RectSelecting field of RectSelection instance
					var m_RectSelecting_instance = (bool)m_RectSelecting_field.GetValue(m_RectSelection_instance);
					if (m_RectSelecting_instance)
					{
						// m_SelectStartPoint of RectSelection instance
						var m_SelectStartPoint_instance = (Vector2)m_SelectStartPoint_field.GetValue(m_RectSelection_instance);

						// m_SelectMousePoint of RectSelection instance
						var m_SelectMousePoint_instance = (Vector2)m_SelectMousePoint_field.GetValue(m_RectSelection_instance);

						// determine if our frustum changed since the last time
						bool modified = false;
						bool needUpdate = false;
						if (prevStartGUIPoint != m_SelectStartPoint_instance)
						{
							prevStartGUIPoint = m_SelectStartPoint_instance;
							prevStartScreenPoint = Event.current.mousePosition;
							needUpdate = true;
						}
						if (prevMouseGUIPoint != m_SelectMousePoint_instance)
						{
							prevMouseGUIPoint = m_SelectMousePoint_instance;
							prevMouseScreenPoint = Event.current.mousePosition;
							needUpdate = true;
						}
						if (needUpdate)
						{
							var rect	= CameraUtility.PointsToRect(prevStartScreenPoint, prevMouseScreenPoint);
							if (rect.width > 3 && rect.height > 3)
							{ 
								var frustum = CameraUtility.GetCameraSubFrustumGUI(sceneView.camera, rect);
								
								// Find all the brushes (and it's gameObjects) that are in the frustum
								if (SceneQueryUtility.GetItemsInFrustum(frustum.Planes, 
																	  rectFoundGameObjects))
								{ 
									modified = true;
								} else
								{
									if (rectFoundGameObjects != null &&
										rectFoundGameObjects.Count > 0)
									{
										rectFoundGameObjects.Clear();
										modified = true;
									}
								}
							}
						}

						GameObject[] currentSelection = null;
						var m_LastSelection_instance	= (Dictionary<GameObject, bool>)m_LastSelection_field.GetValue(m_RectSelection_instance);
						var m_SelectionStart_instance	= (UnityEngine.Object[])m_SelectionStart_field.GetValue(m_RectSelection_instance);
						if (modified &&
							rectFoundGameObjects != null &&
							rectFoundGameObjects.Count > 0)
						{
							if (EditModeManager.ActiveTool == null)
							{
								if (EditModeManager.EditMode != ToolEditMode.Place ||
									EditModeManager.EditMode != ToolEditMode.Edit)
								{
									EditModeManager.EditMode = ToolEditMode.Place;
								}
							}

							foreach(var obj in rectFoundGameObjects)
							{
								// if it hasn't already been added, add the obj
								if (!m_LastSelection_instance.ContainsKey(obj))
								{
									m_LastSelection_instance.Add(obj, false);
								}


								// Remove models that we may have selected when we should be selecting it's brushes
								var model = obj.GetComponentInParent<CSGModel>();
								if (model != null)
								{
									var modelObj = model.gameObject;
									if (model != null &&
										modelObj != obj &&
										m_LastSelection_instance.ContainsKey(modelObj) &&
										!ArrayUtility.Contains(m_SelectionStart_instance, modelObj))
									{
										m_LastSelection_instance.Remove(modelObj);
										modified = true;
									}
								}
							}
							
							currentSelection = m_LastSelection_instance.Keys.ToArray();
							m_CurrentSelection_field.SetValue(m_RectSelection_instance, currentSelection);
						}
						for (int j = m_SelectionStart_instance.Length - 1; j >= 0; j--)
						{
							var obj = m_SelectionStart_instance[j] as GameObject;
							if (obj == null)
								continue;

							if (obj.GetComponent<GeneratedMeshInstance>() != null)
							{
								ArrayUtility.RemoveAt(ref m_SelectionStart_instance, j);
								m_LastSelection_instance.Remove(obj);
								m_SelectionStart_field.SetValue(m_RectSelection_instance, m_SelectionStart_instance);
								modified = true;
							}
						}

						if ((Event.current.commandName == "ModifierKeysChanged" || modified))
						{
							if (currentSelection == null || modified) { currentSelection = m_LastSelection_instance.Keys.ToArray(); }
							var foundObjects = currentSelection;

							for (int j = foundObjects.Length - 1; j >= 0; j--)
							{
								var obj = foundObjects[j];
								if (obj == null || obj.GetComponent<GeneratedMeshInstance>() != null)
								{
									ArrayUtility.RemoveAt(ref foundObjects, j);
									m_LastSelection_instance.Remove(obj);
									m_SelectionStart_field.SetValue(m_RectSelection_instance, m_SelectionStart_instance);
								}
							}


							var selectionTypeNormal = SelectionType_Normal;
							if (Event.current.shift) { selectionTypeNormal = SelectionType_Additive; } else
							if (EditorGUI.actionKey) { selectionTypeNormal = SelectionType_Subtractive; }

							// calling static method UpdateSelection of RectSelection 
							UpdateSelection_method.Invoke(null, 
								new object[] {
									m_SelectionStart_instance,
									foundObjects,
									selectionTypeNormal,
									m_RectSelecting_instance
								});
						}

					}
				}
			}
			if (GUIUtility.hotControl != s_RectSelectionID_instance)
			{
				prevStartGUIPoint = MathConstants.zeroVector2;
				prevMouseGUIPoint = MathConstants.zeroVector2;
				rectFoundGameObjects.Clear();
			}
			

			var eventType = Event.current.GetTypeForControl(s_RectSelectionID_instance);

			var hotControl = GUIUtility.hotControl;

			if (hotControl == s_RectSelectionID_instance &&
				EditModeManager.ActiveTool.IgnoreUnityRect)
			{
				hotControl = 0;
				GUIUtility.hotControl = 0;
			}
			
			switch (eventType)
			{
				case EventType.MouseDown:
				{
					rectClickDown = (Event.current.button == 0 && hotControl == s_RectSelectionID_instance);
					clickMousePosition = Event.current.mousePosition;
					mouseDragged = false;
					break;
				}
				case EventType.MouseUp:
				{
					rectClickDown = false;
					break;
				}
				case EventType.MouseMove:
				{
					rectClickDown = false;
					break;
				}
				case EventType.Used:
				{
					if (clickMousePosition != Event.current.mousePosition)
					{
						mouseDragged = true;
					}
					if (!mouseDragged && rectClickDown && 
						Event.current.button == 0)
					{
						// m_RectSelection field of SceneView
						var m_RectSelection_instance = m_RectSelection_field.GetValue(sceneView);

						var m_RectSelecting_instance = (bool)m_RectSelecting_field.GetValue(m_RectSelection_instance);
						if (!m_RectSelecting_instance)
						{
							// make sure GeneratedMeshes are not part of our selection
							if (Selection.gameObjects != null)
							{
								var selectedObjects = Selection.objects;
								var foundObjects = new List<UnityEngine.Object>();
								foreach (var obj in selectedObjects)
								{
									var component = obj as Component;
									var gameObject = obj as GameObject;
									var transform = obj as Transform;
									if (!(component && component.GetComponent<GeneratedMeshes>()) &&
										!(gameObject && gameObject.GetComponent<GeneratedMeshes>()) &&
										!(transform && transform.GetComponent<Transform>()))
										foundObjects.Add(obj);
								}
								if (foundObjects.Count != selectedObjects.Length)
								{
									Selection.objects = foundObjects.ToArray();
								}
							}
							
							SelectionUtility.DoSelectionClick(sceneView);
							Event.current.Use();
						}

					}
					rectClickDown = false;
					break;
				}


				case EventType.ValidateCommand:
				{
					if (Event.current.commandName == "SelectAll")
					{
						Event.current.Use();
						break;
					}
					if (Keys.HandleSceneValidate(EditModeManager.CurrentTool, true))
					{
						Event.current.Use();
						HandleUtility.Repaint();
					}				
					break; 
				}
				case EventType.ExecuteCommand:
				{
					if (Event.current.commandName == "SelectAll")
					{
						var transforms = new List<UnityEngine.Object>();
						for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
						{
							var scene = SceneManager.GetSceneAt(sceneIndex);
							foreach (var gameObject in scene.GetRootGameObjects())
							{
								foreach (var transform in gameObject.GetComponentsInChildren<Transform>())
								{
									if ((transform.hideFlags & (HideFlags.NotEditable | HideFlags.HideInHierarchy)) == (HideFlags.NotEditable | HideFlags.HideInHierarchy))
										continue;
									transforms.Add(transform.gameObject);
								}
							}
						}
						Selection.objects = transforms.ToArray();

						Event.current.Use();
						break;
					}
					break;
				}

				case EventType.KeyDown:
				{
					if (Keys.HandleSceneKeyDown(EditModeManager.CurrentTool, true))
					{
						Event.current.Use();
						HandleUtility.Repaint();
					}
					break;
				}

				case EventType.KeyUp:
				{
					if (Keys.HandleSceneKeyUp(EditModeManager.CurrentTool, true))
					{
						Event.current.Use();
						HandleUtility.Repaint();
					}
					break;
				}
			}			
		}
	}
}