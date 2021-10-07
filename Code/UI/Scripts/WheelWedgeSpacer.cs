using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Helper tool to evenly distribute the spacing between wedges on a wheel.
Attach to a parent object with all the wedges as children for it to immediately happen,
or click the "Space Now" checkbox to trigger it.
Note: This assumes that each wedge has a parent object that is positioned at 0 x and 0 y.
*/

[ExecuteInEditMode]
public class WheelWedgeSpacer : MonoBehaviour
{
	public bool spaceNow = false;
	
	void Awake()
	{
		if (Application.isPlaying)
		{
			// We only use this at edit time.
			enabled = false;
		}
		else
		{
			doSpacing();
		}
	}
	
	void Update()
	{
		
		if (spaceNow)
		{
			doSpacing();
		}
	}
	
	private void doSpacing()
	{
		for (int i = 0; i < transform.childCount; i++)
		{
			transform.GetChild(i).localEulerAngles = new Vector3(0, 0, -360.0f / transform.childCount * i);
		}
		
		spaceNow = false;
	}
}
