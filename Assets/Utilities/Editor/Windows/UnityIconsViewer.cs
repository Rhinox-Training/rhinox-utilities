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
            _Icons.Clear();

            // Internal icons
            var textures = InternalUnityIcon.GetAll();
            foreach (var group in textures.GroupBy(x => InternalUnityIcon.TrimmedName(x)))
            {
                if (group.Count() == 1)
                {
                    var tex = group.First();
                    _Icons.Add(new InternalUnityIcon
                    {
                        Icon = tex,
                        Name = tex.name,
                        Origin = "Internal",
                    });
                }
                else
                {
                    Texture2D darkTex = null, lightTex = null, highresDarkTex = null, highresLightTex = null;
                    foreach (var texture in group)
                    {
                        if (texture.name.EndsWith("@2x"))
                        {
                            if (texture.name.StartsWith("d_"))
                                highresDarkTex = texture;
                            else
                                highresLightTex = texture;
                        }
                        else
                        {
                            if (texture.name.StartsWith("d_"))
                                darkTex = texture;
                            else
                                lightTex = texture;
                        }
                    }

                    Texture2D tex;

                    if (EditorGUIUtility.isProSkin)
                        tex = highresDarkTex ?? darkTex ?? highresLightTex ?? lightTex;
                    else
                        tex = highresLightTex ?? lightTex ?? highresDarkTex ?? darkTex;

                    bool hasDarkLightVariants = darkTex && lightTex;
                    bool hasHighResVariants = highresDarkTex || highresLightTex;

                    _Icons.Add(new InternalUnityIcon
                    {
                        Icon = tex,
                        Name = hasDarkLightVariants ? lightTex.name : tex.name,
                        HasLightDarkVariants = hasDarkLightVariants,
                        HasHighResVariants = hasHighResVariants,
                        Origin = "Internal",
                    });
                }
            }
            
            // Class icons
            textures = ScriptUnityIcon.GetAll();
            foreach (var tex in textures)
            {
                _Icons.Add(new ScriptUnityIcon
                {
                    Icon = tex,
                    Name = tex.name,
                    Origin = "Script"
                });
            }
            
            // Odin icons
            var odinIcons = OdinUnityIcon.GetAll();
            foreach (var pair in odinIcons)
            {
                _Icons.Add(new OdinUnityIcon
                {
                    Icon = pair.Value,
                    Name = pair.Key,
                    Origin = "Odin",
                });
            }
            
            // resources icons
            _Icons.AddRange(GetAssetIcons(AssetUnityIcon.GetAll()));

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
                var tex = AssetDatabase.LoadAssetAtPath<Texture>(path);
                if (tex == null) continue;

                var name = Path.GetFileNameWithoutExtension(path);
                var ext = Path.GetExtension(path);
                
                icons.Add(new AssetUnityIcon
                {
                    Name = name,
                    Extension = ext,
                    Icon = tex,
                    Origin = "Asset Texture",
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