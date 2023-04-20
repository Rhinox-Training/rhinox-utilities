using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Rhinox.GUIUtils.Editor;
using Rhinox.Utilities.Editor;

namespace Rhinox.Utilities.Odin.Editor
{
    /// <summary>
    /// A unity editor window that allows you to search the scene for objects with
    /// specific properties
    /// </summary>
    public class AdvancedSceneSearchWindow : PagerEditorWindow<AdvancedSceneSearchWindow>, IHasCustomMenu
    {
        protected override object RootPage => new AdvancedSceneSearchOverview(_pager);
        protected override string RootPageName => "Overview";

        [MenuItem(WindowHelper.FindToolsPrefix + "Advanced Scene Search", false, -98)]
        public static void OpenWindow()
        {
            AdvancedSceneSearchWindow window;
            if (!GetOrCreateWindow(out window)) return;

            window.name = "Advanced Search";
            window.titleContent = new GUIContent("Adv. Search", EditorGUIUtility.FindTexture("d_ViewToolZoom"));
        }
    }
}