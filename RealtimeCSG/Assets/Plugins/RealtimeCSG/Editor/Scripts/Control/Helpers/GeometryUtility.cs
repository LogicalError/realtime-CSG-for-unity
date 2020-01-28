using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using InternalRealtimeCSG;
using RealtimeCSG.Legacy;
using RealtimeCSG.Components;

namespace RealtimeCSG
{
    internal enum PrincipleAxis
    {
        X,Y,Z
    }

    internal enum PrincipleAxis2
    {
        XY, XZ, YZ
    }

    internal static class GeometryUtility
    {
        public static float ProjectedSignedMagnitudeOnAxis(Vector3 vector, Vector3 axis)
        {
            var projected = Vector3.Project(vector, axis);			
            return projected.magnitude * Mathf.Sign(Vector3.Dot(projected, axis));
        }
        
        public static float ProjectedSignedMagnitudeOnAxis(Vector3 vector1, Vector3 vector2, Vector3 axis)
        {
            var projected1 = Vector3.Project(vector1, axis);
            var projected2 = Vector3.Project(vector2, axis);

            var magnitude1 = projected1.magnitude;
            var magnitude2 = projected2.magnitude;
            if (magnitude1 > magnitude2)
                return magnitude1 * Mathf.Sign(Vector3.Dot(projected1, axis));
            else
                return magnitude2 * Mathf.Sign(Vector3.Dot(projected2, axis));
        }

        public static float SignedAngle(Vector2 v1, Vector2 v2)
        {
            //  Acute angle [0,180]
            var angle = Vector2.Angle(v1, v2);

            //  -Acute angle [180,-179]
            var sign = Mathf.Sign(Vector3.Dot(MathConstants.forwardVector3, Vector3.Cross(v1, v2)));
            var signedAngle = angle * sign;

            //  360 angle
            return signedAngle;
        }



        static double Angle2D(Vector2 from, Vector2 to)
        {
            var denominator = System.Math.Sqrt(from.sqrMagnitude * to.sqrMagnitude);
            if (denominator < 1e-15F)
                return 0F;

            var dot = (from.x * to.x + from.y * to.y) / denominator;
            if (dot < -1) dot = -1;
            if (dot >  1) dot = 1;
            return System.Math.Acos(dot) * (360.0 / (System.Math.PI * 2.0));
        }

        public static float SignedAngle2D(Vector2 from, Vector2 to)
        {
            var unsignedAngle   = Angle2D(from, to);
            var cross_z         = from.x * to.y - from.y * to.x;
            var sign            = System.Math.Sign(cross_z);
            return (float)(unsignedAngle * sign);
        }
        /*

        static double Angle(Vector3 from, Vector3 to)
        {
            var denominator = System.Math.Sqrt(from.sqrMagnitude * to.sqrMagnitude);
            if (denominator < 1e-15F)
                return 0F;

            var dot = (from.x * to.x + from.y * to.y + from.z * to.z) / denominator;
            if (dot < -1) dot = -1;
            if (dot >  1) dot =  1;
            return System.Math.Acos(dot) * (360.0 / System.Math.PI * 2.0);
        }

        public static float SignedAngle(Vector3 from, Vector3 to, Vector3 axis)
        {
            var unsignedAngle = Angle(from, to);

            var cross_x = from.y * to.z - from.z * to.y;
            var cross_y = from.z * to.x - from.x * to.z;
            var cross_z = from.x * to.y - from.y * to.x;
            var sign = System.Math.Sign(axis.x * cross_x + axis.y * cross_y + axis.z * cross_z);
            return (float)(unsignedAngle * sign);
        }
        /*/

        public static float SignedAngle(Vector3 v1, Vector3 v2, Vector3 n)
        {
            //  Acute angle [0,180]
            var angle = Vector3.Angle(v1, v2);

            //  -Acute angle [180,-179]
            var sign = Mathf.Sign(Vector3.Dot(n, Vector3.Cross(v1, v2)));
            var signedAngle = angle * sign;

            //  360 angle
            return signedAngle;
        }
        //*/

        public static Vector3 ProjectPointOnPlane(CSGPlane plane, Vector3 point)
        { 
             var distance = -Vector3.Dot(plane.normal, (point - plane.pointOnPlane));
             return point + plane.normal * distance;
        }


        public static float CounterClockwise(Vector2 a, Vector2 b, Vector2 c)
        {
            return ((b.x - a.x) * (c.y - a.y) - (c.x - a.x) * (b.y - a.y));
        }

        public static bool Intersects(Vector2 A1, Vector2 A2, Vector2 B1, Vector2 B2)
        {
           if (CounterClockwise(A1, A2, B1) * CounterClockwise(A1, A2, B2) > 0) return false;
           if (CounterClockwise(B1, B2, A1) * CounterClockwise(B1, B2, A2) > 0) return false;
           return true;
        }

