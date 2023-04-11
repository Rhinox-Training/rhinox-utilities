using System;
using System.Collections.Generic;
using System.IO;
using Rhinox.GUIUtils;
using Rhinox.GUIUtils.Editor;
using Rhinox.Lightspeed;
using UnityEditor;
using UnityEngine;

namespace Rhinox.Utilities.Editor
{
    public class UnityPackageImporter : CustomMenuEditorWindow
    {
        private static readonly string[] PackageLocationFolders = new[]
        {
            "%userprofile%/Downloads",
            "%appdata%/Unity/Asset Store-5.x"
        };

        private const string InfoMessage =
            "This window only shows downloaded packages. You might want to go to the Package Manager and download them from there first.\n" +
            "Click the button to open it.  >>> ";

        [MenuItem(WindowHelper.ToolsPrefix + "Import Package", priority = 1000)]
        private static void OpenWindow()
        {
            var window = CreateInstance<UnityPackageImporter>();
            window.position = RectExtensions.AlignCenter(CustomEditorGUI.GetEditorWindowRect(), 800, 500);
            window.titleContent = new GUIContent(window.GetType().Name.SplitCamelCase());
            window.ShowUtility();
        }

        protected override void OnGUI()
        {
            base.OnGUI();

            using (new eUtility.Box())
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(GUIContentHelper.TempContent(InfoMessage, UnityIcon.InternalIcon("console.infoicon")));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Open", GUILayout.ExpandHeight(true)))
                    EditorApplication.ExecuteMenuItem("Window/Package Manager");
                GUILayout.EndHorizontal();
            }
        }

        protected override IEditor CreateEditorForTarget(object obj)
        {
            if (obj is UnityPackageInfo info)
            {
                var editor = new UnityPackageEditor(info);
                return editor;
            }
            return base.CreateEditorForTarget(obj);
        }

        protected override CustomMenuTree BuildMenuTree()
        {
            this.MenuWidth = 270;
            this.WindowPadding = new RectOffset();

            CustomMenuTree tree = new CustomMenuTree();
#if ODIN_INSPECTOR
            tree.DrawSearchToolbar = true;
            tree.DefaultMenuStyle = OdinMenuStyle.TreeViewStyle;
#endif
            foreach (var entry in GetPackagesForTree())
            {
                string path = GetMenuPathForPackage(entry);
                tree.Add(path, entry);
            }
#if ODIN_INSPECTOR
        tree.TryUseThumbnailIcons();
#endif
            tree.SortMenuItemsByName();
            
            return tree;
        }

        private string GetMenuPathForPackage(UnityPackageInfo info)
        {
            return info.Name;
        }

        private IEnumerable<UnityPackageInfo> GetPackagesForTree()
        {
            foreach (var folder in PackageLocationFolders)
            {
                var folderPath = Environment.ExpandEnvironmentVariables(folder);
                folderPath = Path.GetFullPath(folderPath);
                foreach (string filePath in FindFilesInDirectory(folderPath, "*.unitypackage"))
                    yield return new UnityPackageInfo(filePath);
            }
        }

        private static IEnumerable<string> FindFilesInDirectory(string path, string pattern, bool recursive = true)
        {
            var files = Directory.GetFiles(path, pattern);
            foreach (var file in files)
                yield return file;

            if (!recursive) yield break;

            var subDirectories = Directory.GetDirectories(path);
            foreach (var dir in subDirectories)
            {
                foreach (var file in FindFilesInDirectory(dir, pattern, recursive))
                    yield return file;
            }
        }
    }
}