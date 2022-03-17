using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(SpiralPlotter))]
public class SpiralPlot : MonoBehaviour {

	[SerializeField] public string label;
	[SerializeField] public SpiralPlotModel[] plots;

	private SpiralPlotter spiralPlotter;

	void Awake()
	{
		spiralPlotter = GetComponent<SpiralPlotter>();
		
		Refresh();
	}

	void Refresh()
	{
		UpdatePlot();
	}

	public void UpdatePlot()
	{
		var hasSpiralPlotter = spiralPlotter != null;
		var hasPlots = plots != null;
		if(hasSpiralPlotter && hasPlots)
		{
			foreach (var plot in plots)
			{
				var hasVisual = plot.visual != null;
				if(hasVisual)
				{
					SpiralPlacementModel spiralPlacement = spiralPlotter.GetSpiralPlacement(plot.percent);
					plot.visual.transform.position = spiralPlacement.position;
					plot.visual.transform.up = spiralPlacement.angle;
				}
			}
		}
	}

	void OnValidate()
	{
		Refresh();
	}

}