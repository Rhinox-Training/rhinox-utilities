using System;
using System.Collections;
using System.Collections.Generic;
using Rhinox.Lightspeed.IO;
using Rhinox.Perceptor;
using Unity.EditorCoroutines.Editor;
using UnityEngine;

namespace Rhinox.Utilities.Editor
{
    public static class AssetDatabaseExt
    {
        private static Dictionary<string, IImportJob> _importJobs;
        private static Queue<IImportJob> _jobQueue;
        private static bool _initialized = false;
        private static Rhinox.Perceptor.ILogger _logger;
        private static EditorCoroutine _jobQueueProcessor;
        private static IImportJob _runningJob;

        public delegate void JobEventHandler(IImportJob job);

        public static event JobEventHandler JobStarted;
        public static event JobEventHandler JobFinished;

        public static void LoadLogger(Rhinox.Perceptor.ILogger logger)
        {
            _logger = logger;
        }
        
        private static bool TryInitialize()
        {
            if (_initialized)
                return true;

            if (_jobQueue == null)
                _jobQueue = new Queue<IImportJob>();
            
            if (_importJobs == null)
                _importJobs = new Dictionary<string, IImportJob>();

            if (_jobQueueProcessor == null)
                _jobQueueProcessor = EditorCoroutineUtility.StartCoroutineOwnerless(ParseQueue());

            BaseImportJob.GlobalCompleted += OnGlobalCompleted;

            _initialized = true;
            return true;
        }

        public static bool ImportAsset(string assetPath, string targetFolder, params IJobProcessor[] processors)
        {
            var importJob = JobFactory.CreateJob(assetPath, targetFolder, _logger, processors);
            return RegisterJob(importJob);
        }

        public static bool ImportAssets(ICollection<string> files, string targetFolder, Action<AssetChanges> callback = null)
        {
            var _completedImportProcessor = new CompletedImportProcessor(files.Count, callback);
            foreach (var file in files)
            {
                string fullPath = FileHelper.GetFullPath(file, FileHelper.GetProjectPath());
                ImportAsset(fullPath, targetFolder, _completedImportProcessor);
            }


            // TODO: this does not check if jobs can be added correctly, what if one fails to be queued
            return true;
        }
        
        private class CompletedImportProcessor : IJobProcessor
        {
            private int _completeCount = 0;
            public int CompleteCount => _completeCount;
            private readonly object _lockObject = new object();
            private readonly int _targetCount;
            private readonly Action<AssetChanges> _callback;
            private bool _firedEvent = false;

            private readonly List<string> _importedAssets;

            public bool IsComplete => _completeCount >= _targetCount;

            public CompletedImportProcessor(int targetCount, Action<AssetChanges> callback)
            {
                _targetCount = targetCount;
                _callback = callback;
                _importedAssets = new List<string>();
            }
            
            public AssetChanges OnCompleted(IImportJob job, AssetChanges importChanges)
            {
                lock (_lockObject)
                {
                    ++_completeCount;
                    PLog.Info($"AssetDatabase.CompletionCollection: completed import stage {_completeCount} out of {_targetCount}");
                    if (importChanges != null && importChanges.ImportedAssets != null)
                    {
                        foreach (var importedAsset in importChanges.ImportedAssets)
                        {
                            if (CaseInsensitiveContains(_importedAssets, importedAsset))
                            {
                                PLog.Warn($"Asset '{importedAsset}' was doubly imported, '{job.Name}', skipping... add");
                                continue;
                            }
                            _importedAssets.Add(importedAsset);
                        }

                        PLog.Info($"AssetDatabase.CompletionCollection: added {importChanges.ImportedAssets.Count} assets (total: {_importedAssets.Count})");
                    }

                    if (CompleteCount >= _targetCount && !_firedEvent)
                    {
                        try
                        {
                            _callback?.Invoke(new AssetChanges(_importedAssets));
                        }
                        catch (Exception e)
                        {
                            PLog.Error($"AssetDatabase.ImportAssets ({job.Name}) callback failed: {e.ToString()}");
                        }
                    }
                }

                return importChanges;
            }
            
            // TODO: move to utils?
            private bool CaseInsensitiveContains(ICollection<string> list, string entry)
            {
                if (entry == null)
                    return list.Contains(null);
                
                foreach (var listEntry in list)
                {
                    if (listEntry != null && listEntry.Equals(entry, StringComparison.InvariantCultureIgnoreCase))
                        return true;
                }

                return false;
            }
        }

        private static bool RegisterJob(IImportJob job)
        {
            if (job == null)
                return false;
            
            TryInitialize();
            
            if (_importJobs.ContainsKey(job.Name))
            {
                Log($"[ERROR] Cannot register import job '{job.Name}', is currently queued...");
                return false;
            }

            Log($"Enqueuing job {job.Name}, current wait time: {_jobQueue.Count}");

            _importJobs.Add(job.Name, job);
            _jobQueue.Enqueue(job);
            return true;
        }
        
        private static void OnGlobalCompleted(IImportJob job, BaseImportJob.ImportEventArgs args)
        {
            if (_runningJob != job || !_importJobs.ContainsKey(job.Name))
            {
                Log($"[ERROR] No job running for asset '{job.Name}', cancelling clean up");
                return;
            }

            Log($"Job '{job.Name}' completed, cleaning up cache");
            _runningJob = null;
            _importJobs.Remove(job.Name);
        }

        private static void Log(string logLine)
        {
            if (_logger != null)
                _logger.Log(logLine);
            else
                Debug.Log(logLine);
        }
        
        private static IEnumerator ParseQueue()
        {
            while (true)
            {
                if (_jobQueue != null && _jobQueue.Count > 0)
                {
                    if (_runningJob == null || _runningJob.IsCompleted)
                    {
                        if (_runningJob != null)
                            JobFinished?.Invoke(_runningJob);
                        
                        _runningJob = _jobQueue.Dequeue();
                        JobStarted?.Invoke(_runningJob);
                        _runningJob.Start();
                    }
                }
                yield return null;
            }
        }
    }
}