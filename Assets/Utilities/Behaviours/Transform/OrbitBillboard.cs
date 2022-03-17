using System;
using UnityEngine;

public class OrbitBillboard : MonoBehaviour
{
    public GameObject Target;
    public Vector3 WorldOffset;
    private Transform _cameraObject;

    private Transform CachedCamera => _cameraObject != null ? _cameraObject : _cameraObject = Camera.main?.transform;

    public bool RotateToCamera = true;

    private void Start()
    {
        ExecuteFollow();
    }
    
    private void LateUpdate()
    {
        ExecuteFollow();
    }

    private void ExecuteFollow()
    {
        if (RotateToCamera && CachedCamera == null)
            return;

        if (Target != null)
            transform.position = Target.transform.position + WorldOffset;

        if (!RotateToCamera)
            return;
        
        var forward = transform.forward;
        var targetForward = (CachedCamera.position - transform.position);
        if (targetForward.sqrMagnitude < float.Epsilon)
            return;
        var normTargetForward = targetForward.normalized;

        forward.y = 0;
        normTargetForward.y = 0;

        transform.rotation *= Quaternion.FromToRotation(forward, normTargetForward);
    }
}