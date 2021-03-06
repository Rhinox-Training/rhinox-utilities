using Rhinox.Lightspeed;
using Rhinox.GUIUtils;
using UnityEditor;
using UnityEngine;

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
        }

        private static void OnEditorUpdate()
        {
            _isDrawn = false;
        }

        static void DrawSelectionCounter(int instanceID, Rect selectionRect)
        {
            if (!Selection.instanceIDs.Contains(instanceID)) return;

            _currentObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;

            if (_currentObject == null) return;

            DrawItemHierarchyCounter(selectionRect);
            DrawGlobalHierarchyCounter(selectionRect);
        }

        private static void DrawGlobalHierarchyCounter(Rect selectionRect)
        {
            if (_isDrawn) return;

            if (Event.current.type != EventType.Repaint) return;

            var count = Selection.transforms.Length;
            var childCount = Selection.GetFiltered<Transform>(SelectionMode.Deep).Length;

            var content = GUIContentHelper.TempContent($"{count} | {childCount}", _globalTooltip);

            var size = _globalStyle.CalcSize(content);

            var rect = selectionRect.AlignLeft(size.x + 3).SetX(0);

            EditorGUI.DrawRect(rect, CustomGUIStyles.DarkEditorBackground);
            GUI.Label(rect, content, _globalStyle);

            _isDrawn = true;
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