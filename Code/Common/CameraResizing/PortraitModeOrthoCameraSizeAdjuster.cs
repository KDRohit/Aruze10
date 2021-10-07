using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
Class for handling cameras that want to resize when the game switches to and from portrait mode
which aren't being managed by a ReelGame.  This should allow for cameras to adjust more quickly.
For instance if you have a transition over the top of a game to hide the aspect ratio change
you can have that camera update more quickly than the game (which has to wait a number of frames).

Original Author: Scott Lepthien
Creaiton Date: 10/15/2018
*/
[ExecuteInEditMode]
public class PortraitModeOrthoCameraSizeAdjuster : MonoBehaviour 
{
	[SerializeField] private float cameraSizeInLandscape;
	[SerializeField] private Camera targetCamera;
	private float cameraSizeInPortrait;
	
	private void Awake()
	{
		if (targetCamera == null)
		{
			targetCamera = gameObject.GetComponent<Camera>();
			
			if (targetCamera == null && Application.isPlaying)
			{
				Debug.LogWarning("PortraitModeCameraSizeAdjuster.Awake() - Can't determine targetCamera! Destroying script.");
				Destroy(this);
			}
		}
		
		if (!targetCamera.orthographic && Application.isPlaying)
		{
			Debug.LogWarning("PortraitModeCameraSizeAdjuster.Awake() - targetCamera is NOT orthographic! Destroying script.");
			Destroy(this);
		}
	}

	private void Start()
	{
		if (cameraSizeInLandscape > 0.0f)
		{
			calculateCameraSizeInPortrait();
		}
		else if (Application.isPlaying)
		{
			Debug.LogWarning("PortraitModeCameraSizeAdjuster.Awake() - cameraSizeInLandscape wasn't set! Destroying script.");
			Destroy(this);
		}
	}

	private void calculateCameraSizeInPortrait()
	{
		if (ResolutionChangeHandler.isInPortraitMode)
		{
			cameraSizeInPortrait = cameraSizeInLandscape * (UnityEngine.Screen.height / (float)UnityEngine.Screen.width);
		}
		else
		{
			cameraSizeInPortrait = cameraSizeInLandscape * (UnityEngine.Screen.width / (float)UnityEngine.Screen.height);
		}
	}

	private void Update()
	{
		if (targetCamera != null && cameraSizeInLandscape > 0.0f)
		{
			if (ResolutionChangeHandler.isInPortraitMode)
			{
				if (!Application.isPlaying)
				{
					calculateCameraSizeInPortrait();
				}
			
				if (targetCamera.orthographicSize != cameraSizeInPortrait)
				{
					targetCamera.orthographicSize = cameraSizeInPortrait;
				}
			}
			else
			{
				if (targetCamera.orthographicSize != cameraSizeInLandscape)
				{
					targetCamera.orthographicSize = cameraSizeInLandscape;
				}
			}
		}
	}
}
