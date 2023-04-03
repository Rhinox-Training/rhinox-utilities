using UnityEngine;
using UnityEditor;
using System.IO;
using Rhinox.GUIUtils;
using Rhinox.GUIUtils.Editor;
using Rhinox.Lightspeed;
using Rhinox.Lightspeed.IO;
#if UNITY_2021_1_OR_NEWER
using UnityEditor.SceneManagement;
#else
using UnityEditor.Experimental.SceneManagement;
#endif

#if UNITY_2019_2_OR_NEWER && PROBUILDER
using UnityEngine.ProBuilder;
#elif PROBUILDER
// Downgrade to probuilder 3.x if you get this error; No way to check :(
using ProBuilder.Core;
#endif

namespace Rhinox.Utilities.Editor
{
    [CustomEditor(typeof(MeshFilter))]
    [CanEditMultipleObjects]
    public class MeshFilterEditor : DefaultEditorExtender<MeshFilter>
    {
        private bool _showPivotChanger = false;
        private bool _onlyThisMesh = true;

        private Vector3 _pivot; //Pivot value -1..1, calculated from Mesh bounds
        private Vector3 _previousPivot; //Last used pivot
        private Mesh _mesh; //Mesh of the selected object
        private Collider _col; //Collider of the selected object

        private Mesh _originalMesh;
        private Vector3 _originalPivot;
        private Vector3 _originalPosition;

        private bool _pivotUnchanged; //Flag to decide when to instantiate a copy of the mesh
        
        private bool _preservePosition = true;

        
        public override void OnInspectorGUI()
        {
            if (!EditorUtilitiesSettings.Instance.OverrideMeshFilterInspector)
            {
                base.OnInspectorGUI();
                return;
            }
            
            using (new eUtility.HorizontalGroup())
            {
                base.OnInspectorGUI();
                
                serializedObject.Update();
                
                DrawMeshSaveOptions();
                
                serializedObject.ApplyModifiedProperties();
            }
            
            if (Target.sharedMesh == null) return;

            if (!_showPivotChanger)
            {
                DrawEditOptions();
            }
            else
            {
                DrawPivotEditor();

                // Show mesh bounds after the button
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Bounds:", CustomGUIStyles.MiniLabel);
                GUILayout.FlexibleSpace();
                GUILayout.Label(_mesh.bounds.ToString(), CustomGUIStyles.MiniLabelRight);
                EditorGUILayout.EndHorizontal();
            }

            DrawSaveMeshUnderPrefab();
        }

        private void DrawPivotEditor()
        {
            EditorGUILayout.BeginHorizontal();

            // Create a button to set the pivot to the center of the mesh
            if (GUILayout.Button("Center", CustomGUIStyles.MiniButton))
            {
                if (_pivotUnchanged)
                {
                    if (_onlyThisMesh)
                    {
                        // deep copy mesh so you don't alter original
                        _mesh = Instantiate(Target.sharedMesh); //make a deep copy
                        Target.mesh = _mesh;
                    }
                    else _mesh = Target.sharedMesh;
                }

                _pivotUnchanged = false;
                _pivot = Vector3.zero;
                UpdatePivot();
                _previousPivot = _pivot;
            }

            GUILayout.FlexibleSpace();

            _preservePosition = EditorGUILayout.ToggleLeft("Preserve Position", _preservePosition);

            if (GUILayout.Button("Undo", CustomGUIStyles.MiniButtonLeft))
            {
                _previousPivot = _pivot;
                _pivot = _originalPivot;
                UpdatePivot();

                _mesh = _originalMesh;
                Target.mesh = _mesh;

                Target.transform.position = _originalPosition;
                UpdatePivotVector();
                _pivotUnchanged = true;
            }

            if (GUILayout.Button("Apply", CustomGUIStyles.MiniButtonRight))
            {
                _mesh = null;
                _pivotUnchanged = false;
                _showPivotChanger = false;
            }

            EditorGUILayout.EndHorizontal();

            _pivot.x = EditorGUILayout.Slider("X", _pivot.x, -1.0f, 1.0f);
            _pivot.y = EditorGUILayout.Slider("Y", _pivot.y, -1.0f, 1.0f);
            _pivot.z = EditorGUILayout.Slider("Z", _pivot.z, -1.0f, 1.0f);

            // Detects user input on any of the three sliders
            if (_pivot != _previousPivot)
            {
                if (_pivotUnchanged)
                {
                    if (_onlyThisMesh)
                    {
                        // deep copy mesh so you don't alter original
                        _mesh = Instantiate(Target.sharedMesh); //make a deep copy
                        Target.mesh = _mesh;
                    }
                    else _mesh = Target.sharedMesh;
                }

                _pivotUnchanged = false;
                UpdatePivot();
                _previousPivot = _pivot;
            }
        }

