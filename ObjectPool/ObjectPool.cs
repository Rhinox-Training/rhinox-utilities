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
		bool AddToPool(GameObject template, int count);
		bool AddToPool(GameObject template, Transform parent, int count);
		
		bool AddToPool<T>(T template, int count) where T : Component;
		bool AddToPool<T>(T template, Transform parent, int count) where T : Component;

		GameObject PopFromPool(GameObject template, bool forceCreate = false, Vector3 position = default);
		GameObject PopFromPool(GameObject template, Transform parent, bool forceCreate = false, Vector3 position = default);

		T PopFromPool<T>(T template, bool forceCreate = false, Vector3 position = default) where T : Component;
		T PopFromPool<T>(T template, Transform parent, bool forceCreate = false, Vector3 position = default) where T : Component;

		void PushToPool(GameObject obj, bool preserveParent = true);
		void PushToPool(GameObject obj, Transform parent);
		void PushToPool(GameObject obj, GameObject template, bool preserveParent = true);
		void PushToPool(GameObject obj, GameObject template, Transform parent);
		
		void PushToPool<T>(T obj, bool preserveParent = true) where T : Component;
		void PushToPool<T>(T obj, Transform parent) where T : Component;
		void PushToPool<T>(T obj, T template, bool preserveParent = true) where T : Component;
		void PushToPool<T>(T obj, T template, Transform parent) where T : Component;

		void ReleaseItems(GameObject template, bool destroyObjects = false);
		void ReleasePool();
	}

	public sealed class ObjectPool : MonoBehaviour, IObjectPool
	{
		private Dictionary<GameObject, Stack<GameObject>> _objectsByTemplate = new Dictionary<GameObject, Stack<GameObject>>();

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

		public bool AddToPool<T>(T template, int count) where T : Component
			=> AddToPool(template.gameObject, transform, count);

		public bool AddToPool<T>(T template, Transform parent, int count) where T : Component	
			=> AddToPool(template.gameObject, parent, count);

		public bool AddToPool(GameObject template, int count)
			=> AddToPool(template, transform, count);

		public bool AddToPool(GameObject template, Transform parent, int count)
		{
			if (template == null || count <= 0 || _destroyed)
			{
				return false;
			}

			for (int i = 0; i < count; i++)
			{
				GameObject obj = CreateObject(template, parent);
				PushToPool(obj, template, parent);
			}

			return true;
		}

		public T PopFromPool<T>(T template, bool forceCreate = false, Vector3 position = default) where T : Component
			=> PopFromPool<T>(template.gameObject, forceCreate, transform, position);

		public T PopFromPool<T>(T template, Transform parent, bool forceCreate = false, Vector3 position = default) where T : Component			
			=> PopFromPool<T>(template.gameObject, forceCreate, parent, position);


		public T PopFromPool<T>(T template, bool forceCreate = false, Transform parent = null, Vector3 position = default) where T : Component
			=> PopFromPool<T>(template.gameObject, forceCreate, parent, position);

		public T PopFromPool<T>(GameObject template, bool forceCreate = false, Transform parent = null, Vector3 position = default)
			where T : Component
		{
			if (!template.TryGetComponent(out T component))
			{
				PLog.Error<UtilityLogger>("Cannot pop from pool: This template does not have the correct component");
				return null;
			}
			
			GameObject obj = PopFromPool(template, parent, forceCreate, position);

			if (obj != null && obj.TryGetComponent(out component))
				return component;
			return null;
		}

		public GameObject PopFromPool(GameObject template, bool forceCreate = false, Vector3 position = default)
			=> PopFromPool(template, transform, forceCreate, position);
			
		public GameObject PopFromPool(GameObject template, Transform parent, bool forceCreate = false, Vector3 position = default)
		{
			GameObject obj = null;
			if (forceCreate)
			{
				obj = CreateObject(template, parent);
			}
			else
			{
				Stack<GameObject> stack = FindInContainer(template);
				if (stack.Count > 0)
				{
					obj = stack.Pop();
					obj.transform.position = position;
					obj.transform.SetParent(parent, false);
					obj.SetActive(true);
				}
				else
					obj = CreateObject(template, parent, position);
			}
			
			if (obj.TryGetComponent(out IPoolableObject o))
				o.Init(this, template);

			obj.SetActive(true);
			return obj;
		}

		private Stack<GameObject> FindInContainer(GameObject template)
		{
			if (_objectsByTemplate.ContainsKey(template) == false)
			{
				_objectsByTemplate.Add(template, new Stack<GameObject>());
			}

			return _objectsByTemplate[template];
		}

		private GameObject CreateObject(GameObject template, Transform parent, Vector3 position = default)
			=> CreateObject(template, parent, position, Quaternion.identity);
		
		private GameObject CreateObject(GameObject template, Transform parent, Vector3 position, Quaternion rotation)
		{
			var newObj = Instantiate(template, position, rotation);
			newObj.name = template.name;
			newObj.transform.SetParent(parent, false);
			return newObj;
		}

		public void PushToPool<T>(T obj, bool preserveParent = true) where T : Component
			=> PushToPool(obj.gameObject, preserveParent);

		public void PushToPool<T>(T obj, Transform parent) where T : Component
			=> PushToPool(obj.gameObject, parent);
		
		public void PushToPool<T>(T obj, T template, bool preserveParent = true) where T : Component
			=> PushToPool(obj.gameObject, template.gameObject, preserveParent);

		public void PushToPool<T>(T obj, T template, Transform parent) where T : Component
			=> PushToPool(obj.gameObject, template.gameObject, parent);

		public void PushToPool(GameObject obj, bool preserveParent = true)
		{
			if (obj == null) return;

			PushToPool(obj, preserveParent ? obj.transform.parent : transform);
		}

		public void PushToPool(GameObject obj, Transform parent)
		{
			if (obj == null || _destroyed) return;

			obj.SetActive(false);
			obj.transform.SetParent(parent, false);

			if (ValidateObjectAsPoolable(obj, out GameObject template))
			{
				Stack<GameObject> stack = FindInContainer(template);
				stack.Push(obj);
			}
		}

		private bool ValidateObjectAsPoolable(GameObject obj, out GameObject template)
		{
			if (obj.TryGetComponent(out IPoolableObject poolObject))
			{
				template = poolObject.Template;

				if (template != null)
					return true;
				
				PLog.Error<UtilityLogger>("Pushed an object into pool with with an uninitialized IPoolObject. " +
				                          "Use the function that provides a template context instead");
				return false;
			}
			
			template = null;
			PLog.Error<UtilityLogger>("Pushed an object into pool with no IPoolObject behaviour. " +
			                          "Use the function that provides a template context instead");
			return false;
		}

		public void PushToPool(GameObject obj, GameObject template, bool preserveParent = true)
			=> PushToPool(obj, template, preserveParent ? obj.transform.parent : transform);

		public void PushToPool(GameObject obj, GameObject template, Transform parent)
		{
			if (obj == null || _destroyed) return;

			obj.SetActive(false);
			obj.transform.SetParent(parent, false);

			Stack<GameObject> stack = FindInContainer(template);
			stack.Push(obj);
		}
		
		/// <summary>
		/// Releases the pool from all items.
		/// </summary>
		/// <param name="template">The template to be used to find the items.</param>
		/// <param name="destroyObjects">If set to <c>true</c> destroy object, else object is removed from pool but kept in scene. </param>
		public void ReleaseItems(GameObject template, bool destroyObjects = false)
		{
			if (template == null)
				return;

			Stack<GameObject> stack = FindInContainer(template);
			
			if (stack == null)
				return;

			while (stack.Count > 0)
			{
				GameObject obj = stack.Pop();
				if (destroyObjects)
					Utility.DestroyObject(obj);
			}
		}

		/// <summary>
		/// Releases all items from the pool and destroys them.
		/// </summary>
		public void ReleasePool()
		{
			foreach (var kvp in _objectsByTemplate)
			{
				Stack<GameObject> stack = kvp.Value;
				while (stack.Count > 0)
				{
					GameObject obj = stack.Pop();
					Utility.DestroyObject(obj);
				}
			}

			_objectsByTemplate = null;
			_objectsByTemplate = new Dictionary<GameObject, Stack<GameObject>>();
		}
	}
}