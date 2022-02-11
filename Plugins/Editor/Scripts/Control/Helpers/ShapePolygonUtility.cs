using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using InternalRealtimeCSG;
using RealtimeCSG.Legacy;

namespace RealtimeCSG
{
    [Serializable]
    internal sealed class ShapePolygon
    {
        public ShapePolygon() { }
        public ShapePolygon(Vector3[] vertices) { this.Vertices = vertices; }

        // NOTE: an edge is defined as [vertex n, vertex (n+1)%vertices.length]
        public Vector3[]        Vertices;
        public TexGen[]         EdgeTexgens;
    }

    [Serializable]
    internal struct ShapeEdge
    {
        public int PolygonIndex;
        public int EdgeIndex;
    }


    internal static class ShapePolygonUtility
    {
        public static bool IntersectsWithShapePolygon2D(ShapePolygon shapePolygon, Ray ray)
        {
            var polygonPlane = new CSGPlane(MathConstants.upVector3, 0);
            var intersection = polygonPlane.RayIntersection(ray);
            var vertices = shapePolygon.Vertices;
            for (int v0 = vertices.Length - 1, v1 = 0; v1 < vertices.Length; v0 = v1, v1++)
            {
                var vertex0 = vertices[v0];
                var vertex1 = vertices[v1];
                var delta = (vertex1 - vertex0);
                var length = delta.sqrMagnitude;
                if (length <= MathConstants.EqualityEpsilonSqr)
                    continue;

                var tangent = delta / Mathf.Sqrt(length);
                var normal = Vector3.Cross(MathConstants.upVector3, tangent);
                var plane = new CSGPlane(normal, vertex0);
                if (plane.Distance(intersection) < MathConstants.DistanceEpsilon)
                    return false;
            }
            return true;
        }

        public static bool IntersectsWithShapePolygon(ShapePolygon shapePolygon, CSGPlane polygonPlane, Ray ray)
        {
            var intersection = polygonPlane.RayIntersection(ray);
            var vertices = shapePolygon.Vertices;
            for (int v0 = vertices.Length - 1, v1 = 0; v1 < vertices.Length; v0 = v1, v1++)
            {
                var vertex0 = vertices[v0];
                var vertex1 = vertices[v1];
                var delta   = (vertex1 - vertex0);
                var length  = delta.sqrMagnitude;
                if (length <= MathConstants.EqualityEpsilonSqr)
                    continue;

                var tangent = delta / Mathf.Sqrt(length);
                var normal  = Vector3.Cross(polygonPlane.normal, tangent);
                var plane   = new CSGPlane(normal, vertex0);
                if (plane.Distance(intersection) < MathConstants.DistanceEpsilon)
                    return false;
            }
            return true;
        }
        


        public static void RemoveDuplicatePoints(ref Vector3[] vertices)
        {
            // remove any points that are too close to one another
            for (int j = vertices.Length - 1, i = vertices.Length - 2; i >= 0; j = i, i--)
            {
                if ((vertices[j] - vertices[i]).sqrMagnitude < MathConstants.DistanceEpsilon)
                {
                    ArrayUtility.RemoveAt(ref vertices, j);
                }
            }
            while (vertices.Length > 3 && (vertices[0] - vertices[vertices.Length - 1]).sqrMagnitude < MathConstants.DistanceEpsilon)
            {
                var lastIndex = vertices.Length - 1;
                ArrayUtility.RemoveAt(ref vertices, lastIndex);
            }
        }

        public static void RemoveDuplicatePoints(ShapePolygon shapePolygon)
        {
            var vertices		= shapePolygon.Vertices;
            var edgeTexgens		= shapePolygon.EdgeTexgens;

            // remove any points that are too close to one another
            for (int j = vertices.Length - 1, i = vertices.Length - 2; i >= 0; j = i, i--)
            {
                if ((vertices[j] - vertices[i]).sqrMagnitude < MathConstants.DistanceEpsilon)
                {
                    ArrayUtility.RemoveAt(ref vertices, j);
                    ArrayUtility.RemoveAt(ref edgeTexgens, j);
                }
            }
            while (vertices.Length > 3 && (vertices[0] - vertices[vertices.Length - 1]).sqrMagnitude < MathConstants.DistanceEpsilon)
            {
                var lastIndex = vertices.Length - 1;
                ArrayUtility.RemoveAt(ref vertices, lastIndex);
                ArrayUtility.RemoveAt(ref edgeTexgens, lastIndex);
            }

            shapePolygon.Vertices = vertices;
            shapePolygon.EdgeTexgens = edgeTexgens;
        }


