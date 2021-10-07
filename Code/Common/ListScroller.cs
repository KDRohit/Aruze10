using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Controls scrolling of a vertical list of objects.
NGUI's UIDraggablePanel system is a pain in the ass to deal with.
This class is intended to control a list when there is only one list on the screen,
which means it doesn't care where you touch to start scrolling.
If two or more lists need to be visible on screen at the same time, then we'll
need to implement drag areas like the main lobby does for main options & friends.
*/

[ExecuteInEditMode]

public class ListScroller : TICoroutineMonoBehaviour
{
	public int itemsPerRowOrColumn = 1;
	public bool isHorizontal = false;		// Does this list scroll horizontally instead of vertically?
	public SwipeArea swipeArea;				// If specified, only scroll if swiping on this area.
	public float scrollSpeedTiming = 1f;
	public float alphaSpaceAdjust = 0f;
	public float maxSizeLimit = 0f;  // set this to a value to use as the visible size replacement when choosing to display list items

	// The list typically appears in a viewport from a special UICamera that has a UIViewport component attached.
	// The Bottom Right and Top Left objects of the UIViewport should be anchored to the Bottom Right and Top Left
	// of the UISprite linked as viewportSizer. The viewportSizer sprite may be UIAnchor'd and UIStretch'd to a
	// visual sprite that defines visual the border of the viewport. The viewportSizer sprite itself is typically
	// the "Transparent" sprite since we only want it for sizing purposes.
	public UISprite viewportSizer;

	// Offset the scroller within the camera's viewport,
	// for situations where items go outside the normal bounds and you don't want them cut off.
	public Vector2 offset = Vector2.zero;
	
	public bool isAnimatingScroll { get; private set; }	// Is set true during scrolling animation, to allow MonoBehaviour hosts to know when it's finished, since we can't yield on it.
		
	[HideInInspector] public DialogBase dialog = null;	// If provided, only process swipes when showing this dialog, else only process if not showing any dialogs.

	private float touchScrollPos = 0;		// The position of the scrolling when touch started.
	private float lastTouchPos = 0;			// Used for calculating momentum.
	private float beforeLastTouchPos = 0;   // The touch position before last touch position.
	public float momentum { get; private set; }
	public bool isDragging { get; private set; }// Scrolling momentum, so we can keep scrolling after releasing drag touch.
	private float basePos;					// The base position of the list when at the top, or left if horizontal.
	private float maxItemPosition;			// The farthest an item is positioned on the list.
	private float maxScroll;				// The maximum scroll amount to reach the bottom or right of the list.
	private Vector3 listPos = Vector3.zero;	// Keep a Vector3 around so we don't have to keep creating one for updates.
	private float refreshPos = 0;			// Used for determining whether we need to create or destroy item panels.
	private float lastMomentumCheck = 0;	// Used for slowing momentum.
	private bool didSetZ = false;
	private int maxItemSize = 0; // largest lobby option size for centering calculations with low lobby option count
	
	private List<ListScrollerItem> itemMap = new List<ListScrollerItem>();

	// Scroll Bar Functionality
	public UIScrollBar scrollBar;
	public float totalBounds;

	private void Awake()
	{
		// This function gets called as an init. So moving scroll registation here.
		if (scrollBar != null)
		{
			scrollBar.onChange -= scrollBarChanged;
			scrollBar.onChange += scrollBarChanged;
		}
	}

	private void scrollBarChanged(UIScrollBar sb)
	{
		if (sb != null)
		{
			normalizedScroll = (sb.scrollValue);
		}
	}

	private float scrollPos
	{
		get
		{
			return (isHorizontal ? transform.localPosition.x : transform.localPosition.y);
		}
	}

	private float touchPos
	{
		get
		{
			return (isHorizontal ? TouchInput.position.x : TouchInput.position.y);
		}
	}
	
	private float dragDistance
	{
		get
		{
			return (isHorizontal ? TouchInput.dragDistanceX : TouchInput.dragDistanceY);
		}
	}
	
	private float viewportWidth
	{
		get
		{
			if (viewportSizer == null)
			{
				return 0.0f;
			}
			return viewportSizer.transform.localScale.x;	
		}
	}

