using System;
using System.Collections.Generic;
using Rhinox.Lightspeed;
using Rhinox.Lightspeed.Reflection;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Rhinox.Utilities
{
    [RefactoringOldNamespace("")]
    [Serializable]
    public abstract class LayoutElementCondition
    {
        public abstract bool Check(ConditionalHiddenElement element);
    }

    [RefactoringOldNamespace("")]
    public class LayoutSizeRestrictionCondition : LayoutElementCondition
    {
        [MinValue(0)] public float Height;
        [MinValue(0)] public float Width;

        public override bool Check(ConditionalHiddenElement e)
        {
            if (e.Rect.height < Height) return true;
            return e.Rect.height < Width;
        }
    }

    [RefactoringOldNamespace("")]
    [ExecuteAlways]
    [RequireComponent(typeof(LayoutElement))]
    public class ConditionalHiddenElement : MonoBehaviour
    {
        public enum ConditionalState
        {
            Any,
            All
        }

        private LayoutElement _layoutElement;
        private RectTransform _rectTransform;

        public Rect Rect => _rectTransform.rect;

        public ConditionalState State;

        [SerializeReference, TypeFilter(nameof(GetValidTypes))]
        public LayoutElementCondition[] _conditions;

        private void Awake()
        {
            _layoutElement = GetComponent<LayoutElement>();
            _rectTransform = GetComponent<RectTransform>();
        }

        private void Update()
        {
            _layoutElement.enabled = true;

            bool newState = true;

            if (!_conditions.IsNullOrEmpty())
            {
                foreach (var c in _conditions)
                {
                    bool result = c.Check(this);
                    if (result && State == ConditionalState.Any)
                    {
                        newState = false;
                        break;
                    }

                    if (!result && State == ConditionalState.All)
                        break;
                }
            }

            _layoutElement.enabled = newState;

        }

        private List<Type> GetValidTypes()
        {
            return ReflectionUtility.GetTypesInheritingFrom(typeof(LayoutElementCondition));
        }
    }
}