using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PaylineLineDrawer : PaylineDrawer
{
	public const string RESOURCE_PATH = "Prefabs/Slots/Payline/Simple Payline Line";
	
	public Vector2[] linePoints;
			
	private Vector3[] verts = null;
	private Color[] colors = null;
	private int[] triangles = null;
	
	// Constructor
	public PaylineLineDrawer()
	{
		paylineScaler = ReelGame.activeGame.paylineScaler;
	}
		
	/// Called after manipulating any number of the mesh dimension variables to rebuild the associated meshes.
	public CombineInstance[] refreshShape()
	{
		// Handle edge case of too few points
		if (linePoints.Length < 2)
		{
			Debug.LogWarning("PaylineLineDrawer.refreshShape failed to redraw line with less than 2 points!");
			return null;
		}
		
		// Build new meshes
		outlineMesh = computeLineMesh(outlineThickness, softnessThickness, 0.01f);
		inlineMesh = computeLineMesh(inlineThickness, softnessThickness, 0f);
		highlightMesh = computeLineMesh(highlightThickness, softnessThickness, -0.01f);
		
		// Prepare to combine the meshes into a single mesh.
		CombineInstance[] combine = new CombineInstance[3];
		combine[0] = OutcomeDisplayScript.createCombineInstance(outlineMesh.mesh);
		combine[1] = OutcomeDisplayScript.createCombineInstance(inlineMesh.mesh);
		combine[2] = OutcomeDisplayScript.createCombineInstance(highlightMesh.mesh);
		return combine;
	}

	/// Helper function to set the points for a line and automatically call refreshShape().
	public CombineInstance[] setPoints(params Vector2[] newPoints)
	{
		linePoints = newPoints;
		return refreshShape();
	}

	/// Computes a line mesh that follows the given points.
	private PaylineMesh computeLineMesh(float thickness, float softness, float zOffset)
	{
		float strokeReach = thickness * 0.5f;
		float softReach = strokeReach + softness;

		Color edgeColor = new Color(1f, 1f, 1f, 0f);
		Color innerColor = new Color(1f, 1f, 1f, 1f);

		int vertCount = 2 + (4 * linePoints.Length) + 2;
		int triCount = 12 + (18 * (linePoints.Length - 1)) + 12;
		int p = 0;
		
		PaylineMesh paylineMesh = DrawerCache.getPaylineMesh(vertCount, triCount);
		Mesh mesh = paylineMesh.mesh;
		verts = CommonDataStructures.resizedArray<Vector3>(verts, vertCount);
		colors = CommonDataStructures.resizedArray<Color>(colors, vertCount);
		triangles = CommonDataStructures.resizedArray<int>(triangles, triCount);

		// Fill out vertices according to paper speck
		Vector2 p1, p2, p3;		// Points
		Vector2 f12, f23;		// Forward vectors
		Vector2 t12, t23;		// Tangent vectors
		Vector2 intersect;		// Intersection point

		int v = 0;	// Current vertex index

		p = 0;
		p2 = linePoints[p];
		p3 = linePoints[p + 1];
		f23 = (p3 - p2).normalized;
		t23 = new Vector2(-f23.y, f23.x);

		colors[v] = edgeColor;
		verts[v++] = new Vector3(
			p2.x + t23.x * softReach - f23.x * softness,
			p2.y + t23.y * softReach - f23.y * softness,
			zOffset);

		colors[v] = edgeColor;
		verts[v++] = new Vector3(
			p2.x - t23.x * softReach - f23.x * softness,
			p2.y - t23.y * softReach - f23.y * softness,
			zOffset);

		colors[v] = edgeColor;
		verts[v++] = new Vector3(
			p2.x + t23.x * softReach,
			p2.y + t23.y * softReach,
			zOffset);

		colors[v] = innerColor;
		verts[v++] = new Vector3(
			p2.x + t23.x * strokeReach,
			p2.y + t23.y * strokeReach,
			zOffset);

		colors[v] = innerColor;
		verts[v++] = new Vector3(
			p2.x - t23.x * strokeReach,
			p2.y - t23.y * strokeReach,
			zOffset);

		colors[v] = edgeColor;
		verts[v++] = new Vector3(
			p2.x - t23.x * softReach,
			p2.y - t23.y * softReach,
			zOffset);

		for (p = 1; p < linePoints.Length - 1; p++)
		{
			p1 = linePoints[p - 1];
			p2 = linePoints[p];
			p3 = linePoints[p + 1];

			f12 = (p2 - p1).normalized;
			t12 = new Vector2(-f12.y, f12.x);
			f23 = (p3 - p2).normalized;
			t23 = new Vector2(-f23.y, f23.x);

			intersect = calcIntersect(p2, f12, f23, t12, t23, softReach);
			colors[v] = edgeColor;
			verts[v++] = new Vector3(intersect.x, intersect.y, zOffset);

			intersect = calcIntersect(p2, f12, f23, t12, t23, strokeReach);
			colors[v] = innerColor;
			verts[v++] = new Vector3(intersect.x, intersect.y, zOffset);

			intersect = calcIntersect(p2, f12, f23, t12, t23, -strokeReach);
			colors[v] = innerColor;
			verts[v++] = new Vector3(intersect.x, intersect.y, zOffset);

			intersect = calcIntersect(p2, f12, f23, t12, t23, -softReach);
			colors[v] = edgeColor;
			verts[v++] = new Vector3(intersect.x, intersect.y, zOffset);
		}

		p = linePoints.Length - 1;
		p1 = linePoints[p - 1];
		p2 = linePoints[p];
		f12 = (p2 - p1).normalized;
		t12 = new Vector2(-f12.y, f12.x);;

		colors[v] = edgeColor;
		verts[v++] = new Vector3(
			p2.x + t12.x * softReach,
			p2.y + t12.y * softReach,
			zOffset);

		colors[v] = innerColor;
		verts[v++] = new Vector3(
			p2.x + t12.x * strokeReach,
			p2.y + t12.y * strokeReach,
			zOffset);

		colors[v] = innerColor;
		verts[v++] = new Vector3(
			p2.x - t12.x * strokeReach,
			p2.y - t12.y * strokeReach,
			zOffset);

		colors[v] = edgeColor;
		verts[v++] = new Vector3(
			p2.x - t12.x * softReach,
			p2.y - t12.y * softReach,
			zOffset);

		colors[v] = edgeColor;
		verts[v++] = new Vector3(
			p2.x + t12.x * softReach + f12.x * softness,
			p2.y + t12.y * softReach + f12.y * softness,
			zOffset);

		colors[v] = edgeColor;
		verts[v++] = new Vector3(
			p2.x - t12.x * softReach + f12.x * softness,
			p2.y - t12.y * softReach + f12.y * softness,
			zOffset);

		// Fill out vertices according to paper speck
		int t = 0;	// Current triangle vertex index

		// Start cap
		triangles[t++] = 0;
		triangles[t++] = 2;
		triangles[t++] = 3;

		triangles[t++] = 0;
		triangles[t++] = 3;
		triangles[t++] = 1;

		triangles[t++] = 1;
		triangles[t++] = 3;
		triangles[t++] = 4;

		triangles[t++] = 1;
		triangles[t++] = 4;
		triangles[t++] = 5;

		// Line body
		for (p = 0; p < linePoints.Length - 1; p++)
		{
			// For each point, draw the 6 triangles connecting it to the next
			for (v = p * 4 + 2; v < p * 4 + 5; v++)
			{
				triangles[t++] = v;
				triangles[t++] = v + 4;
				triangles[t++] = v + 1;

				triangles[t++] = v + 1;
				triangles[t++] = v + 4;
				triangles[t++] = v + 5;
			}
		}

		// End cap
		triangles[t++] = vertCount - 1;
		triangles[t++] = vertCount - 3;
		triangles[t++] = vertCount - 4;

		triangles[t++] = vertCount - 1;
		triangles[t++] = vertCount - 4;
		triangles[t++] = vertCount - 2;

		triangles[t++] = vertCount - 2;
		triangles[t++] = vertCount - 4;
		triangles[t++] = vertCount - 5;

		triangles[t++] = vertCount - 2;
		triangles[t++] = vertCount - 5;
		triangles[t++] = vertCount - 6;

		// Assemble the mesh
		mesh.vertices = verts;
		mesh.colors = colors;
		mesh.triangles = triangles;
		mesh.RecalculateBounds();

		return paylineMesh;
	}

	/// Helper function calculates intersection points given various vector information
	private Vector2 calcIntersect(Vector2 p2, Vector2 f12, Vector2 f23, Vector2 t12, Vector2 t23, float reach)
	{
		// Calculate points (could probably be simplified)
		float x1 = p2.x + t12.x * reach;
		float y1 = p2.y + t12.y * reach;
		float x2 = x1 + f12.x;
		float y2 = y1 + f12.y;
		float x3 = p2.x + t23.x * reach;
		float y3 = p2.y + t23.y * reach;
		float x4 = x3 + f23.x;
		float y4 = y3 + f23.y;

		float a1 = y2 - y1;
		float b1 = x1 - x2;
		float c1 = x2 * y1 - x1 * y2;

		float a2 = y4 - y3;
		float b2 = x3 - x4;
		float c2 = x4 * y3 - x3 * y4;

		float d = a1 * b2 - a2 * b1;

		if (Mathf.Abs(d) < 0.001f)
		{
			// Lines are close enough to just use a simple tangent * reach
			return new Vector2(x1, y1);
		}
		else
		{
			// Return the intersection point for the input
			return new Vector2(
				(b1 * c2 - b2 * c1) / d,
				(a2 * c1 - a1 * c2) / d);
		}
	}
}
