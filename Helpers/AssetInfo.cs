using System;
using System.Collections.Generic;
using System.Linq;
using Rhinox.Lightspeed;
using Rhinox.Perceptor;
using UnityEngine;

namespace Rhinox.Utilities
{
    public class MeshData
    {
        public TransformState TransformState;
        public string[] Materials;
        public string MeshAsBase64;
        [NonSerialized] public Mesh Mesh;
        
        public static MeshData ExportMeshData(MeshRenderer meshRenderer, Transform transform)
        {
            var filter = meshRenderer.GetComponent<MeshFilter>();
            Mesh mesh = filter.sharedMesh;
            return new MeshData
            {
                TransformState = TransformState.CreateRelative(meshRenderer.transform, transform),
                Materials = meshRenderer.sharedMaterials.Select(x => x.name).ToArray(),
                Mesh = mesh,
                MeshAsBase64 = Convert.ToBase64String( MeshSerializer.MeshToByte(mesh, true) )
            };
        }
    }
    
    public class AssetInfo
    {
        public Guid Id;
        public string Name;
        
        public MeshData[] MeshDatas;
        public IColliderData[] ColliderData;

        [NonSerialized] public GameObject Prefab;

        public AssetInfo()
        {
            Id = Guid.NewGuid();
        }
        
        protected static MeshData[] GetMeshData(Transform transform)
        {
            return transform.GetComponentsInChildren<MeshRenderer>()
                .Select(x => MeshData.ExportMeshData(x, transform))
                .ToArray();
        }

        protected static IColliderData[] GetColliderData(Transform transform, MeshData[] meshData)
        {
            return transform.GetComponentsInChildren<Collider>()
                .Select(x => CreateColliderData(transform, x, meshData))
                .ToArray();
        }

        public static AssetInfo CreateInfo(Transform transform)
        {
            var meshData = GetMeshData(transform);

            var colliderData = GetColliderData(transform, meshData);

            return new AssetInfo
            {
                MeshDatas = meshData,
                ColliderData = colliderData
            };
        }

        protected static IColliderData CreateColliderData(Transform target, Collider collider, MeshData[] meshData)
        {
            IColliderData data = null;
            if (collider is BoxCollider boxCollider)
                data = BoxColliderData.AsData(boxCollider);
            else if (collider is SphereCollider sphereCollider)
                data = SphereColliderData.AsData(sphereCollider);
            else if (collider is CapsuleCollider capsuleCollider)
                data = CapsuleColliderData.AsData(capsuleCollider);
            else if (collider is MeshCollider meshCollider)
                data = MeshColliderData.AsData(meshCollider, meshData);
            else
            {
                PLog.Warn<UtilityLogger>($"[SKIPPED] Unknown Collider Type: {collider}");
                return null;
            }

            var transformState = TransformState.CreateRelative(collider.transform, target);
            data.TransformState = transformState;


            return data;
        }
    }
}