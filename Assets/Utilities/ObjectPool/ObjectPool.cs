using System;
using System.Collections;
using System.Collections.Generic;
using Rhinox.Lightspeed;
using Rhinox.Perceptor;
using UnityEngine;

namespace Rhinox.Utilities
{
	public interface IObjectPool
	{
		bool AddToPool(GameObject prefab, int count);
		bool AddToPool(GameObject prefab, int count, Transform parent);

		GameObject PopFromPool(GameObject prefab, bool forceCreate = false, Vector3 position = default);

		GameObject PopFromPool(GameObject prefab, Transform parent, bool forceCreate = false, Vector3 position = default);

		T PopFromPool<T>(T poolObject, bool forceCreate = false, Vector3 position = default)
			where T : Component;
		
		T PopFromPool<T>(T poolObject, Transform parent, bool forceCreate = false, Vector3 position = default)
			where T : Component;

		void PushToPool(GameObject obj);
		void PushToPool(GameObject obj, Transform parent);
		void PushToPool(GameObject obj, GameObject prefab);
		void PushToPool(GameObject obj, GameObject prefab, Transform parent);
		
		void PushToPool<T>(T obj) where T : Component;
		void PushToPool<T>(T obj, Transform parent) where T : Component;
		void PushToPool<T>(T obj, T prefab) where T : Component;
		void PushToPool<T>(T obj, T prefab, Transform parent) where T : Component;

		void PushToPoolDelayed(GameObject obj, float time);
		void PushToPoolDelayed(GameObject obj, float time, Transform parent);
		void PushToPoolDelayed(GameObject obj, GameObject prefab, float time);
		void PushToPoolDelayed(GameObject obj, GameObject prefab, float time, Transform parent);
		
		void PushToPoolDelayed<T>(T obj, float time) where T : Component;

		void PushToPoolDelayed<T>(T obj, float time, Transform parent) where T : Component;
		void PushToPoolDelayed<T>(T obj, T prefab, float time) where T : Component;
		void PushToPoolDelayed<T>(T obj, T prefab, float time, Transform parent) where T : Component;

		void ReleaseItems(GameObject prefab, bool destroyObjects = false);
		void ReleasePool();
	}

	public sealed class ObjectPool : MonoBehaviour, IObjectPool
	{
		private Dictionary<GameObject, Queue<GameObject>> _container = new Dictionary<GameObject, Queue<GameObject>>();

		private bool _destroyed;
		
		private static IObjectPool _instance = null;

		public static IObjectPool Instance
		{
			get
			{
				if (_instance == null)
				{
					var obj = new GameObject("Object Pool (Recycled Cells)");
					DontDestroyOnLoad(obj);
					_instance = obj.AddComponent<ObjectPool>();
				}

				return _instance;
			}
		}

		/// <summary>
		/// Reset the pool but does not destroy the content.
		/// </summary>
		public void Reset()
		{
			_instance = null;
		}

		private void OnDestroy()
		{
			_destroyed = true;
		}

		private ObjectPool() { }

		public bool AddToPool(GameObject prefab, int count)
			=> AddToPool(prefab, count, transform);

		public bool AddToPool(GameObject prefab, int count, Transform parent)
		{
			if (prefab == null || count <= 0 || _destroyed)
			{
				return false;
			}

			for (int i = 0; i < count; i++)
			{
				GameObject obj = CreateObject(prefab, parent);
				PushToPool(obj, prefab, parent);
			}

			return true;
		}

		public GameObject PopFromPool(GameObject prefab, bool forceCreate = false, Vector3 position = default)
			=> PopFromPool(prefab, transform, forceCreate, position);
			
		public GameObject PopFromPool(GameObject prefab, Transform parent, bool forceCreate = false, Vector3 position = default)
		{
			GameObject obj = null;
			if (forceCreate)
			{
				obj = CreateObject(prefab, null);
			}
			else
			{
				Queue<GameObject> queue = FindInContainer(prefab);
				if (queue.Count > 0)
				{
					obj = queue.Dequeue();
					obj.transform.position = position;
					obj.transform.SetParent(parent, false);
					obj.SetActive(true);
				}
			}

			if (obj == null)
				obj = CreateObject(prefab, parent, position);

			if (obj.TryGetComponent(out IPoolableObject o))
			{
				o.Prefab = prefab;
				o.Init();
			}
			
			return obj;
		}
		
		
		public T PopFromPool<T>(T prefab, bool forceCreate = false, Vector3 position = default)
			where T : Component
			=> PopFromPool<T>(prefab.gameObject, forceCreate, transform, position);

		public T PopFromPool<T>(T prefab, Transform parent, bool forceCreate = false, Vector3 position = default)
			where T : Component			
			=> PopFromPool<T>(prefab.gameObject, forceCreate, parent, position);


		public T PopFromPool<T>(T prefab, bool forceCreate = false, Transform parent = null, Vector3 position = default)
			where T : Component
			=> PopFromPool<T>(prefab.gameObject, forceCreate, parent, position);

		public T PopFromPool<T>(GameObject prefab, bool forceCreate = false, Transform parent = null, Vector3 position = default)
			where T : Component
		{
			if (!prefab.TryGetComponent(out T component))
			{
				PLog.Error<UtilityLogger>("Cannot pop from pool: This prefab does not have the correct component");
				return null;
			}
			
			GameObject obj = PopFromPool(prefab, parent, forceCreate, position);

			if (obj != null && obj.TryGetComponent(out component))
				return component;
			return null;
		}

