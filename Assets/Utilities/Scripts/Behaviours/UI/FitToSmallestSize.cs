using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Rhinox.Utilities
{
    [RequireComponent(typeof(Image))]
    public class FitToSmallestSize : MonoBehaviour
    {
        private Image _image;
        [ShowInInspector, ReadOnly] private float _imageAspect;

        // Start is called before the first frame update
        void Start()
        {
            _image = GetComponent<Image>();
            _imageAspect = _image.preferredHeight / _image.preferredWidth;
        }

        // Update is called once per frame
        void Update()
        {
            float screenAspect = Screen.height / (float) Screen.width;

            float anchorDiff = (_imageAspect - screenAspect) * 2;


            if (screenAspect < _imageAspect)
            {
                _image.rectTransform.anchorMin = new Vector2(0, -anchorDiff);
                _image.rectTransform.anchorMax = new Vector2(1, 1 + anchorDiff);
            }
            // else if (anchorDiff < 0)
            // {
            //     _image.rectTransform.anchorMin = new Vector2(0, 0);
            //     _image.rectTransform.anchorMax = new Vector2(1, 1);
            // }
            else
            {
                _image.rectTransform.anchorMin = new Vector2(anchorDiff, 0);
                _image.rectTransform.anchorMax = new Vector2(1 - anchorDiff, 1);
            }
        }
    }
}
