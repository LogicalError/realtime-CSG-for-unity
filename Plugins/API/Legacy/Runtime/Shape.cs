using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace RealtimeCSG.Legacy
{
    /// <summary>Defines the <see cref="RealtimeCSG.Legacy.Surface"/>s and its properties for a <see cref="RealtimeCSG.Legacy.ControlMesh"/>.</summary>
    /// <remarks><note>This code is legacy and will be removed eventually.</note></remarks>
    /// <seealso cref="RealtimeCSG.Legacy.ControlMesh"/>
    /// <seealso cref="RealtimeCSG.Legacy.Surface"/>
    /// <seealso cref="RealtimeCSG.Legacy.TexGen"/>
    /// <seealso cref="RealtimeCSG.Legacy.TexGenFlags"/>
    [Serializable]
    public sealed class Shape
    {
        /// <value>Version of the Shape</value>
        [SerializeField] public float           Version         = 1.0f;
        
        /// <value>An array of <see cref="RealtimeCSG.Legacy.Surface"/>s, one for each polygon.</value>
        /// <remarks><see cref="RealtimeCSG.Legacy.Surface"/> are used to store information about the orientation of the polygons that are associated with this surface.</remarks>
        [SerializeField] public Surface[]		Surfaces		= new Surface[0];
        
        /// <value>An array of <see cref="RealtimeCSG.Legacy.TexGen"/>s, one for each polygon.</value>
        /// <remarks><see cref="RealtimeCSG.Legacy.TexGen"/> are used to generate texture coordinates for the polygons that are associated with this surface.</remarks>
        [SerializeField] public TexGen[]		TexGens			= new TexGen[0];
        
        /// <value>An array of <see cref="RealtimeCSG.Legacy.TexGenFlags"/>s, one for each polygon.</value>
        /// <remarks><see cref="RealtimeCSG.Legacy.TexGenFlags"/> are used to define if a polygon is visible, part of a collision mesh etc.</remarks>
        [SerializeField] public TexGenFlags[]	TexGenFlags		= new TexGenFlags[0];

        // Do not use this anymore, it's deprecated
        [FormerlySerializedAs("LegacyMaterials")]
        [Obsolete("Materials are now stored in TexGens")]
        [HideInInspector]
        [SerializeField] internal Material[]	Materials		= new Material[0];

#if UNITY_EDITOR
        /// <summary>Constructor for an unintialized <see cref="RealtimeCSG.Legacy.Shape"/></summary>
        public Shape() { }
        
        /// <summary>Constructor that takes a <see cref="RealtimeCSG.Legacy.Shape"/> to copy from</summary>
        public Shape(Shape other)
        {
            CopyFrom(other);
        }
        
        /// <summary>Creates a <see cref="RealtimeCSG.Legacy.Shape"/> and initializes it with <paramref name="polygonCount"/> number of surfaces</summary>
        /// <param name="polygonCount">The number of surfaces to create for this <see cref="RealtimeCSG.Legacy.Shape"/></param>
        public Shape(int polygonCount)
        {
            Surfaces	= new Surface[polygonCount];
            TexGenFlags = new TexGenFlags[polygonCount];
            TexGens		= new TexGen[polygonCount];
        }

        /// <summary>Clear all surfaces in this <see cref="RealtimeCSG.Legacy.Shape"/></summary>
        public void Reset()
        {
            Surfaces		= new Surface[0];
            TexGens			= new TexGen[0];
            TexGenFlags		= new TexGenFlags[0];
        } 
        
        /// <summary>Copy all contents from another <see cref="RealtimeCSG.Legacy.Shape"/> into this <see cref="RealtimeCSG.Legacy.Shape"/></summary>
        /// <param name="other">The <see cref="RealtimeCSG.Legacy.Shape"/> to copy the contents from.</param>
        public void CopyFrom(Shape other)
        {
            if (other == null)
            {
                Reset();
                return;
            }

            if (this.Surfaces != null)
            {
                if (this.Surfaces == null || this.Surfaces.Length != other.Surfaces.Length)
                    this.Surfaces = new Surface[other.Surfaces.Length];
                Array.Copy(other.Surfaces, this.Surfaces, other.Surfaces.Length);
            } else
                this.Surfaces = null;
            
            if (this.TexGens != null)
            {
                if (this.TexGens == null || this.TexGens.Length != other.TexGens.Length)
                    this.TexGens = new TexGen[other.TexGens.Length];
                Array.Copy(other.TexGens, this.TexGens, other.TexGens.Length);
            } else
                this.TexGens = null;

            if (this.TexGenFlags != null)
            {
                if (this.TexGenFlags == null || this.TexGenFlags.Length != other.TexGenFlags.Length)
                    this.TexGenFlags = new TexGenFlags[other.TexGenFlags.Length];
                Array.Copy(other.TexGenFlags, this.TexGenFlags, other.TexGenFlags.Length);
            } else
                this.TexGenFlags = null;
        }
        
        /// <summary>Creates a copy of this <see cref="RealtimeCSG.Legacy.Shape"/>.</summary>
        /// <returns>The copy of this <see cref="RealtimeCSG.Legacy.Shape"/></returns>
        public Shape Clone() { return new Shape(this); }

        /// <summary>Validates if the materials are correct on this <see cref="RealtimeCSG.Legacy.Shape"/>, and fixes them if they are not.</summary>
        /// <returns>*true* if the shape has been modified, *false* if nothing changed</returns>
        public bool CheckMaterials()
        {
            bool dirty = false;
            if (Surfaces == null ||
                Surfaces.Length == 0)
            {
                Debug.LogWarning("Surfaces == null || Surfaces.Length == 0");
                return true;
            }
            
            int maxTexGenIndex = 0;
            for (int i = 0; i < Surfaces.Length; i++)
            {
                maxTexGenIndex = Mathf.Max(maxTexGenIndex, Surfaces[i].TexGenIndex);
            }
            maxTexGenIndex++;

            if (TexGens == null ||
                TexGens.Length < maxTexGenIndex)
            {
                dirty = true;
                var newTexGens = new TexGen[maxTexGenIndex];
                var newTexGenFlags = new TexGenFlags[maxTexGenIndex];
                if (TexGens != null &&
                    TexGens.Length > 0)
                {
                    for (int i = 0; i < TexGens.Length; i++)
                    {
                        newTexGens[i] = TexGens[i];
                        newTexGenFlags[i] = TexGenFlags[i];
                    }/*
                    for (int i = TexGens.Length; i < newTexGens.Length; i++)
                    {
                        newTexGens[i].Color = Color.white;
                    }*/
                }
                TexGens = newTexGens;
                TexGenFlags = newTexGenFlags;
            }
            /*
            for (int i = 0; i < TexGens.Length; i++)
            {
                if (TexGens[i].Color == Color.clear)
                    TexGens[i].Color = Color.white;
            }*/

            return dirty;
        }

#endif
    } 
}
 