using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Rhinox.Utilities.Editor
{
    /// <summary>
    /// [WIP] - does nothing atm
    /// </summary>
    [InitializeOnLoad]
    public static class EditorEvents
    {
        private static GameObject[] _activeObjects;
        
        public delegate void GameObjectHandler(GameObject o);
        
        public static event GameObjectHandler ObjectDeleted;
        public static event GameObjectHandler ObjectDuplicated;
        
        static EditorEvents()
        {
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyWindowItemGUI;

            _activeObjects = GetActiveObjects();
        }

        private static void OnHierarchyChanged()
        {
            var activeObjects = GetActiveObjects();
            foreach (var o in activeObjects)
            {
                if (!_activeObjects.Contains(o))
                    HandleNewItem(o);
            }
            _activeObjects = activeObjects;
        }

        private static void HandleNewItem(GameObject o)
        {
            // TODO cache duplicating item and pass this onto event
            // TODO: Is above possible? Is it sequential? What about multiple?
            Debug.Log($"NEW: {o}", o);
        }

        private static GameObject[] GetActiveObjects()
        {
            return Array.Empty<GameObject>(); // Disable this feature for now
            // return Object.FindObjectsOfType<GameObject>()
            //     .Where(x => x.activeInHierarchy)
            //     .ToArray();
        }

        static void OnHierarchyWindowItemGUI(int instanceID, Rect selectionRect)
        {
            // ignore items which are not selected
            if (Selection.activeInstanceID != instanceID)
                return;
            
            Event e = Event.current;
            
            // if (e.type == EventType.Layout || e.type == EventType.Repaint) return;
            // Debug.Log($"Event: {e.type}");
            
            if (e.type != EventType.ExecuteCommand) return;
            
            var o = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (o == null) return;

            switch (e.commandName)
            {
                case "SoftDelete":
                    ObjectDeleted?.Invoke(o);
                    break;
                case "Duplicate":
                    ObjectDuplicated?.Invoke(o);
                    break;
            }
        }
    }
}