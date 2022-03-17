using System;

namespace Rhinox.Utilities
{
    /// <summary>
    /// TODO Kind of a stupid class; maybe change to something like Timer.Tick?
    /// Although stupid by design, it has its uses
    /// </summary>
    [ExecutionOrder(-9999)]
    public class TickManager : Singleton<TickManager>
    {
        public event Action Tick;
        public event Action LateTick;
        
        private void Update()
        {
            Tick?.Invoke();
        }

        private void LateUpdate()
        {
            LateTick?.Invoke();
        }
    }
}