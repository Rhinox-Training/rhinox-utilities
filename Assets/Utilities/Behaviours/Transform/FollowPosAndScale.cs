using Rhinox.Lightspeed;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Rhinox.Utilities
{
	[RefactoringOldNamespace("")]
	public class FollowPosAndScale : MonoBehaviour
	{
		public float YOffset = -.3f;
		public float SquashScale = .85f;

		[ShowInInspector, ReadOnly] public Transform TransformToFollow { get; set; }
		public Renderer[] Renderers { get; set; }

		void Start()
		{
			MoveToTarget();
		}

		void Update()
		{
			MoveToTarget();
		}

		private void MoveToTarget()
		{
			if (TransformToFollow == null)
			{
				return;
			}

			transform.position = TransformToFollow.transform.position.Add(y: YOffset);
			transform.localScale = transform.localScale.With(x: SquashScale, z: SquashScale);
		}
	}
}
