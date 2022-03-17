using System;
using System.Collections;
using System.Collections.Generic;
using Rhinox.Lightspeed;
using Rhinox.Utilities;
using UnityEngine;

public class TestGizmos : MonoBehaviour
{
    public float Radius = 0.5f;

    public Axis Normal = Axis.X;

    public Vector2 Size = Vector2.one;

    public Vector3 Offset = Vector3.zero;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    #if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        GizmosExt.DrawRoundedRect(transform.position + transform.TransformDirection(Offset), Size, Radius, Normal);
    }
    #endif
}
