using System.Collections;
using System.Collections.Generic;
using TMPro;
using CustomLog;
using UnityEngine;

public abstract class BasePageScroller : TICoroutineMonoBehaviour {

	public delegate void PanelDelegate (GameObject panel, int index);
	public delegate void PanelGroupDelegate (GameObject panel, int groupIndex, int index);
	public delegate void PageChangeDelegate (int newScrollPos);

	protected const string SOUND_MOVE_PREVIOUS = "FriendsLeftArrow";
	protected const string SOUND_MOVE_NEXT = "FriendsRightArrow";

	public int panelsPerRow = 1;
	public int rowsPerPage = 1;
	public float baseScrollTime = .1f;					/// This may be changed to make scrolling faster or slower.
	public float spacingX = 310;
	public float spacingY = 300;						/// Positive values space downwards, not upwards, to be consistent with Flash version.
	public float spacingSmallDeviceX = 0;
	public float spacingSmallDeviceY = 0;				/// Positive values space downwards, not upwards, to be consistent with Flash version.
	public float scalePage = 1f;
	public GameObject panelPrefab = null;				/// The prefab to instantiate each panel that gets created.
	public GameObject panelSmallDevicePrefab = null;	/// The prefab to instantiate each panel that gets created when on a small device. If null, uses panelPrefab.
	public GameObject panelHeaderPrefab = null;			/// Only used if using multiple groups of items.
	public float headerSpacingX = 0;					/// Only used if using multiple groups of items.
	public float headerSpacingY = 0;					/// Only used if using multiple groups of items.
	public Transform pageIndicatorsParent = null;		/// Optional. If this and pageIndicatorPrefab are provided, page indicators are shown.
	public GameObject pageIndicatorPrefab = null;		/// Optional. If provided, then page indicators [typically dots] are shown.
	public float pageIndicatorSpacing = 60;				/// The amount to space the page indicators.
	public UIImageButton incrementPageButton = null;	/// Increments displayed list by 1 page.
	public UIImageButton decrementPageButton = null;	/// Decrements displayed list by 1 page.
	public UIImageButton incrementButton = null;		/// Increments displayed list by 1 item.
	public UIImageButton decrementButton = null;		/// Decrements displayed list by 1 item.
	public TextMeshPro pageNumberLabel = null;
	public iTween.EaseType easeType = iTween.EaseType.easeInOutQuad;
	public SwipeArea swipeArea = null;                  // For detecting swipes on this page scroller.
	public bool canSwipe = true;						// In case we want to manually stop swiping

	// Set to true if the host data is set up for looping.
	// This just hides the first and last page indicators since they're duplicate data.
	// Looping is not supported with single scrolling (page only).
	public bool isLooping;

	[HideInInspector] public DialogBase dialog = null;	// If provided, only process swipes when showing this dialog, else only process if not showing any dialogs.
	[HideInInspector] public GenericDelegate onStartDrag = null;			// Function to call when starting to drag the page scroller.
	[HideInInspector] public GenericDelegate onBeforeScroll = null;			// Function to call before scrolling starts.
	[HideInInspector] public GenericDelegate onAfterScroll = null;			// Function to call after scrolling finishes.
	[HideInInspector] public PageChangeDelegate onClickPageIndicator = null;	// Function to call when a page indicator is clicked.
	[HideInInspector] public PageChangeDelegate onSwipeScrollLeft = null;		// Function to call when scrolling left from a swipe.
	[HideInInspector] public PageChangeDelegate onSwipeScrollRight = null;		// Function to call when scrolling right from a swipe.
	[HideInInspector] public PageChangeDelegate onClickScrollLeft = null;		// Function to call when scrolling left from a click.
	[HideInInspector] public PageChangeDelegate onClickScrollRight = null;		// Function to call when scrolling right from a click.

	protected PanelDelegate onCreatePanel = null;			/// Function to call after creating a panel.
	protected PanelGroupDelegate onCreateGroupPanel = null;	/// Function to call after creating a panel that's in a group.
	protected PanelDelegate onCreateGroupHeaderPanel = null;	/// Function to call after creating a group header panel.
	protected PanelDelegate onDestroyPanel = null;			/// Function to call right before a panel is destroyed so external cleanup or repossession

	[HideInInspector] public List<GameObject> panels;

	[HideInInspector] public bool isScrolling { get; protected set; }/// For knowing whether the page is in the process of scrolling.

