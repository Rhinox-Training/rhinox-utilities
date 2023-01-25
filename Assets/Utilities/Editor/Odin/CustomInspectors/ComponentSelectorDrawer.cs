using Rhinox.GUIUtils;
using Rhinox.GUIUtils.Editor;
using Rhinox.Lightspeed;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Rhinox.Utilities.Odin.Editor
{
    [CustomEditor(typeof(ComponentSelector))]
    public class ComponentSelectorDrawer : 
#if ODIN_INSPECTOR
        OdinEditor
#else
        UnityEditor.Editor
#endif
    {
        private ComponentSelector Target;
        private SerializedProperty _baseProperty;
        private DrawableList _listDrawing;
        
        private void OnEnable()
        {
            Target = target as ComponentSelector;
        }

        public override void OnInspectorGUI()
        {
            // If we're editing, just call the regular editor
            if (Target.IsEditing)
            {
            #if ODIN_INSPECTOR
                base.OnInspectorGUI();
            #else
                var prop = serializedObject.FindProperty(nameof(Target.internalComponents));
                if (_baseProperty == null || prop != _baseProperty)
                {
                    _baseProperty = serializedObject.FindProperty(nameof(Target.internalComponents));
                    _listDrawing = new DrawableList(_baseProperty);
                }
                if (_listDrawing != null)
                    _listDrawing.Draw();
                if (Target.IsEditing && GUILayout.Button("Close Editor"))
                    Target.IsEditing = false;
            #endif

                return;
            }
            
            if (Target.internalComponents.IsNullOrEmpty())
                return;

            // Otherwise provide a compact overview with buttons
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            for (var i = 0; i < Target.internalComponents.Count; i++)
            {
                var set = Target.internalComponents[i];
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(new GUIContent(set.Name, set.Tooltip), EditorStyles.miniButton))
                {
                    Selection.objects = set.Objects;
                }

                HandleIsActive(set);

                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }

        private void HandleIsActive(ComponentSet set)
        {
            EditorGUI.BeginChangeCheck();
            
            bool? currentLimboState = set.GetIsActive();
            var color = currentLimboState == true ? Color.green : Color.black;
            
            string text = "-";
            if (currentLimboState.HasValue)
                text = currentLimboState == true ? "ON" : "OFF";
            
            GUIContentHelper.PushColor((GUI.color + color) / 2);
            bool toggle = currentLimboState ?? false;
            toggle = GUILayout.Toggle(toggle, text, EditorStyles.miniButton, GUILayout.Width(33));
            GUIContentHelper.PopColor();

            if (EditorGUI.EndChangeCheck())
            {
                foreach (var item in set.Objects)
                {
                    Undo.RecordObject(item, "Edit set.IsActive");
                    item.SetActive(toggle);
                }
            }
        }
    }
}