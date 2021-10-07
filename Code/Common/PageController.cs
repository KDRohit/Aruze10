using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PageController : MonoBehaviour
{	
	public delegate void pageViewDelegate(GameObject createdObject, int index);

	public event pageViewDelegate onSwipeLeft;
	public event pageViewDelegate onSwipeRight;
	public event pageViewDelegate onClickLeft;
	public event pageViewDelegate onClickRight;
	public event pageViewDelegate onPageViewed;
	public event pageViewDelegate onPageHide;
	public event pageViewDelegate onPageReset;

	public enum SwipeDirection:int
	{
		LEFT = -1,
		RIGHT = 1,
		NONE = 0
	}

	public bool isEnabled = true; // Used for disabling the flow.
	
	// Inspector Values
	[SerializeField] private SwipeArea swipeArea;
	[SerializeField] private Transform content;
	[SerializeField] private float swipeThreshold = 50f;
	[SerializeField] private float tweenTime = 0.5f;
	[SerializeField] private int numBufferPages = 1;
	[SerializeField] private bool hidePageButtons = true;
	[SerializeField] private ClickHandler leftButton;
	[SerializeField] private ClickHandler rightButton;
	[SerializeField] private PaginationController paginator;
	[SerializeField] private bool usePageCache = false;
	
    public float pageWidth = 1000f;
	
	// For handling page creation/management.
	private List<Transform> pageBuffer;
	private int currentIndex = 0;
	private int size = 0; // number of "pages"
	private pageViewDelegate onPageCreated;
	private Transform centerPage;
	private List<Vector3> pagePositions;
	private GameObject pagePrefab;
	private int pendingPage = -1; // once page controller is ready, go to this page. set to -1 to turn off
	private bool scrollEnabled = true;

	// For handling movement.
	protected SwipeDirection swipeDirection;
	private bool isTouching = false;
	private bool isPastThreshold = false;
	private float previousDragDistance = 0;
	private Vector2int touchPosition;

	private bool hasInitialized = false;

	private GameObjectCacher pageObjectCacher = null;

	public void init(GameObject pagePrefab, int size, pageViewDelegate onPageCreated, int startingPage = 0)
	{
		if (hasInitialized)
		{
			reset();
		}
		else if (usePageCache)
		{
			pageObjectCacher = new GameObjectCacher(gameObject, pagePrefab);
		}

		hasInitialized = true;
		this.size = size;
		this.currentIndex = startingPage;
		this.onPageCreated = onPageCreated;
		this.pagePrefab = pagePrefab;

		// Create the page buffer, working out from the center.
		if (pageBuffer == null)
		{
			pageBuffer = new List<Transform>();
		}
		pageBuffer.Clear();

		if (pagePositions == null)
		{
			pagePositions = new List<Vector3>();
		}
		pagePositions.Clear();

		Transform center = createObject(0, startingPage);

		pageBuffer.Add(center);
		pagePositions.Add(Vector3.zero);

		for (int i = 0; i < numBufferPages; i++)
		{
			int adjustedIndex = i + 1; // Adding one since this is zero indexed but we have a center.
			Transform left = createObject(-adjustedIndex * pageWidth, startingPage - adjustedIndex);
			Transform right = createObject(adjustedIndex * pageWidth, startingPage + adjustedIndex);
			// Create the buffered pages.
			pageBuffer.Insert(0, left);
			pageBuffer.Add(right);
			// Record their positions for tweening targets later.
			pagePositions.Insert(0, left.localPosition);
			pagePositions.Add(right.localPosition);
		}

		if (leftButton != null)
		{
			leftButton.registerEventDelegate(leftClicked);
		}

		if (rightButton != null)
		{
			rightButton.registerEventDelegate(rightClicked);
		}

		if (paginator != null)
		{
			paginator.initWithPageController(size, startingPage, this);
		}
		checkButtons();
		if (onPageViewed != null)
		{
			// Only call this if they have setup the viewed event already.
			RoutineRunner.instance.StartCoroutine(initRoutine(center, currentIndex));
		}
		else if (pendingPage >= 0)
		{
			goToPage(pendingPage);
		}
	}

	public void goToPage(int page, pageViewDelegate onArriveAtPage = null)
	{
		if (onArriveAtPage != null)
		{
			onPageCreated += onArriveAtPage;
		}

		pendingPage = -1;
		init(pagePrefab, size, onPageCreated, page);
		
	}

	public void goToPageAfterInit(int page)
	{
		pendingPage = page;
	}

	public void removeGoToPageCallback(pageViewDelegate callback)
	{
		if (callback != null)
		{
			onPageCreated -= callback;
		}
	}

	public void setScrollerActive(bool active)
	{
		scrollEnabled = active;
	}

	public virtual void Update()
	{
		if (!isEnabled || !hasInitialized)
		{
			return;
		}
		
		if (TouchInput.isDragging && (swipeArea == null || TouchInput.swipeArea == swipeArea) && scrollEnabled)
		{
			// If we are touching the swipe area.
			if (!isTouching)
			{
				touchPosition = TouchInput.position; // Used for returning it to its previous position.
				isTouching = true; // Finger down.
			}
			float dragDistance = TouchInput.dragDistanceX;

			// Check edge cases
			if ((currentIndex <= 0 && dragDistance > 0) ||
				(currentIndex >= size -1 && dragDistance < 0))
			{
				// If we are dragging to the left but are at the left-most,
				// or if we are dragging to the right but are at the right-most, do nothing.
				isPastThreshold = false;
				swipeDirection = SwipeDirection.NONE;
				return;
			}

			// Move the content by that amount so that it follows the finger.
			for (int i = 0; i < pageBuffer.Count; i++)
			{
				float dragDelta = dragDistance - previousDragDistance;
				CommonTransform.addX(pageBuffer[i].transform, dragDelta);
			}

			if (Mathf.Abs(dragDistance) > swipeThreshold)
			{
				swipeDirection = (dragDistance > 0) ? SwipeDirection.RIGHT : SwipeDirection.LEFT;
			}
			else
			{
				swipeDirection = SwipeDirection.NONE;
			}
			previousDragDistance = TouchInput.dragDistanceX;
		}
		else if (isTouching && !TouchInput.isTouchDown)
		{
			isTouching = false; // Finger up
			previousDragDistance = 0; // Reset drag distance.
			swipe(swipeDirection); // Call swipe, direction.NONE will handle going back to position.
		}
	}

	private void reset()
	{
		CommonGameObject.destroyChildren(content.gameObject);

		if (leftButton != null)
		{
			leftButton.clearAllDelegates();
		}
		if (rightButton != null)
		{
			rightButton.clearAllDelegates();
		}
	}

	private IEnumerator initRoutine(Transform center, int index)
	{
		// Special coroutine that gets called on init because the first object will get created before Awake()
		// and this can cause issues (particularly with animator states).
		yield return null;

		if (center != null)
		{
			onPageViewed(center.gameObject, index);
		}

		if (pendingPage >= 0)
		{
			goToPage(pendingPage);
		}
	}

	private Transform createObject(float position, int index)
	{
		GameObject newPage;
		if (!usePageCache)
		{
			newPage = CommonGameObject.instantiate(pagePrefab, content.transform) as GameObject;
		}
		else
		{
			newPage = pageObjectCacher.getInstance();
		}
		
		newPage.transform.parent = content;
		newPage.transform.localPosition = new Vector3(position, 0, 0);

		if (usePageCache)
		{
			newPage.transform.localScale = Vector3.one;
		}

		if (index >= 0 && index < size)
		{
			if (onPageCreated != null)
			{
				onPageCreated(newPage, index);
			}
			newPage.SetActive(true);
		}
		else
		{
			// If this is out of bounds then it is just a spacer page it is inactive.
			newPage.SetActive(false);
		}
		return newPage.transform;
	}

	protected virtual void movePages(SwipeDirection direction)
	{
		if (hasInitialized)
		{
			// Dont allow people to try to move the page controller
			// if we haven't initialized yet.
			StartCoroutine(moveRoutine(direction));
		}
	}

	protected virtual IEnumerator moveRoutine(SwipeDirection direction)
	{
		if (gameObject == null || pageBuffer == null || pagePositions == null)
		{
			Debug.LogErrorFormat("PageController.cs -- moveRoutine -- something is null that should not be null when we are trying to move the page controller.");
			yield break;
		}
		
		bool didTween = false;
		for (int i = 0; i < pageBuffer.Count; i++)
		{
			if ((i + (int)direction < 0) || 
				(i + (int)direction >= pagePositions.Count))
			{
				// If we are going to go out of range, dont move this one.
				continue;
			}

		    iTween.Stop(gameObject);
			// Get our destination position.
			Vector3 destination = pagePositions[i + (int)direction];
			if (destination.x != pageBuffer[i].localPosition.x)
			{
				iTween.MoveTo(pageBuffer[i].gameObject, iTween.Hash(
					"x", destination.x,
					"time", tweenTime,
					"islocal", true,
					"easetype", iTween.EaseType.easeOutBack));
				didTween = true;
			}

		}

		shufflePages(direction); // Now move the pages around so that we are still in the center of the buffers.
		checkButtons();
		if (didTween)
		{
			yield return new WaitForSeconds(tweenTime); // Wait for the tweens to finish.
		}
		else
		{
			yield return null; // Wait a frame so that on the first time through things can initialize
		}

		// If we moved, call the events if they are set.
		if (onPageViewed != null && direction != SwipeDirection.NONE && pageBuffer.Count > numBufferPages)
		{
			onPageViewed(pageBuffer[numBufferPages].gameObject, currentIndex);
		}

		if (onPageHide != null && direction != SwipeDirection.NONE && pageBuffer.Count > (numBufferPages + (int)direction))
		{
			onPageHide(pageBuffer[numBufferPages + (int)direction].gameObject, currentIndex + (int)direction);
		}
	}

	private void shufflePages(SwipeDirection direction)
	{
		Transform page = null;
		int pageIndex = currentIndex;

		if (pageBuffer.Count > 0)
		{
			// Grab the page we want to move.
			switch (direction)
			{
				case SwipeDirection.LEFT:
					page = pageBuffer[0];
					pageIndex -= pageBuffer.Count-1;
					break;
				case SwipeDirection.RIGHT:
					page = pageBuffer[pageBuffer.Count -1];
					pageIndex += pageBuffer.Count-1;
					break;
				case SwipeDirection.NONE:
					return; // Dont shuffle when not moving.
			}
		}

		if (pageIndex >= 0 && pageIndex < size && onPageReset != null)
		{
			onPageReset(page.gameObject, pageIndex);
		}

		// Remove it from the buffer.
		pageBuffer.Remove(page);
		// Re-add it at the end/beginning.

		if (!usePageCache)
		{
			Destroy(page.gameObject);
		}
		else
		{
			pageObjectCacher.releaseInstance(page.gameObject);
			page.gameObject.SetActive(false);
		}

		float newPosition = pageWidth * numBufferPages * -(int)direction;
		int newIndex = currentIndex - (numBufferPages * (int)direction);		
		Transform newPage = createObject(newPosition, newIndex);

		if (direction == SwipeDirection.LEFT)
		{
			pageBuffer.Add(newPage);
		}
		else
		{
			pageBuffer.Insert(0, newPage);
		}
	}

	protected virtual void swipe(SwipeDirection direction, bool wasClick = false)
	{
		switch (direction)
		{
			case SwipeDirection.LEFT:
				currentIndex++; // Swiping left means moving to the right.
				if (onSwipeLeft != null)
				{
					GameObject swipeObject = pageBuffer != null && pageBuffer.Count > numBufferPages ? pageBuffer[numBufferPages].gameObject : null;
					if (swipeObject != null)
					{
						onSwipeLeft(swipeObject, currentIndex);
					}
				}
				break;
			case SwipeDirection.RIGHT:
				currentIndex--; // Swiping left means moving to the left.
				if (onSwipeRight != null)
				{
					GameObject swipeObject = pageBuffer != null && pageBuffer.Count > numBufferPages ? pageBuffer[numBufferPages].gameObject : null;
					if (swipeObject != null)
					{
						onSwipeRight(swipeObject, currentIndex);
					}
				}
				break;
			default:
				break;
		}
		movePages(direction); // Tween all the pages.
	}

	protected virtual void click(SwipeDirection direction, bool wasClick = false)
	{
		switch (direction)
		{
			case SwipeDirection.LEFT:
				currentIndex++; // Swiping left means moving to the right.
				if (onClickLeft != null)
				{
					GameObject clickObject = pageBuffer != null && pageBuffer.Count > numBufferPages ? pageBuffer[numBufferPages].gameObject : null;
					if (clickObject != null)
					{
						onClickLeft(clickObject, currentIndex);
					}
				}
				break;
			case SwipeDirection.RIGHT:
				currentIndex--; // Swiping left means moving to the left.
				if (onClickRight != null)
				{
					GameObject clickObject = pageBuffer != null && pageBuffer.Count > numBufferPages ? pageBuffer[numBufferPages].gameObject : null;
					if (clickObject != null)
					{
						onClickRight(clickObject, currentIndex);
					}
				}
				break;
			default:
				break;
		}
		movePages(direction); // Tween all the pages.
	}

	protected void checkButtons()
	{
		if (leftButton != null && hidePageButtons)
		{
			leftButton.gameObject.SetActive(currentIndex > 0);
		}
		if (rightButton != null && hidePageButtons)
		{
			rightButton.gameObject.SetActive(currentIndex < size - 1);
		}
		if (paginator != null)
		{
			paginator.selectPage(currentIndex);
		}
	}

	protected void leftClicked(Dict args)
	{
		if (currentIndex > 0)
		{
			if (onClickLeft != null)
			{
				click(SwipeDirection.RIGHT);
			}
			else
			{
				swipe(SwipeDirection.RIGHT);
			}
		}
	}

	protected void rightClicked(Dict args)
	{
		if (currentIndex < (size - 1))
		{
			if (onClickLeft != null)
			{
				click(SwipeDirection.LEFT);
			}
			else
			{
				swipe(SwipeDirection.LEFT);
			}
		}
	}

	/** PUBLIC ACCESSABLE CONVENIENCE FUNCTIONS  **/
	public void showButtons(bool shouldShow)
	{
		if (leftButton != null)
		{
			leftButton.gameObject.SetActive(shouldShow);
		}
		if (rightButton != null)
		{
			rightButton.gameObject.SetActive(shouldShow);
		}
	}
	/****************************************************/

	public int currentPage 
	{
		get
		{
			return currentIndex;
		}
	}
}
