using System.Linq;
using Rhinox.Lightspeed;
using UnityEngine;

namespace Rhinox.Utilities
{
    public class RigidbodyData
    {
        public float Mass;
        public bool DetectCollisions;
        public bool Kinematic;
        public bool UseGravity;
        public CollisionDetectionMode CollisionDetectionMode;
    }
    
    public static class RigidbodyHelper
    {
        public static RigidbodyData GetData(this Rigidbody rb)
        {
            var data = new RigidbodyData()
            {
                Mass = rb.mass,
                DetectCollisions = rb.detectCollisions,
                Kinematic = rb.isKinematic, 
                UseGravity = rb.useGravity,
                CollisionDetectionMode = rb.collisionDetectionMode
            };
            return data;
        }
        
        public static Rigidbody CreateRigidbody(this RigidbodyData data, GameObject go)
        {
            var rb = go.GetOrAddComponent<Rigidbody>();

            rb.mass = data.Mass;
            rb.detectCollisions = data.DetectCollisions;
            rb.isKinematic = data.Kinematic;
            rb.useGravity = data.UseGravity;
            rb.collisionDetectionMode = data.CollisionDetectionMode;

            return rb;
        }

        public static void IgnoreCollision(Rigidbody rb1, Rigidbody rb2, bool ignore = true)
        {
            var colliders1 = rb1.GetComponentsInChildren<Collider>();
            var colliders2 = rb2.GetComponentsInChildren<Collider>();
            foreach (var c1 in colliders1)
            {
                if (c1.attachedRigidbody != rb1)
                    continue;
                foreach (var c2 in colliders2)
                {
                    if (c2.attachedRigidbody != rb2)
                        continue;
                    Physics.IgnoreCollision(c1, c2, ignore);
                }
            }
        }

        public static void IgnoreCollision(Rigidbody rb1, Collider[] colliders, bool ignore)
        {
            var colliders1 = rb1.GetComponentsInChildren<Collider>();
            foreach (var c1 in colliders1)
            {
                if (c1.attachedRigidbody != rb1)
                    continue;
                foreach (var c2 in colliders)
                {
                    Physics.IgnoreCollision(c1, c2, ignore);
                }
            }
        }
    }
}