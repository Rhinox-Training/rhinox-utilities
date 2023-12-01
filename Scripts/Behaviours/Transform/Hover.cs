using System;
using System.Collections;
using System.Collections.Generic;
using Rhinox.Lightspeed;
using Rhinox.Utilities;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Rhinox.Utilities
{
    public class Hover : MonoBehaviour
    {
        public float HoverSpeed = .5f;
        public float HoverStrength = .1f;

        public bool LocalSpace = false;
        public bool RandomizeOffsetOnStart = false;

        private float _currAmount = 0;
        private float _originalY = 0;

        // Use this for initialization
        void Start()
        {
            if (LocalSpace)
                _originalY = transform.localPosition.y;
            else
                _originalY = transform.position.y;

            if (RandomizeOffsetOnStart)
                _currAmount = Random.Range(0.0f, Mathf.PI);
        }

        // Update is called once per frame
        void Update()
        {
            _currAmount += (Time.deltaTime * HoverSpeed) % Mathf.PI;

            if (LocalSpace)
                transform.SetLocalYPosition(_originalY + (Mathf.Sin(_currAmount) * HoverStrength));
            else
                transform.SetYPosition(_originalY + (Mathf.Sin(_currAmount) * HoverStrength));
        }
    }
}