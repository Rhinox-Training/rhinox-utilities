using Rhinox.Lightspeed;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Rhinox.Utilities
{
	[RefactoringOldNamespace("")]
	public class Billboard : MonoBehaviour
	{
		[ShowInInspector, ReadOnly] private Transform _target;

		public Transform UpTarget;

		public bool LockY;

		private void Awake()
		{
			_target = Camera.main?.transform;
		}

		private void Update()
		{
			if (_target == null)
				_target = Camera.main?.transform;
		}

		private void OnEnable()
		{
			LookAtTarget();
		}

		void LateUpdate()
		{
			LookAtTarget();
		}

		private void LookAtTarget()
		{
			if (_target == null)
				return;
			Vector3 targetPos = _target.position;
			if (LockY)
				targetPos.y = transform.position.y;
			transform.LookAt(targetPos, UpTarget == null ? Vector3.up : transform.position - UpTarget.position);
		}
	}
}