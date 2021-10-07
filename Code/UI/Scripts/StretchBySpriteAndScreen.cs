using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Stretches a UISprite's width based on the effective pixel width of the screen and another sprite's width.
*/

[ExecuteInEditMode]
public class StretchBySpriteAndScreen : MonoBehaviour
{
	public UISprite stretchSprite;
	public UISprite offsetSprite;
	public int pixelOffset = 0;
	
	void Update()
	{
		if (stretchSprite == null || offsetSprite == null)
		{
			return;
		}
		float screenWidth = NGUIExt.effectiveScreenWidth;
		Debug.LogWarning("screenWidth: " + screenWidth + " - " + offsetSprite.transform.localScale.x + " + " + pixelOffset);
		float newWidth = screenWidth - offsetSprite.transform.localScale.x + pixelOffset;
		
		CommonTransform.setWidth(stretchSprite.transform, newWidth);
	}
}