        private void DrawEditOptions()
        {
            using (new eUtility.HorizontalGroup(disabled: true))
                EditorGUILayout.Vector3Field("Bounds", Target.sharedMesh.bounds.extents);

            using (new eUtility.HorizontalGroup())
            {
                using (new eUtility.VerticalGroup(GUILayout.ExpandWidth(true)))
                {
                    GUILayout.Label("Edit Pivot", CustomGUIStyles.BoldLabel, GUILayout.ExpandWidth(true));

                    GUIContentHelper.PushIndentLevel(EditorGUI.indentLevel + 1);
                    _preservePosition = EditorGUILayout.ToggleLeft("Preserve Position", _preservePosition, GUILayout.ExpandWidth(false));
                    GUIContentHelper.PopIndentLevel();
                }

                using (new eUtility.VerticalGroup())
                {
                    if (GUILayout.Button("Only This Mesh"))
                    {
                        Undo.RecordObject(Target, "Change Pivot of " + Target.gameObject.name);
                        _onlyThisMesh = true;
                        PrepareForPivotChanges();
                    }
                    else if (GUILayout.Button("On Root Mesh"))
                    {
                        Undo.RecordObject(Target, "Change Pivot of " + Target.gameObject.name);
                        Undo.RecordObject(Target.sharedMesh, "Change Pivot of " + Target.gameObject.name);
                        _onlyThisMesh = false;
                        PrepareForPivotChanges();
                    }
                    else _mesh = null;
                }
            }
        }

        private void DrawMeshSaveOptions()
        {
            if (Target.sharedMesh == null) return;
            
#if PROBUILDER
#if UNITY_2019_2_OR_NEWER
            var hasProbuilder = Target.GetComponent<ProBuilderMesh>() != null;
#else
            var hasProbuilder = _targetObject.GetComponent<pb_Object>() != null;
#endif

            if (hasProbuilder)
            {
                if (GUILayout.Button("Remove ProBuilder", GUILayout.ExpandWidth(false)))
                {
                    EditorApplication.ExecuteMenuItem("Tools/ProBuilder/Actions/Strip ProBuilder Scripts in Selection");
                }

                return;
            }
#endif
            
            var path = AssetDatabase.GetAssetPath(Target.sharedMesh);

            if (!string.IsNullOrWhiteSpace(path)) return;
            
            if (GUILayout.Button("Save Mesh", GUILayout.ExpandWidth(false)))
            {
                string rootPath = AskForPath(out path);
                SaveMeshAtPath(path, rootPath);
            }
        }
        
        private void DrawSaveMeshUnderPrefab()
        {
            if (Target.sharedMesh == null) return;
            
            var path = AssetDatabase.GetAssetPath(Target.sharedMesh);

            if (!string.IsNullOrWhiteSpace(path)) return;

            string activePrefabPath = null;
            PrefabStage stage = null;

            if (PrefabUtility.IsPartOfAnyPrefab(Target.gameObject))
            {
                activePrefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(Target);
            }
            else
            {
                stage = PrefabStageUtility.GetCurrentPrefabStage();
                if (stage != null)
#if UNITY_2020_1_OR_NEWER
                    activePrefabPath = stage.assetPath;
#else
                    activePrefabPath = stage.prefabAssetPath;
#endif
            }

            if (activePrefabPath.IsNullOrEmpty()) return;
            
            if (GUILayout.Button("Save Mesh under prefab"))
            {
                if (Target.sharedMesh.name.IsNullOrEmpty())
                    Target.sharedMesh.name = Target.name;
                
                AssetDatabase.AddObjectToAsset(Target.sharedMesh, activePrefabPath);
                if (stage != null)
                    PrefabUtility.SavePrefabAsset(stage.prefabContentsRoot);
                AssetDatabase.SaveAssets();
            }
        }

