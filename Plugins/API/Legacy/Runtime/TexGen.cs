using System;
using UnityEngine;
using System.Runtime.InteropServices;
using InternalRealtimeCSG;

namespace RealtimeCSG.Legacy
{
    /// <summary>Defines what type of surface a surface is (Renderable, Collidable etc.) and how its texture coordinates and normals are generated.</summary>
    /// <remarks><note>This code is legacy and will be removed eventually.</note></remarks>
    /// <seealso cref="RealtimeCSG.Legacy.Shape"/>
    /// <seealso cref="RealtimeCSG.Legacy.ControlMesh"/>
    /// <seealso cref="RealtimeCSG.Legacy.Polygon"/>
    /// <seealso cref="RealtimeCSG.Legacy.TexGenFlags"/>
    [Serializable, StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct TexGen
    {
        /// <value>UV translation relative to plane origin</value>
        public Vector2			Translation;
        
        /// <value>UV scale relative to translated plane origin</value>
        public Vector2			Scale;
        
        /// <value>UV rotation relative to translated plane origin</value>
        public float			RotationAngle;

        /// <value>[UnityEngine.Material](https://docs.unity3d.com/ScriptReference/Material.html) that is used by the polygons associated with this <see cref="TexGen"/> to generate [UnityEngine.MeshRenderer](https://docs.unity3d.com/ScriptReference/MeshRenderer.html) with</value>
        public Material			RenderMaterial;
        
        /// <value>[UnityEngine.PhysicMaterial](https://docs.unity3d.com/ScriptReference/PhysicMaterial.html) that is used by the polygons associated with this <see cref="TexGen"/> to generate [UnityEngine.Collider](https://docs.unity3d.com/ScriptReference/Collider.html) with</value>
        public PhysicMaterial	PhysicsMaterial;
        
        /// <value><see cref="SmoothingGroup"/> used to smooth normals together</value>
        /// <remarks>All normals around a vertex are averaged for all surfaces that have the same <see cref="SmoothingGroup"/>. When <see cref="SmoothingGroup"/> is 0 no smoothing is applied.</remarks>
        public UInt32			SmoothingGroup;

        /// <summary>Constructor to generate <see cref="TexGen"/></summary>
        /// <param name="renderMaterial">[UnityEngine.Material](https://docs.unity3d.com/ScriptReference/Material.html) to use with this <see cref="TexGen"/></param>
        /// <param name="physicsMaterial">[UnityEngine.PhysicMaterial](https://docs.unity3d.com/ScriptReference/PhysicMaterial.html) to use with this <see cref="TexGen"/></param>
        public TexGen(Material renderMaterial = null, PhysicMaterial physicsMaterial = null)
        {
            Translation			= MathConstants.zeroVector3;
            Scale				= MathConstants.oneVector3;
            RotationAngle		= 0;
            RenderMaterial		= renderMaterial;
            PhysicsMaterial		= physicsMaterial;
            SmoothingGroup		= 0;
        }
        
        /// <summary>Generates a matrix that converts a coordinate in plane-space to uv-space</summary>
        /// <returns>A plane space to uv space matrix</returns>
        public Matrix4x4 GeneratePlaneSpaceToTextureSpaceMatrix()
        {
            var sx	= Scale.x;
            var sy	= Scale.y;
            var r	= Mathf.Deg2Rad * -RotationAngle;
            var rs	= Mathf.Sin(r);
            var rc	= Mathf.Cos(r);
            var tx	= Translation.x;
            var ty	= Translation.y;
                
            //*
            var scaleMatrix = new Matrix4x4()
            {
                m00 =  -sx, m10 = 0.0f, m20 = 0.0f, m30 = 0.0f,
                m01 = 0.0f, m11 =   sy, m21 = 0.0f, m31 = 0.0f,
                m02 = 0.0f, m12 = 0.0f, m22 = 1.0f, m32 = 0.0f,
                m03 = 0.0f, m13 = 0.0f, m23 = 0.0f, m33 = 1.0f
            };

            var translationMatrix = new Matrix4x4()
            {
                m00 = 1.0f, m10 = 0.0f, m20 = 0.0f, m30 = 0.0f,
                m01 = 0.0f, m11 = 1.0f, m21 = 0.0f, m31 = 0.0f,
                m02 = 0.0f, m12 = 0.0f, m22 = 1.0f, m32 = 0.0f,
                m03 =   tx, m13 =   ty, m23 = 0.0f, m33 = 1.0f
            };

            var rotationMatrix = new Matrix4x4()
            {
                m00 =   rc, m10 =  -rs, m20 = 0.0f, m30 = 0.0f,
                m01 =   rs, m11 =   rc, m21 = 0.0f, m31 = 0.0f,
                m02 = 0.0f, m12 = 0.0f, m22 = 1.0f, m32 = 0.0f,
                m03 = 0.0f, m13 = 0.0f, m23 = 0.0f, m33 = 1.0f
            };
            return (translationMatrix
                    * scaleMatrix)
                    * rotationMatrix;
            /*/
            return new Matrix4x4()
            {
                m00 = -sx * rc, m10 = sy * -rs, m20 = 0, m30 = 0,
                m01 = -sx * rs, m11 = sy *  rc, m21 = 0, m31 = 0,
                m02 = 0,        m12 = 0,        m22 = 1, m32 = 0,
                m03 = tx,       m13 = ty,       m23 = 0, m33 = 1
            };
            //*/
        }
    }

}
