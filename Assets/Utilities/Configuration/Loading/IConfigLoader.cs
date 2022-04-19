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
        bool Load(ILoadableConfigFile file, string path);
        bool Save(ILoadableConfigFile file, string path, bool overwrite = false);
    }

    public abstract class ConfigLoader : IConfigLoader
    {
        private ICollection<FieldParser> _parsers;
        
        public virtual bool SupportsDynamicGroups => false;

        protected ConfigLoader()
        {
            _parsers = FieldParserHelper.GetParsers().ToList();
        }
        
        public bool Load(ILoadableConfigFile file, string path)
        {
            if (!LoadFileAsync(file, path, GetLoadHandler(path)))
                return false;
            ParseData(file);
            CleanUp();
            return true;
        }

        public abstract bool Save(ILoadableConfigFile file, string path, bool overwrite = false);
        
        public delegate IEnumerator LoadHandler(string path);

        protected bool LoadFileAsync(ILoadableConfigFile file, string path, LoadHandler loader)
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
                FieldInfo field = configField.Field;
                if (FindSetting(configField, out string settingsVal))
                {
                    if (ValidateField(configField, settingsVal, out object value))
                    {
                        field.SetValue(configFile, value);
                        PLog.Debug<UtilityLogger>($"Setting {field.Name} loaded: {settingsVal}");
                    }
                    else
                        PLog.Error<UtilityLogger>($"No load INI support for {field.FieldType.FullName}");
                }
                else
                {
                    if (!SupportsDynamicGroups || configField.Field.FieldType != typeof(DynamicConfigFieldEntry[]))
                        continue;
                    if (!FindGroupSetting(configField, out var dynamicFields))
                        continue;
                    
                    field.SetValue(configFile, dynamicFields.ToArray());
                    PLog.Debug<UtilityLogger>($"Dynamic Group Setting {field.Name} loaded: group of size {dynamicFields.Length}");
                }
            }
        }

        protected virtual bool FindGroupSetting(ConfigField configField, out DynamicConfigFieldEntry[] fields)
        {
            throw new NotImplementedException();
        }

        protected abstract bool FindSetting(ConfigField configField, out string value);
        
        private bool ValidateField(ConfigField configField, string fieldValue, out object value)
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