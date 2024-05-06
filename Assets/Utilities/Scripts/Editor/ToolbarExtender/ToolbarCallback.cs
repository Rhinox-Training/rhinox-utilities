using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using Rhinox.Lightspeed;
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

        public delegate void OnToolbarDrawDelegate(Rect rect);
        public static OnToolbarDrawDelegate OnToolbarGUILeft;
        public static OnToolbarDrawDelegate OnToolbarGUIRight;

        [InitializeOnLoadMethod]
        private static void SetupToolbarCallback()
        {
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
        }

        [MenuItem("Tools/Reset Toolbar")]
        static void ResetToolbar()
        {
            _currentToolbar = null;
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
#if UNITY_2021_3_OR_NEWER
                    var windowBackend = _windowBackend.GetValue(_currentToolbar);
                    var visualTree = (VisualElement) _viewVisualTree.GetValue(windowBackend, null);
                    visualTree = visualTree.Query<VisualElement>("ToolbarContainerContent").First();
                    
                    var leftContainer = new IMGUIContainer();
                    leftContainer.name = "ToolbarExtenderLeft";
                    leftContainer.onGUIHandler += () => OnToolbarGUILeft?.Invoke(leftContainer.contentRect);
                    leftContainer.style.flexGrow = 1;
                    leftContainer.style.SetPadding(4);
                    var rightContainer = new IMGUIContainer();
                    rightContainer.name = "ToolbarExtenderRight";
                    rightContainer.onGUIHandler += () => OnToolbarGUIRight?.Invoke(rightContainer.contentRect);
                    rightContainer.style.flexGrow = 1;
                    rightContainer.style.SetPadding(4);
                    
                    visualTree.Insert(1, leftContainer);
                    visualTree.Insert(3, rightContainer);
#else
#if UNITY_2020_1_OR_NEWER
                    var eventInterests = _eventInterestsProperty.GetValue(_currentToolbar);
                    _wantsMouseMoveProperty.SetValue(eventInterests, true);
                    _eventInterestsProperty.SetValue(_currentToolbar, eventInterests);
                    Internal_SetWantsMouseEnterLeaveWindowMethod.Invoke(_currentToolbar, new object[] { true });
                    _Internal_SetWantsMouseMoveMethod.Invoke(_currentToolbar, new object[] { true});
                    
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
#endif
                }
            }
        }

        private static bool _fetchedToolCount;
        static int _toolCount;
        static GUIStyle _commandStyle = null;

#if UNITY_2019_3_OR_NEWER
        public const float space = 8;
#else
		public const float space = 10;
#endif
        public const float largeSpace = 20;
        public const float buttonWidth = 32;
        public const float dropdownWidth = 80;
#if UNITY_2019_1_OR_NEWER
        public const float playPauseStopWidth = 140;
#else
		public const float playPauseStopWidth = 100;
#endif
        
        static void OnGUI()
        {
            if (OnToolbarGUILeft == null && OnToolbarGUIRight == null)
                return;
            
            if (!_fetchedToolCount)
                _toolCount = ToolbarExtenderUtility.GetToolCount();
            
            if (_commandStyle == null)
                _commandStyle = new GUIStyle("CommandLeft");

            var screenWidth = EditorGUIUtility.currentViewWidth;

            // Following calculations match code reflected from Toolbar.OldOnGUI()
            float playButtonsPosition = Mathf.RoundToInt ((screenWidth - playPauseStopWidth) / 2);

            Rect leftRect = new Rect(0, 0, screenWidth, Screen.height);
            leftRect.xMin += space; // Spacing left
            leftRect.xMin += buttonWidth * _toolCount; // Tool buttons
#if UNITY_2019_3_OR_NEWER
            leftRect.xMin += space; // Spacing between tools and pivot
#else
			leftRect.xMin += largeSpace; // Spacing between tools and pivot
#endif
            leftRect.xMin += 64 * 2; // Pivot buttons
            leftRect.xMax = playButtonsPosition;

            Rect rightRect = new Rect(0, 0, screenWidth, Screen.height);
            rightRect.xMin = playButtonsPosition;
            rightRect.xMin += _commandStyle.fixedWidth * 3; // Play buttons
            rightRect.xMax = screenWidth;
            rightRect.xMax -= space; // Spacing right
            rightRect.xMax -= dropdownWidth; // Layout
            rightRect.xMax -= space; // Spacing between layout and layers
            rightRect.xMax -= dropdownWidth; // Layers
#if UNITY_2019_3_OR_NEWER
            rightRect.xMax -= space; // Spacing between layers and account
#else
			rightRect.xMax -= largeSpace; // Spacing between layers and account
#endif
            rightRect.xMax -= dropdownWidth; // Account
            rightRect.xMax -= space; // Spacing between account and cloud
            rightRect.xMax -= buttonWidth; // Cloud
            rightRect.xMax -= space; // Spacing between cloud and collab
            rightRect.xMax -= 78; // Colab

            // Add spacing around existing controls
            leftRect.xMin += space;
            leftRect.xMax -= space;
            rightRect.xMin += space;
            rightRect.xMax -= space;

            // Add top and bottom margins
#if UNITY_2019_3_OR_NEWER
            leftRect.y = 4;
            leftRect.height = 22;
            rightRect.y = 4;
            rightRect.height = 22;
#else
			leftRect.y = 5;
			leftRect.height = 24;
			rightRect.y = 5;
			rightRect.height = 24;
#endif
            OnToolbarGUILeft?.Invoke(leftRect);
            OnToolbarGUIRight?.Invoke(rightRect);
        }
    }
}