using Rhinox.GUIUtils.Editor;
using UnityEditor;
using UnityEngine;

namespace Rhinox.Utilities.Editor
{
    [CustomEditor(typeof(Rigidbody))]
    public class RigidBodyEditor : DefaultEditorExtender<Rigidbody>
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (!Application.isPlaying)
                return;

            if (FlexibleButton("Reset Forces"))
                Target.ResetInertiaTensor();
        }

        private bool FlexibleButton(string text)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            bool result = GUILayout.Button(text);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            return result;
        }
    }
}