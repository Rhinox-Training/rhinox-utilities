using System;
using Rhinox.Lightspeed;
using Rhinox.Perceptor;
using UnityEngine;

namespace Rhinox.Utilities
{
    [ExecuteInEditMode]
    public class FakeParent : MonoBehaviour
    {
        [SerializeField] private Transform _parent = null;

        private Transform _child;

        public bool RotateAroundParent = true;
        public bool MoveWithParent = true;
        public bool DestroyWithParent = false;

        private bool _parentSet;
        private bool _started;

        private Vector3 _relativePosInParentSpace;
        private Quaternion _relativeRotation;
        
        void Start()
        {
            _child = transform;

            if (!_parentSet)
                SetParent(_parent);

            _started = true;
        }

        private void OnEnable()
        {
            // We only want to trigger this here when it has aleady passed the Start method (AKA the behaviour has been disabled and reenabled)
            // This due to wanting to set the options when created from script without having to disabled the object
            if (_started && !_parentSet)
                SetParent(_parent);
        }

        private void OnDisable()
        {
            _parentSet = false;
        }

        public void SetParent(Transform parent)
        {
            if (parent == null)
            {
                _parentSet = false;
                _parent = null;
                return;
            }

            _parent = parent;
            _relativePosInParentSpace = parent.InverseTransformPoint(transform.position);
            _relativeRotation = transform.rotation * Quaternion.Inverse(parent.rotation);

            _parentSet = true;
        }

        void LateUpdate()
        {
            if (!_parentSet)
            {
                if (_parent == null) return;

                SetParent(_parent);
            }

            // var localPositionDiff = _child.position - _localPos;
            // var localRotationDiff = _child.rotation * Quaternion.Inverse(_localRotation);


            // destroy if parent has been destroyed
            if (_parent == null)
            {
                if (DestroyWithParent)
                {
                    PLog.Info<UtilityLogger>($"Destroying {_child.name} due to (fake) parent being destroyed.");
                    Utility.Destroy(_child.gameObject);
                }
                else
                {
                    _parentSet = false;
                    _parent = null;
                }
                return;
            }

            // move child according to parent.
            if (MoveWithParent)
                _child.position = _parent.TransformPoint(_relativePosInParentSpace);

            if (RotateAroundParent)
                _child.rotation = _parent.rotation * _relativeRotation;

            // _child.localRotation *= localRotationDiff;

            // _relativePosInParentSpace = _parent.InverseTransformPoint(transform.position);
            // _relativeRotation = transform.rotation * Quaternion.Inverse(_parent.rotation);
        }
    }

    #region MakeFakeParentConnection

    public static partial class Extensions
    {
        /// <summary>
        /// Creates a child-parent connection that is unaffected by scale.
        /// </summary>
        public static FakeParent MakeFakeParentConnectionWith(this Component child, Component parent)
        {
            var script = child.gameObject.AddComponent<FakeParent>();
            script.SetParent(parent.transform);
            return script;
        }

        /// <summary>
        /// Creates a child-parent connection that is unaffected by scale.
        /// </summary>
        public static FakeParent MakeFakeParentConnectionWith(this GameObject child, GameObject parent)
        {
            return child.transform.MakeFakeParentConnectionWith(parent.transform);
        }

        /// <summary>
        /// Creates a child-parent connection that is unaffected by scale.
        /// </summary>
        public static FakeParent MakeFakeParentConnectionWith(this GameObject child, Component parent)
        {
            return child.transform.MakeFakeParentConnectionWith(parent);
        }

        /// <summary>
        /// Creates a child-parent connection that is unaffected by scale.
        /// </summary>
        public static FakeParent MakeFakeParentConnectionWith(this Component child, GameObject parent)
        {
            return child.MakeFakeParentConnectionWith(parent.transform);
        }
    }

    #endregion
}