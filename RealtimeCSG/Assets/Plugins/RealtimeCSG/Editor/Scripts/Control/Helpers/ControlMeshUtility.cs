using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using InternalRealtimeCSG;
using System.Linq;
using RealtimeCSG.Legacy;
using RealtimeCSG.Components;

namespace RealtimeCSG
{
    [Serializable]
    internal sealed class MergePoint
    {
        public MergePoint(short i1, short i2)
        {
            if (i1 > i2)
            {
                Index1 = i1;
                Index2 = i2;
            } else
            {
                Index1 = i2;
                Index2 = i1;
            }
        }
        public short Index1;
        public short Index2;
    }

    internal sealed class AcceptableTopology
    {
        public readonly HashSet<short>	AcceptablePoints	= new HashSet<short>();
        public readonly HashSet<int>	AcceptableEdges		= new HashSet<int>();
        public readonly HashSet<short>	AcceptablePolygons	= new HashSet<short>();

        public void Clear()
        {
            AcceptablePoints	.Clear();
            AcceptableEdges		.Clear();
            AcceptablePolygons	.Clear();
        }		
    }

    internal static class ControlMeshUtility
    {
        private static bool _outputToLog = false;
        private static readonly HashSet<string> PrevOutput = new HashSet<string>();
        
        public static bool Validate(ControlMesh controlMesh, Shape shape)
        {
            PrevOutput.Clear();
            if (controlMesh.Edges == null)
            {				
                if (!_outputToLog) return false;
                const string text = ("no edges found in polygon");
                if (PrevOutput.Add(text)) Debug.Log(text);
                return false;
            }
            if (shape == null || shape.Surfaces.Length < 4)
            {
                if (!_outputToLog) return false;
                const string text = ("shape == null || shape.Surfaces.Length < 4");
                if (PrevOutput.Add(text)) Debug.Log(text);
                return false;
            }
            var fail = false;
            for (var i = 0; i < controlMesh.Edges.Length; i++)
            {
                var edge = controlMesh.Edges[i];
                var polygonIndex = edge.PolygonIndex;
                if (polygonIndex < 0 ||
                    polygonIndex >= controlMesh.Polygons.Length)
                {
                    if (!_outputToLog) return false;
                    var text = (i + ": no valid polygon-index found (" + polygonIndex + ")");
                    if (PrevOutput.Add(text)) Debug.Log(text);
                    fail = true;
                    continue;
                }
                var edgeIndices = controlMesh.Polygons[polygonIndex].EdgeIndices;
                if (edgeIndices == null)
                {
                    if (!_outputToLog) return false;
                    var text = polygonIndex + ": polygon has no edges";
                    if (PrevOutput.Add(text)) Debug.Log(text);
                    fail = true; continue;
                }
                var edgeIndex = Array.IndexOf(edgeIndices, i);
                if (edgeIndex == -1)
                {
                    if (!_outputToLog) return false;
                    var text = (i + ": edge not found in polygon");
                    if (PrevOutput.Add(text)) Debug.Log(text);
                    fail = true; continue; 
                } else
                if (edgeIndices[edgeIndex] != i)
                {
                    if (!_outputToLog) return false;
                    var text = (i + ": polygon.edges[edgeIndex] != i");
                    if (PrevOutput.Add(text)) Debug.Log(text);
                    fail = true;
                    continue;
                }

                var prev = controlMesh.GetPrevEdgeIndex(i);
                if (prev < 0 || prev >= controlMesh.Edges.Length)
                {
                    if (!_outputToLog) return false;
                    var text = (i + ": prev < 0 || prev >= controlMesh.edges.Length");
                    if (PrevOutput.Add(text)) Debug.Log(text);
                    fail = true;
                    continue;
                }
                var prevEdge = controlMesh.Edges[prev];
                var prevPolygonIndex = prevEdge.PolygonIndex;
                if (prevPolygonIndex < 0 || prevPolygonIndex >= controlMesh.Polygons.Length)
                {
                    if (!_outputToLog) return false;
                    var text = (i + ": prev_polygon_index < 0 || prev_polygon_index >= controlMesh.polygons.Length");
                    if (PrevOutput.Add(text)) Debug.Log(text);
                    fail = true;
                    continue;
                }
                
                var curr = controlMesh.GetNextEdgeIndex(prev);
                if (curr != i)
                {
                    if (!_outputToLog) return false;
                    var text = (i + ": prev->next != i");
                    if (PrevOutput.Add(text)) Debug.Log(text);
                    fail = true;
                }
                var twin = edge.TwinIndex;
                if (twin < 0)
                {
                    if (!_outputToLog) return false;
                    var text = (i + ": twin ("+twin+") < 0");
                    if (PrevOutput.Add(text)) Debug.Log(text);
                    fail = true;
                } else
                if (twin >= controlMesh.Edges.Length)
                {
                    if (!_outputToLog) return false;
                    var text = (i + ": twin ("+twin+") >= edges.Length ("+ controlMesh.Edges.Length + ")");
                    if (PrevOutput.Add(text)) Debug.Log(text);
                    fail = true;
                } else
                {
                    var twinVertexIndex = controlMesh.Edges[twin].VertexIndex;
                    var prevVertexIndex = controlMesh.Edges[prev].VertexIndex;
                    if (twinVertexIndex != prevVertexIndex)
                    {
                        if (!_outputToLog) return false;
                        var text = ("edges[twin("+twin+")].vertexIndex (" + controlMesh.Edges[twin].VertexIndex + ") != edges[prev(" + prev + ")].vertexIndex (" + controlMesh.Edges[prev].VertexIndex + ")");
                        if (PrevOutput.Add(text)) Debug.Log(text);
                        fail = true;
                    }
                    if (controlMesh.Edges[twin].TwinIndex != i)
                    {
                        if (!_outputToLog) return false;
                        var text = (i + ": edges[twin].twinIndex ("+ controlMesh.Edges[twin].TwinIndex + ") != i ("+i+")");
                        if (PrevOutput.Add(text)) Debug.Log(text);
                        fail = true;
                    }
                }
            }
            
            int texGenCount = shape.TexGens.Length;
            if (__usedTexGens.Length < texGenCount)
            {
                __usedTexGens		= new bool[texGenCount];
                __indexConversion	= new int[texGenCount];
            }
            Array.Clear(__usedTexGens,0,__usedTexGens.Length);

//			var used_planes  = new HashSet<int>();
            for (var p = 0; p < controlMesh.Polygons.Length; p++)
            {
                var texGenIndex = controlMesh.Polygons[p].TexGenIndex;
                if (texGenIndex < 0 )
                {
                    if (!_outputToLog) return false;
                    var text = (p + ": texGenIndex < 0");
                    if (PrevOutput.Add(text)) Debug.Log(text);
                    fail = true;
                    continue;
                }
                __usedTexGens[texGenIndex] = true;
            }

            for (var t = 0; t < shape.TexGens.Length; t++)
            {
                if (__usedTexGens[t])
                    continue;

                if (!_outputToLog) return false;
                var text = ("texGenIndex " + t + " is not used");
                if (PrevOutput.Add(text)) Debug.Log(text);
                fail = true;
            }

            var polygons = controlMesh.Polygons;
            if (shape.Surfaces.Length != polygons.Length)
            {
                if (!_outputToLog) return false;
                var text = ("this.Shape.Surfaces.Length (" + shape.Surfaces.Length + ") != this.polygons.Length ("+ polygons.Length + ")");
                if (PrevOutput.Add(text)) Debug.Log(text);
                fail = true;
            }

            for (var p = 0; p < polygons.Length; p++)
            {
                var edgeIndices	= polygons[p].EdgeIndices;
                for (int e1 = edgeIndices.Length - 1, e2 = 0; e2 < edgeIndices.Length; e1 = e2, e2++)
                {
                    var polyEdgeIndex1 = edgeIndices[e1];
                    var polyEdgeIndex2 = edgeIndices[e2];
                    var foundPolygonIndex = controlMesh.GetEdgePolygonIndex(polyEdgeIndex1);
                    if (foundPolygonIndex != p)
                    {
                        if (!_outputToLog) return false;
                        var text = (p + ": this.GetEdgePolygonIndex(edge_index{" + polyEdgeIndex1 + "}){" + foundPolygonIndex + "} != polygon_index{" + p + "}");
                        if (PrevOutput.Add(text)) Debug.Log(text);
                        fail = true;
                    } else
                    { 
                        var twinPolygonIndex = controlMesh.GetEdgePolygonIndex(controlMesh.GetTwinEdgeIndex(polyEdgeIndex1));
                        if (twinPolygonIndex == foundPolygonIndex)
                        {
                            if (!_outputToLog) return false;
                            var text = (p + ": twin_polygon_index{" + twinPolygonIndex + "} == found_polygon_index{" + foundPolygonIndex + "}");
                            if (PrevOutput.Add(text)) Debug.Log(text);
                            fail = true;
                        }
                    }

                    var twinVertexIndex = controlMesh.GetTwinEdgeVertexIndex(polyEdgeIndex2);
                    var vertexIndex = controlMesh.GetVertexIndex(polyEdgeIndex1);
                    if (twinVertexIndex == vertexIndex)
                        continue;

                    if (!_outputToLog) return false;
                    var text2 = (p + ": this.GetTwinEdgeVertexIndex(" + polyEdgeIndex2 + ") (" + twinVertexIndex + ") != this.GetVertexIndex(" + polyEdgeIndex1 + ") (" + vertexIndex + ")");
                    if (PrevOutput.Add(text2)) Debug.Log(text2);
                    fail = true;
                }
            }

            return !fail;
        }


        // assumes mergePoints is sorted
        public static void MergeVertices(ControlMesh controlMesh, List<MergePoint> mergePoints)
        {
            if (controlMesh == null)
                return;
            for (var i = mergePoints.Count - 1; i >= 0; i--)
            {
                MergeVertices(controlMesh, mergePoints[i].Index1, mergePoints[i].Index2);
            }
        }

        public static void MergeVerticesOnEdges(ControlMesh controlMesh, short[] pointIndices, SortedList<short, MergePoint> mergePoints)
        {
            if (controlMesh == null)
                return;

            for(var i = 0; i < pointIndices.Length; i++)
            {
                var pointIndex = pointIndices[i];
                var point		= controlMesh.Vertices[pointIndex];

                if (mergePoints.ContainsKey(pointIndex))
                    continue;

                var closestEdge			= -1;
                var closestDistance		= MathConstants.SnapDistanceSqr;
                var closestPointOnLine	= MathConstants.zeroVector3;
                for (var e = 0; e < controlMesh.Edges.Length; e++)
                {
                    var edgeIndex		= e;
                    var vertexIndex1	= controlMesh.GetVertexIndex(edgeIndex);
                    var vertexIndex2	= controlMesh.GetTwinEdgeVertexIndex(edgeIndex);
                    if (vertexIndex1 == pointIndex ||
                        vertexIndex2 == pointIndex)
                        continue;
                    var vertex1		= controlMesh.Vertices[vertexIndex1];
                    var vertex2		= controlMesh.Vertices[vertexIndex2];
                    var pointOnLine	= ProjectPointLine(point, vertex1, vertex2);
                    var delta		= pointOnLine - point;
                    var distance	= delta.sqrMagnitude;
                    if (distance < closestDistance)
                    {
                        closestEdge			= e;
                        closestDistance		= distance;
                        closestPointOnLine	= pointOnLine;
                    }
                }
                if (closestEdge != -1)
                {
                    var newPointIndex = InsertControlpoint(controlMesh, closestPointOnLine, closestEdge);
                    mergePoints.Add(newPointIndex, new MergePoint(newPointIndex, pointIndex));
                }
            }			
        }

        // TODO: put somewhere else
        public static Vector3 ProjectPointLine (Vector3 point, Vector3 lineStart, Vector3 lineEnd)
        {
            var relativePoint	= point - lineStart;
            var lineDirection	= lineEnd - lineStart;
            var length			= lineDirection.magnitude;
            var normalizedLineDirection = lineDirection;
            if (length > 0.000001f)
                normalizedLineDirection /= length;

            var dot = Vector3.Dot (normalizedLineDirection, relativePoint);
            dot = Mathf.Clamp (dot, 0.0F, length);
        
            return lineStart + normalizedLineDirection * dot;
        }

        public static void MergeVertices(ControlMesh controlMesh, short oldVertexIndex, short newVertexIndex)
        {
            if (newVertexIndex > oldVertexIndex)
                return;
            var currentEdges = controlMesh.Edges;
            for (var e = 0; e < currentEdges.Length; e++)
            {
                if (currentEdges[e].VertexIndex < oldVertexIndex)
                    continue;
                if (currentEdges[e].VertexIndex > oldVertexIndex)
                {
                    currentEdges[e].VertexIndex--;
                    continue;
                }
                currentEdges[e].VertexIndex = newVertexIndex;
            }

            ArrayUtility.RemoveAt(ref controlMesh.Vertices, oldVertexIndex);

            var removeEdges = new List<int>();
            for (var e = 0; e < currentEdges.Length; e++)
            {
                if (currentEdges[e].VertexIndex == currentEdges[currentEdges[e].TwinIndex].VertexIndex)
                {
                    removeEdges.Add(e);
                    //removeEdges.Add(edges[e].twinIndex);
                }
            }
            removeEdges.Sort();
            var polygons = controlMesh.Polygons;
            for (var i = removeEdges.Count - 1; i >= 0; i--)
            {
                var edgeIndex = removeEdges[i];
                for (var e = 0; e < currentEdges.Length; e++)
                {
                    if (currentEdges[e].TwinIndex <= edgeIndex)
                        continue;

                    currentEdges[e].TwinIndex--;
                }

                for (var p = 0; p < polygons.Length; p++)
                {
                    var edgeIndices = new List<int>(polygons[p].EdgeIndices);
                    for (var e = edgeIndices.Count - 1; e >= 0; e--)
                    {
                        if (edgeIndices[e] < edgeIndex)
                            continue;
                        if (edgeIndices[e] > edgeIndex)
                        {
                            edgeIndices[e]--;
                            continue;
                        }
                        edgeIndices.RemoveAt(e);
                    }
                    polygons[p].EdgeIndices = edgeIndices.ToArray();
                }
                ArrayUtility.RemoveAt(ref currentEdges, edgeIndex);
            }
            controlMesh.Edges = currentEdges;
        }

        public static void FixInvalidPolygons(ControlMesh controlMesh, Shape shape)
        {
            var checkPolygons = new List<short>();

            for (var p = (short)(controlMesh.Polygons.Length - 1); p >= 0; p--)
                checkPolygons.Add(p);
            
            for (var c = 0; c < checkPolygons.Count; c++)
            {
                var polygonIndex	= checkPolygons[c];
                var edgeIndices		= controlMesh.Polygons[polygonIndex].EdgeIndices;

                if (edgeIndices.Length > 2)
                {
                    for (var e0 = 0; e0 < edgeIndices.Length - 1; e0++)
                    {
                        var edgeIndex0		= edgeIndices[e0];
                        var vertexIndex0	= controlMesh.GetVertexIndex(edgeIndex0);
                        for (var e1 = e0 + 1; e1 < edgeIndices.Length; e1++)
                        {
                            var edgeIndex1		= edgeIndices[e1];
                            var vertexIndex1	= controlMesh.GetVertexIndex(edgeIndex1);
                            if (vertexIndex0 != vertexIndex1)
                                continue;

                            if ((e1 - e0) == 1)
                            {
                                // we've found an edge that starts and ends with the same vertex

                                var twinEdgeIndex1		= controlMesh.GetTwinEdgeIndex(edgeIndex1);
                                var twinPolygonIndex	= controlMesh.GetEdgePolygonIndex(twinEdgeIndex1);
                                var twinIndex1			= ArrayUtility.IndexOf(controlMesh.Polygons[twinPolygonIndex].EdgeIndices, twinEdgeIndex1);
                                ArrayUtility.RemoveAt(ref controlMesh.Polygons[twinPolygonIndex].EdgeIndices, twinIndex1);
                                ArrayUtility.RemoveAt(ref edgeIndices, e1);
                                controlMesh.Polygons[polygonIndex].EdgeIndices = edgeIndices;

                                RemoveEdges(controlMesh, twinEdgeIndex1, edgeIndex1);

                                // we might have more than one double vertex, so we need to look again.
                                checkPolygons.Add(polygonIndex);
                            } else
                            {
                                // we've found a polygon that has the same vertex in it twice,
                                // so we're going to split it at that vertex

                                var newEdges = GetEdgeRange(edgeIndices, controlMesh.GetNextEdgeIndex(edgeIndex1), edgeIndex0);
                                var oldEdges = GetEdgeRange(edgeIndices, controlMesh.GetNextEdgeIndex(edgeIndex0), edgeIndex1);

                                var newPolygonIndex = (short)controlMesh.Polygons.Length;
                                for (var i = 0; i < newEdges.Length; i++)
                                {
                                    var newEdgeIndex = newEdges[i];
                                    controlMesh.Edges[newEdgeIndex].PolygonIndex = newPolygonIndex;
                                }
                                    
                                controlMesh.Polygons[polygonIndex].EdgeIndices = oldEdges;
                                var newPolygon = new Polygon(newEdges, CopyTexGen(shape, controlMesh.Polygons[polygonIndex].TexGenIndex));
                                AddNewPolygon(controlMesh, shape, newPolygon);

                                // we might have more than one double vertex, so we need to look again.
                                checkPolygons.Add(polygonIndex);
                                checkPolygons.Add(newPolygonIndex);
                            }

                            goto exit_loops;
                        }
                    }
                    exit_loops: 
                    continue;
                }
                if (edgeIndices.Length == 2)
                {
                    var edgeIndex1 = edgeIndices[0];
                    var edgeIndex2 = edgeIndices[1];

                    var twinIndex1 = controlMesh.GetTwinEdgeIndex(edgeIndex1);
                    var twinIndex2 = controlMesh.GetTwinEdgeIndex(edgeIndex2);

                    controlMesh.Edges[twinIndex1].TwinIndex = twinIndex2;
                    controlMesh.Edges[twinIndex2].TwinIndex = twinIndex1;
                    for (var c2 = c + 1; c2 < checkPolygons.Count; c2++)
                    {
                        if (checkPolygons[c2] > polygonIndex)
                            checkPolygons[c2]--;
                    }
                    RemovePolygon(controlMesh, shape, polygonIndex);  
                    RemoveEdges(controlMesh, edgeIndex1, edgeIndex2);
                } else
                if (edgeIndices.Length == 1)
                {
                    Debug.LogWarning("polygon.edgeIndices.Length == 1!");
                    break;
                } else
                if (edgeIndices.Length == 0)
                {
                    for (var c2 = c+1; c2 < checkPolygons.Count; c2++)
                    {
                        if (checkPolygons[c2] > polygonIndex)
                            checkPolygons[c2]--;
                    }
                    RemovePolygon(controlMesh, shape, polygonIndex);
                }
            }

            RemoveUnusedTexGens(controlMesh, shape);

            if (controlMesh.Polygons.Length != 0 && 
                controlMesh.Edges.Length    != 0 && 
                controlMesh.Vertices.Length != 0)
                return;

            if (controlMesh.Polygons.Length	!= 0) controlMesh.Polygons	= new Polygon[0];
            if (controlMesh.Edges.Length	!= 0) controlMesh.Edges		= new HalfEdge[0];
            if (controlMesh.Vertices.Length	!= 0) controlMesh.Vertices	= new Vector3[0];
            shape.Reset();
        }
        
