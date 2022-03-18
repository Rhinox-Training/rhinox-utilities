using UnityEngine;
using UnityEngine.Events;

namespace Rhinox.Utilities
{
	public class ActivationEvent : MonoBehaviour
	{
		public UnityEvent OnActivation;
		public UnityEvent OnDeactivation;

		private void OnEnable()
		{
			OnActivation?.Invoke();
		}

		private void OnDisable()
		{
			OnDeactivation?.Invoke();
		}
	}
}