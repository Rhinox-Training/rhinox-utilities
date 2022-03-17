using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class TrailBehaviour : MonoBehaviour
{
    private Transform _destObject;
    private Transform _sourceObject;
    private LineRenderer _lr;
    [FormerlySerializedAs("scrollSpeed")] public float ScrollSpeed = 3f;
    private float _offset = 0f;
    private Vector3 _sourceLocalPos;
	// Use this for initialization
	void Start ()
    {
        _lr = GetComponent<LineRenderer>();
        
        _lr.sharedMaterial.mainTextureScale = new Vector3(-10f, 1f);
	}
	
	// Update is called once per frame
	void Update ()
    {
        // set the positions for the line
        _lr.SetPosition(0, _sourceObject.transform.TransformPoint(_sourceLocalPos));
        _lr.SetPosition(1, _destObject.position);

        // UV animate the material
        _offset += Time.deltaTime * ScrollSpeed;
        _lr.sharedMaterial.mainTextureOffset = new Vector2(_offset % 1, 0);
    }
    
    // set the trail connection
    public void SetTrail(Transform source, Transform destinationObject, Vector3 sourceLocalPos)
    {
        _sourceLocalPos = sourceLocalPos;
        _sourceObject = source;
        _destObject = destinationObject;
    }

    private void OnDestroy()
    {
        _lr.sharedMaterial.mainTextureOffset = Vector2.zero;
    }
}
