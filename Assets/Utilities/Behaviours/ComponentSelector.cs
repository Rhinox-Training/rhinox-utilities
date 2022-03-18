using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Rhinox.Lightspeed;
using Sirenix.OdinInspector;

namespace Rhinox.Utilities
{
    [System.Serializable]
    public class ComponentSet
    {
        [HideLabel]
        public string Name = "Description Here";
        public string Tooltip;
        public GameObject[] Objects;

        public bool? GetIsActive()
        {
            if (Objects.IsNullOrEmpty()) return null;
            
            bool? result = null;
            foreach (var o in Objects)
            {
                if (result == null) // First item, just adapt to it
                    result = o.activeSelf;
                else // Inconsistencies in the set
                {
                    result = null;
                    break;
                }
            }

            return result;
        }
    }
    
    public class ComponentSelector : MonoBehaviour
    {
        [LabelText("Selectables")]
        public List<ComponentSet> internalComponents;
        
        // [HideInInspector]
        public bool IsEditing = true;

        public ComponentSet this[int index] => internalComponents[index];


        [ContextMenu("Toggle Editor")]
        private void ShowHideEditor() => IsEditing ^= true;

        private void Reset()
        {
            internalComponents = new List<ComponentSet>();
        }
    }
}