        private static void AddNewPolygon(ControlMesh controlMesh, Shape shape, Polygon newPolygon)
        {
            ArrayUtility.Add(ref controlMesh.Polygons, newPolygon);

            var newSurface = new Surface
            {
                Plane		= GeometryUtility.CalcPolygonPlane(controlMesh, (short) (controlMesh.Polygons.Length - 1)),
                TexGenIndex = newPolygon.TexGenIndex
            };
            GeometryUtility.CalculateTangents(newSurface.Plane.normal, out newSurface.Tangent, out newSurface.BiNormal);

            ArrayUtility.Add(ref shape.Surfaces, newSurface);
        }

        private static void AddNewPolygon(ControlMesh controlMesh, Shape shape, Polygon newPolygon, int surfaceIndex)
        {
            ArrayUtility.Add(ref controlMesh.Polygons, newPolygon);
            var oldSurface = shape.Surfaces[surfaceIndex];
            var newSurface = new Surface
            {
                BiNormal	= oldSurface.BiNormal,
                Plane		= oldSurface.Plane,
                Tangent		= oldSurface.Tangent,
                TexGenIndex = newPolygon.TexGenIndex
            };
            ArrayUtility.Add(ref shape.Surfaces, newSurface);
        }

        private static int[] GetEdgeRange(int[] edges, int fromValue, int toValue, int additionalStorage = 0)
        {
            var fromEdgeIndex	= ArrayUtility.IndexOf(edges, fromValue);
            var toEdgeIndex		= ArrayUtility.IndexOf(edges, toValue);
            if (fromEdgeIndex == -1 ||
                toEdgeIndex == -1)
                return null;

            if (fromEdgeIndex < toEdgeIndex)
            {
                var length = (toEdgeIndex - fromEdgeIndex) + 1;
                var result = new int[length + additionalStorage];
                Array.Copy(edges, fromEdgeIndex, result, 0, length);
                return result;
            } else
            {
                var offsetA = (edges.Length - fromEdgeIndex);
                var offsetB = (toEdgeIndex + 1);
                var length	= offsetA + offsetB;
                var result	= new int[length + additionalStorage];
                if (offsetA > 0)
                {
                    Array.Copy(edges, fromEdgeIndex, result, 0, offsetA);
                }
                Array.Copy(edges, 0, result, offsetA, offsetB);
                return result;
            }
        }

        public static int InsertEdgeBetweenVertices(ControlMesh controlMesh, Shape shape, short vertexIndex1, short vertexIndex2)
        {
            // create an edge between the two vertices that splits an existing polygon into two
            for (var polygonIndex = 0; polygonIndex < controlMesh.Polygons.Length; polygonIndex++)
            {
                var edgeIndices	= controlMesh.Polygons[polygonIndex].EdgeIndices;
                var edgeIndex1	= -1;
                var edgeIndex2	= -1;
                for (var e = 0; e < edgeIndices.Length; e++)
                {
                    var edgeIndex	= edgeIndices[e];
                    var vertexIndex	= controlMesh.GetVertexIndex(edgeIndex);
                    if		(vertexIndex1 == vertexIndex) edgeIndex1 = edgeIndex;
                    else if (vertexIndex2 == vertexIndex) edgeIndex2 = edgeIndex;
                }

                if (edgeIndex1 == -1 || edgeIndex2 == -1)
                    continue;

                var nextIndex1	= controlMesh.GetNextEdgeIndex(edgeIndex1);
                var nextIndex2	= controlMesh.GetNextEdgeIndex(edgeIndex2);

                if (nextIndex1 == edgeIndex2 ||
                    nextIndex2 == edgeIndex1)
                {
                    Debug.LogWarning("next_index1 == edge_index2 || next_index2 == edge_index1");
                    return -1;//vertexIndex
                }
                    
                var oldPolygonIndex	= (short)polygonIndex;
                var newPolygonIndex	= (short)controlMesh.Polygons.Length;

                var newEdgeIndex1	= controlMesh.Edges.Length;
                var newEdgeIndex2	= controlMesh.Edges.Length + 1;

                var newEdge1		= new HalfEdge(newPolygonIndex, newEdgeIndex2, vertexIndex2, true);
                var newEdge2		= new HalfEdge(oldPolygonIndex, newEdgeIndex1, vertexIndex1, true);

                // edge_index_2 leads to new_edge_2 leads to next_index_1 (new_polygon)
                var oldEdges = GetEdgeRange(edgeIndices, nextIndex1, edgeIndex2, 1);
                if (oldEdges == null)
                {
                    Debug.LogWarning("old_edges == null");
                    return -1;//vertexIndex
                }
                oldEdges[oldEdges.Length - 1] = newEdgeIndex2;

                // edge_index_1 leads to new_edge_1 leads to next_index_2 (old_polygon)
                var newEdges = GetEdgeRange(edgeIndices, nextIndex2, edgeIndex1, 1);
                if (newEdges == null)
                {
                    Debug.LogWarning("new_edges == null");
                    return -1;//vertexIndex
                }
                newEdges[newEdges.Length - 1] = newEdgeIndex1;
                     
                ArrayUtility.Add(ref controlMesh.Edges, newEdge1);
                ArrayUtility.Add(ref controlMesh.Edges, newEdge2);
                for (var i = 0; i < oldEdges.Length; i++)
                {
                    controlMesh.Edges[oldEdges[i]].PolygonIndex = oldPolygonIndex;
                }
                for (var i = 0; i < newEdges.Length; i++)
                {
                    controlMesh.Edges[newEdges[i]].PolygonIndex = newPolygonIndex;
                }
                var newPolygon = new Polygon(newEdges, CopyTexGen(shape, controlMesh.Polygons[oldPolygonIndex].TexGenIndex));
                AddNewPolygon(controlMesh, shape, newPolygon, oldPolygonIndex);

                controlMesh.Polygons[polygonIndex].EdgeIndices = oldEdges;

                RemoveUnusedTexGens(controlMesh, shape);

                Validate(controlMesh, shape);
                return newEdgeIndex1;
            }
            return -1;//vertexIndex
        }

        public static int InsertEdgeBetweenEdges(ControlMesh controlMesh, Shape shape, int edgeIndex1, int edgeIndex2)
        {
            // create an edge between the two edges that splits an existing polygon into two
            
            if (edgeIndex1 == -1 ||
                edgeIndex2 == -1)
            {
                Debug.LogWarning("edgeIndex1 == -1 || edgeIndex2 == -1");
                return -1;//edgeIndex
            }

            var edgePolygonIndex = controlMesh.GetEdgePolygonIndex(edgeIndex1);
            if (edgePolygonIndex == -1)
            {
                Debug.LogWarning("polygon_index == -1");
                return -1;//edgeIndex
            }

            if (controlMesh.GetEdgePolygonIndex(edgeIndex2) != edgePolygonIndex)
            {
                Debug.LogWarning("controlMesh.GetEdgePolygonIndex(edgeIndex2) != polygon_index");
                return -1;//edgeIndex
            }
            
            var edgeIndices	= controlMesh.Polygons[edgePolygonIndex].EdgeIndices;
            
            var nextIndex1	= controlMesh.GetNextEdgeIndex(edgeIndex1);
            var nextIndex2	= controlMesh.GetNextEdgeIndex(edgeIndex2);

            if (nextIndex1 == edgeIndex2 ||
                nextIndex2 == edgeIndex1)
            {
                Debug.LogWarning("next_index1 == edge_index2 || next_index2 == edge_index1");
                return -1;//edgeIndex
            }

            var vertexIndex1	= controlMesh.GetVertexIndex(edgeIndex1);
            var vertexIndex2	= controlMesh.GetVertexIndex(edgeIndex2);

            var oldPolygonIndex	= edgePolygonIndex;
            var newPolygonIndex	= (short)controlMesh.Polygons.Length;

            var newEdgeIndex1	= controlMesh.Edges.Length;
            var newEdgeIndex2	= controlMesh.Edges.Length + 1;

            var newEdge1		= new HalfEdge(newPolygonIndex, newEdgeIndex2, vertexIndex2, true);
            var newEdge2		= new HalfEdge(oldPolygonIndex, newEdgeIndex1, vertexIndex1, true);

            // edge_index_2 leads to new_edge_2 leads to next_index_1 (new_polygon)
            var oldEdges = GetEdgeRange(edgeIndices, nextIndex1, edgeIndex2, 1);
            if (oldEdges == null)
            {
                Debug.LogWarning("old_edges == null");
                return -1;//edgeIndex
            }
            oldEdges[oldEdges.Length - 1] = newEdgeIndex2;

            // edge_index_1 leads to new_edge_1 leads to next_index_2 (old_polygon)
            var newEdges = GetEdgeRange(edgeIndices, nextIndex2, edgeIndex1, 1);
            if (newEdges == null)
            {
                Debug.LogWarning("new_edges == null");
                return -1;//edgeIndex
            }
            newEdges[newEdges.Length - 1] = newEdgeIndex1;
                     
            ArrayUtility.Add(ref controlMesh.Edges, newEdge1);
            ArrayUtility.Add(ref controlMesh.Edges, newEdge2);
            for (var i = 0; i < oldEdges.Length; i++)
            {
                controlMesh.Edges[oldEdges[i]].PolygonIndex = oldPolygonIndex;
            }
            for (var i = 0; i < newEdges.Length; i++)
            {
                controlMesh.Edges[newEdges[i]].PolygonIndex = newPolygonIndex;
            }
            var newTexGen	= CopyTexGen(shape, controlMesh.Polygons[oldPolygonIndex].TexGenIndex);
            var newPolygon	= new Polygon(newEdges, newTexGen);
            AddNewPolygon(controlMesh, shape, newPolygon, oldPolygonIndex);
            controlMesh.Polygons[edgePolygonIndex].EdgeIndices = oldEdges;

            //FixTexGens(controlMesh, shape);
            //Validate(controlMesh, shape);
            return newEdgeIndex1;
        }

        public static short InsertControlpoint(ControlMesh controlMesh, Vector3 newPoint, int edgeIndex)
        {
            var selfEdgeIndex		= edgeIndex;
            var selfEdge			= controlMesh.Edges[selfEdgeIndex];
            var selfEdgeIndices		= controlMesh.Polygons[selfEdge.PolygonIndex].EdgeIndices;
            var selfPolygonIndex	= ArrayUtility.IndexOf(selfEdgeIndices, selfEdgeIndex);

            var twinEdgeIndex		= selfEdge.TwinIndex;
            var twinEdge			= controlMesh.Edges[twinEdgeIndex];
            var twinEdgeIndices		= controlMesh.Polygons[twinEdge.PolygonIndex].EdgeIndices;
            var twinPolygonIndex	= ArrayUtility.IndexOf(twinEdgeIndices, twinEdgeIndex);

            var oldVertexIndex		= selfEdge.VertexIndex;
            var newVertexIndex		= (short)controlMesh.Vertices.Length;
            ArrayUtility.Add(ref controlMesh.Vertices, newPoint);

            //
            //        self
            //	o<--------------
            //   --------------->
            //        twin
            //

            //
            //   n-self    self    
            //	o<----- x<------		x = new vertex
            //   ------>x ------>		o = old vertex
            //   n-twin     twin
            //

            var newSelfIndex	= controlMesh.Edges.Length;
            var newTwinIndex	= newSelfIndex + 1;
            
            var newEdge		= new HalfEdge(selfEdge.PolygonIndex,
                                            newTwinIndex,
                                            oldVertexIndex, true);

            var newTwin		= new HalfEdge(twinEdge.PolygonIndex,
                                            newSelfIndex,
                                            newVertexIndex, true);

            ArrayUtility.Add(ref controlMesh.Edges, newEdge);
            ArrayUtility.Add(ref controlMesh.Edges, newTwin);

            controlMesh.Edges[selfEdgeIndex].VertexIndex = newVertexIndex;
            ArrayUtility.Insert(ref selfEdgeIndices, selfPolygonIndex + 1, newSelfIndex);
            ArrayUtility.Insert(ref twinEdgeIndices, twinPolygonIndex    , newTwinIndex);

            controlMesh.Polygons[selfEdge.PolygonIndex].EdgeIndices = selfEdgeIndices;
            controlMesh.Polygons[twinEdge.PolygonIndex].EdgeIndices = twinEdgeIndices;
            //Validate();

            return newVertexIndex;
        }

        private static int FirstEdgeWithVertex(ControlMesh controlMesh, int vertexIndex)
        {
            for (var i = 0; i < controlMesh.Edges.Length; i++)
            {
                if (controlMesh.Edges[i].VertexIndex == vertexIndex)
                    return i;
            }
            return -1;//edgeIndex
        }

        private static void RemoveEdges(ControlMesh controlMesh, HashSet<int> removeEdges)
        {
            if (removeEdges.Count == 0)
                return;

            var sortedRemoveEdges = new List<int>(removeEdges);
            sortedRemoveEdges.Sort();
                
            for (var s = sortedRemoveEdges.Count - 1; s >= 0; s--)
            {
                var value = sortedRemoveEdges[s];
                ArrayUtility.RemoveAt(ref controlMesh.Edges, value);
            }
            for (var p = 0; p < controlMesh.Polygons.Length; p++)
            {
                var edgeIndices = controlMesh.Polygons[p].EdgeIndices;
                for (var i = 0; i < edgeIndices.Length; i++)
                {
                    var original = edgeIndices[i];
                    for (var s = sortedRemoveEdges.Count - 1; s >= 0; s--)
                    {
                        var value = sortedRemoveEdges[s];
                        if (original < value)
                            continue;

                        original -= (s + 1);
                        break;
                    }
                    edgeIndices[i] = original;
                }
            }
            for (var i = 0; i < controlMesh.Edges.Length; i++)
            {
                var original = controlMesh.Edges[i].TwinIndex;
                for (var s = sortedRemoveEdges.Count - 1; s >= 0; s--)
                {
                    var value = sortedRemoveEdges[s];
                    if (original < value)
                        continue;

                    original -= (s + 1);
                    break;
                }
                controlMesh.Edges[i].TwinIndex = original;
            }
        }

        private static void RemoveEdges(ControlMesh controlMesh, params int[] removeEdges)
        {
            var sortedRemoveEdges = new List<int>(removeEdges);
            sortedRemoveEdges.Sort();
                
            for (var s = sortedRemoveEdges.Count - 1; s >= 0; s--)
            {
                var value = sortedRemoveEdges[s];
                ArrayUtility.RemoveAt(ref controlMesh.Edges, value);
            }
            for (var p = 0; p < controlMesh.Polygons.Length; p++)
            {
                var edgeIndices = controlMesh.Polygons[p].EdgeIndices;
                for (var i = 0; i < edgeIndices.Length; i++)
                {
                    var original = edgeIndices[i];
                    for (var s = sortedRemoveEdges.Count - 1; s >= 0; s--)
                    {
                        var value = sortedRemoveEdges[s];
                        if (original < value)
                            continue;

                        original -= (s + 1);
                        break;
                    }
                    edgeIndices[i] = original;
                }
            }
            for (var i = 0; i < controlMesh.Edges.Length; i++)
            {
                var original = controlMesh.Edges[i].TwinIndex;
                for (var s = sortedRemoveEdges.Count - 1; s >= 0; s--)
                {
                    var value = sortedRemoveEdges[s];
                    if (original < value)
                        continue;

                    original -= (s + 1);
                    break;
                }
                controlMesh.Edges[i].TwinIndex = original;
            }
        }

        public static void CalculatePlanes(ControlMesh controlMesh, Shape shape)
        {
            for (short p = 0; p < controlMesh.Polygons.Length; p++)
            {
                shape.Surfaces[p].Plane = GeometryUtility.CalcPolygonPlane(controlMesh, p);
            }
        }

        private static void RemoveVertex(ControlMesh controlMesh, short vertexIndex)
        {
            ArrayUtility.RemoveAt(ref controlMesh.Vertices, vertexIndex);

            for (var i = 0; i < controlMesh.Edges.Length; i++)
            {
                if (controlMesh.Edges[i].VertexIndex >= vertexIndex) controlMesh.Edges[i].VertexIndex--;
            }
        }

        private static void RemoveEdgeFromPolygon(ControlMesh controlMesh, int edgeIndex, ref HashSet<int> removeEdges)
        {
            if (!removeEdges.Add(edgeIndex))
                return;

            var polyIndex = controlMesh.Edges[edgeIndex].PolygonIndex;
            ArrayUtility.Remove(ref controlMesh.Polygons[polyIndex].EdgeIndices, edgeIndex);
        }

        static void RemovePolygons(ControlMesh controlMesh, Shape shape, HashSet<short> removePolygons)
        {
            var sortedRemovePolygons = new List<short>(removePolygons);
            sortedRemovePolygons.Sort();
                
            for (var s = sortedRemovePolygons.Count - 1; s >= 0; s--)
            {
                var value = sortedRemovePolygons[s];
                ArrayUtility.RemoveAt(ref controlMesh.Polygons, value);
                ArrayUtility.RemoveAt(ref shape.Surfaces, value);
            }
            for (var p = 0; p < controlMesh.Polygons.Length; p++)
            {
                var edgeIndices = controlMesh.Polygons[p].EdgeIndices;
                for (var i = 0; i < edgeIndices.Length; i++)
                {
                    controlMesh.Edges[edgeIndices[i]].PolygonIndex = (short)p;
                }
            }
        }

        static void RemovePolygon(ControlMesh controlMesh, Shape shape, short polygonIndex)
        {
            ArrayUtility.RemoveAt(ref controlMesh.Polygons, polygonIndex);
            ArrayUtility.RemoveAt(ref shape.Surfaces, polygonIndex);
            for (var p = 0; p < controlMesh.Polygons.Length; p++)
            {
                var polyEdgeIndices = controlMesh.Polygons[p].EdgeIndices;
                for (var i = 0; i < polyEdgeIndices.Length; i++)
                {
                    controlMesh.Edges[polyEdgeIndices[i]].PolygonIndex = (short)p;
                }
            }
        }

        // assumes mergePoints is sorted
        public static void RemoveControlPoints(ControlMesh controlMesh, Shape shape, short[] sortedRemovePoints)
        {
            for (var i = sortedRemovePoints.Length - 1; i >= 0; i--)
            {
                RemoveControlPoint(controlMesh, shape, sortedRemovePoints[i]);
            }
        }

