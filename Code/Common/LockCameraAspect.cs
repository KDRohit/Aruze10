using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Script to force an aspect ratio on a camera.  Could be useful if you don't want a camera aspect to change when
 * the target resolution changes, for instance if you are using the camera to generate a RenderTexture.
 *
 * Original Author: Scott Lepthien
 * Creation Date: 11/5/2020
 */
[ExecuteInEditMode]
public class LockCameraAspect : MonoBehaviour
{
	[SerializeField] private Camera targetCamera;
	[SerializeField] private float lockedAspect;
	private bool isCameraModified = false;
	
	void Awake()
	{
		if (targetCamera != null && lockedAspect > 0)
		{
			applyAspectToCamera();
		}
	}
	
	void Update()
	{
		if (targetCamera != null)
		{
			if (isCameraModified && lockedAspect <= 0)
			{
				// reset the camera
				targetCamera.ResetAspect();
				isCameraModified = false;
			}
			else if (lockedAspect > 0 && !Mathf.Approximately(targetCamera.aspect, lockedAspect))
			{
				applyAspectToCamera();
			}
		}
	}

	private void applyAspectToCamera()
	{
		targetCamera.aspect = lockedAspect;
		isCameraModified = true;
	}
}
