using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Rhinox.Utilities
{
    public class EventRelay : MonoBehaviour
    {
        public BetterEvent Relay;

        public void Trigger()
        {
            Relay.Invoke();
        }
    }
}