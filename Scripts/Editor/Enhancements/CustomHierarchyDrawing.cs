using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Rhinox.GUIUtils.Editor;
using Rhinox.GUIUtils;
using Rhinox.Lightspeed;
using Rhinox.Utilities.Editor;

namespace Rhinox.Utilities
{
    [InitializeOnLoad]
    public static class CustomHierarchyDrawing
    {
        // add your components and the associated icons here
        public static Dictionary<Type, Texture> DefaultTypeIcons = new Dictionary<Type, Texture>()
        {
            {typeof(Light), UnityIcon.InternalIcon("Lighting")},
            {typeof(Camera), UnityIcon.InternalIcon("Camera Gizmo")},
            // ...
        };

        // cached game object information
        static Dictionary<int, GUIContent> _labeledObjects = new Dictionary<int, GUIContent>();
        static HashSet<int> _unlabeledObjects = new HashSet<int>();
        static GameObject[] _previousSelection = null; // used to update state on deselect

        // cached textures
        private static Texture2D _normalBackground = null;
        private static Texture2D _hoveredBackground = null;
        private static Texture2D _selectedBackground = null;

        // constants
        const int MAX_SELECTION_UPDATE_COUNT = 3; // how many objects we want to allow to get updated on select/deselect

        // set up all callbacks needed
        static CustomHierarchyDrawing()
        {
            EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchyGUI;
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;

            // callbacks for when we want to update the object GUI state:
            ObjectFactory.componentWasAdded -= HandleComponentAdded;
            ObjectFactory.componentWasAdded += HandleComponentAdded;

            // there's no componentWasRemoved callback, but we can use selection as a proxy:
            Selection.selectionChanged -= OnSelectionChanged;
            Selection.selectionChanged += OnSelectionChanged;


            EditorApplication.quitting -= OnEditorQuiting;
            EditorApplication.quitting += OnEditorQuiting;
        }

        private static void OnEditorQuiting()
        {
            EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchyGUI;
            ObjectFactory.componentWasAdded -= HandleComponentAdded;
            Selection.selectionChanged -= OnSelectionChanged;

            EditorApplication.quitting -= OnEditorQuiting;
        }

        private static void HandleComponentAdded(Component c)
        {
            UpdateObject(c.gameObject.GetInstanceID());
        }

        static void OnHierarchyGUI(int id, Rect rect)
        {
            if (!EditorUtilitiesSettings.Instance.EnableCustomIconsInHierarchy)
                return;

            if (_unlabeledObjects.Contains(id))
                return; // don't draw things with no component of interest

            if (ShouldDrawObject(id, out GUIContent icon))
            {
                var iconRect = rect.AlignLeft(18);
                DrawBackground(rect, iconRect, Selection.instanceIDs.Contains(id));
                GUI.Label(iconRect, icon);
            }
        }

        private static void DrawBackground(Rect hitboxRect, Rect iconRect, bool selected)
        {
            TryInitializeTextures();

            Texture2D t;
            if (selected)
            {
                t = _selectedBackground;
            }
            else if (hitboxRect.Contains(Event.current.mousePosition))
            {
                t = _hoveredBackground;
            }
            else
            {
                t = _normalBackground;
            }

            GUI.DrawTexture(iconRect, t, ScaleMode.StretchToFill);
        }

        private static void TryInitializeTextures()
        {
            if (_normalBackground == null)
            {
                _normalBackground = new Texture2D(1, 1);
                _normalBackground.SetPixel(1, 1, CustomGUIStyles.BoxBackgroundColor);
                _normalBackground.Apply();
            }

            if (_hoveredBackground == null)
            {
                _hoveredBackground = new Texture2D(1, 1);
                _hoveredBackground.SetPixel(1, 1, CustomGUIStyles.HoverColor);
                _hoveredBackground.Apply();
            }

            if (_selectedBackground == null)
            {
                _selectedBackground = new Texture2D(1, 1);
                _selectedBackground.SetPixel(1, 1, CustomGUIStyles.SelectedColor);
                _selectedBackground.Apply();
            }
        }

        static bool ShouldDrawObject(int id, out GUIContent icon)
        {
            if (_labeledObjects.TryGetValue(id, out icon))
                return true;
            // object is unsorted, add it and get icon, if applicable
            return SortObject(id, out icon);
        }

        static bool SortObject(int id, out GUIContent icon)
        {
            GameObject go = EditorUtility.InstanceIDToObject(id) as GameObject;
            if (go != null)
            {
                foreach (var entry in EditorUtilitiesSettings.Instance.Entries)
                {
                    if (!entry.IsValid)
                        continue;

                    if (go.GetComponent(entry.Type) && entry.PreviewIcon != null)
                    {
                        _labeledObjects.Add(id, icon = new GUIContent(entry.PreviewIcon));
                        return true;
                    }
                }
            }

            _unlabeledObjects.Add(id);
            icon = default;
            return false;
        }

        static void UpdateObject(int id)
        {
            _unlabeledObjects.Remove(id);
            _labeledObjects.Remove(id);
            SortObject(id, out _);
        }

        static void OnSelectionChanged()
        {
            TryUpdateObjects(_previousSelection); // update on deselect
            TryUpdateObjects(_previousSelection = Selection.gameObjects); // update on select
        }

        static void TryUpdateObjects(GameObject[] objects)
        {
            if (objects != null && objects.Length > 0 && objects.Length <= MAX_SELECTION_UPDATE_COUNT)
            {
                // max of three to prevent performance hitches when selecting many objects
                foreach (GameObject go in objects)
                {
                    UpdateObject(go.GetInstanceID());
                }
            }
        }
    }
}