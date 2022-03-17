using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Rhinox.Lightspeed;

[RequireComponent(typeof(Material))]
public class ToggleMaterialTransparency : MonoBehaviour
{
    public float AlphaA = 1f;
    public float AlphaB = 0.25f;

    Material _mat;
    bool _isTransparent;

    private void Awake()
    {
        _mat = GetComponent<Renderer>().material;
    }

    public void ToggleTransparency()
    {
        _isTransparent = !_isTransparent;
        _mat.SetColor(a: _isTransparent ? AlphaB : AlphaA);
    }
}