using System;
using System.Collections.Generic;
using System.Linq;
using Rhinox.GUIUtils;
using Rhinox.GUIUtils.Editor;
using Rhinox.Lightspeed;
using Rhinox.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Rhinox.Utilities.Editor
{
    internal class PolyCounter : CustomSceneOverlayWindow<PolyCounter>
    {
        private const string _menuItemPath = WindowHelper.ToolsPrefix + "Show Polygon info";
        protected override string Name => "Polygon info";

        private static PersistentValue<bool> IncludeChildren;
        private static PersistentValue<bool> IncludeDisabled;
        private static PersistentValue<bool> IncludeLODs;
        
        private static List<Mesh> _meshes;
        private static bool _requiresRefresh;

        private static string _meshInfo;
        private static string _secondaryMeshInfo;
        
        [MenuItem(_menuItemPath, false, -199)]
        public static void SetupWindow() => Window.Setup();

        [MenuItem(_menuItemPath, true)]
        public static bool SetupValidateWindow() => Window.HandleValidateWindow();

        protected override void Initialize()
        {
            IncludeChildren = PersistentValue<bool>.Create(typeof(PolyCounter), nameof(IncludeChildren), true);
            IncludeDisabled = PersistentValue<bool>.Create(typeof(PolyCounter), nameof(IncludeDisabled), false);
            IncludeLODs = PersistentValue<bool>.Create(typeof(PolyCounter), nameof(IncludeLODs), false);

            _meshes = new List<Mesh>();
            _requiresRefresh = true;
            
            base.Initialize();
        }

        protected override void Setup()
        {
            _requiresRefresh = true;
            base.Setup();
        }

        protected override void OnSelectionChanged()
        {
            _requiresRefresh = true;
        }

        protected override void OnBeforeDraw()
        {
            base.OnBeforeDraw();
            
            if (_requiresRefresh)
                RefreshInfo();
        }

        protected override void OnGUI()
        {
            if (IncludeChildren.ShowField("Include Children"))
                _requiresRefresh = true;
            if (IncludeDisabled.ShowField("Include Disabled"))
                _requiresRefresh = true;
            if (IncludeLODs.ShowField("Include LODs"))
                _requiresRefresh = true;
            
            if (_meshes.Any())
            {
                CustomEditorGUI.HorizontalLine(CustomGUIStyles.BorderColor);
            
                GUILayout.Label(_meshInfo, CustomGUIStyles.BoldLabelCentered);
                GUILayout.Label(_secondaryMeshInfo, CustomGUIStyles.MiniLabel);
            }
        }

        private static void RefreshInfo()
        {
            _meshes.Clear();

            // Unique set of filters; due to include children & actually selecting children, might have some duplicates otherwise
            HashSet<Renderer> renderers = new HashSet<Renderer>();

            foreach (var go in Selection.gameObjects)
            {
                if (IncludeChildren)
                    renderers.AddRange(go.GetComponentsInChildren<Renderer>());
                else
                    renderers.Add(go.GetComponent<Renderer>());
            }

            if (!IncludeLODs)
            {
                var lodGroups = Object.FindObjectsOfType<LODGroup>();
                foreach (var lodGroup in lodGroups)
                {
                    var lods = lodGroup.GetLODs();
                    // Skip the first LOD; remove the others
                    for (var i = 1; i < lods.Length; i++)
                        renderers.RemoveRange(lods[i].renderers);
                }
            }

            foreach (var renderer in renderers)
                AddRenderer(renderer);
            
            var vertices = _meshes.Sum(x => x.vertices.Length);
            var triangles = _meshes.Sum(x => x.triangles.Length / 3);
            var submeshes = _meshes.Sum(x => x.subMeshCount);
            var uniqueMeshes = _meshes.Distinct().Count();

            _meshInfo = $"{triangles:N0} tris; {vertices:N0} verts";
            if (uniqueMeshes == _meshes.Count)
                _secondaryMeshInfo = $"{_meshes.Count:N0} meshes; {submeshes:N0} submeshes";
            else
                _secondaryMeshInfo = $"{_meshes.Count:N0} meshes ({uniqueMeshes:N0} unique); {submeshes:N0} submeshes";
            
            _requiresRefresh = false;
        }

        private static void AddRenderer(Renderer renderer)
        {
            if (renderer == null) return;
            
            if (renderer is SkinnedMeshRenderer skinnedMeshRenderer)
                _meshes.Add(skinnedMeshRenderer.sharedMesh);

            
            var filter = renderer.GetComponent<MeshFilter>();
            
            if (filter == null) return;

            var mesh = filter.sharedMesh;
            
            if (mesh == null) return;

            if (!IncludeDisabled && !renderer.enabled)
                return;
            
            _meshes.Add(mesh);
        }
        
        protected override string GetMenuPath() => _menuItemPath;
    }
}