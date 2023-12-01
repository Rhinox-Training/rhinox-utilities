using System;
using UnityEngine;

namespace Rhinox.Utilities
{
    public class TimedPoolObject : PoolObject
    {
        private DateTime _startupTime;

        public float SecondsBeforeReturningToPool;

        public override void Init(IObjectPool pool, GameObject template)
        {
            base.Init(pool, template);
            _startupTime = DateTime.Now;
        }

        private void Update()
        {
            var time = DateTime.Now - _startupTime;
            if (time.TotalSeconds >= SecondsBeforeReturningToPool)
                PushBackToPool();
        }
    }
}