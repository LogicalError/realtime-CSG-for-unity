using System;
using System.Collections.Generic;
using System.Linq;
using RealtimeCSG;
using UnityEngine;
using UnityEditor;
using RealtimeCSG.Legacy;
using RealtimeCSG.Components;

namespace InternalRealtimeCSG
{
    internal class BrushSelection
    {
        [SerializeField] public CSGBrush[]              Brushes         = new CSGBrush[0]; 
        [SerializeField] public Shape[]                 Shapes          = new Shape[0]; 
        [SerializeField] public ControlMesh[]           ControlMeshes   = new ControlMesh[0]; 
        [SerializeField] public ControlMeshState[]      States          = new ControlMeshState[0]; 

        [SerializeField] public ControlMesh[]           BackupControlMeshes = new ControlMesh[0]; 
        [SerializeField] public Shape[]					BackupShapes		= new Shape[0]; 
        [SerializeField] public Matrix4x4[]             LocalToWorld        = new Matrix4x4[0]; 
        [SerializeField] public Transform[]             ModelTransforms     = new Transform[0];


        public bool HaveSelection
        {
            get
            {
                for (var t = 0; t < Brushes.Length; t++)
                {
                    if (States[t].HaveSelection)
                        return true;
                }
                return false;
            }
        }

        private bool HaveEdgeSelection
        {
            get
            {
                for (var t = 0; t < Brushes.Length; t++)
                    if (States[t].HaveEdgeSelection)
                        return true;
                return false;
            }
        }


		public void Select(CSGBrush brush, int polygonIndex)
		{
			for (var i = Brushes.Length - 1; i >= 0; i--)
			{
				if (Brushes[i] == brush)
				{
					var polygons = States[i].Selection.Polygons;
					for (int j = 0; j < polygons.Length; j++)
					{
						if (j == polygonIndex)
							polygons[j] = SelectState.Selected;
						else
							polygons[j] = SelectState.None;
					}
				}
			}
		}

        public void Select(HashSet<CSGBrush> foundBrushes)
        {
            if (Brushes == null || Brushes.Length == 0)
            {
                Brushes             = foundBrushes.ToArray();
                Shapes              = new Shape[Brushes.Length];
                ControlMeshes       = new ControlMesh[Brushes.Length];
                LocalToWorld        = new Matrix4x4[Brushes.Length];
                BackupControlMeshes = new ControlMesh[Brushes.Length];
				BackupShapes		= new Shape[Brushes.Length];
                States              = new ControlMeshState[Brushes.Length];
                ModelTransforms     = new Transform[Brushes.Length];

                for (var i = 0; i < foundBrushes.Count; i++)
                    LocalToWorld[i] = MathConstants.identityMatrix;
            } else
            {
                // remove brushes that are no longer selected
                for (var i = Brushes.Length - 1; i >= 0; i--)
                {
                    if (foundBrushes.Contains(Brushes[i]))
                        continue;

                    ArrayUtility.RemoveAt(ref Brushes, i);
                    ArrayUtility.RemoveAt(ref Shapes, i);
                    ArrayUtility.RemoveAt(ref ControlMeshes, i);
                    ArrayUtility.RemoveAt(ref LocalToWorld, i);
                    ArrayUtility.RemoveAt(ref BackupControlMeshes, i);
					ArrayUtility.RemoveAt(ref BackupShapes, i);
					ArrayUtility.RemoveAt(ref States, i);
                    ArrayUtility.RemoveAt(ref ModelTransforms, i);
                }

                // add new brushes that are added to the selection
                foreach (var newBrush in foundBrushes)
                {
                    if (Brushes.Contains(newBrush))
                        continue;

                    ArrayUtility.Add(ref Brushes, newBrush);
                    ArrayUtility.Add(ref Shapes, null);
                    ArrayUtility.Add(ref ControlMeshes, null);
                    ArrayUtility.Add(ref LocalToWorld, MathConstants.identityMatrix);
                    ArrayUtility.Add(ref BackupControlMeshes, null);
					ArrayUtility.Add(ref BackupShapes, null);
					ArrayUtility.Add(ref States, null);
                    ArrayUtility.Add(ref ModelTransforms, null);
                }
            }
        }

