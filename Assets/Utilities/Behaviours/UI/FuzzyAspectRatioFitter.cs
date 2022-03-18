using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Rhinox.Utilities
{
    [ExecuteInEditMode]
    public class FuzzyAspectRatioFitter : MonoBehaviour
    {
        public float AspectRatio = 1.0f;
        public float Fuzziness = 0.005f;

        public AspectRatioFitter.AspectMode AspectMode = AspectRatioFitter.AspectMode.None;

        private RectTransform _rectTransform;

        // Start is called before the first frame update
        void Start()
        {
            _rectTransform = gameObject.GetComponent<RectTransform>();
        }

        // Update is called once per frame
        void Update()
        {
            if (Math.Abs(GetAspectRatio() - AspectRatio) > Fuzziness)
            {
                switch (AspectMode)
                {
                    case AspectRatioFitter.AspectMode.HeightControlsWidth:
                        var width = _rectTransform.rect.height * AspectRatio;
                        _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
                        break;
                    case AspectRatioFitter.AspectMode.WidthControlsHeight:
                        var height = _rectTransform.rect.width / AspectRatio;
                        _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
                        break;
                    default:
                        break;
                }
            }
        }

        private float GetAspectRatio()
        {
            return _rectTransform.rect.width / _rectTransform.rect.height;
        }
    }
}