using System.Collections;
using System.Collections.Generic;
using Rhinox.Lightspeed;
using UnityEngine;

public class SmoothFollowVR : MonoBehaviour
{
    public float SmoothTime = 0.3f;

    public Transform Target;

    private TransformState _localState;

    private Transform _target;
    private Transform _endTarget;

    private Vector3 _velocity;

    void Start()
    {
        var end = new GameObject();
        _endTarget = end.transform;
        _endTarget.SetParent(transform);
        _endTarget.Reset();
            
        Initialize();
    }

    void Initialize()
    {
        _target = Target;

        if (_target == null) return;
        
        _localState = TransformState.CreateRelative(transform, _target);
    }
    
    void LateUpdate()
    {
        if (_target != Target)
            Initialize();
        
        // Early out if we don't have a target
        if (!_target) return;
        
        TransformState.RestoreRelative(_localState, _endTarget, _target);

        transform.position = Vector3.SmoothDamp(transform.position, _endTarget.position, ref _velocity, SmoothTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, _endTarget.rotation, SmoothTime);
    }
}
