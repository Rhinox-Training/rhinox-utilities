using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Rhinox.Utilities
{
    public abstract class ConfigFile<T> : ScriptableObject, IConfigFile where T : ConfigFile<T>
    {
        private static T _instance;
        private static readonly string _configFileName = typeof(T).Name;
        
        public static T Instance
        {
            get
            {
                if (_instance == null)
                    _instance = ConfigFileManager.GetConfig<T>();
                return _instance;
            }
        }
        
        public virtual void Initialize()
        {
            
        }

        public virtual ICollection<IConfigField> FindFields()
        {
            Type configType = GetType();
            List<IConfigField> list = new List<IConfigField>();
            var fieldInfos = configType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            foreach (var fi in fieldInfos)
            {
                var sectionAttr = fi.GetCustomAttribute<ConfigSectionAttribute>();
                string section = null;
                if (sectionAttr != null && !string.IsNullOrWhiteSpace(sectionAttr.Section))
                    section = sectionAttr.Section.Trim();

                var field = new ConfigField(fi, section);
                list.Add(field);
            }
            return list;
        }
    }
}