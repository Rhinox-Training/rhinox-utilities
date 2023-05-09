using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Rhinox.GUIUtils;
using Rhinox.GUIUtils.Attributes;
using Rhinox.GUIUtils.Editor;
using Rhinox.Lightspeed;
using Rhinox.Lightspeed.Reflection;
using Rhinox.Perceptor;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector.Editor;
#endif
using UnityEditor;
using UnityEngine;
using RectExtensions = Rhinox.Lightspeed.RectExtensions;

namespace Rhinox.Utilities.Editor
{
    public class ScriptableObjectCreator : CustomMenuEditorWindow
    {
        private static readonly Type[] IgnoredBaseTypes = new[]
        {
            typeof(EditorWindow),
            typeof(UnityEditor.Editor),
            typeof(CustomProjectSettings<>)
        };

        private static readonly string[] IgnoredNamespaces = new[]
        {
            "JetBrains.*",
            "Packages.Rider.*",
            "Sirenix.*",
            "UnityEditor.*",
#if TEXT_MESH_PRO
            "TMPro.*"
#endif
        };
        
        static HashSet<Type> scriptableObjectTypes = new HashSet<Type>(
            AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes())
                .Where(t =>
                    t.IsClass &&
                    typeof(ScriptableObject) != t &&
                    typeof(ScriptableObject).IsAssignableFrom(t) &&
                    !IgnoredBaseTypes.Any(x => t.InheritsFrom(x)) &&
                    !t.ImplementsOpenGenericClass(typeof(UnityEditor.ScriptableSingleton<>)))
        );

        private static bool MatchesIgnoredNamespace(string @namespace)
        {
            return IgnoredNamespaces.Any(r => Regex.Match(@namespace, r).Success);
        }

        [MenuItem("Assets/Create Scriptable Object", priority = -1000)]
        private static void ShowDialog()
        {
            var path = "Assets";
            var obj = Selection.activeObject;
            if (obj && AssetDatabase.Contains(obj))
            {
                path = AssetDatabase.GetAssetPath(obj);
                if (!Directory.Exists(path))
                {
                    path = Path.GetDirectoryName(path);
                }
            }

            var window = CreateInstance<ScriptableObjectCreator>();
            window.position = RectExtensions.AlignCenter(CustomEditorGUI.GetEditorWindowRect(), 800, 500);
            window.titleContent = new GUIContent($"Create new SO in folder {path}");
            window.targetFolder = path.Trim('/');
            window.ShowUtility();
        }

        private ScriptableObject previewObject;
        private string targetFolder;
        private Vector2 scroll;

        private Type SelectedType
        {
            get
            {
                var m = this.MenuTree.Selection.LastOrDefault();
                return m == null ? null : m.RawValue as Type;
            }
        }

        private IEnumerable<Type> GetTypesForTree()
        {
            return scriptableObjectTypes
                .Where(x => !x.IsAbstract)
                .Where(x => string.IsNullOrWhiteSpace(x.Namespace) || !MatchesIgnoredNamespace(x.Namespace))
                .Where(x => Attribute.GetCustomAttribute(x, typeof(IgnoreInScriptableObjectCreatorAttribute)) == null);
        }

        protected override CustomMenuTree BuildMenuTree()
        {
            this.MenuWidth = 270;
            this.WindowPadding = new RectOffset();

            CustomMenuTree tree = new CustomMenuTree();
            tree.DrawSearchToolbar = true;
#if ODIN_INSPECTOR
            tree.DefaultMenuStyle = OdinMenuStyle.TreeViewStyle;
#endif
            foreach (var entry in GetTypesForTree())
            {
                string path = GetMenuPathForType(entry);
                tree.Add(path, entry);
            }
#if ODIN_INSPECTOR
            tree.TryUseThumbnailIcons();
#endif
            tree.SortMenuItemsByName();
            tree.SelectionChanged += (x) =>
            {
                if (this.previewObject && !AssetDatabase.Contains(this.previewObject))
                {
                    DestroyImmediate(this.previewObject);
                }

                if (tree.Selection.Count == 0)
                    return;

                var t = this.SelectedType;
                if (t != null && !t.IsAbstract)
                {
                    this.previewObject = CreateInstance(t) as ScriptableObject;
                }
            };

            return tree;
        }

        private string GetMenuPathForType(Type t) => GetMenuPathForType(t, false);

        private string GetMenuPathForType(Type t, bool isBaseType)
        {
            if (t == null || !scriptableObjectTypes.Contains(t)) return string.Empty;

            if (isBaseType && !string.IsNullOrWhiteSpace(t.Namespace) && MatchesIgnoredNamespace(t.Namespace))
                return string.Empty;

            var nameSpace = isBaseType ? string.Empty : t.Namespace;
            var basePath = GetMenuPathForType(t.BaseType, true);
            if (!string.IsNullOrWhiteSpace(basePath))
                basePath += "/";

            if (!string.IsNullOrWhiteSpace(nameSpace))
            {
                nameSpace = Fixup(nameSpace);
                nameSpace += "/";
            }

            return nameSpace + basePath + t.GetNiceName();
        }

        private static string Fixup(string nameSpace)
        {
            var arr = nameSpace.Trim().Split('.');
            if (arr.Length >= 2)
                return $"{arr[0]}/{arr[1]}";
            return nameSpace;
        }

        protected override IEnumerable<object> GetTargets()
        {
            yield return this.previewObject;
        }

        protected override void DrawEditor(int index)
        {
            if (MenuTree.Selection.Count == 1 && MenuTree.Selection[0].RawValue != null)
            {
                var type = MenuTree.Selection[0].RawValue as Type;
                //SirenixEditorGUI.BeginToolbarBox();
                CustomEditorGUI.BeginHorizontalToolbar();
                GUILayout.BeginHorizontal();
                GUILayout.Label(type.FullName);
                if (type.BaseType != null)
                    GUILayout.Label(type.BaseType.FullName, CustomGUIStyles.SubtitleRight);
                GUILayout.EndHorizontal();
                CustomEditorGUI.EndHorizontalToolbar();
                //SirenixEditorGUI.EndToolbarBox();
            }

            this.scroll = GUILayout.BeginScrollView(this.scroll);
            {
                base.DrawEditor(index);
            }
            GUILayout.EndScrollView();

            if (this.previewObject)
            {
                GUILayout.FlexibleSpace();
                CustomEditorGUI.HorizontalLine(1);
                if (GUILayout.Button("Create Asset", GUILayout.Height(25)))
                {
                    this.CreateAsset(MenuTree.Selection.First());
                }
            }
        }

        private void CreateAsset(IMenuItem uiMenuItem)
        {
            if (!this.previewObject) return;

            var dest = this.targetFolder + "/new " + (uiMenuItem.RawValue as Type).Name.ToLower() + ".asset";
            dest = AssetDatabase.GenerateUniqueAssetPath(dest);
            try
            {
                AssetDatabase.CreateAsset(this.previewObject, dest);
            }
            catch (Exception e)
            {
                PLog.Error<UtilityLogger>($"Failed to create asset '{this.previewObject}' at '{dest}': {e.ToString()}");
                throw;
            }

            AssetDatabase.Refresh();
            Selection.activeObject = this.previewObject;
            EditorApplication.delayCall += this.Close;
        }
    }
}