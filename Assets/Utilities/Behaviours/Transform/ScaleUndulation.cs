using UnityEngine;

namespace Rhinox.Utilities
{
    public class ScaleUndulation : MonoBehaviour
    {
        public float ExtraScalePeak = .2f;
        public float TimeMultiplier = 1;

        public bool randomStart = true;

        private float _currAmount = 0;

        private Vector3 _originalScale = Vector3.one;

        // Use this for initialization
        void Start()
        {
            _originalScale = transform.localScale;

            if (randomStart)
                _currAmount = Random.Range(0.0f, Mathf.PI);
        }

        // Update is called once per frame
        void Update()
        {
            _currAmount += (Time.deltaTime * TimeMultiplier) % Mathf.PI;

            transform.localScale = _originalScale + (Mathf.Sin(_currAmount) * Vector3.one * ExtraScalePeak);
        }
    }
}