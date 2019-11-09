using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace RealtimeCSG
{
	internal struct KeyCodeWithModifier
	{
		public KeyCodeWithModifier(KeyCode _code, EventModifiers _modifier = EventModifiers.None, bool _hold = false) { keyCode = _code; modifiers = _modifier; hold = _hold; }
		public KeyCode					keyCode;
		public EventModifiers			modifiers;
        public readonly bool            hold;
        public bool IsKeyPressed()
		{
			return (//EditorGUIUtility.editingTextField && 
					Event.current.keyCode == keyCode && (Event.current.modifiers & ~EventModifiers.FunctionKey) == modifiers);
		}
		
		public override string ToString()
		{
			var builder = new StringBuilder();
			if ((modifiers & EventModifiers.Command) > 0) { if (builder.Length > 0) builder.Append('+'); builder.Append("Command"); }
			if ((modifiers & EventModifiers.Control) > 0) { if (builder.Length > 0) builder.Append('+'); builder.Append("Control"); }
			if ((modifiers & EventModifiers.Alt    ) > 0) { if (builder.Length > 0) builder.Append('+'); builder.Append("Alt"); }
			if ((modifiers & EventModifiers.Shift  ) > 0) { if (builder.Length > 0) builder.Append('+'); builder.Append("Shift"); }
			if (builder.Length > 0) builder.Append('+');
			builder.Append(KeyEvent.CodeToString(keyCode));
            if (hold)
                builder.Append(" (hold)");
			return builder.ToString();
		}
	}

	// TODO: ability to connect "keyEvent" with delegate
	// TODO: ability to connect list of "keyevent"/"delegate" combo with controlid 

	internal class KeyEvent
    {
        public KeyEvent(KeyCode code) { keys = new KeyCodeWithModifier[1] { new KeyCodeWithModifier(code, EventModifiers.None, false) }; defaultKeys = keys.ToList().ToArray(); }
        public KeyEvent(KeyCode code, bool hold) { keys = new KeyCodeWithModifier[1] { new KeyCodeWithModifier(code, EventModifiers.None, hold) }; defaultKeys = keys.ToList().ToArray(); }
        public KeyEvent(KeyCode code, EventModifiers modifier) { keys = new KeyCodeWithModifier[1] { new KeyCodeWithModifier(code, modifier, false) }; defaultKeys = keys.ToList().ToArray(); }
        public KeyEvent(KeyCode code, EventModifiers modifier, bool hold) { keys = new KeyCodeWithModifier[1] { new KeyCodeWithModifier(code, modifier, hold) }; defaultKeys = keys.ToList().ToArray(); }

        public KeyEvent(params KeyCode[] keyCodes)
		{
			keys = new KeyCodeWithModifier[keyCodes.Length];
			for (int i = 0; i < keyCodes.Length; i++) keys[i] = new KeyCodeWithModifier(keyCodes[i], EventModifiers.None);
			defaultKeys = keys.ToList().ToArray();
		}
		public KeyEvent(params KeyCodeWithModifier[] _keys) { keys = _keys; defaultKeys = _keys.ToList().ToArray(); }
		public readonly KeyCodeWithModifier[] defaultKeys;
		public KeyCodeWithModifier[] keys;

		public bool IsEmpty()
		{
			return keys == null || keys.Length == 0;
		}

		public bool IsKeyPressed()
		{
			if (!RealtimeCSG.CSGSettings.EnableRealtimeCSG) 
				return false;
			for (int i = 0; i < keys.Length; i++)
			{
				if (keys[i].IsKeyPressed())
				{
					return true;
				}
			}
			return false;
		}

		public override string ToString()
		{
			if (keys.Length == 0)
				return string.Empty;
			if (keys.Length == 1)
				return keys[0].ToString();
			var builder = new StringBuilder();
			for (int i = 0; i < keys.Length; i++)
			{
				if (i > 0)
					builder.Append(" or ");
				builder.Append(keys[i].ToString());
			}
			return builder.ToString();
		}

		public static string CodeToString(KeyCode code)
		{
			switch (code)
			{
				case KeyCode.Exclaim: return "!";
				case KeyCode.DoubleQuote: return "\"";
				case KeyCode.Hash: return "#";
				case KeyCode.Dollar: return "$";
				case KeyCode.Ampersand: return "&";
				case KeyCode.Quote: return "\'";
				case KeyCode.LeftParen: return "(";
				case KeyCode.RightParen: return ")";
				case KeyCode.Asterisk: return "*";
				case KeyCode.Plus: return "+";
				case KeyCode.Comma: return ",";
				case KeyCode.Minus: return "-";
				case KeyCode.Period: return ".";
				case KeyCode.Slash: return "/";
				case KeyCode.Alpha0: return "0";
				case KeyCode.Alpha1: return "1";
				case KeyCode.Alpha2: return "2";
				case KeyCode.Alpha3: return "3";
				case KeyCode.Alpha4: return "4";
				case KeyCode.Alpha5: return "5";
				case KeyCode.Alpha6: return "6";
				case KeyCode.Alpha7: return "7";
				case KeyCode.Alpha8: return "8";
				case KeyCode.Alpha9: return "9";
				case KeyCode.Colon: return ":";
				case KeyCode.Semicolon: return ";";
				case KeyCode.Less: return "<";
				case KeyCode.Equals: return "=";
				case KeyCode.Greater: return ">";
				case KeyCode.Question: return "?";
				case KeyCode.At: return "@";
				case KeyCode.LeftBracket: return "[";
				case KeyCode.Backslash: return "\\";
				case KeyCode.RightBracket: return "]";
				case KeyCode.Caret: return "^";
				case KeyCode.Underscore: return "_";
				case KeyCode.BackQuote: return "`";
				case KeyCode.KeypadPeriod: return ("Keypad '.'");
				case KeyCode.KeypadDivide: return ("Keypad '/'");
				case KeyCode.KeypadMultiply: return ("Keypad '*'");
				case KeyCode.KeypadMinus: return ("Keypad '-'");
				case KeyCode.KeypadPlus: return ("Keypad '+'");
				case KeyCode.KeypadEquals: return ("Keypad '='");
				default: return ObjectNames.NicifyVariableName(code.ToString());
			}
		}
	}

	internal class KeyDescription : Attribute
	{
		public KeyDescription(string name, string description = null) { this.Name = name; }
		public string Name;
	}

}