        private static void RemoveControlPoint(ControlMesh controlMesh, Shape shape, short indexOfVertexToRemove)
        {
            var firstEdgeIndex = FirstEdgeWithVertex(controlMesh, indexOfVertexToRemove);
            if (firstEdgeIndex == -1)
                return;

            var edgeIndices	= new List<int>();
            var tested		= new HashSet<int>();
            
            var iterator = firstEdgeIndex;
            do
            {
                if (!tested.Add(iterator))
                    break;
                edgeIndices.Add(iterator);
                iterator = controlMesh.GetNextEdgeIndexAroundVertex(iterator);

            } while (iterator != firstEdgeIndex);

            {
                var removeEdges = new HashSet<int>();

                var newPolygonIndex = (short)controlMesh.Polygons.Length;

                var newEdges = new List<int>();

                for (int i = 0, j = edgeIndices.Count - 1; j >= 0; i = j, j--)
                {
                    var currIndex	 = edgeIndices[i];
                    var vertexIndex	 = controlMesh.GetTwinEdgeVertexIndex(edgeIndices[j]);
                    var twinIndex	 = controlMesh.GetTwinEdgeIndex(currIndex);
                    var edgeIndex	 = controlMesh.Edges.Length;

                    ArrayUtility.Add(ref controlMesh.Edges, new HalfEdge(newPolygonIndex, twinIndex, vertexIndex, true));
                    newEdges.Add(edgeIndex); 
                    removeEdges.Add(currIndex);
                }

                for (var e = 0; e < newEdges.Count; e++)
                {
                    var index = newEdges[e];
                    controlMesh.Edges[controlMesh.Edges[index].TwinIndex].TwinIndex = index;
                }

                AddNewPolygon(controlMesh, shape, new Polygon(newEdges.ToArray(), CreateTexGen(shape)));

                var removePolygons = new HashSet<short>();
                foreach (var index in removeEdges)
                {
                    var polygonIndex = controlMesh.Edges[index].PolygonIndex;
                    ArrayUtility.Remove(ref controlMesh.Polygons[polygonIndex].EdgeIndices, index);
                }

                for (var p = 0; p < controlMesh.Polygons.Length; p++)
                {
                    var polyEdgeIndices = controlMesh.Polygons[p].EdgeIndices;
                    switch (polyEdgeIndices.Length)
                    {
                        case 0:
                        {
                            controlMesh.Polygons[newPolygonIndex].TexGenIndex = controlMesh.Polygons[p].TexGenIndex;
                            removePolygons.Add((short)p);
                            break;
                        }
                        case 1:
                        {
                            controlMesh.Polygons[newPolygonIndex].TexGenIndex = controlMesh.Polygons[p].TexGenIndex;
                            RemoveUnusedTexGens(controlMesh, shape);
                            var selfEdgeIndex0 = polyEdgeIndices[0];
                            var twinEdgeIndex0 = controlMesh.GetTwinEdgeIndex(selfEdgeIndex0);
                            RemoveEdgeFromPolygon(controlMesh, selfEdgeIndex0, ref removeEdges);
                            RemoveEdgeFromPolygon(controlMesh, twinEdgeIndex0, ref removeEdges);
                            removePolygons.Add((short)p);
                            break;
                        }
                        case 2:
                        {
                            controlMesh.Polygons[newPolygonIndex].TexGenIndex = controlMesh.Polygons[p].TexGenIndex;
                            RemoveUnusedTexGens(controlMesh, shape);
                            var selfEdgeIndex0 = polyEdgeIndices[0];
                            var selfEdgeIndex1 = polyEdgeIndices[1];
                            var twinEdgeIndex0 = controlMesh.GetTwinEdgeIndex(selfEdgeIndex0);
                            var twinEdgeIndex1 = controlMesh.GetTwinEdgeIndex(selfEdgeIndex1);
                            controlMesh.Edges[twinEdgeIndex0].TwinIndex = twinEdgeIndex1;
                            controlMesh.Edges[twinEdgeIndex1].TwinIndex = twinEdgeIndex0;
                            RemoveEdgeFromPolygon(controlMesh, selfEdgeIndex0, ref removeEdges);
                            RemoveEdgeFromPolygon(controlMesh, selfEdgeIndex1, ref removeEdges);
                            removePolygons.Add((short)p);
                            break;
                        }
                    }
                }

                if (removePolygons.Count != 0)
                {
                    var smallestTexGen = int.MaxValue;
                    foreach (var polygonIndex in removePolygons)
                    {
                        if (controlMesh.Polygons[polygonIndex].TexGenIndex >= 0)
                            smallestTexGen = Math.Min(controlMesh.Polygons[polygonIndex].TexGenIndex, smallestTexGen);
                    }
                    if (smallestTexGen != int.MaxValue)
                    {
                        controlMesh.Polygons[newPolygonIndex].TexGenIndex = smallestTexGen;
                    }
                    RemovePolygons(controlMesh, shape, removePolygons);
                } else
                {
                    controlMesh.Polygons[newPolygonIndex].TexGenIndex = CopyTexGen(shape, 0);
                }


                if (removeEdges.Count != 0)
                {
                    RemoveEdges(controlMesh, removeEdges);
                }
                                
                // check if vertex is still being used or not 
                //	(would hide issues if we removed it when it's still being used somehow)
                firstEdgeIndex = FirstEdgeWithVertex(controlMesh, indexOfVertexToRemove);
                if (firstEdgeIndex == -1)
                {
                    RemoveVertex(controlMesh, indexOfVertexToRemove);
                }

                RemoveUnusedTexGens(controlMesh, shape);

                if (controlMesh.Polygons.Length == 0 ||
                    controlMesh.Edges.Length == 0 ||
                    controlMesh.Vertices.Length == 0)
                {
                    if (controlMesh.Polygons.Length	!= 0) controlMesh.Polygons	= new Polygon[0];
                    if (controlMesh.Edges.Length	!= 0) controlMesh.Edges		= new HalfEdge[0];
                    if (controlMesh.Vertices.Length	!= 0) controlMesh.Vertices	= new Vector3[0];
                    shape.Reset();
                }

                Validate(controlMesh, shape);
            }
        }

        private static int CreateTexGen(Shape shape)
        {
            var texGen = new TexGen(CSGSettings.DefaultMaterial);
            ArrayUtility.Add(ref shape.TexGens,		texGen);
            ArrayUtility.Add(ref shape.TexGenFlags, RealtimeCSG.CSGSettings.DefaultTexGenFlags);
            return shape.TexGens.Length - 1;
        }

        private static int CopyTexGen(Shape shape, int srcTexGenIndex)
        {
            var texGen = shape.TexGens[srcTexGenIndex];
            ArrayUtility.Add(ref shape.TexGens, texGen);
            ArrayUtility.Add(ref shape.TexGenFlags, shape.TexGenFlags[srcTexGenIndex]);
            return shape.TexGens.Length - 1;
        }
        
        static bool[]	__usedTexGens	= new bool[0];
        static int[]	__indexConversion = new int[0];
        public static void RemoveUnusedTexGens(ControlMesh controlMesh, Shape shape)
        {
            int texGenCount = shape.TexGens.Length;
            var polygons = controlMesh.Polygons;
            for (var p = 0; p < polygons.Length; p++)
            {
                if (polygons[p].TexGenIndex < 0 ||
                    polygons[p].TexGenIndex >= texGenCount)
                {
                    polygons[p].TexGenIndex = CreateTexGen(shape);
                }
            }

            if (__usedTexGens.Length < texGenCount)
            {
                __usedTexGens		= new bool[texGenCount];
                __indexConversion	= new int[texGenCount];
            }
            Array.Clear(__usedTexGens,0,__usedTexGens.Length);
            for (var p = 0; p < polygons.Length; p++)
            {							
                __usedTexGens[polygons[p].TexGenIndex] = true;
            }


            var materialCount = texGenCount;
            for(var i = 0; i < materialCount; i++)
            {
                if (__usedTexGens[i])
                {
                    __indexConversion[i] = i;
                    continue;
                }

                var found = -1;//texgen
                for (var j = materialCount - 1; j > i; j--)
                {
                    if (!__usedTexGens[j])
                        continue;

                    found = j;
                    break;
                }

                if (found == -1)//texgen
                    break;

                __indexConversion[found] = i;
                
                shape.TexGenFlags[i] = shape.TexGenFlags[found];
                shape.TexGens[i] = shape.TexGens[found];
                
                __usedTexGens[i] = true;
                __usedTexGens[found] = false;
                materialCount--;
            }

            if (materialCount == texGenCount)
                return;

            for (var i = 0; i < controlMesh.Polygons.Length; i++)
            {
                var texGenIndex = controlMesh.Polygons[i].TexGenIndex;
                controlMesh.Polygons[i].TexGenIndex = __indexConversion[texGenIndex];
            }

            for (var i = texGenCount - 1; i >= 0; i--)
            {
                if (__usedTexGens[i])
                    break;
                
                ArrayUtility.RemoveAt(ref shape.TexGenFlags, i);
                ArrayUtility.RemoveAt(ref shape.TexGens, i);
            }

            for (int i = 0, maxI = controlMesh.Polygons.Length - 1; i < maxI; i++)
            {
                for (int j = i + 1, maxJ = controlMesh.Polygons.Length; j < maxJ; j++)
                {
                    if (controlMesh.Polygons[i].TexGenIndex > controlMesh.Polygons[j].TexGenIndex)
                    {
                        var tempFlags = shape.TexGenFlags[i];
                        shape.TexGenFlags[i] = shape.TexGenFlags[j];
                        shape.TexGenFlags[j] = tempFlags;

                        var tempTexGens = shape.TexGens[i];
                        shape.TexGens[i] = shape.TexGens[j];
                        shape.TexGens[j] = tempTexGens;

                        var tempTexGenIndex = controlMesh.Polygons[i].TexGenIndex;
                        controlMesh.Polygons[i].TexGenIndex = controlMesh.Polygons[j].TexGenIndex;
                        controlMesh.Polygons[j].TexGenIndex = tempTexGenIndex;
                    }
                }
            }

            for (var i = 0; i < controlMesh.Polygons.Length; i++)
            {
                shape.Surfaces[i].TexGenIndex = controlMesh.Polygons[i].TexGenIndex;
            }
        }
        
        public static void FixTexGens(ControlMesh controlMesh, Shape shape)
        {
            var texGenCount = shape.TexGens.Length;
            if (__usedTexGens.Length < texGenCount)
            {
                __usedTexGens		= new bool[texGenCount];
                __indexConversion	= new int[texGenCount];
            }
            Array.Clear(__usedTexGens,0,__usedTexGens.Length);
            for (var p = 0; p < controlMesh.Polygons.Length; p++)
            {
                var texGenIndex = controlMesh.Polygons[p].TexGenIndex;
                if (texGenIndex < 0 ||
                    texGenIndex >= shape.TexGens.Length)
                {
                    texGenIndex = CreateTexGen(shape);
                    controlMesh.Polygons[p].TexGenIndex = texGenIndex;
                }
                __usedTexGens[texGenIndex] = true;
            }

            for (var t = shape.TexGens.Length - 1; t >= 0; t--)
            {
                if (__usedTexGens[t])
                    continue;

                ArrayUtility.RemoveAt(ref shape.TexGens, t);
                ArrayUtility.RemoveAt(ref shape.TexGenFlags, t);

                // TODO: there's probably a better way of doing this ..
                for (var p = 0; p < controlMesh.Polygons.Length; p++)
                {
                    if (controlMesh.Polygons[p].TexGenIndex > t)
                        controlMesh.Polygons[p].TexGenIndex--;
                }
                for (var s = 0; s < shape.Surfaces.Length; s++)
                {
                    if (shape.Surfaces[s].TexGenIndex > t)
                        shape.Surfaces[s].TexGenIndex--;
                }
            }
        }

        public static bool SplitEdgeLoop(ControlMesh controlMesh, Shape shape, List<int> edgeLoop)
        {
            var polygon1Edges = new int[edgeLoop.Count];
            var polygon2Edges = new int[edgeLoop.Count];
            var polygon1Index = (short)(controlMesh.Polygons.Length);
            var polygon2Index = (short)(controlMesh.Polygons.Length + 1);
            for (var i = 0; i < edgeLoop.Count; i++)
            {
                var currEdgeIndex	= edgeLoop[i];
                var twinEdgeIndex	= controlMesh.GetTwinEdgeIndex(currEdgeIndex);
                var currVertexIndex = controlMesh.GetVertexIndex(currEdgeIndex);
                var twinVertexIndex = controlMesh.GetVertexIndex(twinEdgeIndex);

                var newEdge1		= new HalfEdge(polygon1Index, currEdgeIndex, twinVertexIndex, true);
                var newEdge2		= new HalfEdge(polygon2Index, twinEdgeIndex, currVertexIndex, true);
                var newEdgeIndex1	= controlMesh.Edges.Length;
                var newEdgeIndex2	= controlMesh.Edges.Length + 1;
                ArrayUtility.Add(ref controlMesh.Edges, newEdge1);
                ArrayUtility.Add(ref controlMesh.Edges, newEdge2);

                controlMesh.Edges[currEdgeIndex].TwinIndex = newEdgeIndex1;
                controlMesh.Edges[twinEdgeIndex].TwinIndex = newEdgeIndex2;

                polygon1Edges[(edgeLoop.Count - 1) - i] = newEdgeIndex1;
                polygon2Edges[i] = newEdgeIndex2;
            }

            var polygon1 = new Polygon(polygon1Edges, CreateTexGen(shape));
            var polygon2 = new Polygon(polygon2Edges, CreateTexGen(shape));

            AddNewPolygon(controlMesh, shape, polygon1);
            AddNewPolygon(controlMesh, shape, polygon2);

            for (var i = 0; i < polygon2.EdgeIndices.Length; i++)
            {
                var edgeIndex		= polygon2.EdgeIndices[i];
                var edgeVertexIndex	= controlMesh.GetVertexIndex(edgeIndex);
                var edgeVertex		= controlMesh.Vertices[edgeVertexIndex];
                var newVertexIndex	= (short)controlMesh.Vertices.Length;

                ArrayUtility.Add(ref controlMesh.Vertices, edgeVertex);

                var iterator = edgeIndex;
                do
                {
                    controlMesh.Edges[iterator].VertexIndex = newVertexIndex;
                    iterator = controlMesh.GetNextEdgeIndexAroundVertex(iterator);
                } while (iterator != edgeIndex);
            }

            Validate(controlMesh, shape);
            return true;
        }

        private const float kDistanceEpsilon = 0.0001f;

        private struct VertexSide
        {
            public float	Distance;
            public int		Halfspace;
        }

        private struct TraversalSide
        {
            public VertexSide	Side;
            public int			EdgeIndex;
        };

        private struct PlaneTraversals
        {
            public TraversalSide Traversal0;
            public TraversalSide Traversal1;
        };


        public static bool CutMesh(ControlMesh controlMesh, Shape shape, CSGPlane cuttingPlane, ref List<int> intersectedEdges)
        {
            //float time0 = Time.realtimeSinceStartup;
            if (shape.Surfaces == null ||
                shape.Surfaces.Length != controlMesh.Polygons.Length)
            {
                CalculatePlanes(controlMesh, shape);
            }


            // TODO: make sure we don't cut the mesh on a single polygon

            // TODO: do not store controlMesh anymore and calculate it in the loop instead

            //float time1 = Time.realtimeSinceStartup;
            var vertexDistances = new VertexSide[controlMesh.Vertices.Length];
            for (var p = 0; p < controlMesh.Vertices.Length; p++)
            {
                var distance	= cuttingPlane.Distance(controlMesh.Vertices[p]);
                var	halfspace	= (short)((distance < -kDistanceEpsilon) ? -1 : (distance > kDistanceEpsilon) ? 1 : 0);
                vertexDistances[p].Distance	= distance;
                vertexDistances[p].Halfspace	= halfspace;
            }

            if (intersectedEdges != null)
                intersectedEdges.Clear();

            //float time2 = Time.realtimeSinceStartup;
            //float timeA = 0, timeB = 0, timeC = 0, timeD = 0, timeE = 0;
            var intersections		= new PlaneTraversals[8];
            for (var p = controlMesh.Polygons.Length - 1; p >= 0; p--)
            {
                // TODO: use polygon AABB tests

                var edgeIndices	= controlMesh.Polygons[p].EdgeIndices;

                PlaneTraversals intersection;
                { 
                    var index0			= edgeIndices.Length - 1;
                    var edgeIndex0		= edgeIndices[index0];
                    var vertexIndex0	= controlMesh.Edges[edgeIndex0].VertexIndex;

                    intersection.Traversal1.Side		= vertexDistances[vertexIndex0];
                    intersection.Traversal1.EdgeIndex	= edgeIndex0;
                }

                var intersectionCount	= 0;
                for (var index1 = 0; index1 < edgeIndices.Length; index1++)
                {
                    intersection.Traversal0 = intersection.Traversal1;

                    // TODO: this could be optimized if this.polygons and this.edges are combined 
                    //		 (index/edge_index would be equivalent, edge w/ vertexIndex in 'polygon', fewer cache misses)
                    var edgeIndex1		= edgeIndices[index1];
                    var vertexIndex1	= controlMesh.Edges[edgeIndex1].VertexIndex;

                    intersection.Traversal1.Side		= vertexDistances[vertexIndex1];
                    intersection.Traversal1.EdgeIndex	= edgeIndex1;

                    if (intersection.Traversal0.Side.Halfspace != intersection.Traversal1.Side.Halfspace)
                    {
                        if ((intersectionCount + 1) >= intersections.Length)
                        {
                            //float timeT = Time.realtimeSinceStartup;
                            var newIntersections = new PlaneTraversals[Mathf.Min(intersections.Length,8) * 2];
                            if (intersections.Length > 0)
                            Array.Copy(intersections, newIntersections, intersections.Length);
                            intersections = newIntersections;
                            //timeA += Time.realtimeSinceStartup - timeT;
                        }

                        intersections[intersectionCount] = intersection;
                        intersectionCount++;
                    } else 
                    if (intersectedEdges != null)
                    { 
                        if (intersection.Traversal0.Side.Halfspace == 0)
                        {/*
                            var vertex_index0	= controlMesh.edges[intersection.traversal0.edgeIndex].vertexIndex;
                            var vertex0			= controlMesh.points[vertex_index0];
                            var vertex1			= controlMesh.points[vertex_index1];
                            var direction		= (vertex1 - vertex0).normalized;
                            var cross			= Vector3.Cross(cuttingPlane.normal, shape.Surfaces[p].Plane.normal);
                            var dot				= Vector3.Dot(cross, direction);

                            if (dot < 0)
                            {
                                bool found_positive = false;
                                bool found_negative = false;
                                var iterator = controlMesh.GetNextEdgeIndexAroundVertex(edge_index1);
                                do
                                {
                                    var vertex_index = controlMesh.GetTwinEdgeVertexIndex(iterator);
                                    if (vertex_distances[vertex_index].halfspace > 0)
                                    {
                                        found_positive = true;
                                    }
                                    if (vertex_distances[vertex_index].halfspace < 0)
                                    {
                                        found_negative = true;
                                    }
                                    iterator = controlMesh.GetNextEdgeIndexAroundVertex(iterator);
                                } while (iterator != edge_index1);

                                if (found_positive && found_negative)
                                {
                                    //intersectedEdges.Add(edge_index1);
                                }
                            }*/
                            //float timeT = Time.realtimeSinceStartup;
                            intersectedEdges.Add(edgeIndex1);
                            //timeB += Time.realtimeSinceStartup - timeT;
                        }
                    }
                }

                if (intersectionCount == 1)
                {
                    Debug.LogWarning("Number of edges criss-crossing the plane boundary is 1 (should not be possible)!?");
                    return false;
                }

                if (intersectionCount > 0)
                {
//					var edge_index_count = polygon.edgeIndices.Length;
                    for (int i0 = intersectionCount - 1, i1 = 0; i1 < intersectionCount; i0 = i1, i1++)
                    {
                        if (intersections[i0].Traversal1.Side.Halfspace != intersections[i1].Traversal0.Side.Halfspace ||
                            intersections[i0].Traversal1.Side.Halfspace != 0)
                            continue;

                        // Note: we know traversal0.side.halfspace and traversal1.side.halfspace are always different from each other.

                        if (intersections[i0].Traversal0.Side.Halfspace != intersections[i1].Traversal1.Side.Halfspace)
                            continue;

                        // possibilities: (can have multiple vertices on the plane between intersections)
                        //
                        //       outside				      outside
                        //								       0      1
                        //       1  0					        \    /
                        //  .....*..*....... intersect	 ........*..*.... intersect
                        //      /    \					         1  0
                        //     0      1					
                        //        inside				      inside

                        //float timeT = Time.realtimeSinceStartup;
                        if (i0 > i1)
                        {
                            intersectionCount--; if ((intersectionCount - i0) > 0) Array.Copy(intersections, i0 + 1, intersections, i0, intersectionCount - i0);
                            intersectionCount--; if ((intersectionCount - i1) > 0) Array.Copy(intersections, i1 + 1, intersections, i1, intersectionCount - i1);
                        } else
                        {
                            intersectionCount--; if ((intersectionCount - i1) > 0) Array.Copy(intersections, i1 + 1, intersections, i1, intersectionCount - i1);
                            intersectionCount--; if ((intersectionCount - i0) > 0) Array.Copy(intersections, i0 + 1, intersections, i0, intersectionCount - i0);
                        }
                        //timeC += Time.realtimeSinceStartup - timeT;
                    }
                }

                // Find all traversals that go straight from one side to the other side. 
                //	Create a new intersection point there, split traversals into two traversals.
                if (intersectionCount > 0)
                {
//					var edge_index_count = polygon.edgeIndices.Length;
                    for (var i0 = 0; i0 < intersectionCount; i0++)
                    {
                        // Note: we know traversal0.side.halfspace and traversal1.side.halfspace are always different from each other.

                        //float timeT = Time.realtimeSinceStartup;
                        if (intersections[i0].Traversal0.Side.Halfspace == 0 ||
                            intersections[i0].Traversal1.Side.Halfspace == 0)
                            continue;

                        // possibilities:
                        //    
                        //       outside                      outside
                        //       0                                 1       
                        //        \                               /      
                        //  .......\......... intersect  ......../....... intersect
                        //          \                           /    
                        //           1                         0
                        //        inside                      inside

                        // Calculate intersection of edge with plane split the edge into two, inserting the new vertex
                        
                        var edgeIndex0		= intersections[i0].Traversal0.EdgeIndex;
                        var edgeIndex1		= intersections[i0].Traversal1.EdgeIndex;
                        var vertexIndex0	= controlMesh.Edges[edgeIndex0].VertexIndex;
                        var vertexIndex1	= controlMesh.Edges[edgeIndex1].VertexIndex;
                        var vertex0			= controlMesh.Vertices[vertexIndex0];
                        var vertex1			= controlMesh.Vertices[vertexIndex1];

                        Vector3 newVertex;
                        if (intersections[i0].Traversal0.Side.Halfspace < 0)
                        {
                            // possibilities:
                            //    
                            //       outside
                            //            1       
                            //           /      
                            //  ......../....... intersect
                            //         /    
                            //        0
                            //       inside

                            var vector	= vertex0 - vertex1;
                            var length	= intersections[i0].Traversal0.Side.Distance - intersections[i0].Traversal1.Side.Distance;
                            var delta	= intersections[i0].Traversal0.Side.Distance / length;
                            newVertex	= vertex0 - (vector * delta);
                        } else
                        {
                            // possibilities:
                            //    
                            //       outside
                            //            0
                            //           /      
                            //  ......../....... intersect
                            //         /    
                            //        1
                            //       inside
                            var vector	= vertex1 - vertex0;
                            var length	= intersections[i0].Traversal1.Side.Distance - intersections[i0].Traversal0.Side.Distance;
                            var delta	= intersections[i0].Traversal1.Side.Distance / length;
                            newVertex	= vertex1 - (vector * delta);
                        }


                        //short new_vertex_index = 
                        InsertControlpoint(controlMesh, newVertex, edgeIndex1);
                        var replacedEdgeIndex	= controlMesh.GetNextEdgeIndex(edgeIndex1);
                        var newEdgeIndex		= controlMesh.GetNextEdgeIndex(edgeIndex0);

                        for (var i1 = i0 + 1; i1 < intersectionCount; i1++)
                        {
                            if (intersections[i1].Traversal0.EdgeIndex == edgeIndex1)
                                intersections[i1].Traversal0.EdgeIndex = replacedEdgeIndex;
                            if (intersections[i1].Traversal1.EdgeIndex == edgeIndex1)
                                intersections[i1].Traversal1.EdgeIndex = replacedEdgeIndex;
                        }

                        var newIntersection = new PlaneTraversals
                        {
                            Traversal0 = intersections[i0].Traversal0,
                            Traversal1 =
                            {
                                EdgeIndex = newEdgeIndex,
                                Side =
                                {
                                    Distance = 0,
                                    Halfspace = 0
                                }
                            }
                        };
                        ArrayUtility.Add(ref vertexDistances, newIntersection.Traversal1.Side);

                        intersections[i0].Traversal0 = newIntersection.Traversal1;

                        ArrayUtility.Insert(ref intersections, i0, newIntersection);
                        i0++; intersectionCount++;
                        //timeD += Time.realtimeSinceStartup - timeT;
                    }
                }

                // NOTE: from this point on traversal.Index may no longer be valid!

                if (intersectionCount <= 1)
                    goto skip_to_next_polygon;


                // check if intersection_count is even
                if ((intersectionCount & 1) == 1)
                {
                    Debug.LogWarning("Found an uneven number of edge-plane traversals??");
                    return false;
                }
                
                if (intersectionCount != 4)
                {
                    return false;
                }

                if (intersections[0].Traversal0.Side.Halfspace == 0)
                {
                    intersections[intersectionCount + 1] = intersections[0];
                    Array.Copy(intersections, 1, intersections, 0, intersectionCount);
                }

                // TODO: find all traversal pairs and create edges between them
                int indexOut, indexIn;
                if (intersections[0].Traversal0.Side.Halfspace < 0)
                {
                    indexOut	= intersections[1].Traversal0.EdgeIndex;
                    indexIn	= intersections[2].Traversal1.EdgeIndex;
                } else
                {
                    indexOut	= intersections[2].Traversal1.EdgeIndex;
                    indexIn	= intersections[1].Traversal0.EdgeIndex;
                }
                
                var newEdge = InsertEdgeBetweenEdges(controlMesh, shape, indexOut, indexIn);
                //timeE += Time.realtimeSinceStartup - timeT2;
                if (newEdge == -1)
                    return false;

                if (intersectedEdges != null)
                {
                    intersectedEdges.Add(newEdge);
                    intersectedEdges.Add(controlMesh.GetTwinEdgeIndex(newEdge));
                }

                skip_to_next_polygon:
                ;
            }
            return true;
        }

