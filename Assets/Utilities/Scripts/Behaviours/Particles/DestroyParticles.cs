using UnityEngine;
using System.Collections;
using Rhinox.Lightspeed;

namespace Rhinox.Utilities
{
    [RefactoringOldNamespace("")]
    public class DestroyParticles : MonoBehaviour
    {
        private void Start()
        {
            Destroy(gameObject, GetComponent<ParticleSystem>().main.duration);
        }
    }
}