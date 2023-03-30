using System;
using System.Linq;
using System.Reflection;
using Rhinox.Lightspeed;
using Rhinox.GUIUtils;
using Rhinox.GUIUtils.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Rhinox.Utilities.Editor
{
    [InitializeOnLoad]
    internal static class SelectionCounter
    {
        private static bool _isDrawn;

        private static GUIStyle _localStyle => CustomGUIStyles.MiniLabelRight;
        private static GUIStyle _globalStyle => CustomGUIStyles.MiniLabelLeft;

        private static string _globalTooltip = "Amount selected | Children";
        
        private static GameObject _currentObject;
        
        static SelectionCounter()
        {
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.hierarchyWindowItemOnGUI += DrawSelectionCounter;
            Utility.SubscribeToSceneGui(OnSceneGUI);
        }

        private static void OnEditorUpdate()
        {
            _isDrawn = false;
        }
        
        private static void OnSceneGUI(SceneView obj)
        {
            if (!UtilitiesEditorSettings.Instance.ShowSelectionInfoInSceneOverlay)
                return;
            
            if (Selection.transforms.Length > 0)
                SceneOverlay.AddWindow("Selected", DrawGlobalHierarchyCounter);
        }

        static void DrawSelectionCounter(int instanceID, Rect selectionRect)
        {
            if (!UtilitiesEditorSettings.Instance.ShowSelectionInfoInHierarchy)
                return;
            
            if (!Selection.instanceIDs.Contains(instanceID)) return;

            _currentObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;

            if (_currentObject == null) return;

            DrawItemHierarchyCounter(selectionRect);
        }

        private static void DrawGlobalHierarchyCounter(Object o, SceneView view)
        {
            var count = Selection.transforms.Length;
            if (count == 0) return;
            
            GUILayout.Label($"{count} Object(s)", _globalStyle);
            
            var childCount = Selection.GetFiltered<Transform>(SelectionMode.Deep).Length;

            
            if (count == childCount)
                return;
            
            GUILayout.Label($"- {childCount - count} children", _globalStyle);
        }

        private static void DrawItemHierarchyCounter(Rect selectionRect)
        {
            var childrenCount = _currentObject.transform.childCount;

            if (childrenCount <= 0) return;

            string content = childrenCount.ToString();

            var size = _localStyle.CalcSize(content);
            var rect = selectionRect.AlignRight(size.x + 3).AddX(7);

            GUI.Label(rect, content, _localStyle);
        }
    }
}