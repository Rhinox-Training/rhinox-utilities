using System.Collections.Generic;
using System.IO;
using System.Linq;
using Rhinox.GUIUtils;
using Rhinox.GUIUtils.Editor;
using Rhinox.Lightspeed;
using UnityEditor;
using UnityEngine;

namespace Rhinox.Utilities.Editor
{
    public class UnityPackageEditor : IEditor
    {
        public UnityPackageInfo Target;

        public CustomMenuTree _tree;

        public string ExportRoot;
        public MemorySize SelectedSize;
        
        private readonly Dictionary<IMenuItem, bool> _stateByMenuItem = new Dictionary<IMenuItem, bool>();
        private readonly Dictionary<IMenuItem, MemorySize> _sizeByMenuItem = new Dictionary<IMenuItem, MemorySize>();
        
        private Vector2 _scrollPosition;

        public UnityPackageEditor(UnityPackageInfo info)
        {
            Target = info;
            if (Target.IsLoaded)
                CreateTree();
        }

        private void CreateTree()
        {
            _tree = new CustomMenuTree()
            {
                ShowGrouped = true,
                ItemHeight = 18
            };
            ExportRoot = Target.Root;

            foreach (UnityPackageInfo.UnityPackageFile file in Target.Contents)
            {
                var trimmedPath = file.ActualPath.RemoveFirst(Target.Root);
                if (trimmedPath.StartsWith("/"))
                    trimmedPath = trimmedPath.Substring(1);

                var icon = file.Preview;
                if (icon == null)
                    icon = GetIconForPath(trimmedPath);
                _tree.Add(trimmedPath, file, icon);
            }

            _tree.BeginItemDraw += OnBeginItemDraw;
            _tree.EndItemDraw += OnEndItemDraw;
        }

        private eUtility.DisabledGroup _disabledScope;
        private void OnBeginItemDraw(IMenuItem item, Rect fullRect, ref Rect rect)
        {
            if (IsDisabledByGroup(item))
                _disabledScope = new eUtility.DisabledGroup();
            else
            {
                if (!_stateByMenuItem.ContainsKey(item))
                    _stateByMenuItem.Add(item, true);
            
                var newState = GUI.Toggle(fullRect.AlignLeft(16f), _stateByMenuItem[item], GUIContent.none);
                if (newState != _stateByMenuItem[item])
                {
                    _stateByMenuItem[item] = newState;
                    RecalculateSelectedSize();
                    Event.current.Use();
                }
            }
            
            rect.xMin += 16f;

            if (_disabledScope == null && !_stateByMenuItem[item])
                _disabledScope = new eUtility.DisabledGroup();
        }

        private void OnEndItemDraw(IMenuItem item, Rect fullRect)
        {
            var size = GetMemorySizeForItem(item);
            GUI.Label(fullRect.AlignRight(120), size.ToString(), CustomGUIStyles.MiniLabelRight);

            if (_disabledScope != null)
            {
                _disabledScope.Dispose();
                _disabledScope = null;
            }
        }

        private MemorySize GetMemorySizeForItem(IMenuItem item)
        {
            if (!_sizeByMenuItem.ContainsKey(item))
            {
                MemorySize size = default;
                if (item.RawValue is UnityPackageInfo.UnityPackageFile file)
                    size = file.Size;
                else if (item is HierarchyMenuItem group)
                {
                    var sizes = group.GetAllChildren()
                        .Select(x => x.RawValue)
                        .OfType<UnityPackageInfo.UnityPackageFile>()
                        .Select(x => x.Size);
                    size = new MemorySize(sizes);
                }

                _sizeByMenuItem[item] = size.To(MemoryUnit.MB);
            }

            return _sizeByMenuItem[item];
        }

        private bool IsDisabledByGroup(IMenuItem item)
        {
            foreach (var pair in _stateByMenuItem)
            {
                if (!pair.Value && pair.Key is HierarchyMenuItem groupItem && groupItem.Contains(item))
                    return true;
            }

            return false;
        }

