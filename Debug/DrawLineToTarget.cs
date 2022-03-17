using Rhinox.Lightspeed;
using UnityEngine;

namespace Rhinox.Utilities
{
    [RefactoringOldNamespace("")]
    public class DrawLineToTarget : MonoBehaviour
    {
        public Transform Target;

        private void OnDrawGizmos()
        {
            var color = Gizmos.color;
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, Target.position);
            Gizmos.color = color;
        }
    }
}
