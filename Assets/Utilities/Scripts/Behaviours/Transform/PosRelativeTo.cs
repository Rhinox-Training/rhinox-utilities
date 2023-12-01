using Rhinox.Lightspeed;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Rhinox.Utilities
{
	public class PosRelativeTo : MonoBehaviour
	{
		[Tooltip("Will default to parent")] public Transform RelativeTo;

		[ReadOnly] public Vector3 RelativePosition;

		[ButtonGroup, GUIColor(0.2f, 0.6f, .8f)]
		void SavePosition()
		{
			RelativePosition = GetRelativePosition();
		}

		[ButtonGroup, GUIColor(.8f, 0.2f, .6f)]
		void LoadPosition()
		{
			var relativeTo = GetRelativeTo();
			if (relativeTo)
				transform.localPosition += GetRelativePosition().DirectionTo(RelativePosition, normalized: false);
			else
				transform.localPosition = RelativePosition;
		}

		private Vector3 GetRelativePosition()
		{
			var relativeTo = GetRelativeTo();
			return relativeTo
				? relativeTo.LocalDirectionTo(transform, normalized: false)
				: transform.localPosition;
		}

		private Transform GetRelativeTo()
		{
			return RelativeTo == null ? transform.parent : RelativeTo;
		}
	}
}