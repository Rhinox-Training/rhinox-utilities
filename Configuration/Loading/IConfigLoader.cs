using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
            return LoadFileAsync(file, path, GetLoadHandler(path), callback);
        }

        public abstract bool Save(ILoadableConfigFile file, string path, bool overwrite = false);
        
        public delegate IEnumerator LoadHandler(string path);

        protected bool LoadFileAsync(ILoadableConfigFile file, string path, LoadHandler loader, Action<ILoadableConfigFile> callback = null)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;
            
            try
            {
                PLog.Debug($"Initialize IniParser for {path}");
                new ManagedCoroutine(loader(path)).OnFinished += (manual) =>
                {
                    if (!manual)
                        ParseData(file);
                    CleanUp();
                    callback?.Invoke(file);
                };
            }
            catch (Exception e)
            {
                PLog.Error($"Failed to load ini at: {path}. Reason = {e.ToString()}");
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
                // FieldInfo field = configField.Field;
                if (FindSetting(configField, out string settingsVal))
                {
                    if (ValidateField(configField, settingsVal, out object value))
                    {
                        configField.SetValue(configFile, value);
                        PLog.Debug<UtilityLogger>($"Setting {configField.Name} loaded: {settingsVal}");
                    }
                    else
                        PLog.Error<UtilityLogger>($"No load INI support for {configField.Type.FullName} with value '{settingsVal}'");
                }
                else
                {
                    if (!SupportsDynamicGroups || configField.Type != typeof(DynamicConfigFieldEntry[]))
                        continue;
                    if (!FindGroupSetting(configField, out var dynamicFields))
                        continue;
                    
                    configField.SetValue(configFile, dynamicFields.ToArray());
                    PLog.Debug<UtilityLogger>($"Dynamic Group Setting {configField.Name} loaded: group of size {dynamicFields.Length}");
                }
            }
        }

        protected virtual bool FindGroupSetting(IConfigField configField, out DynamicConfigFieldEntry[] fields)
        {
            throw new NotImplementedException();
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