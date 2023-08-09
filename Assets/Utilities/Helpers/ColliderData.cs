using Rhinox.Lightspeed;
using UnityEngine;

namespace Rhinox.Utilities
{
    public interface IColliderData
    {
        TransformState TransformState { get; set; }
        Collider Recreate(GameObject target, AssetInfo info);
    }
    
    public class BoxColliderData : IColliderData
    {
        [SerializeField] private TransformState _transformState;

        public TransformState TransformState
        {
            get => _transformState;
            set => _transformState = value;
        }

        public Vector3 Center;
        public Vector3 Size;

        public static BoxColliderData AsData(BoxCollider collider)
        {
            return new BoxColliderData
            {
                Center = collider.center,
                Size = collider.size,
            };
        }

        public Collider Recreate(GameObject target, AssetInfo info)
        {
            var collider = target.AddComponent<BoxCollider>();
            collider.center = Center;
            collider.size = Size;
            return collider;
        }
    }

    public class SphereColliderData : IColliderData
    {
        [SerializeField] private TransformState _transformState;

        public TransformState TransformState
        {
            get => _transformState;
            set => _transformState = value;
        }

        public Vector3 Center;
        public float Radius;

        public static SphereColliderData AsData(SphereCollider collider)
        {
            return new SphereColliderData
            {
                Center = collider.center,
                Radius = collider.radius
            };
        }

        public Collider Recreate(GameObject target, AssetInfo info)
        {
            var collider = target.AddComponent<SphereCollider>();
            collider.center = Center;
            collider.radius = Radius;
            return collider;
        }
    }

    public class CapsuleColliderData : IColliderData
    {
        [SerializeField] private TransformState _transformState;

        public TransformState TransformState
        {
            get => _transformState;
            set => _transformState = value;
        }

        public Vector3 Center;
        public float Radius;
        public float Height;
        public int Direction;

        public static CapsuleColliderData AsData(CapsuleCollider collider)
        {
            return new CapsuleColliderData
            {
                Center = collider.center,
                Radius = collider.radius,
                Height = collider.height,
                Direction = collider.direction
            };
        }

        public Collider Recreate(GameObject target, AssetInfo info)
        {
            var collider = target.AddComponent<CapsuleCollider>();
            collider.center = Center;
            collider.radius = Radius;
            collider.height = Height;
            collider.direction = Direction;
            return collider;
        }
    }

    public class MeshColliderData : IColliderData
    {
        [SerializeField] private TransformState _transformState;

        public TransformState TransformState
        {
            get => _transformState;
            set => _transformState = value;
        }

        public bool IsConvex = false;
        public int MeshIndex;

        public static MeshColliderData AsData(MeshCollider collider, MeshData[] availableMeshes)
        {
            var index = availableMeshes.FindIndex(x => x.Mesh == collider.sharedMesh);
            return new MeshColliderData
            {
                IsConvex = collider.convex,
                MeshIndex = index
            };
        }

        public Collider Recreate(GameObject target, AssetInfo info)
        {
            var collider = target.AddComponent<MeshCollider>();
            collider.convex = IsConvex;
            collider.sharedMesh = info.MeshDatas[MeshIndex].Mesh;
            return collider;
        }
    }
}