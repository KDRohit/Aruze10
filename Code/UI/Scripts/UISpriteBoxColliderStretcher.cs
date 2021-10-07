using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/*
Stretches a box collider that's on a different GameObject so that it matches the size of a given UISprite.
This is useful when a parent object is the "button" and the background sprite of the button may change size
depending on aspect ratio or whatever, and you want the button collider to follow suit.
*/

[ExecuteInEditMode]

public class UISpriteBoxColliderStretcher : MonoBehaviour
{
	public UISprite sprite;
	new public BoxCollider collider;
	
	private float lastWidth = 0.0f;
	private float lastHeight = 0.0f;
		
	void Update()
	{
		if (sprite == null || collider == null)
		{
			return;
		}
		
		float width = sprite.transform.localScale.x;
		float height = sprite.transform.localScale.y;
		
		if (lastWidth != width || lastHeight != height)
		{
			collider.size = new Vector3(width, height, 0.0f);
			
			lastWidth = width;
			lastHeight = height;
		}
	}
}
