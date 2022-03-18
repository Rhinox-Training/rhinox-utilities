using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Rhinox.Utilities
{
    public class ObjectToggler : MonoBehaviour
    {
        public void ToggleGameObject(GameObject obj)
        {
            obj.SetActive(!obj.activeSelf);
        }
    }
}