        public void ResetSelection()
        {
            if (Brushes != null && States != null)
            {
                for (var i = 0; i < Brushes.Length; i++)
                {
                    States[i] = null;
                }
            }
        }

        public void UpdateTargets()
        {
            for (var i = 0; i < Brushes.Length; i++)
            {
                if (!Brushes[i] ||
					Brushes[i].ControlMesh == null)
                    continue;

                if (!Brushes[i].ControlMesh.Valid)
                    Brushes[i].ControlMesh.Valid = ControlMeshUtility.Validate(Brushes[i].ControlMesh, Brushes[i].Shape);
            
                LocalToWorld[i] = Brushes[i].transform.localToWorldMatrix;

                if (States[i] != null)
                    continue;

                States[i] = new ControlMeshState(Brushes[i]);
				BackupControlMeshes[i]	= (Brushes[i].ControlMesh != null) ? Brushes[i].ControlMesh.Clone() : null;
				BackupShapes[i]			= (Brushes[i].Shape != null) ? Brushes[i].Shape.Clone() : null;

				ControlMeshes[i]	= BackupControlMeshes[i];
				Shapes[i]			= BackupShapes[i];
            }
            UpdateParentModelTransforms();
        }

        public void UpdateParentModelTransforms()
        {
            for (var i = 0; i < Brushes.Length; i++)
            {
                if (ModelTransforms[i] != null)
                    continue;
				
                if (Brushes[i].ChildData == null ||
                    Brushes[i].ChildData.ModelTransform == null)
                    continue;

                ModelTransforms[i] = Brushes[i].ChildData.ModelTransform;
            }
        }


        public void BackupSelection()
        {
            for (var t = 0; t < States.Length; t++)
            {
                if (States[t] == null)
                    continue;
                
                States[t].BackupSelection();
            }
        }

        public void RevertSelection()
        {
            for (var t = 0; t < States.Length; t++)
            {
                if (States[t] == null)
                    continue;
                
                States[t].RevertSelection();
            }
		}

		public void DestroySelectionBackup()
		{
			for (var t = 0; t < States.Length; t++)
			{
				if (States[t] == null)
					continue;

				States[t].DestroySelectionBackup();
			}
		}

		public bool HasSelectionChanged()
        {
            for (var t = 0; t < States.Length; t++)
            {
                if (States[t] == null)
                    continue;

                if (States[t].HasSelectionChanged())
                    return true;
            }
            return false;
        }

        public void UnHoverAll()
        {
            for (var t = 0; t < States.Length; t++)
            {
                if (States[t] == null)
                    continue;

                States[t].UnHoverAll();
            }
        }

        public void UndoRedoPerformed()
        {
            for (var t = 0; t < States.Length; t++)
            {
                if (t >= Brushes.Length ||
					States[t] == null ||
                    !Brushes[t])
                    continue;

                var controlMesh = Brushes[t].ControlMesh;
                var state = States[t];
                state.UpdatePoints(controlMesh);
            }
        }

        public bool UpdateWorkControlMesh(bool forceUpdate = false)
        {
            for (var t = Brushes.Length - 1; t >= 0; t--)
            {
                if (!Brushes[t])
                {
                    ArrayUtility.RemoveAt(ref Brushes, t);
                    continue;
                }

                if (!forceUpdate &&
                    ControlMeshes[t] != null &&
                    !ControlMeshes[t].Valid)
                    continue;

                Shapes[t] = Brushes[t].Shape.Clone();
                ControlMeshes[t] = Brushes[t].ControlMesh.Clone();
				BackupShapes[t] = Shapes[t];
				BackupControlMeshes[t] = ControlMeshes[t];
            }

            for (var i = 0; i < Brushes.Length; i++)
            {
                LocalToWorld[i] = Brushes[i].transform.localToWorldMatrix;
            }
            return true;
        }

