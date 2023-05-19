using System;
using System.Collections.Generic;
using System.Linq;
using Rhinox.Lightspeed;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Profiling;

namespace Rhinox.Utilities
{
    public enum NavMeshSearchSettings
    {
        StaticObjects,
        NavMeshArea,
        AllMeshRenderers,
        StaticAndNavMeshAreas
    }
    
    public static class NavMeshHelper
    {
        private static EdgeComparer _edgeComparer = new EdgeComparer();
        
        //creates sample points inside the bounds (with a certain offset from the borders)
        //amount of sample points is increments²
        //checks if ALL these sample points fall on the navmesh, if not then return false.
        public static bool IsOnNavMesh(Bounds bounds, int mask = NavMesh.AllAreas, int increments = 4)
        {
            foreach (var pt in bounds.Sample2D(increments))
            {
                //lift sample point 0.5f above navmesh
                var liftedPoint = pt.With(y: pt.y + 0.5f);
            
                //use sample ray length of 1f
                if (NavMesh.SamplePosition(liftedPoint, out var hitResult, 1f, mask))
                {
                    // If the ray is longer than 0.5, then the sample point is not directly underneath
                    // This means the position is not on the navmesh but next to it
                    if (hitResult.distance > 0.5f)
                        return false;
                }
                else
                    return false;
            }

            return true;
        }

        // NOTE: this is only run-time
        public static bool BakeNavMesh(NavMeshSearchSettings searchSettings, int agentTypeID = 0, int includedLayerMask = ~0, NavMeshCollectGeometry collectMode = NavMeshCollectGeometry.RenderMeshes)
        {
            NavMeshBuildSettings navMeshBuildSettings = NavMesh.GetSettingsByID(agentTypeID);
            return BakeNavMesh(searchSettings, navMeshBuildSettings, includedLayerMask, collectMode);
        }
        
        // NOTE: this is only run-time
        public static bool BakeNavMesh(NavMeshSearchSettings searchSettings, NavMeshBuildSettings settings, int includedLayerMask = ~0, NavMeshCollectGeometry collectMode = NavMeshCollectGeometry.RenderMeshes)
        {
            Transform[] sources = null;
            // Search for objects
            switch (searchSettings)
            {
                case NavMeshSearchSettings.StaticObjects:
                    sources = FindMeshRenderers(true);
                    break;
                case NavMeshSearchSettings.AllMeshRenderers:
                    sources = FindMeshRenderers(false);
                    break;
                case NavMeshSearchSettings.NavMeshArea:
                    var navmeshAreas = UnityEngine.Object.FindObjectsOfType<NavMeshArea>();
                    sources = navmeshAreas.Select(x => x.transform).ToArray();
                    break;
                case NavMeshSearchSettings.StaticAndNavMeshAreas:
                    var navmeshAreas2 = UnityEngine.Object.FindObjectsOfType<NavMeshArea>();
                    sources = FindMeshRenderers(true)
                        .Union(navmeshAreas2.Select(x => x.transform))
                        .Distinct()
                        .ToArray();
                    break;
                default:
                    sources = Array.Empty<Transform>();
                    break;
            }
            
            // add all visible objects as a build source
            List<NavMeshBuildSource> navMeshSources = new List<NavMeshBuildSource>();
            foreach (var source in sources)
            {
                var subSources = new List<NavMeshBuildSource>();
                var markups = new List<NavMeshBuildMarkup>();
                markups.Add(new NavMeshBuildMarkup() {
                    overrideArea = false,
                    ignoreFromBuild = false,
                    root = source
                });

                NavMeshBuilder.CollectSources(source, includedLayerMask, collectMode, 0, markups, subSources);

                foreach (var src in subSources)
                    navMeshSources.AddUnique(src);
            }

            navMeshSources.Reverse();
            // build a navmesh and add it
            NavMeshData navData = NavMeshBuilder.BuildNavMeshData(settings, navMeshSources , new Bounds(Vector3.zero, new Vector3(5000, 5000, 5000)), Vector3.zero, Quaternion.identity);
            //NavMesh.RemoveAllNavMeshData();
            var meshData = NavMesh.AddNavMeshData(navData);
            bool isValid = meshData.valid;
            return isValid;
        }

