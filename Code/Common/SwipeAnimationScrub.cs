using UnityEngine;
using System.Collections;

// Class that handles scrubbing through a timeline animation as the user swipes on the game object
// To see a relevant implementation of this, review the LobbyCarouselCard, and LobbyCarouselV3 classes
// for the delegates used to handle this
public class SwipeAnimationScrub : MonoBehaviour
{
	// =============================
	// PRIVATE
	// =============================
	private Vector2int origin; // start position where user first touches
	private bool isTouching; // set to true in relation to TouchInput.isDragging
	private float previousDragDistance = 0f;
	[SerializeField] private SwipeArea swipeArea;
	[SerializeField] private GameObject target;

	// adjustment percentage that is used to calculate the amount distance needed to travel before performing a swipe
	[SerializeField] private float swipeThreshold = 0.1f;

	// =============================
	// PUBLIC
	// =============================
	public delegate void onSwipeDelegate();
	public delegate void onSwipeUpdateDelegate(float delta);

	public event onSwipeDelegate onSwipeLeft;
	public event onSwipeDelegate onSwipeRight;
	public event onSwipeDelegate onSwipeLimitMet; // called when we are 75% through the swipe area movement
	public event onSwipeUpdateDelegate onSwipeReset; // this is called if the user hasn't moved far enough to actually trigger a swipe
	public event onSwipeUpdateDelegate onSwipeUpdate;

	private bool swipeEnabled = true;
	
	void Awake()
	{
		if (swipeArea == null)
		{
			// attempt to grab the bounds by using an available collider
			if (target.GetComponent<Collider>() != null)
			{
				Bounds b = target.GetComponent<Collider>().bounds;

				swipeArea = target.AddComponent<SwipeArea>();
				swipeArea.size = new Vector2(b.size.x, b.size.y);
			}
		}
	}

	void Update()
	{
		float dragDistance = TouchInput.dragDistanceX; // can be negative or positive
		float dragDelta = Mathf.Abs(previousDragDistance/swipeArea.size.x);
		if (!swipeEnabled) { return; }
		if (TouchInput.isDragging && TouchInput.swipeArea == swipeArea)
		{
			if (!isTouching)
			{
				origin = TouchInput.position;
				isTouching = true;
			}

			previousDragDistance = dragDistance;

			if (onSwipeUpdate != null)
			{
				onSwipeUpdate(dragDistance/swipeArea.size.x);
			}

			// check if the animation is going to complete
			if (onSwipeLimitMet != null)
			{
				if (Mathf.Abs(previousDragDistance/swipeArea.size.x) >= 0.75 || Dialog.instance.isShowing)
				{
					onSwipeLimitMet();
				}
			}
		}
		else if (isTouching && !TouchInput.isTouchDown)
		{
			isTouching = false;
			if (dragDelta >= swipeThreshold)
			{
				swipe(previousDragDistance > 0);
			}
			else if (onSwipeReset != null)
			{
				onSwipeReset(dragDelta);
			}
		}
	}

	public void reset()
	{
		isTouching = false;
		TouchInput.isTouchDown = false;
		TouchInput.isDragging = false;
	}

	public void setScrollerActive(bool active)
	{
		swipeEnabled = active;
	}

	private void swipe(bool swipeRight = true)
	{
		if (swipeRight && onSwipeRight != null)
		{
			onSwipeRight();
		}
		else if (onSwipeLeft != null)
		{
			onSwipeLeft();
		}
	}
}
