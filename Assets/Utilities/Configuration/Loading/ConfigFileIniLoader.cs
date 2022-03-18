using System;
using System.Reflection;
using Rhinox.Lightspeed.IO;
using Rhinox.Perceptor;

namespace Rhinox.Utilities
{
    public class ConfigFileIniLoader : ConfigLoader
    {
        private IIniReader _reader;

        protected override LoadHandler GetLoadHandler(string path)
        {
            return (x) => IniParser.ReadAsync(x, (reader) =>
            {
                _reader = reader;
                PLog.Info($"Finished loading Ini: {path}");
            });
        }

        protected override bool FindSetting(ConfigField configField, out string value)
        {
            if (_reader == null)
            {
                value = null;
                return false;
            }
            
            FieldInfo field = configField.Field;
            value = _reader.GetSetting(configField.Section, field.Name);
            return true;
        }

        public override bool Save(ILoadableConfigFile file, string path, bool overwrite = false)
        {
            if (FileHelper.Exists(path) && !overwrite)
                return false;
            
            IniParser parser = IniParser.Open(path, true);
            foreach (var configField in file.FindFields())
            {
                FieldInfo field = configField.Field;
                parser.AddSetting(configField.Section, field.Name, field.GetValue(file).ToString());
            }

            parser.SaveSettings();
            return true;
        }
    }
}
