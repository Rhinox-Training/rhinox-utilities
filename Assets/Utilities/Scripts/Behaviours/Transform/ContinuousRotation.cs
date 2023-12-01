using UnityEngine;
using System.Collections;
using UnityEngine.Serialization;

namespace Rhinox.Utilities
{
	public class ContinuousRotation : MonoBehaviour
	{
		public Vector3 Rotation;

		[FormerlySerializedAs("AffectedByTime")] 
		public bool AffectedByTimeScale = true;

		// Update is called once per frame
		void Update()
		{
			transform.Rotate(Rotation * (AffectedByTimeScale ? Time.deltaTime : Time.unscaledDeltaTime));
		}
	}
}
