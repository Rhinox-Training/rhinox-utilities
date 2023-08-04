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
        bool LoadTarget(CustomProjectSettings instance);
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

        public bool LoadTarget(CustomProjectSettings instance)
        {
            if (_targetObject == instance)
                return false;
            _targetObject = instance;
            return true;
        }

        public virtual void OnActivate(string searchContext, VisualElement rootElement)
        {
            CheckIfPropertyViewsNeedRefresh();
        }

        private void CheckIfPropertyViewsNeedRefresh()
        {
            if (_targetObject == null)
                return;
            
            if (_serializedObject == null)
                _serializedObject = new SerializedObject(_targetObject);
            
            if (_serializedObject.targetObject != _targetObject)
            {
#if ODIN_INSPECTOR
                _propertyTree = PropertyTree.Create(_serializedObject);
#else
                _propertyView = new DrawablePropertyView(_serializedObject);
#endif
            }
        }

        public virtual void OnCustomGUI(string searchContext)
        {
            CheckIfPropertyViewsNeedRefresh();
            
            if (_serializedObject == null)
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
                {
#if ODIN_INSPECTOR
                    // NOP
                    // TODO: is this sufficient?
#else
                    _propertyView.RequestRepaint();
#endif
                    _targetObject.OnChanged();
                }
            }
        }

        protected virtual void OnDrawFooter()
        {
            
        }
    }
}