	private float viewportHeight
	{
		get
		{
			if (viewportSizer == null)
			{
				return 0.0f;
			}
			return viewportSizer.transform.localScale.y;	
		}
	}
	
	private float visibleSize
	{
		get
		{
			return (isHorizontal ? viewportWidth : viewportHeight);
		}
	}
	
	private float listPosCoord
	{
		get
		{
			return (isHorizontal ? listPos.x : listPos.y);
		}
		
		set
		{
			if (isHorizontal)
			{
				listPos.x = value;
			}
			else
			{
				listPos.y = value;
			}
		}
	}
		
	// The panelPool holds panels that have been "destroyed", but not actually destroyed so we
	// can re-use them again for another list item without creating a new one.
	// This is purely for performance reasons.
	// The key object is the prefab used to create the value objects,
	// since we may have more than one type of prefab being used in the list.
	private Dictionary<GameObject, List<GameObject>> panelPool = new Dictionary<GameObject, List<GameObject>>();
		
	// Recalculates the max scroll amount, just in case items were added or removed from the list.
	public void setItemMap(List<ListScrollerItem> newItemMap)
	{
		// First make sure any panels from a previous itemMap are cleared.
		foreach (ListScrollerItem item in itemMap)
		{
			if (item.panel != null)
			{
				destroyItemPanel(item);
			}
		}
		
		itemMap = newItemMap;
		spaceItems();
	}
	
	// Removes an item from the list and adjusts the spacing of the remaining items.
	public void removeItem(ListScrollerItem item)
	{
		destroyItemPanel(item);
		itemMap.Remove(item);
		spaceItems();
	}
	
	// Spaces out the items in the list.
	protected virtual void spaceItems()
	{
		int itemCount = 0;
		float pos = 0;
		
		foreach (ListScrollerItem item in itemMap)
		{
			// Disable spacers to get the correct amount of space.
			// Since things can't be altered on the original prefab,
			// we instantiate a temporary object to use to determine spacing.
			GameObject go = GameObject.Instantiate(item.prefab) as GameObject;
			ListScrollerSpacer[] spacers = go.GetComponentsInChildren<ListScrollerSpacer>(true);
			foreach (ListScrollerSpacer spacer in spacers)
			{
				spacer.gameObject.SetActive(!spacer.doIgnoreForSpacing);
			}

			Bounds bounds = CommonGameObject.getObjectBounds(go);
			Destroy(go);
			
			item.size = bounds.size;

			if (isHorizontal)
			{
				item.x = pos;
				item.y = (itemCount % itemsPerRowOrColumn) * -item.size.y;
			}
			else
			{
				item.y = pos;
				item.x = (itemCount % itemsPerRowOrColumn) * item.size.x;
			}
			
			if (item.panel != null)
			{
				// If a panel has been instantiated already, set the position of it now.
				item.panel.transform.localPosition = new Vector3(item.x, item.y, 0);
			}
			
			itemCount++;
			
			if (itemCount == itemMap.Count 
				|| (itemCount < itemMap.Count && itemCount % itemsPerRowOrColumn == 0))
			{
				if (isHorizontal)
				{
					pos += bounds.size.x;
				}
				else
				{
					pos -= bounds.size.y;
				}
			}

			if ( bounds.size.x > maxItemSize )
			{
				maxItemSize = (int)bounds.size.x;
			}
		}
		
		maxItemPosition = pos;

		totalBounds = Mathf.Abs(pos);
	
		// Make sure the current scroll position is still within legal bounds of the list.
		setScroll(listPosCoord);
		
		refreshPanels();		
	}
	
	// Sets the scrolling to the very top, like the default.
	public void scrollToTop()
	{
		setScroll(basePos);
		refreshPanels();
	}
		
