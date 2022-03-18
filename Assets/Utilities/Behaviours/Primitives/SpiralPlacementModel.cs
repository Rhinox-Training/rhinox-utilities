using Rhinox.Lightspeed;
using UnityEngine;

namespace Rhinox.Utilities
{
	[RefactoringOldNamespace("")]
	[System.Serializable]
	public class SpiralPlacementModel
	{
		public Vector3 position;
		public Vector3 angle;

		public SpiralPlacementModel(Vector3 position, Vector3 angle)
		{
			this.position = position;
			this.angle = angle;
		}
	}
}