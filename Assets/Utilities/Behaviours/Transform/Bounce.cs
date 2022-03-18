using System;
using UnityEngine;

namespace Rhinox.Utilities
{
    public class Bounce : MonoBehaviour
    {
        public enum BounceType
        {
            Sinusoid,
            Loop,
        }
    
        public float Range;
        public float Speed;
        public Vector3 Direction;

        public BounceType Type;

        Vector3 _startPosition;
        private float _timeOffset;

        private void OnEnable()
        {
            _startPosition = transform.localPosition;
            _timeOffset = Time.realtimeSinceStartup;
        }

        private void OnDisable()
        {
            transform.localPosition = _startPosition;
        }

        void Update()
        {
            transform.localPosition = _startPosition + (Direction * (GetOffsetPosition() * Range));
        }

        private float GetOffsetPosition()
        {
            // Does not take into account direction nor range
            // return [-1 .. 1]
            float pos = Time.timeSinceLevelLoad * Speed + _timeOffset;

            switch (Type)
            { 
                case BounceType.Sinusoid:
                    return Mathf.Sin(pos);
                case BounceType.Loop:
                    return (pos % 2)-1;
                default:
                    return 0f;
            }
        }
        
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (Direction.sqrMagnitude <= 0.01f)
                return;
            var parent = transform.parent;
            var range = Direction.normalized * Range;
            if (parent) // we move in local, aka the parent's space
                range = parent.TransformVector(range);
            GizmosExt.DrawArrow(transform.position - range, range*2);
        }
#endif
    }
}