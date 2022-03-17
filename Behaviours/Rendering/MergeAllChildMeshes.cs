using System.Linq;
using UnityEngine;
using System.Collections;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Rhinox.Utilities
{
    [RequireComponent(typeof(MeshFilter))]
    public class MergeAllChildMeshes : MonoBehaviour
    {
        public bool DisableChildMeshes = true;
        public bool ExecuteAtStart = true;

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

            var meshFilters = GetComponentsInChildren<MeshFilter>(true).ToList();
            var skinnedMeshFilters = GetComponentsInChildren<SkinnedMeshRenderer>(true).ToList();
            // remove this object's mesh (since it shouldn't be merged)
            meshFilters.Remove(GetComponent<MeshFilter>());
            var combine = new CombineInstance[meshFilters.Count + skinnedMeshFilters.Count];

            for (int i = 0; i < meshFilters.Count; ++i)
            {
                // combine the meshes into a sharedmesh
                combine[i].mesh = meshFilters[i].sharedMesh;
                combine[i].transform = meshFilters[i].transform.localToWorldMatrix;

                if (DisableChildMeshes)
                    meshFilters[i].gameObject.SetActive(false);
            }

            for (int i = meshFilters.Count; i < meshFilters.Count + skinnedMeshFilters.Count; ++i)
            {
                // combine the meshes into a sharedmesh
                combine[i].mesh = new Mesh();
                skinnedMeshFilters[i - meshFilters.Count].BakeMesh(combine[i].mesh);
                combine[i].transform = skinnedMeshFilters[i - meshFilters.Count].transform.localToWorldMatrix;

                if (DisableChildMeshes)
                    skinnedMeshFilters[i - meshFilters.Count].gameObject.SetActive(false);
            }

            // set the mesh
            transform.GetComponent<MeshFilter>().sharedMesh = new Mesh();
            transform.GetComponent<MeshFilter>().sharedMesh.CombineMeshes(combine, true);
            transform.GetComponent<MeshFilter>().sharedMesh.name = "CombinedMesh";
            transform.gameObject.SetActive(true);

            // move back to original
            transform.position = prevPosition;
            transform.rotation = prevRotation;
            //transform.localScale = new Vector3(1,1,1);
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

        private bool HasRenderer
        {
            get { return GetComponent<MeshRenderer>(); }
        }

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

        [Button]
        private void HideAllChildren()
        {
            foreach (Transform child in transform)
                child.gameObject.SetActive(false);
        }
#endif
    }
}
