using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using InternalRealtimeCSG;

namespace RealtimeCSG
{

	internal static class CSG_EditorGUIUtility
	{
		public static bool PassThroughButton(bool passThrough, bool mixedValues)
		{
			CSG_GUIStyleUtility.InitStyles();

			var rcsgSkin = CSG_GUIStyleUtility.Skin;
			var oldColor = GUI.color;
			GUI.color = Color.white;

			bool pressed = false;
			GUILayout.BeginVertical();
			{
				GUIContent content;
				GUIStyle style;
				if (!mixedValues && GUI.enabled && passThrough)
				{
					content = rcsgSkin.passThroughOn;
					style = CSG_GUIStyleUtility.selectedIconLabelStyle;
				} else
				{
					content = rcsgSkin.passThrough;
					style = CSG_GUIStyleUtility.unselectedIconLabelStyle;
				}
				if (GUILayout.Button(content, style))
				{
					pressed = true;
				}
				TooltipUtility.SetToolTip(CSG_GUIStyleUtility.passThroughTooltip);
			}
			GUILayout.EndVertical();

			GUI.color = oldColor;
			return pressed;
		}

		public static Foundation.CSGOperationType ChooseOperation(Foundation.CSGOperationType operation, bool mixedValues)
		{
			CSG_GUIStyleUtility.InitStyles();

			var rcsgSkin = CSG_GUIStyleUtility.Skin;
			if (rcsgSkin == null)
				return operation;

			var oldColor = GUI.color;
			GUI.color = Color.white;

			GUILayout.BeginVertical();
			try
			{
				GUIContent content;
				GUIStyle style;
				bool have_selection = !mixedValues && GUI.enabled;
				for (int i = 0; i < CSG_GUIStyleUtility.operationTypeCount; i++)
				{
					if (!have_selection || (int)operation != i)
					{
						content = rcsgSkin.operationNames[i];
						style = CSG_GUIStyleUtility.unselectedIconLabelStyle;
					} else
					{
						content = rcsgSkin.operationNamesOn[i];
						style = CSG_GUIStyleUtility.selectedIconLabelStyle;
					}
					if (content == null || style == null)
						continue;
					if (GUILayout.Button(content, style))
					{
						operation = (Foundation.CSGOperationType)i;
						GUI.changed = true;
					}
					TooltipUtility.SetToolTip(CSG_GUIStyleUtility.operationTooltip[i]);
				}
			}
			finally
			{
				GUILayout.EndVertical();
			}

			GUI.color = oldColor;
			return operation;
		}

		static void CalcSize(ref Rect[] rects, out Rect bounds, out int xCount, GUIContent[] contents, float yOffset, float areaWidth = -1)
		{
			if (areaWidth <= 0)
				areaWidth = EditorGUIUtility.currentViewWidth;
			
			var position	= new Rect();
			if (rects == null ||
				rects.Length != contents.Length)
				rects		= new Rect[contents.Length];
			
			{
				var skin		= GUI.skin;
				var buttonSkin	= skin.button;

				var textWidth = buttonSkin.CalcSize(contents[0]).x;
				for (var i = 1; i < contents.Length; i++)
				{
					var width = buttonSkin.CalcSize(contents[i]).x;
					if (width > textWidth)
						textWidth = width;
				}

				var margin = buttonSkin.margin;
				var padding = buttonSkin.padding;
				var paddingWidth = padding.left + padding.right;
				var minButtonWidth = textWidth + paddingWidth + margin.horizontal;
				var screenWidth = areaWidth - margin.horizontal;
				var countValue = Mathf.Clamp((screenWidth / minButtonWidth), 1, contents.Length);
				xCount = Mathf.FloorToInt(countValue);

				var realButtonWidth = (float)(screenWidth / xCount);
				if (xCount == contents.Length)
					realButtonWidth = (screenWidth / countValue);
				
				
				position.x = 0;
				position.y = yOffset;
				position.width = realButtonWidth;
				position.height = 15;

				bounds = new Rect();
				bounds.width = areaWidth;

				xCount--;
				int count = 0;
				while (count < contents.Length)
				{
					position.y ++;
					position.x = 2;
					for (int x = 0; x <= xCount; x++)
					{
						position.x ++;

						rects[count] = position;
								
						position.x += realButtonWidth - 1;
						
						count++;
						if (count >= contents.Length)
							break;
					}
					position.y += 16;
				}
				
				bounds.height = (position.y - yOffset);
			}
		}

