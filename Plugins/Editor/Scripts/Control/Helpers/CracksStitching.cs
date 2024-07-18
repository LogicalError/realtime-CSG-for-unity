#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

// ReSharper disable CompareOfFloatsByEqualityOperator - I know what I am doing, all comparisons are on purpose and take into account precision issues.

namespace RealtimeCSG
{
    public static class CracksStitching
    {
        public const float DefaultMaxDist = 1e-08f;

        /// <summary>
        /// Stitch cracks and small holes in meshes, this can take a significant amount of time depending on the meshes
        /// </summary>
        /// <param name="meshes"> The meshes to process </param>
        /// <param name="maxDist"> The maximum distance to cover when stitching cracks, squared, holes larger than this will be left as is </param>
        /// <param name="debugger"> Object the process will dump debug information info, can be null </param>
        /// <param name="cancellationToken"> Token used to cancel this operation if required </param>
        /// <param name="scenes"> The scenes which will be dertied once the process is completed </param>
        /// <param name="progressName"> Name used for the asynchronous progress report </param>
        public static void RunAsync(IEnumerable<Mesh> meshes, float maxDist, Debugger? debugger, CancellationToken cancellationToken, Scene[] scenes, string progressName)
        {
	        var meshDefs = meshes.Select(x => new CracksStitching.MeshDef(x)).ToArray();
	        Task.Run(() =>
	        {
		        try
		        {
			        CracksStitching.Run(meshDefs, maxDist, debugger, cancellationToken, scenes, progressName);
		        }
		        catch (Exception e) when (e is not OperationCanceledException)
		        {
			        Debug.LogException(e);
		        }
	        }, cancellationToken);
        }

