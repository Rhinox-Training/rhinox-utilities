using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Rhinox.Lightspeed;
using Rhinox.Lightspeed.IO;
using Rhinox.Lightspeed.Reflection;
using Rhinox.Perceptor;

namespace Rhinox.Utilities
{
    public interface IConfigLoader
    {
        Task<bool> LoadAsync(ILoadableConfigFile file, string path);
        bool Load(ILoadableConfigFile file, string path, Action<ILoadableConfigFile> callback = null);
        bool LoadFromCommandLine(ILoadableConfigFile file);
        
        bool Save(ILoadableConfigFile file, string path, bool overwrite = false);
    }

    public abstract class ConfigLoader : IConfigLoader
    {
        public virtual bool SupportsDynamicGroups => false;
        
        protected ICollection<FieldParser> _parsers;

        protected delegate Task LoadHandler(string path);
        
        protected ConfigLoader()
        {
            _parsers = FieldParserHelper.GetParsers().ToList();
        }

        private bool Validate(string path)
        {
            return !string.IsNullOrWhiteSpace(path) && FileHelper.Exists(path);
        }
        
        public async Task<bool> LoadAsync(ILoadableConfigFile file, string path)
        {
            if (!Validate(path))
            {
                PLog.Info<UtilityLogger>($"Skipped config file at: {path}. Reason = Does not exist.");
                return false;
            }
            
            try
            {
                PLog.Debug<UtilityLogger>($"Initialize ConfigLoader for {path}");
                var loader = GetLoadHandler(path);
                await loader(path);
                ParseData(file);
                CleanUp();
                return true;
            }
            catch (Exception e)
            {
                PLog.Error<UtilityLogger>($"Failed to load config file at: {path}. Reason = {e}");
                return false;
            }
        }
        
        protected async void LoadAsync(ILoadableConfigFile file, string path, Action<ILoadableConfigFile> callback)
        {
            await LoadAsync(file, path);
            callback?.Invoke(file);
        }

        public bool Load(ILoadableConfigFile file, string path, Action<ILoadableConfigFile> callback = null)
        {
            if (!Validate(path))
            {
                PLog.Info<UtilityLogger>($"Skipped config file at: {path}. Reason = Does not exist.");
                return false;
            }
            
            LoadAsync(file, path, callback);
            return true;
        }

        public bool LoadFromCommandLine(ILoadableConfigFile file)
        {
            try
            {
                ParseCommandLineArgs(file);
                return true;
            }
            catch (Exception e)
            {
                PLog.Error<UtilityLogger>($"Failed to load config \"{file.GetType().GetNiceName()}\". Reason = {e}");
                return false;
            }
        }

        public abstract bool Save(ILoadableConfigFile file, string path, bool overwrite = false);

        protected abstract LoadHandler GetLoadHandler(string path);

        protected virtual void CleanUp()
        {
            
        }

        protected virtual void ParseData(ILoadableConfigFile configFile)
        {
            var fields = configFile.FindFields();
            foreach (var configField in fields)
            {
                if (FindSetting(configField, out string settingsVal))
                {
                    TrySetValue(configFile, configField, settingsVal);
                }
                else if (SupportsDynamicGroups && configField.Type == typeof(DynamicConfigFieldEntry[]))
                {
                    if (!FindGroupSetting(configField, out var dynamicFields))
                        continue;
                    
                    configField.SetValue(configFile, dynamicFields.ToArray());
                    PLog.Debug<UtilityLogger>($"Dynamic Group Setting {configField.Name} loaded: group of size {dynamicFields.Length}");
                }
            }
        }

        protected virtual void ParseCommandLineArgs(ILoadableConfigFile configFile)
        {
            var fields = configFile.FindFields();
            foreach (var configField in fields)
            {
                // Handle command line args
                var commandLineAttr = configField.GetCustomAttribute<ConfigCommandArgAttribute>();
                if (commandLineAttr != null)
                {
                    if (Utility.TryGetCommandLineArg(out string argValue, commandLineAttr.ArgumentKey))
                    {
                        TrySetValue(configFile, configField, argValue);
                        continue;
                    }
                }
            }
        }

        private void TrySetValue(ILoadableConfigFile configFile, IConfigField configField, string settingsVal)
        {
            if (ValidateField(configField, settingsVal, out object value))
            {
                configField.SetValue(configFile, value);
                PLog.Debug<UtilityLogger>($"Setting {configField.Name} loaded: {settingsVal}");
            }
            else
                PLog.Error<UtilityLogger>($"No load support for {configField.Type.FullName} with value '{settingsVal}'");
        }

        protected virtual bool FindGroupSetting(IConfigField configField, out DynamicConfigFieldEntry[] fields)
        {
            fields = Array.Empty<DynamicConfigFieldEntry>();
            return false;
        }

        protected abstract bool FindSetting(IConfigField configField, out string value);
        
        protected virtual bool ValidateField(IConfigField configField, string fieldValue, out object value)
        {
            if (_parsers == null)
            {
                value = null;
                return false;
            }

            foreach (var parser in _parsers)
            {
                if (!parser.CanParse(configField))
                    continue;
                
                if (parser.ParseValue(configField, fieldValue, out object parserValue))
                {
                    value = parserValue;
                    return true;
                }
            }
            
            value = null;
            return false;
        }
    }
}