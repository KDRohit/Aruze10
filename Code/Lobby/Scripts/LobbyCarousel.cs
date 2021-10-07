using UnityEngine;
using System.Collections.Generic;

/*
Manages the display of the ad carousel in the main lobby.
Since STUD is used to define carousel data, this is actually just a
manager for carousel data from STUD, not a data structure itself.
*/

public class LobbyCarousel : TICoroutineMonoBehaviour, IResetGame
{
	public PageScroller pageScroller;
	public CarouselPanelBase[] carouselPanelPrefabs;	// Defined in any order, since they are indexed in a dictionary later.
	
	private GameTimer delayTimer;
	private int currentPage = 1;
	private float defaultX;

	public const int DISPLAY_SECONDS_DEFAULT = 3;	// Seconds to display the slides when auto-advancing, if a slide-specific time isn't specified.
	public const int DISPLAY_PAUSED_SECONDS = 10;	// Seconds to display the slide that is manually navigated to before it starts auto-advancing again.
		
	// Cache the prefabs in a dictionary the first time one is requested, for faster lookup in the future.
	private static Dictionary<CarouselData.Type, GameObject> prefabDictionary = null;
	
	public static LobbyCarousel instance = null;

	void Awake()
	{
		instance = this;
		defaultX = pageScroller.transform.localPosition.x;
		init();
	}
		
	// This could be called anytime the carousel data changes, to reset the page scroller.
	public void init()
	{
		// Reset the default position before re-initializing,
		// to avoid the scroll position from getting way out of whack.
		pageScroller.stopScrollingImmediately();
		CommonTransform.setX(pageScroller.transform, defaultX);

		pageScroller.onAfterScroll = onAfterScroll;
		pageScroller.onClickPageIndicator = onClickPageIndicator;
		pageScroller.onClickScrollLeft = onClickScrollLeft;
		pageScroller.onClickScrollRight = onClickScrollRight;
		pageScroller.init(CarouselData.looped.Count, onCreatePagePanel);

		startAutoSlideTimer();
	}

	void Update()
	{
		if (delayTimer != null && delayTimer.isExpired)
		{
			// Go to the next slide.
			if (pageScroller.scrollPos + 1 == pageScroller.maxPage)
			{
				pageScroller.resetToFirstPage();
			}
			else
			{
				pageScroller.scrollPosQuietly++;
			}
			startAutoSlideTimer();
		}
	}

	private void startAutoSlideTimer()
	{
		if (CarouselData.looped.Count > 1)
		{
			delayTimer = new GameTimer(CarouselData.looped[pageScroller.scrollPos].seconds);
		}
		else
		{
			// If there is no need to cycle through slides, then make sure the timer is cleared.
			delayTimer = null;
		}
	}

	// NGUI button callback, used on the arrows and the swipe area.
	// When the player manually interacts with the carousel,
	// paused it for a longer amount of time than normal.
	private void startPausedTimer()
	{
		if (CarouselData.looped.Count > 1)
		{
			delayTimer = new GameTimer(DISPLAY_PAUSED_SECONDS);
		}
	}
		
	// Track stats for carousel page scroller usage.
	private void onClickPageIndicator(int newScrollPos)
	{
		startPausedTimer();
	}
	
	// Track stats for carousel page scroller usage.
	private void onClickScrollLeft(int newScrollPos)
	{
		startPausedTimer();
	}
	
	// Track stats for carousel page scroller usage.
	private void onClickScrollRight(int newScrollPos)
	{
		startPausedTimer();
	}

	private void onCreatePagePanel(GameObject pagePanel, int index)
	{
		if (index >= CarouselData.looped.Count)
		{
			// This typically happens of calling pageScroller.stopScrollingImmediately() when initializing the carousel,
			// so ignore this issue since CarouselData.looped.Count will likely be different each time init is called.
			return;
		}
		
		// Instantiate a child object of the real panel based on the type of slide this is.
		CarouselData data = CarouselData.looped[index];

		GameObject prefab = findPrefab(data.type);
		if (prefab == null)
		{
			Debug.LogError("Could not find prefab for carousel panel of type: " + data.type);
			return;
		}
		
		GameObject go = CommonGameObject.instantiate(prefab) as GameObject;
		go.transform.parent = pagePanel.transform;
		go.transform.localScale = Vector3.one;
		go.transform.localPosition = Vector3.zero;
		
		CarouselPanelBase panel = go.GetComponent<CarouselPanelBase>();
		
		if (panel == null)
		{
			Debug.LogError("Could not find a CarouselPanelBase script on instantiated carousel panel.", go);
			return;
		}
		
		panel.data = data;
		panel.init();
	}
	
	public GameObject findPrefab(CarouselData.Type type)
	{
		if (prefabDictionary == null)
		{
			prefabDictionary = new Dictionary<CarouselData.Type, GameObject>();
			
			foreach (CarouselPanelBase prefab in carouselPanelPrefabs)
			{
				if(prefab == null)
				{
					continue;
				}
				prefabDictionary.Add(prefab.panelType, prefab.gameObject);
			}
		}

		GameObject prefabObj;
		if (prefabDictionary.TryGetValue(type, out prefabObj))
		{
			return prefabObj;
		}
		return null;
	}
	
	private void onAfterScroll()
	{
		// Remember which page was last scrolled to.
		currentPage = pageScroller.scrollPos;
		
		if (currentPage >= CarouselData.looped.Count)
		{
			// This typically happens of calling pageScroller.stopScrollingImmediately() when initializing the carousel,
			// so ignore this issue since CarouselData.looped.Count will likely be different each time init is called.
			return;
		}
	}
	
	// Checks whether Watch To Earn is still active.
	// If it is not active, then it looks for an active carousel item and deactivates it.
	// If it is active, then it looks for an inactive carousel and activates it.
	public static void checkWatchToEarn()
	{
		CarouselData w2eData;
		if (WatchToEarn.isEnabled)
		{
			w2eData = CarouselData.findInactiveByAction("watch_to_earn");
			if (w2eData != null)
			{
				w2eData.activate();
			}
		}
		else
		{
			w2eData = CarouselData.findActiveByAction("watch_to_earn");
			if (w2eData != null)
			{
				w2eData.deactivate();
			}
		}
	}
	
	public static void resetStaticClassData()
	{
		prefabDictionary = null;
	}	
}
