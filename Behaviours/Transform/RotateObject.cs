using Rhinox.Lightspeed;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Rhinox.Utilities
{
    public class RotateObject : MonoBehaviour
    {
        public Axis Axis;
        [EnumToggleButtons]
        public Space Space;
        public float Speed;

        void Update()
        {
            Vector3 rotation = Vector3.zero;
                
            if (Axis.HasFlag(Axis.X))
                rotation.x += Speed * Time.deltaTime;
            
            if (Axis.HasFlag(Axis.Y))
                rotation.y += Speed * Time.deltaTime;
            
            if (Axis.HasFlag(Axis.Z))
                rotation.z += Speed * Time.deltaTime;
                
            transform.Rotate(rotation, Space);
        }
    }
}