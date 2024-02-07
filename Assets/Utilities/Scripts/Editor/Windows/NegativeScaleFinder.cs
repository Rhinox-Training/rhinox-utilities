using System.Collections.Generic;
using Rhinox.GUIUtils;
using Rhinox.GUIUtils.Editor;
using Rhinox.Lightspeed;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Rhinox.Utilities.Editor
{
    public class NegativeScaleFinder : CustomEditorWindow
    {
        // public class ResultFindData
        // {
        //     public GameObject GO;
        //     public Component Comp;
        //     public GenericHostInfo HostInfo;
        //     public Object TargetResult;
        //
        //     public ResultFindData(GameObject go, Component comp, GenericHostInfo hostInfo, Object targetResult)
        //     {
        //         GO = go;
        //         Comp = comp;
        //         HostInfo = hostInfo;
        //         TargetResult = targetResult;
        //     }
        // }

        //private GameObject _targetObject = null;
        private List<GameObject> _resultDataList = new List<GameObject>();
        private Vector2 _scrollPos = Vector2.zero;
        private bool _includeInactive = false;
        //private Rect _rect = Rect.zero;
        //private SimpleTableView _simpleTable;

        [MenuItem(WindowHelper.FindToolsPrefix + "Find Negative Scaled In Scene")]
        public static void ShowWindow()
        {
            GetWindow<NegativeScaleFinder>("Find Negative Scale");
        }

        protected override void DrawEditor(int index)
        {
            GUILayout.Space(5f);
            // EditorGUILayout.HelpBox(
            //     "This search operation looks through EVERY field of almost all components in this scene,\nso this searching tool is very -= E X P E N S I V E =-",
            //     MessageType.Warning);
            // GUILayout.Space(5f);

            // GUILayout.BeginHorizontal();
            // GUILayout.Label("GameObject:", EditorStyles.boldLabel);
            // _targetObject = (GameObject)EditorGUILayout.ObjectField(_targetObject, typeof(GameObject), true);
            // GUILayout.EndHorizontal();
            // if (GUILayout.Button("Use selected GameObject"))
            //     _targetObject = Selection.activeObject as GameObject;
            //
            // GUILayout.Space(5f);

            var activeScene = SceneManager.GetActiveScene();
            //var sceneRef = new SceneReference(activeScene);

            // EditorGUI.BeginDisabledGroup(true);
            // GUILayout.BeginHorizontal();
            // GUILayout.Label("Current Scene:", EditorStyles.boldLabel);
            // EditorGUILayout.ObjectField(sceneRef.SceneAsset, typeof(SceneAsset), false);
            // GUILayout.EndHorizontal();
            // EditorGUI.EndDisabledGroup();
            // GUILayout.Space(5f);
            // GameObject excludeGo = null;
            // if (_targetObject == null)
            // {
            //     GUILayout.Label("GameObject was NULL");
            // }
            // else if (PrefabUtility.IsPartOfPrefabAsset(_targetObject))
            // {
            //     GUILayout.Label("This is a PREFAB");
            // }
            // else
            // {
            //     GUILayout.Label("This is a normal GAME OBJECT");
            //     excludeGo = _targetObject;
            // }

            _includeInactive = GUILayout.Toggle(_includeInactive, " Include Inactive Objects");

            GUILayout.Space(10f);
            // EditorGUI.BeginDisabledGroup(_targetObject == null);
            if (GUILayout.Button("Find Objects"))
            {
                _resultDataList.Clear();

                var rootObjects = activeScene.GetRootGameObjects();
                foreach (var rootObj in rootObjects)
                {
                    var lst = rootObj.GetAllChildren(_includeInactive);
                    foreach (var obj in lst)
                    {
                        if (obj.transform.localScale.x < 0f ||
                            obj.transform.localScale.y < 0f ||
                            obj.transform.localScale.z < 0f)
                            _resultDataList.Add(obj);
                    }
                }
            }

            if (_resultDataList.Count > 0)
            {
                GUIStyle objectFieldSkin = GUI.skin.GetStyle("ObjectField");
                GUIStyle labelSkin = GUI.skin.GetStyle("label");
                labelSkin.fixedHeight = 15f;
                labelSkin.alignment = TextAnchor.MiddleCenter;
                labelSkin.fontStyle = FontStyle.Bold;

                GUILayout.Space(20f);
                GUILayout.Label("RESULTS", labelSkin);
                GUILayout.Space(10f);


                _scrollPos = GUILayout.BeginScrollView(_scrollPos, CustomGUIStyles.Clean);
                foreach (GameObject result in _resultDataList)
                {
                    if (GUILayout.Button($"{result.GetFullName()}", objectFieldSkin)) //objectFieldSkin
                    {
                        //user, when clicked goes to the GameObject
                        Selection.activeObject = result;
                        EditorGUIUtility.PingObject(result);
                    }
                }


                // if (_simpleTable == null)
                //     _simpleTable = new SimpleTableView("User", "Path", "Usee", "Type");
                //
                // _simpleTable.BeginDraw();
                // foreach (var result in _resultDataList)
                // {
                //     _simpleTable.DrawRow((Action<GUILayoutOption[]>)((GUILayoutOption[] options) =>
                //         {
                //             if (GUILayout.Button($"{result.GO.GetFullName()}", objectFieldSkin, options))//objectFieldSkin
                //             {
                //                 //user, when clicked goes to the GameObject
                //                 Selection.activeObject = result.Comp;
                //                 EditorGUIUtility.PingObject(result.GO);
                //             }
                //         }),
                //         (Action<GUILayoutOption[]>)((GUILayoutOption[] options) => { GUILayout.Label($">{result.HostInfo.GetNicePath()}", CustomGUIStyles.Label, options); }),
                //         (Action<GUILayoutOption[]>)((GUILayoutOption[] options) =>
                //         {
                //             if (GUILayout.Button($"{result.TargetResult.name}", objectFieldSkin, options))//objectFieldSkin
                //             {
                //                 Selection.activeObject = result.TargetResult;
                //                 EditorGUIUtility.PingObject(result.TargetResult);
                //             }
                //         }),
                //          result.TargetResult.GetType().GetNiceName()
                //     );
                // }
                // _simpleTable.EndDraw();
                GUILayout.EndScrollView();

                // GUILayout.FlexibleSpace();
            }
        }

        // private void FindReferences(GameObject targetObject, GameObject excludeGO, Scene activeScene)
        // {
        //     var rootObjects = activeScene.GetRootGameObjects();
        //     var possibleTargets = new List<UnityEngine.Object>();
        //
        //     //Makes list of all the GameObjects/components of the target object
        //     //These are the ones that could be referenced to. 
        //     foreach (var targetChildObj in BreadthFirstGo(targetObject, null))
        //     {
        //         var targetComponents = targetChildObj.GetComponents<Component>();
        //
        //         possibleTargets.Add(targetChildObj);
        //         possibleTargets.AddRange(targetComponents);
        //     }
        //
        //     //high level go over all the root objects in the active scene
        //     foreach (var rootObj in rootObjects)
        //     {
        //         //go over each child object of this root Object,
        //         //excluding the target object
        //         foreach (var childGo in BreadthFirstGo(rootObj, excludeGO))
        //         {
        //             var comps = childGo.GetComponents<Component>();
        //
        //             //loop over all the components of this Game Object
        //             //excluding
        //             foreach (var comp in comps)
        //             {
        //                 //TODO: maybe make this configurable?
        //                 if (comp is Transform)
        //                     continue;
        //
        //                 var memberInfos = SerializeHelper.GetSerializedMembers(comp.GetType());
        //
        //                 //loop over all member infos of the component
        //                 //and check if THAT member references any of the targets' GameObjets/Components 
        //                 foreach (var memberInfo in memberInfos)
        //                 {
        //                     foreach (var hostInfo in BreadthFirstMemInfo(memberInfo, comp))
        //                     {
        //                         //if (possibleTargets.Any(x => ReferenceEquals(x, hostInfo.GetValue())))
        //                         //    _resultDataList.Add(new ResultFindData(comp.gameObject, comp, hostInfo));
        //                         var targetResult = possibleTargets.Find(x => ReferenceEquals(x, hostInfo.GetValue()));
        //
        //                         if (targetResult != null)
        //                             _resultDataList.Add(new ResultFindData(comp.gameObject, comp, hostInfo, targetResult));
        //                     }
        //                 }
        //             }
        //         }
        //     }
        // }

        /// <summary>
        /// Returns a Game Object that can be evaluated each time the function gets called   
        /// </summary>
        /// <param name="parentObj">GameObject that will be iterated on</param>
        /// <param name="exclude">GameObject to excluded from the iteration</param>
        /// <returns>Child GameObject that is a NOT the excluded GameObject or one of it's children</returns>
        // private IEnumerable<GameObject> BreadthFirstGo(GameObject parentObj, GameObject exclude)
        // {
        //     var goQueue = new Queue<GameObject>();
        //     goQueue.Enqueue(parentObj);
        //
        //     while (goQueue.Count != 0)
        //     {
        //         var currentObj = goQueue.Dequeue();
        //         if (currentObj == exclude)
        //             continue;
        //
        //         yield return currentObj;
        //
        //         foreach (Transform child in currentObj.transform)
        //         {
        //             goQueue.Enqueue(child.gameObject);
        //         }
        //     }
        // }
    }
}