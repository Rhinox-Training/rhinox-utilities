using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Rhinox.Utilities
{
    public class RuntimeInitializer
    {
        private class InitializeAction
        {
            private MethodInfo _methodInfo;
            public int Order { get; }

            public InitializeAction(MethodInfo m, int order = 0)
            {
                _methodInfo = m;
                Order = order;
            }

            public void Invoke()
            {
                _methodInfo.Invoke(null, null);
            }
        }
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Initialize()
        {
            List<InitializeAction> actions = new List<InitializeAction>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.GetCustomAttribute<InitializationHandlerAttribute>() == null)
                        continue;

                    var methods = type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Static |
                                                  BindingFlags.Public | BindingFlags.NonPublic);
                    foreach (var method in methods)
                    {
                        if (method.GetParameters().Length != 0)
                            continue;
                        
                        var attr = method.GetCustomAttribute<OrderedRuntimeInitializeAttribute>();
                        if (attr == null)
                            continue;
                        
                        actions.Add(new InitializeAction(method, attr.Order));
                    }
                }
            }

            foreach (var action in actions.OrderBy(x => x.Order))
                action.Invoke();

        }
    }
}