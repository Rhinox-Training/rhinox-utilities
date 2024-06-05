using System;
using System.Collections.Generic;
using Rhinox.Lightspeed;
using Rhinox.Lightspeed.IO;
using Rhinox.Perceptor;
using UnityEngine;

namespace Rhinox.Utilities
{
    public class ConfigFileIniLoader : ConfigLoader
    {
        private IIniReader _reader;

        public override bool SupportsDynamicGroups => true;

        protected override LoadHandler GetLoadHandler(string path)
        {
            return (x) => IniParser.ReadAsync(x, (reader) =>
            {
                _reader = reader;
                PLog.Debug<UtilityLogger>($"Finished reading Ini: {path}");
            });
        }

        protected override bool FindSetting(IConfigField configField, out string value)
        {
            if (_reader == null || !_reader.HasSetting(configField.Section, configField.Name))
            {
                value = null;
                return false;
            }
            
            value = _reader.GetSetting(configField.Section, configField.Name);
            return true;
        }

        protected override bool FindGroupSetting(IConfigField configField, out DynamicConfigFieldEntry[] fields)
        {
            if (_reader == null)
            {
                fields = Array.Empty<DynamicConfigFieldEntry>();
                return false;
            }
            
            var keys = _reader.EnumSection(configField.Section);

            if (keys.IsNullOrEmpty())
            {
                fields = Array.Empty<DynamicConfigFieldEntry>();
                return false;
            }
            
            var fieldResult = new List<DynamicConfigFieldEntry>();
            foreach (var key in keys)
            {
                fieldResult.Add(new DynamicConfigFieldEntry()
                {
                    Name = key,
                    Value = _reader.GetSetting(configField.Section, key)
                });
            }

            fields = fieldResult.ToArray();
            return true;
        }

        public override bool Save(ILoadableConfigFile file, string path, bool overwrite = false)
        {
            if (FileHelper.Exists(path) && !overwrite)
                return false;
            
            IniParser parser = IniParser.Open(path, true, out string error);
            if (!error.IsNullOrEmpty())
                PLog.Error<UtilityLogger>(error);
            
            foreach (var configField in file.FindFields())
                parser.AddSetting(configField.Section, configField.Name, configField.GetValue(file).ToString());

            parser.SaveSettings();
            return true;
        }
    }
}
