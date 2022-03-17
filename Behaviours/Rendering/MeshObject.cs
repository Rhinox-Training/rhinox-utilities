using System;
using Rhinox.Lightspeed;
using UnityEngine;

namespace Rhinox.Utilities
{
    [ExecuteInEditMode]
    public class MeshObject : MonoBehaviour
    {
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;

        private bool _initialized;

        public static MeshObject GetOrCreateChild(Transform parent, string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new InvalidOperationException("Child object should have a name...");
            Transform child = parent.Find(name);
            if (child == null)
            {
                child = new GameObject(name).transform;
                child.SetParent(parent); // Set self as parent
            }
            
            MeshObject mo = child.GetOrAddComponent<MeshObject>();
            mo.Initialize();
            return mo;
        }
        
        public static MeshObject GetOrCreateChild(Transform parent, string name, Mesh sharedMesh, params Material[] materials)
        {
            var meshObject = GetOrCreateChild(parent, name);
            meshObject.SetSharedMesh(sharedMesh);
            meshObject.SetSharedMaterials(materials);
            return meshObject;
        }

        private void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (_initialized)
                return;
            
            _meshFilter = gameObject.GetOrAddComponent<MeshFilter>();
            _meshRenderer = gameObject.GetOrAddComponent<MeshRenderer>();
            _initialized = true;
        }

        public void SetSharedMaterial(Material m)
        {
            _meshRenderer.sharedMaterial = m;
        }
        
        public void SetSharedMaterials(params Material[] materials)
        {
            _meshRenderer.sharedMaterials = materials;
        }
        
        public void SetSharedMesh(Mesh m)
        {
            _meshFilter.sharedMesh = m;
        }

        public void SetVisible(bool state)
        {
            _meshRenderer.enabled = state;
        }
    }
}