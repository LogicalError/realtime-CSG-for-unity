using UnityEngine;
using UnityEditor;
using InternalRealtimeCSG;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using System.Linq;
using UnityEngine.SceneManagement;

namespace RealtimeCSG
{
	internal sealed class UnityCompilerDefineManager
	{
		const string RealTimeCSGDefine			= "RealtimeCSG";

        static bool IsObsolete(Enum value)
        {
            var fi = value.GetType().GetField(value.ToString());
            var attributes = (ObsoleteAttribute[])
                fi.GetCustomAttributes(typeof(ObsoleteAttribute), false);
            return (attributes != null && attributes.Length > 0);
        }

        public static void UpdateUnityDefines()
		{
            var requiredDefines = new List<string>();

			requiredDefines.Add(RealTimeCSGDefine);

			string	v						= RealtimeCSG.Foundation.Versioning.PluginVersion;
			int		index					= v.IndexOfAny(new char[] { '_', '.' });
			string	release_version_part	= v.Remove(index);
			string	lower_part				= v.Substring(index + 1);
			string	major_version_part		= lower_part.Remove(1);
			var		minor_version_part		= lower_part.Substring(1);

			var release_version = RealTimeCSGDefine + "_" + release_version_part;
			var major_version	= release_version + "_" + major_version_part;
			var minor_version	= major_version + "_" + minor_version_part;

			requiredDefines.Add(release_version);
			requiredDefines.Add(major_version);
			requiredDefines.Add(minor_version);

			var targetGroups = Enum.GetValues(typeof(BuildTargetGroup)).Cast<BuildTargetGroup>().ToArray();
			foreach (var targetGroup in targetGroups)
			{
				if (IsObsolete(targetGroup))
					continue;
				if (targetGroup == BuildTargetGroup.Unknown
#if UNITY_5_6_OR_NEWER
					|| targetGroup == (BuildTargetGroup)27
#endif
					)
					continue;
				
				var symbol_string = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
				if (symbol_string == null)
					continue;

				bool modified = false;
				var symbols = symbol_string.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
				for (int i = symbols.Length - 1; i >= 0; i--)
				{
					symbols[i] = symbols[i].Trim();
					if (symbols[i].Length == 0)
					{
						ArrayUtility.RemoveAt(ref symbols, i);
						continue;
					}
					if (symbols[i].StartsWith(RealTimeCSGDefine))
					{
						bool keepSymbol = false;
						for (int j = 0; j < requiredDefines.Count; j++)
						{
							if (symbols[i] == requiredDefines[j])
							{
								keepSymbol = true;
								break;
							}
						}
						if (keepSymbol)
							continue;
						modified = true;
						ArrayUtility.RemoveAt(ref symbols, i);
					}
				}

				for (int i = 0; i < requiredDefines.Count; i++)
				{
					if (!symbols.Contains(requiredDefines[i]))
					{
						modified = true;
						ArrayUtility.Add(ref symbols, requiredDefines[i]);
					}
				}

				if (!modified)
					continue;

				var stringBuilder = new System.Text.StringBuilder();
				for (int i = 0; i < symbols.Length; i++)
				{
					if (stringBuilder.Length != 0)
						stringBuilder.Append(';');
					stringBuilder.Append(symbols[i]);
				}

				var newSymbolString = stringBuilder.ToString();
				if (newSymbolString != symbol_string)
					PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, newSymbolString);
			}
		}
	}
}