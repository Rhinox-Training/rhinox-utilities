using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Rhinox.GUIUtils;
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
        public class MethodInfoWrapper
        {
            public MethodInfo Info;
            public object Target;
            public string FullName;
            public string TargetName;
        }
        
        public class DrawerData
        {
            public GenericHostInfo Info;
            public Object LocalTarget;
            public GUIContent ActiveContent;
            public IOrderedDrawable ParameterDrawable;
            public SimplePicker<MethodInfoWrapper> Picker;
        }
        
        
        private GUIContent _noneContent;

        protected override void OnInitialize()
        {
            base.OnInitialize();

            _noneContent = new GUIContent("No Function");
        }

        protected override DrawerData CreateData(GenericHostInfo info)
        {
            var value = info.GetSmartValue<BetterEventEntry>();
            if (value == null)
            {
                value = new BetterEventEntry(null);
                info.TrySetValue(value);
            }
            
            var del = value.Delegate;
            var data = new DrawerData
            {
                Info = info,
                ActiveContent = new GUIContent()
            };

            if (del != null)
            {
                data.ActiveContent.text = GetMethodInfoRepresentation(del.Method);
                data.LocalTarget = value.Target;
                data.ParameterDrawable = SetupDrawableForParameters(del.Method, data);
            }

            return data;
        }

        protected override GenericHostInfo GetHostInfo(DrawerData data) => data.Info;
        
        protected override void DrawProperty(Rect position, ref DrawerData data, GUIContent label)
        {
            var headerRect = position.AlignTop(EditorGUIUtility.singleLineHeight);
            
            var objectPickerRect = headerRect.AlignLeft(headerRect.width / 2 - 1f);
            var newTarget = EditorGUI.ObjectField(objectPickerRect, data.LocalTarget, typeof(Object), true);
            if (!Equals(data.LocalTarget, newTarget))
            {
                data.LocalTarget = newTarget;
                data.Picker = null;
            }
            
            var methodPickerRect = headerRect.AlignRight(headerRect.width / 2 - 1f);

            var content = SmartValue?.Delegate == null ? _noneContent : data.ActiveContent;
            
            if (EditorGUI.DropdownButton(methodPickerRect, content, FocusType.Keyboard))
                DoMethodDropdown(headerRect, data);


            if (data.ParameterDrawable != null)
            {
                var parametersRect = position.AlignBottom(position.height - EditorGUIUtility.singleLineHeight - CustomGUIUtility.Padding);
                try
                {
                    data.ParameterDrawable.Draw(parametersRect, GUIContent.none);
                }
                catch (Exception e)
                {
                    EditorGUI.HelpBox(parametersRect, e.Message, MessageType.Error);
                }
            }
        }

        protected override float GetPropertyHeight(GUIContent label, in DrawerData data)
        {
            var height = base.GetPropertyHeight(label, in data);
            if (data.ParameterDrawable != null)
                height += data.ParameterDrawable.ElementHeight;
            return height;
        }

        private void DoMethodDropdown(Rect position, DrawerData data)
        {
            if (data.Picker == null)
            {
                data.Picker = new SimplePicker<MethodInfoWrapper>(
                    GetMethodOptions(data),
                    x => x.FullName,
                    x => x.TargetName);
                data.Picker.OptionSelected += x => SetValue(x, data);
            }
            data.Picker.Show(position);
        }

        private static void SetValue(MethodInfoWrapper wrapper, DrawerData data)
        {
            var value = data.Info.GetSmartValue<BetterEventEntry>();
            if (wrapper != null)
            {
                data.LocalTarget = wrapper.Target as Object;
                data.ActiveContent.text = wrapper.FullName;
                value.CreateAndAssignNewDelegate(wrapper.Target, wrapper.Info);
                data.ParameterDrawable = SetupDrawableForParameters(wrapper.Info, data);
            }
            else
            {
                value.Delegate = null;
                data.ParameterDrawable = null;
            }
        }

        private static IOrderedDrawable SetupDrawableForParameters(MethodInfo info, DrawerData data)
        {
            var value = data.Info.GetSmartValue<BetterEventEntry>();
            
            var wantedParameters = info.GetParameters();
            var parameters = value.ParameterValues;
            Array.Resize(ref parameters, wantedParameters.Length);
            
            for (int i = 0; i < wantedParameters.Length; ++i)
            {
                var type = wantedParameters[i].ParameterType;
                if (parameters[i] == null || !parameters[i].GetType().InheritsFrom(type))
                    parameters[i] = type.GetDefault();
            }

            value.ParameterValues = parameters;
            return DrawableFactory.CreateDrawableForParametersOf(info.GetParameters(), parameters);
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