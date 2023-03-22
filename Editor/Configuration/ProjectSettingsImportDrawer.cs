using System;
using Rhinox.Utilities.Attributes;
using Rhinox.Utilities.Editor.Configuration;
using UnityEditor;
using UnityEngine;

namespace Rhinox.Utilities.Editor
{
    [ProjectSettingsDrawer(typeof(CustomProjectSettings<>))]
    public class ProjectSettingsImportDrawer : ProjectSettingsDrawer
    {
        protected override void OnDrawFooter()
        {
            base.OnDrawFooter();
            bool hasEditorFile = HasOldFile();
            EditorGUI.BeginDisabledGroup(!hasEditorFile);
            {
                if (GUILayout.Button("Import"))
                    TryImport();
            }
            EditorGUI.EndDisabledGroup();
        }

        private bool HasOldFile()
        {
            var assetPath = $"Assets/Editor/{_targetObject.GetType().Name}.asset";
            var settingsGuid = AssetDatabase.AssetPathToGUID(assetPath);
            return string.IsNullOrWhiteSpace(settingsGuid) || Guid.Parse(settingsGuid) != Guid.Empty;
        }

        private void TryImport()
        {
            var assetPath = $"Assets/Editor/{_targetObject.GetType().Name}.asset";
            var settings = AssetDatabase.LoadAssetAtPath<CustomProjectSettings>(assetPath);

            throw new NotImplementedException();
        }
    }
}