        private static bool IsPolygonInsideHalfSpace(CSGPlane cuttingPlane, ControlMesh controlMesh, int polygonIndex)
        {
            var edges		= controlMesh.Edges;
            var vertices	= controlMesh.Vertices;
            var edgeIndices = controlMesh.Polygons[polygonIndex].EdgeIndices;
            var onPlane = true;
            for (var e = 0; e < edgeIndices.Length; e++)
            {
                var edgeIndex = edgeIndices[e];
                var vertexIndex = edges[edgeIndex].VertexIndex;
                var vertex = vertices[vertexIndex];
                if (cuttingPlane.Distance(vertex) >= MathConstants.DistanceEpsilon)
                    return false;
                if (cuttingPlane.Distance(vertex) < -MathConstants.DistanceEpsilon)
                    onPlane = false;
            }

            if (!onPlane)
                return true;

            for (var e0 = 0; e0 < edgeIndices.Length; e0++)
            {
                var edgeIndex			= edgeIndices[e0];
                var twinIndex			= edges[edgeIndex].TwinIndex;
                var twinPolygonIndex	= edges[twinIndex].PolygonIndex;
                var twinEdgeIndices		= controlMesh.Polygons[twinPolygonIndex].EdgeIndices;
                for (var e1 = 0; e1 < twinEdgeIndices.Length; e1++)
                {
                    var	vertexIndex			= edges[twinEdgeIndices[e1]].VertexIndex;
                    var vertex				= vertices[vertexIndex];
                    if (cuttingPlane.Distance(vertex) >= MathConstants.DistanceEpsilon)
                        return false;
                    if (cuttingPlane.Distance(vertex) < -MathConstants.DistanceEpsilon)
                        break;
                }
            }
            return true;
        }

        // find piece of this control-mesh that is completely separate, 
        //	remove it and return it as a new control-mesh.
        public static bool FindAndDetachSeparatePiece(ControlMesh controlMesh, Shape shape, CSGPlane cuttingPlane, out ControlMesh newControlMesh, out Shape newShape)
        {
            var foundPolygons	= new HashSet<short>();
            var checkEdges		= new List<int>();
            var checkedEdges	= new HashSet<int>();

            var startPolygon = (short)-1;
            for (var i = controlMesh.Polygons.Length - 1; i >= 0; i--)
            {
                if (!IsPolygonInsideHalfSpace(cuttingPlane, controlMesh, i))
                    continue;
                
                startPolygon = (short)i;
                break;
            }

            if (startPolygon == -1)
            {
                newControlMesh = null;
                newShape = null;
                return false;
            }
            foundPolygons.Add(startPolygon);

            var polygonEdges = controlMesh.Polygons[startPolygon].EdgeIndices;
            for (var i = 0; i < polygonEdges.Length; i++)
            {
                var edgeIndex = polygonEdges[i];
                checkedEdges.Add(edgeIndex);
                var edgeTwinIndex = controlMesh.GetTwinEdgeIndex(edgeIndex);
                checkEdges.Add(edgeTwinIndex);
            }

            while (checkEdges.Count > 0)
            {
                var checkEdge = checkEdges[checkEdges.Count - 1];
                checkEdges.RemoveAt(checkEdges.Count - 1);
                checkedEdges.Add(checkEdge);

                var checkEdgePolygon = controlMesh.GetEdgePolygonIndex(checkEdge);
                if (foundPolygons.Contains(checkEdgePolygon))
                    continue;

                foundPolygons.Add(checkEdgePolygon);
                if (foundPolygons.Count == controlMesh.Polygons.Length)
                {
                    newControlMesh = null;
                    newShape = null;
                    return false;
                }

                polygonEdges = controlMesh.Polygons[checkEdgePolygon].EdgeIndices;
                for (var i = 0; i < polygonEdges.Length; i++)
                {
                    var edgeIndex = polygonEdges[i];
                    var edgeTwinIndex = controlMesh.GetTwinEdgeIndex(edgeIndex);
                    if (!checkedEdges.Contains(edgeTwinIndex))
                    {
                        checkEdges.Add(edgeTwinIndex);
                    }
                }
            }

            var lookupVertexIndicesA	= new short[controlMesh.Vertices.Length];
            var lookupEdgeIndicesA		= new int  [controlMesh.Edges.Length];
            var lookupPolygonIndicesA	= new short[controlMesh.Polygons.Length];

            var lookupVertexIndicesB	= new short[controlMesh.Vertices.Length];
            var lookupEdgeIndicesB		= new int  [controlMesh.Edges.Length];
            var lookupPolygonIndicesB	= new short[controlMesh.Polygons.Length];

            foreach(var polygonIndex in foundPolygons)
            {
                lookupPolygonIndicesA[polygonIndex] = 1;
                polygonEdges = controlMesh.Polygons[polygonIndex].EdgeIndices;
                for (var i = 0; i < polygonEdges.Length; i++)
                {
                    var edgeIndex = polygonEdges[i];
                    lookupEdgeIndicesA[edgeIndex] = 1;
                    var vertexIndex = controlMesh.GetVertexIndex(edgeIndex);
                    lookupVertexIndicesA[vertexIndex] = 1;
                }
            }

            var vertexLengthA = (short)0;
            var vertexLengthB = (short)0;
            var newVerticesA = new List<Vector3>(controlMesh.Vertices.Length);
            var newVerticesB = new List<Vector3>(controlMesh.Vertices.Length);
            for (var i = 0; i < lookupVertexIndicesA.Length; i++)
            {
                if (lookupVertexIndicesA[i] == 0)
                {
                    lookupVertexIndicesA[i] = -1;
                    lookupVertexIndicesB[i] = vertexLengthB;
                    vertexLengthB++;
                    newVerticesB.Add(controlMesh.Vertices[i]);
                } else
                {
                    lookupVertexIndicesB[i] = -1;
                    lookupVertexIndicesA[i] = vertexLengthA;
                    vertexLengthA++;
                    newVerticesA.Add(controlMesh.Vertices[i]);
                }
            }
            var newVertexArrayA = newVerticesA.ToArray();
            var newVertexArrayB = newVerticesB.ToArray();

            var polygonLengthA = (short)0;
            var polygonLengthB = (short)0;
            for (var i = 0; i < lookupPolygonIndicesA.Length; i++)
            {
                if (lookupPolygonIndicesA[i] == 0)
                {
                    lookupPolygonIndicesA[i] = -1;
                    lookupPolygonIndicesB[i] = polygonLengthB;
                    polygonLengthB++;
                } else
                {
                    lookupPolygonIndicesB[i] = -1;
                    lookupPolygonIndicesA[i] = polygonLengthA;
                    polygonLengthA++;
                }
            }

            var edgeLengthA = 0;
            var edgeLengthB = 0;
            for (var i = 0; i < lookupEdgeIndicesA.Length; i++)
            {
                if (lookupEdgeIndicesA[i] == 0)
                {
                    lookupEdgeIndicesA[i] = -1;
                    lookupEdgeIndicesB[i] = edgeLengthB;
                    edgeLengthB++;
                } else
                {
                    lookupEdgeIndicesB[i] = -1;
                    lookupEdgeIndicesA[i] = edgeLengthA;
                    edgeLengthA++;
                }
            }

            var newEdgeArrayA = new HalfEdge[edgeLengthA];
            var newEdgeArrayB = new HalfEdge[edgeLengthB];
            {
                var counterA = 0;
                var counterB = 0;
                for (var i = 0; i < lookupEdgeIndicesA.Length; i++)
                {
                    var originalEdge = controlMesh.Edges[i];
                    if (lookupEdgeIndicesA[i] == -1)
                    {
                        newEdgeArrayB[counterB] =
                            new HalfEdge(
                                    lookupPolygonIndicesB[originalEdge.PolygonIndex],
                                    lookupEdgeIndicesB   [originalEdge.TwinIndex],
                                    lookupVertexIndicesB [originalEdge.VertexIndex],
                                    originalEdge.HardEdge
                                );
                        counterB++;
                    } else
                    { 				
                        newEdgeArrayA[counterA] =
                            new HalfEdge(
                                    lookupPolygonIndicesA[originalEdge.PolygonIndex],
                                    lookupEdgeIndicesA   [originalEdge.TwinIndex],
                                    lookupVertexIndicesA [originalEdge.VertexIndex],
                                    originalEdge.HardEdge
                                );
                        counterA++;
                    }
                }
            }

            var newTexgenListA		= new List<TexGen>(polygonLengthA);
            var newTexgenListB		= new List<TexGen>(polygonLengthB);
            //var newMaterialListA	= new List<Material>(polygonLengthA);
            //var newMaterialListB	= new List<Material>(polygonLengthB);
            var newFlagsListA		= new List<TexGenFlags>(polygonLengthA);
            var newFlagsListB		= new List<TexGenFlags>(polygonLengthB);
            var newTexgenLookupA	= new int[shape.TexGens.Length];
            var newTexgenLookupB	= new int[shape.TexGens.Length];
            var newPolygonArrayA	= new Polygon[polygonLengthA];
            var newPolygonArrayB	= new Polygon[polygonLengthB];
            var newPlaneArrayA		= new Surface[polygonLengthA];
            var newPlaneArrayB		= new Surface[polygonLengthB];
            {
                var counterA = 0;
                var counterB = 0;
                for (var i = 0; i < lookupPolygonIndicesA.Length; i++)
                {
                    var originalEdges		= controlMesh.Polygons[i].EdgeIndices;
                    var newPolygonEdges		= new int[originalEdges.Length];
                    if (lookupPolygonIndicesA[i] == -1)
                    {
                        for (var e = 0; e < newPolygonEdges.Length; e++)
                        {
                            newPolygonEdges[e] = lookupEdgeIndicesB[originalEdges[e]];
                        }

                        var texgenIndex = controlMesh.Polygons[i].TexGenIndex;
                        if (texgenIndex < 0 ||
                            texgenIndex >= newTexgenLookupB.Length)
                        {
                            Debug.LogWarning("texgen_index < 0 || texgen_index >= new_texgen_lookupB.Length");
                        }
                        if (newTexgenLookupB[texgenIndex] == 0)
                        {
                            newTexgenListB  .Add(shape.TexGens[texgenIndex]);
                            newFlagsListB   .Add(shape.TexGenFlags[texgenIndex]);
                            newTexgenLookupB[texgenIndex] = newTexgenListB.Count;
                            texgenIndex = newTexgenListB.Count - 1;
                        } else
                        {
                            texgenIndex = newTexgenLookupB[texgenIndex] - 1;
                        }

                        newPolygonArrayB[counterB] = new Polygon(newPolygonEdges, texgenIndex);
                        newPlaneArrayB  [counterB] = shape.Surfaces[i];
                        counterB++;
                    } else
                    { 
                        for (var e = 0; e < newPolygonEdges.Length; e++)
                        {
                            newPolygonEdges[e] = lookupEdgeIndicesA[originalEdges[e]];
                        }

                        var texgenIndex = controlMesh.Polygons[i].TexGenIndex;
                        if (texgenIndex < 0 ||
                            texgenIndex >= newTexgenLookupB.Length)
                        {
                            Debug.LogWarning("texgen_index < 0 || texgen_index >= new_texgen_lookupB.Length");
                        }
                        if (newTexgenLookupA[texgenIndex] == 0)
                        {
                            newTexgenListA  .Add(shape.TexGens[texgenIndex]);
                            newFlagsListA   .Add(shape.TexGenFlags[texgenIndex]);
                            newTexgenLookupA[texgenIndex] = newTexgenListA.Count;
                            texgenIndex = newTexgenListA.Count - 1;
                        } else
                        {
                            texgenIndex = newTexgenLookupA[texgenIndex] - 1;
                        }

                        newPolygonArrayA[counterA] = new Polygon(newPolygonEdges, texgenIndex);
                        newPlaneArrayA[counterA] = shape.Surfaces[i];
                        counterA++;
                    }
                }
            }

            newControlMesh = new ControlMesh
            {
                Vertices	= newVertexArrayA,
                Polygons	= newPolygonArrayA,
                Edges		= newEdgeArrayA
            };
            newShape = new Shape
            {
                Surfaces	= newPlaneArrayA,
                TexGens		= newTexgenListA.ToArray(),
                TexGenFlags = newFlagsListA.ToArray()
            };
            Validate(newControlMesh, newShape);
            
            controlMesh.Vertices	= newVertexArrayB;
            controlMesh.Polygons	= newPolygonArrayB;
            controlMesh.Edges		= newEdgeArrayB;
            shape.Surfaces			= newPlaneArrayB;
            shape.TexGens			= newTexgenListB  .ToArray();
            shape.TexGenFlags		= newFlagsListB   .ToArray();
            
            return true;
        }

        private static int SafeEdge(ControlMesh controlMesh, int[] polygonEdges, short vertexIndex)
        {
            for (var e = 0; e < controlMesh.Edges.Length; e++)
            {
                var newEdgeIndex = polygonEdges[e];
                if (vertexIndex == controlMesh.Edges[newEdgeIndex].VertexIndex)
                    return newEdgeIndex;
            }
            return -1;//edgeIndex
        }

