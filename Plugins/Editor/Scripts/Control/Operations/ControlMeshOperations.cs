using System;
using System.Collections.Generic;
using RealtimeCSG;
using UnityEngine;
using UnityEditor;

namespace InternalRealtimeCSG
{
	internal static class ControlMeshOperations
	{
		public static void TranslateControlPoints(this BrushSelection selection, Vector3 translation)
		{
			for (var t = 0; t < selection.Brushes.Length; t++)
			{
				var targetControlMesh = selection.BackupControlMeshes[t].Clone();
				if (!targetControlMesh.Valid)
					targetControlMesh.Valid = ControlMeshUtility.Validate(targetControlMesh, selection.Shapes[t]);

				if (!targetControlMesh.Valid)
					continue;

				var targetBrush         = selection.Brushes[t];
				var targetMeshState     = selection.States[t];
				
				if (targetMeshState.BackupPoints == null)
					continue;

				var targetShape         = selection.Shapes[t].Clone();
				var worldToLocalMatrix  = targetMeshState.BrushTransform.worldToLocalMatrix;
				var localDeltaMovement  = GridUtility.CleanPosition(worldToLocalMatrix.MultiplyVector(translation));

				var selectedIndices = targetMeshState.GetSelectedPointIndices();

				for (var p = 0; p < targetMeshState.WorldPoints.Length; p++)
				{
					if (!selectedIndices.Contains((short)p))
						continue;

					targetControlMesh.Vertices[p] = GridUtility.CleanPosition(targetMeshState.BackupPoints[p] + localDeltaMovement);
				}

				ControlMeshUtility.RebuildShapeFrom(targetBrush, targetControlMesh, targetShape);
				selection.ControlMeshes[t] = targetControlMesh;
			}
		}

		public static void TransformControlPoints(this BrushSelection selection, Matrix4x4 transform)
		{
			for (var t = 0; t < selection.Brushes.Length; t++)
			{
				var targetControlMesh = selection.BackupControlMeshes[t].Clone();
				if (!targetControlMesh.Valid)
				   targetControlMesh.Valid = ControlMeshUtility.Validate(targetControlMesh, selection.Shapes[t]);

				if (!targetControlMesh.Valid)
					continue;

				var targetBrush         = selection.Brushes[t];
				var targetMeshState     = selection.States[t];

				if (targetMeshState.BackupPoints == null)
					continue;

				var targetShape         = selection.Shapes[t].Clone();
				var localToWorldMatrix  = targetMeshState.BrushTransform.localToWorldMatrix;
				var worldToLocalMatrix  = targetMeshState.BrushTransform.worldToLocalMatrix;

				var localCombinedMatrix = worldToLocalMatrix *
										  transform *
										  localToWorldMatrix;

				var selectedIndices = targetMeshState.GetSelectedPointIndices();

				for (var p = 0; p < targetMeshState.BackupPoints.Length; p++)
				{
					if (!selectedIndices.Contains((short)p))
						continue;

					var point = targetMeshState.BackupPoints[p];

					point = localCombinedMatrix.MultiplyPoint(point);

					targetControlMesh.Vertices[p] = GridUtility.CleanPosition(point);
				}

				selection.ControlMeshes[t] = targetControlMesh;
				
				// This might create new controlmesh / shape
				ControlMeshUtility.MergeDuplicatePoints(targetBrush, ref targetControlMesh, ref targetShape);
				if (targetControlMesh.Valid)
				{
					targetControlMesh.SetDirty();
					ControlMeshUtility.RebuildShapeFrom(targetBrush, targetControlMesh, targetShape);
				}
			}
		}
		

		public static void RotateControlPoints(this BrushSelection selection, Vector3 center, Quaternion handleRotation, Quaternion rotationOffset)
		{
			var inverseHandleRotation   = Quaternion.Inverse(handleRotation);

			var rotationMatrix          = Matrix4x4.TRS(Vector3.zero, handleRotation, Vector3.one);
			var inverseRotationMatrix   = Matrix4x4.TRS(Vector3.zero, inverseHandleRotation, Vector3.one);
			var actionMatrix            = Matrix4x4.TRS(Vector3.zero, rotationOffset, Vector3.one);
			var moveMatrix              = Matrix4x4.TRS(center, Quaternion.identity, Vector3.one);
			var inverseMoveMatrix       = Matrix4x4.TRS(-center, Quaternion.identity, Vector3.one);

			var combinedMatrix =
											moveMatrix *

											rotationMatrix *
											actionMatrix *
											inverseRotationMatrix *

											inverseMoveMatrix
											;

			selection.TransformControlPoints(combinedMatrix);
		}
		
		public static void ScaleControlPoints(this BrushSelection selection, Vector3 center, Quaternion rotation, Vector3 scale)
		{
			if (float.IsInfinity(scale.x) || float.IsNaN(scale.x) ||
				float.IsInfinity(scale.y) || float.IsNaN(scale.y) ||
				float.IsInfinity(scale.z) || float.IsNaN(scale.z))
				scale = Vector3.zero;

			if (scale.x <= MathConstants.EqualityEpsilon) { scale.x = 0.0f; }
			if (scale.y <= MathConstants.EqualityEpsilon) { scale.y = 0.0f; }
			if (scale.z <= MathConstants.EqualityEpsilon) { scale.z = 0.0f; }


			var inverseRotation         = Quaternion.Inverse(rotation);

			var rotationMatrix          = Matrix4x4.TRS(Vector3.zero, rotation, Vector3.one);
			var inverseRotationMatrix   = Matrix4x4.TRS(Vector3.zero, inverseRotation, Vector3.one);
			var scaleMatrix             = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, scale);
			var moveMatrix              = Matrix4x4.TRS(center, Quaternion.identity, Vector3.one);
			var inverseMoveMatrix       = Matrix4x4.TRS(-center, Quaternion.identity, Vector3.one);

