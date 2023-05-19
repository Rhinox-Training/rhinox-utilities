using System.Collections;
using System.IO;
using System.Linq;
using Rhinox.Lightspeed;
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

        protected TLoader _loader;

        private bool _loaded;
        public bool Loaded => _loaded;

        private bool _handled;
        public bool Handled => _handled;

        protected bool _isLoading;

        public override void Initialize()
        {
            _loader = new TLoader();
            base.Initialize();
        }

        private void OnValidate()
        {
            _isLoading = false;
        }

        public virtual bool Load()
        {
            if (string.IsNullOrWhiteSpace(RelativeFilePath))
            {
                _handled = true;
                return false;
            }

            string path = Path.GetFullPath(Path.Combine(Application.streamingAssetsPath, RelativeFilePath));

            if (_isLoading)
            {
                PLog.Error<UtilityLogger>($"Cannot reload config '{this.GetType().Name}' from path '{path}', still loading from previous request...");
                return false;
            }

            _loaded = false;
            _handled = false;
            _isLoading = true;
            return _loader.Load(this, path, (config) =>
            {
                _isLoading = false;
                _handled = true;
                _loaded = true;
                LoadableConfigEvents.TriggerLoadEvent(this);
            });
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