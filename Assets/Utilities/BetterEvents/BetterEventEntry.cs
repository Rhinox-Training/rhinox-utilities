using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Rhinox.Lightspeed;
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
        [NonSerialized, HideInInspector] public Delegate Delegate;

        [NonSerialized, HideInInspector] public object[] ParameterValues;

        public BetterEventEntry(Delegate del)
        {
            if (del != null && del.Method != null)
            {
                this.Delegate = del;
                this.ParameterValues = new object[del.Method.GetParameters().Length];
            }
        }

        /// <summary>
        /// Note: Be extremely careful using this method! Certain parameters can cause issues!
        /// i.e. Do not pass through the host of the BetterEvent if it is not a UnityObject; can cause StackOverflow (sometimes)
        /// </summary>
        public BetterEventEntry(Delegate del, params object[] parameters)
        {
            if (del != null && del.Method != null)
            {
                this.Delegate = del;
                Array.Resize(ref parameters, del.Method.GetParameters().Length);
                this.ParameterValues = parameters;
            }
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
                unityObjs.Add(Delegate.Target as UnityEngine.Object);
                _targetType = new SerializableType(Delegate.Method.DeclaringType);
                _methodName = Delegate.Method.Name;
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

            var targetRef = unityReferences.FirstOrDefault();
            if (targetRef == null)
                this.Delegate = System.Delegate.CreateDelegate(_targetType.Type, _targetType.Type.GetMethod(_methodName, BindingFlags.Static | BindingFlags.Public));
            else
                this.Delegate = Delegate.CreateDelegate(_targetType.Type, targetRef, _methodName);
#endif
        }

        #endregion
    }
}