        public static List<ShapePolygon> CreateCleanPolygonsFromVertices(Vector3[]  vertices,
                                                                         Vector3    origin,
                                                                         CSGPlane   buildPlane)
        {
            Vector3[] projectedVertices;
            return CreateCleanPolygonsFromVertices(vertices,
                origin,
                buildPlane,
                out projectedVertices);
        }

        
        public static List<ShapePolygon> CreateCleanPolygonsFromVertices(Vector3[]	 vertices,
                                                                         Vector3	 origin,
                                                                         CSGPlane	 buildPlane,
                                                                         out Vector3[] projectedVertices)
        {
            if (vertices.Length < 3)
            {
                projectedVertices = null;
                return null;
            }
            var m			= Matrix4x4.TRS(-origin, Quaternion.identity, Vector3.one);
            var vertices2d	= GeometryUtility.RotatePlaneTo2D(m, vertices, buildPlane);

            projectedVertices = GeometryUtility.ToVector3XZ(vertices2d);
            return CreateCleanSubPolygonsFromVertices(vertices2d, buildPlane);
        }

        
        private static List<ShapePolygon> CreateCleanSubPolygonsFromVertices(Vector2[]	vertices2d,
                                                                             CSGPlane	buildPlane)
        {
            if (vertices2d.Length < 3)
                return null;
             
            for (int i = 0; i < vertices2d.Length - 2; i++)
            {
                for (int j = i + 2; j < vertices2d.Length; j++)
                {
                    if ((vertices2d[j] - vertices2d[i]).sqrMagnitude < MathConstants.DistanceEpsilon)
                    {
                        List<ShapePolygon> combined_polygons = null;
                        
                        var left_length  = i;
                        var right_length = (vertices2d.Length - j);
                        var other_length = left_length + right_length;

                        if (other_length > 2)
                        {
                            var	other_vertices		= new Vector2[other_length];

                            if (left_length > 0)
                                Array.Copy(vertices2d, 0, other_vertices, 0, left_length);
                                
                            Array.Copy(vertices2d, j, other_vertices, left_length, right_length);
                            combined_polygons = CreateCleanSubPolygonsFromVertices(other_vertices, buildPlane);
                        }

                        var center_length = (j - i);
                        if (center_length > 2)
                        {
                            var first_vertices		= new Vector2[center_length];

                            Array.Copy(vertices2d, i, first_vertices, 0, center_length);

                            var first_polygons = CreateCleanSubPolygonsFromVertices(first_vertices, buildPlane);
                            if (combined_polygons != null)
                                combined_polygons.AddRange(first_polygons);
                            else
                                combined_polygons = first_polygons;
                        }

                        return combined_polygons;
                    }
                }
            }

            var polygonSign = GeometryUtility.CalcPolygonSign(vertices2d);
            if (polygonSign == 0)
                return null;
            
            if (polygonSign < 0)
                Array.Reverse(vertices2d);
            
            List<List<Vector2>> outlines = null;
            try { outlines = InternalCSGModelManager.External.ConvexPartition(vertices2d); } catch { outlines = null; }
            if (outlines == null)
                return null;

            var polygons = new List<ShapePolygon>();
            for (int b = 0; b < outlines.Count; b++)
            {
                if (GeometryUtility.IsNonConvex(outlines[b]))
                    return null;

                polygons.Add(new ShapePolygon(GeometryUtility.ToVector3XZReversed(outlines[b])));
            } 
                
            return polygons;
        }



