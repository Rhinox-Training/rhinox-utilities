#if ODIN_INSPECTOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Sirenix.Utilities;
using UnityEngine;

namespace Rhinox.Utilities
{
    public class SimpleEventBehaviour : MonoBehaviour
    {
        public BetterEvent Event;

        public bool FireOnStart;

        private void Start()
        {
            if (FireOnStart)
                Event.Invoke();
        }

        public void RepeatEvent(float waitSeconds)
        {
            IEnumerator Repeat()
            {
                yield return new WaitForSeconds(waitSeconds);
                Event.Invoke();
            }

            StartCoroutine(Repeat());
        }

        public void DebugValue(object o)
        {
            if (o == null) return;

            Debug.Log(o);
            var fields = o.GetType().GetMembers(Flags.AllMembers);
            foreach (var member in fields)
            {
                if (member is MethodBase) continue;

                if (member.IsStatic())
                    Debug.Log($"\t[STATIC] {member.Name} = {member.GetMemberValue(null)}");
                else
                    Debug.Log($"\t{member.Name} = {member.GetMemberValue(o)}");
            }
        }

        public void DebugType(Type type)
        {
            if (type == null) return;

            Debug.Log(type.AssemblyQualifiedName);
            var fields = type.GetMembers(Flags.StaticAnyVisibility);
            foreach (var member in fields)
            {
                if (member is MethodInfo) continue;

                Debug.Log($"\t[STATIC] {member.Name} = {member.GetMemberValue(null)}");
            }
        }
    }
}
#endif