	void Update()
	{
		setPositionVariables();
				
		// Only handle swiping if no dialog is showing unless a dialog is provided,
		// then only handle it if the provided dialog is the current dialog.
		// Also, don't handle swiping if showing the dev panel or the ingame log.
		bool isDraggingSwipeArea =
			(Dialog.instance != null && dialog == Dialog.instance.currentDialog) &&
			!DevGUI.isActive &&
			!CustomLog.Log.isActive &&
			TouchInput.isDragging &&
			(swipeArea == null || TouchInput.swipeArea == swipeArea);
		
		if (!isDragging)
		{
			if (isDraggingSwipeArea)
			{
				// Started a new drag.
				isDragging = true;
				touchScrollPos = scrollPos;
				momentum = 0;
			}
			else if (TouchInput.isTouchDown)
			{
				// If touching but not dragging, stop the momentum.
				momentum = 0;
			}
		}
		
		isDragging = isDraggingSwipeArea;
		
		if (isDragging)
		{
			// In small devices like iPhone, when draging, lastTouchPos might get equal to touchPos instantly.
			// Then momentum instantly becomes 0 and then scroller stops scrolling. 
			// Storing last 2 touch positions solves this issue.
			if (Mathf.Abs(touchPos - lastTouchPos) > 2f)
			{
				momentum = touchPos - lastTouchPos;
			}
			else
			{
				momentum = touchPos - beforeLastTouchPos;
			}
						
			float adjustedDistance = dragDistance / NGUIExt.pixelFactor;
			
			// If actively dragging, set the position to exactly the offset since starting the touch.
			setScroll(touchScrollPos + adjustedDistance);
		}
		else
		{
			setScroll(listPosCoord + momentum);
			
			if (!Mathf.Approximately(momentum, 0))
			{
				if (Time.realtimeSinceStartup - lastMomentumCheck >= .1f)
				{
					// Slow the momentum each tenth of a second.
					momentum *= .75f;
					lastMomentumCheck = Time.realtimeSinceStartup;
				}
						
				if (Mathf.Abs(momentum) <= 1f)
				{
					momentum = 0;
				}
			}
		}
			
		// Determine which panels should be created or destroyed based on distance from the viewable area.
		float s = maxSizeLimit != 0 ? maxSizeLimit : visibleSize;
		if (Mathf.Abs(refreshPos - listPosCoord) > s)
		{
			refreshPanels();
		}

		beforeLastTouchPos = lastTouchPos;
		lastTouchPos = touchPos;

		if (scrollBar != null && !scrollBar.isSelected)
		{
			updateScrollBar();
		}
	}

	private void updateScrollBar()
	{
		if (scrollBar == null)
		{
			return;
		}	

		scrollBar.setScrollValue(normalizedScroll);
	}
	
	private void setPositionVariables()
	{
		if (isHorizontal)
		{
			basePos = Mathf.Floor(viewportWidth * -0.5f) + offset.x;
			maxScroll = Mathf.Min(basePos - maxItemPosition + viewportWidth, basePos);
			listPos.y = Mathf.Floor(viewportHeight * 0.5f) + offset.y;

			if ( numVisiblePanels() == itemMap.Count && maxItemSize > 0 )
			{
				// BY: If you are looking at this code trying to fix some weird glitchy scrolling issue,
				// that's probably because your objects when translated after
				// the code below happens, are no longer "fully visible" within the viewport.
				// But that doesn't make sense right? Because I'm guessing your objects
				// are probably the right size at some point. There's a good chance the bounds property in
				// this class is reporting something inaccurate (in terms of valid pixel space). What I would make sure is
				// that the prefab you're using to populate this list has a very accurate scaling applied.
				// When I ran into this issue, my particular asset was scaled to x:230, and the parent
				// scaled to x: 2. The bounds calculation assumed the global scale was then x:460 (which makes sense),
				// which in turn made the visible options appear outside the viewport by about 100 pixels.
				// When I opened the asset, I noticed that if I set the parent to scale 1, and scaled up the child asset accurately,
				// it was actually x:410 when the alpha space wasn't blown out of proportion thanks to 9-slice.
				// That fixed the issue. However, in that scenario the art would not look as designed. Enters the property alphaSpaceAdjust.
				// this will help alleviate some of the gratuitous alpha pixel space. You should set that as needed
				basePos = -(maxItemSize * itemMap.Count / 2) + offset.x;
				listPosCoord = 0; // this gets set earlier, and throws off the entire positioning fix
			}
		}
		else
		{
			basePos = Mathf.Floor(viewportHeight * 0.5f) + offset.y;
			maxScroll = Mathf.Max(basePos - maxItemPosition - viewportHeight, basePos);
			listPos.x = Mathf.Floor(viewportWidth * -0.5f) + offset.x;
		}
	}