        public static bool GenerateControlMeshFromVertices(ShapePolygon		shape2DPolygon,
                                                           Matrix4x4		localToWorld,
                                                           Vector3			direction,
                                                           float			height,
                                                           TexGen			capTexgen, 
                                                           bool?			smooth, 
                                                           bool				singleSurfaceEnds, //Plane buildPlane, 
                                                           out ControlMesh	controlMesh, 
                                                           out Shape		shape)
        {
            if (shape2DPolygon == null)
            {
                controlMesh = null; 
                shape = null;
                return false;
            }

            var vertices = shape2DPolygon.Vertices;
            if (vertices.Length < 3)
            {
                controlMesh = null; 
                shape = null;
                return false;
            }
            if (height == 0.0f)
            {
                controlMesh = null; 
                shape = null;
                return false;
            }
                        
            Vector3 from;
            Vector3 to;
            
            if (height > 0)
            {
                @from = direction * height;// buildPlane.normal * height;
                to   = MathConstants.zeroVector3;
            } else
            { 
                @from = MathConstants.zeroVector3;
                to   = direction * height;//buildPlane.normal * height;
            }
            
            var count			= vertices.Length;
            var doubleCount		= (count * 2);
            var extraPoints		= 0;
            var extraEdges		= 0;
            var endsPolygons	= 2;
            var startEdgeOffset	= doubleCount;
            
            if (!singleSurfaceEnds)
            {
                extraPoints		= 2;
                extraEdges		= (4 * count);
                endsPolygons	= doubleCount;
                startEdgeOffset += extraEdges;
            }

            
            var dstPoints	= new Vector3 [doubleCount + extraPoints];
            var dstEdges	= new HalfEdge[(count * 6) + extraEdges];
            var dstPolygons	= new Polygon [count + endsPolygons];

            var center1 = MathConstants.zeroVector3;
            var center2 = MathConstants.zeroVector3;


            for (int i = 0; i < count; i++)
            {
                var point1 = vertices[i];
                var point2 = vertices[(count + i-1) % count];
                
                point1 += @from;
                point2 += to;

                // swap y/z to solve texgen issues
                dstPoints[i].x				= point1.x;
                dstPoints[i].y				= point1.y;
                dstPoints[i].z				= point1.z;
                
                center1 += dstPoints[i];

                dstEdges [i].VertexIndex	= (short)i;
                dstEdges [i].HardEdge		= true;
                
                // swap y/z to solve texgen issues
                dstPoints[i + count].x				= point2.x;
                dstPoints[i + count].y				= point2.y;
                dstPoints[i + count].z				= point2.z;
                center2 += dstPoints[i + count];

                dstEdges [i + count].VertexIndex	= (short)(i + count);
                dstEdges [i + count].HardEdge		= true;
            }

            if (!singleSurfaceEnds)
            {
                dstPoints[doubleCount    ] = center1 / count;
                dstPoints[doubleCount + 1] = center2 / count;

                int edge_offset		= doubleCount;
                short polygon_index	= (short)count;

                // 'top' 
                for (int i = 0, j = count-1; i < count; j=i, i++)
                {
                    var jm = (j) % count;
                    var im = (i) % count;

                    var edgeOut0	= edge_offset + (jm * 2) + 1;
                    var edgeIn0		= edge_offset + (im * 2) + 0;
                    var edgeOut1	= edge_offset + (im * 2) + 1;

                    dstEdges[edgeIn0 ].VertexIndex		= (short)(doubleCount);
                    dstEdges[edgeIn0 ].HardEdge		= true;
                    dstEdges[edgeIn0 ].TwinIndex		= edgeOut1;

                    dstEdges[edgeOut1].VertexIndex		= (short)im;
                    dstEdges[edgeOut1].HardEdge		= true;
                    dstEdges[edgeOut1].TwinIndex		= edgeIn0;
                    
                    dstEdges[im       ].PolygonIndex	= polygon_index;
                    dstEdges[edgeIn0 ].PolygonIndex	= polygon_index;
                    dstEdges[edgeOut0].PolygonIndex	= polygon_index;
                    
                    dstPolygons[polygon_index] = new Polygon(new int[] { im, edgeIn0, edgeOut0 }, polygon_index);
                    polygon_index++;
                }

                edge_offset = doubleCount * 2;
                // 'bottom'
                for (int i = 0, j = count-1; j >= 0; i=j, j--)
                {
                    var jm = (count + count - j) % count;
                    var im = (count + count - i) % count;

                    var edgeOut0	= edge_offset + (jm * 2) + 1;
                    var edgeIn0		= edge_offset + (im * 2) + 0;
                    var edgeOut1	= edge_offset + (im * 2) + 1;
                    
                    dstEdges[edgeIn0 ].VertexIndex		= (short)(doubleCount + 1);
                    dstEdges[edgeIn0 ].HardEdge		= true;
                    dstEdges[edgeIn0 ].TwinIndex		= edgeOut1;

                    dstEdges[edgeOut1].VertexIndex		= (short)(im + count);
                    dstEdges[edgeOut1].HardEdge		= true;
                    dstEdges[edgeOut1].TwinIndex		= edgeIn0;
                    
                    dstEdges[im+count ].PolygonIndex	= polygon_index;
                    dstEdges[edgeIn0 ].PolygonIndex	= polygon_index;
                    dstEdges[edgeOut0].PolygonIndex	= polygon_index;
                    
                    dstPolygons[polygon_index] = new Polygon(new int[] { im+count, edgeIn0, edgeOut0 }, polygon_index);
                    polygon_index++;
                }
            } else
            {			
                var polygon0Edges	= new int[count];
                var polygon1Edges	= new int[count];
                for (var i = 0; i < count; i++)
                {
                    dstEdges [i        ].PolygonIndex	= (short)(count + 0);
                    dstEdges [i + count].PolygonIndex	= (short)(count + 1);
                    polygon0Edges[i]			= i;
                    polygon1Edges[count - (i+1)] = i + count;
                }
                dstPolygons[count + 0] = new Polygon(polygon0Edges, count + 0);
                dstPolygons[count + 1] = new Polygon(polygon1Edges, count + 1);
            }


            for (int v0 = count - 1, v1 = 0; v1 < count; v0 = v1, v1++)
            {
                var polygonIndex = (short)(v1);
                
                var nextOffset = startEdgeOffset + (((v1         + 1) % count) * 4);
                var currOffset = startEdgeOffset + (((v1            )        ) * 4);
                var prevOffset = startEdgeOffset + (((v1 + count - 1) % count) * 4);

                var nextTwin = nextOffset + 1;
                var prevTwin = prevOffset + 3;

                dstEdges[v1        ].TwinIndex = currOffset + 0;
                dstEdges[v1 + count].TwinIndex = currOffset + 2;

                dstEdges[currOffset + 0].PolygonIndex = polygonIndex;
                dstEdges[currOffset + 1].PolygonIndex = polygonIndex;
                dstEdges[currOffset + 2].PolygonIndex = polygonIndex;
                dstEdges[currOffset + 3].PolygonIndex = polygonIndex;

                dstEdges[currOffset + 0].TwinIndex = (v1        );
                dstEdges[currOffset + 1].TwinIndex = prevTwin   ;
                dstEdges[currOffset + 2].TwinIndex = (v1 + count);
                dstEdges[currOffset + 3].TwinIndex = nextTwin   ;

                dstEdges[currOffset + 0].VertexIndex = (short)(v0        );
                dstEdges[currOffset + 1].VertexIndex = (short)(v1 + count);
                dstEdges[currOffset + 2].VertexIndex = (short)(((v1   + 1) % count) + count);
                dstEdges[currOffset + 3].VertexIndex = (short)(v1        );

                dstEdges[currOffset + 0].HardEdge = true;
                dstEdges[currOffset + 1].HardEdge = true;
                dstEdges[currOffset + 2].HardEdge = true;
                dstEdges[currOffset + 3].HardEdge = true;

                dstPolygons[polygonIndex] = new Polygon(new [] { currOffset + 0,
                    currOffset + 1,
                    currOffset + 2,
                    currOffset + 3 }, polygonIndex);
            }

            for (int i = 0; i < dstPoints.Length; i++)
                dstPoints[i] = localToWorld.MultiplyPoint(dstPoints[i]);
            
            controlMesh = new ControlMesh
            {
                Vertices	= dstPoints,
                Edges		= dstEdges,
                Polygons	= dstPolygons
            };
            controlMesh.SetDirty();

            shape = new Shape(dstPolygons.Length);
            for (int i = 0; i < shape.TexGenFlags.Length; i++)
                shape.TexGenFlags[i] = RealtimeCSG.CSGSettings.DefaultTexGenFlags;


            var smoothinggroup = (smooth.HasValue && smooth.Value) ? SurfaceUtility.FindUnusedSmoothingGroupIndex() : 0;


            var containedMaterialCount = 0;
            if (shape2DPolygon.EdgeTexgens != null/* &&
                shape2DPolygon.edgeTexgenFlags != null*/)
            {
                containedMaterialCount = shape2DPolygon.EdgeTexgens.Length;
            }

            if (capTexgen.RenderMaterial == null)
            {
                capTexgen.RenderMaterial = CSGSettings.DefaultMaterial;
            }
            
            for (var i = 0; i < dstPolygons.Length; i++)
            {
                if (i < containedMaterialCount)
                {
                    //shape.TexGenFlags[i] = shape2DPolygon.edgeTexgenFlags[i];
                    shape.TexGens    [i] = shape2DPolygon.EdgeTexgens[i];
                    shape.Surfaces   [i].TexGenIndex = i;
                } else
                {
                    shape.TexGens[i] = capTexgen;
                    //shape.TexGenFlags[i]			= TexGenFlags.None;
                    shape.Surfaces[i].TexGenIndex = i;
                }
                if (smooth.HasValue)
                { 
                    if (i < count)
                    {
                        shape.TexGens[i].SmoothingGroup = smoothinggroup;
                    } else
                    {
                        shape.TexGens[i].SmoothingGroup = 0;
                    }
                }
            }

            for (var s = 0; s < dstPolygons.Length; s++)
            {
                shape.Surfaces[s].Plane = GeometryUtility.CalcPolygonPlane(controlMesh, (short)s);
                var normal = shape.Surfaces[s].Plane.normal;
                Vector3 tangent, binormal;
                GeometryUtility.CalculateTangents(normal, out tangent, out binormal);
                //var tangent		= Vector3.Cross(GeometryUtility.CalculateTangent(normal), normal).normalized;
                //var binormal	= Vector3.Cross(normal, tangent);
                shape.Surfaces[s].Tangent  = tangent;
                shape.Surfaces[s].BiNormal = binormal;
                shape.Surfaces[s].TexGenIndex = s;
            }

            controlMesh.Valid = ControlMeshUtility.Validate(controlMesh, shape);
            if (controlMesh.Valid)
                return true;

            controlMesh = null; 
            shape = null;
            return false;
        }
        

