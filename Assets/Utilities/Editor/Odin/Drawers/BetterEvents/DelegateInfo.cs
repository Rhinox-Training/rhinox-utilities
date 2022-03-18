using System.Reflection;

namespace Rhinox.Utilities.Odin.Editor
{
    public struct DelegateInfo
    {
        public DelegateInfo(UnityEngine.Object target, MethodInfo method, string name = null)
        {
            Target = target;
            Method = method;
            MethodName = name ?? Method?.Name.Replace("add_", "");
            EventInfo = null;
        }

        public DelegateInfo(UnityEngine.Object target, EventInfo ei, string name = null)
        {
            Target = target;
            EventInfo = ei;
            Method = ei.AddMethod;
            MethodName = name ?? ei.Name;
        }

        public UnityEngine.Object Target;
        public MethodInfo Method;
        public string MethodName;

        public EventInfo EventInfo;
    }

    public struct EventDelegateInfo
    {
        public EventDelegateInfo(UnityEngine.Object target, EventInfo eventInfo)
        {
            Target = target;
            EventInfo = eventInfo;
        }

        public UnityEngine.Object Target;
        public EventInfo EventInfo;

        public MethodInfo AddMethod => EventInfo.AddMethod;
        public MethodInfo RemoveMethod => EventInfo.RemoveMethod;
        public MethodInfo RaiseMethod => EventInfo.RaiseMethod;
    }
}