using System.Collections.Generic;
using UnityEngine.AI;
using UnityEngine;
using System.Linq;
using Rhinox.GUIUtils.Attributes;
using Rhinox.Lightspeed;
using Sirenix.OdinInspector;
using UnityEngine.Profiling;

namespace Rhinox.Utilities
{
    public class NavMeshVisualizer : MonoBehaviour
    {
        [Title("General Settings")]
        public bool AutoGenerate = false;

        [Tooltip("Removes edges that extend each other, assumption is made that this only happens inside the navmesh and that they are not edges.")]
        public bool RemoveExtendingEdges = true;
        public bool MergeExtendingEdges = false;

        [Title(" Rendering Settings")]
        public Material[] NavMeshMaterials;
        public Material BorderMaterial;
        public float BorderWidth = .2f;
        public float NavmeshTextureScale = 2f;
        public float BorderTextureScale = 2f;
        public bool ForceUpNormal = true;
        public float OffsetY = 0.0f;

        private NavMeshTriangulation _triangulation;
        private static EdgeComparer EdgeComparer = new EdgeComparer();

        private bool _renderersEnabled = true;
        
        [SerializeField, HideInInspector] private MeshObject _navMeshGraphicsObj;
        [SerializeField, HideInInspector] private MeshObject _borderMeshObj;

        private const string BORDER_MESH_NAME = "BorderMesh";
        private const string NAV_MESH_NAME = "Mesh";
        
        protected virtual void Awake()
        {
            _renderersEnabled = true;
            if (_navMeshGraphicsObj != null)
                _navMeshGraphicsObj.Initialize();
            if (_borderMeshObj != null)
                _borderMeshObj.Initialize();
            // SceneReadyHandler.YieldToggleControl(this);
        }

        protected virtual void OnEnable()
        {
            if (AutoGenerate)
                Generate();
        }

        protected virtual void OnDestroy()
        {
            if (_navMeshGraphicsObj != null)
            {
                Utility.Destroy(_navMeshGraphicsObj.gameObject);
                _navMeshGraphicsObj = null;
            }

            if (_borderMeshObj != null)
            {
                Utility.Destroy(_borderMeshObj.gameObject);
                _borderMeshObj = null;
            }
            
            // SceneReadyHandler.RevertToggleControl(this);
        }

        [Button("Generate Meshes")]
        public void Generate()
        {
            var oldNavMeshGraphic = _navMeshGraphicsObj;
            if (oldNavMeshGraphic != null)
                oldNavMeshGraphic.name = "TO BE DELETED"; // NOTE: GetOrCreate uses name
            var oldBorderMesh = _borderMeshObj;
            if (oldBorderMesh != null)
                oldBorderMesh.name = "TO BE DELETED";
            
            var navMesh = GenerateNavMesh();
            _navMeshGraphicsObj = MeshObject.GetOrCreateChild(transform, NAV_MESH_NAME, navMesh, NavMeshMaterials);
            _navMeshGraphicsObj.transform.SetPosition(y: OffsetY);
            _navMeshGraphicsObj.SetVisible(_renderersEnabled);
            
            // Profiler.BeginSample("Outer Edge Loops");

            var outerEdgeLoops = GetOuterEdgeLoops(navMesh, RemoveExtendingEdges, MergeExtendingEdges);
            
            // Profiler.EndSample();
            
            Profiler.BeginSample("Generate border mesh");

            var borderMesh = GenerateBorderMesh(outerEdgeLoops);
            
            Profiler.EndSample();
            
            _borderMeshObj = MeshObject.GetOrCreateChild(transform, BORDER_MESH_NAME, borderMesh, BorderMaterial);
            _borderMeshObj.transform.SetPosition(y: OffsetY);
            _borderMeshObj.SetVisible(_renderersEnabled);
            
            if (oldNavMeshGraphic != null)
                Utility.Destroy(oldNavMeshGraphic.gameObject);

            if (oldBorderMesh != null)
                Utility.Destroy(oldBorderMesh.gameObject);
            
            // Border test
            // foreach (var loop in outerEdgeLoops)
            // {
            //     var color = Random.ColorHSV();
            //     foreach (var e in loop)
            //         Debug.DrawLine(e.V1, e.V2, color, float.MaxValue);
            // }
        }

        [ButtonGroup, Button]
        protected void EnableRenderers()
        {
            _renderersEnabled = true;
            if (_navMeshGraphicsObj == null || _borderMeshObj == null)
                return;
            
            // Ensure global position is correct
            _navMeshGraphicsObj.transform.SetPosition(x: 0.0f, z: 0.0f);
            _navMeshGraphicsObj.transform.rotation = Quaternion.identity;
            _navMeshGraphicsObj.SetVisible(true);
            
            // Ensure global position is correct
            _borderMeshObj.transform.SetPosition(x: 0.0f, z: 0.0f);
            _borderMeshObj.transform.rotation = Quaternion.identity;
            _borderMeshObj.SetVisible(true);
        }

        [ButtonGroup, Button]
        protected void DisableRenderers()
        {
            _renderersEnabled = false;
            if (_navMeshGraphicsObj == null || _borderMeshObj == null)
                return;
            
            _navMeshGraphicsObj.SetVisible(false);
            _borderMeshObj.SetVisible(false);
        }

