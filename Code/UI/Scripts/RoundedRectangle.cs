using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Attach to an empty GameObject to dynamically generate a rounded rectangle mesh.
*/

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]

public class RoundedRectangle : MonoBehaviour
{
	public Vector2int size = Vector2int.one;
	public int cornerRadius;
	public bool topLeft = true;
	public bool topRight = true;
	public bool bottomLeft = true;
	public bool bottomRight = true;
	
	private MeshFilter meshFilter;
	
	void Awake()
	{
		meshFilter = GetComponent<MeshFilter>();
		// Make sure we get a fresh new mesh, since it's possible for the MeshFilter
		// to be still using some other mesh, which messes up that mesh by modifying it.
		meshFilter.sharedMesh = new Mesh();
		
		createMesh();
	}
	
	void Update()
	{
		if (Application.isPlaying)
		{
			// Awake creates the mesh, then we don't need this anymore at runtime.
			enabled = false;
		}
		else
		{
			createMesh();
		}
	}
	
	private void createMesh()
	{
		if (cornerRadius < 0)
		{
			cornerRadius = 0;
		}
				
		if (size.x < cornerRadius * 2)
		{
			size.x = cornerRadius * 2;
		}
		
		if (size.y < cornerRadius * 2)
		{
			size.y = cornerRadius * 2;
		}

		if (meshFilter == null)
		{
			Debug.LogWarning("No MeshFilter is assigned to the inspector.", gameObject);
			return;
		}

		List<Vector3> vertices = new List<Vector3>();
		List<Vector2> uvs = new List<Vector2>();

		Vector2int innerSize = new Vector2int(size.x - cornerRadius * 2, size.y - cornerRadius * 2);

		float halfX = 0.5f * size.x;
		float halfY = 0.5f * size.y;
		float halfInnerX = 0.5f * innerSize.x;
		float halfInnerY = 0.5f * innerSize.y;

		Mesh topMesh = null;
		Mesh bottomMesh = null;
		RoundedCorner topRightMesh = null;
		RoundedCorner bottomRightMesh = null;
		RoundedCorner bottomLeftMesh = null;
		RoundedCorner topLeftMesh = null;

		if (topLeft)
		{
			topLeftMesh = new RoundedCorner(cornerRadius, 270, innerSize);
		}
		if (topRight)
		{
			topRightMesh = new RoundedCorner(cornerRadius, 0, innerSize);
		}
		if (bottomLeft)
		{
			bottomLeftMesh = new RoundedCorner(cornerRadius, 180, innerSize);
		}
		if (bottomRight)
		{
			bottomRightMesh = new RoundedCorner(cornerRadius, 90, innerSize);
		}
		
		if (topLeft || topRight)
		{
			// Top section.
			topMesh = createQuad(
				-(topLeft ? halfInnerX : halfX),
				(topRight ? halfInnerX : halfX),
				halfY,
				halfInnerY
			);
		}
		
		if (bottomLeft || bottomRight)
		{
			// Bottom section.
			bottomMesh = createQuad(
				-(bottomLeft ? halfInnerX : halfX),
				(bottomRight ? halfInnerX : halfX),
				-halfInnerY,
				-halfY
			);
		}

		// Horizontal middle section.
		Mesh middleMesh = createQuad(
			-halfX,
			halfX,
			(topLeft || topRight ? halfInnerY : halfY),
			-(bottomLeft || bottomRight ? halfInnerY : halfY)
		);

		List<CombineInstance> combine = new List<CombineInstance>();
		
		addMeshToList(combine, middleMesh);
		addMeshToList(combine, topMesh);
		addMeshToList(combine, bottomMesh);		
		addCornerMeshToList(combine, topLeftMesh);
		addCornerMeshToList(combine, topRightMesh);
		addCornerMeshToList(combine, bottomLeftMesh);
		addCornerMeshToList(combine, bottomRightMesh);
		
		meshFilter.sharedMesh.CombineMeshes(combine.ToArray(), true, false);
				
		// Destroy the temp meshes.
		safeDestroyMesh(middleMesh);
		safeDestroyMesh(topMesh);
		safeDestroyMesh(bottomMesh);
		safeDestroyCornerMesh(topLeftMesh);
		safeDestroyCornerMesh(topRightMesh);
		safeDestroyCornerMesh(bottomLeftMesh);
		safeDestroyCornerMesh(bottomRightMesh);

		// disable all unnecessary stuff for 2D
		MeshRenderer renderer = GetComponent<MeshRenderer>();
		renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
		renderer.receiveShadows = false;
		renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
		renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
	}
	
	private void addMeshToList(List<CombineInstance> list, Mesh mesh)
	{
		if (mesh != null)
		{
			list.Add(createCombineInstance(mesh));
		}
	}

	private void addCornerMeshToList(List<CombineInstance> list, RoundedCorner corner)
	{
		if (corner != null)
		{
			list.Add(createCombineInstance(corner.mesh));
		}
	}
	
	private void safeDestroyCornerMesh(RoundedCorner corner)
	{
		if (corner != null)
		{
			// Use DestroyImmediate since this is also called in edit mode.
 			DestroyImmediate(corner.mesh);
		}
	}

