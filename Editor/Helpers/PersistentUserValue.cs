using System;
using Sirenix.OdinInspector.Editor;

namespace Rhinox.Utilities.Editor
{
    public class PersistentUserValue<T>
    {
        private readonly T _defaultVal;
#if ODIN_INSPECTOR
        private GlobalPersistentContext<T> _context;
        public T Value
        {
            get => _context.Value;
            set => _context.Value = value;
        }
#else
        
        public T Value { get; set; }
        #endif
        
        public static PersistentUserValue<T> Get(Type alphaKey, string betaKey, T defaultVal)
        {
            var instance = new PersistentUserValue<T>(defaultVal);
            
#if ODIN_INSPECTOR
            instance._context = PersistentContext.Get(alphaKey, betaKey, defaultVal);
#else
            instance.Value = defaultVal;
#endif
            return instance;
        }

        private PersistentUserValue(T defaultVal)
        {
            _defaultVal = defaultVal;
            
        }
    }
}