        private static void ClipOffTriangle(ControlMesh controlMesh, Shape shape, short[] vertexIndices, short polygonIndex)
        {
            var edgeIndices = controlMesh.Polygons[polygonIndex].EdgeIndices;

            if (edgeIndices.Length == 3)
                return;

            var edges = new [] {
                SafeEdge(controlMesh, edgeIndices, vertexIndices[0]),
                SafeEdge(controlMesh, edgeIndices, vertexIndices[1]),
                SafeEdge(controlMesh, edgeIndices, vertexIndices[2]) };

            if (edges[0] == -1 || edges[1] == -1 || edges[2] == -1)
            {
                Debug.LogWarning("REVERT REVERT");
                return;
            }

            var indices = new [] {
                ArrayUtility.IndexOf(edgeIndices, edges[0]),
                ArrayUtility.IndexOf(edgeIndices, edges[1]),
                ArrayUtility.IndexOf(edgeIndices, edges[2]) };

            for (var i = 0; i < indices.Length - 1; i++)
            {
                for (var j = i + 1; j < indices.Length; j++)
                {
                    if (indices[i] <= indices[j])
                        continue;

                    var t = edges[i];
                    edges[i] = edges[j];
                    edges[j] = t;
                    t = indices[i];
                    indices[i] = indices[j];
                    indices[j] = t;
                }
            }

            if (indices[0] + 1 != indices[1])
            {
                var t0 = edges[0];
                var t1 = edges[1];
                var t2 = edges[2];

                edges[0] = t1;
                edges[1] = t2;
                edges[2] = t0;
                //edges.Add(edges[0]);
                //edges.RemoveAt(0);
            } else
            if (indices[1] + 1 != indices[2])
            {
                var t0 = edges[0];
                var t1 = edges[1];
                var t2 = edges[2];

                edges[0] = t2;
                edges[1] = t0;
                edges[2] = t1;
                //edges.Insert(0, edges[2]);
                //edges.RemoveAt(3);
            }

            var prevEdgeIndex = edges[0];
            var currEdgeIndex = edges[1];
            var nextEdgeIndex = edges[2];

                        
            if (controlMesh.GetPrevEdgeIndex(currEdgeIndex) != prevEdgeIndex||
                controlMesh.GetNextEdgeIndex(currEdgeIndex) != nextEdgeIndex)
            {
                Debug.LogWarning("invalid order of vertices");
                return;
            }
            

//			var currVertexIndex		= controlMesh.GetVertexIndex(currEdgeIndex);
            var prevVertexIndex		= controlMesh.GetVertexIndex(prevEdgeIndex);
            var nextVertexIndex		= controlMesh.GetVertexIndex(nextEdgeIndex);

            var nextTwinIndex		= controlMesh.GetTwinEdgeIndex(nextEdgeIndex);

            var newIndex0			= controlMesh.Edges.Length;
            var newIndex1			= controlMesh.Edges.Length + 1;
            var newPolygonIndex		= (short)controlMesh.Polygons.Length;

            var newPolygonEdges = new [] { currEdgeIndex, newIndex0, newIndex1 };
            var newPolygon		= new Polygon(newPolygonEdges, CopyTexGen(shape, controlMesh.Polygons[polygonIndex].TexGenIndex));
            
            ArrayUtility.Remove(ref controlMesh.Polygons[polygonIndex].EdgeIndices, currEdgeIndex);

            var oldHardEdge = controlMesh.Edges[nextEdgeIndex].HardEdge;

            ArrayUtility.Add(ref controlMesh.Edges, new HalfEdge(newPolygonIndex, nextTwinIndex, nextVertexIndex, oldHardEdge)); // new index 0 
            ArrayUtility.Add(ref controlMesh.Edges, new HalfEdge(newPolygonIndex, nextEdgeIndex, prevVertexIndex, false)); // new index 1
            
            controlMesh.Edges[nextEdgeIndex].TwinIndex	= newIndex1;
            controlMesh.Edges[nextEdgeIndex].HardEdge		= false;
            controlMesh.Edges[nextTwinIndex].TwinIndex	= newIndex0;
            controlMesh.Edges[currEdgeIndex].PolygonIndex	= newPolygonIndex;

            AddNewPolygon(controlMesh, shape, newPolygon);
            if (controlMesh.Polygons[polygonIndex].EdgeIndices.Length == 3)
            {
                shape.Surfaces[polygonIndex].Plane = GeometryUtility.CalcPolygonPlane(controlMesh, polygonIndex);
            }
            
            //Validate(controlMesh, shape);
        }

        private sealed class Triangle
        {
            public short[] VertexIndices;
            public CSGPlane Plane;
        }

        private sealed class DataCache
        {
            public void Clear()
            {
                HardEdges		.Clear();
                TriangleIndices	.Clear();
                AllTriangles	.Clear();
                Path = new TriangulationPath();
            }
            public readonly Dictionary<UInt32, short>	HardEdges		= new Dictionary<UInt32, short>();
            public readonly Dictionary<UInt64, short>	TriangleIndices	= new Dictionary<UInt64, short>();
            public readonly List<Triangle>				AllTriangles	= new List<Triangle>();
            public TriangulationPath			Path;
        }

        private sealed class TriangulationPath
        {
            public TriangulationPath[] SubPaths;
            public short	VertexIndex;
            public short[]	Triangles;
            public float	Heuristic;
        }

        private static Triangle FindTriangleWithEdge(short[] triangles, DataCache cache, short vertexIndex0, short vertexIndex1)
        {
            if (triangles == null)
                return null;

            for (var i = 0; i < triangles.Length; i++)
            {
                var triangle			= cache.AllTriangles[triangles[i]];
                var vertexIndices		= triangle.VertexIndices;
                var edge0VertexIndex	= vertexIndices[0];
                var edge1VertexIndex	= vertexIndices[1];
                var edge2VertexIndex	= vertexIndices[2];

                if ((edge0VertexIndex != vertexIndex0 || edge1VertexIndex != vertexIndex1) &&
                    (edge0VertexIndex != vertexIndex0 || edge2VertexIndex != vertexIndex1) &&
                    (edge1VertexIndex != vertexIndex0 || edge0VertexIndex != vertexIndex1) &&
                    (edge1VertexIndex != vertexIndex0 || edge2VertexIndex != vertexIndex1) &&
                    (edge2VertexIndex != vertexIndex0 || edge0VertexIndex != vertexIndex1) &&
                    (edge2VertexIndex != vertexIndex0 || edge1VertexIndex != vertexIndex1))
                    continue;

                return triangle;
            }
            return null;
        }

        private static class VertPair
        {
            public static uint Create(short a, short b)
            {
                var a32 = (uint)a;
                var b32 = (uint)b;
                return (a < b) ? 
                        (a32 | (b32 << 16)) :
                        (b32 | (a32 << 16));
            }
        }

        private static class TriangleIndex
        {
            public static ulong Create(short a, short b, short c)
            {
                var a32 = (ulong)a;
                var b32 = (ulong)b;
                var c32 = (ulong)c;
                if (a < b)
                {
                    if (c > b) return a32 | (b32 << 16) | (c32 << 32);
                    if (c > a) return a32 | (c32 << 16) | (b32 << 32);
                               return c32 | (a32 << 16) | (b32 << 32);
                } else
                {
                    if (c > a) return b32 | (a32 << 16) | (c32 << 32);
                    if (c > b) return b32 | (c32 << 16) | (a32 << 32);
                               return c32 | (b32 << 16) | (a32 << 32);
                }
            }
        }

        // TODO: make this non-recursive
        private static float SplitAtEdge(ControlMesh controlMesh, Shape shape, out short[] triangles, DataCache cache, short[] vertexIndices, int vertexIndicesLength)
        {
            if (vertexIndicesLength == 3)
            {
                var vi0 = vertexIndices[0];
                var vi1 = vertexIndices[1];
                var vi2 = vertexIndices[2];
                var triangleIndex = TriangleIndex.Create(vi0, vi1, vi2);
                short index;
                Triangle triangle;
                if (!cache.TriangleIndices.TryGetValue(triangleIndex, out index))
                {
                    triangle = new Triangle { VertexIndices = new[] {vi0, vi1, vi2} };

                    if (vi0 < 0 || vi0 >= controlMesh.Vertices.Length ||
                        vi1 < 0 || vi1 >= controlMesh.Vertices.Length ||
                        vi2 < 0 || vi2 >= controlMesh.Vertices.Length)
                    {
                        triangles = null;
                        return float.PositiveInfinity;
                    }
                     
                    var p0 = controlMesh.Vertices[vi0];
                    var p1 = controlMesh.Vertices[vi1];
                    var p2 = controlMesh.Vertices[vi2];

                    triangle.Plane = new CSGPlane(p0, p1, p2);

                    index = (short)cache.AllTriangles.Count;
                    cache.AllTriangles.Add(triangle);
                    cache.TriangleIndices[triangleIndex] = index;
                } else
                {
                    triangle = cache.AllTriangles[index];
                }
                triangles = new [] { index };

                var pair0 = VertPair.Create(vi0, vi1);
                var pair1 = VertPair.Create(vi1, vi2);
                var pair2 = VertPair.Create(vi2, vi0);
                
                var triangleHeuristic = 0.0f;

                short polygonIndex;
                var hardEdges			= cache.HardEdges;
                var trianglePlaneNormal	= triangle.Plane.normal;
                if (hardEdges.TryGetValue(pair0, out polygonIndex))
                {
                    if (polygonIndex < 0 || polygonIndex >= shape.Surfaces.Length)
                        return float.PositiveInfinity;
                    var normal = shape.Surfaces[polygonIndex].Plane.normal;
                    var error = Vector3.Dot(normal, trianglePlaneNormal) - 1;
                    triangleHeuristic += (error * error);
                }
                if (hardEdges.TryGetValue(pair1, out polygonIndex))
                {
                    if (polygonIndex < 0 || polygonIndex >= shape.Surfaces.Length)
                        return float.PositiveInfinity;
                    var normal = shape.Surfaces[polygonIndex].Plane.normal;
                    var error = Vector3.Dot(normal, trianglePlaneNormal) - 1;
                    triangleHeuristic += (error * error);
                }
                if (hardEdges.TryGetValue(pair2, out polygonIndex))
                {
                    if (polygonIndex < 0 || polygonIndex >= shape.Surfaces.Length)
                        return float.PositiveInfinity;
                    var normal = shape.Surfaces[polygonIndex].Plane.normal;
                    var error = Vector3.Dot(normal, trianglePlaneNormal) - 1;
                    triangleHeuristic += (error * error);
                } 
                
                return triangleHeuristic;
            }

            TriangulationPath curLeftPath = null;
            TriangulationPath curRightPath = null;
            var curHeuristic	= float.PositiveInfinity;
            short[]	tempEdges = null;
            for (var startPoint = 0; startPoint < vertexIndicesLength - 2; startPoint++)
            {
                for (var offset = 2; offset < vertexIndicesLength - 1; offset++)
                {
                    var endPoint = (startPoint + offset) % vertexIndicesLength;
                    int t0, t1;
                    if (endPoint < startPoint) { t0 = endPoint;   t1 = startPoint; }
                    else						 { t0 = startPoint; t1 = endPoint; }
                    var vertexIndex0	= vertexIndices[t0];
                    var vertexIndex1	= vertexIndices[t1];

                    var leftPath = cache.Path;
                    var startIndex = -1;
                    // try to find the triangulation in the cache
                    for (var i = t0; i <= t1; i++)
                    {
                        if (leftPath.SubPaths == null) { startIndex = i; break; }
                        var	index = vertexIndices[i];
                        var found = false;
                        for (var j = 0; j < leftPath.SubPaths.Length; j++)
                        {
                            if (leftPath.SubPaths[j].VertexIndex != index)
                                continue;

                            found = true;
                            leftPath = leftPath.SubPaths[j];
                            break;
                        }
                        if (found)
                            continue;

                        startIndex = i;
                        break;
                    }
                    
                    float leftHeuristic;
                    short[]	leftTriangles;
                    if (startIndex != -1 || 
                        leftPath.Triangles == null)
                    {
                        var length0 = (t1 - t0) + 1;
                        if (tempEdges == null ||
                            tempEdges.Length < length0)
                            tempEdges = new short[length0];

                        Array.Copy(vertexIndices, t0, tempEdges, 0, (t1 - t0) + 1);

                        // triangulate for the given vertices
                        leftHeuristic = SplitAtEdge(controlMesh, shape, out leftTriangles, cache, tempEdges, length0);

                        // store the found triangulation in the cache
                        if (startIndex != -1)
                        {
                            for (var i = startIndex; i <= t1; i++)
                            {
                                var newSubPath = new TriangulationPath {VertexIndex = vertexIndices[i]};
                                if (leftPath.SubPaths == null) { leftPath.SubPaths = new[] { newSubPath }; }
                                else { ArrayUtility.Add(ref leftPath.SubPaths, newSubPath); }
                                leftPath = newSubPath;
                            }
                        }

                        leftPath.Triangles	= leftTriangles;
                        leftPath.Heuristic	= leftHeuristic;
                    } else
                    {
                        leftHeuristic		= leftPath.Heuristic;
                        leftTriangles		= leftPath.Triangles;
                    }

                    var newHeuristic = leftHeuristic;
                    if (newHeuristic >= curHeuristic + MathConstants.EqualityEpsilon)
                        continue;
                    
                    
                    var offsetB = (vertexIndicesLength - t1);
                    var length1 = (t0 + 1) + offsetB;
                    if (tempEdges == null ||
                        tempEdges.Length < length1)
                        tempEdges = new short[length1];

                    Array.Copy(vertexIndices, t1, tempEdges, 0, offsetB);
                    Array.Copy(vertexIndices, 0, tempEdges, offsetB, (t0 + 1));
                    
                    var	rightPath = cache.Path;
                    startIndex = -1;
                    // try to find the triangulation in the cache
                    for (int i = 0; i < length1; i++)
                    {
                        if (rightPath.SubPaths == null) { startIndex = i; break; }
                        var		index = tempEdges[i];
                        bool	found = false;
                        for (int j = 0; j < rightPath.SubPaths.Length; j++)
                        {
                            if (rightPath.SubPaths[j].VertexIndex == index) { found = true; rightPath = rightPath.SubPaths[j]; break; }
                        }
                        if (!found) { startIndex = i; break; }
                    }
                    
                    float rightHeuristic;
                    short[] rightTriangles;
                    if (startIndex != -1 || 
                        rightPath.Triangles == null)
                    {
                        // triangulate for the given vertices
                        rightHeuristic = SplitAtEdge(controlMesh, shape, out rightTriangles, cache, tempEdges, length1);
                        
                        // store the found triangulation in the cache
                        if (startIndex != -1)
                        { 
                            for (var i = startIndex; i < tempEdges.Length; i++)
                            {
                                var newSubPath = new TriangulationPath { VertexIndex = tempEdges[i] };
                                if (rightPath.SubPaths == null)	{ rightPath.SubPaths = new[] { newSubPath }; }
                                else							{ ArrayUtility.Add(ref rightPath.SubPaths, newSubPath); }
                                rightPath = newSubPath;
                            }
                        }

                        rightPath.Triangles	= rightTriangles;
                        rightPath.Heuristic	= rightHeuristic;
                    } else
                    {
                        rightHeuristic		= rightPath.Heuristic;
                        rightTriangles		= rightPath.Triangles;
                    }

                    newHeuristic += rightHeuristic;
                    if (newHeuristic >= curHeuristic + MathConstants.EqualityEpsilon)
                        continue;
                    
                    var leftTriangle	= FindTriangleWithEdge(leftTriangles,  cache, vertexIndex0, vertexIndex1);
                    var rightTriangle	= FindTriangleWithEdge(rightTriangles, cache, vertexIndex0, vertexIndex1);

                    if (leftTriangle == null ||
                        rightTriangle == null)
                        continue;

                    var leftPlane	= leftTriangle .Plane;
                    var rightPlane	= rightTriangle.Plane;
                    var error		= Vector3.Dot(leftPlane.normal, rightPlane.normal) - 1;
                    newHeuristic += (error * error);

                    if (!(newHeuristic < curHeuristic - MathConstants.EqualityEpsilon))
                        continue;

                    curLeftPath	= leftPath;
                    curRightPath	= rightPath;
                    curHeuristic	= newHeuristic;
                }
            }
            if (curLeftPath != null &&
                curRightPath != null)
            {
                triangles = new short[curLeftPath.Triangles.Length + curRightPath.Triangles.Length];
                Array.Copy(curLeftPath.Triangles,     triangles, curLeftPath.Triangles.Length);
                Array.Copy(curRightPath.Triangles, 0, triangles, curLeftPath.Triangles.Length, curRightPath.Triangles.Length);
            } else
            {
                curHeuristic = float.PositiveInfinity;
                triangles = null;
            }
            return curHeuristic;
        }
        
        static readonly List<CSGPlane>				__polygonPlanes			= new List<CSGPlane>();
        static readonly Dictionary<int, Triangle[]> __polyTriangles			= new Dictionary<int, Triangle[]>();
        static readonly List<Triangle>				__newTriangles			= new List<Triangle>();
        static readonly DataCache                   __dataCache				= new DataCache();		
        static short[]								__polyVerts				= new short[0];
        static readonly HashSet<short>				__ownedPolygons			= new HashSet<short>();
        static readonly HashSet<int>				__removeEdges			= new HashSet<int>();
        static readonly HashSet<short>				__removePolygons		= new HashSet<short>();
            
