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
            
            var navMesh = NavMeshHelper.GenerateNavMesh(textureScale: NavmeshTextureScale, forceUpNormal: ForceUpNormal);
            _navMeshGraphicsObj = MeshObject.GetOrCreateChild(transform, NAV_MESH_NAME, navMesh, NavMeshMaterials);
            _navMeshGraphicsObj.transform.SetPosition(y: OffsetY);
            _navMeshGraphicsObj.SetVisible(_renderersEnabled);
            
            var borderMesh = NavMeshHelper.GenerateBorderMesh(navMesh, BorderWidth, textureScale: BorderTextureScale, forceUpNormal: ForceUpNormal, 
                removeExtendingEdges: RemoveExtendingEdges);
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

        public void ToggleVisibility(bool state)
        {
            if (_renderersEnabled == state)
                return;
            
            if (state)
                EnableRenderers();
            else
                DisableRenderers();
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
            var mesh = NavMeshHelper.GenerateNavMesh();
            switch (_debugState)
            {
                case DebugState.OuterEdgeLoops:
                {
                    var loops = NavMeshHelper.GetOuterEdgeLoops(mesh, RemoveExtendingEdges);
                    foreach (var edges in loops)
                    {
                        DrawEdges(edges.ToArray());
                    }

                    break;
                }

                case DebugState.OuterEdges:
                {
                    var edges = NavMeshHelper.GetOuterEdges(mesh, RemoveExtendingEdges);
                    DrawEdges(edges);

                    break;
                }

                case DebugState.ShowExtendingEdges:
                {
                    var edges = NavMeshHelper.GetEdges(mesh);
                    NavMeshHelper.FilterExtendingEdges(ref edges, out var extending, out _);
                    DrawEdges(extending);
                    
                    break;
                }
                
                case DebugState.PointsInRange:
                {
                    if (_debugLocation == null) return;
                    
                    var edges = NavMeshHelper.GetOuterEdges(mesh, RemoveExtendingEdges);
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
                    
                    
                    var edges = NavMeshHelper.GetOuterEdges(mesh, RemoveExtendingEdges);
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