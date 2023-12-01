using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Rhinox.Lightspeed;
using UnityEngine;

namespace Rhinox.Utilities
{
    public class Patroller : MonoBehaviour
    {
        public Transform CheckpointsContainer;
        public float MaxDistanceDelta = .01f;
        public float CheckpointDelay = 1f;
        public bool TeleportToOrigEveryCycle;

        private Vector3 _origPosition;
        private List<Transform> _checkpoints;
        private int _currCheckpoint;

        private void Awake()
        {
            _origPosition = transform.position;

            _checkpoints = CheckpointsContainer.GetAllChildren().ToList();
            if (CheckpointsContainer.childCount == 0)
                return;

            StartCoroutine(nameof(Patrol));
        }

        private IEnumerator Patrol()
        {
            while (true)
            {
                var currentPos = transform.position;
                var targetPos = _checkpoints[_currCheckpoint].position;

                if (Vector3.Distance(currentPos, targetPos) <= .01f)
                {
                    yield return new WaitForSeconds(CheckpointDelay);

                    if (TeleportToOrigEveryCycle && _currCheckpoint + 1 == _checkpoints.Count)
                    {
                        transform.position = _origPosition;
                        _currCheckpoint = 0;

                        yield return new WaitForSeconds(CheckpointDelay);

                        continue;
                    }

                    _currCheckpoint = (_currCheckpoint + 1) % _checkpoints.Count;
                }

                transform.position = Vector3.MoveTowards(currentPos, targetPos, MaxDistanceDelta);
                yield return null;
            }
        }

        private void OnDrawGizmos()
        {
            if (_checkpoints.IsNullOrEmpty())
                return;

            for (var i = 0; i < _checkpoints.Count; i++)
            {
                if (_checkpoints[i] == null || _checkpoints[(i + 1) % _checkpoints.Count] == null)
                    continue;

                if (i + 1 == _checkpoints.Count)
                    continue;

                var curr = _checkpoints[i].position;
                var next = _checkpoints[(i + 1) % _checkpoints.Count].position;
                Gizmos.DrawSphere(curr, .05f);
                Gizmos.DrawLine(curr, next);
            }
        }
    }
}