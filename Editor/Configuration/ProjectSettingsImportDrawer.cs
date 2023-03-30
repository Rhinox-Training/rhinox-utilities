using System;
using System.IO;
using Rhinox.GUIUtils.Editor;
using Rhinox.Lightspeed;
using Rhinox.Lightspeed.Reflection;
using Rhinox.Perceptor;
using Rhinox.Utilities.Attributes;
using Rhinox.Utilities.Editor.Configuration;
using UnityEditor;
using UnityEngine;

namespace Rhinox.Utilities.Editor
{
    [ProjectSettingsDrawer(typeof(CustomProjectSettings<>))]
    public class ProjectSettingsImportDrawer : ProjectSettingsDrawer
    {
        private string _oldAssetPath = null;
        protected string OldAssetPath
        {
            get
            {
                if (_oldAssetPath == null)
                    _oldAssetPath = $"Assets/Editor/{_targetObject.GetType().Name}.asset";
                return _oldAssetPath;
            }
        }

        protected override void OnDrawFooter()
        {
            base.OnDrawFooter();
            bool hasOldFile = HasOldFile();
            if (hasOldFile)
            {
                if (GUILayout.Button($"Import Settings at '{OldAssetPath}'"))
                {
                    if (TryImport())
                    {
                        EditorInputDialog.Create("Notice",
                                $"Do you want to remove the old file at '{OldAssetPath}' from the project?")
                            .OnAccept(() =>
                            {
                                if (hasOldFile)
                                    RemoveOldFile();
                            })
                            .Show();
                    }
                }
            }
        }

        private bool HasOldFile()
        {
            var assetPath = OldAssetPath;
            var settingsGuid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrWhiteSpace(settingsGuid) || Guid.Parse(settingsGuid) == Guid.Empty)
                return false;
            return File.Exists(assetPath);
        }

        private bool TryImport()
        {
            var settings = AssetDatabase.LoadAssetAtPath<CustomProjectSettings>(OldAssetPath);
            if (settings == null)
                return false;
            return _targetObject.CopyValuesFrom(settings);
        }

        private void RemoveOldFile()
        {
            try
            {
                AssetDatabase.DeleteAsset(OldAssetPath);
            }
            catch (Exception e)
            {
                PLog.Error<UtilityLogger>($"Failed to delete asset at '{OldAssetPath}', reason: {e.ToString()}");
            }
        }
    }
}