	private int numVisiblePanels()
	{
		float startMultiplier = 1.0f;
		float endMultiplier = 1.0f;

		int count = 0;

		foreach (ListScrollerItem item in itemMap)
		{
			// Create or show the panel if it is close to the view.
			if (isFullyVisible(item, startMultiplier, endMultiplier))
			{
				count++;
			}
		}

		return count;
	}
	
	// Creates and destroys item panels as necessary for the current scroll view.
	private void refreshPanels()
	{
		float startMultiplier = 1.5f;
		float endMultiplier = 2.0f;
		
		foreach (ListScrollerItem item in itemMap)
		{
			if (item.panel != null)
			{
				// First hide the panel if it is too far out of view to be useful.
				if (!isInView(item, startMultiplier, endMultiplier))
				{
					destroyItemPanel(item);
				}
			}
			else
			{
				// Create or show the panel if it is close to the view.
				if (isInView(item, startMultiplier, endMultiplier))
				{
					createItemPanel(item);
				}
			}
		}

		refreshPos = listPosCoord;		
	}
	
	private bool isInView(ListScrollerItem item, float startMultiplier, float endMultiplier)
	{
		float startDiff = visibleSize * startMultiplier;
		float endDiff = visibleSize * endMultiplier;
		
		if (isHorizontal)
		{
			startDiff = -startDiff;
			endDiff = -endDiff;
		}

		float startPos = basePos + startDiff;
		float endPos = basePos - endDiff;
		float itemPos = listPosCoord + (isHorizontal ? item.x : item.y);

		if (isHorizontal)
		{
			return (itemPos >= startPos && itemPos <= endPos);
		}
		else
		{
			return (itemPos <= startPos && itemPos >= endPos);
		}
	}

	private bool isFullyVisible(ListScrollerItem item, float startMultiplier, float endMultiplier)
	{
		float startPos = Mathf.Floor(isHorizontal ? viewportWidth * -0.5f : viewportHeight * 0.5f);
		float endPos = -startPos;
		float itemPos = listPosCoord + (isHorizontal ? item.x + item.size.x: item.y + item.size.y);

		if (isHorizontal)
		{
			return (itemPos - item.size.x + alphaSpaceAdjust >= startPos && itemPos - alphaSpaceAdjust <= endPos);
		}
		else
		{
			return (itemPos - item.size.y <= startPos && itemPos >= endPos);
		}
	}
	
	
	// Sets the scroll position and validates it to stay in bounds.
	private void setScroll(float pos)
	{
		if (!didSetZ)
		{
			listPos.z = transform.localPosition.z;
			didSetZ = true;
		}

		if (isHorizontal)
		{
			// When horizontal, the basePos and maxScroll values are reversed as far as which one is higher.
			listPosCoord = Mathf.Clamp(pos, maxScroll, basePos);
		}
		else
		{
			listPosCoord = Mathf.Clamp(pos, basePos, maxScroll);
		}
		transform.localPosition = listPos;
	}
	
	// Scroll the list so that the given item is shown at the top.
	public void scrollToItem(ListScrollerItem item)
	{
		setScroll(basePos - (isHorizontal ? item.x : item.y));
	}
	
	public IEnumerator animateScroll(float normalizedScrollTo, float time = -1f, System.Action finishedCallback = null)
	{
		if (time < 0)
		{
			time = scrollSpeedTiming;
		}
		
		isAnimatingScroll = true;
		
		iTween.ValueTo(gameObject, iTween.Hash(
			"from", normalizedScroll,
			"to", normalizedScrollTo,
			"time", time,
			"easetype", iTween.EaseType.easeInOutQuad,
			"onupdate", "updateAnimateScroll"
		));
		
		yield return new WaitForSeconds(time);
		
		if (finishedCallback != null)
		{
			finishedCallback();
		}
		
		isAnimatingScroll = false;
	}

	private void updateAnimateScroll(float value)
	{
		normalizedScroll = value;
	}
	
