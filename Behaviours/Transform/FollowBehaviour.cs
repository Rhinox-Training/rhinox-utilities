using UnityEngine;

namespace Rhinox.Utilities
{
    public class FollowBehaviour : MonoBehaviour
    {
        public Transform Parent;
        public bool Global = true;
        private Vector3 _relativePosition;

        private bool _initialized;

        private void Start()
        {
            Init();
        }

        public void Init(bool forceInit = false)
        {
            if (!forceInit && _initialized) return;
            
            _relativePosition = Global ? transform.position - Parent.transform.position : Parent.InverseTransformPoint(transform.position);
            _initialized = true;
        }
        
        private void Update()
        {
            if (Parent == null)
                return;

            if (Global)
                transform.position = Parent.transform.position + _relativePosition;
            else
                transform.position = Parent.TransformPoint(_relativePosition);

        }
    }
}