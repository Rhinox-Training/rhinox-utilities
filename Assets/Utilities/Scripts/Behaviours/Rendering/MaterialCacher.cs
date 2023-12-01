using Rhinox.Lightspeed;
using UnityEngine;

namespace Rhinox.Utilities
{
    [RefactoringOldNamespace("")]
    public class MaterialCacher : MonoBehaviour
    {
        Material _mat;

        void Start()
        {
            _mat = GetComponent<Renderer>().sharedMaterial;
        }

        public void Reset()
        {
            GetComponent<Renderer>().sharedMaterial = _mat;
        }
    }
}