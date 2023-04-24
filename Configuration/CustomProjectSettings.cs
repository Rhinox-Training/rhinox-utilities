using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Rhinox.Lightspeed;
using Rhinox.Lightspeed.Reflection;
using Rhinox.Perceptor;
using Rhinox.Utilities.Attributes;
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
        private string _name;
        public virtual string Name
        {
            get
            {
                if (_name == null)
                    _name = ProjectSettingsTypeToName(GetType());
                return _name;
            }
        }

        public static string ProjectSettingsTypeToName(Type projectSettingsType)
        {
            if (projectSettingsType == null || !projectSettingsType.InheritsFrom(typeof(CustomProjectSettings)))
                return string.Empty;
            return projectSettingsType.Name.SplitCamelCase();
        }
        
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
        
#if UNITY_EDITOR
        public virtual bool HasBackingFileChanged()
        {
            return false;
        }
#endif

        internal bool CopyValuesFrom(CustomProjectSettings settings)
        {
            if (settings.GetType() != this.GetType())
                return false;
            var members = SerializeHelper.GetSerializedMembers(settings.GetType());
            foreach (var member in members)
            {
                try
                {
                    var value = member.GetValue(settings);
                    member.TrySetValue(this, value);
                }
                catch (Exception e)
                {
                    // NOTE: member could have a missing setter
                    PLog.Error<UtilityLogger>(e.ToString());
                }
            }

            return true;
        }
    }
    
    // Create a new type of Settings Asset.
    public abstract class CustomProjectSettings<T> : CustomProjectSettings where T : CustomProjectSettings<T>
    {
        protected static string SettingsFileName => $"{typeof(T).Name}";

        public static bool IsEditorOnly => SettingsAttribute == null || !SettingsAttribute.RuntimeSupported;

        private static CustomProjectSettingsAttribute _settingsAttribute;
        protected static CustomProjectSettingsAttribute SettingsAttribute
        {
            get
            {
                if (_settingsAttribute == null)
                    _settingsAttribute = typeof(T).GetCustomAttribute<CustomProjectSettingsAttribute>();

                return _settingsAttribute;
            }
        }
        
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
        
#if UNITY_EDITOR
        private static DateTime _lastFileUpdateTime = DateTime.MinValue;
#endif
        public static bool HasInstance => _instance != null;
        
        public static T Instance
        {
            get
            {
                if (_instance == null)
                    _instance = GetOrCreateSettings();
#if UNITY_EDITOR
                else if (_instance.HasBackingFileChanged())
                {
                    var newDataInstance = GetOrCreateSettings();
                    _instance.CopyValuesFrom(newDataInstance);
                    _lastFileUpdateTime = File.GetLastWriteTime(SettingsPath);
                }
#endif
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
                _lastFileUpdateTime = File.GetLastWriteTime(SettingsPath);
            }
            
            if (_lastFileUpdateTime == DateTime.MinValue)
                _lastFileUpdateTime = File.GetLastWriteTime(SettingsPath);
#else
            if (IsEditorOnly)
            {
                PLog.Error<UtilityLogger>($"ProjectSettings '{typeof(T).Name}' not available in runtime, returning null...");
                return null;
            }
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
        
#if UNITY_EDITOR
        public override bool HasBackingFileChanged()
        {
            return File.GetLastWriteTime(SettingsPath) != _lastFileUpdateTime;
        }
        
        public override void OnChanged()
        {
            base.OnChanged();
            InternalEditorUtility.SaveToSerializedFileAndForget(new[] {this}, SettingsPath, true);
            _lastFileUpdateTime = File.GetLastWriteTime(SettingsPath);
        }
#endif
    }
}