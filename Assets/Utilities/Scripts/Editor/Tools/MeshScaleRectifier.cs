using System;
using System.Collections.Generic;
using System.Linq;
using Rhinox.GUIUtils.Editor;
using Rhinox.Lightspeed;
using Rhinox.Perceptor;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Rhinox.Utilities.Editor
{
    public class MeshScaleRectifier
    {
        private const string GAMEOBJECT_ITEM_NAME = "GameObject/Mesh/Rectify Negative Scales of Submeshes";
        [MenuItem(GAMEOBJECT_ITEM_NAME, false, 11)]
        private static void RectifyNegativeScaleComplex(MenuCommand menuCommand)
        {
            GameObject searchRoot = menuCommand.context as GameObject;
            if (searchRoot == null)
                return;

            // Check for parent
            EditorInputDialog.Create("Select Parent", "Parent to use for generating new mesh.")
                .BooleanField("Clean up old object tree:", out var cleanupToggle)
                .OnAccept(() =>
                {
                    Transform parent = searchRoot.transform;
                    
                    string generatedName = $"[GENERATED] Unflipped Meshes Root";
                    if (parent.GetComponentsInChildren<MeshFilter>().Length == 0)
                    {
                        PLog.Warn<UtilityLogger>($"Aborted");
                        return;
                    }

                    var child = parent.Find(generatedName);
                    if (child == null)
                    {
                        child = parent != null ? parent.Create(generatedName) : new GameObject(generatedName).transform;
                        child.gameObject.CopyObjectSettingsFrom(parent.GetComponentsInChildren<MeshFilter>().First().gameObject);
                        child.transform.localPosition = Vector3.zero;
                    }
                    else
                    {
                        if (!EditorUtility.DisplayDialog("Warning!",
                            $"Generated object '{generatedName}' already exists at location.\nDo you want to overwrite?",
                            "Confirm", "Cancel"))
                            return;
                        var oldMF = child.GetComponent<MeshFilter>();
                        var oldMR = child.GetComponent<MeshRenderer>();
                        Utility.Destroy(oldMF, true);
                        Utility.Destroy(oldMR, true);
                        var children = child.gameObject.GetDirectChildren().ToArray();
                        foreach (var subchild in children)
                            Utility.Destroy(subchild, true);
                    }

                    var meshFilters = parent.GetComponentsInChildren<MeshFilter>();
                    foreach (var mf in meshFilters)
                    {
                        Transform realChild = child.AddChild(mf.name);
                        
                        // Copy meshfilter (after meshrenderer due to requirecomponent)
                        var newFilter = Undo.AddComponent<MeshFilter>(realChild.gameObject);

                        // TODO: transform mesh
                        newFilter.mesh = UnflipMesh(mf, realChild);

                        // Copy MeshRenderer as well
                        CopyMeshRenderer(mf.transform, realChild);

                        if (cleanupToggle)
                            Cleanup(mf);
                    }
                })
                .Show();
        }

        [MenuItem("CONTEXT/MeshFilter/Rectify Negative Scale")]
        private static void RectifyNegativeScale(MenuCommand menuCommand)
        {
            MeshFilter mf = menuCommand.context as MeshFilter;

            var options = GetPositiveParents(mf.transform);
            // Check for parent
            EditorInputDialog.Create("Select Parent", "Parent to use for generating new mesh.")
#if ODIN_INSPECTOR
                .Dropdown("Transform:", options, out var transformOption, initialValue: options.LastOrDefault().Value)
#else
                .TransformField("Transform:", out var transformOption, initialValue: options.LastOrDefault().Value)
#endif
                .BooleanField("Clean up old object tree:", out var cleanupToggle)
                .OnAccept(() =>
                {
                    Transform parent = transformOption.Value as Transform;

                    Mesh m = mf.sharedMesh;
                    var t = mf.transform;
                    string generatedName = $"[GENERATED]{t.name}#{m.name} (Unflipped)";
                    var child = parent != null
                        ? parent.Find(generatedName)
                        : t.gameObject.scene.GetRootGameObjects().FirstOrDefault(x => x.name.Equals(generatedName))?.transform;
                    if (!child)
                    {
                        child = parent != null ? parent.Create(generatedName) : new GameObject(generatedName).transform;
                        child.gameObject.CopyObjectSettingsFrom(t.gameObject);
                        m.RecalculateBounds();
                        child.transform.position = mf.transform.TransformPoint(m.bounds.center);
                    }
                    else
                    {
                        if (!EditorUtility.DisplayDialog("Warning!",
                            $"Generated object '{generatedName}' already exists at location.\nDo you want to overwrite?",
                            "Confirm", "Cancel"))
                            return;
                        var oldMF = child.GetComponent<MeshFilter>();
                        var oldMR = child.GetComponent<MeshRenderer>();
                        Utility.Destroy(oldMF, true);
                        Utility.Destroy(oldMR, true);
                    }

                    // Copy meshfilter (after meshrenderer due to requirecomponent)
                    var newFilter = Undo.AddComponent<MeshFilter>(child.gameObject);

                    // TODO: transform mesh
                    newFilter.mesh = UnflipMesh(mf, child);

                    // Copy MeshRenderer as well
                    CopyMeshRenderer(t, child);

                    if (cleanupToggle)
                        Cleanup(mf);
                })
                .Show();
        }

        public static MeshFilter RectifyMesh(MeshFilter mf, Transform target, bool copyMeshRenderer = true, bool cleanUpOld = false)
        {
            var newFilter = target.GetOrAddComponent<MeshFilter>();
            if (newFilter == mf)
                throw new InvalidOperationException("Cannot use own transform as target");

            // TODO: transform mesh
            newFilter.mesh = UnflipMesh(mf, target);
            
            if (copyMeshRenderer)
                CopyMeshRenderer(mf.transform, target);

            if (cleanUpOld)
                Cleanup(mf);
            
            return newFilter;
        }
        
        private static Mesh UnflipMesh(MeshFilter mf, Transform target)
        {
            Mesh srcMesh = mf.sharedMesh;
            Mesh m = new Mesh();
            //Iterate over all vertices and move them in the opposite direction of the object position movement
            Vector3[] verts = srcMesh.vertices;
            Transform srcTransform = mf.transform;
            for (int i = 0; i < verts.Length; ++i)
            {
                var oldVert = verts[i];
                var newVert = target.InverseTransformPoint(srcTransform.TransformPoint(oldVert));
                verts[i] = newVert;
            }
            m.vertices = verts;
            // Copy indices
            var srcScale = mf.transform.lossyScale;
            int invertedComponents = 0;
            if (srcScale.x < 0.0f)
                ++invertedComponents;
            if (srcScale.y < 0.0f)
                ++invertedComponents;
            if (srcScale.z < 0.0f)
                ++invertedComponents;
            
            bool shouldFlipTrianglesWinding = invertedComponents % 2 == 1; // NOTE: probably always true, since we flip from a negative scale with any negative components
            m.subMeshCount = srcMesh.subMeshCount;
            for (int i = 0; i < srcMesh.subMeshCount; ++i)
            {
                int[] indices = srcMesh.GetIndices(i);
                if (indices == null)
                    continue;
                var descriptor = srcMesh.GetSubMesh(i);
                
                m.SetIndices(indices, descriptor.topology, i);
            }

            if (shouldFlipTrianglesWinding)
            {
                int[] triangles = m.triangles;
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    int temp = triangles[i + 1];
                    triangles[i + 1] = triangles[i + 2];
                    triangles[i + 2] = temp;
                }

                m.triangles = triangles;
            }

            Vector3[] normals = srcMesh.normals;
            Vector4[] tangents = srcMesh.tangents;
            for (int i = 0; i < verts.Length; ++i)
            {
                // Normals
                var newNormal = srcTransform.TransformVector(normals[i]);
                newNormal = target.InverseTransformVector(newNormal);
                normals[i] = newNormal.normalized;
                
                // Tangents
                var tangentsDir = new Vector3(tangents[i].x, tangents[i].y, tangents[i].z);
                tangentsDir = target.InverseTransformVector(srcTransform.InverseTransformVector(tangentsDir));
                tangents[i] = new Vector4(tangentsDir.x, tangentsDir.y, tangentsDir.z, 
                    shouldFlipTrianglesWinding ? -tangents[i].w : tangents[i].w);
            }
            m.normals = normals;
            m.tangents = tangents;
            
            // Copy uvs
            Vector2[] uvs = srcMesh.uv;
            Vector2[] uvs2 = srcMesh.uv2;
            Vector2[] uvs3 = srcMesh.uv3;
            Vector2[] uvs4 = srcMesh.uv4;
            Vector2[] uvs5 = srcMesh.uv5;
            Vector2[] uvs6 = srcMesh.uv6;
            Vector2[] uvs7 = srcMesh.uv7;
            Vector2[] uvs8 = srcMesh.uv8;
            m.uv = uvs;
            m.uv2 = uvs2;
            m.uv3 = uvs3;
            m.uv4 = uvs4;
            m.uv5 = uvs5;
            m.uv6 = uvs6;
            m.uv7 = uvs7;
            m.uv8 = uvs8;
            
            //Recalculate bounds of the mesh, for the renderer's sake
            //The 'center' parameter of certain colliders needs to be adjusted
            //when the transform position is modified
            m.RecalculateBounds();

            return m;
        }

        private static void CopyMeshRenderer(Transform t, Transform child, bool keepOriginal = true)
        {
            var renderer = t.GetComponent<MeshRenderer>();

            if (renderer != null)
            {
                var newRenderer = Undo.AddComponent<MeshRenderer>(child.gameObject);

                newRenderer.materials = renderer.sharedMaterials;
                newRenderer.additionalVertexStreams = renderer.additionalVertexStreams;
#if UNITY_2019_2_OR_NEWER
                newRenderer.receiveGI = renderer.receiveGI;
#endif
#if UNITY_2019_4_OR_NEWER // Introduced in _3 but only in the latter versions
                newRenderer.scaleInLightmap = renderer.scaleInLightmap;
                newRenderer.stitchLightmapSeams = renderer.stitchLightmapSeams;
#endif
                newRenderer.receiveShadows = renderer.receiveShadows;
                newRenderer.lightmapIndex = renderer.lightmapIndex;
                newRenderer.probeAnchor = renderer.probeAnchor;
                newRenderer.rendererPriority = renderer.rendererPriority;
                newRenderer.shadowCastingMode = renderer.shadowCastingMode;

                if (!keepOriginal)
                    Undo.DestroyObjectImmediate(renderer);
            }
        }
        
        private static void Cleanup(MeshFilter mf)
        {
            if (mf == null)
                return;

            var renderer = mf.GetComponent<MeshRenderer>();
            if (renderer != null)
                Undo.DestroyObjectImmediate(renderer);

            GameObject go = mf.gameObject;
            Undo.DestroyObjectImmediate(mf);

            while (go != null)
            {
                if (!go.IsEmpty())
                    break;

                var parentTransform = go.transform.parent;
                Undo.DestroyObjectImmediate(go);
                
                go = parentTransform != null ? parentTransform.gameObject : null;
            }
        }
        
        [MenuItem(GAMEOBJECT_ITEM_NAME, true, 11)]
        private static bool RectifyNegativeScaleComplexValidate()
        {
            if (Selection.gameObjects == null || Selection.gameObjects.Length > 1)
                return false;
            
            if (!(Selection.activeObject is GameObject selectedGO))
                return false;
            //     
            // GameObject go = menuCommand.context as GameObject;
            if (selectedGO == null)
                return false;

            var comps = selectedGO.GetComponentsInChildren<MeshFilter>();
            if (comps.Length == 0)
                return false;

            return comps.Any(HasNegativeScale);
        }

        [MenuItem("CONTEXT/MeshFilter/Rectify Negative Scale", true)]
        private static bool RectifyNegativeScaleValidate(MenuCommand menuCommand)
        {
            MeshFilter mf = menuCommand.context as MeshFilter;
            return HasNegativeScale(mf);
        }

        private static bool HasNegativeScale(MeshFilter mf)
        {
            Transform t = mf.transform;
            while (t != null)
            {
                if (HasNegativeScale(t, true))
                    return true;
                t = t.parent;
            }

            return false;
        }
        
        private static ValueDropdownList<Transform> GetPositiveParents(Transform t)
        {
            var options = new ValueDropdownList<Transform>();
            Transform parent = t;
            while (parent != null)
            {
                parent = parent.parent;
                if (parent == null)
                {
                    options.Add("[SCENE ROOT]", null);
                }
                else
                {
                    if (!HasNegativeScale(parent))
                        options.Add(parent.GetGameObjectPath(), parent);
                }
            }

            options.Reverse();
            return options;
        }
        
        private static bool HasNegativeScale(Transform t, bool local = false)
        {
            if (local)
                return t.localScale.x < 0.0f || 
                       t.localScale.y < 0.0f || 
                       t.localScale.z < 0.0f;
            return t.lossyScale.x < 0.0f || 
                   t.lossyScale.y < 0.0f || 
                   t.lossyScale.z < 0.0f;
        }
    }
}