        public static void FixMaterials(Vector3[]			originalVertices,
                                        List<ShapePolygon>	polygons,
                                        Quaternion			rotation,
                                        Vector3				origin,
                                        CSGPlane			buildPlane,
                                        TexGen[]			edgeTexgens = null,
                                        ShapeEdge[]			shapeEdges = null)
        {
            if (shapeEdges != null)
            {
                for (int e = 0; e < shapeEdges.Length; e++)
                {
                    shapeEdges[e].EdgeIndex = -1;
                    shapeEdges[e].PolygonIndex = -1;
                }
            }

            for (int p = 0; p < polygons.Count; p++)
            {
                var shapePolygon	= polygons[p];
                var vertices3d		= shapePolygon.Vertices;
                
                var shapeTexgens	= new TexGen[vertices3d.Length];
                //var shapeTexgenFlags	= new TexGenFlags[vertices3d.Length];

                if (edgeTexgens != null)
                {
                    var indices = new int[vertices3d.Length];
                    for (int n0 = 0; n0 < vertices3d.Length; n0++)
                    {
                        indices[n0] = -1;
                        for (int n1 = 0; n1 < originalVertices.Length; n1++)
                        {
                            float diff = (vertices3d[n0] - originalVertices[n1]).sqrMagnitude;
                            if (diff > MathConstants.EqualityEpsilonSqr)
                                continue;

                            indices[n0] = n1;
                            break;
                        }
                    }

                    for (int n0 = indices.Length - 1, n1 = 0; n1 < indices.Length; n0 = n1, n1++)
                    {
                        var vertex0 = indices[n0] % edgeTexgens.Length;
                        var vertex1 = indices[n1] % edgeTexgens.Length;

                        if (vertex0 == -1 || vertex1 == -1)
                        {
                            shapeTexgens[n1] = new TexGen(CSGSettings.DefaultMaterial);//n0
                        } else
                        if ((Mathf.Abs(vertex1 - vertex0) == 1 ||
                             vertex0 == vertices3d.Length - 1 && vertex1 == 0))
                        {
                            if (shapeEdges != null)
                            {
                                shapeEdges[vertex0].PolygonIndex = p;
                                shapeEdges[vertex0].EdgeIndex    = n1;
                            }
                            shapeTexgens  [n1] = edgeTexgens[vertex0];
                        } else
                        {
                            shapeTexgens  [n1] = new TexGen(edgeTexgens[vertex0].RenderMaterial);//n0
                        }
                    }
                } else
                {
                    for (int n0 = 0; n0 < vertices3d.Length; n0++)
                    {
                        shapeTexgens[n0] = new TexGen(CSGSettings.DefaultMaterial);//n0
                        //shapeTexgenFlags[n0]	= TexGenFlags.None;
                    }
                }
                
                shapePolygon.EdgeTexgens = shapeTexgens;

                ShapePolygonUtility.RemoveDuplicatePoints(shapePolygon);
            }
        }
    }
}
