using System;
using Rhinox.Lightspeed;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Rhinox.Utilities
{
    public class LookAt : MonoBehaviour
    {
        public Axis Axis = Axis.Z;
        [Indent] public bool Negative;

        public int NegativeScale
        {
            get { return Negative ? -1 : 1; }
        }

        public Transform Target;

        public Axis LockedAxis;

        // Update is called once per frame
        private void LateUpdate()
        {
            var newRotation = GetTargetRotation();
            transform.localRotation = newRotation;
        }

        private Vector3 GetForward()
        {
            // Works for certain axises, does not work for all, in particular the up axis is unreliable
            switch (Axis)
            {
                case Axis.X:
                    return NegativeScale * Vector3.right;
                case Axis.Y:
                    return NegativeScale * Vector3.up;
                case Axis.Z:
                    return NegativeScale * Vector3.forward;
                default:
                    throw new Exception();
            }
        }

        private Quaternion GetTargetRotation()
        {
            if (Target == null) return Quaternion.identity;
            
            if (LockedAxis != Axis.None)
                transform.localRotation = Quaternion.Euler(transform.localEulerAngles.ResetAxis(LockedAxis));

            var localTarget = transform.InverseTransformPoint(Target.position);

            var frontVectorLocal = GetForward();

            var targetRotation = transform.localRotation * Quaternion.FromToRotation(frontVectorLocal, localTarget);

            if (LockedAxis == 0) return targetRotation;

            var euler = targetRotation.eulerAngles;

            if (LockedAxis.HasFlag(Axis.X))
                euler.x = 0;
            if (LockedAxis.HasFlag(Axis.Y))
                euler.y = 0;
            if (LockedAxis.HasFlag(Axis.Z))
                euler.z = 0;

            return Quaternion.Euler(euler);

        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!Target) return;

            var worldRot = transform.rotation * Quaternion.Inverse(transform.localRotation) * GetTargetRotation();

            GizmosExt.DrawArrow(transform.position, worldRot.GetForward(), Color.blue, .66f);
        }
#endif
    }
}