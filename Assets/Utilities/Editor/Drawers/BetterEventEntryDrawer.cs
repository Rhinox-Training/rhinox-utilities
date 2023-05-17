using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Rhinox.GUIUtils.Editor;
using Rhinox.Lightspeed;
using Rhinox.Lightspeed.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Rhinox.Utilities.Editor
{
    [CustomPropertyDrawer(typeof(BetterEventEntry))]
    public class BetterEventEntryDrawer : BasePropertyDrawer<BetterEventEntry, BetterEventEntryDrawer.DrawerData>
    {
        public class DrawerData
        {
            public GenericHostInfo Info;
            public Object LocalTarget;
            public GUIContent ActiveContent;
        }
        
        private class MethodInfoWrapper
        {
            public MethodInfo Info;
            public object Target;
            public string FullName;
            public string TargetName;
        }
        
        private PickerHandler _methodPicker;

        private GUIContent _noneContent;

        protected override void OnInitialize()
        {
            base.OnInitialize();

            _noneContent = new GUIContent("No Function");
        }

        protected override DrawerData CreateData(GenericHostInfo info)
        {
            var value = info.GetSmartValue<BetterEventEntry>();
            var del = value.Delegate;
            var data = new DrawerData
            {
                Info = info,
                ActiveContent = new GUIContent()
            };

            if (del != null)
            {
                data.ActiveContent.text = GetMethodInfoRepresentation(del.Method);
                data.LocalTarget = del.Target as Object;
            }

            return data;
        }

        protected override GenericHostInfo GetHostInfo(DrawerData data) => data.Info;
        
        protected override void DrawProperty(Rect position, ref DrawerData data, GUIContent label)
        {
            var objectPickerRect = position.AlignLeft(position.width / 2 - 1f);
            var newTarget = EditorGUI.ObjectField(objectPickerRect, data.LocalTarget, typeof(Object), true);
            if (!Equals(data.LocalTarget, newTarget))
            {
                data.LocalTarget = newTarget;
                _methodPicker = null;
            }
            
            var methodPickerRect = position.AlignRight(position.width / 2 - 1f);

            var content = SmartValue?.Delegate == null ? _noneContent : data.ActiveContent;
            
            if (EditorGUI.DropdownButton(methodPickerRect, content, FocusType.Keyboard))
                DoMethodDropdown(position, data);
        }

        private void DoMethodDropdown(Rect position, DrawerData data)
        {
            if (_methodPicker != null)
                GenericPicker.Show(position, _methodPicker);
            else
            {
                _methodPicker = GenericPicker.Show(
                    position, 
                    null, 
                    GetMethodOptions(data), 
                    x => SetValue(x, data),
                    x => x.FullName,
                    x => x.TargetName);
            }
        }

        private void SetValue(MethodInfoWrapper wrapper, DrawerData data)
        {
            if (wrapper != null)
            {
                data.LocalTarget = wrapper.Target as Object;
                data.ActiveContent.text = wrapper.FullName;
                SmartValue.CreateAndAssignNewDelegate(wrapper.Target, wrapper.Info);
            }
            else
                SmartValue.Delegate = null;
        }

        private ICollection<MethodInfoWrapper> GetMethodOptions(DrawerData data)
        {
            if (data.LocalTarget == null)
                return Array.Empty<MethodInfoWrapper>();

            var list = new List<MethodInfoWrapper>();

            GameObject targetObject = null;

            if (data.LocalTarget is Component comp)
                targetObject = comp.gameObject;

            if (data.LocalTarget is GameObject go)
                targetObject = go;
            
            if (targetObject != null)
            {
                list.AddRange(GetMethodOptions(targetObject));
                foreach (var component in targetObject.GetComponents<Component>())
                    list.AddRange(GetMethodOptions(component));
            }
            else
                list.AddRange(GetMethodOptions(data.LocalTarget));
            
            return list;
        }
        
        private IEnumerable<MethodInfoWrapper> GetMethodOptions(object target)
        {
            var type = target.GetType();
            
            if (type == null)
                yield break;
            
            foreach (var mi in type.GetMethods(SmartValue.AllowedFlags))
            {
                if (mi.ContainsGenericParameters) continue;

                if (mi.CustomAttributes.Any(x => x.AttributeType == typeof(CompilerGeneratedAttribute)))
                    continue;

                if (mi.IsSpecialName) // This makes it ignore properties, events, etc
                {
                    if (!mi.Name.StartsWith("set_")) // except for set_ methods
                        continue;
                }
                
                // TODO: ignore unity functions? (Start, Update, etc..)
                
                yield return new MethodInfoWrapper
                {
                    Info = mi,
                    Target = target,
                    FullName = GetMethodInfoRepresentation(mi),
                    TargetName = GetTargetFromFunc(mi)
                };
            }
        }

        private static string GetTargetFromFunc(MethodInfo info)
        {
            return info.DeclaringType.GetNiceName(false);
        }

        private static string GetMethodInfoRepresentation(MethodInfo info)
        {
            var types = info.GetParameters();
            string name = info.Name;

            if (info.IsSpecialName)
            {
                var i = name.IndexOf("_", StringComparison.Ordinal);
                name = name.Substring(i+1);
            }
            
            if (info.IsSpecialName)
                return $"{GetTypeName(types[0])} {name}";
            
            if (types.Any())
            {
                var typesStr = string.Join(", ", types.Select(GetTypeName));
                return $"{GetTypeName(info.ReturnType)} {name} ({typesStr})";
            }

            return $"{GetTypeName(info.ReturnType)} {name} ()";
        }
        
        private static string GetTypeName(ParameterInfo info)
        {
            return GetTypeName(info.ParameterType);
        }

        private static string GetTypeName(Type type)
        {
            return type.GetNiceName(false);
        }
    }
}