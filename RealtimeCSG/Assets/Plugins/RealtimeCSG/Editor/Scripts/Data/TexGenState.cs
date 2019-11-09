using System;
using UnityEngine;
using InternalRealtimeCSG;
using UnityEditor;
using RealtimeCSG.Legacy;
using RealtimeCSG.Components;

namespace RealtimeCSG
{
	[Serializable]
	internal sealed class TexGenState
	{
		[SerializeField] public int[]			surfaceControlID;
		[SerializeField] public SelectState[]	surfaceSelectState;
//		[SerializeField] public Color[]			surfaceColors;

		[NonSerialized] public TexGen[] backupTexGens;

		[NonSerialized] public Vector3	rayIntersectionWorldPoint;
		[NonSerialized] public Vector2	rayIntersectionSurfacePoint;

		public TexGenState(CSGBrush brush)
		{
			var shape	= brush.Shape;
			AllocateSurfaces(shape.Surfaces.Length);
		}

		public void UpdateTexGenState(CSGBrush brush)
		{
			var shape = brush.Shape;
			if (surfaceSelectState == null ||
				surfaceControlID == null)
			{
				AllocateSurfaces(shape.Surfaces.Length);
				return;
			}

			while (shape.Surfaces.Length < surfaceSelectState.Length)
			{
				ArrayUtility.RemoveAt(ref surfaceSelectState, surfaceSelectState.Length - 1);
			}
			while (shape.Surfaces.Length < surfaceControlID.Length)
			{
				ArrayUtility.RemoveAt(ref surfaceControlID, surfaceControlID.Length - 1);
			}
			while (shape.Surfaces.Length > surfaceSelectState.Length)
			{
				ArrayUtility.Add(ref surfaceSelectState, SelectState.None);
			}
			while (shape.Surfaces.Length > surfaceControlID.Length)
			{
				ArrayUtility.Add(ref surfaceControlID, -1);
			}
		}

		void AllocateSurfaces(int surfaceCount)
		{
			surfaceControlID	= new int[surfaceCount];
			surfaceSelectState	= new SelectState[surfaceCount];
//			surfaceColors		= new Color[surfaceCount * 2];
		}

		public void UpdateLayout(CSGBrush brush)
		{
			var shape			= brush.Shape;			
			int surfaceCount	= shape.Surfaces.Length;
			if (surfaceControlID.Length != surfaceCount)
				AllocateSurfaces(surfaceCount);
		}
		/*
		public void UpdateColors(CSGBrush brush)
		{
			int surfaceCount = surfaceSelectState.Length;
			for (int s = 0, c = 0; s < surfaceCount; s++, c += 2)
			{
				var state = (int)surfaceSelectState[s];
				surfaceColors[c + 0] = ColorSettings.SurfaceInnerStateColor[state];
				surfaceColors[c + 1] = ColorSettings.SurfaceOuterStateColor[state];
			}
		}
		*/
		public bool HaveSurfaceSelection
		{
			get
			{
				for (int p = 0; p < surfaceSelectState.Length; p++)
				{
					if ((surfaceSelectState[p] & SelectState.Selected) == SelectState.Selected)
						return true;
				}
				return false;
			}
		}

		public void DeselectAll()
		{
			for (int p = 0; p < surfaceSelectState.Length; p++)
				surfaceSelectState[p] &= ~SelectState.Selected;
		}

		public void SelectAll()
		{
			for (int p = 0; p < surfaceSelectState.Length; p++)
				surfaceSelectState[p] |= SelectState.Selected;
		}

		public void UnHoverAll()
		{
			for (int p = 0; p < surfaceSelectState.Length; p++)
				surfaceSelectState[p] &= ~SelectState.Hovering;
		}

		public bool IsSurfaceSelected(int surfaceIndex)
		{
			var new_state = surfaceSelectState[surfaceIndex];
			return ((new_state & SelectState.Selected) == SelectState.Selected);
		}

		public bool SelectSurface(int surfaceIndex, SelectionType selectionType)
		{
			var old_state = surfaceSelectState[surfaceIndex];
			if ((old_state & SelectState.Hovering) != SelectState.Hovering)
				return false;
			var new_state = old_state;
			if		(selectionType ==   SelectionType.Subtractive) new_state &= ~SelectState.Selected;
			else if (selectionType ==   SelectionType.Toggle     ) new_state ^=  SelectState.Selected;
			else												   new_state |=  SelectState.Selected;
			if (old_state == new_state)
				return false;
			surfaceSelectState[surfaceIndex] = new_state;
			return true;
		}
	};
}
