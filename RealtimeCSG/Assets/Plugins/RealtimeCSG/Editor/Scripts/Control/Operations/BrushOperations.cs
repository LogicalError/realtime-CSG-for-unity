using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using RealtimeCSG;
using RealtimeCSG.Legacy;
using RealtimeCSG.Components;

namespace InternalRealtimeCSG
{
    internal static class BrushOperations
    {
        public static void SnapToGrid(Camera camera, CSGBrush[] brushes)
        {
            var worldDeltaMovement = MathConstants.zeroVector3;

            var transforms = new List<Transform>();
            for (var b = 0; b < brushes.Length; b++)
            {
                if (brushes[b] == null ||
                    !brushes[b])
                {
                    continue;
                }

                var brushTransform = brushes[b].GetComponent<Transform>();
                var brushLocalToWorld = brushTransform.localToWorldMatrix;

                transforms.Add(brushTransform);

                var controlMesh = brushes[b].ControlMesh;
                var points = controlMesh.Vertices;

                var worldPoints = new Vector3[points.Length];
                for (var p = 0; p < points.Length; p++)
                {
                    worldPoints[p] = brushLocalToWorld.MultiplyPoint(points[p]);
                }

                worldDeltaMovement = RealtimeCSG.CSGGrid.SnapDeltaToGrid(camera, worldDeltaMovement, worldPoints.ToArray(), snapToGridPlane: false, snapToSelf: false);
            }

            if (worldDeltaMovement.x == 0.0f && worldDeltaMovement.y == 0.0f && worldDeltaMovement.z == 0.0f)
                return;

            for (var b = 0; b < transforms.Count; b++)
            {
                var transform = transforms[b];
                transform.position = GridUtility.CleanPosition(transform.position + worldDeltaMovement);
            }
        }

        public static void Flip(CSGBrush[] brushes, Matrix4x4 flipMatrix, string undoDescription = "Flip brushes")
        {
            var fail = false;
            Undo.IncrementCurrentGroup();
            Undo.RegisterCompleteObjectUndo(brushes.ToArray<UnityEngine.Object>(), undoDescription);

            var isGlobal = Tools.pivotRotation == PivotRotation.Global;

            var centerAll = BoundsUtilities.GetCenter(brushes);
            for (var t = 0; t < brushes.Length; t++)
            {
                var brush = brushes[t];
                var position = brush.transform.position;

                Matrix4x4 brushFlip;
                Vector3 brushCenter;
                if (isGlobal)
                {
                    brushFlip	= brush.transform.localToWorldMatrix *
                                       flipMatrix *
                                       brush.transform.worldToLocalMatrix;
                    brushCenter = brush.transform.InverseTransformPoint(centerAll) - position;
                } else
                {
                    brushFlip	= flipMatrix;
                    brushCenter = brush.transform.InverseTransformPoint(centerAll);
                }

				brushFlip = Matrix4x4.TRS(brushCenter, Quaternion.identity, Vector3.one) *
							brushFlip *
							Matrix4x4.TRS(-brushCenter, Quaternion.identity, Vector3.one);
				
                brush.EnsureInitialized();
                var shape = brush.Shape;
                for (var s = 0; s < shape.Surfaces.Length; s++)
                {
                    var plane = shape.Surfaces[s].Plane;

                    var normal		= brushFlip.MultiplyVector(plane.normal);
                    var biNormal	= brushFlip.MultiplyVector(shape.Surfaces[s].BiNormal);
                    var tangent		= brushFlip.MultiplyVector(shape.Surfaces[s].Tangent);

                    var pointOnPlane = plane.pointOnPlane;
                    pointOnPlane = brushFlip.MultiplyPoint(pointOnPlane);

                    shape.Surfaces[s].Plane		= new CSGPlane(normal, pointOnPlane);
                    shape.Surfaces[s].BiNormal	= biNormal;
                    shape.Surfaces[s].Tangent	= tangent;
                }

                var controlMesh = brush.ControlMesh;
				var vertices = controlMesh.Vertices;
				for (var v = 0; v < vertices.Length; v++)
					vertices[v] = brushFlip.MultiplyPoint(vertices[v]);

				var polygons = controlMesh.Polygons;
				for (var p = 0; p < polygons.Length; p++)
					Array.Reverse(polygons[p].EdgeIndices);

				var edges = controlMesh.Edges;
				var twinVertices = new short[edges.Length];
				for (var e = 0; e < edges.Length; e++)
					twinVertices[e] = edges[edges[e].TwinIndex].VertexIndex;
				
				for (var e = 0; e < edges.Length; e++)
					edges[e].VertexIndex = twinVertices[e];

                brush.ControlMesh.SetDirty();
                EditorUtility.SetDirty(brush);

                InternalCSGModelManager.CheckSurfaceModifications(brush, true);

                ControlMeshUtility.RebuildShape(brush);
            }
            if (fail)
            {
                Debug.LogWarning("Failed to perform operation");
                Undo.RevertAllInCurrentGroup();
            }
            InternalCSGModelManager.CheckForChanges();
        }

        public static void FlipX(CSGBrush[] flipBrushes, string undoDescription = "Flip brushes on X Axis") { var flipMatrix = MathConstants.identityMatrix; flipMatrix.m00 = -1; Flip(flipBrushes, flipMatrix, "Flip brushes on X Axis"); }
        public static void FlipY(CSGBrush[] flipBrushes, string undoDescription = "Flip brushes on Y Axis") { var flipMatrix = MathConstants.identityMatrix; flipMatrix.m11 = -1; Flip(flipBrushes, flipMatrix, "Flip brushes on Y Axis"); }
        public static void FlipZ(CSGBrush[] flipBrushes, string undoDescription = "Flip brushes on Z Axis") { var flipMatrix = MathConstants.identityMatrix; flipMatrix.m22 = -1; Flip(flipBrushes, flipMatrix, "Flip brushes on Z Axis"); }

    }
}
