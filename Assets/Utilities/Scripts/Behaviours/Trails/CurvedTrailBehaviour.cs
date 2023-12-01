using System.Collections.Generic;
using Rhinox.Lightspeed;
using UnityEngine;

namespace Rhinox.Utilities
{
    [RefactoringOldNamespace("")]
    public class CurvedTrailBehaviour : MonoBehaviour
    {
        public Color NormalColor;
        public Color CorrectColor;
        public Transform ForwardTransform;
        public float MinimumDistance = 2.0f;

        List<Transform> _targets;
        Transform destObject;
        Transform sourceObject;
        LineRenderer lr;
        public float scrollSpeed = 3f;
        float offset = 0f;
        Vector3 _sourceLocalPos;
        Spline _spline;

        void Awake()
        {
            lr = GetComponent<LineRenderer>();
        }

        void Start()
        {
            _spline = new Spline();
            _spline.AnchorPoints.Clear();
            _spline.AnchorPoints.Add(Vector3.zero);
            _spline.AnchorPoints.Add(Vector3.zero);
            _spline.AnchorPoints.Add(Vector3.zero);
            _spline.PointsPerAnchor = 7;
            _spline.Smoothness = 0.2f;
        }

        void Update()
        {
            if (!lr.enabled)
                lr.enabled = SetClosestTarget();

            if (!lr.enabled || _spline == null || sourceObject == null || destObject == null)
                return;

            _spline.AnchorPoints[0] = sourceObject.transform.TransformPoint(_sourceLocalPos);

            // var distanceFromHead = Vector3.Distance(_spline.AnchorPoints[0], ViveHelper.Head.position);
            // var distanceFromDest = Vector3.Distance(_spline.AnchorPoints[0], destObject.position);
            // var helpTrans = distanceFromHead < distanceFromDest ? ViveHelper.Head : destObject;
            // var forward = ViveHelper.Head.forward;
            // var forward = sourceObject.parent == null ? sourceObject.forward : sourceObject.parent.forward;

            _spline.AnchorPoints[1] = sourceObject.position + ForwardTransform.forward *
                (Vector3.Distance(destObject.position, _spline.AnchorPoints[0]) * 0.5f);
            //_spline.AnchorPoints[1] = ViveHelper.Head.position + ViveHelper.Head.forward * (Vector3.Distance(destObject.position, ViveHelper.Head.position) * 0.8f);
            _spline.AnchorPoints[2] = destObject.position;
            _spline.GeneratePoints();
            lr.SetPositions(_spline.Points.ToArray());

            // set the positions for the line
            //lr.SetPosition(0, sourceObject.transform.TransformPoint(_sourceLocalPos));
            //lr.SetPosition(1, destObject.position);

            // UV animate the material
            offset += Time.deltaTime * scrollSpeed;
            lr.sharedMaterial.mainTextureOffset = new Vector2(offset % 1, 0);
            lr.sharedMaterial.mainTextureScale = new Vector3(lr.sharedMaterial.mainTextureScale.x, 1f);
        }

        private bool SetClosestTarget()
        {
            if (_targets == null || sourceObject == null)
                return false;

            var smallestDistance = (_targets[0].position - sourceObject.position).sqrMagnitude;
            for (int i = 1; i < _targets.Count; ++i)
            {
                var distance = sourceObject.SqrDistanceTo(_targets[i]);

                if (distance >= smallestDistance)
                    continue;

                smallestDistance = distance;
                destObject = _targets[i];
            }

            // sqrMagnitude is used => square it
            return smallestDistance > Mathf.Pow(MinimumDistance, 2);
        }

        // set the trail connection
        public void SetTrail(Transform source, Transform destinationObject, Vector3 sourceLocalPos)
        {
            lr.enabled = true;
            _sourceLocalPos = sourceLocalPos;
            sourceObject = source;
            destObject = destinationObject;
        }

        public void SetTrail(Transform source, Transform destinationObject)
        {
            lr.enabled = true;
            _sourceLocalPos = Vector3.zero;
            sourceObject = source;
            destObject = destinationObject;
        }

        public void SetTrail(Transform source, Transform forwardTransform, Transform target)
        {
            ForwardTransform = forwardTransform;
            lr.enabled = true;
            _sourceLocalPos = Vector3.zero;
            sourceObject = source;
            _targets = new List<Transform>() { target };
            destObject = target;
        }

        public void DeactivateTrail(bool resetPoints = false)
        {
            lr.enabled = false;
            if (!resetPoints) return;

            sourceObject = null;
            destObject = null;
        }

        public void SetCorrect(bool correct)
        {
            lr.material.color = correct ? CorrectColor : NormalColor;
        }
    }
}