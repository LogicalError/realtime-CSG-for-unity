using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace RealtimeCSG
{
    internal class CSGKeysPreferenceWindow 
	{
		internal class LocalStyles
		{
			public GUIStyle sectionScrollView = "PreferencesSectionBox";
			public GUIStyle settingsBoxTitle = "OL Title";
			public GUIStyle settingsBox = "OL Box";
			public GUIStyle errorLabel = "WordWrappedLabel";
			public GUIStyle sectionElement = "PreferencesSection";
			public GUIStyle evenRow = "CN EntryBackEven";
			public GUIStyle oddRow = "CN EntryBackOdd";
			//public GUIStyle selected = "ServerUpdateChangesetOn";
			public GUIStyle keysElement = "PreferencesKeysElement";
			public GUIStyle warningIcon = "CN EntryWarn";
			public GUIStyle sectionHeader = new GUIStyle(EditorStyles.largeLabel);
			public GUIStyle cacheFolderLocation = new GUIStyle(GUI.skin.label);
			public GUIStyle redTextBox;

			public LocalStyles()
			{
				sectionScrollView = new GUIStyle(sectionScrollView);
				sectionScrollView.overflow.bottom += 1;

				sectionHeader.fontStyle = FontStyle.Bold;
				sectionHeader.fontSize = 18;
				sectionHeader.margin.top = 10;
				sectionHeader.margin.left += 1;
				if (!EditorGUIUtility.isProSkin)
					sectionHeader.normal.textColor = new Color(0.4f, 0.4f, 0.4f, 1.0f);
				else
					sectionHeader.normal.textColor = new Color(0.7f, 0.7f, 0.7f, 1.0f);

				redTextBox = new GUIStyle(GUI.skin.textField);
				redTextBox.normal.textColor = Color.red;

				cacheFolderLocation.wordWrap = true;
			}
		}

		static Vector2 keyNamesScrollPos = Vector2.zero;
		static Vector2 keySettingsScrollPos = Vector2.zero;
		static KeyPref m_SelectedKey = null;
		static int m_SelectedKeyIndex = -1;
//		static bool foundSelectedKey = false;
		static LocalStyles constants = null;

		public class KeyPref
		{
			public string name;
			public KeyEvent keyEvent;
		}

		static KeyPref[]			keyArray	= null; 
		static KeyCodeWithModifier	newKey		= new KeyCodeWithModifier();

		public static KeyPref[] GetKeys()
		{
			var prefs = new List<KeyPref>();
			var type = typeof(Keys);
			var fields = type.GetFields(BindingFlags.Static | BindingFlags.Public);
			foreach (var field in fields)
			{
				if (field.FieldType == typeof(KeyEvent))
				{
					var pref = new KeyPref();

					var attribute = field.GetCustomAttributes(typeof(KeyDescription), false).FirstOrDefault() as KeyDescription;

					if (attribute != null)
						pref.name = attribute.Name;
					else
						pref.name = ObjectNames.NicifyVariableName(field.Name);
					 
					pref.keyEvent = (KeyEvent)field.GetValue(null);
					prefs.Add(pref);
				}
			}
			return prefs.ToArray();
		}

		static bool checkBounds = false;
		static int s_KeysControlHash = "KeysControlHash".GetHashCode();
		private static int s_KeyEventFieldHash = "KeyEventField".GetHashCode();
		private static int bKeyEventActive = -1;

		static void DoKeyEventField(Rect position, int index, ref KeyCodeWithModifier key, GUIStyle style)
		{
			int id = GUIUtility.GetControlID(s_KeyEventFieldHash + index, FocusType.Passive, position);
			Event evt = Event.current;
			switch (evt.GetTypeForControl(id))
			{
				case EventType.MouseDown:
				{
					// If the mouse is inside the button, we say that we're the hot control
					if (position.Contains(evt.mousePosition))
					{
						GUIUtility.hotControl = id;
						evt.Use();
						if (bKeyEventActive == id)
							// cancel
						{
							bKeyEventActive = -1;
						}
						else
						{
							bKeyEventActive = id;
						}
					}
					return;
				}
				case EventType.MouseUp:
				{
					if (GUIUtility.hotControl == id)
					{
						GUIUtility.hotControl = id;

						// If we got the mousedown, the mouseup is ours as well
						// (no matter if the click was in the button or not)
						evt.Use();
					}
					return;
				}
				case EventType.MouseDrag:
				{
					if (GUIUtility.hotControl == id)
					{
						evt.Use();
					}
					break;
				}
				case EventType.Repaint:
				{
					if (bKeyEventActive == id)
					{
						style.Draw(position, defaultKeyContent, id);
					} else
					{
						string str = KeyEvent.CodeToString(key.keyCode);
						style.Draw(position, new GUIContent(str), id);
					}
					break;
				}
				case EventType.KeyDown:
				{
					if ((GUIUtility.hotControl == id) && bKeyEventActive == id)
					{
						// ignore presses of just modifier keys
						if (evt.character == '\0')
						{
							if (evt.alt &&
							    (evt.keyCode == KeyCode.AltGr || evt.keyCode == KeyCode.LeftAlt || evt.keyCode == KeyCode.RightAlt) ||
							    evt.control && (evt.keyCode == KeyCode.LeftControl || evt.keyCode == KeyCode.RightControl) ||
							    evt.command &&
							    (evt.keyCode == KeyCode.LeftApple || evt.keyCode == KeyCode.RightApple || evt.keyCode == KeyCode.LeftWindows ||
							     evt.keyCode == KeyCode.RightWindows) ||
							    evt.shift &&
							    (evt.keyCode == KeyCode.LeftShift || evt.keyCode == KeyCode.RightShift || (int) evt.keyCode == 0))
							{
								return;
							}
						}
						bKeyEventActive = -1;
						GUI.changed = true;
						key.keyCode = evt.keyCode;
						key.modifiers = (evt.command ? EventModifiers.Command : 0)
									  | (evt.alt     ? EventModifiers.Alt : 0)
									  | (evt.shift   ? EventModifiers.Shift : 0)
									  | (evt.control ? EventModifiers.Control : 0);
						GUIUtility.hotControl = 0;
						evt.Use();
						return;
					}
					break;
				}
			}
		} 

		static GUIContent defaultKeyContent = new GUIContent("[Please press a key]");
		internal static void KeyEventField(Rect position, int index, ref KeyCodeWithModifier key, GUIStyle style)
		{
			DoKeyEventField(position, index, ref key, style);
		}

		internal static void KeyEventField(int index, ref KeyCodeWithModifier key, GUIStyle style, params GUILayoutOption[] options)
		{
			Rect r = GUILayoutUtility.GetRect(defaultKeyContent, style, options);
			KeyEventField(r, index, ref key, style);
		}

		static bool UniqueKey(ref KeyCodeWithModifier keyPref, int index)
		{
			for (int i = 0; i < keyArray.Length; i++)
			{
				var allKeys = keyArray[i].keyEvent.keys;
				for (int j = 0; j < allKeys.Length; j++)
				{
					if (i == m_SelectedKeyIndex && j == index)
						continue;

					if (allKeys[j].keyCode == keyPref.keyCode &&
						allKeys[j].modifiers == keyPref.modifiers)
					{
						return false;
					}
				}
			}
			return true;
		}

		static bool PreferenceKeyItem(ref KeyCodeWithModifier keyPref, int index = -1)
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label("Key:");
			
			if (keyPref.keyCode != KeyCode.None && !UniqueKey(ref keyPref, index))
				KeyEventField(index, ref keyPref, constants.redTextBox);
			else
				KeyEventField(index, ref keyPref, GUI.skin.textField);
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Modifiers:");
			GUILayout.BeginVertical();

			EventModifiers modifier = keyPref.modifiers;

			bool modifier_command	= (modifier & EventModifiers.Command) != 0;
			bool modifier_alt		= (modifier & EventModifiers.Alt) != 0;
			bool modifier_shift		= (modifier & EventModifiers.Shift) != 0;
			bool modifier_control	= (modifier & EventModifiers.Control) != 0;

			if (Application.platform == RuntimePlatform.OSXEditor)
				modifier_command = GUILayout.Toggle(modifier_command, "Command");
			modifier_control = GUILayout.Toggle(modifier_control, "Control");
			modifier_shift = GUILayout.Toggle(modifier_shift, "Shift");
			modifier_alt = GUILayout.Toggle(modifier_alt, "Alt");

			var key_is_no_longer_valid = (index != -1 &&
			                              (GUILayout.Button("Remove") ||
			                               keyPref.keyCode == KeyCode.None));
			 
			GUILayout.EndVertical();
			GUILayout.EndHorizontal();

			if (GUI.changed && key_is_no_longer_valid)
			{
				keyPref.modifiers = (modifier_command ? EventModifiers.Command : 0)
								  | (modifier_alt ? EventModifiers.Alt : 0)
								  | (modifier_shift ? EventModifiers.Shift : 0)
								  | (modifier_control ? EventModifiers.Control : 0);
			}
			return key_is_no_longer_valid;
		}

		static bool PreferenceKey(ref KeyCodeWithModifier[] events, int index)
		{
			var keyPref = events[index];
			if (PreferenceKeyItem(ref keyPref, index))
			{
				ArrayUtility.RemoveAt(ref events, index);
				return false;
			} else
			if (GUI.changed)
			{
				events[index] = keyPref;
			}
			return true;
		}

		static void RevertKeys()
		{
			if (keyArray == null)
				keyArray = GetKeys(); 
			
			for (int i = 0; i < keyArray.Length; i++)
			{
				keyArray[i].keyEvent.keys = keyArray[i].keyEvent.defaultKeys.ToList().ToArray();
			}
		}

		public static void ReadKeys()
		{
			if (keyArray == null)
				keyArray = GetKeys();

			for (int i = 0; i < keyArray.Length; i++)
			{
				keyArray[i].keyEvent.keys = keyArray[i].keyEvent.defaultKeys.ToList().ToArray();
			}
			 
			for (int i = 0; i < keyArray.Length; i++)
			{
				var key_name	= "KEY:" + keyArray[i].name;
				var key_value	= EditorPrefs.GetString(key_name, null);

				if (string.IsNullOrEmpty(key_value))
				{
					keyArray[i].keyEvent.keys = keyArray[i].keyEvent.defaultKeys.ToList().ToArray();
					continue;
				}
				try
				{
					var key_strings = key_value.Split(new[] {'|'}, StringSplitOptions.RemoveEmptyEntries);
					keyArray[i].keyEvent.keys = new KeyCodeWithModifier[key_strings.Length];
					for (int j = 0; j < key_strings.Length; j++)
					{
						var key_string	= key_strings[j].Split(':');
						if (key_string.Length == 0)
							continue;
						var keyCode		= (KeyCode) Enum.Parse(typeof(KeyCode), key_string[0]);
						int intModifier;
						var modifiers = EventModifiers.None;
						if (key_string.Length > 0 && Int32.TryParse(key_string[1], out intModifier))
							modifiers	= (EventModifiers)intModifier;
						keyArray[i].keyEvent.keys[j].keyCode = keyCode;
						keyArray[i].keyEvent.keys[j].modifiers = modifiers;
					}
				}
				catch
				{
					keyArray[i].keyEvent.keys = keyArray[i].keyEvent.defaultKeys.ToList().ToArray();
				}
			}
		}

		static void StoreKeys()
		{
			for (int i = 0; i < keyArray.Length; i++)
			{
				var key_name	= "KEY:" + keyArray[i].name; 
				var key_builder = new StringBuilder();
				var keys = keyArray[i].keyEvent.keys;
				for (int j = 0; j < keys.Length; j++)
				{
					var key = keys[j];
					key_builder.Append(key.keyCode.ToString());
					key_builder.Append(':');
					key_builder.Append((int)key.modifiers);
					key_builder.Append('|');
				}
				EditorPrefs.SetString(key_name, key_builder.ToString()); 
			}
		}

#if !UNITY_2018_4_OR_NEWER && !UNITY_2019_1_OR_NEWER
        [PreferenceItem("CSG Keys")]
#endif
        public static void PreferenceWindow()
		{
			if (constants == null)
			{
				constants = new LocalStyles();
			}
			if (keyArray == null) 
			{ 
				keyArray = GetKeys();
				ReadKeys();
			}
			 
			int id = GUIUtility.GetControlID(s_KeysControlHash, FocusType.Keyboard);

			KeyPref prevKey = null;
			KeyPref nextKey = null;
			bool foundSelectedKey = false;


			var width = Mathf.Min(Mathf.Max(170f, EditorGUIUtility.currentViewWidth - 600), 400);  

			GUILayout.BeginHorizontal();
			GUILayout.BeginVertical(GUILayout.MaxWidth(width));
			GUILayout.Label("Actions", constants.settingsBoxTitle, GUILayout.ExpandWidth(true));
			Rect selectedRect = default(Rect);
			keyNamesScrollPos = GUILayout.BeginScrollView(keyNamesScrollPos);//, constants.settingsBox);
			{
				for (int i = 0; i < keyArray.Length; i++)
				{
					var keyPref = keyArray[i];
					if (!foundSelectedKey)
					{
						if (keyPref == m_SelectedKey)
						{
							foundSelectedKey = true;
						} else
						{
							prevKey = keyPref;
						}
					} else
					{
						if (nextKey == null) nextKey = keyPref;
					}

					EditorGUI.BeginChangeCheck();
					if (GUILayout.Toggle(keyPref == m_SelectedKey, keyPref.name, constants.keysElement))
					{
						if (m_SelectedKey != keyPref)
						{
							checkBounds = true;
						}
						m_SelectedKeyIndex = i;
						m_SelectedKey = keyPref;
						newKey = new KeyCodeWithModifier ();
						if (Event.current.type == EventType.Repaint)
							selectedRect = GUILayoutUtility.GetLastRect();
					}
					if (EditorGUI.EndChangeCheck())
						GUIUtility.keyboardControl = id;
				}
			}
			GUILayout.EndScrollView();
			GUILayout.EndVertical();
			if (Event.current.type == EventType.Repaint && checkBounds)
			{
				checkBounds = false;
				Rect scrollViewRect = GUILayoutUtility.GetLastRect();
				scrollViewRect.position = Vector2.zero;
				scrollViewRect.position += keyNamesScrollPos;
				scrollViewRect.yMax -= 34;
				
				if (selectedRect.yMax > scrollViewRect.yMax)
				{
					keyNamesScrollPos.y = selectedRect.yMax - scrollViewRect.height;
					HandleUtility.Repaint ();
				}
				if (selectedRect.yMin < scrollViewRect.yMin)
				{
					keyNamesScrollPos.y = selectedRect.yMin;
					HandleUtility.Repaint();
				}
				if (keyNamesScrollPos.y < 0)
					keyNamesScrollPos.y = 0;
			}

			GUILayout.Space(10.0f);

			GUILayout.BeginVertical();
			keySettingsScrollPos = GUILayout.BeginScrollView(keySettingsScrollPos);

			if (m_SelectedKey != null)
			{
				GUI.changed = false;

				var allKeys = m_SelectedKey.keyEvent.keys;

				for (int i = 0; i < allKeys.Length; i++)
				{
					PreferenceKey(ref allKeys, i);
				}

				PreferenceKeyItem(ref newKey); 
				if (newKey.keyCode != KeyCode.None)
					ArrayUtility.Add(ref allKeys, newKey);

				m_SelectedKey.keyEvent.keys = allKeys;


				if (GUI.changed)
				{
					StoreKeys();
				} else
				{
					if (GUIUtility.keyboardControl == id && Event.current.type == EventType.KeyDown)
					{
						switch (Event.current.keyCode) 
						{
							case KeyCode.UpArrow:
							if (prevKey != null && prevKey != m_SelectedKey)
							{
								m_SelectedKey = prevKey;
								checkBounds = true;
								//m_ValidKeyChange = true;
							}
							Event.current.Use();
							break;

							case KeyCode.DownArrow:
							if (nextKey != null && nextKey != m_SelectedKey)
							{
								m_SelectedKey = nextKey;
								checkBounds = true;
								//m_ValidKeyChange = true;
							}
							Event.current.Use();
							break;
						}
					}
				}

			}

			GUILayout.EndScrollView();
			GUILayout.EndVertical();
			GUILayout.Space(10f);

			GUILayout.EndHorizontal();
			GUILayout.Space(5f);

			if (GUILayout.Button("Use Defaults", GUILayout.Width(120))) 
			{
				RevertKeys();
				StoreKeys();
			}
		}
	}
}
