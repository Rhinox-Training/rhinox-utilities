using UnityEngine;

namespace Rhinox.Utilities
{
	public interface IPoolableObject
	{
		GameObject Template { get; set; }
		void Init();
	}

	public class PoolObject : MonoBehaviour, IPoolableObject
	{
		protected IObjectPool Pool
		{
			get { return ObjectPool.Instance; }
		}

		public GameObject Template { get; set; }

		public virtual void Init()
		{
		}
	}
}