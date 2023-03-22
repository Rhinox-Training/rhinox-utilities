using UnityEngine;

namespace Tests
{
    public class RuntimeProjectSettingsTest : MonoBehaviour
    {
        public void Awake()
        {
            Debug.Log($"RuntimeProjectSettings.Instance.Enabled: {RuntimeProjectSettings.Instance.Enabled}");
        }
    }
}