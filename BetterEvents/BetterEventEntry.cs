using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Rhinox.Lightspeed;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;
#if ODIN_INSPECTOR
using Sirenix.Serialization;
#endif

namespace Rhinox.Utilities
{
    [Serializable]
    public class BetterEventEntry : ISerializationCallbackReceiver
    {
        public BindingFlags AllowedFlags
        {
            get => _flags;
            set => _flags = value;
        }

        // Keep original target so we have a backup in case of static methods
        private Object _originalTarget;
        public Object Target => Delegate?.Target == null ? _originalTarget : Delegate.Target as Object;
        
        // Delegate so we can support both lambda/actions and bound MethodInfos
        [NonSerialized, HideInInspector] public Delegate Delegate;

        [NonSerialized, HideInInspector] public object[] ParameterValues;

        [SerializeField, HideInInspector] private bool _initialized;
        [SerializeField, HideInInspector] private BindingFlags _flags;

        public BetterEventEntry(Delegate del)
        {
            if (del != null)
                InitDelegate(del);
        }

        /// <summary>
        /// Note: Be extremely careful using this method! Certain parameters can cause issues!
        /// i.e. Do not pass through the host of the BetterEvent if it is not a UnityObject; can cause StackOverflow (sometimes)
        /// </summary>
        public BetterEventEntry(Delegate del, params object[] parameters)
        {
            if (del != null)
            {
                ParameterValues = parameters;
                InitDelegate(del);
            }
        }
        
        private void InitDelegate(Delegate del)
        {
            Delegate = del;
            var argumentsLength = del.Method.GetParameters().Length;
            if (ParameterValues == null)
                ParameterValues = new object[argumentsLength];
            else
                Array.Resize(ref ParameterValues, argumentsLength);
        }
        
        public void Invoke()
        {
            if (this.Delegate != null && this.ParameterValues != null)
            {
                // This is faster than Dynamic Invoke.
                this.Delegate.Method.Invoke(this.Delegate.Target, this.ParameterValues);
            }
        }

        #region Serialization
        
        [SerializeField, HideInInspector] private List<UnityEngine.Object> unityReferences;
#if ODIN_INSPECTOR
        [SerializeField, HideInInspector] private byte[] bytes;


        private struct OdinSerializedData
        {
            public Delegate Delegate;
            public object[] ParameterValues;
        }
#else

        [SerializeReference] private List<object> _parameters;
        [SerializeField] private SerializableType _targetType;
        [SerializeField] private string _methodName;
        [SerializeField] private SerializableType[] _methodParameterTypes;

        
        private struct UnityObjectParameter
        {
            public int Index;
        }
#endif
        
        public void OnBeforeSerialize()
        {
#if ODIN_INSPECTOR
            var val = new OdinSerializedData() {Delegate = this.Delegate, ParameterValues = this.ParameterValues};
            this.bytes = SerializationUtility.SerializeValue(val, DataFormat.Binary, out this.unityReferences);
#else
            if (!_initialized)
            {
                _flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
                
                _initialized = true;
            }
            if (Delegate == null)
            {
                unityReferences = new List<Object>();
                _parameters = new List<object>();
                _targetType = null;
                _methodName = null;
            }
            else
            {
                var unityObjs = new List<UnityEngine.Object>();
                unityObjs.Add(Target); 
                _parameters = new List<object>();
                _targetType = new SerializableType(Delegate.Method.DeclaringType);
                _methodName = Delegate.Method.Name;
                _methodParameterTypes = Delegate.Method.GetParameters().Select(x => new SerializableType(x.ParameterType)).ToArray();
                foreach (var parameterValue in ParameterValues)
                {
                    if (parameterValue is UnityEngine.Object objParam)
                    {
                        _parameters.Add(new UnityObjectParameter() { Index = unityObjs.Count });
                        unityObjs.Add(objParam);
                    }
                    else
                        _parameters.Add(parameterValue);
                }

                this.unityReferences = unityObjs;
            }
#endif       
        }

