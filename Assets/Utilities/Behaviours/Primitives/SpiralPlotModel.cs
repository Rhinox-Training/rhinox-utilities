using UnityEngine;

[System.Serializable]
public class SpiralPlotModel
{
	[SerializeField] public string label;
	[Range(.00001f, 1f)] [SerializeField] public float percent = .5f;
	[SerializeField] public Transform visual;

	public SpiralPlotModel(string label, float percent, Transform visual)
	{
		this.label = label;
		this.percent = percent;
		this.visual = visual;
	}
}