        static void Run(MeshDef[] meshes, float maxDist, Debugger? debugger, CancellationToken cancellationToken, Scene[] scenes, string progressName)
        {
            using var progress = new DisposableProgress(progressName);

            // Filter out edges that are part of a surface from those that are on the bounds of the surface or a hole in the surface
            // We do so by filtering edge whose vertex position are unique as a pair, meaning that their positions are only used by a single triangle

            var sharedPosPair = new HashSet<EdgeDef>(new UniquePosPairComparer());
            var uniquePosPair = new HashSet<EdgeDef>(new UniquePosPairComparer());
            (int, int)[] edges = new (int, int)[3];
            foreach (var mesh in meshes)
            {
                for (int ofBuffer = 0; ofBuffer < mesh.IndexBuffers.Length; ofBuffer++)
                {
                    var indexBuffer = mesh.IndexBuffers[ofBuffer];
                    for (int inBuffer = 0; inBuffer < indexBuffer.Count; inBuffer += 3)
                    {
                        edges[0] = (inBuffer, inBuffer+1);
                        edges[1] = (inBuffer+1, inBuffer+2);
                        edges[2] = (inBuffer+2, inBuffer);
                        foreach (var (aInBuffer, bInBuffer) in edges)
                        {
                            EdgeDef edge = new EdgeDef(aInBuffer, bInBuffer, mesh, ofBuffer);
                            if (sharedPosPair.Contains(edge))
                                continue;

                            if (uniquePosPair.Add(edge) == false) // Another edge registered itself before, those position pairs are no longer unique
                            {
                                uniquePosPair.Remove(edge);
                                sharedPosPair.Add(edge);
                            }
                        }
                    }

                    if (cancellationToken.IsCancellationRequested)
                        return;
                }
            }

            // Pre-compute best pairs for all edges

            {
	            var copy = uniquePosPair.ToArray();
	            for (int i = 0; i < copy.Length; i++)
	            {
		            var edgeA = copy[i];
		            for (int j = i+1; j < copy.Length; j++)
		            {
			            var edgeB = copy[j];
			            ComputeScore(edgeA, edgeB, out float score);
			            edgeA.TryUnsafeInsert(edgeB, score);
			            edgeB.TryUnsafeInsert(edgeA, score);
		            }
	            }
            }

            // Go through all edges, find the best pair,
            // create a quad between them, remove those edges from the collection,
            //  append the two new edges made by the bridge to the collection if necessary
            // repeat

            int initialUniquePosPair = uniquePosPair.Count;
            while (uniquePosPair.Count > 0)
            {
                EdgeDef? bestEdge = null;
                float bestEdgeScore = float.NegativeInfinity;
                foreach (var sourceEdge in uniquePosPair)
                {
                    if (sourceEdge.BestMatch.Score <= bestEdgeScore)
	                    continue;
					
                    while (uniquePosPair.Contains(sourceEdge.BestMatch.Edge) == false && uniquePosPair.Count > 1)
	                    sourceEdge.Evict(sourceEdge.BestMatch.Edge, uniquePosPair);
                    
                    if (sourceEdge.BestMatch.Edge.BestMatch.Edge == sourceEdge)
                    {
                        bestEdge = sourceEdge;
                        bestEdgeScore = sourceEdge.BestMatch.Score;
                    }
                }

                if (bestEdge is null)
                {
                    Debug.LogError("Failed to resolve further");
                    break;
                }

                Merge(bestEdge, bestEdge.BestMatch.Edge, uniquePosPair, sharedPosPair, maxDist, debugger);

                if (cancellationToken.IsCancellationRequested)
                    return;

                progress.Report(1f - (uniquePosPair.Count / initialUniquePosPair));
            }

            if (debugger is not null)
            {
                foreach (var edgeDef in uniquePosPair)
                {
                    debugger.DrawEdge(edgeDef, Color.red, 0f);
                }
            }

            foreach (var meshDef in meshes)
            {
                foreach (IGrouping<int,(int IndexBuffer, int Start)> grouping in meshDef.ToDiscard.GroupBy(x => x.IndexBuffer))
                {
                    var indexBuffer = meshDef.IndexBuffers[grouping.Key];
                    foreach ((_, int Start) in grouping.OrderByDescending(x => x.Start))
                    {
                        indexBuffer.RemoveRange(Start, 6);
                    }
                }
            }

            EditorApplication.CallbackFunction a = null!;
            a = () =>
            {
	            EditorApplication.update -= a;
	            ApplyMeshChanges(cancellationToken, meshes, scenes);
            };

            EditorApplication.update += a;
        }

        static void ApplyMeshChanges(CancellationToken cancellationToken, MeshDef[] meshes, Scene[] scenes)
        {
	        if (cancellationToken.IsCancellationRequested)
		        return;

	        foreach (var meshDef in meshes)
	        {
		        meshDef.Representation.SetVertices(meshDef.Pos);
		        meshDef.Representation.SetNormals(meshDef.Normals);
		        meshDef.Representation.SetTangents(meshDef.Tangents);
		        meshDef.Representation.SetUVs(0, meshDef.UV1);
		        meshDef.Representation.SetUVs(1, meshDef.UV2);

		        for (int i = 0; i < meshDef.IndexBuffers.Length; i++)
			        meshDef.Representation.SetTriangles(meshDef.IndexBuffers[i], i);
	        }

	        foreach (var scene in scenes)
		        EditorSceneManager.MarkSceneDirty(scene);
        }

