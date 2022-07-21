using System.Collections;
using System.Collections.Generic;
using Rhinox.Lightspeed;
using Sirenix.OdinInspector;
using UnityEngine;

public class DefaultMaterialTest : MonoBehaviour
{
    public MaterialType Type;

    private MeshFilter _filter;
    private MeshRenderer _renderer;
    
    public void OnValidate()
    {
        _filter = gameObject.GetOrAddComponent<MeshFilter>();
        _renderer = gameObject.GetOrAddComponent<MeshRenderer>();
        
    }
}
