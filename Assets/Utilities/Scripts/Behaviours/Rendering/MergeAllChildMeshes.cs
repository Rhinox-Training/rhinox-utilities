using System;
using System.Linq;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Rhinox.GUIUtils.Attributes;
using Rhinox.Lightspeed;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Rhinox.Utilities
{
    [RequireComponent(typeof(MeshFilter))]
    [SmartFallbackDrawn(false)]
    public class MergeAllChildMeshes : MonoBehaviour
    {
        public enum TargetSpace
        {
            Local,
            World,
            Identity
        }
        public bool DisableChildMeshes = true;
        public bool ExecuteAtStart = true;
        public TargetSpace Space;

        [SerializeField, HideInInspector]
        private List<MeshFilter> _mergedMeshes;
        [SerializeField, HideInInspector]
        private List<SkinnedMeshRenderer> _mergedSkinnedMeshes;
        [SerializeField, HideInInspector]
        private Material[] _mergedMaterials;

        public Mesh MeshCopy
        {
            get { return GetComponent<MeshFilter>().mesh; }
        }

        /// <summary>
        /// Merges all underlying meshes into 1, handy for things like see-through materials & it is an optimization!
        /// NOTE : Obj where this is on should be 1,1,1 scale, or weird things happen.
        /// </summary>
        void Start()
        {
            if (ExecuteAtStart)
                Execute();
        }

        public void Execute()
        {
            // move to zero for merging
            var prevPosition = transform.position;
            transform.position = Vector3.zero;
            var prevRotation = transform.rotation;
            transform.rotation = Quaternion.identity;

            _mergedMeshes = GetComponentsInChildren<MeshFilter>(true).ToList();
            _mergedSkinnedMeshes = GetComponentsInChildren<SkinnedMeshRenderer>(true).ToList();
            // remove this object's mesh (since it shouldn't be merged)
            _mergedMeshes.Remove(GetComponent<MeshFilter>());
            var combine = new CombineInstance[_mergedMeshes.Count + _mergedSkinnedMeshes.Count];
            var materials = new List<Material>();
            for (int i = 0; i < _mergedMeshes.Count; ++i)
            {
                // combine the meshes into a sharedmesh
                combine[i].mesh = _mergedMeshes[i].sharedMesh;
                combine[i].transform = GetTargetTransform(_mergedMeshes[i].transform);

                var renderer = _mergedMeshes[i].GetComponent<MeshRenderer>();
                var mats = renderer?.sharedMaterials ?? Array.Empty<Material>();
                foreach (var mat in mats)
                    if (!materials.Contains(mat))
                        materials.Add(mat);

                if (DisableChildMeshes)
                    _mergedMeshes[i].gameObject.SetActive(false);
            }
            
            for (int i = _mergedMeshes.Count; i < _mergedMeshes.Count + _mergedSkinnedMeshes.Count; ++i)
            {
                int skinnedMeshI = i - _mergedMeshes.Count;
                // combine the meshes into a sharedmesh
                combine[i].mesh = new Mesh();
                _mergedSkinnedMeshes[skinnedMeshI].BakeMesh(combine[i].mesh);
                combine[i].transform = GetTargetTransform(_mergedSkinnedMeshes[skinnedMeshI].transform);

                if (DisableChildMeshes)
                    _mergedSkinnedMeshes[i - _mergedMeshes.Count].gameObject.SetActive(false);
            }

            // set the mesh
            transform.GetComponent<MeshFilter>().sharedMesh = new Mesh();
            transform.GetComponent<MeshFilter>().sharedMesh.CombineMeshes(combine, true);
            transform.GetComponent<MeshFilter>().sharedMesh.name = "CombinedMesh";
            transform.gameObject.SetActive(true);

            _mergedMaterials = materials.ToArray();
            
            // move back to original
            transform.position = prevPosition;
            transform.rotation = prevRotation;
            //transform.localScale = new Vector3(1,1,1);
        }

        private Matrix4x4 GetTargetTransform(Transform source)
        {
            switch (Space)
            {
                case TargetSpace.Identity:
                    return Matrix4x4.identity;
                case TargetSpace.World:
                    return source.localToWorldMatrix;
                case TargetSpace.Local:
                    return source.localToWorldMatrix * transform.worldToLocalMatrix;
                default:
                    throw new NotImplementedException();
            }
        }

        public void EnableChildMeshes()
        {
            var meshFilters = GetComponentsInChildren<MeshFilter>(true).ToList();

            // remove this object's mesh (since it shouldn't be merged)
            meshFilters.Remove(GetComponent<MeshFilter>());

            foreach (var mesh in meshFilters)
                mesh.gameObject.SetActive(true);
        }

#if UNITY_EDITOR

        private bool HasRenderer => GetComponent<MeshRenderer>();

        private bool HasData => !_mergedMaterials.IsNullOrEmpty();

        [Button, HideIf("HasRenderer")]
        private void AddMeshRenderer()
        {
            Undo.AddComponent<MeshRenderer>(gameObject);
        }

        [ButtonGroup("MeshBtns"), Button, GUIColor(.4f, 1, .8f)]
        private void ExecuteNow()
        {
            Undo.RecordObject(this, "Mesh Execute");
            Execute();
        }

        [ButtonGroup("MeshBtns"), Button, GUIColor(1, .4f, .8f)]
        private void ClearMesh()
        {
            var meshFilter = GetComponent<MeshFilter>();
            if (meshFilter.sharedMesh != null)
            {
                Undo.RecordObject(meshFilter, "Mesh Null");
                meshFilter.sharedMesh = null;
            }
        }

        [Button, ShowIf("HasData")]
        private void RestoreChildren()
        {
            foreach (var merged in _mergedMeshes)
                merged.gameObject.SetActive(true);
            
            foreach (var merged in _mergedSkinnedMeshes)
                merged.gameObject.SetActive(true);
        }
        
        [Button, ShowIf("HasData"), ShowIf("HasRenderer")]
        private void SetMaterials()
        {
            var renderer = GetComponent<MeshRenderer>();
            Undo.RegisterCompleteObjectUndo(renderer, "Set Materials");
            renderer.materials = _mergedMaterials;
        }
#endif
    }
}
