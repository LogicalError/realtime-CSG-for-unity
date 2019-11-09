using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealtimeCSG
{
	internal class ToolTip
	{
		public ToolTip(string _title, string _contents) { title = _title; contents = _contents; }
		public ToolTip(string _title, string _contents, KeyEvent _key) { title = _title; contents = _contents; key = _key; }
		public string   title;
		public string   contents;
		public KeyEvent key;

		public string TitleString()
		{
			return string.Format("<size=13><color=white><b>{0}</b></color></size>", title);
		}

		public string ContentsString()
		{
			return string.Format("<size=12><color=white>{0}</color></size>", contents);
		}

		public string KeyString()
		{
			if (key == null || key.IsEmpty())
				return string.Empty;

			return string.Format("<color=white><size=11><b>hotkey:</b> <i>{0}</i></size></color>", key.ToString());
		}

		public override string ToString()
		{
			return TitleString() + ContentsString() + KeyString();
		}
	}
}
