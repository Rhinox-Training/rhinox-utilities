#if TEXT_MESH_PRO
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rhinox.Utilities
{
    [RequireComponent(typeof(Slider))]
    public class SliderLabelSetter : MonoBehaviour
    {
        public TextMeshProUGUI Text;
        public int Decimals = 2;

        Slider _slider;

        void Awake()
        {
            _slider = GetComponent<Slider>();
        }

        void OnEnable()
        {
            _slider.onValueChanged.AddListener(UpdateValue);
            UpdateValue(_slider.value);
        }

        void OnDisable()
        {
            _slider.onValueChanged.RemoveAllListeners();
        }

        void UpdateValue(float value)
        {
            Text.text = value.ToString("n" + Decimals);
        }
    }
}
#endif