#if ODIN_INSPECTOR
using System;
using Sirenix.Utilities.Editor.Expressions;
using UnityEngine;

namespace Rhinox.Utilities.Editor
{
    [Serializable]
    public class StringExpressionAction : BaseToolbarButton
    {
        public string Name;
        public string Expression;

        public Type ContextType;
		
        protected override string Label => Name;

        protected override void Execute(Rect rect)
        {
            var action = ExpressionUtility.ParseAction(Expression, ContextType, out string errorMessage, true);
            if (string.IsNullOrEmpty(errorMessage))
                action?.Invoke();
            else
                Debug.LogError(errorMessage);
        }
    }
}
#endif