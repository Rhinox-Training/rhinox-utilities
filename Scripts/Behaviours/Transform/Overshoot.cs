using UnityEngine;
using UnityEngine.Events;

namespace Rhinox.Utilities
{
	public class Overshoot : MonoBehaviour
	{
		public float Scale = 1.5f;

		private float _currentScale = 1f;

		public float TimePeak = .3f;

		private bool _busy = false;
		private float _timeSinceStart;

		public UnityEvent OnDone;


		// Update is called once per frame
		void Update()
		{
			if (_busy)
			{
				_timeSinceStart += Time.deltaTime;

				Reset();

				if (_timeSinceStart > TimePeak * 2)
				{
					if (OnDone != null)
						OnDone.Invoke();
					_busy = false;
					return;
				}

				var pct = (_timeSinceStart % TimePeak) / TimePeak;

				if (_timeSinceStart <= TimePeak)
					_currentScale = Mathf.Lerp(_currentScale, Scale, Mathf.Pow(pct, 2));
				else
					_currentScale = Mathf.Lerp(_currentScale, 1f, Mathf.Pow(pct, 2));

				transform.localScale *= _currentScale;
			}
		}

		[ContextMenu("Overshoot")]
		public void Trigger()
		{
			if (_busy) return;

			_timeSinceStart = 0f;
			_busy = true;
		}

		[ContextMenu("Undershoot")]
		public void TriggerNegative()
		{
			if (_busy) return;

			var oldScale = Scale;
			Scale = 1 / Scale;

			_timeSinceStart = 0f;
			_busy = true;

			UnityAction resetAction = null;
			resetAction = () =>
			{
				OnDone.RemoveListener(resetAction);
				Scale = oldScale;
			};

			OnDone.AddListener(resetAction);
		}

		public void Reset()
		{
			transform.localScale /= _currentScale;
		}
	}
}