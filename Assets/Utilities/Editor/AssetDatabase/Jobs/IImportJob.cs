using System;
using System.IO;
using System.Linq;
using Rhinox.Perceptor;

namespace Rhinox.Utilities.Editor
{
    public enum ImportState
    {
        Completed,
        Cancelled,
        Failed
    }
    
    public interface IImportJob
    { 
        string Name { get; }
        string ImportPath { get; }
        
        bool IsRunning { get; }
        bool IsCompleted { get; }
        AssetChanges ImportChanges { get; }

        IImportJob LoadProcessors(params IJobProcessor[] processors);
        bool Start();
    }

    public abstract class BaseImportJob : IImportJob
    {
        public class ImportEventArgs
        {
            public ImportState JobState;
            public string Info;
        }
        
        public string Name { get; }
        
        public string ImportPath { get; }
        public bool IsRunning { get; private set; }
        public bool IsCompleted { get; private set; }

        public AssetChanges ImportChanges { get; private set; }

        protected readonly ILogger _logger;

        public delegate void JobEventHandler(BaseImportJob job);
        public static event JobEventHandler GlobalStarted;
        
        public delegate void JobFinishedEventHandler(BaseImportJob job, ImportEventArgs args);
        public static event JobFinishedEventHandler GlobalCompleted;

        public IJobProcessor[] PostProcessors;

        protected BaseImportJob(string importPath, ILogger logger)
        {
            if (importPath == null) throw new ArgumentNullException(nameof(importPath));
            ImportPath = importPath;
            Name = Path.GetFileNameWithoutExtension(importPath);
            _logger = logger;
        }

        public IImportJob LoadProcessors(params IJobProcessor[] processors)
        {
            PostProcessors = processors.Where(x => x != null).ToArray();
            return this;
        }

        public bool Start()
        {
            if (IsRunning)
                return false;
                
            IsRunning = true;
            // Subscribe
            OnStart();
            
            GlobalStarted?.Invoke(this);
            return true;
        }

        protected abstract void OnStart();

        protected abstract AssetChanges GetImportChanges();

        protected virtual void TriggerCompleted(ImportState state, string errorMsg = null)
        {
            IsCompleted = true;
            IsRunning = false;
            ImportChanges = GetImportChanges(); 
            
            try
            {
                if (PostProcessors != null)
                {
                    var importChanges = ImportChanges;
                    foreach (var processor in PostProcessors)
                        importChanges = processor.OnCompleted(this, importChanges);
                }

                OnCompleted();
                
                GlobalCompleted?.Invoke(this, new ImportEventArgs()
                {
                    JobState = state,
                    Info = errorMsg
                });
            }
            catch (Exception e)
            {
                _logger?.Log($"Error on complete import job '{Name}':\n {e.ToString()}");
                IsCompleted = true;
                IsRunning = false;
                GlobalCompleted?.Invoke(this, new ImportEventArgs()
                {
                    JobState = ImportState.Failed,
                    Info = $"Msg: {errorMsg} - \nExceptionOccured:\n{e.ToString()}"
                });
            }
        }

        protected virtual void OnCompleted()
        {
            
        }
    }
}