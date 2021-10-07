using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
This class is used to draw payline/cluster boxes for slot outcomes.
It was designed originally to replace the RageSpline RoundedRectangle class.
*/
public class PaylineBoxDrawer : PaylineDrawer
{
	public const string RESOURCE_PATH = "Prefabs/Slots/Payline/Simple Payline Box";
	
	public Vector2 position;						// Local position within the overall payline prefab.
	public Vector2 boxSize = Vector2.one;
	public Vector2 boxCenterOffset = Vector2.zero;	// Offsets from the center, this allows for centering of multi cell boxes.
		
	private static Vector3[] verts = new Vector3[16];	// Boxes always have 16 verts, so let's reuse the hell out of this array
	

	// Generate and cache the box mesh colors array, which never changes.
	public static Color[] boxMeshColors
	{
		get
		{
			if (_boxMeshColors == null)
			{
				Color edge = new Color(1f, 1f, 1f, 0f);
				Color inner = new Color(1f, 1f, 1f, 1f);
				_boxMeshColors = new Color[]
				{
					edge, inner, inner, edge,
					edge, inner, inner, edge,
					edge, inner, inner, edge,
					edge, inner, inner, edge
				};
			}
			return _boxMeshColors;
		}
	}
	private static Color[] _boxMeshColors = null;

	// Generate and cache the box mesh triangles array, which never changes.
	public static int[] boxMeshTriangles
	{
		get
		{
			if (_boxMeshTriangles == null)
			{
				_boxMeshTriangles = new int[]
				{
					1, 4, 5, 4, 1, 0,
					2, 5, 6, 5, 2, 1,
					3, 6, 7, 6, 3, 2,

					5, 8, 9, 8, 5, 4,
					6, 9, 10, 9, 6, 5,
					7, 10, 11, 10, 7, 6,

					9, 12, 13, 12, 9, 8,
					10, 13, 14, 13, 10, 9,
					11, 14, 15, 14, 11, 10,

					13, 0, 1, 0, 13, 12,
					14, 1, 2, 1, 14, 13,
					15, 2, 3, 2, 15, 14,
				};
			}
			return _boxMeshTriangles;
		}
	}
	private static int[] _boxMeshTriangles = null;
	
	// Constructor
	public PaylineBoxDrawer(Vector3 position3, ReelGame activeGame = null)
	{
		if (activeGame == null)
		{
			activeGame = ReelGame.activeGame;
		}

		paylineScaler = activeGame.paylineScaler;

		// We only need the x and y, so convert the input position to Vector2.
		position = new Vector3(position3.x, position3.y);
	}
	
	// Called after manipulating any number of the mesh dimension variables to rebuild the associated meshes.
	public CombineInstance[] refreshShape()
	{
		// Build new meshes
		outlineMesh = computeBoxMesh(outlineThickness, softnessThickness, 0.01f);
		inlineMesh = computeBoxMesh(inlineThickness, softnessThickness, 0f);
		highlightMesh = computeBoxMesh(highlightThickness, softnessThickness, -0.01f);

		// Prepare to combine the meshes into a single mesh.
		CombineInstance[] combine = new CombineInstance[3];
		combine[0] = OutcomeDisplayScript.createCombineInstance(outlineMesh.mesh);
		combine[1] = OutcomeDisplayScript.createCombineInstance(inlineMesh.mesh);
		combine[2] = OutcomeDisplayScript.createCombineInstance(highlightMesh.mesh);
		return combine;
	}

	// Computes a box mesh to the given dimensions.
	private PaylineMesh computeBoxMesh(float thickness, float softness, float zOffset)
	{
		Vector2 offset = position + boxCenterOffset;
		float width = boxSize.x;
		float height = boxSize.y;
		float strokeReach = thickness * 0.5f;
		float softReach = strokeReach + softness;
		
		PaylineMesh paylineMesh = DrawerCache.getPaylineMesh(verts.Length, boxMeshTriangles.Length);
		Mesh mesh = paylineMesh.mesh;
		
		// Build the verts, which go from inside the box to outside in four point per corner.
		
		verts[0] =	new Vector3(	offset.x + width - softReach, 		offset.y + height - softReach, 		zOffset);
		verts[1] =	new Vector3(	offset.x + width - strokeReach, 	offset.y + height - strokeReach, 	zOffset);
		verts[2] =	new Vector3(	offset.x + width + strokeReach, 	offset.y + height + strokeReach, 	zOffset);
		verts[3] =	new Vector3(	offset.x + width + softReach, 		offset.y + height + softReach, 		zOffset);

		verts[4] =	new Vector3(	offset.x - width + softReach, 		offset.y + height - softReach, 		zOffset);
		verts[5] =	new Vector3(	offset.x - width + strokeReach, 	offset.y + height - strokeReach, 	zOffset);
		verts[6] =	new Vector3(	offset.x - width - strokeReach, 	offset.y + height + strokeReach, 	zOffset);
		verts[7] =	new Vector3(	offset.x - width - softReach, 		offset.y + height + softReach, 		zOffset);

		verts[8] =	new Vector3(	offset.x - width + softReach, 		offset.y - height + softReach, 		zOffset);
		verts[9] =	new Vector3(	offset.x - width + strokeReach, 	offset.y - height + strokeReach, 	zOffset);
		verts[10] =	new Vector3(	offset.x - width - strokeReach, 	offset.y - height - strokeReach, 	zOffset);
		verts[11] =	new Vector3(	offset.x - width - softReach, 		offset.y - height - softReach, 		zOffset);

		verts[12] =	new Vector3(	offset.x + width - softReach, 		offset.y -height + softReach, 		zOffset);
		verts[13] =	new Vector3(	offset.x + width - strokeReach, 	offset.y -height + strokeReach, 	zOffset);
		verts[14] =	new Vector3(	offset.x + width + strokeReach, 	offset.y -height - strokeReach, 	zOffset);
		verts[15] =	new Vector3(	offset.x + width + softReach, 		offset.y -height - softReach, 		zOffset);
		
		mesh.vertices = verts;
		mesh.colors = boxMeshColors;
		mesh.triangles = boxMeshTriangles;
		mesh.RecalculateBounds();

		return paylineMesh;
	}
}
