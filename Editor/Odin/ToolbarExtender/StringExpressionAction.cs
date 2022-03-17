using System;
using Sirenix.Utilities.Editor.Expressions;
using UnityEngine;

namespace Rhinox.Utilities.Editor
{
    public class StringExpressionAction : BaseToolbarButton
    {
        public string Name;
        public string Expression;

        public Type ContextType;
		
        protected override string Label => Name;

        protected override void Execute()
        {
            var action = ExpressionUtility.ParseAction(Expression, ContextType, out string errorMessage, true);
            if (string.IsNullOrEmpty(errorMessage))
                action?.Invoke();
            else
                Debug.LogError(errorMessage);
        }
    }
}