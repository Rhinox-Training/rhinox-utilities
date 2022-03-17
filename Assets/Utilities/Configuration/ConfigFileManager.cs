using System;
using System.Collections.Generic;
using System.IO;
using Rhinox.Lightspeed.IO;
using Rhinox.Lightspeed.Reflection;
using Rhinox.Perceptor;
using Rhinox.Utilities.Attributes;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using Object = UnityEngine.Object;

namespace Rhinox.Utilities
{
    [ExecutionOrder(-10000), InitializationHandler]
    public static class ConfigFileManager
    {
        private static Dictionary<Type, IConfigFile> _configFileCache;

        public static T GetConfig<T>() where T : ConfigFile<T>
        {
            return GetConfig(typeof(T)) as T;
        }

        public static IConfigFile GetConfig(Type t)
        {
            if (!t.InheritsFrom(typeof(ConfigFile<>)))
                return null;
            
            if (_configFileCache != null && _configFileCache.ContainsKey(t))
                return _configFileCache[t];

            IConfigFile config = TryRead(t);
            return config;
        }
        
        private static readonly string _configFolder = "Assets/Resources/";
        
        [OrderedRuntimeInitialize(-10000)]
        private static void Initialize()
        {
            PLog.Info($"GraphicsDevice: {SystemInfo.graphicsDeviceType.ToString()}");
            
            PLog.Debug("Loading ConfigFiles");
            _configFileCache = new Dictionary<Type, IConfigFile>();
            
            foreach (var type in GetConfigTypes())
            {
                PLog.Debug($"Loading config {type.Name}...");
                IConfigFile config = CreateIfNotExists(type);
                config.Initialize();
                _configFileCache.Add(type, config);
            }
            
            CheckAndRunLoadableTypes(_configFileCache.Values);
            
            PLog.Debug("Finished Loading ConfigFiles");
        }

        private static bool CheckAndRunLoadableTypes(ICollection<IConfigFile> configFiles)
        {
            if (configFiles == null)
                return false;
            
            foreach (IConfigFile config in configFiles)
            {
                if (config == null)
                    continue;

                if (!(config is ILoadableConfigFile loadableConfig))
                    continue;
                
                if (string.IsNullOrWhiteSpace(loadableConfig.RelativeFilePath))
                    continue;
                
                string path = Path.GetFullPath(Path.Combine(Application.streamingAssetsPath, loadableConfig.RelativeFilePath));
                if (!FileHelper.Exists(path))
                    continue;
                
                if (loadableConfig.Load(path))
                    PLog.Info<UtilityLogger>($"Loaded config file ({loadableConfig.GetType().Name}) with data at '{path}'");
                else
                    PLog.Debug<UtilityLogger>($"Unable to load config file ({loadableConfig.GetType().Name}) with data, unable to load from '{path}'...");
            }
            
            return true;
        }

        private static string GetConfigName(Type t)
        {
            string configFileName = t.Name;
            return configFileName;
        }

        private static IConfigFile TryRead(Type t)
        {
            string configFileName = GetConfigName(t);
            Object instance = Resources.Load(configFileName, t);
            if (instance == null)
                return null;
            return (IConfigFile)instance;
        }
        
        private static IConfigFile CreateIfNotExists(Type t)
        {
            IConfigFile instance = TryRead(t);
            string configFullPath = Path.Combine(_configFolder, $"{GetConfigName(t)}.asset");

            if (instance == null)
            {
                PLog.Warn($"No config found at {configFullPath}");
                ScriptableObject so = ScriptableObject.CreateInstance(t);
#if UNITY_EDITOR
                PLog.Warn($"Please configure {t.Name} at {configFullPath}");
                EnsureResourcesFolder();
                AssetDatabase.CreateAsset(so, configFullPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
#endif
                instance = (IConfigFile)so;
            }

            return instance;
        }
#if UNITY_EDITOR
        private static void EnsureResourcesFolder()
        {
            if (!FileHelper.DirectoryExists(FileHelper.GetFullPath(_configFolder, FileHelper.GetProjectPath())))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }
#endif
        public static ICollection<Type> GetConfigTypes()
        {
            List<Type> types = new List<Type>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (!type.IsClass || type.IsAbstract || type.ContainsGenericParameters)
                        continue;

                    if (!type.InheritsFrom(typeof(IConfigFile)))
                        continue;
                    types.Add(type);
                }
            }

            return types;
        }

    }
}