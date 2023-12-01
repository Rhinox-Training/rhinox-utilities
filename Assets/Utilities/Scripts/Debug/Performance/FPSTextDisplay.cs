using UnityEngine;
using System.Collections;
using Rhinox.Lightspeed;
using UnityEngine.UI;

namespace Rhinox.Utilities
{
    public class FPSTextDisplay : MonoBehaviour
    {
        float _updateInterval = 0.1f;
        float _accum;
        int _frames;
        float _timeLeft;
        Text _textUI;

        void Start()
        {
            _timeLeft = _updateInterval;
            _textUI = GetComponent<Text>();
        }

        void Update()
        {
            _timeLeft -= Time.deltaTime;
            _accum += Time.timeScale / Time.deltaTime;
            ++_frames;

            // Interval ended - update GUI text and start new interval
            if (_timeLeft <= 0.0)
            {
                // display two fractional digits (f2 format)
                float fps = _accum / _frames;
                string format = System.String.Format("{0:F2} FPS", fps);
                _textUI.text = format;

                if (fps < 30)
                    _textUI.material.color = Color.yellow;
                else if (fps < 10)
                    _textUI.material.color = Color.red;
                else
                    _textUI.material.color = Color.green;

                _timeLeft = _updateInterval;
                _accum = 0.0F;
                _frames = 0;
            }
        }
    }
}