        private void SaveMeshAtPath(string path, string rootPath)
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                rootPath = Path.GetFullPath(Path.Combine(rootPath, "..", ".."));
                path = FileHelper.GetRelativePath(path, rootPath);
                try
                {
                    AssetDatabase.CreateAsset(Target.sharedMesh, path);
                    AssetDatabase.SaveAssets();
                }
                // if the above failed it is an addressable TODO try not using try catch...
                // I cannot find a way to fetch whether it is so we have to try catch...
                catch
                {
                    // Copy the mesh (getter of mesh makes a copy and assigns it)
                    // We don't want to do this by default because it throws a warning / memory leak
                    AssetDatabase.CreateAsset(Target.mesh, path);
                    AssetDatabase.SaveAssets();
                }
            }
        }

        private string AskForPath(out string path)
        {
            var rootPath = Path.Combine(Application.dataPath, "_Models");
            var name = Target.gameObject.name;
            name = name.Replace(" ", "_");
            path = EditorUtility.SaveFilePanel("Where to save?", rootPath, name, "asset");
            return rootPath;
        }

        void PrepareForPivotChanges()
        {
            _showPivotChanger = true;

            // keep some stuff to restore if needed
            _originalMesh = Target.sharedMesh;
            _originalPosition = Target.transform.position;

            // update some things
            _mesh = Target.sharedMesh;
            _col = Target.GetComponent<Collider>();
            UpdatePivotVector();
            _pivotUnchanged = true;

            _originalPivot = _pivot;
        }

        //Achieve the movement of the pivot by moving the transform position in the specified direction
        //and then moving all vertices of the mesh in the opposite direction back to where they were in world-space
        void UpdatePivot()
        {
            //Calculate difference in 3d position
            Vector3 diff = Vector3.Scale(_mesh.bounds.extents, _previousPivot - _pivot);
            //Move object position by taking localScale into account
            if (_preservePosition)
                Target.transform.position = Target.transform.TransformPoint(-diff);
            //Iterate over all vertices and move them in the opposite direction of the object position movement
            Vector3[] verts = _mesh.vertices;
            for (int i = 0; i < verts.Length; i++)
            {
                verts[i] += diff;
            }

            //Assign the vertex array back to the mesh
            _mesh.vertices = verts;
            //Recalculate bounds of the mesh, for the renderer's sake
            //The 'center' parameter of certain colliders needs to be adjusted
            //when the transform position is modified
            _mesh.RecalculateBounds();

            ShiftCollider(diff);
        }

        private void ShiftCollider(Vector3 movement)
        {
            if (!_col) return;
            
            switch (_col)
            {
                case BoxCollider boxCollider:
                    boxCollider.center += movement;
                    break;
                case CapsuleCollider capsuleCollider:
                    capsuleCollider.center += movement;
                    break;
                case SphereCollider sphereCollider:
                    sphereCollider.center += movement;
                    break;
                default:
                    Debug.LogWarning("Unimplemented Collider for shifting; Collider might not be accurate correct anymore.");
                    break;
            }
        }

        //Look at the object's transform position in comparison to the center of its mesh bounds
        //and calculate the pivot values for xyz
        void UpdatePivotVector()
        {
            Bounds b = _mesh.bounds;
            Vector3 offset = -1 * b.center;
            _pivot = _previousPivot =
                new Vector3(offset.x / b.extents.x, offset.y / b.extents.y, offset.z / b.extents.z);
        }
    }
    
