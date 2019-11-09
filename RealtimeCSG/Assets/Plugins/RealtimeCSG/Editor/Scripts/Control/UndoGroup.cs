using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using RealtimeCSG.Components;

namespace RealtimeCSG
{
	internal sealed class UndoGroup : IDisposable
	{
		private CSGBrush[] _brushes;
		private int _undoGroupIndex;

		static readonly HashSet<CSGBrush> uniqueBrushes   = new HashSet<CSGBrush>();
		static readonly HashSet<CSGModel> uniqueModels    = new HashSet<CSGModel>();

		public UndoGroup(SelectedBrushSurface[] selectedBrushSurfaces, string name, bool ignoreGroup = false)
		{
			if (selectedBrushSurfaces == null)
				return;

			uniqueBrushes.Clear();
			uniqueModels.Clear();
			for (int i = 0; i < selectedBrushSurfaces.Length; i++)
			{
				if (!selectedBrushSurfaces[i].brush)
					continue;

				var brush = selectedBrushSurfaces[i].brush;
//				var surface_index = selectedBrushSurfaces[i].surfaceIndex;
				if (uniqueBrushes.Add(brush))
				{
					uniqueModels.Add(brush.ChildData.Model);
				}
			}

			_undoGroupIndex = -1;

			_brushes = uniqueBrushes.ToArray();
			if (_brushes.Length > 0)
			{
				if (!ignoreGroup)
				{
					_undoGroupIndex = Undo.GetCurrentGroup();
					Undo.IncrementCurrentGroup();
				}
				Undo.RegisterCompleteObjectUndo(_brushes, name);
				for (int i = 0; i < _brushes.Length; i++)
				{
					if (!_brushes[i])
						continue;
					UnityEditor.EditorUtility.SetDirty(_brushes[i]);
				}
			}
		}
			
		private bool disposedValue = false;
		public void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					if (_brushes == null)
						return;

					if (_brushes.Length > 0)
					{
						for (int i = 0; i < _brushes.Length; i++)
						{
							if (!_brushes[i])
								continue;
							_brushes[i].EnsureInitialized();
							ShapeUtility.CheckMaterials(_brushes[i].Shape);
						}
						for (int i = 0; i < _brushes.Length; i++)
						{
							if (!_brushes[i])
								continue;
							InternalCSGModelManager.CheckSurfaceModifications(_brushes[i], true);
						}
						if (_undoGroupIndex != -1)
						{
							Undo.CollapseUndoOperations(_undoGroupIndex);
							Undo.FlushUndoRecordObjects();
						}
					}
				}
				_brushes = null;
				disposedValue = true;
			}
		}

		public void Dispose() { Dispose(true); }
	}
		
}
