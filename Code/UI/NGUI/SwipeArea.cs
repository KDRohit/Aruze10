using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Attach to a collider that is an area that can be swiped for paging, so that the source of the swipe area can be detected,
so that we can only do the swipe action on the thing being swiped for if there is multiple swipe areas on screen at once.
*/

public class SwipeArea : TICoroutineMonoBehaviour
{
	public Vector2 size = Vector2.one;
	public Vector2 center = Vector2.zero;
	
	/// Allows overriding the default swipe threshold to trigger a swipe on this swipe area.
	/// Useful for small swipe areas like slider buttons.
	/// 0 means use the default from TouchInput.
	public int swipeThreshold = 0;

	private void Start()
	{
		TouchInput.allSwipeAreas.Add(this);
	}

	private void OnDestroy()
	{
		TouchInput.allSwipeAreas.Remove(this);
	}

	/// Returns the area as a Rect that is scaled properly for the screen.
	public virtual Rect getScreenRect()
	{
		Camera cam = NGUIExt.getObjectCamera(gameObject);

		Vector3 topLeftWorld = transform.TransformPoint(new Vector3(center.x - size.x / 2, center.y + size.y / 2, 0));
		Vector3 bottomRightWorld = transform.TransformPoint(new Vector3(center.x + size.x / 2, center.y - size.y / 2, 0));
		
		Vector2int topLeft = NGUIExt.screenPositionOfWorld(cam, topLeftWorld);
		Vector2int bottomRight = NGUIExt.screenPositionOfWorld(cam, bottomRightWorld);

		float x = topLeft.x;
		float y = bottomRight.y;
		float w = bottomRight.x - topLeft.x;
		float h = topLeft.y - bottomRight.y;
				
		return new Rect(x, y, w, h);
	}
	
	protected virtual void OnDrawGizmosSelected()
	{
		// Draw bounds with a proper 3D transform
		Gizmos.color = Color.yellow;
		Gizmos.matrix = transform.localToWorldMatrix;
		Gizmos.DrawWireCube(center, size);
	}
}
