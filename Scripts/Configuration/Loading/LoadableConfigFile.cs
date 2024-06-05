using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Rhinox.Lightspeed;
using Rhinox.Lightspeed.Collections;
using Rhinox.Lightspeed.IO;
using Rhinox.Perceptor;
using UnityEngine;

namespace Rhinox.Utilities
{
    public abstract class LoadableConfigFile<T, TLoader> : ConfigFile<T>, ILoadableConfigFile
        where T : ConfigFile<T>
        where TLoader : IConfigLoader, new()
    {
        public abstract string RelativeFilePath { get; }

        private PriorityQueue<int, string> _fileTiers;

        protected TLoader _loader;

        private bool _loaded;
        public bool Loaded => _loaded;

        private bool _handled;
        public bool Handled => _handled;

        protected bool _isLoading;

        public override void Initialize()
        {
            _loader = new TLoader();
            _fileTiers = new PriorityQueue<int, string>();
            base.Initialize();
        }

        private void OnValidate()
        {
            _isLoading = false;
        }

        /// Note: Higher priority = Evaluated last (and thus overrides lower priority values)
        protected void AddTier(string filePath, int priority = 0)
        {
            _fileTiers.Enqueue(filePath, priority);
        }

        protected string GetRootedPath(string path, string root = null)
        {
            if (string.IsNullOrWhiteSpace(path))
                return path;

            if (Path.IsPathRooted(path))
                return path;
            
            if (root == null)
                root = Application.streamingAssetsPath;
            
            path = Path.Combine(root, path);
            return Path.GetFullPath(path);
        }
        
        
        public virtual string GetDefaultFilePath()
        {
            return GetRootedPath(RelativeFilePath);
        }

        /// Things added first, will be overridden by tiers added after (if applicable)
        public virtual void RegisterFileTiers()
        {
            var path = GetDefaultFilePath();
            if (!path.IsNullOrEmpty())
                AddTier(path);
        }

        public virtual bool Load()
        {
            if (_isLoading)
            {
                PLog.Error<UtilityLogger>($"Cannot reload config '{this.GetType().Name}', still loading from previous request...");
                return false;
            }
            
            _loaded = false;
            _handled = false;
            _isLoading = true;
            
            RegisterFileTiers();

            // Need to do this async so we have proper ordering
            LoadConfigTiers(() => {
                _isLoading = false;
                _handled = true;
                _loaded = true;
                LoadableConfigEvents.TriggerLoadEvent(this);
            });
            
            return !_fileTiers.IsNullOrEmpty();
        }

        protected async void LoadConfigTiers(Action callback = null)
        {
            foreach (var path in _fileTiers)
            {
                bool loaded = await _loader.LoadAsync(this, path);
                if (loaded)
                    PLog.Info<UtilityLogger>($"Finished loading config '{this.GetType().Name}' from path '{path}'");
            }
            
            PLog.Info<UtilityLogger>($"Checking command line args for config '{this.GetType().Name}'");
            _loader.LoadFromCommandLine(this);
            
            callback?.Invoke();
        }

        public virtual bool Save(string path, bool overwrite = false)
        {
            Initialize();

            if (string.IsNullOrWhiteSpace(path) ||
                (FileHelper.Exists(path) && !overwrite))
                return false;

            return _loader.Save(this, path, overwrite);
        }
    }
}