using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Rhinox.Utilities
{
	[ExecuteInEditMode]
	public class LinearTranslation : MonoBehaviour
	{
		public Vector3 MinValue;
		public Vector3 MaxValue;

		public event Action MinReached;
		public event Action MaxReached;

		private float _currentTranslation;

		[ShowInInspector, PropertyRange(0, 1)]
		public float Translation
		{
			get { return _currentTranslation; }
			set { Translate(value); }
		}

		public void Translate(float translation)
		{
			if (translation <= .005f)
			{
				translation = 0f;
				if (MinReached != null)
					MinReached.Invoke();
			}

			if (translation >= .995f)
			{
				translation = 1f;
				if (MaxReached != null)
					MaxReached.Invoke();
			}

			_currentTranslation = translation;
			transform.localPosition = Vector3.Lerp(MinValue, MaxValue, _currentTranslation);


		}
	}
}