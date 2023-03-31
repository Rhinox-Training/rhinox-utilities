using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Rhinox.Lightspeed;
using Rhinox.Lightspeed.IO;
using Rhinox.Perceptor;
using Rhinox.Utilities.Attributes;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Rhinox.Utilities.Editor
{
    internal class CustomProjectSettingsBuilder : IPreprocessBuildWithReport
    {
        private static Dictionary<CustomProjectSettings, string> _clonePathsByInstance;
        
        public int callbackOrder => int.MinValue;
        public void OnPreprocessBuild(BuildReport report)
        {
            if (_clonePathsByInstance == null)
                _clonePathsByInstance = new Dictionary<CustomProjectSettings, string>();
            
            foreach (var projectSettingsInstance in ProjectSettingsHelper.EnumerateProjectSettings())
            {
                var attr = projectSettingsInstance.GetType().GetCustomAttribute<CustomProjectSettingsAttribute>();
                if (attr == null || !attr.RuntimeSupported)
                    continue;
                
                var clone = ScriptableObject.Instantiate(projectSettingsInstance);

                if (!ProjectSettingsHelper.TryGetSettingsPath(projectSettingsInstance.GetType(),
                        out string settingsPath))
                {
                    PLog.Warn<UtilityLogger>($"Cannot find settings path for {projectSettingsInstance.GetType().Name} projectsettings object...");
                    continue;
                }
                
                string assetPath = Path.Combine("Assets/Resources", ProjectSettingsHelper.BUILD_FOLDER, settingsPath);
                string folderName = Path.GetDirectoryName(assetPath);
                FileHelper.CreateAssetsDirectory(folderName);
                AssetDatabase.CreateAsset(clone, assetPath);

                _clonePathsByInstance.Add(clone, assetPath);
            }
        }

        [PostProcessBuild]
        public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (_clonePathsByInstance == null)
                return;
            foreach (var key in _clonePathsByInstance.Keys)
            {
                string path = _clonePathsByInstance[key];
                AssetDatabase.DeleteAsset(path);
            }
            AssetDatabase.Refresh();

            FileHelper.ClearAssetDirectory(Path.Combine("Assets/Resources", ProjectSettingsHelper.BUILD_FOLDER));

            _clonePathsByInstance.Clear();
        }
    }
}