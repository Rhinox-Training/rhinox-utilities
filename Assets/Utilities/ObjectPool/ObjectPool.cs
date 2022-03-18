using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Rhinox.Utilities
{
	public interface IObjectPool
	{
		bool AddToPool(GameObject prefab, int count, Transform parent = null);

		GameObject PopFromPool(GameObject prefab, bool forceInstantiate = false, bool instantiateIfNone = true,
			Transform container = null, Vector3 position = default(Vector3));

		T PopFromPool<T>(T poolObject, bool forceInstantiate = false, bool instantiateIfNone = true,
			Transform container = null, Vector3 position = default(Vector3))
			where T : Component, IPoolableObject;

		T PopFromPool<T>(GameObject poolObject, bool forceInstantiate = false, bool instantiateIfNone = true,
			Transform container = null, Vector3 position = default(Vector3))
			where T : Component, IPoolableObject;

		void PushToPool(GameObject obj, bool retainObject = true, Transform newParent = null);

		void ReleaseItems(GameObject prefab, bool destroyObject = false);

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

		private ObjectPool()
		{
		}

		/// <summary>
		/// Adds to pool.
		/// </summary>
		/// <returns><c>true</c>, if item was successfully created, <c>false</c> otherwise.</returns>
		/// <param name="prefab">The prefab to instantiate new items.</param>
		/// <param name="count">The amount of instances to be created.</param>
		/// <param name="parent">The Transform container to store the items. If null, items are placed as parent</param>
		public bool AddToPool(GameObject prefab, int count, Transform parent = null)
		{
			if (prefab == null || count <= 0 || _destroyed)
			{
				return false;
			}

			for (int i = 0; i < count; i++)
			{
				GameObject obj = PopFromPool(prefab, true, false, parent);
				PushToPool(obj, true, parent);
			}

			return true;
		}

		/// <summary>
		/// Pops item from pool.
		/// </summary>
		/// <returns>The from pool.</returns>
		/// <param name="prefab">Prefab to be used. Matches the prefab used to create the instance</param>
		/// <param name="forceInstantiate">If set to <c>true</c> force instantiate regardless the pool already contains the same item.</param>
		/// <param name="instantiateIfNone">If set to <c>true</c> instantiate if no item is found in the pool.</param>
		/// <param name="container">The Transform container to store the popped item.</param>
		/// <param name="position">Initial position of the popped item.</param>
		public GameObject PopFromPool(GameObject prefab, bool forceInstantiate = false, bool instantiateIfNone = true, Transform container = null, Vector3 position = default(Vector3))
		{
			GameObject obj = null;
			IPoolableObject poolObj = null;
			if (forceInstantiate)
			{
				poolObj = CreateObject<IPoolableObject>(prefab, null, out obj);
			}
			else
			{
				Queue<GameObject> queue = FindInContainer(prefab);
				if (queue.Count > 0)
				{
					obj = queue.Dequeue();
					obj.transform.position = position;
					obj.SetActive(true);
					obj.transform.SetParent(container, false);

					poolObj = obj.GetComponent<IPoolableObject>();
				}
			}

			if (obj == null && instantiateIfNone)
				poolObj = CreateObject<IPoolableObject>(prefab, container, position, out obj);

			if (poolObj != null)
				poolObj.Init();
			return obj;
		}

		public T PopFromPool<T>(T prefabPoolObject, bool forceInstantiate = false, bool instantiateIfNone = true,
			Transform container = null, Vector3 position = default(Vector3))
			where T : Component, IPoolableObject
		{
			return PopFromPool<T>(prefabPoolObject.gameObject, forceInstantiate, instantiateIfNone, container, position);
		}

		public T PopFromPool<T>(GameObject prefabPoolObject, bool forceInstantiate = false,
			bool instantiateIfNone = true, Transform container = null, Vector3 position = default(Vector3)) where T : Component, IPoolableObject
		{
			GameObject obj = null;
			T poolObject = null;
			var prefab = prefabPoolObject;

			if (forceInstantiate)
			{
				poolObject = CreateObject<T>(prefab, null, out obj);
			}
			else
			{
				Queue<GameObject> queue = FindInContainer(prefab);
				if (queue.Count > 0)
				{
					obj = queue.Dequeue();
					obj.transform.position = position;
					obj.SetActive(true);
					obj.transform.SetParent(container, false);

					poolObject = obj.GetComponent<T>();
				}
			}

			if (obj == null && instantiateIfNone)
				poolObject = CreateObject<T>(prefab, container, position, out obj);

			if (poolObject != null)
				poolObject.Init();
			return poolObject;
		}

		private Queue<GameObject> FindInContainer(GameObject prefab)
		{
			if (_container.ContainsKey(prefab) == false)
			{
				_container.Add(prefab, new Queue<GameObject>());
			}

			return _container[prefab];
		}

		private T CreateObject<T>(GameObject prefab, Transform container, out GameObject newObj)
			where T : IPoolableObject
		{
			return CreateObject<T>(prefab, container, Vector3.zero, Quaternion.identity, out newObj);
		}

		private T CreateObject<T>(GameObject prefab, Transform container, Vector3 position, out GameObject newObj)
			where T : IPoolableObject
		{
			return CreateObject<T>(prefab, container, position, Quaternion.identity, out newObj);
		}

		private T CreateObject<T>(GameObject prefab, Transform container, Vector3 position, Quaternion rotation,
			out GameObject newObj)
			where T : IPoolableObject
		{
			T poolObjectPrefab = prefab.GetComponent<T>();
			if (poolObjectPrefab == null)
			{
				Debug.Log("Wrong type of object");
				newObj = null;
				return default(T);
			}

			newObj = Instantiate(prefab, position, rotation);
			T poolObject = newObj.GetComponent<T>();
			newObj.name = prefab.name;
			poolObject.Prefab = prefab;
			newObj.transform.SetParent(container, false);
			return poolObject;
		}

		/// <summary>
		/// Pushs back the item to the pool.
		/// </summary>
		/// <param name="obj">A reference to the item to be pushed back.</param>
		/// <param name="retainObject">If set to <c>true</c> retain object.</param>
		/// <param name="newParent">The Transform container to store the item.</param>
		public void PushToPool(GameObject obj, bool retainObject = true, Transform newParent = null)
		{
			if (obj == null || _destroyed) return;

			if (retainObject == false)
			{
				Destroy(obj);
				return;
			}

			obj.SetActive(false);
			obj.transform.SetParent(newParent ? newParent : transform, false);

			IPoolableObject poolObject = obj.GetComponent<IPoolableObject>();
			if (poolObject != null)
			{
				GameObject prefab = poolObject.Prefab;

				if (prefab == null)
				{
					Debug.Log("Pushed obj to pool with uninitialized Prefab.");
					return;
				}

				Queue<GameObject> queue = FindInContainer(prefab);
				queue.Enqueue(obj);
			}
			else
				Debug.Log("Pushed obj to pool with no IPoolObject behaviour.");
		}

		/// <summary>
		/// Releases the pool from all items.
		/// </summary>
		/// <param name="prefab">The prefab to be used to find the items.</param>
		/// <param name="destroyObject">If set to <c>true</c> destroy object, else object is removed from pool but kept in scene. </param>
		public void ReleaseItems(GameObject prefab, bool destroyObject = false)
		{
			if (prefab == null)
			{
				return;
			}

			Queue<GameObject> queue = FindInContainer(prefab);
			if (queue == null)
			{
				return;
			}

			while (queue.Count > 0)
			{
				GameObject obj = queue.Dequeue();
				if (destroyObject)
				{
					Destroy(obj);
				}
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
					Object.Destroy(obj);
				}
			}

			_container = null;
			_container = new Dictionary<GameObject, Queue<GameObject>>();
		}
	}
}