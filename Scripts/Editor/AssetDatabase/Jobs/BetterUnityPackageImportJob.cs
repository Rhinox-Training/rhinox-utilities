#if SHARPZIPLIB
using System;
using System.Linq;
using Rhinox.Perceptor;
using ILogger = Rhinox.Perceptor.ILogger;

namespace Rhinox.Utilities.Editor
{
    public class BetterUnityPackageImportJob : BaseImportJob
    {
        public string TargetPath { get; }
        private string[] _imports;
        
        public BetterUnityPackageImportJob(string importPath, string targetPath, ILogger logger) : base(importPath, logger)
        {
            TargetPath = targetPath;
        }

        protected override void OnStart()
        {
            _logger?.Log($"BetterUnityPackageImporter.ImportPackage({ImportPath}, {TargetPath}, ImportMode.Flatten, OverwriteMode.OverwriteReplace)");
            if (!BetterUnityPackageImporter.ImportPackage(ImportPath, TargetPath, mode: BetterUnityPackageImporter.ImportMode.Flatten,
                overwrite: BetterUnityPackageImporter.OverwriteMode.OverwriteReplace, response: OnComplete))
            {
                _imports = Array.Empty<string>();
                TriggerCompleted(ImportState.Failed);
            }
        }

        protected override AssetChanges GetImportChanges()
        {
            return new AssetChanges(_imports ?? Array.Empty<string>());
        }

        protected virtual void OnComplete(UnityPackageResponse unityPackageResponse)
        {
            _imports = unityPackageResponse.ImportedAssets.ToArray();
            TriggerCompleted(ImportState.Completed);
            
            foreach (var asset in _imports)
                PLog.Info($"Imported asset: {asset}");
        }
    }
}
#endif