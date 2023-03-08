using System;
using System.Collections.Generic;
using System.Linq;
using Rhinox.GUIUtils.Editor;
using Rhinox.Lightspeed;
using Rhinox.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Rhinox.Utilities.Odin.Editor
{
    public class MissingScriptsWindow : CustomEditorWindow
    {
        private static int _goCount;
        private static int _componentsCount;
        private static int _missingCount;

        private static bool _bHaveRun;

        [MenuItem(WindowHelper.ToolsPrefix + "Clean Up Missing Components", false, 1500)]
        public static void ShowWindow()
        {
            var win = GetWindow(typeof(MissingScriptsWindow));
            win.titleContent = new GUIContent("Missing Comp cleaner");
        }

        protected override void OnGUI()
        {
            base.OnGUI();
            
            if (_bHaveRun)
            {

                GUILayout.TextField(_goCount + " GameObjects Selected");
                if (_goCount > 0) GUILayout.TextField(_componentsCount + " Components");
                if (_goCount > 0) GUILayout.TextField(_missingCount + " Deleted");
            }
            
            if (GUILayout.Button("Remove from Selection"))
                FindAndRemoveInSelection();
            
            GUILayout.FlexibleSpace();
        }
        
        private static void FindAndRemoveInSelection()
        {
            var go = Selection.gameObjects;
            _goCount = 0;
            _componentsCount = 0;
            _missingCount = 0;
            foreach (var g in go)
            {
                Undo.RegisterCompleteObjectUndo(g, "CleanMissingComponents");
#if UNITY_2019_1_OR_NEWER
                FindAndRemoveMissingInSelected();
#else
                FindInGo(g);
#endif
            }

            _bHaveRun = true;
            Debug.Log($"Searched {_goCount} GameObjects, {_componentsCount} components, found {_missingCount} missing");

            AssetDatabase.SaveAssets();
        }

#if !UNITY_2019_1_OR_NEWER
        private static void FindInGo(GameObject g)
        {
            ++_goCount;
            var components = g.GetComponents<Component>();
            
            for (var i = components.Length-1; i >= 0; --i)
            {
                ++_componentsCount;
                if (components[i] != null) continue;
                
                ++_missingCount;
                var s = g.name;
                var t = g.transform;
                while (t.parent != null)
                {
                    s = t.parent.name + "/" + s;
                    t = t.parent;
                }

                Debug.Log($"{s} has a missing script '' at {i}", g);

                var serializedObject = new SerializedObject(g);

                var prop = serializedObject.FindProperty("m_Component");

                prop.DeleteArrayElementAtIndex(i);

                serializedObject.ApplyModifiedProperties();
                
                EditorUtility.SetDirty(g);
            }

            foreach (Transform childT in g.transform)
            {
                FindInGo(childT.gameObject);
            }
        }
#endif
        
        private static int CountMissingInGo(GameObject go)
        {
#if UNITY_2019_1_OR_NEWER
            return GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
#else
            var components = go.GetComponents<Component>();

            int missing = 0;
            for (var i = components.Length - 1; i >= 0; --i)
            {
                if (components[i] != null) continue;

                ++missing;
            }

            return missing;
#endif
        }

        private static void RemoveMissingInGo(GameObject go)
        {
#if UNITY_2019_1_OR_NEWER
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
#else
            var components = go.GetComponents<Component>();

            for (var i = components.Length - 1; i >= 0; --i)
            {
                if (components[i] != null) continue;

                var serializedObject = new SerializedObject(go);

                var prop = serializedObject.FindProperty("m_Component");

                prop.DeleteArrayElementAtIndex(i);

                serializedObject.ApplyModifiedProperties();
                
                EditorUtility.SetDirty(go);
            }
#endif
        }
        
        private static void FindAndRemoveMissingInSelected()
        {
            // EditorUtility.CollectDeepHierarchy does not include inactive children
            var deepSelection = Selection.gameObjects.SelectMany(go => go.GetAllChildren(true));
            
            var prefabs = new HashSet<Object>();
            foreach (var go in deepSelection)
            {
                _componentsCount += go.GetComponents<Component>().Length;
                int count = CountMissingInGo(go);
                if (count > 0)
                {
                    if (PrefabUtility.IsPartOfAnyPrefab(go))
                    {
                        RecursivePrefabSource(go, prefabs, ref _missingCount, ref _goCount);
                        count = CountMissingInGo(go);
                        // if count == 0 the missing scripts has been removed from prefabs
                        if (count == 0)
                            continue;
                        // if not the missing scripts must be prefab overrides on this instance
                    }
 
                    Undo.RegisterCompleteObjectUndo(go, "Remove missing scripts");
                    RemoveMissingInGo(go);
                    _missingCount += count;
                    ++_goCount;
                }
            }
        }
        
        // Prefabs can both be nested or variants, so best way to clean all is to go through them all
        // rather than jumping straight to the original prefab source.
        private static void RecursivePrefabSource(GameObject instance, HashSet<Object> prefabs, ref int compCount, ref int goCount)
        {
            var source = PrefabUtility.GetCorrespondingObjectFromSource(instance);
            // Only visit if source is valid, and hasn't been visited before
            if (source == null || !prefabs.Add(source))
                return;
 
            // go deep before removing, to differantiate local overrides from missing in source
            RecursivePrefabSource(source, prefabs, ref compCount, ref goCount);
 
            int count = CountMissingInGo(source);
            if (count > 0)
            {
                Undo.RegisterCompleteObjectUndo(source, "Remove missing scripts");
                RemoveMissingInGo(source);
                compCount += count;
                goCount++;
            }
        }
    }
}