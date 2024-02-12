using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Rhinox.Utilities
{
    public abstract class ScriptableConfig<T> : ScriptableObject
        where T: ScriptableConfig<T>
    {
        private const string _configFolder = "Assets/Resources/";

        private static T _instance;
        public static T Instance => GetOrCreate();

        private static T GetOrCreate()
        {
            var instance = Resources.LoadAll<T>("").FirstOrDefault();
            
            if (instance == null)
            {
                instance = CreateInstance<T>();
#if UNITY_EDITOR
                const string baseFolder = "Assets";
                const string subFolder = "Resources";
                var folder = Path.Combine(baseFolder, subFolder);
                if (!AssetDatabase.IsValidFolder(folder))
                    AssetDatabase.CreateFolder(baseFolder, subFolder);
                
                Debug.Log($"Created {typeof(T).Name} Config in Resources.");
                AssetDatabase.CreateAsset(instance, Path.Combine(folder, typeof(T).Name + ".asset"));

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
#endif
            }

            return instance;
        }
    }
}