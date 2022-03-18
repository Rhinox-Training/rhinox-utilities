#if ODIN_INSPECTOR
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using UnityEngine;

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

        #region OdinSerialization

        [SerializeField, HideInInspector] private List<UnityEngine.Object> unityReferences;

        [SerializeField, HideInInspector] private byte[] bytes;

        public void OnBeforeSerialize()
        {
            var val = new OdinSerializedData() {Delegate = this.Delegate, ParameterValues = this.ParameterValues};
            this.bytes = SerializationUtility.SerializeValue(val, DataFormat.Binary, out this.unityReferences);
        }

        public void OnAfterDeserialize()
        {
            var val = SerializationUtility.DeserializeValue<OdinSerializedData>(this.bytes, DataFormat.Binary, this.unityReferences);
            this.Delegate = val.Delegate;
            this.ParameterValues = val.ParameterValues;
        }

        private struct OdinSerializedData
        {
            public Delegate Delegate;
            public object[] ParameterValues;
        }

        #endregion
    }
}
#endif