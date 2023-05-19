using UnityEditor;
using UnityEngine;

namespace Rhinox.Utilities.Editor
{
    public static class OcclusionBakeDetector
    {
        private static bool _wasRunning;

        public delegate void DetectorEventHandler();
        public static event DetectorEventHandler BakeStarted;
        
        [InitializeOnLoadMethod]
        private static void HookTickManager()
        {
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
        }

        private static void OnUpdate()
        {
            if (StaticOcclusionCulling.isRunning && !_wasRunning)
                BakeStarted?.Invoke();
            _wasRunning = StaticOcclusionCulling.isRunning;
        }
    }
}