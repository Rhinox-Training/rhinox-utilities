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
    internal static class PolyCounter
    {
        private static bool _active;

        private const string _menuItemPath = "Tools/Show Polygon info";

        private static PersistentUserValue<bool> IncludeChildren;
        private static PersistentUserValue<bool> IncludeDisabled;
        private static PersistentUserValue<bool> IncludeLODs;
        
        private static List<Mesh> _meshes;
        private static bool _requiresRefresh;

        private static string _meshInfo;
        private static string _secondaryMeshInfo;

        [MenuItem(_menuItemPath, false, 100)]
        public static void ActivateSizeVisualizer()
        {
            IncludeChildren = PersistentUserValue<bool>.Get(typeof(PolyCounter), nameof(IncludeChildren), true);
            IncludeDisabled = PersistentUserValue<bool>.Get(typeof(PolyCounter), nameof(IncludeDisabled), false);
            IncludeLODs = PersistentUserValue<bool>.Get(typeof(PolyCounter), nameof(IncludeLODs), false);
            _meshes = new List<Mesh>();

            if (!_active)
            {
                Utility.SubscribeToSceneGui(ShowSceneGUI);
                Selection.selectionChanged += OnSelectionChanged;
                _active = true;
            }
            else
            {
                Utility.UnsubscribeFromSceneGui(ShowSceneGUI);
                Selection.selectionChanged -= OnSelectionChanged;
                _active = false;
            }

            _requiresRefresh = true;
        }

        private static void OnSelectionChanged()
        {
            _requiresRefresh = true;
        }

        [MenuItem(_menuItemPath, true)]
        public static bool IsActive()
        {
            Menu.SetChecked(_menuItemPath, _active);
            return true; // returns whether it is clickable
        }

        private static void ShowSceneGUI(SceneView sceneview)
        {
            if (sceneview.camera == null) return;

            if (_requiresRefresh)
                RefreshInfo();
            
            SceneOverlay.AddWindow("Polygon Info", SizeVisualizerOptionsFunc);
        }

        private static void SizeVisualizerOptionsFunc(Object target, SceneView sceneview)
        {
            EditorPreferenceToggle("Include Children", IncludeChildren);
            EditorPreferenceToggle("Include Disabled", IncludeDisabled);
            EditorPreferenceToggle("Include LODs", IncludeLODs);
            
            if (_meshes.Any())
            {
                CustomEditorGUI.HorizontalLine(CustomGUIStyles.BorderColor);
            
                GUILayout.Label(_meshInfo, CustomGUIStyles.BoldLabelCentered);
                GUILayout.Label(_secondaryMeshInfo, CustomGUIStyles.MiniLabel);
            }
        }

        private static void EditorPreferenceToggle(string label, PersistentUserValue<bool> context)
        {
            var value = context.Value;
            var newValue = EditorGUILayout.Toggle(label, value);
            if (value != newValue)
            {
                context.Value = newValue;
                _requiresRefresh = true;
            }
        }

        private static void RefreshInfo()
        {
            _meshes.Clear();

            // Unique set of filters; due to include children & actually selecting children, might have some duplicates otherwise
            HashSet<Renderer> renderers = new HashSet<Renderer>();

            foreach (var go in Selection.gameObjects)
            {
                if (IncludeChildren.Value)
                    renderers.AddRange(go.GetComponentsInChildren<MeshRenderer>());
                else
                    renderers.Add(go.GetComponent<MeshRenderer>());
            }

            if (!IncludeLODs.Value)
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

            _meshInfo = $"{triangles:N0} tris; {vertices:N0} verts";
            _secondaryMeshInfo = $"{_meshes.Count:N0} meshes; {submeshes:N0} submeshes";
            
            _requiresRefresh = false;
        }

        private static void AddRenderer(Renderer renderer)
        {
            if (renderer == null) return;
            
            var filter = renderer.GetComponent<MeshFilter>();
            
            if (filter == null) return;

            var mesh = filter.sharedMesh;
            
            if (mesh == null) return;

            if (!IncludeDisabled.Value && !renderer.enabled)
                return;
            
            _meshes.Add(mesh);
        }
    }
}