#if UNITY_2019_2_OR_NEWER && PROBUILDER
    [CustomEditor(typeof(ProBuilderMesh))]
    [CanEditMultipleObjects]
    public class ProbuilderMeshEditor : DefaultEditorExtender<ProBuilderMesh>
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            if (GUILayout.Button("Remove ProBuilder"))
            {
                EditorApplication.ExecuteMenuItem("Tools/ProBuilder/Actions/Strip ProBuilder Scripts in Selection");
            }
        }
    }
#endif

    public static class MeshUtilityEditor
    {
        [MenuItem("CONTEXT/MeshFilter/Move Mesh To Child")]
        private static void MoveToChild (MenuCommand menuCommand)
        {
            MeshFilter mf = menuCommand.context as MeshFilter;

            var t = mf.transform;
            var child = t.Find("Mesh");
            if (!child)
            {
                child = t.Create("Mesh");
                child.gameObject.CopyObjectSettingsFrom(t.gameObject);
            }
            
            // Copy meshfilter (after meshrenderer due to requirecomponent)
            var newFilter = Undo.AddComponent<MeshFilter>(child.gameObject);

            newFilter.mesh = mf.sharedMesh;

            Undo.DestroyObjectImmediate(mf);
            
            // Copy MeshRenderer as well
            var renderer = t.GetComponent<MeshRenderer>();

            CopyRenderer(renderer, child);
            
            Undo.DestroyObjectImmediate(renderer);
        }

        [MenuItem("CONTEXT/SkinnedMeshRenderer/Bake Mesh To Child")]
        private static void BakeToChild(MenuCommand menuCommand)
        {
            SkinnedMeshRenderer renderer = menuCommand.context as SkinnedMeshRenderer;
            
            var t = renderer.transform;
            // Copy meshfilter (after meshrenderer due to requirecomponent)
            Undo.RegisterCompleteObjectUndo(renderer, "Bake to Child");
            
            var root = renderer.rootBone;
            while (root.parent != null && !t.IsChildOf(root))
                root = root.parent;
            if (!t.IsChildOf(root))
            {
                Debug.LogError("Cannot process SkinnedMeshRenderer");
                return;
            }
            
            var child = t.Find("Mesh");
            if (!child)
            {
                child = t.Create("Mesh");
                child.gameObject.CopyObjectSettingsFrom(t.gameObject);
            }
            var newFilter = Undo.AddComponent<MeshFilter>(child.gameObject);

            newFilter.sharedMesh = new Mesh();

            var parent = root.parent;
            root.SetParent(null, false);
            
            renderer.BakeMesh(newFilter.sharedMesh);
            
            root.SetParent(parent, false);

            renderer.enabled = false;

            CopyRenderer(renderer, child);
        }

        private static void CopyRenderer<T>(T source, Transform child)
            where T : Renderer
        {
            if (source != null)
            {
                var newRenderer = Undo.AddComponent<MeshRenderer>(child.gameObject);

                if (source is MeshRenderer meshRenderer)
                    CopyMeshRendererParams(meshRenderer, newRenderer);

                newRenderer.materials = source.sharedMaterials;

                newRenderer.receiveShadows = source.receiveShadows;
                newRenderer.lightmapIndex = source.lightmapIndex;
                newRenderer.probeAnchor = source.probeAnchor;
                newRenderer.rendererPriority = source.rendererPriority;
                newRenderer.shadowCastingMode = source.shadowCastingMode;
            }
        }

        private static void CopyMeshRendererParams(MeshRenderer source, MeshRenderer target)
        {
            target.additionalVertexStreams = source.additionalVertexStreams;
#if UNITY_2019_2_OR_NEWER
            target.receiveGI = source.receiveGI;
#endif
#if UNITY_2019_4_OR_NEWER // Introduced in _3 but only in the latter versions
            target.scaleInLightmap = source.scaleInLightmap;
            target.stitchLightmapSeams = source.stitchLightmapSeams;
#endif
        }
    }
}
