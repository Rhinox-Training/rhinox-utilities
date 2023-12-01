using UnityEngine.EventSystems;

namespace Rhinox.Utilities
{
    /// <summary>
    /// When implementing ILoadableConfigFile remember to call the config loaded events
    /// </summary>
    public static class LoadableConfigEvents
    {
        public delegate void LoadEventHandler(ILoadableConfigFile sender);
        public static event LoadEventHandler ConfigLoaded;

        public static void TriggerLoadEvent(ILoadableConfigFile sender)
        {
            ConfigLoaded?.Invoke(sender);
        }
    }
    
    public interface ILoadableConfigFile : IConfigFile
    {
        string RelativeFilePath { get; }
        
        bool Loaded { get; }
        bool Handled { get; }

        bool Load();

        bool Save(string path, bool overwrite = false);
    }
}