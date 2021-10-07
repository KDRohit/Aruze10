using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Attach this to an object with a "Unlit/GUI Texture" shader on its renderer to cycle the colors on it.
*/

public class ColorCyclerUnlitGuiTexture : TICoroutineMonoBehaviour
{
	public float speed;
	public Color[] colors;
	
	private float _cycle = 0;
	private int currentColorIndex = 0;
	private int nextColorIndex = 1;
	
	void Update()
	{
		if (GetComponent<Renderer>() != null)
		{
			_cycle += Time.deltaTime * speed;
			if(_cycle > 1f)
			{
				currentColorIndex = (currentColorIndex + 1) % (colors.Length);
				nextColorIndex = (currentColorIndex + 1) % (colors.Length);
				_cycle -= 1f;
			}
			GetComponent<Renderer>().material.color = Color.Lerp(colors[currentColorIndex], colors[nextColorIndex], _cycle);//new Color(0, 0, Mathf.PingPong(_cycle, 1f));//.sharedMaterial.SetFloat("_Hue", Mathf.Repeat(_cycle, 1f));
		}
	}
}