	protected int panelsPerPage = 3;
	protected bool isMultiScrolling = false;
	protected bool shouldPlaySound = true;
	protected float scrollBasePos = 0;	// The x position of the panel area when showing the left-most buttons in the list.


	protected float scrollMinPos = 0;		// The minimum x or y position of the panel area when scrolling to the last page or option.
	protected int actualPanelsPerRow = 3;
	protected int actualRowsPerPage = 1;



	protected List<PageIndicator> pageIndicators = new List<PageIndicator>();

	protected bool isDragging = false;
	protected float downScrollX = 0f;
	protected float lastScrollX = 0f;
	protected Vector2int downTouchPos;
	protected int lastScrollDirection = 0;
	protected float lastPageButtonTouchTime = 0;

	public float effectiveSpacingX
	{
		get
		{
			return (MobileUIUtil.isSmallMobile && spacingSmallDeviceX != 0 ? spacingSmallDeviceX : spacingX);
		}
	}

	public float effectiveSpacingY
	{
		get
		{
			return (MobileUIUtil.isSmallMobile && spacingSmallDeviceY != 0 ? spacingSmallDeviceX : spacingY);
		}
	}

	// Convenience property for a commonly used value.
	protected float pageSpacing
	{
		get { return actualPanelsPerRow * effectiveSpacingX; }
	}

	public GameObject effectivePanelPrefab
	{
		get
		{
			return (MobileUIUtil.isSmallMobile && panelSmallDevicePrefab != null ? panelSmallDevicePrefab : panelPrefab);
		}
	}

	protected void fillPanelArray()
	{
		// Fill the panel array with nulls, the same amount as possible panels to display.
		// These will be filled with actual panel display object as they are scrolled into view,
		// and destroyed as they scroll out of view.
		panels.Clear();	// Clear it first just in case this is for a reinitialize.
		for (int i = 0; i < totalPanels; i++)
		{
			panels.Add(null);
		}
	}

	/// Must be called as soon as the panel count is known, but after attaching it to the NGUI anchor.
	public virtual void init(int panelCount, PanelDelegate onCreatePanel, PanelGroupDelegate onCreateGroupPanel = null, PanelDelegate onCreateGroupHeaderPanel = null, PanelDelegate onDestroyPanel = null)
	{
		scrollBasePos = transform.localPosition.x;
		this.onCreatePanel = onCreatePanel;
		this.onCreateGroupPanel = onCreateGroupPanel;
		this.onCreateGroupHeaderPanel = onCreateGroupHeaderPanel;
		this.onDestroyPanel = onDestroyPanel;
		setPanelCounts(panelsPerRow, rowsPerPage);
		_scrollPos = 0;
		totalPanels = panelCount;
		if (isLooping && panelCount > 1)
		{
			// Start on the second page since the first page is a copy of the last page when looping.
			scrollPosQuietly = 1;
		}
	}

	/// A separate function to set panels counts so the inspector values can be overridden in code for variable lists.
	public void setPanelCounts(int overridePanelsPerRow, int overrideRowsPerPage)
	{
		actualPanelsPerRow = overridePanelsPerRow;
		actualRowsPerPage = overrideRowsPerPage;
		panelsPerPage = actualPanelsPerRow * actualRowsPerPage;
		setScrollMinPos();
	}

	// Sets the scrollMinPos variable.
	// Should be called whenever one of the variables changes that affects this.
	protected void setScrollMinPos()
	{
		scrollMinPos = scrollBasePos;

		if (allowSingleScrolling)
		{
			scrollMinPos -= (_totalPanels - panelsPerPage) * effectiveSpacingX;
		}
		else
		{
			scrollMinPos -= (maxPage - 1) * pageSpacing;
		}
	}



	/// Show the appropriate number of dots for the number of pages of panels, and center them.
	protected void createPageIndicators()
	{
		// First clean up any indicators from a previous call.
		cleanupPageIndicators();

		int pages = maxPage;
		float x = -(pageIndicatorSpacing * (pages - 1) / 2);

		for (int i = 0; i < pages; i++)
		{
			GameObject go = CommonGameObject.instantiate(pageIndicatorPrefab) as GameObject;
			go.transform.parent = pageIndicatorsParent;
			go.transform.localScale = Vector3.one;
			go.transform.localPosition = new Vector3(x, 0, 0);
			CommonGameObject.setLayerRecursively(go, pageIndicatorsParent.gameObject.layer);
			PageIndicator ind = go.GetComponent<PageIndicator>();
			ind.pageScroller = this;
			ind.page = i;
			pageIndicators.Add(ind);
			x += pageIndicatorSpacing;

			// If set up for looping, hide the first and last page indicators since they're for redundant data.
			if (!allowSingleScrolling && isLooping && (i == 0 || i == pages - 1))
			{
				go.SetActive(false);
			}
		}
	}