        private Mesh GenerateNavMesh()
        {
            _triangulation = NavMesh.CalculateTriangulation();

            return new Mesh()
            {
                name = "navmesh",
                vertices = _triangulation.vertices,
                triangles = _triangulation.indices,
                uv = CalculateUVs(_triangulation.vertices, _triangulation.indices, NavmeshTextureScale, ForceUpNormal)
            };
        }

        private Mesh GenerateBorderMesh(List<LinkedList<Edge>> borders)
        {
            List<Vector3> verticesList;
            List<int> indicesList;
            GenerateBorderMeshData(borders, out verticesList, out indicesList);

            var vertices = verticesList.ToArray();
            var indices = indicesList.ToArray();
            
            return new Mesh()
            {
                name = "bordermesh",
                vertices = vertices,
                triangles = indices,
                uv = CalculateUVs(vertices, indices, BorderTextureScale, ForceUpNormal)
            };
        }

        private void GenerateBorderMeshData(List<LinkedList<Edge>> borderLoop, out List<Vector3> vertices, out List<int> indices)
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

                    var currQuad = EdgeToQuad(currEdge, BorderWidth);

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
        private static List<Edge> GetEdges(Mesh mesh)
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

        private static IList<Edge> GetOuterEdges(Mesh mesh, bool removeExtending, bool mergeExtending)
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

                    if (!EdgeComparer.Equals(other, edge))
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
        
        private static void FilterExtendingEdges(List<Edge> edges, out List<Edge> extendingEdges, out List<Edge> mergedEdges)
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

        private static List<LinkedList<Edge>> GetOuterEdgeLoops(Mesh mesh, bool removeExtending, bool mergeExtending)
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

        /// ================================================================================================================
        /// DEBUG
        public enum DebugState
        {
            None,
            OuterEdges,
            OuterEdgeLoops,
            ShowExtendingEdges,
            PointsInRange,
            EdgesInRange
        }

        [FoldoutContainer("Debug")]
        [SerializeField] private DebugState _debugState;

        private bool _requiresDebugLocation => _debugState.EqualsOneOf(DebugState.EdgesInRange, DebugState.PointsInRange);
        
        [FoldoutContainer("Debug")]
        [ShowIf(nameof(_requiresDebugLocation))]
        [SerializeField] private Transform _debugLocation;
        
        [FoldoutContainer("Debug")]
        [ShowIf(nameof(_requiresDebugLocation))]
        [SerializeField] private float _debugRange;
        
        private void OnDrawGizmosSelected()
        {
            if (_debugState == DebugState.None) return;
            var mesh = GenerateNavMesh();
            switch (_debugState)
            {
                case DebugState.OuterEdgeLoops:
                {
                    var loops = GetOuterEdgeLoops(mesh, RemoveExtendingEdges, MergeExtendingEdges);
                    foreach (var edges in loops)
                    {
                        DrawEdges(edges.ToArray());
                    }

                    break;
                }

                case DebugState.OuterEdges:
                {
                    var edges = GetOuterEdges(mesh, RemoveExtendingEdges, MergeExtendingEdges);
                    DrawEdges(edges);

                    break;
                }

                case DebugState.ShowExtendingEdges:
                {
                    var edges = GetEdges(mesh);
                    FilterExtendingEdges(edges, out var extending, out _);
                    DrawEdges(extending);
                    
                    break;
                }
                
                case DebugState.PointsInRange:
                {
                    if (_debugLocation == null) return;
                    
                    var edges = GetOuterEdges(mesh, RemoveExtendingEdges, MergeExtendingEdges);
                    var refPoint = _debugLocation.position;
                    
                    Gizmos.DrawWireSphere(refPoint, _debugRange);

                    var edgesWithinRange = edges.Where(x => 
                        x.V1.LossyEquals(refPoint, _debugRange) ||
                        x.V2.LossyEquals(refPoint, _debugRange)
                    ).ToArray();
                    DrawEdges(edgesWithinRange);

                    break;
                }
                
                case DebugState.EdgesInRange:
                {
                    if (_debugLocation == null) return;
                    
                    
                    var edges = GetOuterEdges(mesh, RemoveExtendingEdges, MergeExtendingEdges);
                    var refPoint = _debugLocation.position;
                    
                    Gizmos.DrawWireSphere(refPoint, _debugRange);

                    var edgesWithinRange = edges.Where(x => 
                        x.V1.LossyEquals(refPoint, _debugRange) &&
                        x.V2.LossyEquals(refPoint, _debugRange)
                        ).ToArray();
                    DrawEdges(edgesWithinRange);

                    break;
                }
            }
        }

        private static void DrawEdges(IList<Edge> edges)
        {
            for (var i = 0; i < edges.Count; i++)
            {
                var edge = edges[i];
                var color = Color.white / edges.Count * i;
                color.a = 1;
                GizmosExt.DrawArrow(edge.V1, edge.V1.DirectionTo(edge.V2, false), color);
                Gizmos.DrawWireSphere(edge.V1, .02f);
                Gizmos.DrawWireSphere(edge.V2, .02f);
            }
        }
    }
}