		public static int ToolbarWrapped(int selected, ref Rect[] rects, out Rect bounds, GUIContent[] contents, ToolTip[] tooltips = null, float yOffset = 0, float areaWidth = -1)
		{
			if (areaWidth <= 0)
				areaWidth = EditorGUIUtility.currentViewWidth;

			int xCount;
			CalcSize(ref rects, out bounds, out xCount, contents, yOffset, areaWidth);
			
			var leftStyle	= EditorStyles.miniButtonLeft;
			var middleStyle = EditorStyles.miniButtonMid;
			var rightStyle	= EditorStyles.miniButtonRight;
			var singleStyle = EditorStyles.miniButton;

			
			int count = 0;
			while (count < contents.Length)
			{
				var last = Mathf.Min(xCount, contents.Length - 1 - count);
				for (int x = 0; x <= xCount; x++)
				{
					GUIStyle style = (x > 0) ? ((x < last) ? middleStyle : rightStyle) : ((x < last) ? leftStyle : singleStyle);
						
					if (GUI.Toggle(rects[count], selected == count, contents[count], style))//, buttonWidthLayout))
					{
						if (selected != count)
						{
							selected = count;
							GUI.changed = true;
						}
					}
						
					if (tooltips != null)
						TooltipUtility.SetToolTip(tooltips[count], rects[count]);
					count++;
					if (count >= contents.Length)
						break;
				}
			}
			
			return selected;
		}

		internal const int materialSmallSize = 48;
		internal const int materialLargeSize = 100;
		[NonSerialized]
		private static MaterialEditor materialEditor = null;
		private static readonly GUILayoutOption materialSmallWidth = GUILayout.Width(materialSmallSize);
		private static readonly GUILayoutOption materialSmallHeight = GUILayout.Height(materialSmallSize);
		private static readonly GUILayoutOption materialLargeWidth = GUILayout.Width(materialLargeSize);
		private static readonly GUILayoutOption materialLargeHeight = GUILayout.Height(materialLargeSize);


		static Material GetDragMaterial()
		{
			if (DragAndDrop.objectReferences != null &&
				DragAndDrop.objectReferences.Length > 0)
			{
				var dragMaterials = new List<Material>();
				foreach (var obj in DragAndDrop.objectReferences)
				{
					var dragMaterial = obj as Material;
					if (dragMaterial == null)
						continue;
					dragMaterials.Add(dragMaterial);
				}
				if (dragMaterials.Count == 1)
					return dragMaterials[0];
			}
			return null;
		}

		public static Material MaterialImage(Material material, bool small = true)
		{
			var showMixedValue = EditorGUI.showMixedValue;
			EditorGUI.showMixedValue = false;

			var width  = small ? materialSmallWidth : materialLargeWidth;
			var height = small ? materialSmallHeight : materialLargeHeight;

			GUILayout.BeginHorizontal(CSG_GUIStyleUtility.emptyMaterialStyle, width, height);
			{
				//if (!materialEditor || prevMaterial != material)
				{
					var editor = materialEditor as Editor;
					Editor.CreateCachedEditor(material, typeof(MaterialEditor), ref editor);
					materialEditor = editor as MaterialEditor;
					//prevMaterial = material; 
				}

				if (materialEditor)
				{
					var rect = GUILayoutUtility.GetRect(small ? materialSmallSize : materialLargeSize,
														small ? materialSmallSize : materialLargeSize);
					EditorGUI.showMixedValue = showMixedValue;
					materialEditor.OnPreviewGUI(rect, GUIStyle.none);
					EditorGUI.showMixedValue = false;
				}
				else
				{
					GUILayout.Box(new GUIContent(), CSG_GUIStyleUtility.emptyMaterialStyle, width, height);
				}
			}
			GUILayout.EndHorizontal();

			var currentArea = GUILayoutUtility.GetLastRect();
			var currentPoint = Event.current.mousePosition;
			if (currentArea.Contains(currentPoint))
			{
				if (Event.current.type == EventType.DragUpdated &&
					GetDragMaterial() != null)
				{
					DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
					Event.current.Use();
				}
				if (Event.current.type == EventType.DragPerform)
				{
					var new_material = GetDragMaterial();
					if (new_material != null)
					{
						material = new_material;
						GUI.changed = true;
						Event.current.Use();
						return material;
					}
				}
			}
			return material;
		}

