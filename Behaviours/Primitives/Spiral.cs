using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter))]
public class Spiral : MonoBehaviour {

	[MaxValue(10f)] [SerializeField] float radius = 5;
	[MaxValue(10f)] [SerializeField] float width = 1.5f;
	[MaxValue(10f)] [SerializeField] float height = .5f;
	[SerializeField] float length = 9.06f;
	[MaxValue(120)] [SerializeField] int sides = 45;
	[MaxValue(5f)] [SerializeField] float offset = 2f;
	
	Mesh mesh;

	Vector3[] radiusSurfaceVertices;
	Vector3[] vertices;
	int[] triangles;

	void Awake()
	{
		// MeshRenderer
		if(gameObject.GetComponent<MeshRenderer>() == null)
		{
			MeshRenderer meshRenderer = gameObject.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
		}

		// MeshFilter
		mesh = new Mesh();
		GetComponent<MeshFilter>().mesh = mesh;

		Refresh();
	}

	void Refresh()
	{
		CreateScaffold();
		UpdateMesh();
		UpdatePlotter();
	}

	public SpiralPlacementModel GetPlacementAlongLength(float percent)
	{
		var vertOffset = Mathf.RoundToInt(percent * vertices.Length);
		vertOffset = Mathf.RoundToInt(Mathf.Clamp(vertOffset, 0, vertices.Length - 5));
		var p1 = vertices[vertOffset + 0];
		var p2 = vertices[vertOffset + 1];
		var p3 = vertices[vertOffset + 4];
		var angle = Vector3.Cross(p2 - p1, p2 - p3).normalized;

		return new SpiralPlacementModel(radiusSurfaceVertices[vertOffset/4], angle);
	}

	void CreateScaffold()
	{
		// Helpers
		float halfWidth = width/2;
		var sidesIncludingCaps = sides + 2;
		int dist = Mathf.CeilToInt(((int)sidesIncludingCaps) * length);
		Vector3 vertexSurfaceRadius;
		Vector3 vertexInnerTop;
		Vector3 vertexOuterTop;
		Vector3 vertexInnerBottom;
		Vector3 vertexOuterBottom;

		// Vertices
		radiusSurfaceVertices = new Vector3[dist];
		vertices = new Vector3[dist * 4];
		float step = offset / sides;
		float y = 0;
		for (var i = 0; i < dist; i++)
		{
			// Initial centered vert
			var angle = i * Mathf.PI * 2 / sides;
			var pos = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
			var upOffset = Vector3.up * y;
			var downOffset = Vector3.up * (y - height);

			// Radius surface helpers
			vertexSurfaceRadius = pos * radius;
			vertexSurfaceRadius += upOffset;

			// Inner top vertex
			vertexInnerTop = pos * (radius - halfWidth);
			vertexInnerTop += upOffset;
			
			// Outer top vertex
			vertexOuterTop = pos * (radius + halfWidth);
			vertexOuterTop += upOffset;

			// Inner bottom vertex
			vertexInnerBottom = pos * (radius - halfWidth);
			vertexInnerBottom += downOffset;

			// Outer bottom vertex
			vertexOuterBottom = pos * (radius + halfWidth);
			vertexOuterBottom += downOffset;

			// Update vertices
			radiusSurfaceVertices[i] = vertexSurfaceRadius;
			vertices[i*4+0] = vertexInnerTop;
			vertices[i*4+1] = vertexOuterTop;
			vertices[i*4+2] = vertexInnerBottom;
			vertices[i*4+3] = vertexOuterBottom;
			y += step;
		}

		// Triangles
		var vertexIterationCount = 24;
		var trianglePathVertexCount = dist * vertexIterationCount;
		var triangleCapVertexCount = 12;
		triangles = new int[trianglePathVertexCount + triangleCapVertexCount];
		var vertAnchor = 0;
		var meshAnchor = 0;
		var innerTop = 0;
		var outerTop = 1;
		var innerBottom = 2;
		var outerBottom = 3;
		for (var i = 0; i < dist; i++)
		{
			// Start cap
			if(i == 0)
			{
				triangles[vertAnchor+0] = innerBottom;
				triangles[vertAnchor+1] = innerTop;
				triangles[vertAnchor+2] = outerTop;
				triangles[vertAnchor+3] = innerBottom;
				triangles[vertAnchor+4] = outerTop;
				triangles[vertAnchor+5] = outerBottom;
				vertAnchor += 6;
			}

			// Path
			else if(i < dist - 1)
			{
				// Top quad
				triangles[vertAnchor+0] = meshAnchor + innerTop;
				triangles[vertAnchor+1] = meshAnchor + innerTop+4;
				triangles[vertAnchor+2] = meshAnchor + outerTop+4;
				triangles[vertAnchor+3] = meshAnchor + innerTop;
				triangles[vertAnchor+4] = meshAnchor + outerTop+4;
				triangles[vertAnchor+5] = meshAnchor + outerTop;
			
				// Right quad
				triangles[vertAnchor+6] = meshAnchor + outerTop;
				triangles[vertAnchor+7] = meshAnchor + outerTop+4;
				triangles[vertAnchor+8] = meshAnchor + outerBottom+4;
				triangles[vertAnchor+9] = meshAnchor + outerTop;
				triangles[vertAnchor+10] = meshAnchor + outerBottom+4;
				triangles[vertAnchor+11] = meshAnchor + outerBottom;

				// Bottom quad
				triangles[vertAnchor+12] = meshAnchor + outerBottom;
				triangles[vertAnchor+13] = meshAnchor + outerBottom+4;
				triangles[vertAnchor+14] = meshAnchor + innerBottom+4;
				triangles[vertAnchor+15] = meshAnchor + outerBottom;
				triangles[vertAnchor+16] = meshAnchor + innerBottom+4;
				triangles[vertAnchor+17] = meshAnchor + innerBottom;

				// Left quad
				triangles[vertAnchor+18] = meshAnchor + innerBottom;
				triangles[vertAnchor+19] = meshAnchor + innerBottom+4;
				triangles[vertAnchor+20] = meshAnchor + innerTop+4;
				triangles[vertAnchor+21] = meshAnchor + innerBottom;
				triangles[vertAnchor+22] = meshAnchor + innerTop+4;
				triangles[vertAnchor+23] = meshAnchor + innerTop;

				// Offset helpers
				vertAnchor += vertexIterationCount;
				meshAnchor += 4;
			}

			// End cap
			else
			{
				vertAnchor += vertexIterationCount;
				triangles[vertAnchor+0] = meshAnchor + outerTop;
				triangles[vertAnchor+1] = meshAnchor + innerTop;
				triangles[vertAnchor+2] = meshAnchor + innerBottom;
				triangles[vertAnchor+3] = meshAnchor + outerTop;
				triangles[vertAnchor+4] = meshAnchor + innerBottom;
				triangles[vertAnchor+5] = meshAnchor + outerBottom;
			}
		}
	}

	void UpdateMesh()
	{
		if(mesh != null)
		{
			mesh.Clear();

			mesh.vertices = vertices;
			mesh.triangles = triangles;

			mesh.RecalculateNormals();
		}
	}

	void UpdatePlotter()
	{
		var spiralPlotter = GetComponent<SpiralPlotter>();
		if(spiralPlotter != null)
		{
			spiralPlotter.UpdatePlotter();
		}
	}

	void OnValidate()
	{
		Refresh();
	}
}
