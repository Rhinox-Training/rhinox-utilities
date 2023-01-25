using Sirenix.OdinInspector.Editor;
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
using UnityEditor;
using UnityEngine;
using RectExtensions = Rhinox.Lightspeed.RectExtensions;

namespace Rhinox.Utilities.Odin.Editor
{
    public class ScriptableObjectCreator : OdinMenuEditorWindow
    {
        static HashSet<Type> scriptableObjectTypes = new HashSet<Type>(
            AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes())
                .Where(t =>
                    t.IsClass &&
                    typeof(ScriptableObject).IsAssignableFrom(t) &&
                    !typeof(EditorWindow).IsAssignableFrom(t) &&
                    !typeof(UnityEditor.Editor).IsAssignableFrom(t) &&
                    !t.ImplementsOpenGenericClass(typeof(UnityEditor.ScriptableSingleton<>)))
        );

        private static readonly string[] IgnoredNamespaces = new[]
        {
            "JetBrains.*",
            "Sirenix.*",
            "UnityEditor.*",
#if TEXT_MESH_PRO
            "TMPro.*"
#endif
        };

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
            window.ShowUtility();
            window.position = RectExtensions.AlignCenter(CustomEditorGUI.GetEditorWindowRect(), 800, 500);
            window.titleContent = new GUIContent(path);
            window.targetFolder = path.Trim('/');
        }

        private ScriptableObject previewObject;
        private string targetFolder;
        private Vector2 scroll;

        private Type SelectedType
        {
            get
            {
                var m = this.MenuTree.Selection.LastOrDefault();
                return m == null ? null : m.Value as Type;
            }
        }

        private IEnumerable<Type> GetTypesForTree()
        {
            return scriptableObjectTypes
                .Where(x => !x.IsAbstract)
                .Where(x => string.IsNullOrWhiteSpace(x.Namespace) || !MatchesIgnoredNamespace(x.Namespace))
                .Where(x => Attribute.GetCustomAttribute(x, typeof(IgnoreInScriptableObjectCreatorAttribute)) == null);
        }

        protected override OdinMenuTree BuildMenuTree()
        {
            this.MenuWidth = 270;
            this.WindowPadding = Vector4.zero;

            OdinMenuTree tree = new OdinMenuTree(false);
            tree.Config.DrawSearchToolbar = true;
            tree.DefaultMenuStyle = OdinMenuStyle.TreeViewStyle;
            tree.AddRange(GetTypesForTree(), GetMenuPathForType).AddThumbnailIcons();
            tree.SortMenuItemsByName();
            tree.Selection.SelectionConfirmed += x => this.CreateAsset();
            tree.Selection.SelectionChanged += e =>
            {
                if (this.previewObject && !AssetDatabase.Contains(this.previewObject))
                {
                    DestroyImmediate(this.previewObject);
                }

                if (e != SelectionChangedType.ItemAdded)
                {
                    return;
                }

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

            var name = t.Name.Split('`').First().SplitCamelCase();
            var nameSpace = t.Namespace;
            var basePath = GetMenuPathForType(t.BaseType, true);
            if (!string.IsNullOrWhiteSpace(basePath))
                basePath += "/";

            if (!string.IsNullOrWhiteSpace(nameSpace))
                nameSpace += "/";

            return nameSpace + basePath + name;
        }

        protected override IEnumerable<object> GetTargets()
        {
            yield return this.previewObject;
        }

        protected override void DrawEditor(int index)
        {
            if (MenuTree.Selection.Count == 1 && MenuTree.Selection[0].Value != null)
            {
                var type = MenuTree.Selection[0].Value as Type;
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
                    this.CreateAsset();
                }
            }
        }

        private void CreateAsset()
        {
            if (!this.previewObject) return;

            var dest = this.targetFolder + "/new " + this.MenuTree.Selection.First().Name.ToLower() + ".asset";
            dest = AssetDatabase.GenerateUniqueAssetPath(dest);
            AssetDatabase.CreateAsset(this.previewObject, dest);
            AssetDatabase.Refresh();
            Selection.activeObject = this.previewObject;
            EditorApplication.delayCall += this.Close;
        }
    }
}