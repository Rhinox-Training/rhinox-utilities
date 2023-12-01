/*
 *	Created by Philippe Groarke on 2016-08-28.
 *	Copyright (c) 2016 Tarfmagougou Games. All rights reserved.
 *
 *	Dedication : I dedicate this code to Gabriel, who makes kickass extensions. Now go out and use awesome icons!
 */
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Rhinox.GUIUtils;
using Rhinox.GUIUtils.Editor;
using Rhinox.Lightspeed;
using Sirenix.OdinInspector;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
#endif

using ObjectFieldAlignment = Sirenix.OdinInspector.ObjectFieldAlignment;

namespace Rhinox.Utilities.Editor
{
    /// ================================================================================================================
    /// UnityIconsViewer
    public class UnityIconsViewer : CustomMenuEditorWindow
    {
        private List<UnityIcon> _Icons = new List<UnityIcon>();
        private Vector2 _scrollPos;
        private GUIContent _refreshButton;
        
        [MenuItem(WindowHelper.WindowPrefix + "Asset Management/View Icons List")]
        public static void ShowWindow()
        {
            var w = GetWindow<UnityIconsViewer>();
            w.titleContent = TitleContent;
        }

        public static GUIContent TitleContent
        {
            get { return new GUIContent("Icon List", UnityIcon.InternalIcon("Search On Icon")); }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            FindIcons();
        }

        protected override void OnBeginDrawEditors()
        {
            var toolbarHeight = MenuTree.ToolbarHeight;

            // Draws a toolbar with the name of the currently selected menu item.
            CustomEditorGUI.BeginHorizontalToolbar(height: toolbarHeight);
            {
                GUILayout.Label(_Icons.Count + " Icons found");

                if (CustomEditorGUI.ToolbarButton(new GUIContent("Refresh", UnityIcon.InternalIcon("Refresh@2x"))))
                    FindIcons();
            }
            CustomEditorGUI.EndHorizontalToolbar();
        }

        /* Find all textures and filter them to narrow the search. */
        void FindIcons()
        {
            UnityIconFinder.FindIcons(ref _Icons);

            Repaint();
        }


        protected override CustomMenuTree BuildMenuTree()
        {
            var tree = new CustomMenuTree(); // true: multisearch
#if ODIN_INSPECTOR
            if (tree.DefaultMenuStyle != null)
                tree.DefaultMenuStyle.IconSize = 16.00f;
            tree.DrawSearchToolbar = true;
#endif

            foreach (var icon in _Icons)
                tree.Add(icon.Origin + "/" + icon.Name, icon, icon.Icon);

            tree.SortMenuItemsByName();

            return tree;
        }

#if !ODIN_INSPECTOR
        protected override void DrawEditor(int index)
        {
            var target = (UnityIcon)GetTargets().ElementAt(index);

            var propertyDrawer = new DrawablePropertyView(target);
            propertyDrawer.RepaintRequested += Repaint;
            propertyDrawer.DrawLayout();
        }
#endif
    }
}