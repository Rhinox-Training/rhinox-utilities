 using System;
 using UnityEngine;

namespace Rhinox.Utilities
{
    [RequireComponent(typeof(LineRenderer))]
    public class ManualTrail : MonoBehaviour
    {
        public int TrailResolution;
        LineRenderer _lineRenderer;

        Vector3[] _lineSegmentPositions;
        Vector3[] _lineSegmentVelocities;

        [Range(0, 1)] public float Speed;
        private float _currSpeed;

        // This would be the distance between the individual points of the line renderer
        public float Offset;
        private float _initialOffset;
        Vector3 _facingDirection;

        public enum LocalDirections
        {
            XAxis,
            YAxis,
            ZAxis
        }

        public LocalDirections LocalDirectionToUse;

        public bool UseDirection;
        public Vector3 TrailDirection;

        // How far the points 'lag' behind each other in terms of position
        public float LagTime;

        public Vector3 GetLocalDirection()
        {
            switch (LocalDirectionToUse)
            {
                case LocalDirections.XAxis:
                    return transform.right;
                case LocalDirections.YAxis:
                    return transform.up;
                case LocalDirections.ZAxis:
                    return transform.forward;
            }

            Debug.LogError("The variable 'localDirectionToUse' on the 'ManualTrail' script, located on object " + name +
                           ", was somehow invalid. Please investigate!");
            return Vector3.zero;
        }

        Vector3 GetDirection()
        {
            if (UseDirection)
                return TrailDirection;

            return GetLocalDirection();
        }

        // Use this for initialization
        void Start()
        {
            _lineRenderer = GetComponent<LineRenderer>();

            _lineRenderer.positionCount = TrailResolution;

            _lineSegmentPositions = new Vector3[TrailResolution];
            _lineSegmentVelocities = new Vector3[TrailResolution];

            _facingDirection = GetDirection();

            _initialOffset = Offset;
            UpdateSpeed(Speed);

            // Initialize our positions
            for (int i = 0; i < _lineSegmentPositions.Length; i++)
            {
                _lineSegmentPositions[i] = new Vector3();
                _lineSegmentVelocities[i] = new Vector3();

                if (i == 0)
                {
                    // Set the first position to be at the base of the transform
                    _lineSegmentPositions[i] = transform.position;
                }
                else
                {
                    // All subsequent positions would be an offset of the original position.
                    _lineSegmentPositions[i] = transform.position + (_facingDirection * (Offset * i));
                }
            }
        }

        // Update is called once per frame
        void Update()
        {
            _facingDirection = GetDirection();

            for (int i = 0; i < _lineSegmentPositions.Length; i++)
            {
                if (i == 0)
                {
                    // We always want the first position to be exactly at the original position
                    _lineSegmentPositions[i] = transform.position;
                }
                else
                {
                    // All others will follow the original with the offset that you set up
                    _lineSegmentPositions[i] = Vector3.SmoothDamp(
                        _lineSegmentPositions[i],
                        _lineSegmentPositions[i - 1] + (_facingDirection * Offset),
                        ref _lineSegmentVelocities[i],
                        LagTime / Speed
                    );
                }

                // Once we're done calculating where our position should be, set the line segment to be in its proper place
                _lineRenderer.SetPosition(i, _lineSegmentPositions[i]);
            }

            if (Math.Abs(Speed - _currSpeed) > .01f)
            {
                UpdateSpeed(Speed);
            }
        }

        private void UpdateSpeed(float speed)
        {
            if (speed > .9f)
            {
                SetAlpha(.8f, speed);
                Offset = _initialOffset * (1 + speed * 1.5f);
            }
            else if (speed > .7f)
            {
                SetAlpha(speed - .2f, speed);
                Offset = _initialOffset * (1 + speed * .7f);
            }
            else if (speed > .5f)
            {
                SetAlpha(speed - .2f, speed);
                Offset = _initialOffset * (1 + speed * .5f);
            }
            else if (speed > .35f)
            {
                SetAlpha(speed / 1.5f - .2f, speed);
                Offset = _initialOffset * (1 + speed * .35f);
            }
            else if (speed > .2f)
            {
                SetAlpha(speed / 2f - .2f, speed / 1.5f);
                Offset = _initialOffset * (1 + speed * .2f);
            }
            else
            {
                SetAlpha(0, speed / 2);
                Offset = _initialOffset * (1 + speed);
            }

            _currSpeed = speed;
        }

        private void SetAlpha(float initial, float final)
        {
            var gradient = _lineRenderer.colorGradient;

            gradient.SetKeys(gradient.colorKeys, new[]
            {
                new GradientAlphaKey
                {
                    time = 0.0f,
                    alpha = initial,
                },
                new GradientAlphaKey
                {
                    time = 1.0f,
                    alpha = final,
                },
            });
            _lineRenderer.colorGradient = gradient;
        }
    }
}