        private static Transform[] FindMeshRenderers(bool onlyStatic = false)
        {
            var meshRenderers = UnityEngine.Object.FindObjectsOfType<MeshRenderer>();

            List<Transform> result = new List<Transform>();
            foreach (var meshRenderer in meshRenderers)
            {
                if (onlyStatic && !meshRenderer.gameObject.isStatic)
                    continue;

                var meshFilter = meshRenderer.GetComponent<MeshFilter>();
                if (meshFilter != null && !meshFilter.sharedMesh.isReadable)
                    continue;
                
                result.Add(meshRenderer.transform);
            }

            return result.ToArray();
        }
        
        
        public static Mesh GenerateNavMesh(string name = "navmesh", float textureScale = 1f, bool forceUpNormal = false, int areaMask = NavMesh.AllAreas)
        {
            var triangulation = CalculateTriangulation(areaMask);

            return new Mesh()
            {
                name = name,
                vertices = triangulation.vertices,
                triangles = triangulation.indices,
                uv = CalculateUVs(triangulation.vertices, triangulation.indices, textureScale, forceUpNormal)
            };
        }

        public static NavMeshTriangulation CalculateTriangulation(int areaMask = NavMesh.AllAreas)
        {
            var triangulation = NavMesh.CalculateTriangulation();

            if (areaMask == NavMesh.AllAreas)
                return triangulation;

            var filteredVerts = new List<Vector3>();
            var filteredIndices = new List<int>();
            var filteredAreas = new List<int>();
            for (var triangleIndex = 0; triangleIndex < triangulation.areas.Length; ++triangleIndex)
            {
                var areaIndex = triangulation.areas[triangleIndex];
                if ((areaMask & (1 << areaIndex)) == 0)
                    continue;
                
                for (int i = 0; i < 3; ++i)
                {
                    int vertIndex = (triangleIndex * 3) + i;
                    filteredIndices.Add(triangulation.indices[vertIndex]);
                }

                filteredAreas.Add(areaIndex);
            }

            var filteredVectorIndices = filteredIndices.Distinct().OrderBy(x => x).ToArray();
            foreach (var vectorI in filteredVectorIndices)
                filteredVerts.Add(triangulation.vertices[vectorI]);
            // Retarget indices in the new vertex range
            for (int i = 0; i < filteredIndices.Count; ++i)
            {
                var index = filteredIndices[i];
                var newIndex = filteredVectorIndices.IndexOf(index);
                filteredIndices[i] = newIndex;
            }

            var filteredTriangulation = new NavMeshTriangulation();
            filteredTriangulation.vertices = filteredVerts.ToArray();
            filteredTriangulation.indices = filteredIndices.ToArray();
            filteredTriangulation.areas = filteredAreas.ToArray();
            
            return filteredTriangulation;
        }

        public static Mesh GenerateBorderMesh(Mesh navMesh, float borderWidth, string name = "NavMesh - Border", 
            float textureScale = 1.0f, bool forceUpNormal = false, bool removeExtendingEdges = true)
        {
            // Profiler.BeginSample("Outer Edge Loops");

            var outerEdgeLoops = NavMeshHelper.GetOuterEdgeLoops(navMesh, removeExtendingEdges);
            
            // Profiler.EndSample();
            
            Profiler.BeginSample("Generate border mesh");

            var borderMesh = NavMeshHelper.GenerateBorderMesh(outerEdgeLoops, borderWidth, name, textureScale: textureScale, forceUpNormal: forceUpNormal);
            
            Profiler.EndSample();

            return borderMesh;
        }
        
        public static Mesh GenerateBorderMesh(List<LinkedList<Edge>> borders, float borderWidth, string name = "NavMesh - Border", float textureScale = 1.0f, bool forceUpNormal = false)
        {
            List<Vector3> verticesList;
            List<int> indicesList;
            GenerateBorderMeshData(borders, borderWidth, out verticesList, out indicesList);

            var vertices = verticesList.ToArray();
            var indices = indicesList.ToArray();
            
            return new Mesh()
            {
                name = name,
                vertices = vertices,
                triangles = indices,
                uv = CalculateUVs(vertices, indices, textureScale, forceUpNormal)
            };
        }

