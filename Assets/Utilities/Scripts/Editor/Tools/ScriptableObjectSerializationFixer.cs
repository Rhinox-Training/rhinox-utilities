using System;
using System.Collections.Generic;
using System.Linq;
using Rhinox.GUIUtils.Editor;
using Rhinox.Lightspeed.Editor;
using Rhinox.Lightspeed.IO;
using Rhinox.Lightspeed.Reflection;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools.Utils;


namespace Rhinox.Utilities.Editor
{
    public abstract class AssetModalPopupWindow<T, TAsset> : EditorWindow where T : AssetModalPopupWindow<T, TAsset> where TAsset : UnityEngine.Object
    {
        private TAsset _asset;
        protected TAsset Asset => _asset;

        protected static bool ValidateContextMenu()
        {
            if (Selection.objects == null || Selection.objects.Length > 1)
                return false;

            return Selection.activeObject is TAsset;
        }

        protected static void OpenWindow()
        {
            var window = GetWindow<T>();
            window._asset = (TAsset)Selection.activeObject;
            window.Initialize();
            window.ShowModal();
        }

        protected virtual void Initialize()
        {
            
        }

        private void OnGUI()
        {
            OnDraw();
            
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("Confirm"))
                {
                    OnConfirm();
                    this.Close();
                }
                if (GUILayout.Button("Cancel"))
                    this.Close();
            }
            GUILayout.EndHorizontal();
        }

        protected abstract void OnConfirm();

        protected abstract void OnDraw();
    }
    
    [InlineEditor(InlineEditorObjectFieldModes.Hidden)]
    public class ScriptableObjectSerializationFixer : AssetModalPopupWindow<ScriptableObjectSerializationFixer, ScriptableObject>
    {
        [Serializable]
        public class MissingSerializedTypeEntry
        {
            [ToggleLeft, HorizontalGroup("Row", 24), HideLabel]
            public bool Include;
            [HorizontalGroup("Row"), HideLabel, ReadOnly]
            public string OriginalTypeName;
            [HorizontalGroup("Row"), HideLabel, InlineButton(nameof(TryFindType), " Find ")]
            public Type Type;

            public void TryFindType()
            {
                if (string.IsNullOrEmpty(OriginalTypeName))
                    return;
                Type = ReflectionUtility.FindTypeExtensively(ref OriginalTypeName);
            }
        }

        [ListDrawerSettings(HideAddButton = true, HideRemoveButton = true, DraggableItems = false), ShowInInspector]
        private List<MissingSerializedTypeEntry> _entries;
        
        private const int PRIORITY = 39;
        private static ScriptableObject _obj;
        private IOrderedDrawable _propertyView;

        [MenuItem("Assets/Run Serialization Checker", true, PRIORITY)]
        public static bool ValidateOpen() => ValidateContextMenu();

        [MenuItem("Assets/Run Serialization Checker", false, PRIORITY)]
        public static void Open() => OpenWindow();

        protected override void Initialize()
        {
            base.Initialize();
            _entries = ParseEntries();
            _propertyView = DrawableFactory.CreateDrawableFor(this);
        }

        
        
        private List<MissingSerializedTypeEntry> ParseEntries()
        {
            var list = new List<MissingSerializedTypeEntry>();
            foreach (var assemblyQualifiedName in ScriptableObjectUtility.FetchAssemblyQualifiedNamesFromScriptableObject(Asset))
            {
                if (assemblyQualifiedName == null)
                    continue;

                var type = Type.GetType(assemblyQualifiedName, false);
                if (type != null)
                    continue;

                if (list.Any(x => x.OriginalTypeName.Equals(assemblyQualifiedName)))
                    continue;
                
                var entry = new MissingSerializedTypeEntry()
                {
                    Include = true,
                    OriginalTypeName = assemblyQualifiedName
                };
                list.Add(entry);
            }
            return list;
        }

        protected override void OnConfirm()
        {
            AssetDatabase.StartAssetEditing();
            var assetPath = AssetDatabase.GetAssetPath(Asset);
            if (!string.IsNullOrEmpty(assetPath))
            {
                var text = FileHelper.ReadAllText(assetPath); 
                foreach (var entry in _entries)
                {
                    if (entry == null || !entry.Include)
                        continue;

                    if (entry.Type == null)
                        continue;

                    text = text.Replace(entry.OriginalTypeName, entry.Type.GetShortAssemblyQualifiedName());
                }
                System.IO.File.WriteAllText(assetPath, text);
            }
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
        }

        protected override void OnDraw()
        {
            if (_propertyView != null)
                _propertyView.Draw(GUIContent.none);
        }

        private void GetIssues()
        {
            var idk = new SerializedObject(Asset);
         
            
        }
    }
}