        public void FindClosestIntersection(SceneView sceneView, out int closestBrushNodeIndex, out int closestSurfaceIndex)
        {
            var mouseWorldRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            var rayStart = mouseWorldRay.origin;
            var rayVector = (mouseWorldRay.direction * (sceneView.camera.farClipPlane - sceneView.camera.nearClipPlane));
            var rayEnd = rayStart + rayVector;

            var minDistance = float.PositiveInfinity;
            closestBrushNodeIndex = -1;
            closestSurfaceIndex = -1;
            for (var t = 0; t < Brushes.Length; t++)
            {
                var brush = Brushes[t];
                if (!Brushes[t] || !Brushes[t].isActiveAndEnabled)
                    continue;

                var parentModelTransform = ModelTransforms[t];
                if (parentModelTransform == null)
                    continue;

                var modelTransformation = parentModelTransform.localToWorldMatrix;

                LegacyBrushIntersection intersection;
                if (!SceneQueryUtility.FindBrushIntersection(brush, modelTransformation, rayStart, rayEnd, out intersection))
                    continue;
                
                var distance = (intersection.worldIntersection - rayStart).magnitude;
                if (distance > minDistance)
                    continue;
                
                minDistance = distance;
                closestBrushNodeIndex = t;
                closestSurfaceIndex = intersection.surfaceIndex;
            }
        }
		

		public Vector3[] GetSelectedWorldPoints(bool useBackupPoints = false)
        {
            var points  = new HashSet<Vector3>();
            for (var t = 0; t < States.Length; t++)
            {
                var meshState = States[t];
                var brushLocalToWorld = LocalToWorld[t];

				if (useBackupPoints)
				{
					if (meshState.BackupPoints == null)
						continue;

					foreach (var index in meshState.GetSelectedPointIndices())
					{
						points.Add(brushLocalToWorld.MultiplyPoint(meshState.BackupPoints[index]));
					}
				} else
				{
					foreach (var index in meshState.GetSelectedPointIndices())
					{
						points.Add(meshState.WorldPoints[index]);
					}
				}
            }
            return points.ToArray();
        }

        public AABB GetSelectionBounds()
        {
            var newBounds = AABB.Empty;
            for (var t = 0; t < Brushes.Length; t++)
            {
                var meshState = States[t];
				if (meshState == null)
					continue;

				if (meshState.Selection.Points != null)
				{
					for (var p = 0; p < meshState.Selection.Points.Length; p++)
					{
						if ((meshState.Selection.Points[p] & SelectState.Selected) != SelectState.Selected)
							continue;
						newBounds.Extend(meshState.WorldPoints[p]);
					}
				}

				if (meshState.Selection.Edges != null)
				{
					for (var e = 0; e < meshState.Selection.Edges.Length; e++)
					{
						if ((meshState.Selection.Edges[e] & SelectState.Selected) != SelectState.Selected)
							continue;

						var index0 = meshState.Edges[(e * 2) + 0];
						var index1 = meshState.Edges[(e * 2) + 1];

						newBounds.Extend(meshState.WorldPoints[index0]);
						newBounds.Extend(meshState.WorldPoints[index1]);
					}
				}

				if (meshState.Selection.Polygons != null)
				{
					for (var p = 0; p < meshState.Selection.Polygons.Length; p++)
					{
						if ((meshState.Selection.Polygons[p] & SelectState.Selected) != SelectState.Selected)
							continue;

						var indices = meshState.PolygonPointIndices[p];
						for (var i = 0; i < indices.Length; i++)
						{
							var index = indices[i];

							newBounds.Extend(meshState.WorldPoints[index]);
						}
					}
				}
            }

            return newBounds;
        }

		internal void DebugLogData()
		{
			int pointCount = 0;
			int polygonCount = 0;
			int edgeCount = 0;
			for (var t = 0; t < States.Length; t++)
			{
				var state = States[t];
				var selection = state.Selection;
				for (var p = 0; p < selection.Points.Length; p++)
				{
					if ((selection.Points[p] & SelectState.Selected) == SelectState.Selected)
						pointCount++;
				}
				for (var e = 0; e < selection.Edges.Length; e++)
				{
					if ((selection.Edges[e] & SelectState.Selected) == SelectState.Selected)
						edgeCount++;
				}
				for (var e = 0; e < selection.Polygons.Length; e++)
				{
					if ((selection.Polygons[e] & SelectState.Selected) == SelectState.Selected)
						polygonCount++;
				}
			}
			Debug.Log(pointCount + " " + edgeCount + " " +polygonCount);
		}
	}
}
