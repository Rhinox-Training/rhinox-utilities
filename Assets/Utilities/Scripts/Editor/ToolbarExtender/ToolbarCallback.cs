using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using UnityEngine.UIElements;

namespace Rhinox.Utilities.Editor
{
    public static class ToolbarCallback
    {
        static Type _toolbarType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Toolbar");
        static Type _guiViewType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GUIView");
#if UNITY_2020_1_OR_NEWER
        private static PropertyInfo _eventInterestsProperty = _guiViewType.GetProperty("eventInterests",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        private static Type _eventInterestType =
            typeof(UnityEngine.Event).Assembly.GetType("UnityEngine.EventInterests");

        private static MethodInfo _Internal_SetWantsMouseMoveMethod = _guiViewType.GetMethod(
            "Internal_SetWantsMouseMove",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        
        private static MethodInfo Internal_SetWantsMouseEnterLeaveWindowMethod = _guiViewType.GetMethod(
            "Internal_SetWantsMouseEnterLeaveWindow",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        private static PropertyInfo _wantsMouseMoveProperty = _eventInterestType.GetProperty("wantsMouseMove",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        static Type _iWindowBackendType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.IWindowBackend");
		static PropertyInfo _windowBackend = _guiViewType.GetProperty("windowBackend",
			BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		static PropertyInfo _viewVisualTree = _iWindowBackendType.GetProperty("visualTree",
			BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
#else
        static PropertyInfo _viewVisualTree = _guiViewType.GetProperty("visualTree",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
#endif
        static FieldInfo _imguiContainerOnGui = typeof(IMGUIContainer).GetField("m_OnGUIHandler",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        static ScriptableObject _currentToolbar;

        /// <summary>
        /// Callback for toolbar OnGUI method.
        /// </summary>
        public static Action OnToolbarGUI;

        [InitializeOnLoadMethod]
        private static void SetupToolbarCallback()
        {
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
        }

        static void OnUpdate()
        {
            // Relying on the fact that toolbar is ScriptableObject and gets deleted when layout changes
            if (_currentToolbar == null)
            {
                // Find toolbar
                var toolbars = Resources.FindObjectsOfTypeAll(_toolbarType);
                _currentToolbar = toolbars.Length > 0 ? (ScriptableObject) toolbars[0] : null;
                if (_currentToolbar != null)
                {
#if UNITY_2020_1_OR_NEWER
                    _Internal_SetWantsMouseMoveMethod.Invoke(_currentToolbar, new object[] { true});
                    Internal_SetWantsMouseEnterLeaveWindowMethod.Invoke(_currentToolbar, new object[] { true });
                    var eventInterests = _eventInterestsProperty.GetValue(_currentToolbar);
                    _wantsMouseMoveProperty.SetValue(eventInterests, true);
                    _eventInterestsProperty.SetValue(_currentToolbar, eventInterests);
                    
					var windowBackend = _windowBackend.GetValue(_currentToolbar);

					// Get it's visual tree
					var visualTree = (VisualElement) _viewVisualTree.GetValue(windowBackend, null);
#else
                    // Get it's visual tree
                    var visualTree = (VisualElement) _viewVisualTree.GetValue(_currentToolbar, null);
#endif

                    // Get first child which 'happens' to be toolbar IMGUIContainer
                    var container = (IMGUIContainer) visualTree[0];

                    // (Re)attach handler
                    var handler = (Action) _imguiContainerOnGui.GetValue(container);
                    handler -= OnGUI;
                    handler += OnGUI;
                    _imguiContainerOnGui.SetValue(container, handler);
                }
            }
        }

        static void OnGUI()
        {
            OnToolbarGUI?.Invoke();
        }
    }
}