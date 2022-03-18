using System.Reflection;
using Rhinox.Lightspeed.IO;
using Rhinox.Perceptor;

namespace Rhinox.Utilities
{
    public abstract class LoadableConfigFile<T, TLoader> : ConfigFile<T>, ILoadableConfigFile 
        where T : ConfigFile<T>
        where TLoader : IConfigLoader, new()
    {
        public abstract string RelativeFilePath { get; }

        protected TLoader _loader;

        public override void Initialize()
        {
            _loader = new TLoader();
            base.Initialize();
        }

        public virtual bool Load(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !FileHelper.Exists(path))
                return false;
            
            return _loader.Load(this, path);
        }

        public virtual bool Save(string path, bool overwrite = false)
        {
            if (string.IsNullOrWhiteSpace(path) || 
                (FileHelper.Exists(path) && !overwrite))
                return false;
            
            return _loader.Save(this, path, overwrite);
        }
    }
}