        static void Merge(EdgeDef sourceEdge, EdgeDef targetEdge, HashSet<EdgeDef> uniquePosPair, HashSet<EdgeDef> sharedPosPair, float maxDist, Debugger? debugger)
        {
	        uniquePosPair.Remove(sourceEdge);
            uniquePosPair.Remove(targetEdge);
            sharedPosPair.Add(sourceEdge);
            sharedPosPair.Add(targetEdge);

            // Let's draw a quad to join those two edges

            int indexOppositeA;
            int indexOppositeB;
            var dist1 = Vector3.SqrMagnitude(targetEdge.A - sourceEdge.A) + Vector3.SqrMagnitude(targetEdge.B - sourceEdge.B);
            var dist2 = Vector3.SqrMagnitude(targetEdge.B - sourceEdge.A) + Vector3.SqrMagnitude(targetEdge.A - sourceEdge.B);
            bool swapIndex = dist1 > dist2; // Figure out which vertices of the edges should get connected when bridging them, a with other a or a with other b
            if (targetEdge.Mesh != sourceEdge.Mesh) // When the mesh don't match, we'll pick the opposite meshes' vertices and append them to the source's mesh
            {
                sourceEdge.Mesh.Pos.Add(targetEdge.Mesh.Pos[targetEdge.IndexA]);
                sourceEdge.Mesh.Pos.Add(targetEdge.Mesh.Pos[targetEdge.IndexB]);
                sourceEdge.Mesh.Normals.Add(targetEdge.Mesh.Normals[targetEdge.IndexA]);
                sourceEdge.Mesh.Normals.Add(targetEdge.Mesh.Normals[targetEdge.IndexB]);
                sourceEdge.Mesh.Tangents.Add(targetEdge.Mesh.Tangents[targetEdge.IndexA]);
                sourceEdge.Mesh.Tangents.Add(targetEdge.Mesh.Tangents[targetEdge.IndexB]);

                // Use stuff from source instead, the uvs of target may map to something widely different,
                // better to stretch the source uvs instead of setting wrong ones
                var sourceA = swapIndex ? sourceEdge.IndexB : sourceEdge.IndexA;
                var sourceB = swapIndex ? sourceEdge.IndexA : sourceEdge.IndexB;
                sourceEdge.Mesh.UV1.Add(sourceEdge.Mesh.UV1[sourceA]);
                sourceEdge.Mesh.UV1.Add(sourceEdge.Mesh.UV1[sourceB]);
                if (sourceEdge.Mesh.UV2.Count > 0)
                {
                    sourceEdge.Mesh.UV2.Add(sourceEdge.Mesh.UV2[sourceA]);
                    sourceEdge.Mesh.UV2.Add(sourceEdge.Mesh.UV2[sourceB]);
                }

                indexOppositeA = sourceEdge.Mesh.Pos.Count - 2;
                indexOppositeB = sourceEdge.Mesh.Pos.Count - 1;
            }
            else
            {
                indexOppositeA = targetEdge.IndexA;
                indexOppositeB = targetEdge.IndexB;
            }

            if (swapIndex)
            {
                (indexOppositeA, indexOppositeB) = (indexOppositeB, indexOppositeA);
            }

            var indexBuffer = sourceEdge.Mesh.IndexBuffers[sourceEdge.IndexBuffer];

            int baseIndex = indexBuffer.Count;
            indexBuffer.Add(0);
            indexBuffer.Add(0);
            indexBuffer.Add(0);

            indexBuffer.Add(0);
            indexBuffer.Add(0);
            indexBuffer.Add(0);

            ComputeScore(sourceEdge, targetEdge, out _, out float distanceA, out float distanceB);
            bool discard = distanceB > maxDist && distanceA > maxDist;
            if (discard)
                sourceEdge.Mesh.ToDiscard.Add((sourceEdge.IndexBuffer, baseIndex));

            // This nonsense is to ensure that winding is correct,
            //  When two triangles share the same edge, their winding passes through the two vertices they share in opposite direction,
            //  e.g.: for vertices {a,b,c,d} one triangle goes {a,b,c} the other goes {b,a,d}:
            //          A
            //        ↙╮┆╭←
            //      ↙  ↑┆↓  ↖
            //    ↙    ↑┆↓    ↖
            // B ╰┈→┈→┈╯┴╰→┈┈→┈╯ C
            //          D

            // Knowing that, we can figure out the winding for those two new triangles by copying the winding of the triangle the source edge is on

            int aOrder = sourceEdge.IndexAInBuffer % 3; // is 'a' the first, second or third element in the source triangle
            int bOrder = sourceEdge.IndexBInBuffer % 3; // is 'b' the first, second or third element in the source triangle
            int cOrder = 3 - (aOrder + bOrder); // Which spot has not been taken by the two others

            // Add the triangle that lays right against sourceEdge in opposite winding order ('2 - x' reverses winding) see comment above why
            indexBuffer[baseIndex + (2 - aOrder)] = sourceEdge.IndexA;
            indexBuffer[baseIndex + (2 - bOrder)] = sourceEdge.IndexB;
            indexBuffer[baseIndex + (2 - cOrder)] = indexOppositeB;

            // Add the other triangle in normal winding order
            indexBuffer[baseIndex + 3 + aOrder] = indexOppositeA;
            indexBuffer[baseIndex + 3 + bOrder] = indexOppositeB;
            indexBuffer[baseIndex + 3 + cOrder] = sourceEdge.IndexA;

            // Now that we created a bridge between the two edges, we need to add the two new edges we just created
            var aToOppositeA = new EdgeDef(baseIndex + 3 + aOrder, baseIndex + 3 + cOrder, sourceEdge.Mesh, sourceEdge.IndexBuffer);
            var bToOppositeB = new EdgeDef(baseIndex + (2 - bOrder), baseIndex + (2 - cOrder), sourceEdge.Mesh, sourceEdge.IndexBuffer);

            //             aToOppositeA
            //                  ↓
            //            A ┬───────┬ oppositeA
            //              │ ╲     │
            // sourceEdge → │   ╲   │ ← targetEdge
            //              │     ╲ │
            //            B ┴───────┴ oppositeB
            //                  ↑
            //            bToOppositeB

            if (debugger is not null)
            {
                if (discard)
                {
                    debugger.DrawEdge(sourceEdge, Color.red);
                    debugger.DrawEdge(targetEdge, Color.red);
                    debugger.DrawEdge(aToOppositeA, Color.red);
                    debugger.DrawEdge(bToOppositeB, Color.red);
                }
                else
                {
                    debugger.DrawEdge(sourceEdge, Color.blue);
                    debugger.DrawEdge(targetEdge, Color.green);
                    debugger.DrawEdge(aToOppositeA, Color.cyan);
                    debugger.DrawEdge(bToOppositeB, Color.cyan);
                }
            }

            if (aToOppositeA.A.Equals(aToOppositeA.B) == false && sharedPosPair.Contains(aToOppositeA) == false)
            {
                if (uniquePosPair.Add(aToOppositeA))
                {
                    foreach (var otherOtherCache in uniquePosPair)
                    {
	                    if (ReferenceEquals(aToOppositeA, otherOtherCache))
		                    continue;

	                    ComputeScore(aToOppositeA, otherOtherCache, out var score);
	                    aToOppositeA.TryUnsafeInsert(otherOtherCache, score);
	                    otherOtherCache.TryGuardedInsert(aToOppositeA, score);
                    }
                }
                else // It already exists, meaning that we just solved another edge at the same time as this one
                {
                    uniquePosPair.TryGetValue(aToOppositeA, out aToOppositeA); // may not map exactly and we need the exact ref for eviction
                    uniquePosPair.Remove(aToOppositeA);
                    sharedPosPair.Add(aToOppositeA);
                }
            }

            if (bToOppositeB.A.Equals(bToOppositeB.B) == false && sharedPosPair.Contains(bToOppositeB) == false)
            {
                if (uniquePosPair.Add(bToOppositeB))
                {
	                foreach (var otherOtherCache in uniquePosPair)
	                {
		                if (ReferenceEquals(bToOppositeB, otherOtherCache))
			                continue;

		                ComputeScore(bToOppositeB, otherOtherCache, out var score);
		                bToOppositeB.TryUnsafeInsert(otherOtherCache, score);
		                otherOtherCache.TryGuardedInsert(bToOppositeB, score);
	                }
                }
                else // It already exists, meaning that we just solved another edge at the same time as this one
                {
                    uniquePosPair.TryGetValue(bToOppositeB, out bToOppositeB); // may not map exactly and we need the exact ref for eviction
                    uniquePosPair.Remove(bToOppositeB);
                    sharedPosPair.Add(bToOppositeB);
                }
            }
        }

