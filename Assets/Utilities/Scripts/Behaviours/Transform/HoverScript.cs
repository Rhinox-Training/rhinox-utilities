using UnityEngine;
using Rhinox.Lightspeed;

namespace Rhinox.Utilities
{
    [RefactoringOldNamespace("")]
    public class HoverScript : MonoBehaviour
    {
        public float HoverPeak = 2;
        public float TimeMultiplier = 1;

        public bool localPosHover = true;
        public bool randomStartPosition = true;

        private float _currAmount = 0;

        private float _originalY = 0;

        // Use this for initialization
        void Start()
        {
            if (localPosHover)
                _originalY = transform.localPosition.y;
            else
                _originalY = transform.position.y;

            if (randomStartPosition)
                _currAmount = Random.Range(0.0f, Mathf.PI);
        }

        // Update is called once per frame
        void Update()
        {
            _currAmount += (Time.deltaTime * TimeMultiplier) % Mathf.PI;

            if (localPosHover)
                transform.SetLocalYPosition(_originalY + (Mathf.Sin(_currAmount) * HoverPeak));
            else
                transform.SetYPosition(_originalY + (Mathf.Sin(_currAmount) * HoverPeak));
        }
    }
}