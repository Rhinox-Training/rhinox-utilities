using System;
using System.Collections.Generic;
using Rhinox.GUIUtils.Odin.Editor;
using UnityEditor;
using UnityEngine;

using Object = System.Object;

/// <summary>
/// A unity editor window that allows you to search the scene for objects with
/// specific properties
/// </summary>
public class AdvancedSceneSearchWindow : OdinPagerEditorWindow<AdvancedSceneSearchWindow>, IHasCustomMenu
{
    protected override object RootPage => new AdvancedSceneSearchOverview(_pager);
    protected override string RootPageName => "Overview";
    
    public static void OpenWindow()
    {
        AdvancedSceneSearchWindow window;
        if (!GetOrCreateWindow(out window)) return;
        
        window.name = "Advanced Search";
        window.titleContent = new GUIContent("Adv. Search", EditorGUIUtility.FindTexture("d_ViewToolZoom"));
    }
}