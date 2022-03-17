using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialCacher : MonoBehaviour
{
    Material _mat;

	void Start ()
    {
        _mat = GetComponent<Renderer>().sharedMaterial;
	}
	
	public void Reset()
    {
        GetComponent<Renderer>().sharedMaterial = _mat;
    }
}
