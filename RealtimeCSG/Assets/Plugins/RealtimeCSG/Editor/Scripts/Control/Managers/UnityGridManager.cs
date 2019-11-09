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
	internal sealed class UnityGridManager
	{
		static Type			UnityAnnotationUtility;
		static PropertyInfo UnityShowGridProperty;

		static bool         initialized = false;
		static bool			reflectionSucceeded = false;

		static void InitReflectedData()
		{
			if (initialized)
				return;
			initialized = true;
			reflectionSucceeded = false;

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
			UnityAnnotationUtility		= types.FirstOrDefault(t => t.FullName == "UnityEditor.AnnotationUtility");
			
			if (UnityAnnotationUtility != null)
			{
				UnityShowGridProperty = UnityAnnotationUtility.GetProperty("showGrid", BindingFlags.NonPublic | BindingFlags.Static);
				if (UnityShowGridProperty != null)
					UnityShowGridProperty.SetValue(UnityAnnotationUtility, false, null);
			} else
			{
				UnityShowGridProperty = null;
			}
			
			reflectionSucceeded =	UnityShowGridProperty   != null;
		}

		internal static bool ShowGrid
		{
			get
			{
				InitReflectedData();
				if (reflectionSucceeded && UnityShowGridProperty != null)
					return (bool)UnityShowGridProperty.GetValue(UnityAnnotationUtility, null);
				return true;
			}
			set
			{
				InitReflectedData();
				if (reflectionSucceeded && UnityShowGridProperty != null)
					UnityShowGridProperty.SetValue(UnityAnnotationUtility, value, null);
			}
		}
	}
}