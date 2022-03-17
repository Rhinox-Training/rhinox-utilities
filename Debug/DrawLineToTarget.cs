using System;
using System.Collections;
using System.Collections.Generic;
using Rhinox.Utilities;
using UnityEngine;

public class DrawLineToTarget : MonoBehaviour
{
    public Transform Target;

    private void OnDrawGizmos()
    {
        var color = Gizmos.color;
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, Target.position);
        Gizmos.color = color;
    }
}
