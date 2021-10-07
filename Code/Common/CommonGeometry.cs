using UnityEngine;

/**
This is a purely static class of generic useful functions that relate to Geometry.
*/
public static class CommonGeometry
{

	/// Returns a GameObject with a MeshFilter and MeshRenderer that present the given plane.
	/// Uses the Common.createPlane function to generate the actual planar mesh.
	public static GameObject createPlaneObject(int unitWidth, int unitDepth, float unitSpacing = 1f, float uvLoops = 1f)
	{
		GameObject go = new GameObject();
		MeshFilter filter = go.AddComponent<MeshFilter>() as MeshFilter;
		filter.mesh = createPlane(unitWidth, unitDepth, unitSpacing, uvLoops);
		go.AddComponent<MeshRenderer>();
		return go;
	}
	
	/// Creates a planar mesh build with the given number of quads at a specific spacing.
	/// The resulting mesh is UV mapped such that the larger dimension uvs range from 0~uvLoops,
	/// and the texture itself maintains a square aspect ratio by cutting off at the far edge.
	/// Vertices in the mesh are generated from x=0 to x=unitWidth, from z=0 to z=unitDepth,
	/// such that a given vertex at (x, z) with unitSpacing=1 is at vertices[z * unitWidth + x].
	/// Set uvLoops to control how many times the uvs tile across the larger dimension.
	public static Mesh createPlane(int unitWidth, int unitDepth, float unitSpacing = 1f, float uvLoops = 1f)
	{
		Mesh mesh = new Mesh();
		Vector3[] vertices = new Vector3[(unitWidth + 1) * (unitDepth + 1)];
		Vector3[] normals = new Vector3[vertices.Length];
		Vector2[] uvs = new Vector2[vertices.Length];
		int[] triangles = new int[unitWidth * unitDepth * 6];
		
		float uvOffset = uvLoops / Mathf.Max(unitWidth, unitDepth);
		
		int i, x, z;		// Iterators
		int a, b, c, d;		// Point indices
		
		// Create vertices
		i = 0;
		for (z = 0; z < unitDepth + 1; z++)
		{
			for (x = 0; x < unitWidth + 1; x++)
			{
				vertices[i] = new Vector3(x * unitSpacing, 0.0f, z * unitSpacing);
				normals[i] = Vector3.up;			// Normals on the plane all face the same way
				uvs[i] = new Vector2(uvOffset * x, uvOffset * z);
				i++;
			}
		}
		
		// Create triangles
		i = 0;
		for (z = 0; z < unitDepth; z++)
		{
			for (x = 0; x < unitWidth; x++)
			{
				// Create two triangles at a time (a quad), so increment by 6 with each tile.
				// Figure out which indexes are for the four vertices around the current tile's quad.
				a = z * (unitWidth + 1) + x;
				b = (z + 1) * (unitWidth + 1) + x;
				c = (z + 1) * (unitWidth + 1) + x + 1;
				d = z * (unitWidth + 1) + x + 1;
				
				triangles[i] = a;
				triangles[i + 1] = b;
				triangles[i + 2] = c;
				triangles[i + 3] = a;
				triangles[i + 4] = c;
				triangles[i + 5] = d;
				
				i += 6;
			}
		}
		
		mesh.vertices = vertices;
		mesh.normals = normals;
		mesh.uv = uvs;
		
		mesh.triangles = triangles;
		
		// This is a lot cheaper for planes than the more flexible RecalculateBounds()
		Vector3 size = new Vector3(unitWidth * unitSpacing, 0f, unitDepth * unitSpacing);
		mesh.bounds = new Bounds(size * .5f, size);
		
		return mesh;
	}
	
	// Rotates a mesh's vertices within the mesh.
	public static void rotateMesh(Mesh mesh, Vector3 rotation)
	{
		Quaternion angle = Quaternion.Euler(rotation.x, rotation.y, rotation.z);
		
		Vector3[] verts = mesh.vertices;
		
		for (int vert = 0; vert < verts.Length; vert++)
		{
			verts[vert] = angle * verts[vert];
		}
		
		mesh.vertices = verts;
	}
	
	public static void translateMesh(Mesh mesh, Vector3 translation)
	{
		Vector3[] verts = mesh.vertices;
		
		for (int vert = 0; vert < verts.Length; vert++)
		{
			verts[vert] += translation;
		}
		
		mesh.vertices = verts;
	}

}
	