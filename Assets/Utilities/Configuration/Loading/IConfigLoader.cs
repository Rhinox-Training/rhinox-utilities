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
        bool Load(ILoadableConfigFile file, string path, Action<ILoadableConfigFile> callback = null);
        bool Save(ILoadableConfigFile file, string path, bool overwrite = false);
    }

    public abstract class ConfigLoader : IConfigLoader
    {
        protected ICollection<FieldParser> _parsers;
        
        public virtual bool SupportsDynamicGroups => false;
        
        protected ConfigLoader()
        {
            _parsers = FieldParserHelper.GetParsers().ToList();
        }
        
        public bool Load(ILoadableConfigFile file, string path, Action<ILoadableConfigFile> callback = null)
        {
            if (!string.IsNullOrWhiteSpace(path) && FileHelper.Exists(path))
                return LoadFileAsync(file, path, GetLoadHandler(path), callback);
            try
            {
                ParseCommandLineArgs(file);
                callback?.Invoke(file);
                return true;
            }
            catch (Exception e)
            {
                PLog.Error($"Failed to load config \"{file.GetType().GetNiceName()}\". Reason = {e}");
                return false;
            }
        }

        public abstract bool Save(ILoadableConfigFile file, string path, bool overwrite = false);
        
        public delegate Task LoadHandler(string path);

        protected bool LoadFileAsync(ILoadableConfigFile file, string path, LoadHandler loader, Action<ILoadableConfigFile> callback = null)
        {
            try
            {
                PLog.Debug($"Initialize ConfigLoader for {path}");
                var loaderTask = loader(path);
                var awaiter = loaderTask.GetAwaiter();
                awaiter.OnCompleted(() =>
                {
                    ParseData(file);
                    CleanUp();
                    callback?.Invoke(file);
                });
            }
            catch (Exception e)
            {
                PLog.Error($"Failed to load config file at: {path}. Reason = {e}");
                return false;
            }

            return true;
        }

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

            // Parse command line args in a second path since its values take priority
            ParseCommandLineArgs(configFile);
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