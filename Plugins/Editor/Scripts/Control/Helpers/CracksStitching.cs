// I know what I'm doing when comparing floats.
// ReSharper disable CompareOfFloatsByEqualityOperator

// Debug purposes, provides ability to step through the procedure, to inspect and visualize data between each steps
//#define YIELD_SUBSTEPS

namespace RealtimeCSG
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using UnityEditor;
    using UnityEngine;
    using static System.Math;
    using Vector3 = UnityEngine.Vector3;

    public static class CracksStitching
    {
        /// <summary>
        /// Stitch cracks and small holes in meshes, this can take a significant amount of time depending on the mesh.
        /// </summary>
        /// <param name="mesh"> The mesh </param>
        /// <param name="debug"> An object which will receive debug information </param>
        /// <param name="maxDist">The maximum distance to cover when stitching cracks, larger than this will not be stitched</param>
        public static void Solve(Mesh mesh, ISolverDebugProvider debug = null, float maxDist = 0.05f)
        {
            var subMeshes = new List<int>[mesh.subMeshCount];
            for (int i = 0; i < mesh.subMeshCount; i++)
                subMeshes[i] = mesh.GetTriangles(i).ToList();

            foreach (var o in SolveRaw(mesh.vertices, mesh.triangles, subMeshes, debug, maxDist)){ }

            var totalIndices = 0;
            foreach (var list in subMeshes)
                totalIndices += list.Count;
            
            if(totalIndices > ushort.MaxValue)
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            for (int i = 0; i < subMeshes.Length; i++)
                mesh.SetTriangles(subMeshes[i], i);
        }

        /// <summary>
        /// Stitch cracks and small holes in meshes, this can take a significant amount of time depending on the mesh.
        /// </summary>
        /// <param name="vertices">All vertices of the mesh</param>
        /// <param name="tris">All geometry indices of the mesh</param>
        /// <param name="subMeshes">Submeshes geometry indices</param>
        /// <param name="debug"> An object which will receive debug information </param>
        /// <param name="maxDist"> The maximum distance to cover when stitching cracks, larger than this will not be stitched </param>
        /// <param name="pCancellationToken"> Optional cancellation token </param>
        /// <returns> Yield while solving if the preprocessor has been enabled, otherwise returns empty </returns>
        public static IEnumerable SolveRaw(Vector3[] vertices, int[] tris, List<int>[] subMeshes, ISolverDebugProvider debug, float maxDist = 0.05f, CancellationToken pCancellationToken = default)
        {
            // Merging duplicate vertices to ignore material-specific topology
            Merge(vertices, out var newVertices, ref tris);
            
            pCancellationToken.ThrowIfCancellationRequested();
            
            var nativeVertices = vertices;
            vertices = newVertices;

            var allEdges = new HashSet<EdgeId>();
            var sharedEdges = new HashSet<EdgeId>();
            
            // We're thinking of cracks as edges that do not have multiple triangles 
            for (int i = 0; i < tris.Length; i+=3)
            {
                int x = tris[i], y = tris[i + 1], z = tris[i + 2];
                
                var edgeA = new EdgeId(x, y);
                var edgeB = new EdgeId(y, z);
                var edgeC = new EdgeId(z, x);
                
                if (allEdges.Add(edgeA) == false)
                    sharedEdges.Add(edgeA);
                if(allEdges.Add(edgeB) == false)
                    sharedEdges.Add(edgeB);
                if(allEdges.Add(edgeC) == false)
                    sharedEdges.Add(edgeC);
            }
            
            pCancellationToken.ThrowIfCancellationRequested();
            
            // Only keep edges which do not share multiple triangles
            var leftToSolve = new HashSet<EdgeId>(allEdges);
            foreach (var edge in sharedEdges)
                leftToSolve.Remove(edge);

            var trianglesToAdd = new List<(int a, int b, int c)>();
            var workingData = new WorkingData(trianglesToAdd, allEdges, leftToSolve, vertices);
            
            debug?.HookIntoWorkingData(workingData);

            while (leftToSolve.Count > 0)
            {
                pCancellationToken.ThrowIfCancellationRequested();
                
                // Take one random edge from the hashset
                EdgeId thisEdge;
                using (var e = leftToSolve.GetEnumerator())
                {
                    e.MoveNext();
                    thisEdge = e.Current;
                }

                if (ReturnBestMatchFor(ref thisEdge, ref workingData, out var bestMatch) == false || bestMatch.dist > maxDist)
                {
                    if(bestMatch.dist <= maxDist)
                        throw new InvalidOperationException($"Stray edge ({thisEdge}) could not be solved for");
                    
                    debug?.LogWarning($"For edge {thisEdge}, closest match ({bestMatch.edge}) did not satisfy {nameof(maxDist)} constraint {bestMatch.dist}/{maxDist}");
                    leftToSolve.Remove(thisEdge);
                    continue;
                }

                // Found a best match for thisEdge but let's check that they are both best matches for each other
                int swapCount = 0;
                do
                {
                    pCancellationToken.ThrowIfCancellationRequested();
                    
                    ReturnBestMatchFor(ref bestMatch.edge, ref workingData, out var otherBestMatch);
                    if (otherBestMatch.dist > maxDist)
                    {
                        debug?.LogWarning($"For edge {bestMatch.edge}, closest match ({otherBestMatch.edge}) did not satisfy {nameof(maxDist)} constraint {otherBestMatch.dist}/{maxDist}");
                        leftToSolve.Remove(bestMatch.edge);
                        break;
                    }

                    if (otherBestMatch.edge == thisEdge || swapCount > 10)
                    {
                        if (swapCount > 10)
                        {
                            // swapCount prevents very unlikely infinite loop with weird edge topology in
                            // cases where X's best match is Y but Y's is Z which itself is X
                            debug?.LogWarning($"Sub par match for {thisEdge} -> {otherBestMatch.edge}");
                        }

                        // Both edges are each other's best match
                        leftToSolve.Remove(thisEdge);
                        leftToSolve.Remove(bestMatch.edge);
                        
                        int index = trianglesToAdd.Count;
                        CreateTriangles(ref workingData, ref thisEdge, ref bestMatch.edge);
                        debug?.Log($"{thisEdge} -> {bestMatch.edge}: {trianglesToAdd[index]} {(trianglesToAdd.Count - index > 1 ? trianglesToAdd[index+1].ToString() : "")}");
                        #if YIELD_SUBSTEPS
                        yield return null;
                        #endif
                        break;
                    }
                    else
                    {
                        // They don't match, try to see if this new best match matches each other instead
                        thisEdge = bestMatch.edge;
                        bestMatch = otherBestMatch;
                        swapCount++;
                        #if YIELD_SUBSTEPS
                        yield return null;
                        #endif
                        continue;
                    }
                } while (true);
            }

            var posToSubMesh = new Dictionary<Vector3, (int submesh, int vertIndex)>();
            for (int subMeshIndex = 0; subMeshIndex < subMeshes.Length; subMeshIndex++)
            {
                var subMesh = subMeshes[subMeshIndex];
                for (int i = 0; i < subMesh.Count; i++)
                {
                    var subMeshVertIndex = subMesh[i];
                    var pos = nativeVertices[subMeshVertIndex];
                    // Multiple assignment on the same key would matter
                    // only for large holes next to multiple materials,
                    // we do not expect cracks to be large enough to warrant solving for this.
                    posToSubMesh[pos] = (subMeshIndex, subMeshVertIndex);
                }
            }
            
            pCancellationToken.ThrowIfCancellationRequested();

            foreach (var (x, y, z) in trianglesToAdd)
            {
                var (subMeshIndex, mappingA) = posToSubMesh[vertices[x]];
                var (           _, mappingB) = posToSubMesh[vertices[y]];
                var (           _, mappingC) = posToSubMesh[vertices[z]];
                
                // Effectively randomly picking subMesh, i.e.: material, those triangles will be assigned to, see comment above
                var indices = subMeshes[subMeshIndex];
                
                // Not sure yet how to properly solve winding order, add both sides for now
                indices.Add(mappingA);
                indices.Add(mappingB);
                indices.Add(mappingC);
                indices.Add(mappingC);
                indices.Add(mappingB);
                indices.Add(mappingA);
            }
            
            pCancellationToken.ThrowIfCancellationRequested();
            
            #if !YIELD_SUBSTEPS
                return Array.Empty<System.Object>();
            #endif
        }

        /// <summary> Duplicate positions are stripped and indices are remapped appropriately </summary>
        static void Merge(Vector3[] positions, out Vector3[] outPos, ref int[] indices)
        {
            var newPos = new List<Vector3>();
            var posToIndex = new Dictionary<Vector3, int>();
            for (int i = 0; i < indices.Length; i++)
            {
                var oldIndex = indices[i];
                var pos = positions[oldIndex];
                if (posToIndex.TryGetValue(pos, out var newIndex) == false)
                {
                    newIndex = newPos.Count;
                    newPos.Add(pos);
                    posToIndex.Add(pos, newIndex);
                }

                indices[i] = newIndex;
            }

            outPos = newPos.ToArray();
        }

        /// <summary> Create bridge between the given edges, append those new triangles and edges to working data </summary>
        static void CreateTriangles(ref WorkingData data, ref EdgeId edgeAB, ref EdgeId edgeXY)
        {
            (int a, int b) = edgeAB;
            (int x, int y) = edgeXY;
        
            (int a, int b, int x, int dupe)? isTri = null;
            // Do those two edge share a vertex
            if (x == a || x == b)
                isTri = (a, b, y, x);
            else if (y == a || y == b)
                isTri = (a, b, x, y);
            
            if (isTri.HasValue)
            {
                var tri = isTri.Value;
                data.tris.Add((tri.a, tri.b, tri.x));
                
                var newEdge = tri.dupe == a ? new EdgeId(b, tri.x) : new EdgeId(a, tri.x);
                
                // Created a triangle from two existing edges, the third one formed by them must now be added to the pool to be solved for
                if (data.allEdges.Add(newEdge))
                    data.edgesLeft.Add(newEdge);
                else
                    data.edgesLeft.Remove(newEdge);
            }
            else
            {
                var vertices = data.vertices;
                var pivot = Vector3.Dot((vertices[b] - vertices[a]).normalized, (vertices[y] - vertices[x]).normalized) >= 0 ? b : a;
                
                data.tris.Add((a, b, x));
                data.tris.Add((x, pivot, y));

                EdgeId newEdge0, newEdge1;
                if (pivot == b)
                {
                    newEdge0 = new EdgeId(a, x);
                    newEdge1 = new EdgeId(b, y);
                }
                else
                {
                    newEdge0 = new EdgeId(a, y);
                    newEdge1 = new EdgeId(b, x);
                }
                
                // Created a quad to bridge those two edges, two new edges were formed through this process and
                // must now be added to the pool to be solved for.
                if (data.allEdges.Add(newEdge0))
                    data.edgesLeft.Add(newEdge0);
                else            
                    data.edgesLeft.Remove(newEdge0);
                if (data.allEdges.Add(newEdge1))
                    data.edgesLeft.Add(newEdge1);
                else
                    data.edgesLeft.Remove(newEdge1);
            }
        }
        
        static bool ReturnBestMatchFor(ref EdgeId edge, ref WorkingData data, out (float dist, EdgeId edge, Segment seg) output)
        {
            var vertices = data.vertices;
            var edgesLeft = data.edgesLeft;
            
            // Prevents testing edge against itself on every iteration of the loop, will be added back lower
            edgesLeft.Remove(edge);
            
            var seg = new Segment(vertices[edge.a], vertices[edge.b]);
            (float dist, EdgeId edge, Segment seg) closest = (float.PositiveInfinity, default, default);
            foreach (var otherEdge in edgesLeft)
            {
                var otherSeg = new Segment(vertices[otherEdge.a], vertices[otherEdge.b]);
                ComputeScoreFor(ref seg, ref otherSeg, out var dist);
                if (dist < closest.dist)
                    closest = (dist, otherEdge, otherSeg);
            }

            edgesLeft.Add(edge);

            output = closest;
            return closest.dist != float.PositiveInfinity;
        }

        static void ComputeScoreFor(ref Segment segX, ref Segment segY, out float score)
        {
            if (segX.lengthSqr == 0f)
            {
                // segX is a point
                if (segY.lengthSqr == 0f)
                {
                    // Both segments are points
                    score = (segX.a - segY.a).sqrMagnitude;
                }
                else
                {
                    // segX is a point and segY a segment
                    var aOnSegB = Vector3.Dot(segX.a - segY.a, segY.delta) / segY.lengthSqr;
                    score = (segX.a - (segY.a + segY.delta * aOnSegB)).sqrMagnitude;
                }
                return;
            }
            else if (segY.lengthSqr == 0f)
            {
                // Swap segments and let recursion handle this
                ComputeScoreFor(ref segY, ref segX, out score);
                return;
            }

            // From here on out, both segments are guaranteed to have a length above zero

            if (segX.lengthSqr > segY.lengthSqr)
                ComputeScoreInner(ref segX, ref segY, out score);
            else
                ComputeScoreInner(ref segY, ref segX, out score);
        }
        
        /// <summary> segX must be longer than segY, swap them if they aren't ! </summary>
        static void ComputeScoreInner(ref Segment segX, ref Segment segY, out float score)
        {
            // this method operates knowing that segY is smaller than segX

            // Find closest point on segmentX from both edges of segmentY
            // ... now computing factor along segmentX
            var fA = Vector3.Dot(segY.a - segX.a, segX.delta) / segX.lengthSqr;
            var fB = Vector3.Dot(segY.b - segX.a, segX.delta) / segX.lengthSqr;
            // factor may be outside [0,1], meaning that the closest point is outside of the segment along its line
            var fCA = Mathf.Clamp01(fA);
            var fCB = Mathf.Clamp01(fB);
            if (fCA == fA || fCB == fB)
            {
                // At least one of the closest pos is on segmentX
                // Project them both back onto segmentY and find the differences to derive a score
                // hinting to how skewed the resulting quads/tris bridging those segments would be.
                // This came mostly through intuition, even if this is flawed, the score is
                // not nearly as important as validating that both segments are each other's best match.
                
                var aOnX = segX.a + segX.delta * fA;
                var aBack = segY.a + segY.delta * Mathf.Clamp01(Vector3.Dot(aOnX - segY.a, segY.delta) / segY.lengthSqr);
                var bOnX = segX.a + segX.delta * fB;
                var bBack = segY.a + segY.delta * Mathf.Clamp01(Vector3.Dot(bOnX - segY.a, segY.delta) / segY.lengthSqr);
                
                // Projection to projection distance is rated lower than projection back to vertex
                // this way edges slightly further away but parallel are preferred over those perpendicular to each other
                score = ((aOnX - aBack).sqrMagnitude + (bOnX - bBack).sqrMagnitude) * 0.5f
                        + (segY.a - aBack).sqrMagnitude + (segY.b - bBack).sqrMagnitude;
                score *= 0.5f;
            }
            else
            {
                // The segments do not share a plane in common, return distance from edges
                score = (segX.a - segY.a).sqrMagnitude +
                        (segX.b - segY.b).sqrMagnitude;
                score *= 0.5f;
            }
        }

        /// <summary> Provides hooks into the stitching procedure for debug purposes </summary>
        public interface ISolverDebugProvider
        {
            /// <summary>
            /// Provides a way to hook into the data the solver is working with,
            /// to read while the solver yields for example.
            /// Writing to those collections will lead to undefined behaviors.
            /// </summary>
            void HookIntoWorkingData(WorkingData data);
            void LogWarning(string str);
            void Log(string str);
        }

        /// <summary> Deterministic identity for an edge </summary>
        public readonly struct EdgeId : IEquatable<EdgeId>
        {
            /// <summary>
            /// <see cref="a"/> is guaranteed to be smaller than <see cref="b"/>,
            /// they are indices to the vertex position buffer.
            /// </summary>
            public readonly int a, b;

            public EdgeId(int x, int y)
            {
                a = Min(x, y);
                b = Max(x, y);
            }

            public void Deconstruct(out int oA, out int oB)
            {
                oA = a;
                oB = b;
            }

            public static bool operator ==(EdgeId x, EdgeId y) => x.a == y.a && x.b == y.b;
            public static bool operator !=(EdgeId x, EdgeId y) => x.a != y.a || x.b != y.b;

            public bool Equals(EdgeId other) => a == other.a && b == other.b;
            public override bool Equals(object obj) => obj is EdgeId other && Equals(other);
            public override int GetHashCode() => (a, b).GetHashCode();
            public override string ToString() => (a, b).ToString();
        }

        public readonly struct WorkingData
        {
            public readonly List<(int, int, int)> tris;
            public readonly HashSet<EdgeId> allEdges;
            public readonly HashSet<EdgeId> edgesLeft;
            public readonly Vector3[] vertices;

            public WorkingData(
                List<(int, int, int)> pTris, 
                HashSet<EdgeId> pAllEdges, 
                HashSet<EdgeId> pEdgesLeft, 
                Vector3[] pVertices)
            {
                tris = pTris;
                allEdges = pAllEdges;
                edgesLeft = pEdgesLeft;
                vertices = pVertices;
            }
        }

        readonly struct Segment
        {
            public readonly Vector3 a, b, delta;
            public readonly float lengthSqr;

            public Segment(Vector3 pA, Vector3 pB)
            {
                a = pA;
                b = pB;
                delta = pB - pA;
                lengthSqr = delta.sqrMagnitude;
            }
        }

        public static void SolveAsync(Mesh[] pMesh, ISolverDebugProvider debug, CancellationToken cancellationToken, Action onFinished, float maxDist = 0.05f)
        {
            Mesh combinedMesh = new Mesh();

            var combineInstances = new List<CombineInstance>();
            foreach (var mesh1 in pMesh)
                for (int i = 0; i < mesh1.subMeshCount; i++)
                    combineInstances.Add(new CombineInstance{ mesh = mesh1, subMeshIndex = i });
            
            combinedMesh.CombineMeshes(combineInstances.ToArray(), false, false);

            var verts = combinedMesh.vertices;
            var indices = combinedMesh.triangles;
            
            var subMeshes = new List<int>[combinedMesh.subMeshCount];
            for (int i = 0; i < combinedMesh.subMeshCount; i++)
                subMeshes[i] = combinedMesh.GetTriangles(i).ToList();
            
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    foreach (var o in SolveRaw(verts, indices, subMeshes, debug, maxDist)){ }

                    cancellationToken.ThrowIfCancellationRequested();
                        
                    var totalIndices = 0;
                    foreach (var list in subMeshes)
                        totalIndices += list.Count;
                    
                    // Mesh is not thread safe, we can run the process asynchronously as long as we don't directly interact with mesh.
                    //   to that end we're relying on the editor update callback to apply those changes back, but inline delegates cannot remove
                    //   themselves from a callback -> using a class to hold the delegate reference which removes itself after the call to work around this.
                    var jobWorkAround = new AsyncJobWorkaround();
                    EditorApplication.update += jobWorkAround.Post = () =>
                    {
                        EditorApplication.update -= jobWorkAround.Post;
                        if(cancellationToken.IsCancellationRequested)
                            return;
            
                        if(totalIndices > ushort.MaxValue)
                            combinedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

                        for (int i = 0; i < subMeshes.Length; i++)
                            combinedMesh.SetTriangles(subMeshes[i], i);

                        // Redistribute data to the right meshes
                        int submeshIndex = 0;
                        foreach (var mesh1 in pMesh)
                        {
                            var ci = new CombineInstance[mesh1.subMeshCount];
                            mesh1.Clear(); 
                            for (int i = 0; i < ci.Length; i++)
                                ci[i] = new CombineInstance { mesh = combinedMesh, subMeshIndex = submeshIndex++ };
                            mesh1.CombineMeshes(ci, false, false);
                            // CombineMeshes dumps all vertex data from all referenced meshes into the resulting mesh
                            // even if most of the vertex data ends up unused because those vertices' are not referenced in the index/triangle array
                            mesh1.OptimizeReorderVertexBuffer();
                        }

                        onFinished?.Invoke();
                    };
                }
                catch (Exception e) when(e is OperationCanceledException == false)
                {
                    Debug.LogException(e);
                }
            }, cancellationToken);
        }

        class AsyncJobWorkaround
        {
            public EditorApplication.CallbackFunction Post;
        }
    }
}