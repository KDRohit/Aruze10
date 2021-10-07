using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Oz03ScarecrowPickemItemGroup : TICoroutineMonoBehaviour
{
	public UISprite[] sprites;
	public UISprite[] shadowSprites;
	public Animator[] itemAnimators = null;
	
	// Starts the marching animation for all of the items.
	public void startMarchAnimation()
	{
		foreach (Animator anim in itemAnimators)
		{
			anim.CrossFade("Item March", .1f, -1, 0f);
		}
	}
	
	// Stops the marching animation and fades all items back to normal position.
	public IEnumerator stopMarchAnimation(float delay, bool changeDepth)
	{
		yield return new WaitForSeconds(delay);
		foreach (Animator anim in itemAnimators)
		{
			anim.CrossFade("Item Idle", .1f, -1, 0f);
		}
		
		if (changeDepth)
		{
			// It's time to change the depth of the shadows so they're in front of the bend in the road.
			setShadowDepth(10);
		}
	}
	
	// Sets the sprite depth for all shadow sprites.
	public void setShadowDepth(int depth)
	{
		foreach (UISprite shadow in shadowSprites)
		{
			shadow.depth = depth;
		}
	}
}
