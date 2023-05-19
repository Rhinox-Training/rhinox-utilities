using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rhinox.Utilities
{
    [Serializable, HideLabel]
    public struct BetterEvent
    {
        [HideReferenceObjectPicker, ListDrawerSettings(OnTitleBarGUI = "DrawInvokeButton")]
        [LabelText("$property.Parent.NiceName")]
        public List<BetterEventEntry> Events;

        public BetterEvent(params BetterEventEntry[] entries)
        {
            Events = new List<BetterEventEntry>(entries);
        }

        public void Invoke()
        {
            if (this.Events == null) return;
            for (int i = 0; i < this.Events.Count; i++)
            {
                this.Events[i].Invoke();
            }
        }

        public void AddListener(Action action)
        {
            if (Events == null)
                Events = new List<BetterEventEntry>();

            Events.Add(new BetterEventEntry(action));
        }

        public void AddListener<T>(Action<T> action, T param)
        {
            if (Events == null)
                Events = new List<BetterEventEntry>();

            Events.Add(new BetterEventEntry(action, param));
        }

        public void AddListener<T1, T2>(Action<T1, T2> action, T1 param1, T2 param2)
        {
            if (Events == null)
                Events = new List<BetterEventEntry>();

            Events.Add(new BetterEventEntry(action, param1, param2));
        }

        public void AddListener<T1, T2, T3>(Action<T1, T2, T3> action, T1 param1, T2 param2, T3 param3)
        {
            if (Events == null)
                Events = new List<BetterEventEntry>();

            Events.Add(new BetterEventEntry(action, param1, param2, param3));
        }

        public void RemoveListener(Action action)
        {
            Events?.RemoveAll(x => x.Delegate == (Delegate) action);
        }

        public void RemoveListener(Delegate action)
        {
            Events?.RemoveAll(x => x.Delegate == action);
        }

        public static BetterEvent operator +(BetterEvent e, Action a)
        {
            e.AddListener(a);
            return e;
        }

        public static BetterEvent operator -(BetterEvent e, Action a)
        {
            e.RemoveListener(a);
            return e;
        }

#if UNITY_EDITOR
        private void DrawInvokeButton()
        {
            if (GUILayout.Button("Invoke"))
                this.Invoke();
        }
#endif
    }
}