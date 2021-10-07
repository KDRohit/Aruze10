using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Attach to an empty GameObject to create a quad and a controller transform that
allows you to move and rotate the controller to manipulate the UVs on the quad,
to allow positioning and rotation of the texture within the quad.
*/

[ExecuteInEditMode]
public class UVManipulator : TICoroutineMonoBehaviour
{
	public Transform controlTransform;
	public Vector2 uvCenter = new Vector2(0.5f, 0.5f);
	// "button" to reset controller center
	public bool recenterController = false;
	private bool lastRecenterController = false;

	public bool scaleWithController = false;

	public bool rectangleFriendly = false;
	private bool lastRectangleFriendly = false;

	private MeshFilter meshFilter = null;
	private Mesh mesh = null;

	private Vector2[] uvs = new Vector2[4];

	private MeshRenderer meshRenderer = null;
	private Bounds meshRendererBounds;
	
	private Vector2 lastPosition = Vector2.zero;
	private Vector2 lastScale = Vector2.one;
	private Vector2 lastUvCenter = Vector2.zero;
	private Vector2 lastParentScale = Vector2.one;
	private float lastAngle = -9999.0f;

	private float left = 0.0f;
	private float right = 0.0f;
	private float bottom = 0.0f;
	private float top = 0.0f;

	// Needed for capturing UV and bounds size values of original mesh
	Vector2[] meshFilterUVs = new Vector2[4];
	Vector3 meshRenderBoundsSize = Vector3.zero;
	[HideInInspector]
	public bool meshBoundsChecked = false;

	public void Awake()
	{
		// While in the editor and the game isn't running we will call Initialize in the Update loop
		if (Application.isPlaying)
		{	
			Initialize();
		}

		// We call update once in order to ensure the graphics are correctly displayed once the object is awake.
		Update();
	}

#if UNITY_EDITOR
	protected virtual void OnDrawGizmosSelected()
	{
		// Render all the mesh-renderers in Red...
		Gizmos.color = Color.red;
		MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>(false);  //or meshRenderer for world bounds
		for (int i = 0; i < meshFilters.Length; i++)
		{
			MeshFilter meshFilter = meshFilters[i];
			Gizmos.matrix = meshFilter.transform.localToWorldMatrix;
			Bounds bounds = meshFilter.sharedMesh.bounds; // in local space
			Gizmos.DrawWireCube(bounds.center, bounds.size);
		}
    }
#endif

	protected void Initialize()
	{
		if (meshFilter == null)
		{
			meshFilter = gameObject.GetComponent<MeshFilter>();
			if (meshFilter == null)
			{
				meshFilter = gameObject.AddComponent<MeshFilter>();
			}
		}

		if (GetComponent<Renderer>() == null)
		{
			meshRenderer = gameObject.AddComponent<MeshRenderer>();
		}
		else
		{
			meshRenderer = gameObject.GetComponent<MeshRenderer>();
		}

		// applying the manipulator for the first time
		if (!meshBoundsChecked && controlTransform == null)
		{
			// TODO - apply the original mesh UVs to the new mesh

			rectangleFriendly = true;

			// Mesh scale factor of existing mesh
			meshRenderBoundsSize = meshRenderer.bounds.size;
			gameObject.transform.localScale = new Vector3(meshRenderBoundsSize.x, meshRenderBoundsSize.y, meshRenderBoundsSize.z);

			meshBoundsChecked = true;
		}

		if (mesh == null)
		{
			// Dynamically create a mesh.
			mesh = CommonGeometry.createPlane(1, 1);
			CommonGeometry.rotateMesh(mesh, new Vector3(-90.0f, 0.0f, 0.0f));
			CommonGeometry.translateMesh(mesh, new Vector3(-0.5f, -0.5f, 0.0f));
			mesh.RecalculateBounds();
			meshFilter.mesh = mesh;
		}
		
		if (controlTransform == null)
		{
			controlTransform = new GameObject().transform;
			controlTransform.gameObject.name = gameObject.name + "_Controller";
			controlTransform.parent = transform;
			controlTransform.localPosition = Vector3.zero;
			controlTransform.localScale = Vector3.one;
		}

		// Lock X and Y rotation and Z position of Controller
		controlTransform.rotation = Quaternion.Euler(0.0f, 0.0f, controlTransform.rotation.eulerAngles.z);
		Vector3 localPosition = controlTransform.localPosition;
		if (localPosition.z != 1.0f)
		{
			localPosition.z = 1.0f;
			controlTransform.localPosition = localPosition;
		}

		// if recenter is clicked
#if UNITY_EDITOR
		if (recenterController != lastRecenterController)
		{
			uvCenter.x = (meshFilter.sharedMesh.uv[0].x + meshFilter.sharedMesh.uv[1].x) / 2;
			uvCenter.y = (meshFilter.sharedMesh.uv[0].y + meshFilter.sharedMesh.uv[2].y) / 2;

			CommonTransform.setX(controlTransform, 0.0f);
			CommonTransform.setY(controlTransform, 0.0f);
			localPosition = controlTransform.localPosition;

			recenterController = false;
			lastRecenterController = false;
		}
#endif
	}