	public virtual int totalPanels
	{
		get { return _totalPanels; }

		protected set
		{
			_totalPanels = value;

			setScrollMinPos();

			fillPanelArray(); 


			if (pageIndicatorsParent != null && pageIndicatorPrefab != null)
			{
				createPageIndicators();

				// Select the first page dot by default.
				setPageIndicators();

			}

			// Refresh the display.
			if (scrollPos >= _totalPanels && scrollPos > 0)
			{
				// The current position no longer exists, so we need to go back to the previous page.
				// Do the change immediately instead of scrolling.
				scrollPosImmediate = (page - 1) * panelsPerPage;
			}
			else
			{
				// Refresh the current position.
				scrollPos = scrollPos;
			}
			enableScrollButtons();
		}
	}
	protected int _totalPanels = 0;

	/// Sets the number of items in each group when multiple groups of items are in a list.
	public void setGroupPanelCounts(List<int> counts)
	{
		// We need to manually tally up the total number of panels in the list,
		// which includes actual contents panels and header panels for groups.
		// Every page with items also has a header panel at the top,
		// plus a header panel can exist in the middle of a page if the group
		// contents change within that page.

		int panelIndex = 0;

		panelInfo.Clear();

		if (counts == null)
		{
			return;
		}

		for (int groupIndex = 0; groupIndex < counts.Count; groupIndex++)
		{
			int itemIndex = 0;

			if (panelIndex % panelsPerPage > 0)
			{
				// Starting this group in the middle of a page, so create a header here.
				panelInfo.Add(new PanelInfo(-1, groupIndex, -1));
				panelIndex++;
			}

			while (counts[groupIndex] > 0)
			{
				if (panelIndex % panelsPerPage == 0)
				{
					// First panel on a page, so it has to be a header.
					panelInfo.Add(new PanelInfo(-1, groupIndex, -1));
				}
				else
				{
					// Is an item, not a header.
					panelInfo.Add(new PanelInfo(groupIndex, -1, itemIndex));
					counts[groupIndex]--;
					itemIndex++;
				}

				panelIndex++;
			}
		}


		totalPanels = panelInfo.Count;
	}

	protected List<PanelInfo> panelInfo = new List<PanelInfo>();	// One entry for every panel in the list, to describe what kind of panel goes in this slot.

	/// Sets or gets the current scroll position.
	public virtual int scrollPos
	{
		get { return _scrollPos; }

		set { _scrollPos = value; }
	}
	protected int _scrollPos = 0;		// How many panels over that the panels are scrolled.

	// Provide a way to immediately jump to a page, even if it's an adjacent page.
	public int scrollPosImmediate
	{
		// Provide a getter so we can do things like scrollPosImmediate++
		get { return scrollPos; }

		set
		{
			float tempBaseScrollTime = baseScrollTime;
			baseScrollTime = 0.0f;
			scrollPosQuietly = value;
			baseScrollTime = tempBaseScrollTime;
		}
	}

	public int scrollPosQuietly
	{
		// Provide a getter so we can do things like scrollPosQuietly++
		get { return scrollPos; }

		set
		{
			shouldPlaySound = false;
			scrollPos = value;
			shouldPlaySound = true;
		}
	}

	// Calculates how long scrolling should take to tween.
	protected float getScrollTime(int absDiff)
	{
		return baseScrollTime * (absDiff / actualRowsPerPage);
	}

