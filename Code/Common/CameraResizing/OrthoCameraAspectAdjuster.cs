using UnityEngine;
using System.Collections;

/*
Attach this component to an orthographic camera to have it scale properly based on the original device resolution
 */ 
[RequireComponent(typeof(Camera))]
public class OrthoCameraAspectAdjuster : MonoBehaviour {
	Vector3 baseScale = Vector3.one;
	public bool adjustX = true;
	public bool adjustY = true;

	void Awake()
	{
		baseScale = this.gameObject.transform.localScale;
	}

	void Update()
	{
		// Perform calculation based upon aspect ratio, resizing the screen appropriately
		Camera targetCamera = this.gameObject.GetComponent<Camera>();
		float baseAspectRatio = 4.0f / 3.0f;
		if (targetCamera.aspect < baseAspectRatio)
		{
			float adjustmentMultiplier = targetCamera.aspect / baseAspectRatio;
			float xAdjustment = adjustX ? adjustmentMultiplier : 1.0f;
			float yAdjustment = adjustY ? adjustmentMultiplier : 1.0f;
			this.gameObject.transform.localScale = new Vector3(
				baseScale.x * xAdjustment,
				baseScale.y * yAdjustment,
				baseScale.z
				);
			//Game seems to scale just fine for now if it is at a greater aspect ratio than base, we just need to have a background showing.
		}
		else
		{
			this.gameObject.transform.localScale = baseScale;
		}

		// Don't destroy, so we can re-enable if resolution changes.
		enabled = false;
	}
}
