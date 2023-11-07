using System;
using System.Linq;
using Rhinox.Perceptor;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace Rhinox.Utilities
{
    public interface ISingleton { bool IsDestroying { get; set; }  }

    // Reason of this interface -> disallows you from doing Singleton<Monobehaviour> & making mistakes
    public interface ISingleton<out T> : ISingleton where T : MonoBehaviour { }
    
    /// <summary>
    /// NOTE: this only spawns one if there isn't one yet
    /// Use HasInstance to prevent this behaviour
    /// If you wish to manage destruction (only 1 instance) use ShouldDestroy
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class Singleton<T> : MonoBehaviour, ISingleton<T>
        where T : MonoBehaviour, ISingleton<T>
    {
        public static bool HasInstance
        {
            get
            {
                if (_instance != null && !_instance.IsDestroying) return true;
                
                _instance = FindInstance();

                return _instance != null;
            }
        }

        private static T _instance;
        
        public static T Instance
        {
            get
            {
                FindOrCreate();

                return _instance;
            }
            protected set
            {
                _instance = value;
            }
        }

        public static void FindOrCreate()
        {
            if (_instance == null || _instance.IsDestroying)
                _instance = FindInstance();

            if (_instance == null)
                _instance = CreateSingleton();
        }

        public bool IsDestroying { get; set; }
        
        // No need to check for HasInstance as this is called on an Instance
        protected bool ShouldDestroy() => Instance != this;

        protected virtual void OnDestroy()
        {
            PLog.Trace<UtilityLogger>($"Destroying Singleton {name} (Type:{typeof(T).Name})");
            IsDestroying = true;
        }

        private static T FindInstance()
        {
            var instances = FindObjectsOfType<T>();
            var instance = instances.FirstOrDefault(x => !x.IsDestroying);
            return instance;
        }

        private static T CreateSingleton()
        {
            Debug.LogWarning($"Singleton::Instance - Created instance of ({typeof(T)})");

            var singleton = new GameObject();
            T instance = singleton.AddComponent<T>();
            singleton.name = "(singleton) " + typeof(T);

            DontDestroyOnLoad(singleton);
            return instance;
        }

    }
    
#if ODIN_INSPECTOR    
    public abstract class SerializedSingleton<T> : SerializedMonoBehaviour, ISingleton<T>
        where T : SerializedMonoBehaviour, ISingleton<T>
    {
        public static bool HasInstance
        {
            get
            {
                if (_instance != null && !_instance.IsDestroying) return true;
                
                _instance = FindInstance();

                return _instance != null;
            }
        }

        private static T _instance;
        
        public static T Instance
        {
            get
            {
                if (_instance == null || _instance.IsDestroying)
                    _instance = FindInstance();
                
                if (_instance == null && Application.isPlaying)
                {
                    PLog.Warn<UtilityLogger>($"SerializedSingleton::Instance - Created instance of ({typeof(T)})");
                    var singleton = new GameObject();
                    _instance = singleton.AddComponent<T>();
                    singleton.name = "(singleton) "+ typeof(T);
						
                    DontDestroyOnLoad(singleton);
                }

                return _instance;

            }
            set { _instance = value; }
        }
        
        public bool IsDestroying { get; set; }
        
        // No need to check for HasInstance as this is called on an Instance
        protected bool ShouldDestroy() => Instance != this;

        protected virtual void OnDestroy()
        {
            PLog.Trace<UtilityLogger>($"Destroying SerializedSingleton {name} (Type:{typeof(T).Name})");
            IsDestroying = true;
        }

        private static T FindInstance()
        {
            var instances = FindObjectsOfType<T>();
            var instance = instances.FirstOrDefault(x => !x.IsDestroying);
            return instance;
        }
    }
#endif
}