        static void ComputeScore(EdgeDef edge1, EdgeDef edge2, out float score)
        {
            ComputeScore(edge1, edge2, out float matchLength, out float distanceA, out float distanceB);
            // This is the most important line in the whole file
            // Measure the length of the match, the 1/x here to ensure the longer the match is the closer the difference is to zero
            score = matchLength;
            score /= 1f + (distanceA + distanceB); // Divide by projection distance, we don't want to prioritize edges that are similar but far away
        }

        static void ComputeScore(EdgeDef edge1, EdgeDef edge2, out float matchLength, out float distanceA, out float distanceB)
        {
            EdgeDef smallest;
            EdgeDef largest;
            if (edge1.AToB.sqrMagnitude > edge2.AToB.sqrMagnitude)
            {
                smallest = edge2;
                largest = edge1;
            }
            else
            {
                smallest = edge1;
                largest = edge2;
            }

            // This is the heart of the algorithm
            // Here we're computing how close the two edges match by removing the parts of the two segments that don't touch and measuring it
            // ──────     x
            //      ────  y
            //      ─     subSegment
            var subSegment = (a:largest.A, b:largest.B);
            ClipBetweenSegment(ref subSegment.a, smallest.A, smallest.AToB);
            ClipBetweenSegment(ref subSegment.b, smallest.A, smallest.AToB);

            var alongLargest = (a:smallest.A, b:smallest.B);
            ClipBetweenSegment(ref alongLargest.a, largest.A, largest.AToB);
            ClipBetweenSegment(ref alongLargest.b, largest.A, largest.AToB);

            matchLength = (subSegment.a - subSegment.b).sqrMagnitude;
            distanceA = (alongLargest.a - smallest.A).sqrMagnitude;
            distanceB = (alongLargest.b - smallest.B).sqrMagnitude;
        }

