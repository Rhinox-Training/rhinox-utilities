using UnityEngine;

namespace Rhinox.Utilities
{
    public class ToggleChildren : MonoBehaviour
    {
        private void Awake()
        {
            ToggleAllChildren(false);
        }

        public void ToggleAllChildren(bool isEnabled)
        {
            foreach (Transform child in transform)
                child.gameObject.SetActive(isEnabled);
        }
    }
}