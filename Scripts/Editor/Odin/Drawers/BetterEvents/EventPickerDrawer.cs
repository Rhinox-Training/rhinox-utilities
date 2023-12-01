using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Rhinox.Utilities.Odin.Editor
{
    public class EventTargetInfo
    {
        public EventTargetInfo(EventInfo info)
        {
            Info = info;
        }
    
        public EventInfo Info;
        public MethodInfo Method => Info.AddMethod;
        public Object Target;
    }

    
    public class EventPickerDrawer : OdinValueDrawer<EventPicker>
    {
        private UnityEngine.Object tmpTarget;

        protected override void DrawPropertyLayout(GUIContent label)
        {
            //SirenixEditorGUI.BeginBox();
            //{
            //    SirenixEditorGUI.BeginToolbarBoxHeader();
            //    {
            var rect = GUILayoutUtility.GetRect(0, 19);
            var unityObjectFieldRect = rect.Padding(2).AlignLeft(rect.width / 2);
            var eventSelectorRect = rect.Padding(2).AlignRight(rect.width / 2 - 5);
            var dInfo = this.GetDelegateInfo();

            EditorGUI.BeginChangeCheck();
            var newTarget = SirenixEditorFields.UnityObjectField(unityObjectFieldRect, dInfo.Target, typeof(UnityEngine.Object), true);
            if (EditorGUI.EndChangeCheck())
            {
                this.tmpTarget = newTarget;
            }

            EditorGUI.BeginChangeCheck();
            var selectorText = (dInfo.Method == null || this.tmpTarget) ? "Select an event" : dInfo.MethodName;
            var newMethod = MethodSelector.DrawSelectorDropdown(eventSelectorRect, selectorText, this.CreateSelector);
            if (EditorGUI.EndChangeCheck())
            {
                this.CreateAndAssignNewDelegate(newMethod.FirstOrDefault());
                this.tmpTarget = null;
            }

            //     }
            //     SirenixEditorGUI.EndToolbarBoxHeader();
            // 
            // Draws the rest of the ICustomEvent, and since we've drawn the label, we simply pass along null.
            this.CallNextDrawer(null);
            // }
            // SirenixEditorGUI.EndBox();
        }

        private void CreateAndAssignNewDelegate(DelegateInfo delInfo)
        {
            var method = delInfo.Method;
            var target = delInfo.Target;
            var pTypes = method.GetParameters().Select(x => x.ParameterType).ToArray();
            var args = new object[pTypes.Length];

            Type delegateType = null;

            if (method.ReturnType == typeof(void))
            {
                if (args.Length == 0) delegateType = typeof(Action);
                else if (args.Length == 1) delegateType = typeof(Action<>).MakeGenericType(pTypes);
                else if (args.Length == 2) delegateType = typeof(Action<,>).MakeGenericType(pTypes);
                else if (args.Length == 3) delegateType = typeof(Action<,,>).MakeGenericType(pTypes);
                else if (args.Length == 4) delegateType = typeof(Action<,,,>).MakeGenericType(pTypes);
            }

            if (delegateType == null)
            {
                Debug.LogError("Unsupported Event Type");
                return;
            }

            var del = Delegate.CreateDelegate(delegateType, target, method);
            this.Property.Tree.DelayActionUntilRepaint(() =>
            {
                this.ValueEntry.WeakSmartValue = new EventPicker(del, delInfo.EventInfo);
                GUI.changed = true;
                this.Property.RefreshSetup();
            });
        }

        private DelegateInfo GetDelegateInfo()
        {
            var value = this.ValueEntry.SmartValue;
            var del = value.EventAdder;
            var methodInfo = del == null ? null : del.Method;

            UnityEngine.Object target = null;
            if (this.tmpTarget)
            {
                target = this.tmpTarget;
            }
            else if (del != null)
            {
                target = del.Target as UnityEngine.Object;
            }

            return new DelegateInfo(target, methodInfo);
        }

        private OdinSelector<DelegateInfo> CreateSelector(Rect arg)
        {
            arg.xMin -= arg.width;
            var info = this.GetDelegateInfo();
            var sel = new EventSelector(info.Target);
            sel.SetSelection(info);
            sel.ShowInPopup(arg);
            return sel;
        }
    }
}
