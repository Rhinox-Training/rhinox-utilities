using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Rhinox.GUIUtils;
using Rhinox.GUIUtils.Editor;
using Rhinox.Lightspeed;
using Rhinox.Lightspeed.Reflection;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using Component = UnityEngine.Component;

namespace Rhinox.Utilities.Odin.Editor
{
    [HideReferenceObjectPicker, HideLabel]
    public class ComponentsContainer
    {
        [HideReferenceObjectPicker, HideLabel]
        public class TypeSearchData
        {
            public enum CompareMethod
            {
                [Description("==")] Equals = 1,
                Contains = 2, // Only available if implementing IEnumerable
                [Description(">")] LargerThan = 4, // Only available if implementing IComparable
                [Description("<")] LesserThan = 8, // Only available if implementing IComparable

                [Description(">=")] LargerThanOrEqual =
                    Equals | LargerThan, // Only available if implementing IComparable

                [Description("<=")] LesserThanOrEqual =
                    Equals | LesserThan, // Only available if implementing IComparable
            }

            private static readonly CompareMethod[] DefaultOptions = new[]
            {
                CompareMethod.Equals
            };

            private static readonly CompareMethod[] ListOptions = new[]
            {
                CompareMethod.Equals, CompareMethod.Contains
            };

            private static readonly CompareMethod[] ComparableOptions = new[]
            {
                CompareMethod.Equals, CompareMethod.LargerThan, CompareMethod.LargerThanOrEqual,
                CompareMethod.LesserThan, CompareMethod.LesserThanOrEqual
            };

            public class SerializedVariableData
            {
                public string Name;
                public CompareMethod Comparer = CompareMethod.Equals;
                public readonly CompareMethod[] Options;

                public SerializedVariableData(SerializedProperty prop)
                {
                    Name = prop.name;
                    var info = prop.GetHostInfo();
                    var type = info.GetReturnType();
                    if (type == null)
                    {
                        Options = DefaultOptions;
                        return;
                    }

                    if (type.InheritsFrom(typeof(Enumerable)))
                        Options = ListOptions;
                    else if (type.InheritsFrom(typeof(IComparable)) ||
                             type == typeof(Vector2) ||
                             type == typeof(Vector3) ||
                             type == typeof(Vector4))
                        Options = ComparableOptions;
                    else
                        Options = DefaultOptions;
                }
            }

            [HorizontalGroup, CustomValueDrawer(nameof(DrawType))]
            public readonly SerializableType Type;

            [HorizontalGroup(width: 50), CustomValueDrawer(nameof(DrawAmount)), OnValueChanged(nameof(TriggerChanged))]
            public int Amount;

            [HorizontalGroup(width: 22), CustomValueDrawer(nameof(DrawExpanded)), HideLabel]
            public bool Expanded;

            public List<SerializedVariableData> SerializedVars { get; }

            public bool ShowCustomExpression { get; private set; }
            public Delegate CustomExpressionDelegate { get; set; }

            public bool ExpressionHasError => !string.IsNullOrWhiteSpace(_customExpressionError);

            private string _customExpression;
            private string _customExpressionError;

            public static GameObject _dummyObject;
            
            public event Action Changed;

            public TypeSearchData(Type type, int amount = -1)
            {
                Type = new SerializableType(type);
                Amount = amount;
                SerializedVars = new List<SerializedVariableData>();
            }

            private void TriggerChanged()
            {
                Changed?.Invoke();
            }

            private SerializableType DrawType(SerializableType type, GUIContent label)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    Texture2D tex = AssetPreview.GetMiniTypeThumbnail(type);
                    GUILayout.Label(new GUIContent(type.Name, tex), GUILayout.Height(18));

                    GUILayout.FlexibleSpace();
                }

                return type;
            }

