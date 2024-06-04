using System.Collections.Generic;
using System.Linq;
using Rhinox.Lightspeed;
#if ODIN_INSPECTOR
using Sirenix.Utilities.Editor;
#endif
using UnityEditor;
using UnityEngine;

namespace Rhinox.Utilities.Editor
{
    internal static class ContextMenuEnhancements
    {
        [MenuItem("CONTEXT/BoxCollider/Set Collider Bounds")]
        private static void SetColliderBounds(MenuCommand command)
        {
            BoxCollider coll = command.context as BoxCollider;

            // We want a list of colliders without the current collider
            // otherwise if it envelops all others, it will always just return itself
            var list = new List<Collider>();
            coll.GetComponentsInChildren(list);
            list.Remove(coll);
            var colliders = list.ToArray();
            var bounds = coll.gameObject.GetObjectBounds(null, colliders);
            
            Undo.RegisterCompleteObjectUndo(coll, "Set Collider Bounds");
            coll.center = coll.transform.InverseTransformPoint(bounds.center);
            coll.size = coll.transform.InverseTransformVector(bounds.size).Abs();
        }
        
#if !UNITY_2020_1_OR_NEWER && ODIN_INSPECTOR
        [MenuItem("CONTEXT/MonoBehaviour/Properties")]
        private static void OpenInInspector(MenuCommand command)
        {
            GUIHelper.OpenInspectorWindow(command.context);
        }
#endif
    }
}