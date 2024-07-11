using System;
using Rhinox.Utilities.Attributes;

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
        public event Action AwakeTick;
        public event Action StartTick;
        
        private void Update()
        {
            Tick?.Invoke();
        }

        private void LateUpdate()
        {
            LateTick?.Invoke();
        }

        private void Awake()
        {
            AwakeTick?.Invoke();
        }

        private void Start()
        {
            StartTick?.Invoke();
        }
    }
}