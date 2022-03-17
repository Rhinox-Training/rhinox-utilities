using System;
using System.Collections;
using System.Collections.Generic;
using Rhinox.Lightspeed;
using Rhinox.Utilities;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ImageAlphaSinewave : MonoBehaviour
{
    public float Delay;
    public float Speed = 1;
    
    private Image _image;

    private bool _stop;

    private void Awake()
    {
        _image = GetComponent<Image>();
        _image.color = _image.color.With(a: 0f);
    }

    private void OnEnable()
    {
        Invoke(nameof(StartSinewave), Delay);
    }

    private void OnDisable()
    {
        _stop = true;
    }
    
    private void StartSinewave()
    {
        StartCoroutine(UpdateAlpha());
    }
    
    private IEnumerator UpdateAlpha()
    {
        while (true)
        {
            if (_stop) break;
            
            var alpha = (Mathf.Sin((Time.timeSinceLevelLoad - Delay) * Speed) + 1) / 2;
            _image.color = _image.color.With(a: alpha);
            yield return null;
        }
    }
}
