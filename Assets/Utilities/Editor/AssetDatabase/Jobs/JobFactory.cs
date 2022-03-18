using System;
using System.IO;
using System.Linq;
using Rhinox.Perceptor;

namespace Rhinox.Utilities.Editor
{
    public static class JobFactory
    {
#if SHARPZIPLIB
        public static bool UseDefaultUnityImport = false;
#endif
        private const string UNITYPACKAGE_EXTENSION = ".unitypackage";
        
        public static IImportJob CreateJob(string importPath, string targetFolder, ILogger logger = null,
            params IJobProcessor[] processors)
        {
            if (!ValidateAssetPath(importPath))
                return null;

            IImportJob job = null;
            
            string ext = Path.GetExtension(importPath.ToLowerInvariant());
            if (ext.Equals(UNITYPACKAGE_EXTENSION, StringComparison.InvariantCulture))
            {
                
#if SHARPZIPLIB
                if (UseDefaultUnityImport)
                    job = new UnityPackageImportJob(importPath, logger);
                else
                    job = new BetterUnityPackageImportJob(importPath, targetFolder, logger);
#else
                job = new UnityPackageImportJob(importPath, logger);
#endif
            }
            else
            {
                // TODO: Not implemented yet
                return null;
            }

            if (job is UnityPackageImportJob) // TODO: asset import job?
                processors.Prepend(new DumpContentInFolderProcessor(importPath));

            job.LoadProcessors(processors);
            return job;
        }

        private static bool ValidateAssetPath(string packagePath, string extension = UNITYPACKAGE_EXTENSION)
        {
            if (string.IsNullOrWhiteSpace(packagePath))
                return false;

            if (!Path.IsPathRooted(packagePath))
                return false;

            return File.Exists(packagePath);
        }
    }
}