        public static bool Triangulate(CSGBrush brush, ControlMesh controlMesh, Shape shape)
        {
            if (controlMesh == null ||
                controlMesh.Polygons == null)
                return false;

            //float time0 = Time.realtimeSinceStartup;

            __polygonPlanes.Clear();
            for (var p = 0; p < controlMesh.Polygons.Length; p++)
            {
                var plane = GeometryUtility.CalcPolygonPlane(controlMesh, (short)p);
                __polygonPlanes.Add(plane);
                shape.Surfaces[p].Plane = plane;
            }
            //float time1 = Time.realtimeSinceStartup;

            __polyTriangles.Clear();
            __newTriangles.Clear();
            __dataCache.Clear();
            for (var p = controlMesh.Polygons.Length - 1; p >= 0; p--)
            {
                var edgeIndices	= controlMesh.Polygons[p].EdgeIndices;

                if (edgeIndices.Length > 16)
                    continue;

                /*
                if (edge_indices.Length <= 3)
                {
                    return true;
                }*/

                var polygonPlane = __polygonPlanes[p];
                var isPlanar = true;
                for (var e = 0; e< edgeIndices.Length; e++)
                {
                    var vertex = controlMesh.GetVertex(edgeIndices[e]);
                    if (!(polygonPlane.Distance(vertex) > MathConstants.DistanceEpsilon))
                        continue;
                    
                    isPlanar = false;
                    break;
                }
                
                if (isPlanar)
                    continue;

                // TODO: only update the 'polygons' that have been modified

                __dataCache.Clear();
                for (var e = 0; e < edgeIndices.Length; e++)
                {
                    var curEdge			= controlMesh.Edges[edgeIndices[e]];
                    var twinEdge		= controlMesh.Edges[curEdge.TwinIndex];
                    var polygonIndex	= twinEdge.PolygonIndex;
                    var pair			= VertPair.Create(curEdge.VertexIndex, twinEdge.VertexIndex);
                    __dataCache.HardEdges[pair] = polygonIndex;
                }

                if (__polyVerts.Length < edgeIndices.Length)
                    __polyVerts = new short[edgeIndices.Length];

                for (var i = 0; i < edgeIndices.Length; i++)
                    __polyVerts[i] = controlMesh.Edges[edgeIndices[i]].VertexIndex;

                short[] foundTriangles;
                SplitAtEdge(controlMesh, shape, out foundTriangles, __dataCache, __polyVerts, edgeIndices.Length);

                __newTriangles.Clear();
                while (foundTriangles != null && foundTriangles.Length > 0)
                {
                    if (foundTriangles.Length == 1)
                    {
                        __newTriangles.Add(__dataCache.AllTriangles[foundTriangles[0]]);
                        break;
                    }

                    var found = -1;
                    var foundEdgeCount = 0;
                    for (var t = 0; t < foundTriangles.Length; t++)
                    {
                        var triangle = __dataCache.AllTriangles[foundTriangles[t]];
                        var vertPair1 = VertPair.Create(triangle.VertexIndices[0], triangle.VertexIndices[1]);
                        var vertPair2 = VertPair.Create(triangle.VertexIndices[1], triangle.VertexIndices[2]);
                        var vertPair3 = VertPair.Create(triangle.VertexIndices[2], triangle.VertexIndices[0]);

                        var hardEdgeCount = 0;
                        if (__dataCache.HardEdges.ContainsKey(vertPair1)) hardEdgeCount++;
                        if (__dataCache.HardEdges.ContainsKey(vertPair2)) hardEdgeCount++;
                        if (__dataCache.HardEdges.ContainsKey(vertPair3)) hardEdgeCount++;

                        if (hardEdgeCount <= foundEdgeCount)
                            continue;

                        foundEdgeCount = hardEdgeCount;
                        found = t;
                    }
                    if (found == -1)
                    {
                        Debug.LogWarning("Failed to find appropriate triangle to clip during triangulation");
                        break;
                    }

                    {
                        var triangle = __dataCache.AllTriangles[foundTriangles[found]];
                        var vertPair1 = VertPair.Create(triangle.VertexIndices[0], triangle.VertexIndices[1]);
                        var vertPair2 = VertPair.Create(triangle.VertexIndices[1], triangle.VertexIndices[2]);
                        var vertPair3 = VertPair.Create(triangle.VertexIndices[2], triangle.VertexIndices[0]);

                        __newTriangles.Add(triangle);
                        ArrayUtility.RemoveAt(ref foundTriangles, found);
                        //found_triangles.RemoveAt(found);
                        if (!__dataCache.HardEdges.ContainsKey(vertPair1)) __dataCache.HardEdges.Add(vertPair1, -1);
                        if (!__dataCache.HardEdges.ContainsKey(vertPair2)) __dataCache.HardEdges.Add(vertPair2, -1);
                        if (!__dataCache.HardEdges.ContainsKey(vertPair3)) __dataCache.HardEdges.Add(vertPair3, -1);
                    }
                }

                __polyTriangles.Add(p, __newTriangles.ToArray());
            }
            //float time2 = Time.realtimeSinceStartup;

            //var original_polygon_length = controlMesh.polygons.Length;
            __ownedPolygons.Clear();
            foreach(var pair in __polyTriangles)
            {
                var polygonIndex	= (short)pair.Key;
                var triangles		= pair.Value;
                if (triangles == null)
                    continue;

                __ownedPolygons.Clear();
                __ownedPolygons.Add(polygonIndex);
                var currentLastIndex = controlMesh.Polygons.Length;

                for (var t = 0; t < triangles.Length; t++)
                {
                    ClipOffTriangle(controlMesh, shape, triangles[t].VertexIndices, polygonIndex);
                }

                for (var i = currentLastIndex; i < controlMesh.Polygons.Length; i++)
                {
                    __polygonPlanes.Add(GeometryUtility.CalcPolygonPlane(controlMesh, (short)i));
                    __ownedPolygons.Add((short)i);
                }
            }
            //float time3 = Time.realtimeSinceStartup;

            var polygonPlaneArray = __polygonPlanes.ToArray();
            for (var i = 0; i < polygonPlaneArray.Length; i++)
            {
                polygonPlaneArray[i] = GeometryUtility.CalcPolygonPlane(controlMesh, (short)i);
            }
            //float time4 = Time.realtimeSinceStartup;
            /*
            var unique_planes = new List<CSGPlane>(polygon_plane_array.Length);
            var plane_indices = new int[polygon_plane_array.Length];
            for (int i=0;i< polygon_plane_array.Length; i++)
            {
                if (plane_indices[i] != 0)
                    continue;

                var polygon_plane = polygon_plane_array[i];
                var current_index = unique_planes.Count + 1;
                unique_planes.Add(polygon_plane);
                plane_indices[i] = current_index;
                for (int j = i + 1; j < polygon_plane_array.Length; j++)
                {
                    if (polygon_plane_array[j] == polygon_plane)
                    {
                        plane_indices[j] = current_index;
                    }
                }
            }
            */
            //float time5 = Time.realtimeSinceStartup;


            // combine triangles into planar (possibly self-intersecting) polygons
            __removeEdges.Clear();
            __removePolygons.Clear();
            for (var p = (short)(controlMesh.Polygons.Length - 1); p >= 0; p--)
            {
                retry_polygon:
                var currPlaneIndex	= p;//plane_indices[p];
                var currEdges		= controlMesh.Polygons[p].EdgeIndices;

                for (var e = 0; e < currEdges.Length; e++)
                {
                    var currIndex	= currEdges[e];
                    if (currIndex < 0 || currIndex >= controlMesh.Edges.Length)
                    {
                        Debug.LogWarning("edge with index " + currIndex + " is invalid", brush);
                        continue;
                    }
                    var twinIndex	= controlMesh.GetTwinEdgeIndex(currIndex);

                    if (twinIndex < 0 || twinIndex >= controlMesh.Edges.Length)
                    {
                        Debug.LogWarning("edge " +currIndex + " with twin-index " + twinIndex + " is invalid", brush);
                        continue;
                    }
                    
                    if (controlMesh.Edges[currIndex].HardEdge ||
                        controlMesh.Edges[twinIndex].HardEdge)
                        continue;

                    var twinPolygonIndex	= controlMesh.GetEdgePolygonIndex(twinIndex);
                    var twinPlaneIndex		= twinPolygonIndex;//plane_indices[twin_polygon_index];

                    // internal edge, that on both sides leads to same polygon
                    // this can happen when joining enough polygons together
                    if (p == twinPolygonIndex)
                    {
                        // remove both edges from polygon
                        var srcIndex1 = ArrayUtility.IndexOf(currEdges, currIndex);
                        var srcIndex2 = ArrayUtility.IndexOf(currEdges, twinIndex);
                        if (srcIndex1 == -1 ||
                            srcIndex2 == -1)
                        {
                            Debug.LogWarning("src_index1 == -1 || src_index2 == -1");
                            continue;
                        }
                        if (srcIndex1 > srcIndex2)
                        {
                            ArrayUtility.RemoveAt(ref controlMesh.Polygons[p].EdgeIndices, srcIndex1);
                            ArrayUtility.RemoveAt(ref controlMesh.Polygons[p].EdgeIndices, srcIndex2);
                        } else
                        {
                            ArrayUtility.RemoveAt(ref controlMesh.Polygons[p].EdgeIndices, srcIndex2);
                            ArrayUtility.RemoveAt(ref controlMesh.Polygons[p].EdgeIndices, srcIndex1);
                        }
                        __removeEdges.Add(currIndex);
                        __removeEdges.Add(twinIndex);

                        // polygon can still have other internal edges / edges to 
                        //	other polygons on same plane
                        goto retry_polygon;
                    }

                    if (currPlaneIndex != twinPlaneIndex)
                        continue;

                    var srcIndex		= ArrayUtility.IndexOf(currEdges, currIndex);
                    var dstIndex		= ArrayUtility.IndexOf(controlMesh.Polygons[twinPolygonIndex].EdgeIndices, twinIndex);
                    __removeEdges.Add(currIndex);
                    __removeEdges.Add(twinIndex);
                    ArrayUtility.RemoveAt(ref controlMesh.Polygons[twinPolygonIndex].EdgeIndices, dstIndex);

                    if (srcIndex < 0 ||
                        srcIndex >= currEdges.Length)
                    {
                        Debug.LogWarning("src_index < 0 || src_index >= curr_edges.Length");
                        continue;
                    }

                    var iterator = (srcIndex + 1) % currEdges.Length;
                    while (iterator != srcIndex)
                    {
                        var srcEdgeIndex = currEdges[iterator];
                        controlMesh.Edges[srcEdgeIndex].PolygonIndex = twinPolygonIndex;
                        ArrayUtility.Insert(ref controlMesh.Polygons[twinPolygonIndex].EdgeIndices, dstIndex, srcEdgeIndex);
                        iterator = (iterator + 1) % currEdges.Length; dstIndex++;
                    }

                    __removePolygons.Add(p);
                    break;
                }
            }
            //float time6 = Time.realtimeSinceStartup;
            
            RemoveEdges(controlMesh, __removeEdges); __removeEdges.Clear();
            //float time7 = Time.realtimeSinceStartup;

            var sortedRemovePolygons = new List<short>(__removePolygons);
            sortedRemovePolygons.Sort();

            for (var p = sortedRemovePolygons.Count - 1; p >= 0; p--)
            {
                var polygonIndex = sortedRemovePolygons[p];
                ArrayUtility.RemoveAt(ref controlMesh.Polygons, polygonIndex);
                ArrayUtility.RemoveAt(ref shape.Surfaces, polygonIndex);
                //ArrayUtility.RemoveAt(ref plane_indices, polygon_index);
                for (int i = 0; i < controlMesh.Edges.Length; i++)
                {
                    if (controlMesh.Edges[i].PolygonIndex >= polygonIndex)
                        controlMesh.Edges[i].PolygonIndex--;
                }
            }
            
            CalculatePlanes(controlMesh, shape);
            
            Validate(controlMesh, shape);
            return true;
        }

        private sealed class PointIntersection
        {
            public PointIntersection(short vertexIndex, List<int> planes)
            {
                PlaneIndices.AddRange(planes);
                VertexIndex	= vertexIndex;
                EdgeIndices.Clear();
            }


            public readonly List<int>	EdgeIndices = new List<int>();
            public readonly List<int>	PlaneIndices = new List<int>();
            public short		VertexIndex;

            public bool ContainsPlanes(int[] planeIndex)
            {
                if (PlaneIndices.Count < 3)
                    return false;
                var i = 0;
                for (var n = 0;n < 3; n++)
                {
                    while(true)
                    {
                        if (i == PlaneIndices.Count ||
                            PlaneIndices[i] > planeIndex[n])
                            return false;
                        if (PlaneIndices[i] == planeIndex[n])
                            break;
                        i++;
                    }
                    i++;
                }
                return true;
            }
        }

        private sealed class EdgeIntersection
        {
            public EdgeIntersection(short vertexIndex, short vertexIndexB, int edgeIndex, int planeIndexA, int planeIndexB)
            {
                VertexIndex		= vertexIndex;
                VertexIndexB	= vertexIndexB;
                EdgeIndex		= edgeIndex;
                TwinIndex		= -1;
                NextIndex		= -1;

                PlaneIndices[0] = planeIndexA;
                PlaneIndices[1] = planeIndexB;		
            }

            public readonly short	VertexIndex;
            public readonly short	VertexIndexB;
            public readonly int		EdgeIndex;
            public int				TwinIndex;
            public int				NextIndex;
            public readonly int[] PlaneIndices = new int[2];

            public override string ToString()
            {
                return string.Format("vA: {0}, vB: {1}, pA: {2}, pB: {3}, eI: {4}, tI: {5}, nI: {6}",
                        VertexIndex, VertexIndexB,
                        PlaneIndices[0], PlaneIndices[1],
                        EdgeIndex, TwinIndex, NextIndex);
            }
        };