		public static void RepaintAll()
		{
			SceneView.RepaintAll();
		}


		static readonly GUIContent VectorXContent = new GUIContent("X");
		static readonly GUIContent VectorYContent = new GUIContent("Y");
		static readonly GUIContent VectorZContent = new GUIContent("Z");

		static readonly float Width22Value = 22;
		static readonly GUILayoutOption Width22 = GUILayout.Width(Width22Value);

		public static float DistanceField(GUIContent label, float value, GUILayoutOption[] options = null)
		{
			bool modified = false;
			var distanceUnit = RealtimeCSG.CSGSettings.DistanceUnit;
			var nextUnit = Units.CycleToNextUnit(distanceUnit);
			var unitText = Units.GetUnitGUIContent(distanceUnit);

			float realValue = value;
			EditorGUI.BeginChangeCheck();
			{
				value = Units.DistanceUnitToUnity(distanceUnit, EditorGUILayout.DoubleField(label, Units.UnityToDistanceUnit(distanceUnit, value), options));
			}
			if (EditorGUI.EndChangeCheck())
			{
				realValue = value; // don't want to introduce math errors unless we actually modify something
				modified = true;
			}
			if (GUILayout.Button(unitText, EditorStyles.miniLabel, Width22))
			{
				distanceUnit = nextUnit;
				RealtimeCSG.CSGSettings.DistanceUnit = distanceUnit;
				RealtimeCSG.CSGSettings.UpdateSnapSettings();
				RealtimeCSG.CSGSettings.Save();
				RepaintAll();
			}
			GUI.changed = modified;
			return realValue;
		}

		static GUIContent angleUnitLabel = new GUIContent("°");

		public static Vector3 EulerDegreeField(Vector3 value, GUILayoutOption[] options = null)
		{
			bool modified = false;
			const float vectorLabelWidth = 12;

			var realValue = value;
			var originalLabelWidth = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = vectorLabelWidth;
			GUILayout.BeginHorizontal();
			{
				EditorGUI.BeginChangeCheck();
				{
					value.x = EditorGUILayout.FloatField(VectorXContent, value.x, options);
				}
				if (EditorGUI.EndChangeCheck())
				{
					realValue.x = value.x; // don't want to introduce math errors unless we actually modify something
					modified = true;
				}
				EditorGUI.BeginChangeCheck();
				{
					value.y = EditorGUILayout.FloatField(VectorYContent, value.y, options);
				}
				if (EditorGUI.EndChangeCheck())
				{
					realValue.y = value.y; // don't want to introduce math errors unless we actually modify something
					modified = true;
				}
				EditorGUI.BeginChangeCheck();
				{
					value.z = EditorGUILayout.FloatField(VectorZContent, value.z, options);
				}
				if (EditorGUI.EndChangeCheck())
				{
					realValue.z = value.z; // don't want to introduce math errors unless we actually modify something
					modified = true;
				}
				GUILayout.Label(angleUnitLabel, EditorStyles.miniLabel, Width22);
			}
			GUILayout.EndHorizontal();
			EditorGUIUtility.labelWidth = originalLabelWidth;
			GUI.changed = modified;
			return realValue;
		}

