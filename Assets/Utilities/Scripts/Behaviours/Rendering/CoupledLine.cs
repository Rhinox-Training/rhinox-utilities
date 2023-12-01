using Rhinox.Lightspeed;
using UnityEngine;

namespace Rhinox.Utilities
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(LineRenderer))]
    public class CoupledLine : MonoBehaviour
    {
        // -- variables --
        [Header("References")]
        [SerializeField] private Transform _coupledTo = null;

        LineRenderer _lineRenderer = null;

        private float _currentLengthSqr;
        private Vector3 _currentStart;
        private Vector3 _currentEnd;
        
        [Range(0, 10)]
        [SerializeField] private int _divisions;
        [SerializeField] private float _length;

        private Vector3[] _positions;

        private void Awake()
        {
            _lineRenderer = this.GetComponent<LineRenderer>();
            _positions = new Vector3[2];
        }

        private void LateUpdate()
        {
            if (_coupledTo == null) return;

            bool dirty = false;

            var start = transform.InverseTransformPoint(transform.position);
            var end = transform.InverseTransformPoint(_coupledTo.position) ;

            if (!start.LossyEquals(_currentStart) ||
                !end.LossyEquals(_currentEnd))
                dirty = true;

            var lengthSqr = _length * _length;
            if (!lengthSqr.LossyEquals(_currentLengthSqr))
                dirty = true;
            
            var d = end - start;

            // Handle creation of more points if needed
            int p = 2;
            // Only add more than 2 points when the length of the rope is less than the max
            if (d.sqrMagnitude < lengthSqr)
            {
                for (int i = 0; i < _divisions; ++i)
                    p += p - 1;
            }

            if (_positions.Length != p)
            {
                _positions = new Vector3[p];
                dirty = true;
            }
            
            if (!dirty)
                return;
            
            // If we have more than 2 points, calculate them
            if (_positions.Length > 2)
            {
                var direction = d.normalized;
                var up = Vector3.up;
                var forward = Vector3.Cross(direction, up);
                var q = Quaternion.LookRotation(forward, up);
                Vector3 delta = Quaternion.Inverse(q) * d;
                
                var aValue = RopeUtilities.FindCatenaryConstant(delta.x, delta.y, lengthSqr);
                var peakOffset = RopeUtilities.SimulatePeakXOffset(aValue, delta.x, start.y, end.y);

                if (peakOffset > .99f)
                {
                    // Calculations will be wrong, just lerp between the points
                    for (int i = 1; i < _positions.Length - 1; i++)
                    {
                        _positions[i] = Vector3.Lerp(start, end, i / (float) _positions.Length);
                    }
                }
                else
                {
                    var middleI = _positions.Length / 2;

                    for (int i = 1; i < middleI; i++)
                    {
                        var val = (i / (float) middleI) * peakOffset;
                        _positions[i] = q * RopeUtilities.GetPointOnRope(aValue, start, delta, val, peakOffset);
                    }

                    _positions[middleI] = q * RopeUtilities.GetPointOnRope(aValue, start, delta, peakOffset, peakOffset);
                
                    for (int i = 1; i < _positions.Length - middleI; i++)
                    {
                        var val = peakOffset + (i / ((float) _positions.Length - middleI)) * (1f - peakOffset);
                        _positions[i + middleI] = q * RopeUtilities.GetPointOnRope(aValue, start, delta, val, peakOffset);
                    }
                }
            }

            _currentStart = start;
            _currentEnd = end;
            _currentLengthSqr = lengthSqr;

            _positions[0] = start;
            _positions[_positions.Length - 1] = end;
            _lineRenderer.positionCount = _positions.Length;
            _lineRenderer.SetPositions(_positions);
        }
    }
}