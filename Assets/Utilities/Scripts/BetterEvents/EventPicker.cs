#if ODIN_INSPECTOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace Rhinox.Utilities
{
    [Serializable, HideReferenceObjectPicker]
    public class EventPicker : ISerializationCallbackReceiver
    {
        [HideInInspector] public string EventName;

        [NonSerialized, HideInInspector] public Delegate EventAdder;

        [NonSerialized, HideInInspector] public object[] ParameterValues;

        public EventPicker() : this(null)
        {
        }

        public EventPicker(Delegate del, EventInfo ei = null)
        {
            if (del != null && del.Method != null)
            {
                this.EventAdder = del;
                this.ParameterValues = new object[del.Method.GetParameters().Length];
            }

            EventName = ei?.Name;
        }

        public void ApplyEventBinding(params object[] parameterValues)
        {
            if (parameterValues.Length != ParameterValues.Length)
                throw new ArgumentException("Cannot apply Event Bindings, Parameter count does not match.");

            if (this.EventAdder != null && parameterValues != null)
            {
                // This is faster than Dynamic Invoke.
                this.EventAdder.Method.Invoke(this.EventAdder.Target, parameterValues);
            }
        }

        public Delegate ApplyEventBinding(Action action)
        {
            return AddEventHandler(EventName, EventAdder.Target, action);
        }

        public void RemoveEventBinding(Delegate del)
        {
            var item = EventAdder.Target;
            if (item == null) return;

            var eventInfo = item.GetType().GetEvent(EventName);

            if (eventInfo == null) return;

            eventInfo.RemoveEventHandler(item, del);
        }


        #region OdinSerialization

        [SerializeField, HideInInspector] private List<UnityEngine.Object> unityReferences;

        [SerializeField, HideInInspector] private byte[] bytes;

        public void OnAfterDeserialize()
        {
            var val = SerializationUtility.DeserializeValue<OdinSerializedData>(this.bytes, DataFormat.Binary, this.unityReferences);
            this.EventAdder = val.Delegate;
            this.ParameterValues = val.ParameterValues;
        }

        public void OnBeforeSerialize()
        {
            var val = new OdinSerializedData() {Delegate = this.EventAdder, ParameterValues = this.ParameterValues};
            this.bytes = SerializationUtility.SerializeValue(val, DataFormat.Binary, out this.unityReferences);
        }

        private struct OdinSerializedData
        {
            public Delegate Delegate;
            public object[] ParameterValues;
        }

        #endregion

        static Delegate AddEventHandler(string eventName, object item, Action action)
        {
            var eventInfo = item.GetType().GetEvent(eventName);

            return AddEventHandler(eventInfo, item, action);
        }

        static Delegate AddEventHandler(EventInfo eventInfo, object item, Action action)
        {
            var parameters = eventInfo.EventHandlerType
                .GetMethod("Invoke")
                .GetParameters()
                .Select(parameter => Expression.Parameter(parameter.ParameterType))
                .ToArray();

            var handler = Expression.Lambda(
                    eventInfo.EventHandlerType,
                    Expression.Call(Expression.Constant(action), "Invoke", Type.EmptyTypes),
                    parameters
                )
                .Compile();

            eventInfo.AddEventHandler(item, handler);

            return handler;
        }

        static void AddEventHandler(EventInfo eventInfo, object item, Action<object, EventArgs> action)
        {
            var parameters = eventInfo.EventHandlerType
                .GetMethod("Invoke")
                .GetParameters()
                .Select(parameter => Expression.Parameter(parameter.ParameterType))
                .ToArray();

            var invoke = action.GetType().GetMethod("Invoke");

            var handler = Expression.Lambda(
                    eventInfo.EventHandlerType,
                    Expression.Call(Expression.Constant(action), invoke, parameters[0], parameters[1]),
                    parameters
                )
                .Compile();

            eventInfo.AddEventHandler(item, handler);
        }
    }
}
#endif