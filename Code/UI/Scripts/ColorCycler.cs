using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Attach this to an object with a "Special HSV" shader on its renderer to cycle the colors on it.
*/

public class ColorCycler : TICoroutineMonoBehaviour
{
	public float speed;
	
	private float _cycle = 0;
	
	void Update()
	{
		if (GetComponent<Renderer>() != null)
		{
			GetComponent<Renderer>().sharedMaterial.SetFloat("_Hue", Mathf.Repeat(_cycle, 1f));
			_cycle += Time.deltaTime * speed;
		}
	}
}