        public struct BoundsInformation
        {
            public int Area;
            public List<Bounds> ConvexDecomposedBounds;
        }

        public static List<BoundsInformation> GetNavMeshBounds(float height, int areaMask = NavMesh.AllAreas, float overlapMargin = 0.01f)
        {
            var triangulation = CalculateTriangulation(areaMask);
            return GetNavMeshBounds(triangulation, height, areaMask, overlapMargin);
        }

        public static List<BoundsInformation> GetNavMeshBounds(NavMeshTriangulation triangulation, float height, int areaMask = NavMesh.AllAreas, float overlapMargin = 0.01f)
        {
            var listOfBoundList = new List<BoundsInformation>();

            var edgeLoopsList = NavMeshHelper.GetOuterEdgeLoops(triangulation, true);
            foreach (var edgeLoop in edgeLoopsList)
            {
                if (edgeLoop.HasEdges)
                    continue;
                var rootBounds = new Bounds(edgeLoop.Edges.First.Value.V1, Vector3.zero);
                var slicingPlanes = new List<OrthogonalPlane>();
                foreach (var edge in edgeLoop.Edges)
                {
                    rootBounds.Encapsulate(edge.V1);
                    rootBounds.Encapsulate(edge.V2);
                    
                    var edgeDir = edge.V2 - edge.V1;
                    Vector3 nrml = Vector3.Cross(edgeDir.normalized, Vector3.up);

                    if (!nrml.TryGetCardinalAxis(out Axis cardinalAxis))
                        continue;

                    slicingPlanes.Add(new OrthogonalPlane(cardinalAxis, edge.V1));
                }

                rootBounds = rootBounds.Resize(Axis.Y, height, BoundsExtensions.Side.Negative);
                
                var boundsList = BoundsSubdivisionUtility.DivideAndMergeConcave(rootBounds, slicingPlanes,
                    x => !IsOnNavMesh(x, areaMask), overlapMargin);
                if (boundsList.Count == 0)
                    continue;

                var boundsInfo = new BoundsInformation()
                {
                    ConvexDecomposedBounds = boundsList,
                    Area = edgeLoop.Area
                };
                listOfBoundList.Add(boundsInfo);
            }

            return listOfBoundList;
        }

        private static void GenerateBorderMeshData(List<LinkedList<Edge>> borderLoop, float borderWidth, out List<Vector3> vertices, out List<int> indices)
        {
            var quads = new List<Quad>();
            vertices = new List<Vector3>();
            indices = new List<int>();

            foreach (var border in borderLoop)
            {
                Quad firstQuadInLoop = null;

                for (var e = border.First;; e = e.Next)
                {
                    var currEdge = e.Value;
                    if (currEdge.SqrLength < float.Epsilon)
                        continue;

                    var currQuad = EdgeToQuad(currEdge, borderWidth);

                    if (e != border.First)
                    {
                        var prevQuad = quads.Last();
                        ConnectQuadCorners(prevQuad, currQuad);
                    }

                    firstQuadInLoop = firstQuadInLoop ?? currQuad;
                    quads.Add(currQuad);

                    if (e.Next != null) continue;
                    ConnectQuadCorners(currQuad, firstQuadInLoop);
                    break;
                }
            }

            foreach (var quad in quads)
            {
                int i = vertices.Count;
                vertices.Add(quad.V1);
                vertices.Add(quad.V2);
                vertices.Add(quad.V3);
                vertices.Add(quad.V4);

                indices.Add(i + 2); // V3
                indices.Add(i + 0); // V1
                indices.Add(i + 3); // V4

                indices.Add(i + 0); // V1
                indices.Add(i + 1); // V2
                indices.Add(i + 3); // V4
            }
        }

        public class NavMeshEdgeLoop
        {
            public LinkedList<Edge> Edges;
            public int Area;

