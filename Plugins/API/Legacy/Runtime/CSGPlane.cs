using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using InternalRealtimeCSG;
using Vector3 = UnityEngine.Vector3;
using Vector4 = UnityEngine.Vector4;
using Matrix4x4 = UnityEngine.Matrix4x4;
using Ray = UnityEngine.Ray;
using Quaternion = UnityEngine.Quaternion;
using Mathf = UnityEngine.Mathf;

namespace RealtimeCSG.Legacy
{
    /// <summary>A geometric plane</summary>
    /// <remarks><note>This code will be replaced by [UnityEngine.Plane](https://docs.unity3d.com/ScriptReference/Plane.html) eventually. In older versions of Realtime-CSG [UnityEngine.Plane](https://docs.unity3d.com/ScriptReference/Plane.html), Unity's own Plane, was not serializable and wasn't an option.</note>
    /// <note>This class can be safely serialized.</note></remarks>
    /// <note>This plane class uses the plane equation ax+by+cz-d=0, while uses [UnityEngine.Plane](https://docs.unity3d.com/ScriptReference/Plane.html) ax+by+cz+d=0</note>
    [Serializable, StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
    public struct CSGPlane
    {
        /// <value>a in the plane equation: ax+by+cz-d=0</value>
        public float a;
        /// <value>b in the plane equation: ax+by+cz-d=0</value>
        public float b;
        /// <value>c in the plane equation: ax+by+cz-d=0</value>
        public float c;
        /// <value>d in the plane equation: ax+by+cz-d=0</value>
        public float d;
        
        /// <value>Gets or sets the normal of this plane</value>
        public Vector3 normal
        {
            get { return new Vector3(a, b, c); }
            set { a = value.x; b = value.y; c = value.z; } 
        }
        
        /// <value>Point on the plane</value>
        public Vector3 pointOnPlane { get { return normal * d; } }

        #region Constructors
        /// <summary>Create a plane with a normal and the distance to the origin</summary>
        /// <param name="inNormal">The normal of the plane</param>
        /// <param name="inD">The distance to the origin</param>
        public CSGPlane(UnityEngine.Plane plane)
        {
            a = plane.normal.x;
            b = plane.normal.y;
            c = plane.normal.z;
            d = -plane.distance;
        }

        /// <summary>Create a plane with a normal and the distance to the origin</summary>
        /// <param name="inNormal">The normal of the plane</param>
        /// <param name="inD">The distance to the origin</param>
        public CSGPlane(Vector3 inNormal, float inD)
        {
            var normal = inNormal.normalized;
            a = normal.x;
            b = normal.y;
            c = normal.z;
            d = inD;
        }
        
        /// <summary>Create a plane with a point and a normal</summary>
        /// <param name="inNormal">The normal of the plane</param>
        /// <param name="pointOnPlane">The position of the plane</param>
        public CSGPlane(Vector3 inNormal, Vector3 pointOnPlane)
        {
            var normal = inNormal.normalized;
            a = normal.x;
            b = normal.y;
            c = normal.z;
            d = Vector3.Dot(normal, pointOnPlane);
        }

        /// <summary>Create a plane with a point and a quaternion</summary>
        /// <param name="inRotation">The orientation of the plane</param>
        /// <param name="pointOnPlane">The position of the plane</param>
        public CSGPlane(UnityEngine.Quaternion inRotation, Vector3 pointOnPlane)
        {
            var normal	= (inRotation * MathConstants.upVector3).normalized;
            a	= normal.x;
            b	= normal.y;
            c	= normal.z;
            d	= Vector3.Dot(normal, pointOnPlane);
        }

        /// <summary>Create a plane by giving the plane equation ax+by+cz-d=0.</summary>
        /// <param name="inA">a in the plane equation: ax+by+cz-d=0</param>
        /// <param name="inB">b in the plane equation: ax+by+cz-d=0</param>
        /// <param name="inC">c in the plane equation: ax+by+cz-d=0</param>
        /// <param name="inD">d in the plane equation: ax+by+cz-d=0</param>
        public CSGPlane(float inA, float inB, float inC, float inD)
        {
            a = inA;
            b = inB;
            c = inC;
            d = inD;
            Normalize();
        } 

        /// <summary>Calculate a plane from 3 points</summary>
        /// <param name="point1">First point</param>
        /// <param name="point2">Second point</param>
        /// <param name="point3">Third point</param>
        public CSGPlane(Vector3 point1, Vector3 point2, Vector3 point3)
        {
            var ab = (point2 - point1);
            var ac = (point3 - point1);

            var normal = Vector3.Cross(ab, ac).normalized;

            a = normal.x;
            b = normal.y;
            c = normal.z;
            d = Vector3.Dot(normal, point1);
        }

        #endregion

        #region Ray Intersection
        /// <summary>Calculate  an intersection with a given ray and the plane.</summary>
        /// <param name="ray">The ray to find an intersection with</param>
        /// <returns>The intersection point.</returns>
        public Vector3 RayIntersection(UnityEngine.Ray ray)
        {
            var start_x			= (double)ray.origin.x;
            var start_y			= (double)ray.origin.y;
            var start_z			= (double)ray.origin.z;

            var direction_x		= (double)ray.direction.x;
            var direction_y		= (double)ray.direction.y;
            var direction_z		= (double)ray.direction.z;

            var distanceA	= (a * start_x) +
                              (b * start_y) +
                              (c * start_z) -
                              (d);
            var length		= (a * direction_x) +
                              (b * direction_y) +
                              (c * direction_z);
            var delta		= distanceA / length;

            var x = start_x - (delta * direction_x);
            var y = start_y - (delta * direction_y);
            var z = start_z - (delta * direction_z);

            return new Vector3((float)x, (float)y, (float)z);
        }
        
        /// <summary>Attempt to find an intersection with a given ray and the plane.</summary>
        /// <param name="ray">The ray to find an intersection with</param>
        /// <param name="intersection">The intersection point, if found</param>
        /// <returns><b>true</b> if we found an intersection, <b>false</b> if we didn't.</returns>
        public bool TryRayIntersection(UnityEngine.Ray ray, out Vector3 intersection)
        {
            var start		= ray.origin;
            var end			= ray.origin + ray.direction * 1000.0f;
            var distanceA	= Distance(start);
            if (float.IsInfinity(distanceA) || float.IsNaN(distanceA))
            {
                intersection = MathConstants.zeroVector3;
                return false;
            }
            var distanceB	= Distance(end);
            if (float.IsInfinity(distanceB) || float.IsNaN(distanceB))
            {
                intersection = MathConstants.zeroVector3;
                return false;
            } 
            
            Vector3 vector	= end - start;
            float length	= distanceB - distanceA;
            float delta		= distanceB / length;
            intersection = end - (delta * vector);
            if (float.IsInfinity(intersection.x) || float.IsNaN(intersection.x) ||
                float.IsInfinity(intersection.y) || float.IsNaN(intersection.y) ||
                float.IsInfinity(intersection.z) || float.IsNaN(intersection.z))
            {
                intersection = MathConstants.zeroVector3;
                return false;
            }
            return true;
        }

        /// <summary>Calculates an intersection between a line and the plane</summary>
        /// <param name="start">Start of the line</param>
        /// <param name="end">End of the line</param>
        /// <returns>Intersection point</returns>
        public Vector3 LineIntersection(Vector3 start, Vector3 end)
        {
            Vector3 vector	= end - start;
            float edist		= Distance(end);
            float sdist		= Distance(start);
            float length	= edist - sdist;
            float delta		= edist / length;

            return end - (delta * vector);
        }
        
        /// <summary>Calculates the intersection point between 3 planes</summary>
        /// <param name="inPlane1">The first plane</param>
        /// <param name="inPlane2">The second plane</param>
        /// <param name="inPlane3">The third plane</param>
        /// <returns>The intersection point between the 3 planes.The returned point will consist out NaN values when there is no intersection.</returns>
        static public Vector3 Intersection(CSGPlane inPlane1,
                                           CSGPlane inPlane2,
                                           CSGPlane inPlane3)
        {
            try
            {
                var plane1a = (decimal)inPlane1.a;
                var plane1b = (decimal)inPlane1.b;
                var plane1c = (decimal)inPlane1.c;
                var plane1d = (decimal)inPlane1.d;

                var plane2a = (decimal)inPlane2.a;
                var plane2b = (decimal)inPlane2.b;
                var plane2c = (decimal)inPlane2.c;
                var plane2d = (decimal)inPlane2.d;
            
                var plane3a = (decimal)inPlane3.a;
                var plane3b = (decimal)inPlane3.b;
                var plane3c = (decimal)inPlane3.c;
                var plane3d = (decimal)inPlane3.d;
                        
            
                var bc1 = (plane1b * plane3c) - (plane3b * plane1c);
                var bc2 = (plane2b * plane1c) - (plane1b * plane2c);
                var bc3 = (plane3b * plane2c) - (plane2b * plane3c);

                var w = -((plane1a * bc3) + (plane2a * bc1) + (plane3a * bc2));
                
                var ad1 = (plane1a * plane3d) - (plane3a * plane1d);
                var ad2 = (plane2a * plane1d) - (plane1a * plane2d);
                var ad3 = (plane3a * plane2d) - (plane2a * plane3d);

                var x = -((plane1d * bc3) + (plane2d * bc1) + (plane3d * bc2));
                var y = -((plane1c * ad3) + (plane2c * ad1) + (plane3c * ad2));
                var z = +((plane1b * ad3) + (plane2b * ad1) + (plane3b * ad2));

                x /= w;
                y /= w;
                z /= w;
            
                var result = new Vector3( (float)x, (float)y, (float)z);
                if (float.IsNaN(result.x) || float.IsInfinity(result.x) ||
                    float.IsNaN(result.y) || float.IsInfinity(result.y) ||
                    float.IsNaN(result.z) || float.IsInfinity(result.z))
                {
                    return MathConstants.NaNVector3;
                }

                return result;
            }
            catch
            {
                return MathConstants.NaNVector3;
            }
        }
        #endregion
        
        /// <summary>Calculates the distance to a point to this plane</summary>
        /// <param name="point">A point</param>
        /// <returns>The distance from the point to the plane</returns>
        public float Distance(Vector3 point)
        {
            return
                (
                    (a * point.x) +
                    (b * point.y) +
                    (c * point.z) -
                    (d)
                );
        }

        /// <summary>Normalize the normal of the plane</summary>
        public void Normalize()
        {
            var magnitude = 1.0f / Mathf.Sqrt((a * a) + (b * b) + (c * c));
            a *= magnitude;
            b *= magnitude;
            c *= magnitude;
            d *= magnitude;
        }		
        
        /// <summary>Transform the plane with the given transformation</summary>
        /// <param name="transformation">The transformation to transform the plane with</param>
        public void Transform(UnityEngine.Matrix4x4 transformation)
        {
            var ittrans = transformation.inverse.transpose;
            var vector = ittrans * new Vector4(this.a, this.b, this.c, -this.d);
            this.a =  vector.x;
            this.b =  vector.y;
            this.c =  vector.z;
            this.d = -vector.w;
        }
        
        /// <summary>Creates a copy of this plane that has its direction inverted</summary>
        /// <returns>A new plane that is flipped</returns>
        public CSGPlane Negated() { return new CSGPlane(-a, -b, -c, -d); }
        
        /// <summary>Translates a plane in space</summary>
        /// <param name="translation">A vector to translate the plane over</param>
        /// <returns>A plane translated by <paramref name="translation"/></returns>
        public CSGPlane Translated(Vector3 translation)
        {
            return new CSGPlane(a, b, c,
                // translated offset = Normal.Dotproduct(translation)
                // normal = A,B,C
                                d + (a * translation.x) +
                                    (b * translation.y) +
                                    (c * translation.z));
        }
        
        /// <summary>Project a point on this plane</summary>
        /// <param name="point">A point</param>
        /// <returns>The projected point</returns>
        public Vector3 Project(Vector3 point)
        {
            float px = point.x;
            float py = point.y;
            float pz = point.z;

            float nx = normal.x;
            float ny = normal.y;
            float nz = normal.z;

            float ax  = (px - (nx * d)) * nx;
            float ay  = (py - (ny * d)) * ny;
            float az  = (pz - (nz * d)) * nz;
            float dot = ax + ay + az;

            float rx = px - (dot * nx);
            float ry = py - (dot * ny);
            float rz = pz - (dot * nz);

            return new Vector3(rx, ry, rz);
        }
        
        #region Equality
        public override int GetHashCode()
        {
            return a.GetHashCode() ^
                    b.GetHashCode() ^
                    c.GetHashCode() ^
                    d.GetHashCode();
        }

        public bool Equals(CSGPlane other)
        {
            if (System.Object.ReferenceEquals(this, other))
                return true;
            if (System.Object.ReferenceEquals(other, null))
                return false;
            return	Mathf.Abs(this.Distance(other.pointOnPlane)) <= MathConstants.DistanceEpsilon &&
                    Mathf.Abs(other.Distance(this.pointOnPlane)) <= MathConstants.DistanceEpsilon &&
                    Mathf.Abs(a - other.a) <= MathConstants.NormalEpsilon &&
                    Mathf.Abs(b - other.b) <= MathConstants.NormalEpsilon &&
                    Mathf.Abs(c - other.c) <= MathConstants.NormalEpsilon;
        }

        public override bool Equals(object obj)
        {
            if (System.Object.ReferenceEquals(this, obj))
                return true;
            if (!(obj is CSGPlane))
                return false;
            CSGPlane other = (CSGPlane)obj;
            if (System.Object.ReferenceEquals(other, null))
                return false;
            return	Mathf.Abs(this.Distance(other.pointOnPlane)) <= MathConstants.DistanceEpsilon &&
                    Mathf.Abs(other.Distance(this.pointOnPlane)) <= MathConstants.DistanceEpsilon &&
                    Mathf.Abs(a - other.a) <= MathConstants.NormalEpsilon &&
                    Mathf.Abs(b - other.b) <= MathConstants.NormalEpsilon &&
                    Mathf.Abs(c - other.c) <= MathConstants.NormalEpsilon;
        }

        public static bool operator ==(CSGPlane left, CSGPlane right)
        {
            if (System.Object.ReferenceEquals(left, right))
                return true;
            if (System.Object.ReferenceEquals(left, null) ||
                System.Object.ReferenceEquals(right, null))
                return false;
            return	Mathf.Abs(left.Distance(right.pointOnPlane)) <= MathConstants.DistanceEpsilon &&
                    Mathf.Abs(right.Distance(left.pointOnPlane)) <= MathConstants.DistanceEpsilon &&
                    Mathf.Abs(left.a - right.a) <= MathConstants.NormalEpsilon &&
                    Mathf.Abs(left.b - right.b) <= MathConstants.NormalEpsilon &&
                    Mathf.Abs(left.c - right.c) <= MathConstants.NormalEpsilon;
        }

        public static bool operator !=(CSGPlane left, CSGPlane right)
        {
            if (System.Object.ReferenceEquals(left, right))
                return false;
            if (System.Object.ReferenceEquals(left, null) ||
                System.Object.ReferenceEquals(right, null))
                return true;
            return	Mathf.Abs(left.Distance(right.pointOnPlane)) > MathConstants.DistanceEpsilon &&
                    Mathf.Abs(right.Distance(left.pointOnPlane)) > MathConstants.DistanceEpsilon &&
                    Mathf.Abs(left.a - right.a) > MathConstants.NormalEpsilon ||
                    Mathf.Abs(left.b - right.b) > MathConstants.NormalEpsilon ||
                    Mathf.Abs(left.c - right.c) > MathConstants.NormalEpsilon;
        }
        #endregion

        public override string ToString() { return string.Format(CultureInfo.InvariantCulture, "({0}, {1}, {2}, {3})", a,b,c,d); }
    }
}
