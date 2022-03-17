using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Rhinox.Lightspeed;
using Rhinox.Utilities;
using Rhinox.Utilities.Editor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using Sirenix.Utilities.Editor.Expressions;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using TypeSearchData = ComponentsContainer.TypeSearchData;
using CompareMethod = ComponentsContainer.TypeSearchData.CompareMethod;

namespace Rhinox.Utilities.Odin.Editor
{
    [Serializable]
    public class ComponentFilter : BaseAdvancedSearchSearchFilter
    {
        [ShowInInspector, PropertyOrder(50), ShowIf("@_components.Any()")]
        private ComponentsContainer _components = new ComponentsContainer();

        private AddComponentWindowProxy _addComponentWindowProxy;
        private GameObject dummyObj;

        private const string FakeObjectName = "_advancedSceneSearchTempObjComponentSearch";

        public ComponentFilter() : base("Components")
        {
            _components.Changed += TriggerChanged;
        }

        public void InitFakeObj()
        {
            if (dummyObj != null) return;

            dummyObj = new GameObject(FakeObjectName);

            foreach (var typeSearchData in _components.Data)
            {
                if (typeSearchData.Type == typeof(Transform)) continue; // just gives a info log otherwise, so just skip
                dummyObj.AddComponent(typeSearchData.Type);
            }

            if (_components.Data.Count == 0)
                _components.Add(typeof(Transform));

            dummyObj.hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy;
            _components.SetDummyObject(dummyObj);
        }

        public void Terminate()
        {
            if (dummyObj != null)
                Object.DestroyImmediate(dummyObj);
        }

        public override void Reset()
        {
            _components.Clear();
            base.Reset();
        }

        [Button(ButtonSizes.Medium)]
        public void AddComponent()
        {
            if (_addComponentWindowProxy == null) _addComponentWindowProxy = new AddComponentWindowProxy();
            var width = GUIHelper.CurrentWindow.position.width;
            _addComponentWindowProxy.Show(AddComponents, new Rect(5, -15, width - 10, 76));
        }

        public override void HandleDragged(Object draggedObject)
        {
            AddComponent(draggedObject);

            base.HandleDragged(draggedObject);

            TriggerChanged();
        }

        private void AddComponent(Object component)
        {
            switch (component)
            {
                case Component _:
                    AddComponent(component.GetType());
                    break;
                case GameObject gameObject:
                {
                    foreach (Component comp in gameObject.GetComponents<Component>())
                        AddComponent(comp.GetType());
                    break;
                }
                case MonoScript script:
                    AddComponent(script.GetClass());
                    break;
            }
        }

        private void AddComponent(Type type)
        {
            // If it already exists, don't add it
            if (_components.HasType(type))
                return;

            _components.Add(type);

            InitFakeObj();
            if (type == typeof(Transform)) return; // just gives a info log otherwise, so just skip

            dummyObj.AddComponent(type);
        }

        private void AddComponents(Component[] obj)
        {
            foreach (Component component in obj)
                AddComponent(component);

            TriggerChanged();
        }

        public override ICollection<GameObject> ApplyFilter(ICollection<GameObject> selectedObjs)
        {
            if (!_components.Any()) return selectedObjs;

            foreach (var searchData in _components.Data)
            {
                if (searchData == null || searchData.Type == null) continue;

                selectedObjs.RemoveAll(x => !MatchComponent(x, searchData));
            }

            return selectedObjs;
        }

        private bool MatchComponent(GameObject go, TypeSearchData searchData)
        {
            int countMatching = 0;
            foreach (var comp in go.GetComponents(searchData.Type))
            {
                // if not the correct type => just continue
                if (searchData.Type != comp.GetType())
                    continue;

                // if the expression does not match => just continue
                if (!TestCodeExpression(comp, searchData))
                    continue;

                // if it's a monobehaviour, we can use the PropertyTree of Odin
                if (comp is MonoBehaviour)
                {
                    if (OdinComponentMatch(comp, searchData))
                        ++countMatching;
                }
                // otherwise, it is better not too due to implementations being in c++
                else if (ComponentMatch(comp, searchData))
                    ++countMatching;

                if (countMatching == 0) continue;

                // if the matches are higher than the amount needed, we're done
                // Note: Amount needed == -1 => any amount more than 0 is true
                if (searchData.Amount < 0 || countMatching >= searchData.Amount)
                    return true;

                if (searchData.Amount == 0)
                    return false;
            }

            // if we get here, both values need to be 0 to be correct
            return countMatching == searchData.Amount;
        }

