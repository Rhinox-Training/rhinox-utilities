using System.Linq;
using UnityEngine;

namespace Rhinox.Utilities
{
	public class PooledFx : PoolObject
	{
		public bool AddToPoolWhenDead;

		private ParticleSystem[] _systems;

		public override void Init()
		{
			gameObject.SetActive(true);
		}

		public void Awake()
		{
			_systems = GetComponentsInChildren<ParticleSystem>();
		}

		public void Update()
		{
			if (Template && AddToPoolWhenDead && _systems.All(x => !x.IsAlive()))
			{
				gameObject.SetActive(false);
				Pool.PushToPool(gameObject);
			}
		}

		[ContextMenu("Emit Once")]
		public void EmitOnce()
		{
			foreach (var sys in _systems)
			{
				var main = sys.main;
				main.loop = false;
				sys.Play();
			}
		}

		[ContextMenu("Start Loop")]
		public void StartLoop()
		{
			foreach (var sys in _systems)
			{
				var main = sys.main;
				main.loop = true;
				sys.Play();
			}
		}

		[ContextMenu("Stop emitting")]
		public void Stop()
		{
			foreach (var sys in _systems)
				sys.Stop();
		}
	}
}