	public float normalizedScroll
	{
		get
		{
			setPositionVariables();	// Guarantees that basePos and maxScroll are set.

			if (isHorizontal)
			{
				return Mathf.InverseLerp(basePos, maxScroll, listPosCoord);
			}
			else
			{
				return Mathf.InverseLerp(maxScroll, basePos, listPosCoord);
			}
		}
		
		set
		{
			setPositionVariables();	// Guarantees that basePos and maxScroll are set.
			
			if (isHorizontal)
			{
				setScroll(Mathf.Lerp(basePos, maxScroll, value));
			}
			else
			{
				setScroll(Mathf.Lerp(maxScroll, basePos, value));
			}
		}
	}
		
	// Creates or re-uses a panel for the given item.
	private void createItemPanel(ListScrollerItem item)
	{
		// First look for an unused panel in the pool.
		
		GameObject panel = null;
		
		if (panelPool.ContainsKey(item.prefab))
		{
			foreach (GameObject checkPanel in panelPool[item.prefab])
			{
				if (!item.isBusy)
				{
					panel = checkPanel;
					break;
				}
			}
			
			if (panel != null)
			{
				panelPool[item.prefab].Remove(panel);
			}
		}
		
		if (panel == null)
		{
			// Panel not found in pool, so make a new one.
			panel = GameObject.Instantiate(item.prefab) as GameObject;
			panel.transform.parent = transform;
			panel.transform.localScale = Vector3.one;
			
			// Hide any spacers that should be hidden.
			ListScrollerSpacer[] spacers = panel.GetComponentsInChildren<ListScrollerSpacer>(true);
			foreach (ListScrollerSpacer spacer in spacers)
			{
				spacer.gameObject.SetActive(spacer.doIgnoreForSpacing);
			}
		}
				
		// Set it up.
		StartCoroutine(item.setupPanel(panel, this));
	}
	
	// Puts the item's panel game object into the pool and unassigns it from the item.
	private void destroyItemPanel(ListScrollerItem item)
	{
		if (item.panel == null)
		{
			// Huh?
			return;
		}
		
		// Instead of deactivating, set the X or Y position so it's out of view of the list.
		if (isHorizontal)
		{
			CommonTransform.setX(item.panel.transform, item.size.x * -2);
		}
		else
		{
			CommonTransform.setY(item.panel.transform, item.size.y * 2);
		}
		
		if (!panelPool.ContainsKey(item.prefab))
		{
			panelPool.Add(item.prefab, new List<GameObject>());
		}
		
		panelPool[item.prefab].Add(item.panel);
		item.panel = null;
	}
	
	// Immediately set the alpha of all visible panels. Requires MasterFader component on the panels.
	public void setPanelsAlpha(float alpha)
	{
		foreach (ListScrollerItem item in itemMap)
		{
			if (item.panel != null)
			{
				item.setPanelAlpha(alpha);
			}
		}
	}

	// Immediately set the scale of all visible panels. Requires ListScrollerScaler component on the panel's object to use for scaling.
	public void setPanelsScale(Vector3 scale)
	{
		foreach (ListScrollerItem item in itemMap)
		{
			if (item.panel != null)
			{
				item.setPanelScale(scale);
			}
		}
	}
	
	// Fades all visible panels from the current alpha to the given alpha,
	// with the given delay between each panel's fade.
	// Requires MasterFader component on the panels.
	public void fadePanels(float alpha, float time, float delayBetweenEach)
	{
		float delay = 0.0f;
		
		foreach (ListScrollerItem item in itemMap)
		{
			if (item.panel != null)
			{
				if (isInView(item, 0.25f, 1.0f))
				{
					item.fadePanel(alpha, time, delay);
					delay += delayBetweenEach;
				}
				else
				{
					// If not actually in view, immediately set the alpha instead of fading,
					// to avoid a visible delay in fading in the panels that are actually in view,
					// since there are some active panels that are out of view.
					item.setPanelAlpha(alpha);
				}
			}
		}
	}

	// Animates all visible panels from the current scale to the given scale,
	// with the given delay between each panel's scaling.
	// Requires ListScrollerScaler component on the panel's object to be used for scaling.
	public void scalePanels(Vector3 scale, float time, float delayBetweenEach)
	{
		float delay = 0.0f;
		
		foreach (ListScrollerItem item in itemMap)
		{
			if (item.panel != null)
			{
				if (isInView(item, 0.25f, 1.0f))
				{
					item.scalePanel(scale, time, delay);
					delay += delayBetweenEach;
				}
				else
				{
					// If not actually in view, immediately set the scale instead of animating,
					// to avoid a visible delay in animating the panels that are actually in view,
					// since there are some active panels that are out of view.
					item.setPanelScale(scale);
				}
			}
		}
	}
	