        /// <summary>
        /// Take a point and move it to the closest point on segment
        /// </summary>
        static void ClipBetweenSegment(ref Vector3 point, Vector3 segmentPos, Vector3 segmentDelta)
        {
            var delta = point - segmentPos;
            float dot = Vector3.Dot(delta, segmentDelta);
            if (dot < 0)
            {
                point = segmentPos;
            }
            else
            {
                float segDot = Vector3.Dot(segmentDelta, segmentDelta);
                Vector3 projection;
                if (segDot == 0f)
                    projection = Vector3.zero;
                else
                    projection = segmentDelta * (dot / segDot);

                if (projection.sqrMagnitude > segmentDelta.sqrMagnitude)
                    point = segmentPos + segmentDelta;
                else
                    point = segmentPos + projection;
            }
        }

        public class MeshDef
        {
            public readonly Mesh Representation;
            public readonly List<Vector3> Pos;
            public readonly List<Vector3> Normals;
            public readonly List<Vector4> Tangents;
            public readonly List<Vector2> UV1, UV2;
            public readonly List<int>[] IndexBuffers;
            public readonly List<(int IndexBuffer, int Start)> ToDiscard = new();

            public MeshDef(Mesh mesh)
            {
                Representation = mesh;
                var vertCount = mesh.vertexCount;
                Pos = new(vertCount);
                Normals = new(vertCount);
                Tangents = new(vertCount);
                UV1 = new(vertCount);
                UV2 = new(vertCount);
                mesh.GetVertices(Pos);
                mesh.GetNormals(Normals);
                mesh.GetTangents(Tangents);
                mesh.GetUVs(0, UV1);
                mesh.GetUVs(1, UV2);
                IndexBuffers = new List<int>[mesh.subMeshCount];
                for (int i = 0; i < mesh.subMeshCount; i++)
                {
                    var tris = new List<int>(vertCount);
                    mesh.GetTriangles(tris, i);
                    IndexBuffers[i] = tris;
                }
            }
        }

