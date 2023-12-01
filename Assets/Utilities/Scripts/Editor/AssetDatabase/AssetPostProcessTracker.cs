using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Rhinox.Lightspeed.IO;
using UnityEditor;

namespace Rhinox.Utilities.Editor
{
    public class AssetChanges
    {
        private List<string> _importedAssets;
        private List<string> _createdFolderStructure;

        public IReadOnlyCollection<string> ImportedAssets =>
            _importedAssets ?? (IReadOnlyCollection<string>)Array.Empty<string>();

        public IReadOnlyCollection<string> ImportedFolders =>
            _createdFolderStructure ?? (IReadOnlyCollection<string>)Array.Empty<string>();

        public AssetChanges()
        {
        }

        public AssetChanges(ICollection<string> assets)
        {
            AddImportedAssets(assets);
        }

        public AssetChanges Clone()
        {
            return new AssetChanges()
            {
                _importedAssets = this._importedAssets?.ToList(),
                _createdFolderStructure = this._createdFolderStructure?.ToList()
            };
        }

        public void AddImportedAssets(ICollection<string> assets)
        {
            if (assets == null)
                return;

            if (_importedAssets == null)
                _importedAssets = new List<string>();
            if (_createdFolderStructure == null)
                _createdFolderStructure = new List<string>();

            foreach (var asset in assets)
            {
                if (Directory.Exists(FileHelper.GetFullPath(asset, FileHelper.GetProjectPath())))
                    _createdFolderStructure.Add(asset);
                else
                    _importedAssets.Add(asset);
            }
        }
    }

    public class AssetPostProcessTracker : AssetPostprocessor
    {
        private static AssetChanges _changeCache;
        private static bool _active = false;

        public static bool StartRecording()
        {
            if (_active)
                return false; // TODO: should we refresh the recording cache?

            _changeCache = new AssetChanges();
            _active = true;
            return true;
        }

        public static AssetChanges StopRecording()
        {
            StopRecording(out AssetChanges changes);
            return changes;
        }

        public static bool StopRecording(out AssetChanges changes)
        {
            if (!_active)
            {
                changes = null;
                return false;
            }

            changes = _changeCache.Clone();
            _changeCache = null;
            _active = false;
            return true;
        }

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (_active && _changeCache != null)
            {
                if (importedAssets != null && importedAssets.Length > 0)
                    _changeCache.AddImportedAssets(importedAssets);
            }
        }
    }
}