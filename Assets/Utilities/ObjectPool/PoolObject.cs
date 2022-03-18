using UnityEngine;

namespace Rhinox.Utilities
{
	public interface IPoolableObject
	{
		GameObject Prefab { get; set; }
		void Init();
	}

	public class PoolObject : MonoBehaviour, IPoolableObject
	{
		protected IObjectPool Pool
		{
			get { return ObjectPool.Instance; }
		}

		public GameObject Prefab { get; set; }

		public virtual void Init()
		{
		}
	}
}