        public static ControlMesh CreateFromShape(Shape shape, float distanceEpsilon = MathConstants.DistanceEpsilon)
        {
        //	var surfaces		= shape.surfaces;
            var surfaceCount	= shape.Surfaces.Length;
             
            if (shape.TexGens == null)
            {
                shape.TexGens = new TexGen[shape.Surfaces.Length];
            } else
            while (shape.TexGens.Length < shape.Surfaces.Length)
            {
                ArrayUtility.Add(ref shape.TexGens, shape.TexGens[shape.TexGens.Length - 1]);
            }
            if (shape.TexGenFlags == null)
            {
                shape.TexGenFlags = new TexGenFlags[shape.Surfaces.Length];
                for (int i = 0; i < shape.TexGenFlags.Length; i++)
                    shape.TexGenFlags[i] = RealtimeCSG.CSGSettings.DefaultTexGenFlags;
            } else
            while (shape.TexGenFlags.Length < shape.Surfaces.Length)
            {
                ArrayUtility.Add(ref shape.TexGenFlags, shape.TexGenFlags[shape.TexGenFlags.Length - 1]);
            }

            for (var s = 0; s < shape.Surfaces.Length; s++)
            {
                Vector3 binormal, tangent;
                var normal		= shape.Surfaces[s].Plane.normal;
                GeometryUtility.CalculateTangents(normal, out tangent, out binormal);

                var scaleX = shape.Surfaces[s].Tangent .magnitude;
                var scaleY = shape.Surfaces[s].BiNormal.magnitude;
                if (Mathf.Abs(scaleX) < MathConstants.EqualityEpsilon) scaleX = 1;
                if (Mathf.Abs(scaleY) < MathConstants.EqualityEpsilon) scaleY = 1;

                shape.Surfaces[s].Tangent  /= scaleX;
                shape.Surfaces[s].BiNormal /= scaleY;

                if (Vector3.Dot(shape.Surfaces[s].Tangent,  tangent ) < 0) { scaleX = -scaleX; }
                if (Vector3.Dot(shape.Surfaces[s].BiNormal, binormal) < 0) { scaleY = -scaleY; }
                
                shape.Surfaces[s].Tangent  = tangent;
                shape.Surfaces[s].BiNormal = binormal;

                shape.TexGens[s].Scale.x *= scaleX;
                shape.TexGens[s].Scale.y *= scaleY;
            }
            
            var	pointIntersections	= new List<PointIntersection>(surfaceCount * surfaceCount);
            var vertices			= new List<Vector3>(4 * surfaceCount);

            var planeVertices		= new List<int>[surfaceCount];
            for (int i = 0; i < surfaceCount; i++)
                planeVertices[i] = new List<int>();

            // rotate/scale the planes into object space
            var planes = new CSGPlane[surfaceCount];
            for (var i = 0; i < surfaceCount; i++)
                planes[i] = shape.Surfaces[i].Plane;

                
            { 
                var intersectingPlanes	= new List<int>(3 * surfaceCount);
            
                var	curPlaneIndex	= new int[3];
                var curPlane		= new CSGPlane[3];

                // Find all point intersections where 3 (or more planes) intersect
                for (var p1 = 0; p1 < surfaceCount - 2; p1++)
                {
                    curPlaneIndex[0]	= p1;
                    curPlane[0]			= planes[curPlaneIndex[0]];
                    for (var p2 = p1 + 1; p2 < surfaceCount - 1; p2++)
                    {
                        curPlaneIndex[1]	= p2;
                        curPlane[1]			= planes[curPlaneIndex[1]];
                        for (var p3 = p2 + 1; p3 < surfaceCount; p3++)
                        {
                            curPlaneIndex[2]	= p3;
                            curPlane[2]			= planes[curPlaneIndex[2]];

                            var	found = false;
                            // Fix for situation where duplicate plane triplets are 
                            // generated due to precision issues
                            for (var i = pointIntersections.Count - 1; i >= 0; i--)
                            {
                                if (!pointIntersections[i].ContainsPlanes(curPlaneIndex))
                                    continue;

                                found = true;
                                break;
                            }
                            if (found)
                                continue;

                            // Calculate the intersection
                            var vertex = CSGPlane.Intersection(curPlane[0],
                                                               curPlane[1], 
                                                               curPlane[2]);

                            // Check if the intersection is valid
                            if (float.IsNaN(vertex.x) || float.IsInfinity(vertex.x) ||
                                float.IsNaN(vertex.y) || float.IsInfinity(vertex.y) ||
                                float.IsNaN(vertex.z) || float.IsInfinity(vertex.z))
                            {
                                continue;
                            }

                            intersectingPlanes.Clear();
                            intersectingPlanes.Add(curPlaneIndex[0]);
                            intersectingPlanes.Add(curPlaneIndex[1]);
                            intersectingPlanes.Add(curPlaneIndex[2]);
                            
                            for (var p4 = 0; p4 < surfaceCount; p4++)
                            {
                                var	planeIndex4 = p4;
                                if (planeIndex4 == curPlaneIndex[0] ||
                                    planeIndex4 == curPlaneIndex[1] ||
                                    planeIndex4 == curPlaneIndex[2])
                                    continue;

                                var plane4	= planes[planeIndex4];
                                var side	= plane4.Distance(vertex);

                                if (side < -distanceEpsilon) // inside
                                    continue;

                                if (side > distanceEpsilon)	// outside
                                    // Intersection is outside of other planes
                                    goto SkipIntersection;

                                // intersects
                                if (planeIndex4 < curPlaneIndex[2])
                                    // Already found this vertex
                                    goto SkipIntersection;
                                
                                // We've found another plane which goes trough our found intersection point
                                intersectingPlanes.Add(planeIndex4);
                            }
                            /*
                            if (intersectingPlanes.Count > 3)
                            {
                                for (int p = 0; p < intersectingPlanes.Count; p++)
                                {
                                    vertex = planes[intersectingPlanes[p]].Project(vertex);
                                }
                            }
                            //*/

                            {
                                //*
                                // in case we have a situation where plane intersections create vertices *really* close to each other
                                for (int p = 0; p < pointIntersections.Count; p++)
                                {
                                    var pointIntersection = pointIntersections[p];
                                    var delta = vertex - vertices[pointIntersection.VertexIndex];

                                    if (Mathf.Abs(delta.x) < distanceEpsilon &&
                                        Mathf.Abs(delta.y) < distanceEpsilon &&
                                        Mathf.Abs(delta.z) < distanceEpsilon)
                                    {
                                        for (int n = 0; n < intersectingPlanes.Count; n++)
                                        {
                                            if (!pointIntersection.PlaneIndices.Contains(intersectingPlanes[n]))
                                                pointIntersection.PlaneIndices.Add(intersectingPlanes[n]);
                                        }
                                        goto SkipIntersection;
                                    }
                                }
                                //*/
                                
                                var vertexIndex = (short)(vertices.Count);
                                vertices.Add(vertex);

                                
                                //*
                                for (int i = 0; i < intersectingPlanes.Count; i++)
                                    planeVertices[intersectingPlanes[i]].Add(pointIntersections.Count);
                                //*/

                                // Add intersection point to our list
                                pointIntersections.Add(new PointIntersection(vertexIndex, intersectingPlanes));
                            }

                        SkipIntersection:
                            ;
                        }
                    }
                }
                intersectingPlanes.Clear();
            }

            if (pointIntersections.Count == 0)
            {
#if DEBUG
                Debug.LogWarning("No valid plane intersections found in brush");
#endif
                return null;
            }
            
            //*
            bool needToCleanUpPlanesAgain = false;
CleanUpPlanesAgain:

            // Check if we have planes that intersect with too few points / or are duplicates
            for (var p = planeVertices.Length - 1; p >= 0; p--)
            {
                if (planeVertices.Length >= 3)
                {
                    bool haveDuplicatePlane = false;
                    for (var p2 = 0; p2 < p; p2++)
                    {
                        if (planeVertices[p].Count != planeVertices[p2].Count)
                            continue;
                        haveDuplicatePlane = true;
                        for (int j = 0; j < planeVertices[p].Count; j++)
                        {
                            if (planeVertices[p] != planeVertices[p2])
                            {
                                haveDuplicatePlane = false;
                                break;
                            }

                        }
                    }
                    if (!haveDuplicatePlane)
                        continue;
#if DEBUG
                    Debug.LogWarning("Found duplicate plane in brush");
#endif
                } else
                {
#if DEBUG
                    Debug.LogWarning("Found plane in brush that only intersects a single edge");
#endif
                }
                
                ArrayUtility.RemoveAt(ref planeVertices, p);
                ArrayUtility.RemoveAt(ref shape.Surfaces, p);
                ArrayUtility.RemoveAt(ref shape.TexGens, p);
                ArrayUtility.RemoveAt(ref shape.TexGenFlags, p);
                for (int i = 0; i < pointIntersections.Count; i++)
                {
                    var intersection = pointIntersections[i];
                    var planeIndices = intersection.PlaneIndices;

                    for (int j = planeIndices.Count - 1; j >= 0; j--)
                    {
                        if (planeIndices[j] < p)
                            continue;
                        if (planeIndices[j] == p)
                            planeIndices.RemoveAt(j);
                        else
                            planeIndices[j]--;
                    }
                }
            }

            // Check if we have points that intersect with too few planes
            for (var i = pointIntersections.Count - 1; i >= 0; i--)
            {
                var intersection = pointIntersections[i];
                var planeIndices = intersection.PlaneIndices;
                if (planeIndices.Count >= 3)
                    continue;
                
                for (int p = planeVertices.Length - 1; p >= 0; p--)
                {
                    var verticesForPlane = planeVertices[p]; 
                    for (int v = verticesForPlane.Count - 1; v >= 0; v--)
                    {
                        if (verticesForPlane[v] < i)
                            continue;
                        if (verticesForPlane[v] == i)
                        {
                            verticesForPlane.RemoveAt(v);
                            needToCleanUpPlanesAgain = true;
                        } else
                            verticesForPlane[v]--;
                    }
                }
            }
            
            if (needToCleanUpPlanesAgain)
            {
                needToCleanUpPlanesAgain = false;
                goto CleanUpPlanesAgain;
            }

            if (planeVertices.Length < 4)
            {
#if DEBUG
                Debug.LogWarning("Not enough valid planes to create brush");
#endif
                return null;
            }
            //*/

            
#if DEBUG
            var vertexPlanes = new float[planes.Length][];
            for (int p = 0; p < planes.Length; p++)
            {
                vertexPlanes[p] = new float[vertices.Count];
                for (int v = 0; v < vertices.Count; v++)
                {
                    vertexPlanes[p][v] = planes[p].Distance(vertices[v]);
                    if (vertexPlanes[p][v] > -MathConstants.DistanceEpsilon &&
                        vertexPlanes[p][v] < MathConstants.DistanceEpsilon)
                        vertexPlanes[p][v] = 0;
                }
            }
#endif



            var	edgeIntersections = new List<EdgeIntersection>(surfaceCount * surfaceCount * 2);
            var edgeCount = 0;
            //*
            var planeEdges = new List<EdgeIntersection>[surfaceCount];
            for (int i = 0; i < surfaceCount; i++)
                planeEdges[i] = new List<EdgeIntersection>();
            
            for (int p = 0; p < planeVertices.Length; p++)
            {
                var verticesForPlane = planeVertices[p];
                for (int v0 = 0; v0 < verticesForPlane.Count - 1; v0++)
                {
                    var point0			= verticesForPlane[v0];
                    var planesIndices0	= pointIntersections[point0].PlaneIndices;
                    for (int i0 = 0; i0 < planesIndices0.Count; i0++)
                    {
                        var planeIndex0 = planesIndices0[i0];
                        if (planeIndex0 <= p)
                            continue;

                        for (int v1 = v0 + 1; v1 < verticesForPlane.Count; v1++)
                        {
                            var point1			= verticesForPlane[v1];
                            var planesIndices1	= pointIntersections[point1].PlaneIndices;

                            for (int i1 = 0; i1 < planesIndices1.Count; i1++)
                            {
                                var planeIndex1 = planesIndices1[i1];
                                if (planeIndex0 != planeIndex1)
                                    continue;
                                
                                // Create our found intersection edge
                                var edgeIndex0 = edgeCount; edgeCount++;
                                var edgeIndex1 = edgeCount; edgeCount++;
                                
                                var intersectionIndex0 = edgeIntersections.Count;
                                var intersection0 = new EdgeIntersection(pointIntersections[point0].VertexIndex, pointIntersections[point1].VertexIndex,
                                                                             edgeIndex0,
                                                                             p,
                                                                             planeIndex1);
                                edgeIntersections.Add(intersection0);

                                var intersectionIndex1 = edgeIntersections.Count;
                                var intersection1 = new EdgeIntersection(pointIntersections[point1].VertexIndex, pointIntersections[point0].VertexIndex,
                                                                             edgeIndex1,
                                                                             planeIndex1,
                                                                             p); 
                                edgeIntersections.Add(intersection1); 

                                edgeIntersections[intersectionIndex0].TwinIndex = intersectionIndex1;
                                edgeIntersections[intersectionIndex1].TwinIndex = intersectionIndex0;
                                
                                planeEdges[p          ].Add(intersection0);
                                planeEdges[planeIndex1].Add(intersection1);

                                // Add it to our points
                                pointIntersections[point0].EdgeIndices.Add(intersectionIndex0);
                                pointIntersections[point1].EdgeIndices.Add(intersectionIndex1);
                            }
                        }
                    }
                }
            }
             
            /*/
            var foundPlanes = new[] { 0, 0 };
            // Find all our intersection edges which are formed by a pair of planes
            // (this could probably be done inside the previous loop)
            for (var i = 0; i < pointIntersections.Count; i++)
            {
                for (var j = i + 1; j < pointIntersections.Count; j++)
                {
                    var	planesIndicesA	= pointIntersections[i].PlaneIndices;
                    var	planesIndicesB	= pointIntersections[j].PlaneIndices;

                    var foundPlaneIndex = 0;
                    for (var piA = 0; piA < planesIndicesA.Count; piA++)
                    {
                        var currentPlaneIndex = planesIndicesA[piA];

                        var found = false;
                        for (var piB = 0; piB < planesIndicesB.Count; piB++)
                        {
                            if (planesIndicesB[piB] != currentPlaneIndex)
                                continue;

                            found = true;
                            break;
                        }

                        if (!found)//!planesIndicesB.Contains(currentPlaneIndex))
                            continue;

                        foundPlanes[foundPlaneIndex] = currentPlaneIndex;
                        foundPlaneIndex++;

                        if (foundPlaneIndex == 2)
                            break;
                    }

                    // If foundPlaneIndex is 0 or 1 then either this combination does not exist, 
                    // or only goes trough one point 
                    if (foundPlaneIndex < 2)
                        continue;

                    // Create our found intersection edge
                    var edgeIndexA = edgeCount; edgeCount++;
                    var edgeIndexB = edgeCount; edgeCount++;

                    var intersectionA = edgeIntersections.Count;
                    edgeIntersections.Add(new EdgeIntersection(pointIntersections[i].VertexIndex, pointIntersections[j].VertexIndex,
                                                                 edgeIndexA,
                                                                 foundPlanes[0],
                                                                 foundPlanes[1]));
                    var intersectionB = edgeIntersections.Count;
                    edgeIntersections.Add(new EdgeIntersection(pointIntersections[j].VertexIndex, pointIntersections[i].VertexIndex,
                                                                 edgeIndexB,
                                                                 foundPlanes[1],
                                                                 foundPlanes[0])); 
                    edgeIntersections[intersectionA].TwinIndex = intersectionB;
                    edgeIntersections[intersectionB].TwinIndex = intersectionA;

                    // Add it to our points
                    pointIntersections[i].EdgeIndices.Add(intersectionA);
                    pointIntersections[j].EdgeIndices.Add(intersectionB);
                }
            }
            //*/
    
            var polygonFirst	= new int[surfaceCount];
            var polygonBounds	= new AABB[surfaceCount];
            for (var i = 0; i < surfaceCount; i++)
            {
                polygonFirst [i] = -1;
                polygonBounds[i] = new AABB();
            }
            
            var bounds = new AABB();
            for (var i = pointIntersections.Count - 1; i >= 0; i--)
            {
                var pointEdgeIndices	= pointIntersections[i].EdgeIndices;

                // Make sure that we have at least 2 edges ...
                // This may happen when a plane only intersects at a single edge.
                if (pointEdgeIndices.Count <= 2)
                {
                    var	vertexIndex1 = pointIntersections[i].VertexIndex;

                    for (int p = 0; p < pointIntersections.Count; p++)
                    {
                        if (pointIntersections[p].VertexIndex > vertexIndex1)
                            pointIntersections[p].VertexIndex--;
                    }
                    
                    pointIntersections.RemoveAt(i);
                    vertices.RemoveAt(vertexIndex1);
                    continue;
                }

                var	vertexIndex		= pointIntersections[i].VertexIndex;
                var	vertex			= vertices[vertexIndex];

                for (var j = 0; j < pointEdgeIndices.Count - 1; j++)
                {
                    var	edge1Index	= pointEdgeIndices[ j ];
                    for (var k = j + 1; k < pointEdgeIndices.Count; k++)
                    {
                        var	edge2Index	= pointEdgeIndices[ k ];

                        int planeOrder1, planeOrder2;

                        // Determine if and which of our 2 planes are identical
                        if		(edgeIntersections[edge1Index].PlaneIndices[0] == edgeIntersections[edge2Index].PlaneIndices[0]) { planeOrder1 = 0; planeOrder2 = 0; }
                        else if (edgeIntersections[edge1Index].PlaneIndices[0] == edgeIntersections[edge2Index].PlaneIndices[1]) { planeOrder1 = 0; planeOrder2 = 1; }
                        else if (edgeIntersections[edge1Index].PlaneIndices[1] == edgeIntersections[edge2Index].PlaneIndices[0]) { planeOrder1 = 1; planeOrder2 = 0; }
                        else if (edgeIntersections[edge1Index].PlaneIndices[1] == edgeIntersections[edge2Index].PlaneIndices[1]) { planeOrder1 = 1; planeOrder2 = 1; }
                        else
                            continue;


                        int ingoingEdgeIndex;
                        int outgoingEdgeIndex;

                        var sharedPlane	= planes[edgeIntersections[edge1Index].PlaneIndices[    planeOrder1]];
                        var edge1Plane	= planes[edgeIntersections[edge1Index].PlaneIndices[1 - planeOrder1]];
                        var edge2Plane	= planes[edgeIntersections[edge2Index].PlaneIndices[1 - planeOrder2]];

                        var direction	= Vector3.Cross(sharedPlane.normal, edge1Plane.normal);

                        // Determine the orientation of our two edges to determine 
                        // which edge is in-going, and which one is out-going
                        var alignment = Vector3.Dot(direction, edge2Plane.normal);
                        if (alignment < 0)
                        {
                            ingoingEdgeIndex	= edge2Index;
                            outgoingEdgeIndex	= edgeIntersections[edge1Index].TwinIndex;
                        } else
                        {
                            ingoingEdgeIndex	= edge1Index;
                            outgoingEdgeIndex	= edgeIntersections[edge2Index].TwinIndex;
                        }


                        // Link the out-going half-edge to the in-going half-edge
                        edgeIntersections[ingoingEdgeIndex].NextIndex = outgoingEdgeIndex;

                     
                        // Add reference to polygon to half-edge, and make sure our  
                        // polygon has a reference to a half-edge 
                        // Since a half-edge, in this case, serves as a circular 
                        // linked list this just works.
                        var polygonIndex = edgeIntersections[edge1Index].PlaneIndices[planeOrder1];
                    
                        polygonFirst [polygonIndex] = outgoingEdgeIndex;
                        polygonBounds[polygonIndex].MinX = Mathf.Min(polygonBounds[polygonIndex].MinX, vertex.x);
                        polygonBounds[polygonIndex].MaxX = Mathf.Min(polygonBounds[polygonIndex].MaxX, vertex.x);
                        polygonBounds[polygonIndex].MinY = Mathf.Min(polygonBounds[polygonIndex].MinY, vertex.y);
                        polygonBounds[polygonIndex].MaxY = Mathf.Min(polygonBounds[polygonIndex].MaxY, vertex.y);
                        polygonBounds[polygonIndex].MinZ = Mathf.Min(polygonBounds[polygonIndex].MinZ, vertex.z);
                        polygonBounds[polygonIndex].MaxZ = Mathf.Min(polygonBounds[polygonIndex].MaxZ, vertex.z);
                    }
                }

                // Add the intersection point to the area of our bounding box
                bounds.MinX = Mathf.Min(bounds.MinX, vertex.x);
                bounds.MaxX = Mathf.Min(bounds.MaxX, vertex.x);
                bounds.MinY = Mathf.Min(bounds.MinY, vertex.y);
                bounds.MaxY = Mathf.Min(bounds.MaxY, vertex.y);
                bounds.MinZ = Mathf.Min(bounds.MinZ, vertex.z);
                bounds.MaxZ = Mathf.Min(bounds.MaxZ, vertex.z);
            }


            var edgeLookup = new int[edgeCount];
            for (var i = 0; i < edgeCount; i++)
                edgeLookup[i] = -1;

            var polygonEdgeIndices = new List<int>(edgeCount);

            var newMesh = new ControlMesh
            {
                Edges		= new HalfEdge[edgeCount]
            };

            var polygons	= new List<Polygon>();
            var edges		= new List<HalfEdge>(edgeCount);
            for (var i = 0; i < edgeCount; i++)
                edges.Add(new HalfEdge());

            for (var i = 0; i < edgeIntersections.Count; i++)
            {
                var edgeIndex	= edgeIntersections[i].EdgeIndex;
                var edge		= edges[edgeIndex];
                edge.TwinIndex		= edgeIntersections[edgeIntersections[i].TwinIndex].EdgeIndex;
                edge.PolygonIndex	= -1;
                edge.VertexIndex	= edgeIntersections[i].VertexIndex;
                edge.HardEdge		= true;
                edges[edgeIndex]	= edge;
            }


            for (var s = 0; s < surfaceCount; s++)
            {
                var firstIntersectionIndex	= polygonFirst[s];
                if (firstIntersectionIndex == -1)
                {
#if DEBUG
                    Debug.LogWarning("Polygon " + s + " has no found edges");
#endif
                    continue;
                }

                //var const	polygon_index	= PolygonIndex(s);
                var surfaceIndex	= s;
//				var polygon_bounds	= polygonBounds[s];
                var polygonIndex	= (short)polygons.Count;
                {
                    polygonEdgeIndices.Clear();
                    var intersectionIteratorIndex = firstIntersectionIndex;
                    uint polygonCount = 0;
                    do
                    {
                        var edgeIndex = edgeIntersections[intersectionIteratorIndex].EdgeIndex;
                        var edge = edges[edgeIndex];
                        edge.PolygonIndex = polygonIndex;
                        edges[edgeIndex] = edge;
                        polygonEdgeIndices.Add(edgeIndex);

                        intersectionIteratorIndex = edgeIntersections[intersectionIteratorIndex].NextIndex;
                        if (intersectionIteratorIndex == -1)
                        {
#if DEBUG
                            Debug.LogWarning("intersectionIteratorIndex == -1");
#endif
                            break;
                        }
                        polygonCount++;
                        if (polygonCount <= 10000)
                            continue;

#if DEBUG
                        Debug.LogError("loop_counter > 10000");
#endif
                        return null;
                    } while (intersectionIteratorIndex != firstIntersectionIndex);
                }

                if (polygonEdgeIndices.Count < 3)
                {
#if DEBUG
                    Debug.LogWarning("Not enough edges found (" + polygonEdgeIndices.Count + ") for polygon " + s);
                    continue;
#endif
                }

                {
                    polygons.Add(new Polygon(polygonEdgeIndices.ToArray(), surfaceIndex));
                }
            }
            /*
            var vertexUsed = new HashSet<short>();
            for (var e = edges.Count - 1; e >= 0; e--)
            {
                if (edges[e].PolygonIndex != -1)
                {
                    vertexUsed.Add(edges[e].VertexIndex);
                    continue;
                }

                edges.RemoveAt(e);
                for (var e2 = 0; e2 < edges.Count; e2++)
                {
                    if (edges[e2].TwinIndex > e)
                    {
                        var otherEdge = edges[e2];
                        otherEdge.TwinIndex--;
                        edges[e2] = otherEdge;
                    }
                }
                
                for (var p = 0; p < polygons.Count; p++)
                {
                    var edgeIndices = polygons[p].EdgeIndices;

                    for (var e3 = 0; e3 < edgeIndices.Length; e3++)
                    {
                        if (edgeIndices[e3] < e)
                            continue;
                        
                        if (edgeIndices[e3] == e)
                        {
                            Debug.LogWarning("Corrupted data");
                            return null;							
                        }
                        if (edgeIndices[e3] > e)
                            edgeIndices[e3]--;
                    }
                }
            }

            for (var v = vertices.Count - 1; v >= 0; v--)
            {
                if (vertexUsed.Contains((short)v))
                    continue;

                vertices.RemoveAt(v);
                for (var e = 0; e < edges.Count; e++)
                {
                    if (edges[e].VertexIndex > v)
                    {
                        var otherEdge = edges[e];
                        otherEdge.VertexIndex--;
                        edges[e] = otherEdge;
                    }
                    if (edges[e].VertexIndex == v)
                    {
                        Debug.LogWarning("Corrupted data");
                        return null;	
                    }
                }
            }
            */

            newMesh.Vertices	= vertices.ToArray();
            newMesh.Polygons	= polygons.ToArray();
            newMesh.Edges		= edges.ToArray();
            
            RemoveUnusedTexGens(newMesh, shape);
            
            return newMesh;
        }

        public static List<int> FindEdgeLoop(ControlMesh controlMesh, ref List<int> selectedEdges)
        {
            if (selectedEdges.Count == 0)
                return null;

            var edgeLoop		= new List<int>();
            var startEdgeIndex	= selectedEdges.Count - 1;
            var selectedEdge	= selectedEdges[startEdgeIndex];
            var startTwinIndex	= selectedEdges.IndexOf(controlMesh.GetTwinEdgeIndex(selectedEdge));
            var startVertexCurr = controlMesh.GetVertexIndex(selectedEdge);
            var startVertexPrev = controlMesh.GetTwinEdgeVertexIndex(selectedEdge);
            var prevVertex		= startVertexCurr;
            edgeLoop.Add(selectedEdge);
            selectedEdges.RemoveAt(startEdgeIndex);
            if (startTwinIndex != -1)
                selectedEdges.RemoveAt(startTwinIndex);

            while (true)
            {
                var found = false;
                for (var currentEdgeIndex = selectedEdges.Count - 1; currentEdgeIndex >= 0; currentEdgeIndex--)
                {
                    var currentEdge		  = selectedEdges[currentEdgeIndex];
                    var currentVertexCurr = controlMesh.GetVertexIndex(currentEdge);
                    var currentVertexPrev = controlMesh.GetTwinEdgeVertexIndex(currentEdge);
                    if (currentVertexPrev != prevVertex)
                        continue;

                    edgeLoop.Add(currentEdge);
                    selectedEdges.RemoveAt(currentEdgeIndex);

                    var currentTwinIndex = selectedEdges.IndexOf(controlMesh.GetTwinEdgeIndex(currentEdge));
                    if (currentTwinIndex != -1)
                        selectedEdges.RemoveAt(currentTwinIndex);

                    found = true;
                    if (currentVertexCurr == startVertexPrev)
                    {
                        return edgeLoop;
                    }

                    prevVertex = currentVertexCurr;
                    break;
                }
                if (!found)
                    return null;
            }
        }


        public static ControlMesh EnsureValidControlMesh(CSGBrush brush)
        {
            if (brush == null)
                return null;

            var controlMesh = brush.ControlMesh;
            if (controlMesh == null ||
                controlMesh.Vertices == null || controlMesh.Vertices.Length == 0 ||
                controlMesh.Edges == null || controlMesh.Edges.Length == 0 ||
                controlMesh.Polygons == null || controlMesh.Polygons.Length == 0 ||
                brush.Shape == null)
            {
                BrushFactory.CreateCubeControlMesh(out brush.ControlMesh, out brush.Shape, Vector3.one);
            }
            if (brush.Shape.Surfaces == null ||
                brush.Shape.Surfaces.Length == 0)
            {
                CalculatePlanes(controlMesh, brush.Shape);
            }

            return controlMesh;
        }


