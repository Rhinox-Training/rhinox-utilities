using System;
using System.IO;
using Rhinox.Perceptor;
using UnityEditor;

namespace Rhinox.Utilities.Editor
{
    public class UnityPackageImportJob : BaseImportJob
    {
        public string PackageName => Name;

        public UnityPackageImportJob(string importPath, ILogger logger)
            : base(importPath, logger)
        {
        }

        protected override void OnStart()
        {
            if (!AssetPostProcessTracker.StartRecording())
                _logger?.Log($"Asset '{Name}' import warning: cannot start recording asset changes");
            
            // Subscribe
            AssetDatabase.importPackageStarted += OnPackageImportStarted;
            AssetDatabase.importPackageCancelled += OnPackageImportCancelled;
            AssetDatabase.importPackageFailed += OnPackageImportFailed;
            AssetDatabase.importPackageCompleted += OnPackageImportCompleted;
            
            _logger?.Log($"AssetDatabase.ImportPackage({ImportPath}, interactive: {false})");
            AssetDatabase.ImportPackage(ImportPath, false);
        }

        protected override AssetChanges GetImportChanges()
        {
            return AssetPostProcessTracker.StopRecording();
        }

        protected override void OnCompleted()
        {
            base.OnCompleted();
            AssetDatabase.Refresh();
        }

        private void OnPackageImportStarted(string packagename)
        {
            if (!CheckJob(packagename))
                return;

            _logger?.Log($"Package import started of '{ImportPath}'");
        }

        private void OnPackageImportCancelled(string packagename)
        {
            if (!CheckJob(packagename))
                return;
            
            HandleFinished(ImportState.Cancelled);
        }
        
        private void OnPackageImportFailed(string packagename, string errormessage)
        {
            if (!CheckJob(packagename))
                return;

            HandleFinished(ImportState.Failed, errormessage);
        }

        private void OnPackageImportCompleted(string packagename)
        {
            if (!CheckJob(packagename))
                return;
            
            HandleFinished(ImportState.Completed);
        }

        private void HandleFinished(ImportState state, string errormessage = null)
        {
            if (state == ImportState.Completed)
                AssetDatabase.Refresh();
            _logger?.Log($"Package import {state.ToString().ToLowerInvariant()} of '{ImportPath}'");
            TriggerCompleted(state, errormessage);

            AssetDatabase.importPackageStarted -= OnPackageImportStarted;
            AssetDatabase.importPackageCancelled -= OnPackageImportCancelled;
            AssetDatabase.importPackageFailed -= OnPackageImportFailed;
            AssetDatabase.importPackageCompleted -= OnPackageImportCompleted;
        }
        
        private bool CheckJob(string packagename)
        {
            // NOTE: cause you know unity packagename = absolute path without extension
            return ImportPath.Equals(packagename + ".unitypackage", StringComparison.InvariantCulture);
        }
    }
}