            public bool HasEdges => Edges != null && Edges.Count > 0;
        }
        
        public static List<NavMeshEdgeLoop> GetOuterEdgeLoops(NavMeshTriangulation triangulation, bool removeExtending)
        {
            var edgeLookup = GetOuterEdges(triangulation, removeExtending);

            var loops = new List<NavMeshEdgeLoop>();

            foreach (var area in edgeLookup.Keys)
            {
                var edgeStack = edgeLookup[area];
                var currEdge = edgeStack.FirstOrDefault();
                var currLoop = new LinkedList<Edge>();

                while (edgeStack.Count > 0)
                {
                    edgeStack.Remove(currEdge);

                    FetchNextEdge(edgeStack, currEdge, out Edge nextEdge);

                    if (nextEdge != null)
                    {
                        nextEdge = AlignEdge(currEdge, nextEdge);
                    }
                    else
                    {
                        // No more edges found, looking for a new loop
                        FetchNextEdge(currLoop, currEdge, out Edge startOfLoop);

                        if (startOfLoop != null)
                            currLoop.AddLast(currEdge);

                        if (currLoop.Count > 0)
                        {
                            var entry = new NavMeshEdgeLoop()
                            {
                                Edges = currLoop,
                                Area = area
                            };
                            loops.Add(entry);
                        }

                        if (edgeStack.Count > 0)
                        {
                            currLoop = new LinkedList<Edge>();
                            currEdge = edgeStack.FirstOrDefault();
                        }

                        continue;
                    }

                    currLoop.AddLast(currEdge);
                    currEdge = nextEdge;
                }
            }

            return loops;
        }

        public static List<LinkedList<Edge>> GetOuterEdgeLoops(Mesh mesh, bool removeExtending)
        {
            var edgeStack = GetOuterEdges(mesh, removeExtending);

            var currEdge = edgeStack.FirstOrDefault();
            var currLoop = new LinkedList<Edge>();
            var loops = new List<LinkedList<Edge>>();

            while (edgeStack.Count > 0)
            {
                edgeStack.Remove(currEdge);
                
                FetchNextEdge(edgeStack, currEdge, out Edge nextEdge);

                if (nextEdge != null)
                {
                    nextEdge = AlignEdge(currEdge, nextEdge);
                }
                else
                {
                    // No more edges found, looking for a new loop
                    FetchNextEdge(currLoop, currEdge, out Edge startOfLoop);

                    if (startOfLoop != null)
                        currLoop.AddLast(currEdge);

                    if (currLoop.Count > 0)
                        loops.Add(currLoop);

                    if (edgeStack.Count > 0)
                    {
                        currLoop = new LinkedList<Edge>();
                        currEdge = edgeStack.FirstOrDefault();
                    }

                    continue;
                }

                currLoop.AddLast(currEdge);
                currEdge = nextEdge;
            }

            // Profiler.EndSample();

            return loops;
        }
        
        private static Quad EdgeToQuad(Edge edge, float width)
        {
            var halfWidth = width / 2f;
            Vector3 normal = Vector3.up; // NOTE: always use up, since nav mesh edge will never be created vertically
            var side = Vector3.Cross(normal, edge.V2 - edge.V1);
            side.Normalize();

            return new Quad
            (
                edge.V1 + side * halfWidth,
                edge.V1 + side * -halfWidth,
                edge.V2 + side * halfWidth,
                edge.V2 + side * -halfWidth
            );
        }

        private static void ConnectQuadCorners(Quad prevQuad, Quad currQuad)
        {
            var i1 = Utility.GetApproximateIntersection
            (
                prevQuad.V4, prevQuad.V4 - prevQuad.V2,
                currQuad.V2, currQuad.V2 - currQuad.V4
            );

            var i2 = Utility.GetApproximateIntersection
            (
                prevQuad.V3, prevQuad.V3 - prevQuad.V1,
                currQuad.V1, currQuad.V1 - currQuad.V3
            );

            if (!i1.AnyIsNaN())
            {
                currQuad.V2 = i1;
                prevQuad.V4 = i1;
            }

            if (!i2.AnyIsNaN())
            {
                currQuad.V1 = i2;
                prevQuad.V3 = i2;
            }
        }
        