        public static void FindAllAcceptableEdgesAroundEdge(ControlMesh controlMesh, AcceptableTopology acceptableTopology, int centerEdge, int currentOverPoint)
        {
            acceptableTopology.Clear();

            var centerTwinEdge	= controlMesh.GetTwinEdgeIndex(centerEdge);

            var polygonIndex1	= controlMesh.GetEdgePolygonIndex(centerEdge);
            var polygonIndex2	= controlMesh.GetEdgePolygonIndex(centerTwinEdge);

            if (polygonIndex1 >= 0) acceptableTopology.AcceptablePolygons.Add(polygonIndex1);
            if (polygonIndex2 >= 0) acceptableTopology.AcceptablePolygons.Add(polygonIndex2);

            foreach (var polygonIndex in acceptableTopology.AcceptablePolygons)
            {
                var edgeIndices = controlMesh.Polygons[polygonIndex].EdgeIndices;
                for (var e = 0; e < edgeIndices.Length; e++)
                {
                    var edgeIndex = edgeIndices[e];
                    if (controlMesh.GetTwinEdgeVertexIndex(edgeIndex) == currentOverPoint)
                        continue;

                    acceptableTopology.AcceptableEdges.Add(edgeIndex);
                    acceptableTopology.AcceptablePoints.Add(controlMesh.GetVertexIndex(edgeIndex));
                }
            }

            acceptableTopology.AcceptableEdges.Remove(centerEdge);
            acceptableTopology.AcceptableEdges.Remove(centerTwinEdge);
            acceptableTopology.AcceptablePoints.Remove(controlMesh.GetVertexIndex(centerEdge));
            acceptableTopology.AcceptablePoints.Remove(controlMesh.GetVertexIndex(centerTwinEdge));
        }

        public static void FindAllAcceptableEdgesAroundPoint(ControlMesh controlMesh, AcceptableTopology acceptableTopology, int centerPointEdge, int currentOverPoint)
        {
            acceptableTopology.Clear();

            var iterator = centerPointEdge;
            do
            {
                var polygonIndex = controlMesh.GetEdgePolygonIndex(iterator);
                if (acceptableTopology.AcceptablePolygons.Add(polygonIndex))
                {
                    var edgeIndices = controlMesh.Polygons[polygonIndex].EdgeIndices;
                    for (var e = 0; e < edgeIndices.Length; e++)
                    {
                        var edgeIndex = edgeIndices[e];
                        if (edgeIndex == iterator || 
                            controlMesh.GetTwinEdgeVertexIndex(edgeIndex) == currentOverPoint)
                            continue;

                        acceptableTopology.AcceptableEdges.Add(edgeIndex);
                        acceptableTopology.AcceptablePoints.Add(controlMesh.GetVertexIndex(edgeIndex));
                    }
                }
                iterator = controlMesh.GetNextEdgeIndexAroundVertex(iterator);
            } while (iterator != centerPointEdge);
            
            iterator = centerPointEdge;
            do
            {
                var twinIterator	= controlMesh.GetTwinEdgeIndex(iterator);
                var vertex1			= controlMesh.GetVertexIndex(iterator);
                var vertex2			= controlMesh.GetVertexIndex(twinIterator);
                acceptableTopology.AcceptablePoints.Remove(vertex1);
                acceptableTopology.AcceptablePoints.Remove(vertex2);
                acceptableTopology.AcceptableEdges.Remove(iterator);
                acceptableTopology.AcceptableEdges.Remove(twinIterator);
                iterator = controlMesh.GetNextEdgeIndexAroundVertex(iterator);
            } while (iterator != centerPointEdge);
        }
        
        public static bool RebuildShapes(CSGBrush[] brushes)
        {
            var result = true;
            for (var i = 0; i < brushes.Length; i++)
                result = RebuildShape(brushes[i]) && result;
            return result;
        }
        
        public static bool RebuildShapeFrom(CSGBrush brush, ControlMesh srcControlMesh, Shape srcShape)
        {
            if (brush == null ||
                srcControlMesh == null ||
                srcShape == null)
                return false;

            var triangulatedControlMesh		= srcControlMesh;
            var triangulatedShape			= srcShape;
            
            // subdivide polygons using ear-clipping
            if (!Triangulate(brush, triangulatedControlMesh, triangulatedShape))
            {
                srcControlMesh.Valid = false;
                return false;
            }

            FixTexGens(triangulatedControlMesh, triangulatedShape);
            
            if (!IsConvex(triangulatedControlMesh, triangulatedShape))
            {
                triangulatedControlMesh.Valid = false;
                return false;
            }

            UpdateTangents(triangulatedControlMesh, triangulatedShape);

            if (!UpdateBrushMesh(brush, triangulatedControlMesh, triangulatedShape))
            {
                srcControlMesh.Valid = false;
                return false;
            }
            
            
            triangulatedControlMesh.Generation = -1;
            brush.ControlMesh.Valid	= true;
            return true;
        }
        
        public static bool RebuildShape(CSGBrush brush)
        { 
            if (brush == null || brush.ControlMesh == null || brush.Shape == null)
                return false;
            return RebuildShapeFrom(brush, brush.ControlMesh.Clone(), brush.Shape.Clone());
        }

        internal static bool IsConvex(ControlMesh controlMesh, Shape srcShape)
        {
            var isConvex	= true;
            if (srcShape == null || controlMesh == null)
                return false;

            for (var j = 0; isConvex && j < controlMesh.Vertices.Length; j++)
            {
                var point = controlMesh.Vertices[j];
                for (var i = 0; i < srcShape.Surfaces.Length; i++)
                {
                    var plane = srcShape.Surfaces[i].Plane;
                    if (plane.Distance(point) > MathConstants.DistanceEpsilon)
                    {
                        isConvex = false;
                        break;
                    }
                }
            }

            return isConvex;
        }

        public static bool BuildMesh(CSGBrush brush, ControlMesh controlMesh, Shape srcShape)
        {
            if (srcShape == null || controlMesh == null)
                return false;

            if (IsConvex(controlMesh, srcShape))
                return UpdateBrushMesh(brush, controlMesh, srcShape);

            controlMesh.Valid = false;
            return false;
        }

        internal static void UpdateTangents(ControlMesh controlMesh, Shape srcShape)
        {
            RemoveUnusedTexGens(controlMesh, srcShape);

            for (var i = 0; i < srcShape.Surfaces.Length; i++)
            {
                var normal = srcShape.Surfaces[i].Plane.normal;

                Vector3 tangent, binormal;
                GeometryUtility.CalculateTangents(normal, out tangent, out binormal);

                srcShape.Surfaces[i].Tangent = tangent;
                srcShape.Surfaces[i].BiNormal = binormal;
                srcShape.Surfaces[i].TexGenIndex = controlMesh.Polygons[i].TexGenIndex;
            }
            
            ShapeUtility.CheckMaterials(srcShape);
        }
        
        internal static bool UpdateBrushMesh(CSGBrush brush, ControlMesh controlMesh, Shape srcShape)
        {
            if (srcShape.Surfaces.Length > controlMesh.Polygons.Length)
            {
                return false;
            }

            brush.Shape = srcShape.Clone();
            brush.ControlMesh = controlMesh.Clone();
            brush.EnsureInitialized();
                
            if (brush.brushNodeID != CSGNode.InvalidNodeID)
            {
                InternalCSGModelManager.CheckSurfaceModifications(brush, true);
                EditorUtility.SetDirty(brush);
            }
            return true;
        }


        // note: expect values in worldspace
        public static void TransformBrushes(CSGBrush[] brushes, Vector3 centerBefore, Vector3 centerAfter, Vector3 scale, SpaceMatrices spaceMatrices)
        {
            var invertBrush = ((((scale.x < 0) ? 1 : 0) + ((scale.y < 0) ? 1 : 0) + ((scale.z < 0) ? 1 : 0)) & 1) == 1;

            var pivotRotation = Tools.pivotRotation;
            for (int t = 0; t < brushes.Length; t++)
            {
                var brush			= brushes[t];
                var brushTransform	= brushes[t].transform;
                var shape			= brush.Shape;
                var controlMesh		= brush.ControlMesh;

                var brushLocalToWorld = brushTransform.localToWorldMatrix;
                var brushWorldToLocal = brushTransform.worldToLocalMatrix;
                
                var vertices = controlMesh.Vertices;
                for (var p = 0; p < controlMesh.Vertices.Length; p++)
                {
                    var pt1 = vertices[p];

                    {
                        pt1 = brushLocalToWorld.MultiplyPoint(pt1);
                    }
                    if (pivotRotation == PivotRotation.Local)
                    {
                        pt1 = spaceMatrices.activeWorldToLocal.MultiplyPoint(pt1);
                    }

                    // world space
                    pt1 -= centerBefore;
                    pt1.Scale(scale);
                    pt1 += centerAfter;
                    
                    if (pivotRotation == PivotRotation.Local)
                    {
                        pt1 = spaceMatrices.activeLocalToWorld.MultiplyPoint(pt1);
                    }
                    {
                        pt1 = brushWorldToLocal.MultiplyPoint(pt1);
                    }

                    vertices[p] = GridUtility.CleanPosition(pt1);
                }

                if (invertBrush)
                {
                    InvertControlMesh(controlMesh);
                    InvertShape(shape);
                    controlMesh.Valid = Validate(controlMesh, shape);
                }
                CalculatePlanes(controlMesh, shape);
                brush.EnsureInitialized();

                brush.ControlMesh.SetDirty();
                EditorUtility.SetDirty(brush);
            }
        }

        public static void InvertControlMesh(ControlMesh controlMesh)
        {
            for (var p = 0; p < controlMesh.Polygons.Length; p++)
            {
                Array.Reverse(controlMesh.Polygons[p].EdgeIndices);
            }
            
            var vertexIndices = new short[controlMesh.Edges.Length];
            for (var e = 0; e < controlMesh.Edges.Length; e++)
                vertexIndices[e] = controlMesh.Edges[e].VertexIndex;

            for (var e = 0; e < controlMesh.Edges.Length; e++)
            {
                var twinIndex = controlMesh.GetTwinEdgeIndex(e);
                controlMesh.Edges[e].VertexIndex = vertexIndices[twinIndex];
            }
        }
        
        public static void InvertShape(Shape shape)
        {
            for (var s = 0; s < shape.Surfaces.Length; s++)
            {
                shape.Surfaces[s].Plane = shape.Surfaces[s].Plane.Negated();
            }
        }


        public static short[] FindDuplicateVerticesToRemove(CSGBrush brush, ControlMeshState meshState)
        {
            var targetControlMesh		= brush.ControlMesh;
            var edges					= targetControlMesh.Edges;
            //var pointSelectState		= meshState.Selection.Points;
            var vertices				= targetControlMesh.Vertices;
            var foundVerticesToRemove	= new HashSet<short>();
            for (var index1 = 0; index1 < vertices.Length; index1++)
            {
                var vertex1 = vertices[index1];
                for (var index2 = index1 + 1; index2 < vertices.Length; index2++)
                {
                    var vertex2 = vertices[index2];
                    if (!((vertex1 - vertex2).sqrMagnitude < MathConstants.EqualityEpsilon))
                        continue;

                    //if ((pointSelectState[index1] & SelectState.Selected) != SelectState.Selected &&
                    //	(pointSelectState[index2] & SelectState.Selected) != SelectState.Selected)
                    //	continue;

                    var found = false;
                    for (var e = 0; e < edges.Length; e++)
                    {
                        var vertexIndex1 = edges[e].VertexIndex;
                        var twinIndex	 = edges[e].TwinIndex;
                        var vertexIndex2 = edges[twinIndex].VertexIndex;
                        if (vertexIndex1 == index1 && vertexIndex2 == index2)
                        {
                            found = true;
                            break;
                        }

                        if (vertexIndex1 != index2 || vertexIndex2 != index1)
                            continue;

                        found = true;
                        break;
                    }

                    if (found)
                        foundVerticesToRemove.Add((short)index1);
                }
            }
            if (foundVerticesToRemove.Count == 0)
                return null;
            
            var foundVerticesToRemoveArray = foundVerticesToRemove.ToArray();
            Array.Sort(foundVerticesToRemoveArray);
            return foundVerticesToRemoveArray;
        }

        public static short[] FindDuplicateVerticesToRemove(CSGBrush brush)
        {
            var targetControlMesh = brush.ControlMesh;
            var edges = targetControlMesh.Edges;
            var vertices = targetControlMesh.Vertices;
            var foundVerticesToRemove = new HashSet<short>();
            for (var index1 = 0; index1 < vertices.Length; index1++)
            {
                var vertex1 = vertices[index1];
                for (var index2 = index1 + 1; index2 < vertices.Length; index2++)
                {
                    var vertex2 = vertices[index2];
                    if (!((vertex1 - vertex2).sqrMagnitude < MathConstants.EqualityEpsilon))
                        continue;

                    var found = false;
                    for (var e = 0; e < edges.Length; e++)
                    {
                        var vertexIndex1 = edges[e].VertexIndex;
                        var twinIndex = edges[e].TwinIndex;
                        var vertexIndex2 = edges[twinIndex].VertexIndex;
                        if (vertexIndex1 == index1 && vertexIndex2 == index2)
                        {
                            found = true;
                            break;
                        }

                        if (vertexIndex1 != index2 || vertexIndex2 != index1)
                            continue;

                        found = true;
                        break;
                    }

                    if (found)
                        foundVerticesToRemove.Add((short)index1);
                }
            }
            if (foundVerticesToRemove.Count == 0)
                return null;

            var foundVerticesToRemoveArray = foundVerticesToRemove.ToArray();
            Array.Sort(foundVerticesToRemoveArray);
            return foundVerticesToRemoveArray;
        }

        public static void MergeDuplicatePoints(CSGBrush[] brushes, ControlMeshState[] controlMeshStates)
        {
            if (brushes == null ||
                controlMeshStates == null)
                return;

            for (var b = 0; b < brushes.Length; b++)
            {
                var brush = brushes[b];
                if (!brush)
                    continue;

                if (b >= controlMeshStates.Length)
                    continue;

                var meshState = controlMeshStates[b];
                if (meshState == null)
                    continue;

                var deletePoints = FindDuplicateVerticesToRemove(brush, meshState);
                if (deletePoints == null)
                    continue;

                var controlMesh	= brush.ControlMesh.Clone();
                var shape		= brush.Shape.Clone();

                RemoveControlPoints(controlMesh, shape, deletePoints);
                controlMesh.SetDirty();

                RebuildShapeFrom(brush, controlMesh, shape);

                EditorUtility.SetDirty(brush);
            }
        }

        public static bool MergeDuplicatePoints(CSGBrush brush, ref ControlMesh controlMesh, ref Shape shape)
        {
            if (!brush ||
                controlMesh == null)
                return false;

            var deletePoints = FindDuplicateVerticesToRemove(brush);
            if (deletePoints == null)
                return false;

            controlMesh = controlMesh.Clone();
            shape = shape.Clone();
            RemoveControlPoints(controlMesh, shape, deletePoints);
            return true;
        }

        public static void MergeHoverPolygonPoints(CSGBrush brush, int polygonIndex)
        {
            if (brush == null)
                return;
            
            var controlMesh = brush.ControlMesh;
            var shape		= brush.Shape;
            var edgeIndices = controlMesh.Polygons[polygonIndex].EdgeIndices.ToArray();

            var deletePoints = new HashSet<short>();
            for (var e = edgeIndices.Length - 1; e >= 0; e--)
            {
                var halfEdgeIndex = edgeIndices[e];
                var twinEdgeIndex = controlMesh.GetTwinEdgeIndex(halfEdgeIndex);

                var vertexIndex1 = controlMesh.Edges[halfEdgeIndex].VertexIndex;
                var vertexIndex2 = controlMesh.Edges[twinEdgeIndex].VertexIndex;

                var vertex1 = controlMesh.Vertices[vertexIndex1];
                var vertex2 = controlMesh.Vertices[vertexIndex2];

                var newPoint = (vertex2 + vertex1) * 0.5f;

                // insert points on all edges, in between points
                InsertControlpoint(controlMesh, newPoint, halfEdgeIndex);

                deletePoints.Add(vertexIndex1);
                deletePoints.Add(vertexIndex2);
            }

            // remove all old edge points
            var controlPoints = deletePoints.ToArray();
            Array.Sort(controlPoints);
            RemoveControlPoints(controlMesh, shape, controlPoints);

            controlMesh.SetDirty();
            RebuildShape(brush);
        }

        public static void MergeHoverEdgePoints(CSGBrush brush, ControlMeshState controlMeshState, int edgeIndex)
        {
            var controlMesh = brush.ControlMesh;
            var shape = brush.Shape;
            var meshState = controlMeshState;

            var halfEdgeIndex = meshState.EdgeStateToHalfEdge[edgeIndex];
            var twinEdgeIndex = controlMesh.GetTwinEdgeIndex(halfEdgeIndex);

            var vertexIndex1 = controlMesh.Edges[halfEdgeIndex].VertexIndex;
            var vertexIndex2 = controlMesh.Edges[twinEdgeIndex].VertexIndex;

            var vertex1 = controlMesh.Vertices[vertexIndex1];
            var vertex2 = controlMesh.Vertices[vertexIndex2];

            var newPoint = (vertex2 + vertex1) * 0.5f;

            // insert points on all edges, in between points
            InsertControlpoint(controlMesh, newPoint, halfEdgeIndex);

            var controlPoints = vertexIndex1 > vertexIndex2 ? new [] { vertexIndex2 , vertexIndex1 } : new[] { vertexIndex1, vertexIndex2 };
            
            // remove all old edge points
            RemoveControlPoints(controlMesh, shape, controlPoints);
            controlMesh.SetDirty();

            RebuildShape(brush);
        }

        public static void MergeSelectedEdgePoints(CSGBrush[] brushes, ControlMeshState[] controlMeshStates)
        {
            if (brushes == null ||
                controlMeshStates == null)
                return;
            
            for (var b = 0; b < brushes.Length; b++)
            {
                var brush		= brushes[b];
                if (!brush ||
                    b >= controlMeshStates.Length)
                    continue;

                var meshState	= controlMeshStates[b];
                if (meshState == null)
                    continue;

                var controlMesh = brush.ControlMesh;
                var shape		= brush.Shape;

                var deletePoints = new HashSet<short>();
                for (var e = meshState.Selection.Edges.Length - 1; e >= 0; e--)
                {
                    if ((meshState.Selection.Edges[e] & SelectState.Selected) != SelectState.Selected)
                        continue;

                    var halfEdgeIndex = meshState.EdgeStateToHalfEdge[e];
                    var twinEdgeIndex = controlMesh.GetTwinEdgeIndex(halfEdgeIndex);

                    var vertexIndex1 = controlMesh.Edges[halfEdgeIndex].VertexIndex;
                    var vertexIndex2 = controlMesh.Edges[twinEdgeIndex].VertexIndex;

                    var vertex1 = controlMesh.Vertices[vertexIndex1];
                    var vertex2 = controlMesh.Vertices[vertexIndex2];

                    var newPoint = (vertex2 + vertex1) * 0.5f;

                    // insert points on all edges, in between points
                    InsertControlpoint(controlMesh, newPoint, halfEdgeIndex);

                    deletePoints.Add(vertexIndex1);
                    deletePoints.Add(vertexIndex2);
                }

                // remove all old edge points
                var controlPoints = deletePoints.ToArray();
                Array.Sort(controlPoints);
                RemoveControlPoints(controlMesh, shape, controlPoints);

                controlMesh.SetDirty();
                RebuildShape(brush);
            }
        }

        public static bool DeleteSelectedPoints(CSGBrush[] brushes, ControlMeshState[] controlMeshStates)
        {
            var modified = false;
            for (var t = brushes.Length - 1; t >= 0; t--)
            {
                var targetBrush         = brushes[t];
                var targetControlMesh   = targetBrush.ControlMesh;
                var targetShape         = targetBrush.Shape;
                var meshState           = controlMeshStates[t];

                var controlPoints       = meshState.GetSelectedPointIndices();

                if (controlPoints == null)
                    continue;

                var controlPointArray   = controlPoints.ToArray();
                Array.Sort(controlPointArray);
                RemoveControlPoints(targetControlMesh, targetShape, controlPointArray);
                if (targetShape.Surfaces.Length < 4)
                {
                    Undo.DestroyObjectImmediate(brushes[t].gameObject);
                    ArrayUtility.RemoveAt(ref brushes, t);
                    modified = true;
                } else
                {
                    targetControlMesh.SetDirty();
                    RebuildShape(targetBrush);
                }
            }
            return modified;
        }

    }
}
