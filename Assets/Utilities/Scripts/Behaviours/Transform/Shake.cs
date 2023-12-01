using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

namespace Rhinox.Utilities
{
	public class Shake : MonoBehaviour
	{
		public bool AutoDampen = true;

		[Indent, Range(0.1f, 5f), ShowIf("AutoDampen")]
		public float DampTime = 1f;

		[Range(0f, 5f)] public float Amount = 1;

		[Indent, Range(0f, 1f)] public float TranslationalShake = 1;

		[Indent, Range(0f, 1f)] public float RotationalShake = 1;

		private bool HasRotationalShake
		{
			get { return RotationalShake > 0; }
		}

		[Indent(2), Range(0f, 1f), ShowIf("HasRotationalShake")]
		public float EulerX = 1f, EulerY, EulerZ;

		[PropertySpace(20), Range(0f, 1f)] public float ShakeTimer = 0;

		private Vector3 _shakeTranslation;
		private Vector3 _shakeEuler;
		private float _dampVelocity;

		public UnityEvent OnShakeStopped;
		public UnityEvent OnShakeStarted;

		[ButtonGroup("Shake")]
		public void ShakeHeavy()
		{
			AddShake(1f);
		}

		[ButtonGroup("Shake")]
		public void ShakeMajor()
		{
			AddShake(.7f);
		}

		[ButtonGroup("Shake")]
		public void ShakeMinor()
		{
			AddShake(.2f);
		}

		public void SetShake(float amount)
		{
			ShakeTimer = amount;
		}

		public void AddShake(float amount)
		{
			SetShake(ShakeTimer + amount);
		}

		private void Start()
		{
			if (ShakeTimer > 0)
				OnShakeStarted.Invoke();
		}

		public void StopShake(bool silent = false)
		{
			ShakeTimer = 0f;
			ResetPos();
			ResetRot();

			if (!silent)
				OnShakeStopped.Invoke();
		}

		// Update is called once per frame
		void Update()
		{
			if (ShakeTimer <= 0)
				return;

			var timeInfluence = Mathf.Pow(Mathf.Min(1, ShakeTimer), 2);
			var shakeAmount = Amount * timeInfluence;

			ResetPos();
			_shakeTranslation = Random.insideUnitSphere * shakeAmount * TranslationalShake;
			transform.localPosition += _shakeTranslation;

			ResetRot();
			_shakeEuler = new Vector3
			{
				x = Vector3.Angle(Vector2.up, Random.insideUnitCircle) * EulerX,
				y = Vector3.Angle(Vector2.up, Random.insideUnitCircle) * EulerY,
				z = Vector3.Angle(Vector2.up, Random.insideUnitCircle) * EulerZ
			} * shakeAmount * RotationalShake;
			transform.localRotation *= Quaternion.Euler(_shakeEuler);

			if (!AutoDampen) return;

			ShakeTimer = Mathf.SmoothDamp(ShakeTimer, 0f, ref _dampVelocity, DampTime);

			if (!(Mathf.Abs(_dampVelocity) < .0001f)) return;

			StopShake();
		}

		void ResetPos()
		{
			transform.localPosition -= _shakeTranslation;
			_shakeTranslation = Vector3.zero;
		}

		void ResetRot()
		{
			transform.localRotation *= Quaternion.Inverse(Quaternion.Euler(_shakeEuler));
			_shakeEuler = Vector3.zero;
		}

		[Button(ButtonSizes.Medium)]
		void Reset()
		{
			ResetPos();
			ResetRot();
		}
	}
}