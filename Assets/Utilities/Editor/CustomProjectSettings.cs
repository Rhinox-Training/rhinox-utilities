using System;
using System.Collections.Generic;
using System.Linq;
using Rhinox.GUIUtils.Editor;
using Rhinox.Lightspeed;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
#endif
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace Rhinox.Utilities.Editor
{
    // Create a new type of Settings Asset.
    // TODO: add new odin version?
    public abstract class CustomProjectSettings<T> : ScriptableObject where T : CustomProjectSettings<T>
    {
        public virtual string Name => GetType().Name.SplitCamelCase();
        
        private static string _settingsPath = null;
        public static string SettingsPath
        {
            get
            {
                if (_settingsPath == null)
                    _settingsPath = $"ProjectSettings/{typeof(T).Name}.asset";
                return _settingsPath;
            }
        }

        private static T _instance = null;
        
#if ODIN_INSPECTOR
        private PropertyTree _propertyTree;
#else
        private DrawablePropertyView _propertyView;
#endif


        private UnityEditor.Editor _editor;
        private SerializedObject _serializedObject;

        public static bool HasInstance => _instance != null;
        
        public static T Instance
        {
            get
            {
                if (_instance == null)
                    _instance = GetOrCreateSettings();
                return _instance;
            }
        }

        private static T GetOrCreateSettings()
        {
            var settingsObjs = InternalEditorUtility.LoadSerializedFileAndForget(SettingsPath);
            T settings = null;
            if (settingsObjs != null)
                settings = settingsObjs.OfType<T>().FirstOrDefault();
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<T>();
                settings.LoadDefaults();
                
                InternalEditorUtility.SaveToSerializedFileAndForget(new[] {settings}, SettingsPath, true);
            }

            return settings;
        }

        public virtual void OnActivate(string searchContext, VisualElement rootElement)
        {
            if (_serializedObject == null)
                _serializedObject = new SerializedObject(this);
        }

        public virtual void OnChanged()
        {
            InternalEditorUtility.SaveToSerializedFileAndForget(new[] {this}, SettingsPath, true);
        }

        protected virtual void LoadDefaults()
        {
            
        }

        internal virtual void OnCustomGUI(string searchContext)
        {
#if ODIN_INSPECTOR
            if (_propertyTree == null)
                _propertyTree = PropertyTree.Create(Instance);
#else
            if (_propertyView == null)
                _propertyView = new DrawablePropertyView(_serializedObject);
#endif


            // if (_editor == null)
            //     _editor = UnityEditor.Editor.CreateEditor(this, typeof(GenericSmartUnityObjectEditor));
            // _editor.DrawDefaultInspector();

            
            using (new eUtility.PaddedGUIScope())
            {
                EditorGUI.BeginChangeCheck();

#if ODIN_INSPECTOR
                _propertyTree.Draw();
#else
                _propertyView.DrawLayout();
#endif

                bool hasEditorFile = HasOldFile();
                EditorGUI.BeginDisabledGroup(!hasEditorFile);
                {
                    if (GUILayout.Button("Import"))
                        TryImport();
                }
                EditorGUI.EndDisabledGroup();

                if (EditorGUI.EndChangeCheck())
                    OnChanged();
            }
        }

        private bool HasOldFile()
        {
            var assetPath = $"Assets/Editor/{typeof(T).Name}.asset";
            var settingsGuid = AssetDatabase.AssetPathToGUID(assetPath);
            return string.IsNullOrWhiteSpace(settingsGuid) || Guid.Parse(settingsGuid) != Guid.Empty;
        }

        private void TryImport()
        {
            var assetPath = $"Assets/Editor/{typeof(T).Name}.asset";
            var settings = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            
        }

        protected SettingsProvider CreateSettingsProvider()
        {
            var provider = new CustomSettingsProvider<T>(Instance, SettingsScope.Project);

            // Automatically extract all keywords from the Styles.
            provider.keywords = Instance.GetKeywords();
            return provider;
        }

        protected virtual ICollection<string> GetKeywords()
        {
            return Array.Empty<string>();
        }
    }

    internal class CustomSettingsProvider<T> : SettingsProvider where T : CustomProjectSettings<T>
    {
        private T _settingsInstance;

        public CustomSettingsProvider(T instance, SettingsScope scope = SettingsScope.User)
            : base($"Project/Rhinox/{(instance == null ? typeof(T).Name : instance.Name)}", scope)
        {
            _settingsInstance = instance;
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            guiHandler = _settingsInstance.OnCustomGUI;
            activateHandler = _settingsInstance.OnActivate;
            
            base.OnActivate(searchContext, rootElement);
        }
    }
}