        internal static List<Edge> GetEdges(Vector3[] vertices, int[] triangles)
        {
            var edges = new List<Edge>();
            for (var i = 0; i < triangles.Length; i += 3)
            {
                var v1 = vertices[triangles[i + 0]];
                var v2 = vertices[triangles[i + 1]];
                var v3 = vertices[triangles[i + 2]];

                // NOTE:
                // Only need to check one cross product, since if one of the two is zero it will return null
                // and if they are non-null and overlap the result is also zero
                if (!HasTriangleArea(v2 - v1, v3 - v1))
                    continue;

                edges.Add(new Edge(v1, v2));
                edges.Add(new Edge(v2, v3));
                edges.Add(new Edge(v3, v1));
            }

            return edges;
        }
        
        private static Dictionary<int, List<Edge>> GetEdgesByArea(Vector3[] vertices, int[] triangles, int[] areas)
        {
            var result = new Dictionary<int, List<Edge>>();
            for (var i = 0; i < triangles.Length; i += 3)
            {
                var v1 = vertices[triangles[i + 0]];
                var v2 = vertices[triangles[i + 1]];
                var v3 = vertices[triangles[i + 2]];
                
                // NOTE:
                // Only need to check one cross product, since if one of the two is zero it will return null
                // and if they are non-null and overlap the result is also zero
                if (!HasTriangleArea(v2 - v1, v3 - v1))
                    continue;
                
                var area = areas[triangles[i + 0]];
                if (!result.ContainsKey(area))
                    result.Add(area, new List<Edge>());

                var edges = result[area];
                edges.Add(new Edge(v1, v2));
                edges.Add(new Edge(v2, v3));
                edges.Add(new Edge(v3, v1));
            }

            return result;
        }

        private static bool HasTriangleArea(Vector3 edgeA, Vector3 edgeB)
        {
            // Note: Triangle Area = | V_12 x V_13 | / 2
            // Simplified if (V_12 x V_13).sqrmagnitude = 0 -> The triangle area = 0 
            float triangleArea = Vector3.Cross((edgeA), (edgeB)).sqrMagnitude;
            return triangleArea > float.Epsilon;
        }

        public static IList<Edge> GetOuterEdges(Mesh mesh, bool removeExtending)
        {
            var edges = GetEdges(mesh.vertices, mesh.triangles);
            
            var resultSet = FilterOverlappingEdges(edges);
            
            // Try to group edges with the same direction
            // Sometimes edges may be split in the middle and only used once thus causing it to be seen as an edge
            // NOTE: Assumption is made that extending edge are not part of the edge, this can be incorrect!
            if (removeExtending)
            {
                FilterExtendingEdges(ref resultSet, out _, out var mergedEdges);
                resultSet.AddRange(mergedEdges);
            }

            resultSet = FilterOverlappingEdges(resultSet);

            return resultSet;
        }
        
        private static Dictionary<int, List<Edge>> GetOuterEdges(NavMeshTriangulation triangulation, bool removeExtending)
        {
            var edges = GetEdgesByArea(triangulation.vertices, triangulation.indices, triangulation.areas);
            foreach (var area in edges.Keys)
            {
                var resultSet = FilterOverlappingEdges(edges[area]);

                // Try to group edges with the same direction
                // Sometimes edges may be split in the middle and only used once thus causing it to be seen as an edge
                // NOTE: Assumption is made that extending edge are not part of the edge, this can be incorrect!
                if (removeExtending)
                {
                    FilterExtendingEdges(ref resultSet, out _, out var mergedEdges);
                    resultSet.AddRange(mergedEdges);
                }

                resultSet = FilterOverlappingEdges(resultSet);
                edges[area] = resultSet;
            }

            return edges;
        }

