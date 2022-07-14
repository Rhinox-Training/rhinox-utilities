
using System;
using System.Collections.Generic;
using System.Linq;
using Rhinox.Lightspeed;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Profiling;
using UnityEngine.AI;
using UnityEngine;
using System.Linq;
using Rhinox.GUIUtils.Attributes;
using Rhinox.Lightspeed;
using Sirenix.OdinInspector;
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
        
        public static bool UpdateNavMesh(NavMeshSearchSettings searchSettings, int agentTypeID = 0, int includedLayerMask = ~0, NavMeshCollectGeometry collectMode = NavMeshCollectGeometry.RenderMeshes)
        {
            NavMeshBuildSettings navMeshBuildSettings = NavMesh.GetSettingsByID(agentTypeID);
            return UpdateNavMesh(searchSettings, navMeshBuildSettings, includedLayerMask, collectMode);
        }
        
        public static bool UpdateNavMesh(NavMeshSearchSettings searchSettings, NavMeshBuildSettings settings, int includedLayerMask = ~0, NavMeshCollectGeometry collectMode = NavMeshCollectGeometry.RenderMeshes)
        {
            Transform[] sources = null;
            // Search for objects
            switch (searchSettings)
            {
                case NavMeshSearchSettings.StaticObjects:
                    var mrs1 = UnityEngine.Object.FindObjectsOfType<MeshRenderer>();
                    sources = mrs1.Where(x => x.gameObject.isStatic).Select(x => x.transform).ToArray();
                    break;
                case NavMeshSearchSettings.AllMeshRenderers:
                    var mrs2 = UnityEngine.Object.FindObjectsOfType<MeshRenderer>();
                    sources = mrs2.Select(x => x.transform).ToArray();
                    break;
                case NavMeshSearchSettings.NavMeshArea:
                    var navmeshAreas = UnityEngine.Object.FindObjectsOfType<NavMeshArea>();
                    sources = navmeshAreas.Select(x => x.transform).ToArray();
                    break;
                case NavMeshSearchSettings.StaticAndNavMeshAreas:
                    var mrs3 = UnityEngine.Object.FindObjectsOfType<MeshRenderer>();
                    var navmeshAreas2 = UnityEngine.Object.FindObjectsOfType<NavMeshArea>();
                    sources = mrs3.Where(x => x.gameObject.isStatic)
                        .Select(x => x.transform)
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

                navMeshSources.AddRange(subSources);
            }

            // build a navmesh and add it
            NavMeshData navData = NavMeshBuilder.BuildNavMeshData(settings, navMeshSources, new Bounds(), Vector3.zero, Quaternion.identity);
            NavMesh.RemoveAllNavMeshData();
            var meshData = NavMesh.AddNavMeshData(navData);
            bool isValid = meshData.valid;
            return isValid;
        }
        
        
        public static Mesh GenerateNavMesh(string name = "navmesh", float textureScale = 1f, bool forceUpNormal = false)
        {
            var triangulation = NavMesh.CalculateTriangulation();

            return new Mesh()
            {
                name = name,
                vertices = triangulation.vertices,
                triangles = triangulation.indices,
                uv = CalculateUVs(triangulation.vertices, triangulation.indices, textureScale, forceUpNormal)
            };
        }

        public static Mesh GenerateBorderMesh(Mesh navMesh, float borderWidth, string name = "NavMesh - Border", 
            float textureScale = 1.0f, bool forceUpNormal = false, bool removeExtendingEdges = true, bool mergeExtendingEdges = true)
        {
            // Profiler.BeginSample("Outer Edge Loops");

            var outerEdgeLoops = NavMeshHelper.GetOuterEdgeLoops(navMesh, removeExtendingEdges, mergeExtendingEdges);
            
            // Profiler.EndSample();
            
            Profiler.BeginSample("Generate border mesh");

            var borderMesh = NavMeshHelper.GenerateBorderMesh(outerEdgeLoops, borderWidth, name, textureScale: textureScale, forceUpNormal: forceUpNormal);
            
            Profiler.EndSample();

            return borderMesh;
        }
        
        private static Mesh GenerateBorderMesh(List<LinkedList<Edge>> borders, float borderWidth, string name = "NavMesh - Border", float textureScale = 1.0f, bool forceUpNormal = false)
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
        
        internal static List<LinkedList<Edge>> GetOuterEdgeLoops(Mesh mesh, bool removeExtending, bool mergeExtending)
        {
            var edgeStack = GetOuterEdges(mesh, removeExtending, mergeExtending);

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
            var normal = Vector3.Cross(edge.V1, edge.V2).Abs();
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
        public static List<Edge> GetEdges(Mesh mesh)
        {
            var edges = new List<Edge>();
            var indices = mesh.triangles;
            var verts = mesh.vertices;
            for (var i = 0; i < indices.Length; i += 3)
            {
                var v1 = verts[indices[i + 0]];
                var v2 = verts[indices[i + 1]];
                var v3 = verts[indices[i + 2]];

                edges.Add(new Edge(v1, v2));
                edges.Add(new Edge(v2, v3));
                edges.Add(new Edge(v3, v1));
            }

            return edges;
        }

        internal static IList<Edge> GetOuterEdges(Mesh mesh, bool removeExtending, bool mergeExtending)
        {
            var edges = GetEdges(mesh);
            
            // Try to group edges with the same direction
            // Sometimes edges may be split in the middle and only used once thus causing it to be seen as an edge
            // NOTE: Assumption is made that extending edge are not part of the edge, this can be incorrect!
            if (mergeExtending || removeExtending)
            {
                FilterExtendingEdges(edges, out _, out var mergedEdges);
                if (mergeExtending)
                    edges.AddRange(mergedEdges);
            }

            var resultSet = FilterOverlappingEdges(edges);

            
            
            return resultSet;
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
        
        internal static void FilterExtendingEdges(List<Edge> edges, out List<Edge> extendingEdges, out List<Edge> mergedEdges)
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
                    for (int j = i - 1; j >= 0; --j)
                    {
                        var other = edgeGroup[j];
                        var connectPoint = EdgeComparer.GetEdgePointConnectedToEdge(edge, other);

                        if (connectPoint < 0)
                            continue;

                        // remove old edges
                        edges.Remove(edge);
                        edges.Remove(other);
                        
                        extendingEdges.Add(edge);
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

                        i = -1; // stop outer loop
                        break;
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