	// The Update function monitors and handles dragging for partial page scrolls until touch is released.
	protected void Update()
	{
		// Only handle swiping if no dialog is showing unless a dialog is provided,
		// then only handle it if the provided dialog is the current dialog.
		// Also, don't bother handling swiping if there are less options than can fit on a page.
		if ((Dialog.instance != null && dialog == Dialog.instance.currentDialog) &&
			!DevGUI.isActive &&
			!Log.isActive &&
			!isScrolling &&
			totalPanels > panelsPerPage
			)
		{
			if (!isDragging &&
				TouchInput.isDragging &&
				swipeArea != null &&
				TouchInput.swipeArea == swipeArea &&
			    canSwipe
				)
			{
				// Started a new drag.
				isDragging = true;
				downScrollX = transform.localPosition.x;
				downTouchPos = TouchInput.downPosition;
				lastScrollDirection = 0;
				if (onStartDrag != null)
				{
					onStartDrag();
				}
			}
			else if (isDragging && !TouchInput.isDragging)
			{
				isDragging = false;

				// Stopped dragging. Snap to the nearest page or option last scrolled towards,
				// but only if the user dragged more left/right than up/down.
				if (Mathf.Abs(downTouchPos.y - TouchInput.position.y) > Mathf.Abs(downTouchPos.x - TouchInput.position.x))
				{
					// Dragged more up/down than left/right, so ignore the left/right and snap back to original position.
					resetScrollPos();
				}
				else if (downScrollX < transform.localPosition.x)
				{
					// Stopped dragging to the left of the original position.
					if (lastScrollDirection == -1)
					{
						// Scrolled left.
						scrollPos -= panelsPerPage;
						if (onSwipeScrollLeft != null)
						{
							onSwipeScrollLeft(scrollPos);
						}
					}
					else if (lastScrollDirection == 1)
					{
						// Scrolled back toward the original position, so stay the same.
						resetScrollPos();
					}
				}
				else if (downScrollX > transform.localPosition.x)
				{
					// Stopped dragging to the right of the original position.
					if (lastScrollDirection == 1)
					{
						// Scrolled right.
						scrollPos += panelsPerPage;
						if (onSwipeScrollRight != null)
						{
							onSwipeScrollRight(scrollPos);
						}
					}
					else if (lastScrollDirection == -1)
					{
						// Scrolled back toward the original position, so stay the same.
						resetScrollPos();
					}
				}
				else
				{
					// Very slim chance that the scrolling was put back to the original position, but just reset to make sure.
					resetScrollPos();
				}
			}

			if (isDragging)
			{
				if (lastScrollX < transform.localPosition.x)
				{
					lastScrollDirection = -1;
				}
				else if (lastScrollX > transform.localPosition.x)
				{
					lastScrollDirection = 1;
				}

				lastScrollX = transform.localPosition.x;

				float adjustedDistance = TouchInput.dragDistanceX / NGUIExt.pixelFactor;

				// Limit the scrolling by a single page amount, to avoid seeing empty space two pages away
				// if someone decides to try scrolling really far on a single drag.
				adjustedDistance = Mathf.Clamp(adjustedDistance, -pageSpacing, pageSpacing);

				CommonTransform.setX(transform, Mathf.Clamp(downScrollX + adjustedDistance, scrollMinPos, scrollBasePos));
			}
		}
	}

	// Resets the position of the panels to match the current scrollPos.
	// This is done to snap a drag back to normal if no change in scrollPos is needed.
	protected abstract void resetScrollPos();


	/// Jumps to the first page immediately.
	public abstract void resetToFirstPage();

	/// A prettier way to reset to the first page.
	public void scrollToFirstPage()
	{
		Audio.play(SOUND_MOVE_PREVIOUS);
		StartCoroutine(scrollToPage(!allowSingleScrolling && isLooping ? 1 : 0));
	}

	/// A prettier way to scroll to the last page.
	public void scrollToLastPage()
	{
		Audio.play(SOUND_MOVE_NEXT);
		StartCoroutine(scrollToPage(maxPage - (!allowSingleScrolling && isLooping ? 2 : 1)));
	}

	private IEnumerator scrollToPage(int finalPage)
	{
		if (allowSingleScrolling)
		{
			// Only scrolling to a particular page if single scrolling isn't allowed.
			yield break;
		}

		// If we are already scrolling then we don't want to do this.
		if (isMultiScrolling)
		{
			yield break;
		}

		isMultiScrolling = true;

		while (page != finalPage)
		{
			if (page < finalPage)
			{
				incrementPage();
			}
			else if (page > finalPage)
			{
				decrementPage();
			}

			// Wait for the current page scroll to finish before starting the next one.
			while (isScrolling)
			{
				yield return null;
			}
		}

		isMultiScrolling = false;
	}

	/// Returns the current page being viewed.
	/// If using single panel scrolling instead of pages (like friends bar),
	/// this page value might not be as expected and isn't very useful.
	/// Where page 0 is the first page.
	public int page
	{
		get { return Mathf.FloorToInt(_scrollPos / panelsPerPage); }
	}

