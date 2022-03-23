using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEditor;
using RealtimeCSG;

namespace InternalRealtimeCSG
{
    internal static class TransformOperations
    {
        public static void Rotate(this TransformSelection selection, Vector3 rotationCenter, Vector3 rotationAxis, float rotationAngle)
        {
            var rotationQuaternion = Quaternion.AngleAxis(rotationAngle, rotationAxis);
            if (Mathf.Abs(rotationAngle) < MathConstants.EqualityEpsilon)
            {
                for (var t = 0; t < selection.Transforms.Length; t++)
                {
                    var targetTransform = selection.Transforms[t];
                    targetTransform.position = selection.BackupPositions[t];
                    targetTransform.rotation = selection.BackupRotations[t];
                }
            } else
            {
                for (var t = 0; t < selection.Transforms.Length; t++)
                {
                    var targetTransform = selection.Transforms[t];
                    var originalCenter = targetTransform.InverseTransformPoint(rotationCenter);
                    targetTransform.position = selection.BackupPositions[t];
                    targetTransform.rotation = rotationQuaternion * selection.BackupRotations[t];
                    var newCenter = targetTransform.TransformPoint(originalCenter);
                    targetTransform.position += rotationCenter - newCenter;
                }
            }
        }

        public static void Translate(this TransformSelection selection, Vector3 translation)
        {
            for (var t = 0; t < selection.Transforms.Length; t++)
            {
                selection.Transforms[t].position = GridUtility.CleanPosition(selection.BackupPositions[t] + translation);
            }
        }
    }
}