	public void Update()
	{
#if UNITY_EDITOR
		if (!Application.isPlaying)
		{
			Initialize();
		}
#endif
		
		Vector3 localPosition = controlTransform.localPosition;
		float x = localPosition.x;
		float y = localPosition.y;
		Vector2 scale = controlTransform.localScale;
		Vector2 parentScale = controlTransform.parent.localScale;
		float angle = controlTransform.localEulerAngles.z;

		// Check for changes
		if (lastPosition.x != x ||
		    lastPosition.y != y ||
		    lastScale.x != scale.x ||
		    lastScale.y != scale.y ||
		    lastUvCenter.x != uvCenter.x ||
		    lastUvCenter.y != uvCenter.y ||
		    lastAngle != angle ||
		    lastParentScale != parentScale ||
		    lastRectangleFriendly != rectangleFriendly)
		{
			// The Quad's UV's are arranged as such:
			// 0 = lower left
			// 1 = lower right
			// 2 = upper left
			// 3 = upper right
			if (rectangleFriendly)
			{
				// shifting center and corners of quad by parent scale
				left = -1 * (x * parentScale.x) - (0.5f * parentScale.x);
				right = -1 * (x * parentScale.x) + (0.5f * parentScale.x);
				bottom = -1 * (y * parentScale.y) - (0.5f * parentScale.y);
				top = -1 * (y * parentScale.y) + (0.5f * parentScale.y);

				if (scaleWithController)
				{
					Vector2 scalePercentage = new Vector2((lastScale.x / scale.x), (lastScale.y / scale.y));
					Vector3 scaleUpParent = new Vector3((parentScale.x / scalePercentage[0]), (parentScale.y / scalePercentage[1]), gameObject.transform.localScale.z);
					gameObject.transform.localScale = scaleUpParent;
				}
			}
			else
			{
				left = -x - 0.5f;
				right = -x + 0.5f;
				bottom = -y - 0.5f;
				top = -y + 0.5f;
			}
			
			uvs[0] = CommonMath.rotatePointAroundPoint(uvCenter, new Vector2(left, bottom), angle, scale);
			uvs[1] = CommonMath.rotatePointAroundPoint(uvCenter, new Vector2(right, bottom), angle, scale);
			uvs[2] = CommonMath.rotatePointAroundPoint(uvCenter, new Vector2(left, top), angle, scale);
			uvs[3] = CommonMath.rotatePointAroundPoint(uvCenter, new Vector2(right, top), angle, scale);

			mesh.uv = uvs;
			
			// Update changes
			lastPosition.x = x;
			lastPosition.y = y;
			lastScale.x = scale.x;
			lastScale.y = scale.y;
			lastUvCenter.x = uvCenter.x;
			lastUvCenter.y = uvCenter.y;
			lastAngle = angle;
			lastParentScale = parentScale;
			lastRectangleFriendly = rectangleFriendly;
		}
	}
}