        private bool TestCodeExpression(Component comp, TypeSearchData searchData)
        {
            if (!searchData.ShowCustomExpression)
                return true;

            if (searchData.ExpressionHasError)
                return false;

            if (searchData.CustomExpressionDelegate == null)
                return true;

            // TODO why pass a bool as param, how to get only return type
            bool result = (bool)searchData.CustomExpressionDelegate.DynamicInvoke(comp, true);

            return result;
        }

        private bool ComponentMatch(Component component, TypeSearchData searchData)
        {
            if (searchData.SerializedVars == null || searchData.SerializedVars.Count <= 0)
                return true;

            var serializedObject = new SerializedObject(component);

            foreach (var serializedVarData in searchData.SerializedVars)
            {
                var property = serializedObject.FindProperty(serializedVarData.Name);
                var dummyComponent = dummyObj.GetComponent(component.GetType());
                var serializedDummyObject = new SerializedObject(dummyComponent);
                var dummyProperty = serializedDummyObject.FindProperty(serializedVarData.Name);

                if (!AreSerializedPropertiesEqualValue(property, dummyProperty, serializedVarData.Comparer))
                    return false;
            }

            return true;
        }

        private bool OdinComponentMatch(Component component, TypeSearchData searchData)
        {
            if (searchData.SerializedVars == null || searchData.SerializedVars.Count <= 0) return true;

            var tree = PropertyTree.Create(component);

            foreach (var serializedVarData in searchData.SerializedVars)
            {
                var property = tree.GetPropertyAtPath(serializedVarData.Name);
                var dummyComponent = dummyObj.GetComponent(component.GetType());
                var dummyTree = PropertyTree.Create(dummyComponent);
                var dummyProperty = dummyTree.GetPropertyAtPath(serializedVarData.Name);

                if (!AreInspectorPropertiesEqualValue(property.ValueEntry, dummyProperty.ValueEntry,
                    serializedVarData.Comparer))
                    return false;
            }

            return true;
        }

        private bool AreInspectorPropertiesEqualValue(IPropertyValueEntry prop, IPropertyValueEntry dummyProp,
            CompareMethod compareMethod)
        {
            // if not the same amount of -> false (Basically always false & count == 1?)
            if (prop.WeakValues.Count != dummyProp.WeakValues.Count)
                return false;

            for (int i = 0; i < prop.WeakValues.Count; ++i)
            {
                var dummyValues = dummyProp.WeakValues;
                var values = prop.WeakValues;

                // try to get it as an enumerable (Lists, dictionaries, etc.)
                var dummyValuesList = (dummyValues[i] as IEnumerable)?.Cast<object>();
                if (dummyValuesList != null)
                {
                    // foreach value on the dummy (aka the search data), search if the scene prop contains it
                    var valuesList = (values[i] as IEnumerable)?.Cast<object>();
                    if (compareMethod.HasFlag(CompareMethod.Equals)
                        && dummyValuesList.Count() != valuesList.Count())
                        return false;

                    foreach (var value in dummyValuesList)
                    {
                        if (!valuesList.Contains(value))
                            return false;
                    }
                }
                else
                {
                    if (compareMethod.HasFlag(CompareMethod.Equals) && values[i].Equals(dummyValues[i]))
                        continue;

                    // if above did not continue & compare method is only Equals -> false
                    if (compareMethod == CompareMethod.Equals) return false;

                    // assuming data is correct and value inherits from IComparable
                    if (!CheckCompare(compareMethod, values[i], dummyValues[i]))
                        return false;
                }
            }

            return true;
        }

        private static bool CheckCompare(CompareMethod compareMethod, object a, object b)
        {
            var result = Comparer.DefaultInvariant.Compare(a, b);

            if (compareMethod.HasFlag(CompareMethod.Equals) && result == 0) return true;
            if (compareMethod.HasFlag(CompareMethod.LargerThan) && result > 0) return true;
            if (compareMethod.HasFlag(CompareMethod.LesserThan) && result < 0) return true;
            return false;
        }

        private static bool AreSerializedPropertiesEqualValue(SerializedProperty property,
            SerializedProperty dummyProperty, CompareMethod compareMethod)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Generic:

                    if (!property.isArray)
                        throw new NotImplementedException(
                            "Generic comparison for SerializedProperties has not been implemented. TODO.");

                    if (property.arraySize != dummyProperty.arraySize)
                        return false;

                    for (int i = 0; i < property.arraySize; ++i)
                    {
                        if (!AreSerializedPropertiesEqualValue(property.GetArrayElementAtIndex(i),
                            dummyProperty.GetArrayElementAtIndex(i), compareMethod))
                            return false;
                    }

                    return true;

