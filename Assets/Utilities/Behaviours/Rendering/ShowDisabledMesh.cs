using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Rhinox.Lightspeed;
using Rhinox.Utilities;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

[ExecuteAlways]
public class ShowDisabledMesh : MonoBehaviour
{
    [Autohook]
    public MeshFilter Filter;
    [Autohook]
    public MeshRenderer Renderer;

    public bool Wireframe = false;
    
#if UNITY_EDITOR
    private static HashSet<ShowDisabledMesh> Objects = new HashSet<ShowDisabledMesh>();
    private static Material WireFrameMaterial;

    private int _drawn;
    
    private ShowDisabledMesh()
    {
        Objects.Add(this);
        _drawn = 0;
    }
    
    ~ShowDisabledMesh()
    {
        Objects.Remove(this);
    }

    [InitializeOnLoadMethod, Button("Refresh Hook"), ButtonGroup]
    private static void HookCall()
    {
        Utility.SubscribeToSceneGui(DrawMeshWhenDisabled);
    }

    [Button("Refresh Objects"), ButtonGroup]
    private void RefreshObjects()
    {
        Objects.Clear();
        Utility.FindSceneObjectsOfTypeAll(Objects);
    }

    private static void DrawMeshWhenDisabled(SceneView view)
    {
        Objects.RemoveAll(x => x == null);
        foreach (var o in Objects)
        {
            if (o.Filter == null || o.Renderer == null || o._drawn >= Time.frameCount) return;
            
            if (o.Renderer.enabled && o.gameObject.activeInHierarchy) return;

            var t = o.transform;

            if (o.Wireframe && WireFrameMaterial == null)
                CreateWireFrameMaterial();

            var mat = o.Wireframe ? WireFrameMaterial : o.Renderer.sharedMaterial;
            Graphics.DrawMesh (o.Filter.sharedMesh, t.position, t.rotation, mat, o.gameObject.layer);
            
            o._drawn = Time.frameCount;
        }
    }

    private static void CreateWireFrameMaterial()
    {
        var shader = Shader.Find("Rhinox/Wireframe-Transparent-Culled");
        WireFrameMaterial = new Material(shader);
        WireFrameMaterial.SetColor("_WireColor", Color.black.With(a: .25f));
        WireFrameMaterial.SetColor("_BaseColor", Color.clear);
    }
#endif
}
