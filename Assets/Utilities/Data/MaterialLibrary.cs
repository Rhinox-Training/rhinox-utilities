using System;
using System.Collections.Generic;
using System.Linq;
using Rhinox.Lightspeed;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Rhinox.Utilities.Data
{
    public class MaterialLibrary : Dictionary<string, Material>
    {
        public int CountMissing => Values.Count(x => x == null);

        public void Add(string name)
        {
            if (!ContainsKey(name))
                Add(name, FindAsset(name));
        }

        public new void Add(string name, Material mat)
        {
            if (ContainsKey(name))
                this[name] = mat;
            else
                base.Add(name, mat);
        }

        public Material Find(string name)
        {
            return this.GetOrDefault(name);
        }

        private Material FindAsset(string name)
        {
#if UNITY_EDITOR
            var materials = AssetDatabase.FindAssets($"t:Material {name}");
            var guid = materials.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(guid)) return null;
            var path = AssetDatabase.GUIDToAssetPath(guid);
            return AssetDatabase.LoadAssetAtPath<Material>(path);
#else
        // TODO
        return null;
#endif
        }
    }
}