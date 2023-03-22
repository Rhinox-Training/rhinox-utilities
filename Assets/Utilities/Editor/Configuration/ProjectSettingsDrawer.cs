using System;
using Rhinox.GUIUtils.Editor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector.Editor;
#endif
using UnityEngine.UIElements;

namespace Rhinox.Utilities.Editor.Configuration
{
    public interface IProjectSettingsDrawer
    {
        void LoadTarget(CustomProjectSettings instance);
        void OnActivate(string searchContext, VisualElement rootElement);
        void OnCustomGUI(string searchContext);
    }
    
    public class ProjectSettingsDrawer : IProjectSettingsDrawer
    {
        public CustomProjectSettings _targetObject;
#if ODIN_INSPECTOR
        private PropertyTree _propertyTree;
#else
        private DrawablePropertyView _propertyView;
#endif
        
        private UnityEditor.Editor _editor;
        private SerializedObject _serializedObject;

        public void LoadTarget(CustomProjectSettings instance)
        {
            _targetObject = instance;
        }

        public virtual void OnActivate(string searchContext, VisualElement rootElement)
        {
            if (_targetObject == null)
                return;
            if (_serializedObject == null)
                _serializedObject = new SerializedObject(_targetObject);
        }
        
        public virtual void OnCustomGUI(string searchContext)
        {
            if (_targetObject == null)
                return;
#if ODIN_INSPECTOR
            if (_propertyTree == null)
                _propertyTree = PropertyTree.Create(_serializedObject);
#else
            if (_propertyView == null)
                _propertyView = new DrawablePropertyView(_serializedObject);
#endif
       
            using (new eUtility.PaddedGUIScope())
            {
                EditorGUI.BeginChangeCheck();

#if ODIN_INSPECTOR
                _propertyTree.Draw();
#else
                _propertyView.DrawLayout();
#endif
                
                OnDrawFooter();

                if (EditorGUI.EndChangeCheck())
                    _targetObject.OnChanged();
            }
        }

        protected virtual void OnDrawFooter()
        {
            
        }
    }
}