        public static bool TryIntersection(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, out Vector2 intersection)
        {
            var s1 = p1 - p0;
            var s2 = p3 - p2;
            var s3 = p0 - p2;

            var divisor = -s2.x * s1.y + s1.x * s2.y;

            float s, t;
            s = (-s1.y * s3.x + s1.x * s3.y) / divisor;
            t = ( s2.x * s3.y - s2.y * s3.x) / divisor;

            if (s >=     MathConstants.EqualityEpsilon && 
                s <= 1 - MathConstants.EqualityEpsilon && 
                t >=     MathConstants.EqualityEpsilon && 
                t <= 1 - MathConstants.EqualityEpsilon)
            {
                // Collision detected
                intersection = p0 + (t * s1);

                //if ( (intersection - p0).sqrMagnitude > MathConstants.EqualityEpsilonSqr
                    //   (intersection - p1).sqrMagnitude > MathConstants.EqualityEpsilonSqr 
                    //&& (intersection - p2).sqrMagnitude > MathConstants.EqualityEpsilonSqr 
                    //&& (intersection - p3).sqrMagnitude > MathConstants.EqualityEpsilonSqr
                    //)
                    return true;
            }
            
            intersection = MathConstants.zeroVector2;
            return false; // No collision
        }

        /*
        public static bool TryIntersection(Vector2 A1, Vector2 A2, Vector2 B1, Vector2 B2, out Vector2 intersection)
        {
            var deltaYa		= A2.y - A1.y;
            var deltaXa		= A1.x - A2.x;
            var dotA		= deltaYa * A1.x + deltaXa * A1.y;
            
            var deltaYb		= B2.y - B1.y;
            var deltaXb		= B1.x - B2.x;
            var dotB		= deltaYb * B1.x + deltaXb * B1.y;
            
            var magnitude	= deltaYa * deltaXb - deltaYb * deltaXa;
            if (magnitude <=     MathConstants.EqualityEpsilon)
            {
                intersection = MathConstants.zeroVector2;
                return false;
            }


            // now return the Vector2 intersection point
            intersection = new Vector2( (deltaXb * dotA - deltaXa * dotB) / magnitude, 
                                        (deltaYa * dotB - deltaYb * dotA) / magnitude);
            

            if ((intersection - A1).sqrMagnitude < MathConstants.EqualityEpsilonSqr ||
                (intersection - A2).sqrMagnitude < MathConstants.EqualityEpsilonSqr ||
                (intersection - B1).sqrMagnitude < MathConstants.EqualityEpsilonSqr ||
                (intersection - B2).sqrMagnitude < MathConstants.EqualityEpsilonSqr)
            {
                intersection = MathConstants.zeroVector2;
                return false;
            }

            return true;
        }*/

        private struct Line { public int Index0, Index1; }


