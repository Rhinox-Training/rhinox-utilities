using System;
using System.Collections.Generic;
using System.Linq;
using Rhinox.Lightspeed;
using UnityEngine;

namespace Rhinox.Utilities
{
    public class MaterialOverrideSource
    {
        public object Originator { get; }
        public Material Material { get; }

        public MaterialOverrideSource(object originator, Material material)
        {
            Originator = originator;
            Material = material;
        }
    }
    
    public class MaterialOverride : MonoBehaviour
    {
        private Dictionary<MeshRenderer, Material[]> _originalMaterials;
        private MeshRenderer[] _renderers;

        private List<MaterialOverrideSource> _overrides;

        private void Awake()
        {
            _renderers = GetComponentsInChildren<MeshRenderer>();
            _originalMaterials = _renderers.ToDictionary(x => x, x => x.sharedMaterials);

            _overrides = new List<MaterialOverrideSource>();
        }

        private void OnDestroy()
        {
            _overrides.Clear();
            RefreshRendering();
            _originalMaterials = null;
            _renderers = null;
        }

        public bool AddOverride(Material material, object originator)
        {
            if (_overrides.Any(x => x.Originator == originator))
                return false;

            var matOverride = new MaterialOverrideSource(originator, material);
            _overrides.Add(matOverride);
            RefreshRendering();
            return true;
        }

        public bool RemoveOverride(object originator)
        {
            int removedCount = _overrides.RemoveAll(x => x.Originator == originator);
            if (removedCount == 0)
                return false;
            RefreshRendering();
            return true;
        }

        private void RefreshRendering()
        {
            if (_overrides.Count > 0)
            {
                var overrideVal = _overrides.Last();
                foreach (var renderer in _renderers)
                    renderer.sharedMaterial = overrideVal.Material;
            }
            else
            {
                foreach (var renderer in _renderers)
                    renderer.sharedMaterials = _originalMaterials[renderer];
            }
        }
    }
    
    public static class MaterialOverrideExtensions
    {
        // TODO: multiple material override config support
        public static MaterialOverride AddMaterialOverride(this GameObject obj, Material materialOverride, object originator = null)
        {
            var outline = obj.GetOrAddComponent<MaterialOverride>();
            outline.AddOverride(materialOverride, originator);

            return outline;
        }
		
        public static void RemoveMaterialOverrides(this GameObject obj)
        {
            if (obj == null) return;

            var materialOverride = obj.GetComponent<MaterialOverride>();
            if (materialOverride == null)
                return;
            
            Utility.Destroy(materialOverride);
        }
        
        public static void RemoveMaterialOverride(this GameObject obj, object originator)
        {
            if (obj == null) return;

            var materialOverride = obj.GetComponent<MaterialOverride>();
            if (materialOverride == null)
                return;

            materialOverride.RemoveOverride(originator);
        }
    }
}