        public class EdgeDef
        {
            const int CacheSizeMax = 4;

            public readonly Vector3 A;
            public readonly Vector3 B;
            public readonly Vector3 AToB;
            public readonly int IndexA;
            public readonly int IndexB;
            public readonly int IndexBuffer;
            public readonly int IndexAInBuffer;
            public readonly int IndexBInBuffer;
            public readonly MeshDef Mesh;

            private readonly (EdgeDef Edge, float Score)[] BestMatches;

            private int _bestMatchesCount;

            /// <remarks>
            /// May not be entirely valid if the amount of edges in the pool
            /// is less than two or of nothing was happended to it yet
            /// </remarks>
            public (EdgeDef Edge, float Score) BestMatch => BestMatches[0];

            public EdgeDef(int indexAInBuffer, int indexBInBuffer, MeshDef mesh, int indexBuffer)
            {
	            BestMatches = new (EdgeDef Edge, float Score)[CacheSizeMax];
                var indexA = mesh.IndexBuffers[indexBuffer][indexAInBuffer];
                var indexB = mesh.IndexBuffers[indexBuffer][indexBInBuffer];
                Vector3 a = mesh.Pos[indexA];
                Vector3 b = mesh.Pos[indexB];

                IndexBuffer = indexBuffer;
                Mesh = mesh;

                bool ordered = false;
                if (a.x < b.x)
                {
                    ordered = true;
                }
                else if (a.x == b.x)
                {
                    if (a.y < b.y || (a.y == b.y && a.z < b.z))
                        ordered = true;
                }

                // Ordering to guarantee deterministic comparison and hashcode given the same pair but in different order
                // See UniquePosPairComparer for usage
                A = ordered ? a : b;
                B = ordered ? b : a;
                IndexA = ordered ? indexA : indexB;
                IndexB = ordered ? indexB : indexA;
                IndexAInBuffer = ordered ? indexAInBuffer : indexBInBuffer;
                IndexBInBuffer = ordered ? indexBInBuffer : indexAInBuffer;
                AToB = B - A;
            }

            /// <summary>
            /// Insert this edge in the cache, should only be used when rebuilding this edge's cache entirely
            /// </summary>
            public void TryUnsafeInsert(EdgeDef otherEdge, float score)
            {
	            if (_bestMatchesCount == 0)
	            {
		            BestMatches[_bestMatchesCount++] = (otherEdge, score);
		            return;
	            }
	            else if (score >= BestMatches[0].Score)
	            {
		            for (int i = Math.Min(_bestMatchesCount - 1, CacheSizeMax - 2); i >= 0; i--)
			            BestMatches[i + 1] = BestMatches[i];
		            _bestMatchesCount = Math.Min(_bestMatchesCount + 1, CacheSizeMax);
		            BestMatches[0] = (otherEdge, score);
	            }
	            else
	            {
		            for (int i = _bestMatchesCount - 1; i >= 0; i--)
		            {
			            if (score > BestMatches[i].Score)
				            continue;

			            if (i+1 == CacheSizeMax)
				            return;

			            for (int j = Math.Min(_bestMatchesCount - 1, CacheSizeMax - 2); j >= i + 1; j--)
				            BestMatches[j + 1] = BestMatches[j];
			            _bestMatchesCount = Math.Min(_bestMatchesCount + 1, CacheSizeMax);

			            BestMatches[i+1] = (otherEdge, score);
			            break;
		            }
	            }
            }

