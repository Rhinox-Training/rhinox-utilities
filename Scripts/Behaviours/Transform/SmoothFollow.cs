using Rhinox.Lightspeed;
using UnityEngine;

namespace Rhinox.Utilities
{
	public class SmoothFollow : MonoBehaviour
	{
		// The target we are following
		public Transform target;

		// The distance in the x-z plane to the target
		public float Distance = 10.0f;
		[Range(0, 180)]
		public float AcceptableAngle = 30f;

		// the height we want the camera to be above the target
		public float Height = 5.0f;

		// How much we 
		public float HeightDamping = 2.0f;
		public float RotationDamping = 3.0f;

		public bool FollowRotationZ;

		private const float MAX_DT = 1f / 60;

		private Quaternion DampedRotation(Vector3 axis)
		{
			var wantedRotationAngle = Vector3.Scale(target.eulerAngles, axis);
			var currentRotationAngle = Vector3.Scale(transform.eulerAngles, axis);

			var currentRotation = Quaternion.Euler(currentRotationAngle);
			var wantedRotation = Quaternion.Euler(wantedRotationAngle);

			var diff = QuaternionExtensions.Difference(currentRotation, wantedRotation);
			diff.ToAngleAxis(out float angle, out Vector3 rotationAxis);
			angle = Utility.WrapAngle(angle);

			if (angle < AcceptableAngle) return currentRotation;
			

			// var diff = wantedRotationAngle.z - currentRotationAngle.z;
			// if (Mathf.Abs(diff) >= 360)
			// 	wantedRotationAngle.z += 360 * (diff > 0 ? -1 : 1);

			var t = Mathf.Min(MAX_DT, Time.smoothDeltaTime);
			return Quaternion.Lerp(currentRotation, wantedRotation, RotationDamping * t);
		}

		// Place the script in the Camera-Control group in the component menu
		void LateUpdate()
		{
			// Early out if we don't have a target
			if (!target) return;

			// Damp the height
			float wantedHeight = target.position.y + Height;
			float currentHeight = transform.position.y;
			currentHeight = Mathf.Lerp(currentHeight, wantedHeight, HeightDamping * Time.deltaTime);

			// Calculate the current rotation angles
			var currentRotation = DampedRotation(Vector3.up);

			// Set the position of the camera on the x-z plane to:
			// distance meters behind the target
			transform.position = target.position;
			transform.position -= currentRotation * Vector3.forward * Distance;

			// Set the height of the camera
			transform.position = new Vector3(transform.position.x, currentHeight, transform.position.z);

			if (FollowRotationZ)
				transform.rotation = DampedRotation(new Vector3(0, 1, 1));
			else
				// Always look at the target
				transform.LookAt(target);
		}
	}
}