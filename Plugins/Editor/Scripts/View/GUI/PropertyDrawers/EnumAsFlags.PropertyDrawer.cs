using UnityEngine;
using UnityEditor;

namespace RealtimeCSG
{
	[CustomPropertyDrawer(typeof(EnumAsFlagsAttribute))]
	public sealed class EnumAsFlagsPropertyDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			property.intValue = EditorGUI.MaskField(position, label, property.intValue, property.enumNames);
		}
	}
}
 