		private Queue<GameObject> FindInContainer(GameObject prefab)
		{
			if (_container.ContainsKey(prefab) == false)
			{
				_container.Add(prefab, new Queue<GameObject>());
			}

			return _container[prefab];
		}

		private GameObject CreateObject(GameObject prefab, Transform parent, Vector3 position = default)
			=> CreateObject(prefab, parent, position, Quaternion.identity);
		
		private GameObject CreateObject(GameObject prefab, Transform parent, Vector3 position, Quaternion rotation)
		{
			var newObj = Instantiate(prefab, position, rotation);
			newObj.name = prefab.name;
			newObj.transform.SetParent(parent, false);
			return newObj;
		}

		public void PushToPool(GameObject obj)
			=> PushToPool(obj, transform);

		public void PushToPool(GameObject obj, Transform parent)
		{
			if (obj == null || _destroyed) return;

			obj.SetActive(false);
			obj.transform.SetParent(parent, false);

			if (ValidateObjectAsPoolable(obj, out GameObject prefab))
			{
				Queue<GameObject> queue = FindInContainer(prefab);
				queue.Enqueue(obj);
			}
		}

		private bool ValidateObjectAsPoolable(GameObject obj, out GameObject prefab)
		{
			if (obj.TryGetComponent(out IPoolableObject poolObject))
			{
				prefab = poolObject.Prefab;

				if (prefab != null)
					return true;
				
				PLog.Error<UtilityLogger>("Pushed an object into pool with with an uninitialized IPoolObject. " +
				                          "Use the function that provides a prefab context instead");
				return false;
			}
			
			prefab = null;
			PLog.Error<UtilityLogger>("Pushed an object into pool with no IPoolObject behaviour. " +
			                          "Use the function that provides a prefab context instead");
			return false;
		}

		public void PushToPool(GameObject obj, GameObject prefab)
			=> PushToPool(obj, prefab, transform);

		public void PushToPool(GameObject obj, GameObject prefab, Transform parent)
		{
			if (obj == null || _destroyed) return;

			obj.SetActive(false);
			obj.transform.SetParent(parent, false);

			Queue<GameObject> queue = FindInContainer(prefab);
			queue.Enqueue(obj);
		}
		
		public void PushToPool<T>(T obj) where T : Component => PushToPool(obj.gameObject, transform);

		public void PushToPool<T>(T obj, Transform parent) where T : Component => PushToPool(obj.gameObject, parent);
		
		public void PushToPool<T>(T obj, T prefab) where T : Component => PushToPool(obj.gameObject, prefab.gameObject, transform);

		public void PushToPool<T>(T obj, T prefab, Transform parent) where T : Component
			=> PushToPool(obj.gameObject, prefab.gameObject, parent);

		public void PushToPoolDelayed(GameObject obj, float time)
			=> PushToPoolDelayed(obj, time, transform);

		public void PushToPoolDelayed(GameObject obj, float time, Transform parent)
		{
			if (obj == null)
				return;
			
			if (ValidateObjectAsPoolable(obj, out GameObject prefab))
				PushToPoolDelayed(obj, prefab, time, parent);
		}

		public void PushToPoolDelayed(GameObject obj, GameObject prefab, float time)
			=> PushToPoolDelayed(obj, prefab, time, transform);

		public void PushToPoolDelayed(GameObject obj, GameObject prefab, float time, Transform parent)
			=> ManagedCoroutine.Begin(PushToPoolDelayedEnumerator(obj, prefab, time, parent));

		public void PushToPoolDelayed<T>(T obj, float time)
			where T : Component
			=> PushToPoolDelayed(obj.gameObject, time, transform);

		public void PushToPoolDelayed<T>(T obj, float time, Transform parent)
			where T : Component
			=> PushToPoolDelayed(obj.gameObject, time, parent);
		
		public void PushToPoolDelayed<T>(T obj, T prefab, float time)
			where T : Component
			=> PushToPoolDelayed(obj.gameObject, prefab.gameObject, time, transform);

		public void PushToPoolDelayed<T>(T obj, T prefab, float time, Transform parent)
			where T : Component
			=> PushToPoolDelayed(obj.gameObject, prefab.gameObject, time, parent);

		private IEnumerator PushToPoolDelayedEnumerator(GameObject obj, GameObject prefab, float secondsUntilPush, Transform parent)
		{
			yield return new WaitForSeconds(secondsUntilPush);
			PushToPool(obj, prefab, parent: parent);
		}

		/// <summary>
		/// Releases the pool from all items.
		/// </summary>
		/// <param name="prefab">The prefab to be used to find the items.</param>
		/// <param name="destroyObjects">If set to <c>true</c> destroy object, else object is removed from pool but kept in scene. </param>
		public void ReleaseItems(GameObject prefab, bool destroyObjects = false)
		{
			if (prefab == null)
				return;

			Queue<GameObject> queue = FindInContainer(prefab);
			
			if (queue == null)
				return;

			while (queue.Count > 0)
			{
				GameObject obj = queue.Dequeue();
				if (destroyObjects)
					Utility.DestroyObject(obj);
			}
		}

		/// <summary>
		/// Releases all items from the pool and destroys them.
		/// </summary>
		public void ReleasePool()
		{
			foreach (var kvp in _container)
			{
				Queue<GameObject> queue = kvp.Value;
				while (queue.Count > 0)
				{
					GameObject obj = queue.Dequeue();
					Utility.DestroyObject(obj);
				}
			}

			_container = null;
			_container = new Dictionary<GameObject, Queue<GameObject>>();
		}
	}
}