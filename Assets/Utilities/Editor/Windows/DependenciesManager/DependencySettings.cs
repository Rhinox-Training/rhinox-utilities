using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Rhinox.GUIUtils.Editor;
using Rhinox.Lightspeed;
using Sirenix.OdinInspector;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Rhinox.Utilities.Editor
{
    [TypeInfoBox("The filters below use the Regex Syntax. Visit Regex101.com for references.")]
    public class DependencySettings : ISerializationCallbackReceiver
    {
        [ListDrawerSettings(Expanded = true)] public List<string> FilesToIgnore = new List<string>();
        [ListDrawerSettings(Expanded = true)] public List<string> DirectoriesToIgnore = new List<string>();

        public Regex[] IgnoredFileRegexs
        {
            get { return FilesToIgnore.Select(x => new Regex(x, RegexOptions.IgnoreCase)).ToArray(); }
        }

        public Regex[] IgnoredDirectoryRegexs
        {
            get { return DirectoriesToIgnore.Select(x => new Regex(x, RegexOptions.IgnoreCase)).ToArray(); }
        }

        [PropertyOrder(10), PropertySpace(10)]
        public Dictionary<Type, Texture> IconMapper { get; private set; }

        [Button(ButtonSizes.Medium, ButtonStyle.FoldoutButton)]
        public void Test(string path)
        {
            if (IsAnyMatch(path, IgnoredFileRegexs, IgnoredDirectoryRegexs))
                Debug.Log("Match was found: This path will be ignored!");
            else Debug.Log("Match was not found: This path will be included.");
        }

        public static bool IsAnyMatch(string path, Regex[] filesToIgnore, Regex[] directoriesToIgnore)
        {
            string filename = Path.GetFileName(path);

            if (filesToIgnore != null && filesToIgnore.Any(r => r.IsMatch(filename)))
                return true;

            if (directoriesToIgnore != null && directoriesToIgnore.Any(r => r.IsMatch(path)))
                return true;

            return false;
        }

        [Button("Reset to defaults", ButtonSizes.Medium)]
        public void Load()
        {
            IconMapper = new Dictionary<Type, Texture>
            {
                {typeof(Material), UnityIcon.InternalIcon("Material Icon")},
                {typeof(Shader), UnityIcon.InternalIcon("Shader Icon")},
                // {typeof(SubstanceArchive), UnityIcon.AssetIcon("SDDOC") },
                {typeof(Texture), UnityIcon.AssetIcon("Fa_Images")},
                {typeof(Texture2D), UnityIcon.AssetIcon("Fa_Images")},
                {typeof(Sprite), UnityIcon.InternalIcon("d_Sprite Icon")},
                {typeof(AudioClip), UnityIcon.InternalIcon("AudioClip Icon")},
                {typeof(GameObject), UnityIcon.InternalIcon("GameObject Icon")},
                {typeof(ScriptableObject), UnityIcon.InternalIcon("d_ScriptableObject Icon")},
                {typeof(MonoScript), UnityIcon.InternalIcon("cs Script Icon")},
                {typeof(Font), UnityIcon.InternalIcon("Font Icon")},
#if TEXT_MESH_PRO
                {typeof(TMP_FontAsset), UnityIcon.InternalIcon("Font Icon")}
#endif
            };

            FilesToIgnore = new List<string>
            {
                "LightingData",
                "NavMesh",
                "ReflectionProbe",
                "Lightmap-",
                @".*\.asmdef",
                ".*Generated.*"
            };

            DirectoriesToIgnore = new List<string>
            {
                "/Plugins/",
                "/TextMesh Pro/",
                "/VRTK/",
                "/SteamVR/",
                "/PATCH/",
                "/Resonai.*/",
                "Packages/"
            };
        }


        // =============================================================================================================
        // Serialization
        [Serializable]
        private struct IconEntry
        {
            public SerializableType TypeKey;
            public Texture Value;
        }

        [SerializeField] private List<IconEntry> _backingSerializationDict;
        
        public void OnBeforeSerialize()
        {
            if (_backingSerializationDict == null)
                _backingSerializationDict = new List<IconEntry>();

            if (IconMapper != null)
            {
                _backingSerializationDict.Clear();
                foreach (var entry in IconMapper)
                {
                    _backingSerializationDict.Add(new IconEntry()
                    {
                        TypeKey = new SerializableType(entry.Key),
                        Value = entry.Value
                    });
                }
            }
        }

        public void OnAfterDeserialize()
        {
            if (_backingSerializationDict != null)
            {
                if (IconMapper == null)
                    IconMapper = new Dictionary<Type, Texture>();
                
                IconMapper.Clear();
                foreach (var entry in _backingSerializationDict)
                {
                    IconMapper.Add(entry.TypeKey, entry.Value);
                }
            }
        }
    }
}