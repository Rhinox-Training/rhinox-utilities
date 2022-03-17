using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Rhinox.Utilities.Editor
{
    // Create a new type of Settings Asset.
    public abstract class CustomProjectSettings<T> : SerializedScriptableObject where T : CustomProjectSettings<T>
    {
        public virtual string Name => GetType().Name.SplitPascalCase();
        
        private static string _settingsPath = null;
        public static string SettingsPath
        {
            get
            {
                if (_settingsPath == null)
                    _settingsPath = $"Assets/Editor/{typeof(T).Name}.asset";
                return _settingsPath;
            }
        }

        private static T _instance = null;
        private PropertyTree _propertyTree;

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
            var settings = AssetDatabase.LoadAssetAtPath<T>(SettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<T>();
                settings.LoadDefaults();
                if (!AssetDatabase.IsValidFolder("Assets/Editor"))
                {
                    AssetDatabase.CreateFolder("Assets", "Editor");
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }

                AssetDatabase.CreateAsset(settings, SettingsPath);
                AssetDatabase.SaveAssets();
            }

            return settings;
        }

        public virtual void OnActivate(string searchContext, VisualElement rootElement)
        {
            
        }

        public virtual void OnChanged()
        {
            
        }

        protected virtual void LoadDefaults()
        {
            
        }

        internal virtual void OnCustomGUI(string searchContext)
        {
            if (_propertyTree == null)
                _propertyTree = PropertyTree.Create(Instance);
            
            EditorGUI.BeginChangeCheck();
            
            
            _propertyTree.Draw();
            
            if (EditorGUI.EndChangeCheck())
                OnChanged();
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