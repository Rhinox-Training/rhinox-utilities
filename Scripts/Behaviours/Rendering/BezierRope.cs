using Rhinox.GUIUtils.Attributes;
using Rhinox.Lightspeed;
using UnityEngine;

namespace Rhinox.Utilities
{
    [RequireComponent(typeof(LineRenderer))]
    [ExecuteAlways, SmartFallbackDrawn]
    public class BezierRope : MonoBehaviour
    {
        public Transform A;
        public Transform B;
        
        [Range(2, 100)] public int AmountOfVertices = 10;
        public float Stiffness = 300;
        public float Damping = 18;
        public float RopeLength = 15;

        [ShowReadOnly]
        public float ActualLength => _lineRenderer.GetLength();

        private Vector3 _currentValue;
        private Vector3 _currentVelocity;
        private Vector3 _targetValue;
        private const float _valueThreshold = 0.01f;
        private const float _velocityThreshold = 0.01f;

        private LineRenderer _lineRenderer;

        private void OnEnable()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            if (A && B)
                _currentValue = GetMidPoint(A.position, B.position);
        }

        private void Update()
        {
            if (A && B)
                SetSplinePoint();
        }

        void SetSplinePoint()
        {
            if (_lineRenderer.positionCount != AmountOfVertices + 1)
                _lineRenderer.positionCount = AmountOfVertices + 1;

            var a = A.position;
            var b = B.position;

            Vector3 mid = GetMidPoint(a, b);
            _targetValue = mid;
            if (Application.isPlaying)
                mid = _currentValue;

            for (int i = 0; i < AmountOfVertices; i++)
            {
                Vector3 p = GetBezierPoint(a, mid, b, i / (float)AmountOfVertices);
                _lineRenderer.SetPosition(i, p);
            }

            _lineRenderer.SetPosition(AmountOfVertices, B.position);
        }

        Vector3 GetMidPoint(Vector3 a, Vector3 b)
        {
            Vector3 midpos = Vector3.Lerp(a, b, .5f);
            float yFactor = RopeLength - Mathf.Min(Vector3.Distance(a, b), RopeLength);
            midpos.y -= yFactor;
            return midpos;
        }

        Vector3 GetBezierPoint(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            Vector3 a = Vector3.Lerp(p0, p1, t);
            Vector3 b = Vector3.Lerp(p1, p2, t);
            Vector3 point = Vector3.Lerp(a, b, t);
            return point;
        }
        
        void FixedUpdate()
        {
            SimulatePhysics();
        }

        void SimulatePhysics()
        {
            float dampingFactor = Mathf.Max(0, 1 - Damping * Time.fixedDeltaTime);
            Vector3 acceleration = (_targetValue - _currentValue) * (Stiffness * Time.fixedDeltaTime);
            _currentVelocity = _currentVelocity * dampingFactor + acceleration;
            _currentValue += _currentVelocity * Time.fixedDeltaTime;

            CheckValueReached(ref _currentValue.x, _targetValue.x, ref _currentVelocity.x);
            CheckValueReached(ref _currentValue.y, _targetValue.y, ref _currentVelocity.y);
            CheckValueReached(ref _currentValue.z, _targetValue.z, ref _currentVelocity.z);
        }

        private void CheckValueReached(ref float val, float target, ref float velocity)
        {
            if (Mathf.Abs(val - target) < _valueThreshold &&
                Mathf.Abs(velocity) < _velocityThreshold)
            {
                val = target;
                velocity = 0f;
            }
        }

        private void OnDrawGizmos()
        {
            if (B == null || A == null)
                return;
            
            Vector3 midPos = GetMidPoint(A.position, B.position);

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(midPos, 0.2f);
        }
    }
}