		public static float DegreeField(GUIContent label, float value, GUILayoutOption[] options = null)
		{
			bool modified = false;

			float realValue = value;
			EditorGUI.BeginChangeCheck();
			{
				GUILayout.BeginHorizontal();
				{
					value = EditorGUILayout.FloatField(label, value, options);
					GUILayout.Label(angleUnitLabel, EditorStyles.miniLabel, Width22);
				}
				GUILayout.EndHorizontal();
			}
			if (EditorGUI.EndChangeCheck())
			{
				realValue = value; // don't want to introduce math errors unless we actually modify something
				modified = true;
			}
			GUI.changed = modified;
			return realValue;
		}

		public static float DegreeField(float value, GUILayoutOption[] options = null)
		{
			return DegreeField(GUIContent.none, value, options);
		}

		public static Vector3 DistanceVector3Field(Vector3 value, bool multiLine, GUILayoutOption[] options = null)
		{
			var distanceUnit = RealtimeCSG.CSGSettings.DistanceUnit;
			var nextUnit	 = Units.CycleToNextUnit(distanceUnit);
			var unitText	 = Units.GetUnitGUIContent(distanceUnit);

			bool modified = false;
			bool clickedUnitButton = false;

			var areaWidth = EditorGUIUtility.currentViewWidth;

			const float minWidth = 65;
			const float vectorLabelWidth = 12;

			var allWidth = (12 * 3) + (Width22Value * 3) + (minWidth * 3);

			Vector3 realValue = value;
			multiLine = multiLine || (allWidth >= areaWidth);
			if (multiLine)
				GUILayout.BeginVertical();
			var originalLabelWidth = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = vectorLabelWidth;
			GUILayout.BeginHorizontal();
			{
				EditorGUI.BeginChangeCheck();
				{
					value.x = Units.DistanceUnitToUnity(distanceUnit, EditorGUILayout.DoubleField(VectorXContent, Units.UnityToDistanceUnit(distanceUnit, value.x), options));
				}
				if (EditorGUI.EndChangeCheck())
				{
					realValue.x = value.x; // don't want to introduce math errors unless we actually modify something
					modified = true;
				}
				if (multiLine)
					clickedUnitButton = GUILayout.Button(unitText, EditorStyles.miniLabel, Width22) || clickedUnitButton;
				if (multiLine)
				{
					GUILayout.EndHorizontal();
					GUILayout.BeginHorizontal();
				}
				EditorGUI.BeginChangeCheck();
				{
					value.y = Units.DistanceUnitToUnity(distanceUnit, EditorGUILayout.DoubleField(VectorYContent, Units.UnityToDistanceUnit(distanceUnit, value.y), options));
				}
				if (EditorGUI.EndChangeCheck())
				{
					realValue.y = value.y; // don't want to introduce math errors unless we actually modify something
					modified = true;
				}
				if (multiLine)
					clickedUnitButton = GUILayout.Button(unitText, EditorStyles.miniLabel, Width22) || clickedUnitButton;
				if (multiLine)
				{
					GUILayout.EndHorizontal();
					GUILayout.BeginHorizontal();
				}
				EditorGUI.BeginChangeCheck();
				{
					value.z = Units.DistanceUnitToUnity(distanceUnit, EditorGUILayout.DoubleField(VectorZContent, Units.UnityToDistanceUnit(distanceUnit, value.z), options));
				}
				if (EditorGUI.EndChangeCheck())
				{
					realValue.z = value.z; // don't want to introduce math errors unless we actually modify something
					modified = true;
				}
				clickedUnitButton = GUILayout.Button(unitText, EditorStyles.miniLabel, Width22) || clickedUnitButton;
			}
			GUILayout.EndHorizontal();
			EditorGUIUtility.labelWidth = originalLabelWidth;
			if (multiLine)
				GUILayout.EndVertical();
			if (clickedUnitButton)
			{
				distanceUnit = nextUnit;
				RealtimeCSG.CSGSettings.DistanceUnit = distanceUnit;
				RealtimeCSG.CSGSettings.UpdateSnapSettings();
				RealtimeCSG.CSGSettings.Save();
				RepaintAll();
			}
			GUI.changed = modified;
			return realValue;
		}
	}
}
