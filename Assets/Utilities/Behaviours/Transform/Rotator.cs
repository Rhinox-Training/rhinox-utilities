using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Rhinox.Utilities
{
    public class Rotator : MonoBehaviour
    {
        public Vector3 Speed = new Vector3(0f, .1f, 0f);

        private void Update()
        {
            transform.Rotate(Speed);
        }
    }
}