        public static bool SelfIntersecting(List<Vector2> points)
        {
            var lines = new Line[points.Count];
            for (int i = points.Count - 1, j = 0; j < points.Count; i = j, j++)
            {
                lines[j].Index0 = i;
                lines[j].Index1 = j;
            }

            for (var i = 0; i < lines.Length - 2; i++)
            {
                for (var j = i + 1; j < lines.Length - 1; j++)
                {
                    if (lines[i].Index1 == lines[j].Index0 ||
                        lines[i].Index0 == lines[j].Index1)
                        continue;

                    if (Intersects(points[lines[i].Index0], points[lines[i].Index1], 
                                   points[lines[j].Index0], points[lines[j].Index1]))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool SelfIntersecting(Vector2[] points)
        {
            var lines = new Line[points.Length];
            for (int i = points.Length - 1, j = 0; j < points.Length; i = j, j++)
            {
                lines[j].Index0 = i;
                lines[j].Index1 = j;
            }

            for (var i = 0; i < lines.Length - 2; i++)
            {
                for (var j = i + 1; j < lines.Length - 1; j++)
                {
                    if (lines[i].Index1 == lines[j].Index0 ||
                        lines[i].Index0 == lines[j].Index1)
                        continue;

                    if (Intersects(points[lines[i].Index0], points[lines[i].Index1], 
                                   points[lines[j].Index0], points[lines[j].Index1]))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static Vector2[] RotatePlaneTo2D(Matrix4x4 extraMatrix, Vector3[] points, CSGPlane plane)
        {
            var mat = RotatePlaneTo2DMatrix(plane) * extraMatrix;

            var points2D = new Vector2[points.Length];
            for (var i = 0; i < points2D.Length; i++)
            {
                var point = mat.MultiplyPoint(points[i]);
                points2D[i].x = point.x;
                points2D[i].y = point.z;
            }
            return points2D;
        }

        public static Vector2[] RotatePlaneTo2D(Vector3[] points, CSGPlane plane)
        {
            var mat = RotatePlaneTo2DMatrix(plane);
            
            var points2D = new Vector2[points.Length];
            for (var i = 0; i < points2D.Length; i++)
            {
                var point = mat.MultiplyPoint(points[i]);
                points2D[i].x = point.x;
                points2D[i].y = point.z;
            }
            return points2D;
        }

        public static Vector3 RotatePointIntoPlaneSpace(CSGPlane plane, Vector3 point)
        {
            var mat = RotatePlaneTo2DMatrix(plane);
            return mat.MultiplyPoint(point);
        }

        public static Matrix4x4 RotatePlaneTo2DMatrix(CSGPlane plane)
        {
            return Matrix4x4.TRS(MathConstants.zeroVector3, Quaternion.FromToRotation(plane.normal, MathConstants.upVector3), MathConstants.oneVector3);
        }
        public static Matrix4x4 RotateSurfaceTo2DMatrix(Surface surface)
        {
            var from = Quaternion.LookRotation(surface.BiNormal, -surface.Plane.normal);
            return Matrix4x4.TRS(MathConstants.zeroVector3, Quaternion.Inverse(from), MathConstants.oneVector3);
        }
        
        public static Matrix4x4 Rotate2DToPlaneMatrix(CSGPlane plane)
        {
            return RotatePlaneTo2DMatrix(plane).inverse;
            //return   Matrix4x4.TRS(plane.pointOnPlane, MathConstants.identityQuaternion, MathConstants.oneVector3)
            //	   * Matrix4x4.TRS(MathConstants.zeroVector3, Quaternion.FromToRotation(MathConstants.upVector3, plane.normal), MathConstants.oneVector3);
        }
        
        public static void RotateTransform2DToPlane(CSGPlane plane, Vector3 origin, Transform transform)
        {
            // rotate upward shape into direction of plane
            transform.rotation = Quaternion.FromToRotation(MathConstants.upVector3, plane.normal);
            transform.position = origin;
        }

        public static Vector3[] Rotate2DToPlane(Vector2[] points2D, CSGPlane plane)
        {
            var mat = Rotate2DToPlaneMatrix(plane);
            var points = new Vector3[points2D.Length];
            for (var i = 0; i < points2D.Length; i++)
            {
                var input = new Vector3(points2D[i].x, 0, points2D[i].y);
                var point = mat.MultiplyPoint(input);
                points[i] = ProjectPointOnPlane(plane, point);
            }
            return points;
        }

        public static Vector3 Rotate2DToPlane(Vector2 point2D, CSGPlane plane)
        {
            var mat = Rotate2DToPlaneMatrix(plane);
            var input = new Vector3(point2D.x, 0, point2D.y);
            var point = mat.MultiplyPoint(input);
            return ProjectPointOnPlane(plane, point);
        }

        public static Vector3[] ToVector3XZ(List<Vector2> points2D)
        {
            var points = new Vector3[points2D.Count];
            for (var i = 0; i < points2D.Count; i++)
            {
                points[i].x = points2D[i].x;
                points[i].z = points2D[i].y;
            }
            return points;
        }
        public static Vector3[] ToVector3XZReversed(List<Vector2> points2D)
        {
            var points = new Vector3[points2D.Count];
            
            for (int i = 0, last = points2D.Count - 1; i <= last; i++)
            {
                points[last - i].x = points2D[i].x;
                points[last - i].z = points2D[i].y;
            }
            return points;
        }

        public static Vector3[] ToVector3XZ(Vector2[] points2D)
        {
            var points = new Vector3[points2D.Length];
            for (var i = 0; i < points2D.Length; i++)
            {
                points[i].x = points2D[i].x;
                points[i].z = points2D[i].y;
            }
            return points;
        }

        public static float CleanLength(float length)
        {
            var intLength	= Mathf.FloorToInt(length);

            var fractLength	= (length - intLength);

            fractLength = Mathf.Round(fractLength * 1000.0f) / 1000.0f;

            const float epsilon = MathConstants.EqualityEpsilon;

            if (fractLength >=    - epsilon && 
                fractLength <       epsilon) fractLength = 0;			
            if (fractLength >=  1 - epsilon) fractLength = 1;			
            if (fractLength <= -1 + epsilon) fractLength = -1;
            
            return intLength + fractLength;
            //return new Vector3(intPosX, intPosY, intPosZ);
        }

        public static bool IsNonConvex(List<Vector2> points)
        {
            for (int i = points.Count - 2, j = points.Count - 1, k = 0; k < points.Count; i = j, j = k, k++)
            {
                if (CounterClockwise(points[i],points[j],points[k]) < 0)
                {
                    return true;
                }
            }
            return false;
        }

        public static CSGPlane CalcPolygonPlane(Vector3[] points)
        {
            if (points.Length == 3)
            {
                var v0 = points[0];
                var v1 = points[1];
                var v2 = points[2];

                return new CSGPlane(v0, v1, v2);
            }

            // newell's method to calculate a normal for a concave polygon
            var normal = MathConstants.zeroVector3;
            var prevIndex = points.Length - 1;
            var prevVertex = points[prevIndex];
            for (var e = 0; e < points.Length; e++)
            {
                var currVertex = points[e];
                normal.x = normal.x + ((prevVertex.y - currVertex.y) * (prevVertex.z + currVertex.z));
                normal.y = normal.y + ((prevVertex.z - currVertex.z) * (prevVertex.x + currVertex.x));
                normal.z = normal.z + ((prevVertex.x - currVertex.x) * (prevVertex.y + currVertex.y));

                prevVertex = currVertex;
            }
            normal = normal.normalized;

            var d = 0.0f;
            var count = 0;
            for (var e = 0; e < points.Length; e++)
            {
                var currVertex = points[e];
                d += Vector3.Dot(normal, currVertex);
                count++;
            }
            d /= count;

            return new CSGPlane(normal, d);
        }

        public static CSGPlane CalcPolygonPlane(Vector3[] points, int[] indices)
        {
            if (indices.Length == 3)
            {
                var v0 = points[indices[0]];
                var v1 = points[indices[1]];
                var v2 = points[indices[2]];

                return new CSGPlane(v0, v1, v2);
            }

            // newell's method to calculate a normal for a concave polygon
            var normal = MathConstants.zeroVector3;
            var prevIndex = indices.Length - 1;
            var prevVertex = points[indices[prevIndex]];
            for (var e = 0; e < indices.Length; e++)
            {
                var currVertex = points[indices[e]];
                normal.x = normal.x + ((prevVertex.y - currVertex.y) * (prevVertex.z + currVertex.z));
                normal.y = normal.y + ((prevVertex.z - currVertex.z) * (prevVertex.x + currVertex.x));
                normal.z = normal.z + ((prevVertex.x - currVertex.x) * (prevVertex.y + currVertex.y));

                prevVertex = currVertex;
            }
            normal = normal.normalized;

            var d = 0.0f;
            var count = 0;
            for (var e = 0; e < indices.Length; e++)
            {
                var currVertex = points[indices[e]];
                d += Vector3.Dot(normal, currVertex);
                count++;
            }
            d /= count;

            return new CSGPlane(normal, d);
        }

        public static float CalcPolygonSign(Vector2[] points)
        {
            if (points.Length == 3)
            {
                var v0 = points[0];
                var v1 = points[1];
                var v2 = points[2];
                
                var ab = (v1 - v0);
                var ac = (v2 - v0);

                return (ab.x * ac.y - ab.y * ac.x);
            }
            
            var length = 0.0f;
            var prevIndex = points.Length - 1;
            var prevVertex = points[prevIndex];
            for (var e = 0; e < points.Length; e++)
            {
                var currVertex = points[e];
                length = length + ((prevVertex.x - currVertex.x) * (prevVertex.y + currVertex.y));
                prevVertex = currVertex;
            }
            return length;
        }



        public static CSGPlane CalcPolygonPlane(ControlMesh controlMesh, short polygonIndex)
        {
            if (controlMesh == null ||
                controlMesh.Polygons == null ||
                polygonIndex >= controlMesh.Polygons.Length)
                return new CSGPlane(MathConstants.upVector3, 0);
            var edgeIndices = controlMesh.Polygons[polygonIndex].EdgeIndices;
            if (edgeIndices.Length == 3)
            {
                var v0 = controlMesh.GetVertex(edgeIndices[0]);
                var v1 = controlMesh.GetVertex(edgeIndices[1]);
                var v2 = controlMesh.GetVertex(edgeIndices[2]);

                return new CSGPlane(v0, v1, v2);
            }

            // newell's method to calculate a normal for a concave polygon
            var normal = MathConstants.zeroVector3;
            var prevIndex = edgeIndices.Length - 1;
            if (prevIndex < 0)
            {
                return new CSGPlane(MathConstants.upVector3, MathConstants.zeroVector3);
            }

            var prevVertex = controlMesh.GetVertex(edgeIndices[prevIndex]);
            for (var e = 0; e < edgeIndices.Length; e++)
            {
                var currVertex = controlMesh.GetVertex(edgeIndices[e]);
                normal.x = normal.x + ((prevVertex.y - currVertex.y) * (prevVertex.z + currVertex.z));
                normal.y = normal.y + ((prevVertex.z - currVertex.z) * (prevVertex.x + currVertex.x));
                normal.z = normal.z + ((prevVertex.x - currVertex.x) * (prevVertex.y + currVertex.y));

                prevVertex = currVertex;
            }
            normal = normal.normalized;

            var d = 0.0f;
            var count = 0;
            for (var e = 0; e < edgeIndices.Length; e++)
            {
                var currVertex = controlMesh.GetVertex(edgeIndices[e]);
                d += Vector3.Dot(normal, currVertex);
                count++;
            }
            d /= count;

            return new CSGPlane(normal, d);
        }

        public static PrincipleAxis GetPrincipleAxis(Vector3 vector)
        {
            var absX = Mathf.Abs(vector.x);
            var absY = Mathf.Abs(vector.y);
            var absZ = Mathf.Abs(vector.z);
            if (absX > absY)
                return absX > absZ ? PrincipleAxis.X : PrincipleAxis.Z;
            return absY > absZ ? PrincipleAxis.Y : PrincipleAxis.Z;
        }

        public static Vector3 SnapToClosestAxis(Vector3 vector)
        {
            //Vector3 tangent;
            var absX = Mathf.Abs(vector.x);
            var absY = Mathf.Abs(vector.y);
            var absZ = Mathf.Abs(vector.z);
            if (absX > absY)
                return absX > absZ ? new Vector3(Mathf.Sign(vector.x), 0, 0) : new Vector3(0, 0, Mathf.Sign(vector.z));
            return absY > absZ ? new Vector3(0, Mathf.Sign(vector.y), 0) : new Vector3(0, 0, Mathf.Sign(vector.z));
        }
        
        public static Quaternion QuaternionFromMatrix(Matrix4x4 m)
        {
            // Adapted from: http://www.euclideanspace.com/maths/geometry/rotations/conversions/matrixToQuaternion/index.htm
            Quaternion q;
            q.w = Mathf.Sqrt(Mathf.Max(0, 1 + m[0, 0] + m[1, 1] + m[2, 2])) / 2;
            q.x = Mathf.Sqrt(Mathf.Max(0, 1 + m[0, 0] - m[1, 1] - m[2, 2])) / 2;
            q.y = Mathf.Sqrt(Mathf.Max(0, 1 - m[0, 0] + m[1, 1] - m[2, 2])) / 2;
            q.z = Mathf.Sqrt(Mathf.Max(0, 1 - m[0, 0] - m[1, 1] + m[2, 2])) / 2;
            q.x *= Mathf.Sign(q.x * (m[2, 1] - m[1, 2]));
            q.y *= Mathf.Sign(q.y * (m[0, 2] - m[2, 0]));
            q.z *= Mathf.Sign(q.z * (m[1, 0] - m[0, 1]));
            return q;
        }
        
//		private static readonly Vector3 PositiveY = new Vector3(0, 1, 0);
        private static readonly Vector3 NegativeY = new Vector3(0, -1, 0);
        private static readonly Vector3 PositiveZ = new Vector3(0, 0, 1);

        public static Vector3 CalculateTangent(Vector3 vector)
        {
            var absX = Mathf.Abs(vector.x);
            var absY = Mathf.Abs(vector.y);
            var absZ = Mathf.Abs(vector.z);
            
            if (absY > absX && absY > absZ)
                return PositiveZ;


            //if (absX > absY && absX > absZ)
            //if (Mathf.Abs(absX - absY) > MathConstants.NormalEpsilon &&
            //	Mathf.Abs(absX - absZ) > MathConstants.NormalEpsilon)
                return NegativeY;
            
            //if (absZ > absY && absZ > absX)
            //if (Mathf.Abs(absZ - absY) > MathConstants.NormalEpsilon &&
            //	Mathf.Abs(absZ - absX) > MathConstants.NormalEpsilon)
            //	return NegativeY;

            //return PositiveZ;
        }

        public static void CalculateTangents(Vector3 normal, out Vector3 tangent, out Vector3 binormal)
        {
            tangent		= GeometryUtility.CalculateTangent(normal);
            tangent		= Vector3.Cross(normal, tangent).normalized;
            binormal	= Vector3.Cross(normal, tangent).normalized;
        }

        public static Vector3 ProjectPointOnInfiniteLine(Vector3 point, Vector3 lineOrigin, Vector3 normalizedLineDirection)
        {
            var dot = Vector3.Dot(normalizedLineDirection, point - lineOrigin);
            return lineOrigin + normalizedLineDirection * dot;
        }
        
        public static CSGPlane InverseTransformPlane(Matrix4x4 inverseMatrix, CSGPlane plane) 
        {
            var dstMatrix = Matrix4x4.Transpose(inverseMatrix);
            var dstPlaneV = dstMatrix * new Vector4(plane.a, plane.b, plane.c, -plane.d);
            return new CSGPlane(dstPlaneV.x, dstPlaneV.y, dstPlaneV.z, -dstPlaneV.w);
        }
        
        public static CSGPlane TransformPlane(Matrix4x4 matrix, CSGPlane plane) 
        {
            var dstMatrix = Matrix4x4.Transpose(Matrix4x4.Inverse(matrix));
            var dstPlaneV = dstMatrix * new Vector4(plane.a, plane.b, plane.c, -plane.d);
            return new CSGPlane(dstPlaneV.x, dstPlaneV.y, dstPlaneV.z, -dstPlaneV.w);
        }

        public static Quaternion GetOrientationForPlane(CSGPlane plane)
        {
            var normal = plane.normal;
            var tangent = Vector3.Cross(normal, Vector3.Cross(normal, CalculateTangent(normal)).normalized).normalized;
            Quaternion q = Quaternion.LookRotation(tangent, normal);
            return q;
        }
        
        public static float DistancePointToCircle(SceneView sceneView, Vector3 point, Vector3 circleCenter, float circleRadius)
        {
            var pointCenter  = HandleUtility.WorldToGUIPoint(point);
            var screenCenter = HandleUtility.WorldToGUIPoint(circleCenter);
            var cam			 = sceneView.camera;
            if (cam)
            {
                var screenEdge = HandleUtility.WorldToGUIPoint(circleCenter + cam.transform.right * circleRadius);
                circleRadius = (screenCenter - screenEdge).magnitude;
            }
            var dist = (screenCenter - pointCenter).magnitude;
            if (dist < circleRadius)
                return 0;
            return dist - circleRadius;
        }
        
        public static void MoveControlMeshVertices(CSGBrush[] brushes, Vector3 offset)
        {			
            for (int b = 0; b < brushes.Length; b++)
            {
                var controlMesh = brushes[b].ControlMesh;
                if (controlMesh == null ||
                    controlMesh.Vertices == null)
                    continue;

                var localOffset = brushes[b].transform.worldToLocalMatrix.MultiplyVector(offset);
                for (var p = 0; p < controlMesh.Vertices.Length; p++)
                    controlMesh.Vertices[p] = GridUtility.CleanPosition(controlMesh.Vertices[p] + localOffset);
            }
        }

        public static void MoveControlMeshVertices(CSGBrush brush, Vector3 offset)
        {
            if (!brush)
                return;

            var controlMesh = brush.ControlMesh;
            if (controlMesh == null ||
                controlMesh.Vertices == null)
                return;

            var localOffset = brush.transform.worldToLocalMatrix.MultiplyVector(offset);
            for (var p = 0; p < controlMesh.Vertices.Length; p++)
                controlMesh.Vertices[p] = GridUtility.CleanPosition(controlMesh.Vertices[p] + localOffset);
        }

        public static Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            Vector2 ret;

            // TODO: see if we can re-use calculations across list of points

            var t0 = t; // TODO: turn this into t * 0.5f?
            var t1 = t0 * t;
            var t2 = t1 * t;


            ret.x = 0.5f * // TODO: remove this?
                    (
                        (p1.x * 2.0f) + // TODO: change to 1.0f?
                        (-p0.x + p2.x) * t0 +
                        (2.0f * p0.x - 5.0f * p1.x + 4.0f * p2.x - p3.x) * t1 +
                        (      -p0.x + 3.0f * p1.x - 3.0f * p2.x + p3.x) * t2
                    );

            ret.y = 0.5f * // TODO: remove this?
                    (
                        (p1.y * 2.0f) + // TODO: change to 1.0f?
                        (-p0.y + p2.y) * t0 +
                        (2.0f * p0.y - 5.0f * p1.y + 4.0f * p2.y - p3.y) * t1 +
                        (      -p0.y + 3.0f * p1.y - 3.0f * p2.y + p3.y) * t2
                    );

            return ret;
        }



        public static Matrix4x4 Multiply3x3To4x4(Matrix4x4 left3x3, Matrix4x4 right3x3)
        {
            float lM00 = left3x3.m00, lM01 = left3x3.m01, lM02 = left3x3.m02, lM03 = left3x3.m03;
            float lM10 = left3x3.m10, lM11 = left3x3.m11, lM12 = left3x3.m12, lM13 = left3x3.m13;
            float lM20 = left3x3.m20, lM21 = left3x3.m21, lM22 = left3x3.m22, lM23 = left3x3.m23;
            float lM30 = left3x3.m30, lM31 = left3x3.m31, lM32 = left3x3.m32, lM33 = left3x3.m33;

            float rM00 = right3x3.m00, rM01 = right3x3.m01, rM02 = right3x3.m02;
            float rM10 = right3x3.m10, rM11 = right3x3.m11, rM12 = right3x3.m12;
            float rM20 = right3x3.m20, rM21 = right3x3.m21, rM22 = right3x3.m22;

            return new Matrix4x4()
            {
                m00 = (lM00 * rM00) + (lM01 * rM10) + (lM02 * rM20),
                m01 = (lM00 * rM01) + (lM01 * rM11) + (lM02 * rM21),
                m02 = (lM00 * rM02) + (lM01 * rM12) + (lM02 * rM22),
                m03 = lM03,

                m10 = (lM10 * rM00) + (lM11 * rM10) + (lM12 * rM20),
                m11 = (lM10 * rM01) + (lM11 * rM11) + (lM12 * rM21),
                m12 = (lM10 * rM02) + (lM11 * rM12) + (lM12 * rM22),
                m13 = lM13,

                m20 = (lM20 * rM00) + (lM21 * rM10) + (lM22 * rM20),
                m21 = (lM20 * rM01) + (lM21 * rM11) + (lM22 * rM21),
                m22 = (lM20 * rM02) + (lM21 * rM12) + (lM22 * rM22),
                m23 = lM23,

                m30 = (lM30 * rM00) + (lM31 * rM10) + (lM32 * rM20),
                m31 = (lM30 * rM01) + (lM31 * rM11) + (lM32 * rM21),
                m32 = (lM30 * rM02) + (lM31 * rM12) + (lM32 * rM22),
                m33 = lM33
            };
        }


        /*
        public static bool ContinueTexGenFromSurfaceToSurface(CSGBrush srcBrush, int srcSurfaceIndex, CSGBrush dstBrush, int dstSurfaceIndex)
        {
            if (srcSurfaceIndex < 0 || srcSurfaceIndex >= srcBrush.Shape.Materials.Length ||
                srcBrush == null)
                return false;
            
            var src_brush_cache = CSGModelManager.GetBrushCache(srcBrush);
            if (src_brush_cache == null ||
                src_brush_cache.childData == null ||
                src_brush_cache.childData.modelTransform == null)
                return false;

            var dst_brush_cache = CSGModelManager.GetBrushCache(dstBrush);
            if (dst_brush_cache == null ||
                dst_brush_cache.childData == null ||
                dst_brush_cache.childData.modelTransform == null)
                return false;

            var dstPlane	= dstBrush.Shape.Surfaces[dstSurfaceIndex].Plane;
            var srcPlane	= srcBrush.Shape.Surfaces[srcSurfaceIndex].Plane;

            // convert planes into worldspace
            dstPlane = GeometryUtility.InverseTransformPlane(dstBrush.transform.worldToLocalMatrix, dstPlane);
            srcPlane = GeometryUtility.InverseTransformPlane(srcBrush.transform.worldToLocalMatrix, srcPlane);
                
            var dstNormal	= dstPlane.normal;
            var srcNormal	= srcPlane.normal;
                
            var srcTexGenIndex = srcBrush.Shape.Surfaces[srcSurfaceIndex].TexGenIndex;
            var dstTexGenIndex = dstBrush.Shape.Surfaces[dstSurfaceIndex].TexGenIndex;
                
            var scrShape = srcBrush.Shape;
            
            dstBrush.Shape.Materials[dstSurfaceIndex] = scrShape.Materials[srcSurfaceIndex];
            Vector3 srcPoint1, srcPoint2;
            Vector3 dstPoint1, dstPoint2;
            var edgeDirection = Vector3.Cross(dstNormal, srcNormal);
            var det = edgeDirection.sqrMagnitude;
            if (det < Constants.AlignmentTestEpsilon)
            {
                // Find 2 points on intersection of 2 planes
                srcPoint1 = srcPlane.pointOnPlane;
                srcPoint2 = GeometryUtility.ProjectPointOnPlane(srcPlane, srcPoint1 + MathConstants.oneVector3);

                dstPoint1 = GeometryUtility.ProjectPointOnPlane(dstPlane, srcPoint1);
                dstPoint2 = GeometryUtility.ProjectPointOnPlane(dstPlane, srcPoint2);
            } else
            {	
                // Find 2 points on intersection of 2 planes
                srcPoint1 = ((Vector3.Cross(edgeDirection, srcNormal) * -dstPlane.d) +
                                (Vector3.Cross(dstNormal, edgeDirection) * -srcPlane.d)) / det;
                srcPoint2 = srcPoint1 + edgeDirection;
                dstPoint1 = srcPoint1;
                dstPoint2 = srcPoint2;
            }
                
            Vector3 srcLocalPoint1 = srcBrush.transform.InverseTransformPoint(srcPoint1);
            Vector3 srcLocalPoint2 = srcBrush.transform.InverseTransformPoint(srcPoint2);

            Vector3 dstLocalPoint1 = dstBrush.transform.InverseTransformPoint(dstPoint1);
            Vector3 dstLocalPoint2 = dstBrush.transform.InverseTransformPoint(dstPoint2);

            var srcShape	= srcBrush.Shape;
            var srcTexGens	= srcShape.TexGens;
            var srcSurfaces	= srcShape.Surfaces;
            var dstShape	= dstBrush.Shape;
            var dstTexGens	= dstShape.TexGens;
            var dstSurfaces	= dstShape.Surfaces;


            // Reset destination shape to simplify calculations
            dstTexGens[dstTexGenIndex].Scale			= scrShape.TexGens[srcTexGenIndex].Scale;
            dstTexGens[dstTexGenIndex].Translation		= MathConstants.zeroVector2;
            dstTexGens[dstTexGenIndex].RotationAngle	= 0;

            if (!CSGModelManager.AlignTextureSpacesInLocalSpace(ref srcTexGens[srcTexGenIndex], ref srcSurfaces[srcSurfaceIndex], 
                                                                srcLocalPoint1, srcLocalPoint2,
                                                                ref dstTexGens[dstTexGenIndex], ref dstSurfaces[dstSurfaceIndex], 
                                                                dstLocalPoint1, dstLocalPoint2))
                return false;
            return true;			
        }
        */

        public static bool ContinueTexGenFromSurfaceToSurface(CSGBrush brush, int srcSurfaceIndex, int dstSurfaceIndex)
        {
            if (srcSurfaceIndex < 0 || srcSurfaceIndex >= brush.Shape.TexGens.Length ||
                !brush)
                return false;
                        
            var shape		= brush.Shape;
            var texGens		= shape.TexGens;
            var texGenFlags = shape.TexGenFlags;
            var surfaces	= shape.Surfaces;

            var srcTexGenIndex = surfaces[srcSurfaceIndex].TexGenIndex;
            var dstTexGenIndex = surfaces[dstSurfaceIndex].TexGenIndex;

//          var src_is_world_space = (texGenFlags[srcTexGenIndex] & TexGenFlags.WorldSpaceTexture) != TexGenFlags.None;
//          var dst_is_world_space = (texGenFlags[dstTexGenIndex] & TexGenFlags.WorldSpaceTexture) != TexGenFlags.None;
            
//          if (src_is_world_space) SurfaceUtility.SetTextureLock(brush, srcSurfaceIndex, true);
//          if (dst_is_world_space) SurfaceUtility.SetTextureLock(brush, dstSurfaceIndex, true);

            var dstLocalPlane	= surfaces[dstSurfaceIndex].Plane;
            var srcLocalPlane   = surfaces[srcSurfaceIndex].Plane;

            CSGModel parentModel;
            CSGOperation parentOp;
            InternalCSGModelManager.FindParentOperationAndModel(brush.transform, out parentOp, out parentModel);

            var localFromWorld = brush.transform.worldToLocalMatrix;
            var worldFromModel = parentModel.transform.localToWorldMatrix;
            var modelFromWorld = parentModel.transform.worldToLocalMatrix;
            var localFromModel = localFromWorld * worldFromModel;
            //var localToWorldMatrix = brush.transform.localToWorldMatrix;

            // convert planes into worldspace
            var dstWorldPlane   = InverseTransformPlane(localFromWorld, dstLocalPlane);
            var srcWorldPlane   = InverseTransformPlane(localFromWorld, srcLocalPlane);
                
            var dstWorldNormal  = dstWorldPlane.normal;
            var srcWorldNormal  = srcWorldPlane.normal;
                
            
            shape.TexGens[dstSurfaceIndex].RenderMaterial = shape.TexGens[srcSurfaceIndex].RenderMaterial;
            Vector3 srcWorldPoint1, srcWorldPoint2;
            Vector3 dstWorldPoint1, dstWorldPoint2;
            var edgeDirection = Vector3.Cross(dstWorldNormal, srcWorldNormal);
            var det = edgeDirection.sqrMagnitude;
            if (det < MathConstants.AlignmentTestEpsilon)
            {
                // Find 2 points on intersection of 2 planes
                srcWorldPoint1 = srcWorldPlane.pointOnPlane;
                srcWorldPoint2 = GeometryUtility.ProjectPointOnPlane(srcWorldPlane, srcWorldPoint1 + MathConstants.oneVector3);

                dstWorldPoint1 = GeometryUtility.ProjectPointOnPlane(dstWorldPlane, srcWorldPoint1);
                dstWorldPoint2 = GeometryUtility.ProjectPointOnPlane(dstWorldPlane, srcWorldPoint2);
            } else
            {	
                // Find 2 points on intersection of 2 planes
                srcWorldPoint1 = ((Vector3.Cross(edgeDirection, srcWorldNormal) * -dstWorldPlane.d) +
                                (Vector3.Cross(dstWorldNormal, edgeDirection) * -srcWorldPlane.d)) / det;
                srcWorldPoint2 = srcWorldPoint1 + edgeDirection;
                dstWorldPoint1 = srcWorldPoint1;
                dstWorldPoint2 = srcWorldPoint2;
            }

            var srcModelPoint1 = modelFromWorld.MultiplyPoint(srcWorldPoint1);
            var srcModelPoint2 = modelFromWorld.MultiplyPoint(srcWorldPoint2);

            var dstModelPoint1 = modelFromWorld.MultiplyPoint(dstWorldPoint1);
            var dstModelPoint2 = modelFromWorld.MultiplyPoint(dstWorldPoint2);

            var result = SurfaceUtility.AlignTextureSpaces(localFromModel,     texGens[srcTexGenIndex], texGenFlags[srcTexGenIndex], ref surfaces[srcSurfaceIndex], srcModelPoint1, srcModelPoint2,
                                                           localFromModel, ref texGens[dstTexGenIndex], texGenFlags[dstTexGenIndex], ref surfaces[dstSurfaceIndex], dstModelPoint1, dstModelPoint2, false);

//          if (src_is_world_space) SurfaceUtility.SetTextureLock(brush, srcSurfaceIndex, false);
//          if (dst_is_world_space) SurfaceUtility.SetTextureLock(brush, dstSurfaceIndex, false);
            return result;
        }
        
    }
}