                case SerializedPropertyType.Integer:
                    return CheckCompare(compareMethod, property.intValue, dummyProperty.intValue);
                case SerializedPropertyType.Boolean:
                    return CheckCompare(compareMethod, property.boolValue, dummyProperty.boolValue);
                case SerializedPropertyType.Float:
                    if (compareMethod.HasFlag(CompareMethod.Equals) &&
                        Math.Abs(property.floatValue - dummyProperty.floatValue) <= float.Epsilon)
                        return true;
                    return CheckCompare(compareMethod, property.floatValue, dummyProperty.floatValue);
                case SerializedPropertyType.String:
                    return CheckCompare(compareMethod, property.stringValue, dummyProperty.stringValue);
                case SerializedPropertyType.Color:
                    return property.colorValue == dummyProperty.colorValue;
                case SerializedPropertyType.ObjectReference:
                    return property.objectReferenceValue == dummyProperty.objectReferenceValue;
                case SerializedPropertyType.LayerMask:
                    return property.intValue == dummyProperty.intValue;
                case SerializedPropertyType.Enum:
                    return property.enumValueIndex == dummyProperty.enumValueIndex;
                case SerializedPropertyType.Vector2:
                    if (compareMethod.HasFlag(CompareMethod.Equals) &&
                        property.vector2Value == dummyProperty.vector2Value)
                        return true;
                    if (CheckCompare(compareMethod, property.vector2Value.x, dummyProperty.vector2Value.x)
                        && CheckCompare(compareMethod, property.vector2Value.y, dummyProperty.vector2Value.y))
                        return true;
                    return false;
                case SerializedPropertyType.Vector3:
                    if (compareMethod.HasFlag(CompareMethod.Equals) &&
                        property.vector3Value == dummyProperty.vector3Value)
                        return true;
                    else if (CheckCompare(compareMethod, property.vector3Value.x, dummyProperty.vector3Value.x)
                             && CheckCompare(compareMethod, property.vector3Value.y, dummyProperty.vector3Value.y)
                             && CheckCompare(compareMethod, property.vector3Value.z, dummyProperty.vector3Value.z))
                        return true;
                    return false;
                case SerializedPropertyType.Vector4:
                    if (compareMethod.HasFlag(CompareMethod.Equals) &&
                        property.vector4Value == dummyProperty.vector4Value)
                        return true;
                    else if (CheckCompare(compareMethod, property.vector4Value.x, dummyProperty.vector4Value.x)
                             && CheckCompare(compareMethod, property.vector4Value.y, dummyProperty.vector4Value.y)
                             && CheckCompare(compareMethod, property.vector4Value.z, dummyProperty.vector4Value.z)
                             && CheckCompare(compareMethod, property.vector4Value.w, dummyProperty.vector4Value.w))
                        return true;
                    return false;
                case SerializedPropertyType.Rect:
                    return property.rectValue == dummyProperty.rectValue;
                case SerializedPropertyType.ArraySize:
                    return property.arraySize == dummyProperty.arraySize;
                case SerializedPropertyType.Character:
                    return property.intValue == dummyProperty.intValue;
                case SerializedPropertyType.AnimationCurve:
                    return property.animationCurveValue.Equals(dummyProperty.animationCurveValue);
                case SerializedPropertyType.Bounds:
                    return property.boundsValue == dummyProperty.boundsValue;
                case SerializedPropertyType.Gradient:
                    throw new NotImplementedException(
                        "Gradient comparison for SerializedProperties has not been implemented. TODO.");
                case SerializedPropertyType.Quaternion:
                    return property.quaternionValue == dummyProperty.quaternionValue;
                case SerializedPropertyType.ExposedReference:
                    return property.exposedReferenceValue == dummyProperty.exposedReferenceValue;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override string GetFilterInfo()
        {
            string searchInfo = string.Empty;
            foreach (var typeSearch in _components.Data)
            {
                if (typeSearch.Type == null)
                    continue;

                if (!string.IsNullOrWhiteSpace(searchInfo))
                    searchInfo += Environment.NewLine;

                if (typeSearch.Amount == 0)
                    searchInfo += "No " + typeSearch.Type.Name + "s";
                else if (typeSearch.Amount < 0)
                    searchInfo += "1 or more " + typeSearch.Type.Name + "s";
                else if (typeSearch.Amount == 1)
                    searchInfo += "Exactly " + typeSearch.Amount + " " + typeSearch.Type.Name;
                else
                    searchInfo += "Exactly " + typeSearch.Amount + " " + typeSearch.Type.Name + "s";
            }

            return searchInfo;
        }
    }
}