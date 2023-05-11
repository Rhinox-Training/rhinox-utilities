using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Rhinox.GUIUtils.Editor;
using Rhinox.Lightspeed;
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
            public string FullName;
            
            public MethodInfoWrapper(MethodInfo info)
            {
                Info = info;
                FullName = GetMethodInfoRepresentation(info);
            }

            public override string ToString() => FullName;
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
            return new DrawerData()
            {
                Info = info,
                ActiveContent = new GUIContent(GetMethodInfoRepresentation(value.Delegate.Method)),
                LocalTarget = value.Delegate.Target as Object
            };
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
                _methodPicker = GenericPicker.Show(position, null, GetMethodOptions(data), x => SetValue(x, data));
        }

        private void SetValue(MethodInfoWrapper wrapper, DrawerData data)
        {
            if (wrapper != null)
            {
                data.ActiveContent.text = wrapper.FullName;
                SmartValue.CreateAndAssignNewDelegate(data, wrapper.Info);
            }
            else
                SmartValue.Delegate = null;
        }

        private ICollection<MethodInfoWrapper> GetMethodOptions(DrawerData data)
        {
            var targets = new [] {data.LocalTarget};

            return targets.SelectMany(x => GetMethodOptions(x)).ToArray();
        }


        private IEnumerable<MethodInfoWrapper> GetMethodOptions<T>(T target)
            => GetMethodOptions(target?.GetType());
        
        private IEnumerable<MethodInfoWrapper> GetMethodOptions(Type type)
        {
            if (type == null)
                yield break;
            
            foreach (var mi in type.GetMethods(SmartValue.AllowedFlags))
            {
                if (mi.CustomAttributes.Any(x => x.AttributeType == typeof(CompilerGeneratedAttribute)))
                    continue;
                
                if (mi.IsSpecialName) // This makes it ignore properties
                {
                    if (!mi.Name.StartsWith("set_")) // except for set_ methods
                        continue;
                }
                
                yield return new MethodInfoWrapper(mi);
            }
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
                return $"{types[0].Name} {name}";
            
            if (types.Any())
            {
                var typesStr = string.Join(", ", types.Select(x => x.ParameterType.Name));
                return $"{info.ReturnType.Name} {name} ({typesStr})";
            }

            return $"{info.ReturnType.Name} {name} ()";
        }
    }
}