	public void clickPageIndicator(int i)
	{
		if (isScrolling)
		{
			// Not today. Don't permit a new scroll target while we're still in flight.
			return;
		}

		scrollPos = i * panelsPerPage;

		if (onClickPageIndicator != null)
		{
			onClickPageIndicator(scrollPos);
		}

		// Immediately set the page indicator if clicking it to jump,
		// instead of waiting for scrolling to finish.
		setPageIndicators();
	}

	/// One of the page indicator dots was clicked. Jump to that page.
	protected void setPageIndicators()
	{
		for (int i = 0; i < pageIndicators.Count; i++)
		{
			PageIndicator indicator = pageIndicators[i];

			if (i == page)
			{
				// Disable the button on the indicator for the current page.
				indicator.button.isEnabled = false;
			}
			else
			{
				indicator.button.isEnabled = true;
			}
		}
	}

	// How many pages total are there? (based on 1 being the first page)
	public int maxPage
	{
		get { return Mathf.CeilToInt((float)_totalPanels / panelsPerPage); }
	}

	/// If no page indicator parent was provided,
	/// then allow scrolling by one panel at a time.
	protected bool allowSingleScrolling
	{
		get { return (pageIndicatorsParent == null && pageNumberLabel == null); }
	}

	// If scrolling, stop immediately.
	public void stopScrollingImmediately()
	{
		if (!isScrolling)
		{
			return;
		}

		iTween.Stop(gameObject);

		finishScrolling();
	}

	protected virtual void finishScrolling()
	{
//		Debug.LogWarning("finishScrolling at " + scrollPos);

		if (pageIndicatorsParent != null)
		{
			// If page indicators are provided, light up the one for the current page,
			// and turn off the rest of them.
			setPageIndicators();
		}

		isScrolling = false;

		createVisiblePanels();

		enableScrollButtons();

		if (onAfterScroll != null)
		{
			onAfterScroll();
		}

		// Delete panels that are no longer on screen.
		cleanupShownPanels();

	}

	// Create new panels for the ones that will be visible now.
	protected virtual void createVisiblePanels()
	{
		for (int i = scrollPos - panelsPerPage; i < scrollPos + panelsPerPage * 2 && i < _totalPanels; i++)
		{
			if (i >= 0)
			{
				createPanel(i);
			}
		}
	}

	protected abstract void createPanel(int index);

	/// Cleans up shown panels, optionally only cleaning up ones out of view or all of them.
	public abstract void cleanupShownPanels(bool includeInView = false);

	/// Cleans up a single panel.
	protected abstract void cleanupPanel(int index);

	// Returns whether the panel at the given index is currently within view for the scroll position.
	// By "within view", I mean it's either on the current page, or a page before or after the current page,
	// since partial scrolling will bring the adjacent pages into view.
	protected bool isPanelInView(int index)
	{
		if (!allowSingleScrolling && isLooping)
		{
			// If looping, then we also consider the redundant data pages and adjacent within view.
			if ((scrollPos == 0 || scrollPos == 1) && index == maxPage - 2)
			{
				return true;
			}
			else if ((scrollPos == maxPage - 1 || scrollPos == maxPage - 2) && index == 1)
			{
				return true;
			}

		}

		return (index >= scrollPos - panelsPerPage && index <= scrollPos + panelsPerPage * 2 - 1);
	}

	/// Calls a given function with each shown panel as arguments,
	/// so host code can do something to each shown panel.
	public abstract void forEachShownPanel(PanelDelegate doFunction);

	/// Disable the buttons to scroll.
	public void disableScrollButtons()
	{
		if (_totalPanels <= panelsPerPage)
		{
			// We've already hidden the buttons, so there's nothing to disable.
			return;
		}
		disableIncrementButton();
		disableDecrementButton();
	}