	private void safeDestroyMesh(Mesh mesh)
	{
		if (mesh != null)
		{
			// Use DestroyImmediate since this is also called in edit mode.
			DestroyImmediate(mesh);
		}
	}
	
	// Creates a quad with appropriate UV coords for the overall mesh.
	private Mesh createQuad(float left, float right, float top, float bottom)
	{
		List<Vector3> vertices = new List<Vector3>();
		List<Vector2> uvs = new List<Vector2>();
		List<Vector3> normals = new List<Vector3>();

		addQuadVertex(vertices, uvs, left, top);
		addQuadVertex(vertices, uvs, right, top);
		addQuadVertex(vertices, uvs, left, bottom);
		addQuadVertex(vertices, uvs, right, bottom);		

		for (int i = 0; i < vertices.Count; i++)
		{
			normals.Add(Vector3.back);
		}

		Mesh mesh = new Mesh();
		mesh.vertices = vertices.ToArray();
		mesh.normals = normals.ToArray();
		mesh.uv = uvs.ToArray();
		mesh.triangles = new int[]
		{
			0, 1, 2,
			1, 3, 2
		};
		return mesh;
	}
	
	// Adds a vertex and sets the UV's for the middle section.
	private void addQuadVertex(List<Vector3> vertices, List<Vector2> uvs, float x, float y)
	{
		vertices.Add(new Vector3(x, y, 0));

		float u = x / size.x + 0.5f;
		float v = y / size.y + 0.5f;
		uvs.Add(new Vector2(u, v));
	}

	// Creates a CombineInstance from the given MeshFilter, and returns it to be put into the collection to combine.
	private static CombineInstance createCombineInstance(Mesh mesh)
	{
		CombineInstance inst = new CombineInstance();
		inst.mesh = mesh;
		return inst;
	}

	// Simple class to hold vertex info for one rounded corner.
	public class RoundedCorner
	{
		// Vertex 0 is the one at the pivot point of the arc.
		// Vertex 1 is the first arc point that is left-most if going clockwise.
		// Defaults to upper-right corner for 0 rotation, lower-right for 90, etc.
		public List<Vector3> vertices = new List<Vector3>();
		public List<int> triangles = new List<int>();
		public List<Vector3> normals = new List<Vector3>();
		public List<Vector2> uvs = new List<Vector2>();
		
		public Mesh mesh;
		
		public RoundedCorner(float radius, int rotation, Vector2int innerSize)
		{
			int triangleDensity = Mathf.Max(2, Mathf.CeilToInt(radius / 4.0f));

			vertices.Add(Vector3.zero);
			normals.Add(Vector3.back);
			
			float degreesPerTriangle = 90.0f / triangleDensity;
			float s;
			float c;
			float degrees;
			Vector2 rotated = Vector2.zero;
			
			// Make a fan of triangles.
			for (int i = 0; i <= triangleDensity; i++)
			{
				degrees = degreesPerTriangle * i;
				
				s = Mathf.Sin(CommonMath.degreesToRadians(degrees));
				c = Mathf.Cos(CommonMath.degreesToRadians(degrees));
				
				vertices.Add(new Vector3(s * radius, c * radius, 0.0f));
				// All normals face the viewer (negative z).
				normals.Add(Vector3.back);
				
				if (i > 0)
				{
					triangles.AddRange(new int[] { 0, i, i + 1 });
				}
			}
			
			float halfX = 0.5f * innerSize.x;
			float halfY = 0.5f * innerSize.y;
			
			// Offset and rotate the vertices now.
			for (int i = 0; i < vertices.Count; i++)
			{
				switch (rotation)
				{
				case 0:
						rotated = CommonMath.rotatePointAroundPoint(new Vector2(halfX, halfY), vertices[i], 0, Vector2.one);
						break;
					case 90:
						rotated = CommonMath.rotatePointAroundPoint(new Vector2(halfX, -halfY), vertices[i], 90, Vector2.one);
						break;
					case 180:
						rotated = CommonMath.rotatePointAroundPoint(new Vector2(-halfX, -halfY), vertices[i], 180, Vector2.one);
						break;
					case 270:
						rotated = CommonMath.rotatePointAroundPoint(new Vector2(-halfX, halfY), vertices[i], 270, Vector2.one);
						break;
				}
				vertices[i] = new Vector3(rotated.x, rotated.y, 0.0f);
				
				float u = rotated.x / (halfX + radius) * 0.5f + 0.5f;
				float v = rotated.y / (halfY + radius) * 0.5f + 0.5f;
				
				uvs.Add(new Vector2(u, v));
			}

			mesh = new Mesh();
			mesh.vertices = vertices.ToArray();
			mesh.normals = normals.ToArray();
			mesh.triangles = triangles.ToArray();
			mesh.uv = uvs.ToArray();
		}
	}
	
	void OnDestroy()
	{
		DestroyImmediate(meshFilter.sharedMesh);	// Using DestroyImmediate() so it also works in edit mode.
	}
}