            private int DrawAmount(int value, GUIContent label)
            {
                if (value >= 0)
                {
                    EditorGUIUtility.labelWidth = 60;
                    return EditorGUILayout.IntField(label, value, GUILayout.ExpandWidth(false));
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(label, GUILayout.ExpandWidth(false));

                    if (GUILayout.Button("Any", EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                        value = 1;
                }

                return value;
            }

            private bool DrawExpanded(bool value, GUIContent label)
            {
                return GUILayout.Toggle(value, "?", CustomGUIStyles.MiniButton, GUILayout.ExpandWidth(false));
            }

            [OnInspectorGUI]
            private void DrawSerializedVars()
            {
                // if checking if none of the component, don't bother drawing the props
                if (Amount == 0) return;

                var comp = _dummyObject.GetComponent(Type);
                DrawComponent(comp);
#if ODIN_INSPECTOR
                DrawCustomCodeCheck();
#endif
            }

            private void DrawComponent(Component component)
            {
                SerializedObject obj = new SerializedObject(component);
                SerializedProperty iterator = obj.GetIterator();

                bool enterChildren = true;
                while (iterator.NextVisible(enterChildren))
                {
                    var serializedVar = SerializedVars.FirstOrDefault(x => x.Name == iterator.name);

                    if (Expanded)
                    {
                        using (new eUtility.HorizontalGroup())
                        {
                            if (serializedVar != null)
                            {
                                // draw toggle to provide option to remove it
                                if (!EditorGUILayout.ToggleLeft(iterator.displayName, true, GUILayout.ExpandWidth(true)))
                                {
                                    SerializedVars.Remove(serializedVar);
                                    TriggerChanged();
                                }

                                // draw compare options
                                DrawSerializedVariableCompareOptions(serializedVar);
                            }
                            // if serializedVar is null -> draw option to add it
                            else if (EditorGUILayout.ToggleLeft(iterator.displayName, false))
                            {
                                SerializedVars.Add(new SerializedVariableData(iterator));
                                TriggerChanged();
                            }
                        }
                    }
                    else if (serializedVar != null)
                    {
                        using (new eUtility.HorizontalGroup())
                        {
                            EditorGUILayout.PropertyField(iterator, true);

                            if (GUILayout.Button("X", GUILayout.Height(14), GUILayout.Width(20)))
                            {
                                SerializedVars.Remove(serializedVar);
                                TriggerChanged();
                            }
                        }
                    }

                    enterChildren = false;
                }

                obj.ApplyModifiedProperties();
#if ODIN_INSPECTOR
                DrawCustomCodeCheck();
#endif
            }
#if ODIN_INSPECTOR

            private void DrawCustomCodeCheck()
            {
                if (Expanded)
                {
                    var newState = EditorGUILayout.ToggleLeft("Custom Expression", ShowCustomExpression);
                    if (newState != ShowCustomExpression)
                    {
                        ShowCustomExpression = newState;
                        TriggerChanged();
                    }
                }
                else if (ShowCustomExpression)
                {
                    if (ExpressionHasError)
                        EditorGUILayout.HelpBox(_customExpressionError, MessageType.Error);

                    using (new eUtility.HorizontalGroup())
                    {
                        var newExp = EditorGUILayout.TextField(_customExpression);
                        if (newExp != _customExpression)
                        {
                            _customExpression = newExp;
                            OnExpressionChanged();
                        }

                        if (GUILayout.Button("X", GUILayout.Height(14), GUILayout.Width(20)))
                        {
                            ShowCustomExpression = false;
                            TriggerChanged();
                        }
                    }
                }
            }

            private void OnExpressionChanged()
            {
                CustomExpressionDelegate = ExpressionUtility.ParseExpression(_customExpression, false, Type,
                    new[] { typeof(bool) }, out _customExpressionError, true);

                if (!string.IsNullOrWhiteSpace(_customExpressionError)) return;

                var mi = CustomExpressionDelegate.GetMethodInfo();
                if (mi.ReturnType != typeof(bool))
                {
                    _customExpressionError = "Expression must return a boolean";
                    return;
                }

                TriggerChanged();
            }
#endif

            private static void DrawSerializedVariableCompareOptions(SerializedVariableData serializedVar)
            {
                int optionCount = serializedVar.Options.Length;
                if (optionCount == 1) return;

                for (var i = 0; i < optionCount; i++)
                {
                    var option = serializedVar.Options[i];
                    var style = CustomGUIStyles.GetMiniButtonGroupStyle(i, optionCount,
                        option == serializedVar.Comparer);
                    if (GUILayout.Button(Utility.GetDescription(option), style, GUILayout.ExpandWidth(false)))
                        serializedVar.Comparer = option;
                }
            }
        }

        [ListDrawerSettings(Expanded = true, DraggableItems = false, ShowPaging = false, HideAddButton = true)]
        [LabelText("Components"), OnValueChanged(nameof(TriggerChanged))]
        public readonly List<TypeSearchData> Data = new List<TypeSearchData>();

        public event Action Changed;

        private void TriggerChanged()
        {
            Changed?.Invoke();
        }

        public void SetDummyObject(GameObject go)
        {
            TypeSearchData._dummyObject = go;
        }

        public bool Any() => Data != null && Data.Any();
        public bool HasType(Type type) => Data.Any(x => x.Type == type);

        public void Add(Type type)
        {
            var typeSearchData = new TypeSearchData(type);
            typeSearchData.Changed += TriggerChanged;

            Data.Add(typeSearchData);
            //TriggerChanged(); // This is already triggered 
        }

        public void Clear()
        {
            Data.Clear();
            TriggerChanged();
        }
    }
}