        private static List<Edge> FilterOverlappingEdges(List<Edge> edges)
        {
            List<Edge> parsedSet = new List<Edge>();
            List<Edge> resultSet = new List<Edge>();

            // Detect the outer edges by checking how many times each edge is used
            // If it is used multiple times, it is not an outer edge
            foreach (var edge in edges)
            {
                bool foundDuplicate = false;

                for (int i = 0; i < parsedSet.Count; ++i)
                {
                    var other = parsedSet[i];

                    if (!_edgeComparer.Equals(other, edge))
                        continue;

                    resultSet.Remove(other);

                    foundDuplicate = true;
                    break;
                }

                if (!foundDuplicate && edge.SqrLength > float.Epsilon)
                    resultSet.Add(edge);

                if (!foundDuplicate)
                    parsedSet.Add(edge);
            }

            return resultSet;
        }
        
        internal static void FilterExtendingEdges(ref List<Edge> edges, out List<Edge> extendingEdges, out List<Edge> mergedEdges)
        {
            mergedEdges = new List<Edge>();
            extendingEdges = new List<Edge>();
            
            var edgesByDirection = edges
                .GroupBy(x => x.V1.DirectionTo(x.V2))
                .Where(x => x.Count() > 1)
                .Select(x => x.ToArray())
                .ToArray();

            foreach (var edgeGroup in edgesByDirection)
            {
                for (int i = edgeGroup.Length - 1; i >= 0; --i)
                {
                    var edge = edgeGroup[i];
                    if (extendingEdges.Contains(edge))
                        continue;
                    
                    for (int j = i - 1; j >= 0; --j)
                    {
                        var other = edgeGroup[j];
                        if (extendingEdges.Contains(other))
                            continue;
                        
                        var connectPoint = EdgeComparer.GetEdgePointConnectedToEdge(edge, other);

                        if (connectPoint < 0)
                            continue;

                        // move old edges to extending set
                        if (edges.Remove(edge))
                            extendingEdges.Add(edge);
                        
                        if (edges.Remove(other))
                            extendingEdges.Add(other);
                        
                        // add merged version
                        if (connectPoint == 0)
                        {
                            if (!EdgeComparer.EdgesAlign(other, edge))
                                edge.Flip();
                            mergedEdges.Add(new Edge(other.V1, edge.V2));
                        }
                        else
                        {
                            if (!EdgeComparer.EdgesAlign(edge, other))
                                other.Flip();
                            mergedEdges.Add(new Edge(edge.V1, other.V2));
                        }

                        // NOTE: we break the outer loop, because this method currently cannot recursively merge mergedEdges
                        // This already improves the complexity somewhat (one loop per edgegroup, so we're gonna leave it like is)
                        i = -1; // stop middle loop, move to next edgeGroup
                        break;
                        // END NOTE: Do not touch this, unless you refactor the entire method
                    }
                }
            }
        }

        private static Edge AlignEdge(Edge edge, Edge other)
        {
            if (!EdgeComparer.EdgesAlign(edge, other))
                other.Flip();

            return other;
        }
        
        private static void FetchNextEdge(ICollection<Edge> edges, Edge curr, out Edge next)
        {
            foreach (var edge in edges)
            {
                if (!EdgeComparer.EdgeConnectsToPoint(curr.V2, edge)) continue;

                next = edge;
                return;
            }

            next = null;
        }
        
        private static Vector2[] CalculateUVs(Vector3[] vertices, int[] indices, float textureScale = 1f, bool forceUpNormal = false)
        {
            var uvs = new Vector2[vertices.Length];
            for (var i = 0; i < indices.Length; i += 3)
            {
                var v1 = vertices[indices[i + 0]];
                var v2 = vertices[indices[i + 1]];
                var v3 = vertices[indices[i + 2]];

                var normal = forceUpNormal ? Vector3.up : Vector3.Cross(v3 - v1, v2 - v1);
                var rotation = normal.magnitude <= 0.001f
                    ? Quaternion.identity
                    : Quaternion.Inverse(Quaternion.LookRotation(normal));

                uvs[indices[i + 0]] = (Vector2) (rotation * v1) / textureScale;
                uvs[indices[i + 1]] = (Vector2) (rotation * v2) / textureScale;
                uvs[indices[i + 2]] = (Vector2) (rotation * v3) / textureScale;
            }

            return uvs;
        }
    }
}