			var combinedMatrix =
											moveMatrix *
											rotationMatrix *
											scaleMatrix *
											inverseRotationMatrix *
											inverseMoveMatrix
											;

			selection.TransformControlPoints(combinedMatrix);
		}

		public static void CleanPoints(this BrushSelection selection)
		{
			var brushModified = false;
			for (var t = 0; t < selection.States.Length; t++)
			{
				var brush = selection.Brushes[t];
				if (!brush)
					continue;
				
//				var brushTransform		= brush.GetComponent<Transform>();
//				var brushLocalToWorld	= brushTransform.localToWorldMatrix;
//				var brushWorldToLocal	= brushTransform.worldToLocalMatrix;

				var controlMesh			= brush.ControlMesh;
				var points				= controlMesh.Vertices;
				bool vertexModified = false;
				for (int i=0;i<points.Length;i++)
				{ 
					var cleanedPoint = GridUtility.CleanPosition(points[i]);
					if (controlMesh.Vertices[i].x == cleanedPoint.x &&
						controlMesh.Vertices[i].y == cleanedPoint.y &&
						controlMesh.Vertices[i].z == cleanedPoint.z)
						continue;
					controlMesh.Vertices[i] = cleanedPoint;
					vertexModified = true;
				}
				if (vertexModified)
				{
					brush.ControlMesh.SetDirty();
					brushModified = true;
				}
			}
			if (brushModified)
				InternalCSGModelManager.CheckForChanges();
		}
		
		public static void PointSnapToGrid(this BrushSelection selection, Camera camera)
		{
			var minWorldDeltaMovement = MathConstants.PositiveInfinityVector3;
			for (var t = 0; t < selection.States.Length; t++)
			{
				var brush = selection.Brushes[t];
				if (!brush)
					continue;

				var controlMeshState	= selection.States[t];
				var	brushTransform		= brush.GetComponent<Transform>();
				var brushLocalToWorld	= brushTransform.localToWorldMatrix;
				var brushWorldToLocal	= brushTransform.worldToLocalMatrix;

				var controlMesh			= brush.ControlMesh;
				var points				= controlMesh.Vertices;

				var localPoints = new List<Vector3>(points.Length);
				var selectedIndices = controlMeshState.GetSelectedPointIndices();
				foreach(var index in selectedIndices)
				{
					localPoints.Add(points[index]);
				}

				if (localPoints.Count > 0)
				{
					var worldDeltaMovement	= RealtimeCSG.CSGGrid.SnapLocalPointToWorldGridDelta(camera, brushLocalToWorld, brushWorldToLocal, localPoints.ToArray());
					if (Mathf.Abs(worldDeltaMovement.x) < Mathf.Abs(minWorldDeltaMovement.x)) { minWorldDeltaMovement.x = worldDeltaMovement.x; }
					if (Mathf.Abs(worldDeltaMovement.y) < Mathf.Abs(minWorldDeltaMovement.y)) { minWorldDeltaMovement.y = worldDeltaMovement.y; }
					if (Mathf.Abs(worldDeltaMovement.z) < Mathf.Abs(minWorldDeltaMovement.z)) { minWorldDeltaMovement.z = worldDeltaMovement.z; }
				}
			}

			if (float.IsInfinity(minWorldDeltaMovement.x))
				return; // nothing to do

			var brushModified = false;
			for (var t = 0; t < selection.States.Length; t++)
			{
				var brush = selection.Brushes[t];
				if (!brush)
					continue;

				var controlMeshState = selection.States[t];

				var brushTransform		= brush.GetComponent<Transform>();
				var brushLocalToWorld	= brushTransform.localToWorldMatrix;
				var brushWorldToLocal	= brushTransform.worldToLocalMatrix;

				var controlMesh			= brush.ControlMesh;
				var points				= controlMesh.Vertices;
				var selectedIndices		= controlMeshState.GetSelectedPointIndices();
				bool vertexModified = false;
				foreach (var index in selectedIndices)
				{ 
					var worldPoint	 = brushLocalToWorld.MultiplyPoint(points[index]);
					worldPoint += minWorldDeltaMovement;
					var localPoint	= brushWorldToLocal.MultiplyPoint(worldPoint);
					var snappedPoint = GridUtility.CleanPosition(localPoint);
					if (controlMesh.Vertices[index].x == snappedPoint.x &&
						controlMesh.Vertices[index].y == snappedPoint.y &&
						controlMesh.Vertices[index].z == snappedPoint.z)
						continue;
					controlMesh.Vertices[index] = snappedPoint;
					vertexModified = true;
				}
				if (vertexModified)
				{
					brush.ControlMesh.SetDirty();
					brushModified = true;
				}
			}
			if (brushModified)
				InternalCSGModelManager.CheckForChanges();
		}
	}
}
