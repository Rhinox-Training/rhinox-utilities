using UnityEngine;

namespace Rhinox.Utilities
{
	public interface IPoolableObject
	{
		GameObject Template { get; }
		void Init(IObjectPool pool, GameObject template);
	}

	public class PoolObject : MonoBehaviour, IPoolableObject
	{
		protected IObjectPool Pool { get; private set; }

		public GameObject Template { get; private set; }

		public virtual void Init(IObjectPool pool, GameObject template)
		{
			Template = template;
			Pool = pool;
		}
		
		public void PushBackToPool(bool preserveParent = true)
		{
			Pool.PushToPool(gameObject, Template, preserveParent);
		}

		public void PushBackToPool(Transform parent)
		{
			Pool.PushToPool(gameObject, Template, parent);
		}
	}
}