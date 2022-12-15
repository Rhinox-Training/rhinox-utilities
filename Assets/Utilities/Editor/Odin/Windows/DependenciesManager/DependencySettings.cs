﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Rhinox.GUIUtils.Editor;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Sirenix.Utilities.Editor;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Rhinox.Utilities.Odin.Editor
{
    [TypeInfoBox("The filters below use the Regex Syntax. Visit Regex101.com for references.")]
    public class DependencySettings : ScriptableObject
    {
        [ListDrawerSettings(Expanded = true)] public List<string> FilesToIgnore;
        [ListDrawerSettings(Expanded = true)] public List<string> DirectoriesToIgnore;

        public Regex[] IgnoredFileRegexs
        {
            get { return FilesToIgnore.Select(x => new Regex(x, RegexOptions.IgnoreCase)).ToArray(); }
        }

        public Regex[] IgnoredDirectoryRegexs
        {
            get { return DirectoriesToIgnore.Select(x => new Regex(x, RegexOptions.IgnoreCase)).ToArray(); }
        }

        [OdinSerialize, PropertyOrder(10), PropertySpace(10)]
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
                {typeof(Texture), EditorIcons.ImageCollection.Active},
                {typeof(Texture2D), EditorIcons.ImageCollection.Active},
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
    }
}