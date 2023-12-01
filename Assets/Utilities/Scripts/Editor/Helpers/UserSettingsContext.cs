#if UNITY_2020_1_OR_NEWER
using System;
using System.Configuration;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditorInternal;

namespace Rhinox.Utilities.Editor
{
    public class UserSettingsContext<T> where T : ScriptableObject
    {
        private const string USERSETTINGS_PATH = "UserSettings";

        private static string FilePath => Path.Combine(USERSETTINGS_PATH, $"{typeof(T).Name}.asset");

        public static UserSettingsContext<T> Create()
        {
            var serializedFiles = InternalEditorUtility.LoadSerializedFileAndForget(FilePath);
            
            T scriptableObject = null;
            if (serializedFiles != null)
                scriptableObject = serializedFiles.OfType<T>().FirstOrDefault();

            bool needsSave = false;
            if (scriptableObject == null)
            {
                scriptableObject = ScriptableObject.CreateInstance<T>();
                needsSave = true;
            }
            
            
            var instance = new UserSettingsContext<T>(scriptableObject, needsSave);
            return instance;
        }
        
        
        private readonly T _scriptableObj;
        
        private UserSettingsContext(T scriptableObject, bool needsSave = false)
        {
            if (scriptableObject == null) throw new ArgumentNullException(nameof(scriptableObject));
            _scriptableObj = scriptableObject;
            if (needsSave)
                Save();
        }

        public void Save()
        {
            InternalEditorUtility.SaveToSerializedFileAndForget(new [] { _scriptableObj }, FilePath, true);
        }
        
    }
}
#endif