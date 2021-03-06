using System;
using System.Collections.Generic;
using Rhinox.GUIUtils.Attributes;
using Sirenix.OdinInspector;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Rhinox.Utilities.Odin.Editor
{
    public class DependencyAsset
    {
        public DependencyAsset(string path)
        {
            Path = path;
            Directory = GetDirectory(Path);
            // _reference = GetLoadedReference();
        }

        public DependencyAsset(Object reference, string path = null)
        {
            _reference = reference;
            Path = path ?? AssetDatabase.GetAssetPath(reference);
            Directory = GetDirectory(Path);
        }

        [ShowInInspector, ReadOnly] public string Path { get; }
        public string Directory { get; }

        [PropertyOrder(5), ShowInInspector, InlineEditor(InlineEditorObjectFieldModes.Boxed, Expanded = true),
         HideLabel]
        private Object _reference;

        private const string AssetsPart = "Assets/";

        public string PathNoAssets
        {
            get { return Path.StartsWith(AssetsPart) ? Path.Substring(AssetsPart.Length) : Path; }
        }

        public string Name
        {
            get { return System.IO.Path.GetFileNameWithoutExtension(Path); }
        }

        [ShowInInspector, ReadOnly]
        public Type Type
        {
            get { return GetLoadedReference()?.GetType(); }
        }

        public Object GetLoadedReference()
        {
            if (_reference == null)
                _reference = AssetDatabase.LoadAssetAtPath<Object>(Path);
            return _reference;
        }

        public static string GetDirectory(Object asset)
        {
            if (asset == null) return null;
            return GetDirectory(AssetDatabase.GetAssetPath(asset));
        }

        public static string GetDirectory(string path)
        {
            return System.IO.Path.GetDirectoryName(path)?.Replace("\\", "/");
        }
    }

    public class Dependency : DependencyAsset
    {
        public Dependency(string path) : base(path)
        {
        }

        public Dependency(Object reference, string path = null) : base(reference, path)
        {
        }

        [ListDrawerSettings(IsReadOnly = true)]
        [AssetsOnly, DrawAsUnityObject]
        public readonly List<Object> Users = new List<Object>();
    }
}