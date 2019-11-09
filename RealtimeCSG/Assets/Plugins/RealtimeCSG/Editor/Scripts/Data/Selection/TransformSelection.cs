using System;
using System.Collections.Generic;
using RealtimeCSG;
using UnityEngine;
using UnityEditor;

namespace InternalRealtimeCSG
{
    [Serializable]
    internal class TransformSelection
    {
        [SerializeField] public Transform[]     Transforms       = new Transform[0];         // all transforms
        [SerializeField] public Vector3[]       BackupPositions  = new Vector3[0];           // all transforms
        [SerializeField] public Quaternion[]    BackupRotations  = new Quaternion[0];        // all transforms

        public void Select(Transform[] transforms)
        {
            Transforms = transforms;
            BackupPositions = new Vector3[Transforms.Length];
            BackupRotations = new Quaternion[Transforms.Length];
            for (var i = 0; i < Transforms.Length; i++)
            {
                BackupPositions[i] = Transforms[i].position;
                BackupRotations[i] = Transforms[i].rotation;
            }
        }

        public void Update()
        {
            for (var t = 0; t < Transforms.Length; t++)
            {
                if (!Transforms[t])
                    continue;

                BackupPositions[t] = Transforms[t].position;
                BackupRotations[t] = Transforms[t].rotation;
            }
        }
    }
}
