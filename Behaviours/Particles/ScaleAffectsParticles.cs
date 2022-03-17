using System.Collections.Generic;
using Rhinox.Lightspeed;
using UnityEngine;

namespace Rhinox.Utilities
{
    public class ScaleAffectsParticles : MonoBehaviour
    {
        [Tooltip("If it should search for Particle Systems in this objects children.")]
        public bool AffectsParticleChildren = true;

        private struct ParticleSystemKeeper
        {
            public ParticleSystem System;
            public float OriginalStartSize;
            public float OriginalStartSpeed;
        }

        private readonly List<ParticleSystemKeeper> _particleKeeperList = new List<ParticleSystemKeeper>();

        void Start()
        {
            var particleSystems = AffectsParticleChildren
                ? GetComponentsInChildren<ParticleSystem>()
                : GetComponents<ParticleSystem>();

            foreach (var system in particleSystems)
            {
                var main = system.main;
                var keeper = new ParticleSystemKeeper
                {
                    System = system,
                    OriginalStartSize = main.startSizeMultiplier,
                    OriginalStartSpeed = main.startSpeedMultiplier
                };

                _particleKeeperList.Add(keeper);
            }
        }

        // Update is called once per frame
        void Update()
        {
            var scale = transform.localScale.Average();

            foreach (var systemKeeper in _particleKeeperList)
            {
                var main = systemKeeper.System.main;
                main.startSize = systemKeeper.OriginalStartSize * scale;
                main.startSpeed = systemKeeper.OriginalStartSpeed * scale;
            }
        }
    }
}
