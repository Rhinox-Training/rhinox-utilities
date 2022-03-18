using System.Collections.Generic;
using System.IO;
using Rhinox.Lightspeed.IO;
using Rhinox.Perceptor;
using UnityEditor;

namespace Rhinox.Utilities.Editor
{
    public class DumpContentInFolderProcessor : IJobProcessor
    {
        public string TargetPath { get; }

        private List<string> _importedFiles;
        public IReadOnlyCollection<string> ImportedFiles => _importedFiles;

        public DumpContentInFolderProcessor(string targetPath)
        {
            TargetPath = targetPath;
            _importedFiles = new List<string>();
        }

        public AssetChanges OnCompleted(IImportJob job, AssetChanges importChanges)
        {
            if (importChanges == null || importChanges.ImportedAssets == null)
            {
                PLog.Info($"Job '{job.Name}' cannot be post-processed, no ImportChanges detected. (PostProcessor: '{GetType().Name}')");
                return null;
            }
            
            if (importChanges.ImportedAssets.Count > 0)
                FileHelper.CreateAssetsDirectory(TargetPath);
            
            foreach (var newFile in importChanges.ImportedAssets)
            {
                var fileName = Path.GetFileName(newFile);
                var targetPath = Path.Combine(TargetPath, fileName);

                AssetDatabase.MoveAsset(newFile, targetPath);
                //File.Move(newFile, targetPath);
                
                _importedFiles.Add(targetPath);
            }

            AssetDatabase.Refresh();

            foreach (var newFolder in importChanges.ImportedFolders)
            {
                Directory.Delete(newFolder);
            }
            
            

            AssetChanges ac = new AssetChanges(_importedFiles);
            return ac;
            // TODO: what to do with other file changes?
        }
    }
}