        public void OnAfterDeserialize()
        {
#if ODIN_INSPECTOR
            var val = SerializationUtility.DeserializeValue<OdinSerializedData>(this.bytes, DataFormat.Binary, this.unityReferences);
            this.Delegate = val.Delegate;
            this.ParameterValues = val.ParameterValues;
#else
            if (_targetType == null || _targetType.Type == null)
                return;
            
            this.ParameterValues = new object[_parameters.Count];
            for (int i = 0; i < _parameters.Count; ++i)
            {
                var parameterValue = _parameters[i];
                if (parameterValue is UnityObjectParameter objParam)
                {
                    ParameterValues[i] = unityReferences[objParam.Index];
                }
                else
                    ParameterValues[i] = parameterValue;
            }

            _originalTarget = unityReferences.FirstOrDefault();
            
            MethodInfo[] possibleInfos = _targetType.Type.GetMethods(_flags);
            var info = possibleInfos.FirstOrDefault(MatchParameter);
            CreateAndAssignNewDelegate(_originalTarget, info);
#endif
        }

        private bool MatchParameter(MethodInfo info)
        {
            if (info.Name != _methodName)
                return false;
            
            var parameters = info.GetParameters();
            
            if (parameters.Length != _methodParameterTypes.Length)
                return false;

            for (var i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].ParameterType != _methodParameterTypes[i])
                    return false;
            }

            return true;
        }

        public void CreateAndAssignNewDelegate(object target, MethodInfo info)
        {
            if (info == null)
            {
                Delegate = null;
                return;
            }
            
            var pTypes = info.GetParameters().Select(x => x.ParameterType).ToArray();
            var args = new object[pTypes.Length];

            Type delegateType = null;

            if (info.ReturnType == typeof(void))
            {
                if (args.Length == 0) delegateType = typeof(Action);
                else if (args.Length == 1) delegateType = typeof(Action<>).MakeGenericType(pTypes);
                else if (args.Length == 2) delegateType = typeof(Action<,>).MakeGenericType(pTypes);
                else if (args.Length == 3) delegateType = typeof(Action<,,>).MakeGenericType(pTypes);
                else if (args.Length == 4) delegateType = typeof(Action<,,,>).MakeGenericType(pTypes);
                else if (args.Length == 5) delegateType = typeof(Action<,,,,>).MakeGenericType(pTypes);
                else if (args.Length == 6) delegateType = typeof(Action<,,,,,>).MakeGenericType(pTypes);
                else if (args.Length == 7) delegateType = typeof(Action<,,,,,,>).MakeGenericType(pTypes);
            }
            else
            {
                pTypes = pTypes.Append(info.ReturnType).ToArray();
                if (args.Length == 0) delegateType = typeof(Func<>).MakeGenericType(pTypes);
                else if (args.Length == 1) delegateType = typeof(Func<,>).MakeGenericType(pTypes);
                else if (args.Length == 2) delegateType = typeof(Func<,,>).MakeGenericType(pTypes);
                else if (args.Length == 3) delegateType = typeof(Func<,,,>).MakeGenericType(pTypes);
                else if (args.Length == 4) delegateType = typeof(Func<,,,,>).MakeGenericType(pTypes);
                else if (args.Length == 5) delegateType = typeof(Func<,,,,,>).MakeGenericType(pTypes);
                else if (args.Length == 6) delegateType = typeof(Func<,,,,,,>).MakeGenericType(pTypes);
                else if (args.Length == 7) delegateType = typeof(Func<,,,,,,,>).MakeGenericType(pTypes);
            }
            
            if (delegateType == null)
            {
                Debug.LogError("Unsupported Method Type");
                return;
            }
            
            if (info.IsStatic)
                target = null;
            
            var del = Delegate.CreateDelegate(delegateType, target, info);
            InitDelegate(del);
        }

        #endregion
    }
}