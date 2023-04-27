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
using Rhinox.GUIUtils.Editor;
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
        private readonly List<UnityIcon> _Icons = new List<UnityIcon>();
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
            get { return new GUIContent("Icon List", UnityIcon.AssetIcon("Fa_Search")); }
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

                if (CustomEditorGUI.ToolbarButton(new GUIContent("Refresh", UnityIcon.AssetIcon("Fa_Redo"))))
                    FindIcons();
            }
            CustomEditorGUI.EndHorizontalToolbar();
        }

        /* Find all textures and filter them to narrow the search. */
        void FindIcons()
        {
            _Icons.Clear();

            // Internal icons
            var textures = UnityIcon.GetAllInternalIcons();
            foreach (var tex in textures)
            {
                // TODO: why do this? to get the resource is memory? but it has no path...
                Resources.Load<Texture>("");

                _Icons.Add(new UnityIcon
                {
                    Icon = tex,
                    Name = tex.name,
                    Origin = "Internal",
                    // TextureUseage = "EditorGUIUtility.IconContent(\"" + tex.name + "\").image"
                    TextureUsage = "UnityIcon.InternalIcon(\"" + tex.name + "\")"
                });
            }

            // Odin icons
#if ODIN_INSPECTOR
            var odinIcons = UnityIcon.GetAllOdinIcons();
            foreach (var pair in odinIcons)
            {
                _Icons.Add(new UnityIcon
                {
                    Icon = pair.Value,
                    Name = pair.Key,
                    Origin = "Odin",
                    TextureUsage = "EditorIcons." + pair.Key + ".Active"
                });
            }
#endif
            // resources icons
            _Icons.AddRange(GetAssetIcons(UnityIcon.GetAllAssetIcons()));

            _Icons.Sort();
            Resources.UnloadUnusedAssets();
            GC.Collect();

            Repaint();
        }

        private static List<UnityIcon> GetAssetIcons(string[] iconPaths)
        {
            var icons = new List<UnityIcon>();
            foreach (var path in iconPaths)
            {
                var tex = (Texture) AssetDatabase.LoadAssetAtPath(path, typeof(Texture));
                if (tex == null) continue;

                var name = Path.GetFileNameWithoutExtension(path);
                var ext = Path.GetExtension(path);
                
                icons.Add(new UnityIcon
                {
                    Name = name,
                    Icon = tex,
                    Origin = "Asset Texture",
                    // TextureUsage = "AssetDatabase.LoadAssetAtPath<Texture>(\"" + path + "\")"
                    TextureUsage = "UnityIcon.AssetIcon(\"" + name + (ext == ".png" ? "" : ", \"" + ext + "\"") + "\")"
                });
            }

            return icons;
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