using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Rhinox.Lightspeed;
using Rhinox.Lightspeed.Reflection;
#if UNITY_EDITOR
using UnityEditorInternal;
#else
using System.IO;
using Rhinox.Perceptor;
#endif
using UnityEngine;
using UnityEngine.UIElements;

namespace Rhinox.Utilities
{
    // Create a new type of Settings Asset.
    public abstract class CustomProjectSettings : ScriptableObject
    {
        public virtual string Name => GetType().Name.SplitCamelCase();

        protected virtual void LoadDefaults()
        {
            
        }
        
        public virtual void OnChanged()
        {
            
        }
        
        public virtual ICollection<string> GetKeywords()
        {
            return Array.Empty<string>();
        }
    }
    
    // Create a new type of Settings Asset.
    public abstract class CustomProjectSettings<T> : CustomProjectSettings where T : CustomProjectSettings<T>
    {
        protected static string SettingsFileName => $"{typeof(T).Name}";
        
        private static string _settingsPath = null;
        public static string SettingsPath
        {
            get
            {
                if (_settingsPath == null)
                    _settingsPath = $"ProjectSettings/{SettingsFileName}.asset";
                return _settingsPath;
            }
        }
        
        private static T _instance = null;
        
        public static bool HasInstance => _instance != null;
        
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = GetOrCreateSettings();
                }

                return _instance;
            }
        }

        private static T GetOrCreateSettings()
        {
#if UNITY_EDITOR
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
#else
            string settingsPath = Path.Combine(ProjectSettingsHelper.BUILD_FOLDER, SettingsPath).ToLinuxSafePath();
            string extension = Path.GetExtension(settingsPath);
            settingsPath = settingsPath.ReplaceLast(extension, "");
            T settings = Resources.Load<T>(settingsPath);
            if (settings == null)
            {
                PLog.Warn<UtilityLogger>($"No ProjectSettings found at '{settingsPath}', creating default...");
                settings = ScriptableObject.CreateInstance<T>();
                settings.LoadDefaults();
            }
#endif
            return settings;
        }

        public override void OnChanged()
        {
            base.OnChanged();
#if UNITY_EDITOR
            InternalEditorUtility.SaveToSerializedFileAndForget(new[] {this}, SettingsPath, true);
#endif
        }
    }
}