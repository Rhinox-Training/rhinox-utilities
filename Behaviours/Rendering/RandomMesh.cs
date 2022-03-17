using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using Rhinox.Lightspeed;
using Rhinox.Utilities;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class RandomMesh : MonoBehaviour
{
    public List<Mesh> MeshOptions; 

	// Use this for initialization
	void Start ()
	{
	    GetComponent<MeshFilter>().mesh = MeshOptions.GetRandomObject();
	}
}
