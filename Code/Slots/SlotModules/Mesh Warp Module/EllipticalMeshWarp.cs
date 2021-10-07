using UnityEngine;
using System.Collections;

public class EllipticalMeshWarp : MonoBehaviour
{
	public float a_left = 0f;
	public float b_left = 20f;
	public float xbound_left = -10f;
	public float xoffset_left = 0f;

	public float a_right = 0f;
	public float b_right = 20f;
	public float xbound_right = 10f;
	public float xoffset_right = 0f;

	public float world_y_center = 0;

	public int newSubdivisions = 0;

	public bool subdivide = false; // for editor: see how an extra subdivision would affect visual quality
	public bool alwaysUpdate = false; // for editor: see effects of parameter chagnes when position doesn't change

#if UNITY_EDITOR
	private MeshFilter warpMeshFilter;
	private Mesh warpMesh;
	private Vector3[] originalVerts;
#endif
	private Vector3 lastWorldPos = new Vector3(float.NaN, float.NaN, float.NaN);

	private MeshRenderer warpMeshRenderer;
	private MaterialPropertyBlock materialProperty;
	private bool initialized;

	void Awake()
	{
		warpMeshRenderer = GetComponent<MeshRenderer>();

#if UNITY_EDITOR
		warpMeshFilter = GetComponent<MeshFilter>();
		if (warpMeshFilter == null || warpMeshRenderer == null)
		{
			Debug.LogError("No MeshFilter or MeshRenderer found. Removing EllipticalMeshWarp script", gameObject);
			Destroy(this);
			return;
		}
		warpMesh = warpMeshFilter.sharedMesh;
		originalVerts = warpMesh.vertices;
#endif

		materialProperty = new MaterialPropertyBlock();
		initialized = true;
	}

	void OnEnable()
	{
		if (!initialized)
		{
			return;
		}

		warp();
	}

	void Update()
	{
		if (!initialized)
		{
			return;
		}

		if (subdivide)
		{
			subdivide = false;
			subdivideMesh();
		}

		while (newSubdivisions > 0 && newSubdivisions < 5)
		{
			subdivideMesh();
			newSubdivisions--;
		}

		warp();
	}

	void warp()
	{
		Vector3 worldPos = transform.position;
		if (lastWorldPos == worldPos && !alwaysUpdate)
		{
			return;
		}
		lastWorldPos = worldPos;

		float world_xbound_left = worldPos.x + xbound_left;
		float world_xbound_right = worldPos.x + xbound_right;

		// set parameters for shader

		warpMeshRenderer.GetPropertyBlock(materialProperty);

		materialProperty.SetVector("_Warp_Parameters_Left", new Vector4(a_left, b_left, world_xbound_left, xoffset_left));
		materialProperty.SetVector("_Warp_Parameters_Right", new Vector4(a_right, b_right, world_xbound_right, xoffset_right));
		materialProperty.SetFloat("_Warp_World_Y_Center", world_y_center);

		warpMeshRenderer.SetPropertyBlock(materialProperty);

		/* CPU version, for reference
		   now implemented in SymbolWarp.shader and SymbolWarpAdditive.shader
		   
		float y;
		float x_left;
		float x_right;
		Vector3 worldPoint;

		Matrix4x4 localToWorld = transform.localToWorldMatrix;
		Matrix4x4 worldToLocal = transform.worldToLocalMatrix;

		// For each vert, calculate the left and right side ellipse curves for a given world space y height,
		// lerp the world space x value between those, then store local space vert position in warpMesh.
		for (int i = 0; i < originalVerts.Length; i++)
		{
			worldPoint = localToWorld.MultiplyPoint3x4(originalVerts[i]);

			y = worldPoint.y - world_y_center;
			x_left = -a_left * Mathf.Sqrt(1 - (y / b_left) * (y / b_left)) + world_xbound_left + a_left + xoffset_left;
			x_right = a_right * Mathf.Sqrt(1 - (y / b_right) * (y / b_right)) + world_xbound_right - a_right + xoffset_right;
			float interp = (worldPoint.x - world_xbound_left) / (world_xbound_right - world_xbound_left);
			worldPoint.x = Mathf.LerpUnclamped(x_left, x_right, interp);

			warpedVerts[i] = worldToLocal.MultiplyPoint3x4(worldPoint);
		}

		warpMesh.vertices = warpedVerts;
		
		*/
	}

	public void forceWarp()
	{
		if (!initialized)
		{
			return;
		}

		lastWorldPos = new Vector3(float.NaN, float.NaN, float.NaN);
		warp();
	}

	void subdivideMesh()
	{
#if UNITY_EDITOR
		warpMesh.vertices = originalVerts;
		MeshHelper.Subdivide4(warpMesh);
		originalVerts = warpMesh.vertices;
		warpMeshFilter.mesh = warpMesh;
#endif
	}
}