            /// <summary>
            /// Try to append a newly introduced edge into the cache
            /// </summary>
            public void TryGuardedInsert(EdgeDef otherEdge, float score)
            {
                // Here we have to be very careful, if the cache is not filled up, any empty slot just means that we have no clue which edge *should* be in that slot
                // We can't insert this new edges at an empty spot since it may be misleading, they may not actually be the fourth closest edge,
                // another one in the pool that was the sixth at the time may very well be the fourth now that a couple of the closest one were evicted.
                // We just haven't re-filled the cache since then as we didn't need to

                if (_bestMatchesCount == 0)
                    return;

                // Note the lack of 'if (BestEdges.Count == 0)', this wouldn't ever be hit given the if above

                // We can only insert in slots before the last *existing* value, not the one before the maximum amount of slots, see larger comment above
                if (score < BestMatches[_bestMatchesCount-1].Score)
                    return; // So exit if we're larger than the last value

                if (score >= BestMatches[0].Score)
                {
	                for (int i = Math.Min(_bestMatchesCount - 1, CacheSizeMax - 2); i >= 0; i--)
		                BestMatches[i + 1] = BestMatches[i];
	                _bestMatchesCount = Math.Min(_bestMatchesCount + 1, CacheSizeMax);
	                BestMatches[0] = (otherEdge, score);
                }
                else
                {
	                for (int i = _bestMatchesCount - 1; i >= 0; i--)
	                {
		                if (score > BestMatches[i].Score)
			                continue;

		                if (i + 1 == CacheSizeMax)
			                return;

		                for (int j = Math.Min(_bestMatchesCount - 1, CacheSizeMax - 2); j >= i + 1; j--)
			                BestMatches[j + 1] = BestMatches[j];
		                _bestMatchesCount = Math.Min(_bestMatchesCount + 1, CacheSizeMax);
			            
		                BestMatches[i + 1] = (otherEdge, score);
		                break;
	                }
                }
            }

            /// <summary>
            /// Evict an edge from the cache, potentially triggering the cache to be rebuilt
            /// </summary>
            public void Evict(EdgeDef edge, HashSet<EdgeDef> uniquePosPair)
            {
	            for (int i = 0; i < _bestMatchesCount; i++)
	            {
		            if (ReferenceEquals(BestMatches[i].Edge, edge))
		            {
			            for (int j = i; j < _bestMatchesCount-1; j++)
				            BestMatches[j] = BestMatches[j+1];
			            _bestMatchesCount--;
			            break;
		            }
	            }

	            if (_bestMatchesCount == 0)
	            {
		            foreach (var otherEdge in uniquePosPair)
		            {
			            if (ReferenceEquals(otherEdge, this))
				            continue;

			            ComputeScore(this, otherEdge, out var score);
			            TryUnsafeInsert(otherEdge, score);
		            }
	            }
            }
        }

        public class Debugger
        {
            public List<(Vector3 a, Vector3 b, Color color)> DebugLines = new();

            public void DrawEdge(EdgeDef edge, Color color, float offset = 0.01f)
            {
                var pos = edge.Mesh.Pos;
                var norms = edge.Mesh.Normals;
                var p0 = pos[edge.IndexA] + norms[edge.IndexA] * offset;
                var p1 = pos[edge.IndexB] + norms[edge.IndexB] * offset;
                DebugLines.Add((p0, p1, color));
            }
        }

        class UniquePosPairComparer : IEqualityComparer<EdgeDef>
        {
            public bool Equals(EdgeDef x, EdgeDef y)
            {
                return x.A.Equals(y.A) && x.B.Equals(y.B); // Using Vector3.Equals instead of '==' as the latter is not an exact equality, which we definitely want since we're resolving precision issues
            }

            public int GetHashCode(EdgeDef obj)
            {
                return HashCode.Combine(obj.A, obj.B);
            }
        }

        class DisposableProgress : IDisposable
        {
	        private int progressId;

	        public DisposableProgress(string name)
	        {
		        progressId = Progress.Start(name);
	        }

	        public void Report(float value)
	        {
		        Progress.Report(progressId, value);
	        }

	        public void Dispose()
	        {
		        Progress.Remove(progressId);
	        }
        }
    }
}