	// Call the specified callback function for each item in the list.
	public void forEachListItem(ListScrollerItemDelegateVoid callback)
	{
		foreach (ListScrollerItem item in itemMap)
		{
			callback(item);
		}
	}
	
	// Returns the position in the list map of a given item.
	public int getItemIndex(ListScrollerItem item)
	{
		return itemMap.IndexOf(item);
	}
}

// Represents a single item in a ListScroller list.
public class ListScrollerItem
{
	public ListScrollerItemDelegate createFunction;	// The function to call after creating a panel, so it can be set up.
	public object data;								// The data that is specific to this item. Useful in the createFunction call.
	public GameObject prefab;						// The prefab to instantiate the object from.
	public Vector3 size;							// The size of the panel when instantiated.
	public float x;									// The x position for the instantiated object when instantiated.
	public float y;									// The y position for the instantiated object when instantiated.
	public GameObject panel;						// The instantiated object, or null if not instantiated.
	public bool isBusy = false;						// Set to true when being set up, to prevent re-use while it is busy setting up a previous use.
	public int layer = 0;							// The original layer of the panel. Used for restoring visibility.
	
	//private ListScroller scroller = null;
	private MasterFader fader = null;
	private ListScrollerScaler scaler = null;

	public ListScrollerItem(GameObject prefab, ListScrollerItemDelegate createFunction, object data)
	{
		this.prefab = prefab;
		this.createFunction = createFunction;
		this.data = data;
	}
	
	public IEnumerator setupPanel(GameObject panel, ListScroller scroller)
	{
		isBusy = true;
		//this.scroller = scroller;
		this.panel = panel;
		
		// Kindly position it.
		panel.transform.localPosition = new Vector3(x, y, 0);
		
		fader = panel.GetComponent<MasterFader>();
		scaler = panel.GetComponentInChildren<ListScrollerScaler>();
		
		setPanelAlpha(1.0f);
		
		yield return RoutineRunner.instance.StartCoroutine(createFunction(this));
		
		isBusy = false;
	}
	
	// Fade a panel from its current alpha to the given alpha, over the given time.
	// In order to do this, the panel must have a script with a setAlpha(float alpha) method implemented.
	public void fadePanel(float alpha, float time, float delay = 0.0f)
	{
		if (panel == null)
		{
			return;
		}
		
		if (fader == null)
		{
			Debug.LogWarning("ListScrollerItem.fadePanel() couldn't find a MasterFader component.");
			return;
		}
		
		iTween.ValueTo(panel, iTween.Hash(
			"from", fader.alpha,
			"to", alpha,
			"time", time,
			"delay", delay,
			"easetype", iTween.EaseType.linear,
			"onupdate", "setAlpha"
		));
	}

	// Fade a panel from its current alpha to the given alpha, over the given time.
	// In order to do this, the panel must have a script with a setAlpha(float alpha) method implemented.
	public void scalePanel(Vector3 scale, float time, float delay = 0.0f)
	{
		if (panel == null)
		{
			return;
		}
		
		if (scaler == null)
		{
			Debug.LogWarning("ListScrollerItem.fadePanel() couldn't find a ListScrollerScaler component.");
			return;
		}

		iTween.ScaleTo(scaler.gameObject, iTween.Hash(
			"scale", scale,
			"time", time,
			"delay", delay,
			"easetype", iTween.EaseType.linear
		));
	}
	
	// Immediately set the panel's alpha.
	public void setPanelAlpha(float alpha)
	{
		if (fader != null)
		{
			fader.setAlpha(alpha);
		}
	}
	
	// Immediately set the panel's scale.
	public void setPanelScale(Vector3 scale)
	{
		if (scaler != null)
		{
			scaler.transform.localScale = scale;
		}
	}
}

public delegate IEnumerator ListScrollerItemDelegate(ListScrollerItem item);
public delegate void ListScrollerItemDelegateVoid(ListScrollerItem item);