        public void Draw()
        {
            using (new eUtility.Box(CustomGUIStyles.Box, GUILayout.ExpandHeight((true))))
            {
                GUILayout.Label(Target.Name, CustomGUIStyles.BoldTitleCentered);

                _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

                using (new eUtility.HorizontalGroup(CustomGUIStyles.Clean))
                {
                    EditorGUILayout.TextField(Target.Folder);
                    if (GUILayout.Button("Copy", GUILayout.Width(120)))
                        GUIUtility.systemCopyBuffer = Target.Folder;
                }
                
                
                using (new eUtility.HorizontalGroup())
                {
                    GUILayout.Label("Compressed Size:", GUILayout.Width(120));
                    GUILayout.Label(Target.CompressedFileSize.ToString(MemoryUnit.MB));
                    GUILayout.FlexibleSpace();
                    if (Target.IsLoaded)
                    {
                        GUILayout.Label("Uncompressed Size:", GUILayout.Width(120));
                        GUILayout.Label(Target.UncompressedFileSize.ToString(MemoryUnit.MB));
                    }
                }

                if (Target.IsLoaded)
                {
                    using (new eUtility.HorizontalGroup())
                    {
                        EditorGUILayout.PrefixLabel("Root");
                        ExportRoot = EditorGUILayout.TextField(ExportRoot);
                    }

                    using (new eUtility.Box())
                    {             
                        _tree.Draw();
                    }
                    _tree.Update();
                }
                else if (GUILayout.Button("Load"))
                {
                    Target.Load();
                    CreateTree();
                    RecalculateSelectedSize();
                }

                GUILayout.EndScrollView();

                if (!Target.IsLoaded) return;
                
                using (new eUtility.HorizontalGroup())
                {
                    if (GUILayout.Button("All", CustomGUIStyles.ButtonLeft))
                    {
                        foreach (var item in _stateByMenuItem.Keys.ToArray())
                            _stateByMenuItem[item] = true;
                        RecalculateSelectedSize();
                    }
                    if (GUILayout.Button("None", CustomGUIStyles.ButtonRight))
                    {
                        foreach (var item in _stateByMenuItem.Keys.ToArray())
                            _stateByMenuItem[item] = false;
                        RecalculateSelectedSize();
                    }
                    GUILayout.Label("Selected Size:", GUILayout.Width(90));
                    GUILayout.Label(SelectedSize.ToString(MemoryUnit.MB));
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("IMPORT", GUILayout.Width(200)))
                    {
                        if (!ExportRoot.IsFileSystemSafe())
                            EditorApplication.delayCall += () => EditorUtility.DisplayDialog("Invalid Root", $"'{ExportRoot}' is not valid for the file system.", "OK");
                        else
                        {
                            var guids = GetSelectedItems()
                                .Select(x => x.RawValue)
                                .OfType<UnityPackageInfo.UnityPackageFile>()
                                .Select(x => x.GUID)
                                .ToArray();
                            Target.ImportGuids(guids, ExportRoot);
                        }
                    }
                }
            }
        }

        private ICollection<IMenuItem> GetSelectedItems()
        {
            HashSet<IMenuItem> items = new HashSet<IMenuItem>();
            foreach (var item in _tree.MenuItems)
            {
                if (_stateByMenuItem.GetOrDefault(item, true) == false || IsDisabledByGroup(item))
                    continue;
                if (item is HierarchyMenuItem group)
                    items.AddRange(group.GetAllChildren());
                else
                    items.Add(item);
            }

            return items;
        }

        private void RecalculateSelectedSize()
        {
            if (_tree == null) return;

            var sizes = GetSelectedItems()
                .Select(x => x.RawValue)
                .OfType<UnityPackageInfo.UnityPackageFile>()
                .Select(x => x.Size);

            SelectedSize = new MemorySize(sizes, MemoryUnit.MB);
        }

        public bool HasPreviewGUI() => false;

        public void DrawPreview(Rect rect) {}

        public bool CanDraw() => Target != null;

        public void Destroy() { }
        
        private static Texture GetIconForPath(string trimmedPath)
        {
            var ext = Path.GetExtension(trimmedPath);
            switch (ext)
            {
                case ".txt":
                case ".hlsl":
                    return UnityIcon.InternalIcon("d_TextAsset Icon");
                case ".unity":
                    return UnityIcon.InternalIcon("d_SceneAsset Icon");
                case ".cs":
                    return UnityIcon.InternalIcon("d_cs Script Icon");
                case ".asmdef":
                    return UnityIcon.InternalIcon("d_AssemblyDefinitionAsset Icon");
                case ".dll":
                    return UnityIcon.InternalIcon("d_Assembly Icon");
                case ".shader":
                    return UnityIcon.InternalIcon("d_Shader Icon");
                case ".compute":
                    return UnityIcon.InternalIcon("d_ComputeShader Icon");
                case ".prefab":
                    return UnityIcon.InternalIcon("d_Prefab Icon");
                case ".mat":
                    return UnityIcon.InternalIcon("d_Material Icon");
                case ".fbx":
                    return UnityIcon.InternalIcon("d_Mesh Icon");
                case ".asset":
                    // alt: UnityIcon.InternalIcon("GameManager Icon")
                    return UnityIcon.InternalIcon("d_ScriptableObject Icon");
                case ".png":
                case ".jpg":
                case ".jpeg":
                case ".bmp":
                    return UnityIcon.InternalIcon("d_Texture Icon");
                default:
                    return UnityIcon.InternalIcon("d_DefaultAsset Icon");
            }
        }
    }
}