	/// Enable the buttons to scroll.
	public void enableScrollButtons()
	{
		if (_totalPanels <= panelsPerPage)
		{
			// If not more than one page of buttons,
			// then hide both the left and right buttons instead of enabling/disabling.
			if (incrementPageButton != null)
			{
				incrementPageButton.gameObject.SetActive(false);
			}
			if (decrementPageButton != null)
			{
				decrementPageButton.gameObject.SetActive(false);
			}
			if (incrementButton != null)
			{
				incrementButton.gameObject.SetActive(false);
			}
			if (decrementButton != null)
			{
				decrementButton.gameObject.SetActive(false);
			}
			return;
		}

		if (incrementPageButton != null)
		{
			incrementPageButton.gameObject.SetActive(true);
		}
		if (decrementPageButton != null)
		{
			decrementPageButton.gameObject.SetActive(true);
		}
		if (incrementButton != null)
		{
			incrementButton.gameObject.SetActive(true);
		}
		if (decrementButton != null)
		{
			decrementButton.gameObject.SetActive(true);
		}

		// Enable or disable arrow buttons if at the ends of the list.
		if (scrollPos == 0)
		{
			disableDecrementButton();
		}
		else
		{
			enableDecrementButtons();
		}

		if (scrollPos >= _totalPanels - panelsPerPage)
		{
			disableIncrementButton();
		}
		else
		{
			enableIncrementButtons();
		}
	}

	private void disableIncrementButton()
	{
		if (incrementPageButton != null)
		{
			incrementPageButton.isEnabled = false;
		}
		if (incrementButton != null)
		{
			incrementButton.isEnabled = false;
		}
	}

	private void disableDecrementButton()
	{
		if (decrementPageButton != null)
		{
			decrementPageButton.isEnabled = false;
		}
		if (decrementButton != null)
		{
			decrementButton.isEnabled = false;
		}
	}

	private void enableIncrementButtons()
	{
		if (incrementPageButton != null)
		{
			incrementPageButton.isEnabled = true;
		}
		if (incrementButton != null)
		{
			incrementButton.isEnabled = true;
		}
	}

	private void enableDecrementButtons()
	{
		if (decrementPageButton != null)
		{
			decrementPageButton.isEnabled = true;
		}
		if (decrementButton != null)
		{
			decrementButton.isEnabled = true;
		}
	}

	/// NGUI button callback.
	public void clickDecrementButton()
	{
		clickArrowButton(-1);
		if (onClickScrollLeft != null)
		{
			onClickScrollLeft(scrollPos);
		}
	}

	/// NGUI button callback.
	public void clickIncrementButton()
	{
		clickArrowButton(1);
		if (onClickScrollRight != null)
		{
			onClickScrollRight(scrollPos);
		}
	}

	/// NGUI button callback.
	private void clickDecrementPageButton()
	{
		decrementPage();
		if (onClickScrollLeft != null)
		{
			onClickScrollLeft(scrollPos);
		}
	}

	/// NGUI button callback.
	private void clickIncrementPageButton()
	{
		incrementPage();
		if (onClickScrollRight != null)
		{
			onClickScrollRight(scrollPos);
		}
	}

	/// Increments the page. Does not validate, so that should already be done.
	public void incrementPage()
	{
		clickArrowButton(panelsPerPage);
	}

	/// Decrements the page. Does not validate, so that should already be done.
	public void decrementPage()
	{
		clickArrowButton(-panelsPerPage);
	}

	private void clickArrowButton(int offset)
	{
		if (isScrolling || Time.realtimeSinceStartup - lastPageButtonTouchTime < .25f) // || isMultiScrolling) Commented out because it causes an infinite loop from the scrollToPage() function
		{
			// Not today. Don't permit a new scroll target while we're still in flight or if a button was touched very recently (to prevent spam crashing).
			return;
		}
		lastPageButtonTouchTime = Time.realtimeSinceStartup;
		scrollPos += offset;
	}

	/// Resets everything to default values so that a new set of data can be instantiated.
	public void destroy()
	{
		cleanupShownPanels(true);
		CommonTransform.setX(transform, scrollBasePos);

		if (incrementPageButton != null)
		{
			incrementPageButton.isEnabled = false;
		}
		if (decrementPageButton != null)
		{
			decrementPageButton.isEnabled = false;
		}
		if (incrementButton != null)
		{
			incrementButton.isEnabled = false;
		}
		if (decrementButton != null)
		{
			decrementButton.isEnabled = false;
		}

		cleanupPageIndicators();
	}

	protected virtual void cleanupPageIndicators()
	{
	}

	/// Simple data structure used internally.
	protected class PanelInfo
	{
		public int itemGroup = -1;
		public int headerGroup = -1;
		public int itemIndex = -1;

		public PanelInfo(int itemGroup, int headerGroup, int itemIndex)
		{
			this.itemGroup = itemGroup;
			this.headerGroup = headerGroup;
			this.itemIndex = itemIndex;
		}
	}
}
