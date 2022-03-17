using System.Collections;
using System.Collections.Generic;
using Rhinox.Lightspeed;
using UnityEngine;

namespace Rhinox.Utilities
{
	[RefactoringOldNamespace("")]
	[ExecuteInEditMode]
	[RequireComponent(typeof(Spiral))]
	public class SpiralPlotter : MonoBehaviour
	{

		[SerializeField] float mappedLength = 4543000000f;

		[Range(200000f, 4543000000f)] [SerializeField]
		float mappedSlice = 4543000000f;

		Spiral spiral;

		void Awake()
		{
			spiral = GetComponent<Spiral>();

			Refresh();
		}

		void Refresh()
		{
			UpdatePlotter();
		}

		public SpiralPlacementModel GetSpiralPlacement(float percentOfLength)
		{
			var cutoff = mappedLength - mappedSlice;
			var amount = percentOfLength * mappedLength;
			var targetPercent = 0f;
			if (amount >= cutoff)
			{
				targetPercent = (amount - cutoff) / mappedSlice;
			}

			if (spiral != null)
			{
				return spiral.GetPlacementAlongLength(targetPercent);
			}
			else
			{
				return new SpiralPlacementModel(Vector3.one, Vector3.one);
			}
		}

		public void UpdatePlotter()
		{
			var spiralPlots = GetComponents<SpiralPlot>();
			foreach (var spiralPlot in spiralPlots)
			{
				spiralPlot.UpdatePlot();
			}
		}

		void OnValidate()
		{
			Refresh();
		}
	}
}