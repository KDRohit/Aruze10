using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Stretches a capsule height to match a sprite's size in a given direction.
*/

[ExecuteInEditMode]

public class CapsuleHeightStretcher : TICoroutineMonoBehaviour
{
	public enum Dimension
	{
		X,
		Y
	}
	
	public CapsuleCollider capsule;
	public UISprite targetSprite;
	public Dimension spriteDimension;
	public int pixelOffset = 0;
		
	void Update()
	{
		if (targetSprite == null || capsule == null)
		{
			return;
		}
		
		float size = targetSprite.transform.localScale.x;
		
		if (spriteDimension == Dimension.Y)
		{
			size = targetSprite.transform.localScale.y;
		}
		
		capsule.height = size + pixelOffset;
	}
}
