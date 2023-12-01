using UnityEngine;
using UnityEngine.UI;

namespace Rhinox.Utilities
{
	[RequireComponent(typeof(Image))]
	public class FlashImage : MonoBehaviour
	{
		public bool StartAtMin = true;
		public float FullFlashTime = 2f;

		public bool IsFlashing;
		private bool _goingUp = true;

		[Range(0f, 1f)] public float MinAlpha;

		[Range(0f, 1f)] public float MaxAlpha = 1f;

		private Image _image;
		private float _initialAlpha;

		// Use this for initialization
		private void Awake()
		{
			_image = GetComponent<Image>();

			if (StartAtMin)
				SetA(MinAlpha);

			_initialAlpha = _image.color.a;
		}

		private void OnEnable()
		{
			Reset();
		}

		// Update is called once per frame
		void Update()
		{
			if (IsFlashing)
			{
				var newA = _image.color.a;

				if (newA >= MaxAlpha)
					_goingUp = false;
				else if (newA <= MinAlpha)
					_goingUp = true;

				var range = MaxAlpha - MinAlpha;
				newA += (_goingUp ? 1 : -1) * (range * Time.deltaTime * FullFlashTime / 2);
				SetA(newA);
			}
		}

		public void Reset()
		{
			SetA(_initialAlpha);
		}

		private void SetA(float a)
		{
			var col = _image.color;
			col.a = a;
			_image.color = col;
		}
	}
}
