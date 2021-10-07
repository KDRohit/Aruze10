using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.LobbyTransitions;
using Com.Scheduler;
using Zynga.Zdk;
using CustomLog;

/**
Controls the look and behavior of the main lobby.
*/

public class MainLobbyV3 : MainLobby
{
	// =============================
	// PRIVATE
	// =============================
	// The Y position of the feature buttons when in view.
	// Treated like a const, but taken from the initial value upon Awake.
	private float FEATURE_BUTTONS_ON_Y = 0;
	
	private List<LobbyOption> unpinnedOptions = new List<LobbyOption>();

	private List<Dictionary<int, bool>> pinnedSpots = new List<Dictionary<int, bool>>();	// Stores which spots have pinned options on each page. The Dictionary is indexed on the page's slot position.
	private List<int> pageStartingIndexes = new List<int>();
	private int pages = 0;
	private List<LobbyOption> displayedOptions = new List<LobbyOption>();	// All options that have buttons linked, so we can disable the buttons.

	private int contentOffset = 0;			
	private int personalizedPage = -1;
	private float originalPageScrollerSpacingX = 1.0f;
	private LobbyTransitionBlackFade blackFade;

	// =============================
	// PUBLIC
	// =============================
	public PageController pageController;
	public LobbyPageV3 pagePrefab;
	public GameObject pageScrollerArrows;
	public GameObject transitionPrefab;
	public GameObject inboxOfferAnchor; // Need a handle to this so I can disable it if I have to.
	public GameObject incentivizedInboxOfferAnchor;
	public AlphaFadePanelSizer alphaSizer; // used for assigning to lobby options for masking
	public PageUI pageUI;
	public TextMeshProMasker masker; // Linked in inspector and used to mask text.
	public GameObject background;
	public GameObject backgroundElite;
	public GameObject backgroundReturnFromElite;
	public ObjectSwapper pageItemsSwapper;
	public int currentPageIndex { get; private set; }

	// Various lobby option prefabs.
	public GameObject optionButtonPrefabGeneric;
	public GameObject optionButtonPrefabLearnMore1X2;
	public GameObject optionButtonPrefabComingSoon;
	public GameObject optionButtonPrefabAction;		// For any action options that don't have special prefabs.

	public GameObject leftEdge;						// uiAnchor attached to left edge of background
	public GameObject rightEdge;					// uiAnchor attached to right edge of background
	public GameObject topSection;					// holds lobby cards and carousel
	public GameObject bottomSection;                // holds vip carousel, tabs and daily deal items, pinned to bottom of screen

	private static GameObject eliteTransitionObject;
	private static LobbyTransitionElite eliteTransition;

	// =============================
	// CONST
	// =============================
	private const int SEC_TO_HOUR = 3600;
	private const float LOBBY_PAGE_WIDTH = 1800; // static page size, everything else on screen resizes instead
	public const int OPTION_COLUMNS_PER_PAGE = 3;
	public const int OPTION_ROWS_PER_PAGE = 2;
	public const int TOTAL_OPTIONS_PER_PAGE = OPTION_COLUMNS_PER_PAGE * OPTION_ROWS_PER_PAGE;
	public const float SCREEN_BASE_WIDTH = 2730.0f;	// base value to use when calculating scale
	public const float SCREEN_BASE_HEIGHT = 1536.0f;	// base value to use when calculating scale
	public const string LOBBY_PREFAB_PATH = "Assets/Data/HIR/Bundles/Initialization/Features/Lobby V3/Lobby Prefabs/Lobby Main Panel V3.prefab";
	private const string DEFAULT_BG_PATH = "assets/data/hir/bundles/initialization/features/lobby v3/textures/hir lobby v3 background.png";
	private const string ELITE_BG_PATH = "assets/data/hir/bundles/initialization/features/elite/elite lobby background 3.png";
	private const float TWEEN_POSITION = 3000;
	private const string ELITE_STATE = "elite";
	private const string DEFAULT_STATE = "default";

	//Lobby Option Cache Variables
	private Stack<GameObject> genericLobbyOptionsCache = new Stack<GameObject>();
	public Transform genericOptionCacheParent;

	// debugging only
	private bool IS_TEST_RUN = false;
	private bool isLoadingElite = false;

	// move sounds
	private const string SOUND_MOVE_PREVIOUS = "FriendsLeftArrow";
	private const string SOUND_MOVE_NEXT = "FriendsRightArrow";

	private LobbyPageV3 lobbyPageV3;
	
	protected override void preAwake()
	{
		base.preAwake();
		
		StartCoroutine(BlackFaderScript.instance.fadeTo(0.0f));

		pageController.onPageViewed += onPageView;
		pageController.onPageHide += onPageHide;
		pageController.onSwipeLeft += onSwipeLeft;
		pageController.onSwipeRight += onSwipeRight;
		pageController.onPageReset += onPageReset;
		pageController.onClickLeft += onClickLeft;
		pageController.onClickRight += onClickRight;

		initElite();

		setBackgroundAsset();
	}

	public void initElite(bool forceLoad = false)
	{
		if (forceLoad || (EliteManager.isActive && EliteManager.showLobbyTransition))
		{
			loadEliteTransition();
		}
	}

	private void loadEliteTransition()
	{
		if (eliteTransitionObject != null || isLoadingElite)
		{
			return;
		}
		isLoadingElite = true;
		AssetBundleManager.load(this, "Features/Elite/Prefabs/Elite Lobby Transition/Elite Lobby Transition", onEliteLoaded, onEliteFailed, isSkippingMapping:true, fileExtension:".prefab");
	}

	private void onEliteLoaded(string path, Object obj, Dict args)
	{
		isLoadingElite = false;
		eliteTransitionObject = CommonGameObject.instantiate(obj as GameObject) as GameObject;
		eliteTransitionObject.transform.parent = BlackFaderScript.instance.transform.parent;
		eliteTransitionObject.transform.localPosition = Vector3.zero;
		//eliteTransitionObject.transform.localScale = Vector3.one;

		eliteTransitionObject.SetActive(false);
		eliteTransition = eliteTransitionObject.GetComponent<LobbyTransitionElite>();

		handleEliteTransition();
	}

	public void handleEliteTransition()
	{
		if (eliteTransitionObject == null)
		{
			loadEliteTransition();
		}
		else if (EliteManager.showLobbyTransition)
		{
			if (EliteManager.hasActivePass)
			{
				playEliteTransition();
				loadEliteBg(backgroundElite);
			}
			else
			{
				playLobbyTransition();
				loadEliteBg(backgroundReturnFromElite);
			}
		}
	}

	private void onEliteFailed(string path, Dict args)
	{
		isLoadingElite = false;
		Debug.LogError("MainLobbyV3: Failed to load " + path);
	}

	protected void setBackgroundAsset()
	{
		// no custom backgrounds if elite is active, elite controls the backgrounds
		if (EliteManager.isActive)
		{
			if (EliteManager.hasActivePass && !EliteManager.showLobbyTransition)
			{
				loadEliteBg(backgroundElite);
				SafeSet.gameObjectActive(background, false);
				SafeSet.gameObjectActive(backgroundReturnFromElite, false);
				SafeSet.gameObjectActive(backgroundElite, true);
			}
			else
			{
				loadEliteBg(backgroundReturnFromElite);
				SafeSet.gameObjectActive(backgroundElite, false);
				SafeSet.gameObjectActive(background, false);
				SafeSet.gameObjectActive(backgroundReturnFromElite, true);
			}
			return;
		}

		if (LoLaLobby.main != null && background != null)
		{
			Renderer bgRenderer = background.GetComponent<Renderer>();
			if (bgRenderer != null)
			{
				if (!string.IsNullOrEmpty(LoLaLobby.main.backgroundAsset))
				{
					DisplayAsset.loadTextureToRenderer(bgRenderer, LoLaLobby.main.backgroundAsset, "", false, false);
				}
				else
				{
					Texture defaultBgTexture = SkuResources.getObjectFromMegaBundle<Texture>(DEFAULT_BG_PATH);
					bgRenderer.material.mainTexture = defaultBgTexture;
				}
			}
		}
	}

	private void loadEliteBg(GameObject rendererParent)
	{
		Renderer bgRenderer = rendererParent.GetComponent<Renderer>();
		Texture eliteBgTexture = SkuResources.getObjectFromMegaBundle<Texture>(ELITE_BG_PATH);
		bgRenderer.material.mainTexture = eliteBgTexture;
	}
	
	protected override void postAwake()
	{
		if (HyperEconomyIntroBuy.shouldShow)
		{
			// Disabled this delay for all situations for now (using the above direct call instead).
			// The first hyper economy sound will be buried by the jingle if music is enabled.
			// StartCoroutine(showHyperEconomyIntroWhenReady());
		}
		
		// Check if we have extra popcorn surfacing turned on.
		if (!isFirstTime)
		{
			int lastViewTimePopcornSale = CustomPlayerData.getInt(CustomPlayerData.STUD_SALE_LAST_VIEWED_POPCORN, 0);
			int currentTime = GameTimer.currentTime;
			int cooldownTime = Glb.POPCORN_SALE_RTL_SHOW_COOLDOWN;
				
			STUDSale popcornSale = STUDSale.getSale(SaleType.POPCORN);

			if (popcornSale != null &&
				currentTime > 0 &&
				!ExperimentWrapper.FirstPurchaseOffer.isInExperiment &&
				(currentTime >= lastViewTimePopcornSale + (SEC_TO_HOUR * cooldownTime)))
			{
				// We only want to gate the cooldown on the last time we saw this dialog on RTL, not globally, so this moving this check here.
				CustomPlayerData.setValue(CustomPlayerData.STUD_SALE_LAST_VIEWED_POPCORN, GameTimer.currentTime);
				STUDSaleDialog.showDialog(popcornSale);
			}
		}

		// setup page controller
		pageController.pageWidth = LOBBY_PAGE_WIDTH;
		pageController.init(pagePrefab.gameObject, pages, onPageCreated);

		// setup the page ui
		pageUI.init(pages);

		LoyaltyLoungeLobbyToaster.registerToasterEvent();
		
		// adjust all for resolution
		StartCoroutine(resolutionChangeHandler());

		bool veryFirstLaunch = isFirstTime && NotificationManager.DayZero;
		if (veryFirstLaunch)
		{
			WelcomeJourney.instance.isFirstLaunch = true;
		}
		else
		{
			//set first launch false
			WelcomeJourney.instance.isFirstLaunch = false;

			//activate carousel if it's been inactivated
			if (WelcomeJourney.instance.isActive())
			{
				CarouselData data = CarouselData.findInactiveByAction("welcome_journey");
				if (data != null)
				{
					data.activate();
				}
			}
		}
		
		base.postAwake();

		if (GiftChestOffer.instance.canShowTooltip)
		{
			GameTimerRange incentivizedInboxDelayTimer = GameTimerRange.createWithTimeRemaining(15);
			incentivizedInboxDelayTimer.registerFunction(checkIfCanShowTooltip);
		}

		if (MainLobbyBottomOverlay.instance != null)
		{
			MainLobbyBottomOverlay.instance.initNewCardsAlert();
		}

		if (pageItemsSwapper != null)
		{
			if (EliteManager.isActive && EliteManager.hasActivePass)
			{
				pageItemsSwapper.setState(ELITE_STATE);
			}
			else
			{
				pageItemsSwapper.setState(DEFAULT_STATE);
			}	
		}	
	}

	public bool isEliteTranstionLoaded()
	{
		return eliteTransition != null;
	}

	public bool isEliteTranstionLoading()
	{
		return isLoadingElite;
	}

	public void playEliteTransition()
	{
		if (eliteTransition != null)
		{
			eliteTransitionObject.SetActive(true);
			eliteTransition.transform.parent = BlackFaderScript.instance.transform.parent;
			eliteTransition.transform.localPosition = Vector3.zero;
			eliteTransition.transform.localScale = Vector3.one;
			eliteTransition.addDoorsClosedCallback(onTransitionDoors);
			eliteTransition.addCompleteCallback(onEliteTransitionComplete);
			eliteTransition.updateState(LobbyTransitionElite.TRANSITION_TO_ELITE);
			Scheduler.addTask(new LobbyTransitionTask(Dict.create(D.OBJECT, eliteTransition)), SchedulerPriority.PriorityType.BLOCKING);
		}
		else
		{
			Debug.LogError("No elite transition object");
		}
	}

	public void onEliteTransitionComplete(Dict args = null)
	{
		EliteManager.onLobbyTransitionComplete();
		// enableAllMouseInput is needed for edge case where a dialog was visible but not closed before the lobby transition happens.
		// When this happens the mouse input is disable when the code gets to this point and lobbyTransition will not enable the mouse.
		NGUIExt.enableAllMouseInput(); 
		if (EliteManager.hasActivePass)
		{
			Dict eliteArgs = Dict.create(D.CUSTOM_INPUT, EliteManager.hasGoldFromUpgrade);
			EliteDialog.showDialog(eliteArgs);
		}
		else
		{
			Dict dialogArgs = Dict.create(D.STATE, EliteAccessState.REJOIN);
			EliteAccessDialog.showDialog(dialogArgs);
		}
		eliteTransition.removeCompleteCallback(onEliteTransitionComplete);
	}

	public void playLobbyTransition()
	{
		if (eliteTransition != null)
		{
			eliteTransitionObject.SetActive(true);
			eliteTransition.transform.parent = BlackFaderScript.instance.transform.parent;
			eliteTransition.transform.localPosition = Vector3.zero;
			eliteTransition.transform.localScale = Vector3.one;
			eliteTransition.addDoorsClosedCallback(onTransitionDoors);
			eliteTransition.addCompleteCallback(onEliteTransitionComplete);
			eliteTransition.updateState(LobbyTransitionElite.TRANSITION_TO_LOBBY);
			Scheduler.addTask(new LobbyTransitionTask(Dict.create(D.OBJECT, eliteTransition)), SchedulerPriority.PriorityType.BLOCKING);
		}
	}

	
	public void transitionOut(float time)
	{
		iTween.MoveTo(topSection, iTween.Hash("x", TWEEN_POSITION, "islocal", true, "time", time, "delay", 1));
	}

	public void transitionIn(float time)
	{
		iTween.MoveTo(topSection, iTween.Hash("x", 0, "time", time, "islocal", true));
	}

	public void onTransitionDoors()
	{
		if (EliteManager.showLobbyTransition && EliteManager.hasActivePass)
		{
			SafeSet.gameObjectActive(backgroundElite, true);
			SafeSet.gameObjectActive(background, false);
			SafeSet.gameObjectActive(backgroundReturnFromElite, false);

			if (Overlay.instance != null)
			{
				Overlay.instance.enableElite();
			}

			if (SpinPanel.hir != null)
			{
				SpinPanel.hir.enableElite();
			}

			if (MainLobbyBottomOverlayV4.instance != null)
			{
				MainLobbyBottomOverlayV4.instance.enableElite();
			}

			
			pageUI.enableEliteIndicators();
			pageItemsSwapper.setState(ELITE_STATE);
		}
		else
		{
			SafeSet.gameObjectActive(backgroundElite, false);
			SafeSet.gameObjectActive(background, false);
			SafeSet.gameObjectActive(backgroundReturnFromElite, true);

			if (Overlay.instance != null)
			{
				Overlay.instance.disableElite();
			}

			if (SpinPanel.hir != null)
			{
				SpinPanel.hir.disableElite();
			}

			if (MainLobbyBottomOverlayV4.instance != null)
			{
				MainLobbyBottomOverlayV4.instance.disableElite();
			}
			pageUI.disableEliteIndicators();
			pageItemsSwapper.setState(DEFAULT_STATE);
		}
	}

	private void checkIfCanShowTooltip(Dict args = null, GameTimerRange sender = null)
	{
		if (ToasterManager.instance != null && MainLobby.instance != null && GiftChestOffer.instance.canShowTooltip)
		{
			if (ToasterManager.instance.isStillPlayingToasters || Dialog.isDialogShowing)		
			{
				sender = GameTimerRange.createWithTimeRemaining(5);
				sender.registerFunction(checkIfCanShowTooltip);
			}
			else
			{
				showIncentivizedOfferTooltip();
			}
		}
	}

	private void showIncentivizedOfferTooltip()
	{
	    GiftChestOffer.instance.startCooldown();
		NGUITools.AddChild(Overlay.instance.topHIR.inboxButton, GiftChestOffer.instance.tooltipPrefab, true);
	}
	
	// Also do ask for credits validation since HIR shows the MFS in ASK mode.
	public override bool shouldShowMFS
	{
		get
		{
			return base.shouldShowMFS && MFSDialog.shouldSurfaceAskForCredits();
		}
	}

	public override int getTrackedScrollPosition()
	{
		if (pageController != null)
		{
			return pageController.currentPage;
		}
		return currentPageIndex;
	}

	public void storeCurrentPageIndex()
	{
		if (pageController != null)
		{
			currentPageIndex = pageController.currentPage;
		}
	}
	
	public override IEnumerator resolutionChangeHandler()
	{
		yield return null;
		yield return null;

		if (rightEdge == null || leftEdge == null || bottomSection == null || topSection == null)
		{
			yield break;
		}

		float screenWidth = rightEdge.transform.localPosition.x - leftEdge.transform.localPosition.x;
		// scale all the gameobject containers that have this UI scaling style, in prototype just having two anchored containers seems to work pretty well

		// bottom section has vip lobby button, tabs and daily deal, anchored to bottom of background
		// not allowed to scale larger than 1.0
		if (lobbyScale > 1.0f)
		{
			bottomSection.transform.localScale = Vector3.one;
			topSection.transform.localScale = Vector3.one;
		}
		else
		{
			bottomSection.transform.localScale = Vector3.one * lobbyScale;
			// top part has lobby cards and carousel, anchored to center of background
			topSection.transform.localScale = Vector3.one * lobbyScale;
		}

		if (background != null && NGUIExt.effectiveScreenWidth > SCREEN_BASE_WIDTH)
		{
			background.transform.localScale = new Vector3(screenWidth, background.transform.localScale.y, 1);
		}

		UIStretch[] stretchToReposition = GetComponentsInChildren<UIStretch>();
		for (int i = 0; i < stretchToReposition.Length; i++)
		{
			stretchToReposition[i].enabled = true;
		}

		UIAnchor[] anchorsToReposition = GetComponentsInChildren<UIAnchor>();
		for (int i = 0; i < anchorsToReposition.Length; i++)
		{
			anchorsToReposition[i].enabled = true;
		}

	}

	// Adjust the size of some elements based on resolution and aspect ratio.
	// MCC making this public so that any FTUEs trying to match with their respective buttons
	// in the lobby can adjust their scaling along with the lobby.
	public float lobbyScale
	{
		get
		{
			// get current screen width
			float screenWidth = rightEdge.transform.localPosition.x - leftEdge.transform.localPosition.x;

			//Debug.LogError("Screen Width is " + screenWidth + " lobby scale is " + lobbyScale);

			// calc scale value
			// Example for an ipad 4:3 screenWidth would be 2048, we divide that by SCREEN_BASE_WIDTH (2730) to get a scale value of .75.
			float lobbyScale = screenWidth/SCREEN_BASE_WIDTH;

			return lobbyScale;
		}
	}
	
	public float getScaleFactor()
	{
		return Mathf.Min(1.0f, lobbyScale);
	}

	protected override void Update()
	{
		// Sparkles used to be cross SKU, but now they're HIR only. 
		// They were the first thing in the base update method...so they're the first thing here.
		CommonEffects.setEmissionRate(touchSparkle, 0);

		pageController.isEnabled = RoyalRushTooltipController.instance == null;

		pageController.setScrollerActive((Dialog.instance == null || !Dialog.instance.isShowing) && RoyalRushTooltipController.instance == null);

		base.Update();
	}

	// Return to the page the player was on when launching a game.
	protected override void restorePreviousScrollPosition()
	{
		if (pageBeforeGame > 0)
		{
			pageController.goToPageAfterInit(pageBeforeGame);
		}
	}

	public void resetToFirstPage()
	{
		if (pageController != null)
		{
			pageController.goToPage(0);
		}
	}

	private void onPageView(GameObject page, int index)
	{
		pageUI.setCurrentPage(index);
	}

	private void onPageReset(GameObject pageParent, int index)
	{
		LobbyPageV3 pageV3 = pageParent.GetComponent<LobbyPageV3>();
		for (int i = 0; i < pageV3.lobbyOptions.Count; i++) 
		{
			pageV3.lobbyOptions[i].button.reset();
			pageV3.lobbyOptions[i].panel.SetActive(false);
			genericLobbyOptionsCache.Push(pageV3.lobbyOptions[i].panel);
			pageV3.lobbyOptions[i].panel.transform.parent = genericOptionCacheParent;
		}
	}

	private void onPageHide(GameObject page, int index)
	{
	}

	private void onSwipeLeft(GameObject page, int index)
	{
		StatsManager.Instance.LogCount
		(
			  counterName: "lobby",
			  kingdom: "page_scroll",
			  phylum: "left",
			  klass: "",
			  family: "",
			  genus: "swipe"
		);
		Audio.play(SOUND_MOVE_PREVIOUS);
		pageBeforeGame = pageController.currentPage;
	}

	private void onSwipeRight(GameObject page, int index)
	{
		StatsManager.Instance.LogCount
		(
			counterName: "lobby",
			kingdom: "page_scroll",
			phylum: "right",
			klass: "",
			family: "",
			genus: "swipe"
		);
		Audio.play(SOUND_MOVE_NEXT);
		pageBeforeGame = pageController.currentPage;
	}

	private void onClickLeft(GameObject page, int index)
	{
		StatsManager.Instance.LogCount
		(
			counterName: "lobby",
			kingdom: "page_scroll",
			phylum: "left",
			klass: "",
			family: "",
			genus: "click"
		);
		Audio.play(SOUND_MOVE_NEXT);
		pageBeforeGame = pageController.currentPage;
	}

	private void onClickRight(GameObject page, int index)
	{
		StatsManager.Instance.LogCount
		(
			counterName: "lobby",
			kingdom: "page_scroll",
			phylum: "right",
			klass: "",
			family: "",
			genus: "click"
		);
		Audio.play(SOUND_MOVE_NEXT);
		pageBeforeGame = pageController.currentPage;
	}
	
	// Callback for when the game is paused (called from PauseHandler.cs).
	public override void pauseHandler(bool isPaused)
	{
		if (!isPaused)
		{
			// on unpause, check to see if we're connected so that we can show the congrats dialog.
			// Only check if we're pending.
			if (LinkedVipProgram.instance.isPending && LinkedVipProgram.instance.isEligible)
			{
				Server.registerEventDelegate("network_status", LinkedVipProgram.instance.onUnpauseCheck);
				NetworkAction.getNetworkState();
				NetworkAction.getVipStatus();
			}
		}
	}
	
	// Do some stuff to get the menu options organized for display.
	protected override void organizeOptions()
	{
		if (lobbyInfo == null)
		{
			return;
		}

		pinnedSpots = lobbyInfo.pinnedSpots;
		pages = lobbyInfo.pages;
		unpinnedOptions = lobbyInfo.unpinnedOptions;
		pageStartingIndexes = lobbyInfo.pageStartingIndexes;
	}

	public void organizeOptionsForAllPages()
	{
		for (int i = 0; i < pages; i++)
		{
			organizeOptionsForPage(i);
		}
	}

	// We need to do some swapping around and whatnot if we have personal content. This recurrs once
	private List<LobbyOption> organizeOptionsForPage(int page)
	{
		int spot = 0;
		int index = pageStartingIndexes[page];
		List<LobbyOption> optionsForThisPage = new List<LobbyOption>();

		// So we can pull 1x1s into the place of personalized content. 
		if (page > personalizedPage)
		{
			index += contentOffset;
		}


		while (spot < TOTAL_OPTIONS_PER_PAGE)
		{
			LobbyOption option = null;
			int spotX = spot % OPTION_COLUMNS_PER_PAGE;
			int spotY = Mathf.FloorToInt((float)spot / OPTION_COLUMNS_PER_PAGE);

			if (pinnedSpots[page].ContainsKey(spot) && getPinnedOption(page, spotX, spotY) != null)
			{
				// This spot has a pinned option. Find out which one and whether we've already created the button for it.
				option = getPinnedOption(page, spotX, spotY);

				if (option == null)
				{
					Debug.LogWarning("Couldn't find option at " + page + ", " + spot + ", " + spotX + ", " + spotY);
				}

				if (!optionsForThisPage.Contains(option))
				{
					optionsForThisPage.Add(option);
					option.page = page;
				}
			}
			else
			{
				// If we ran out of options, put in the place holder ones.
				if (index >= unpinnedOptions.Count)
				{
					option = new LobbyOption();
					option.type = LobbyOption.Type.COMING_SOON;
					option.name = "coming soon";
					option.isNormal = true;
					option.sortOrder = 9999999;
				}
				else
				{
					option = unpinnedOptions[index];
				}

				index++;

				optionsForThisPage.Add(option);
				option.page = page;
			}

			spot++;
		}

		// From here on out, we can actually manipulate the games before we make em.

		// Removed the personalized content if the game is already on the page. If we ever had multiple
		// personalized contents, we'd want to continually increment content offset and have a way to reset it after.
		// Same deal with the personalized page I suppose.
		if (PersonalizedContentLobbyOptionDecorator1x2.gameKey != "")
		{
			LobbyOption personalizedContent = null;
			for (int i = 0; i < optionsForThisPage.Count; i++)
			{
				// Find the option we need
				if (personalizedContent == null && optionsForThisPage[i].action == "personalized_content")
				{
					personalizedContent = optionsForThisPage[i];

					// go around again
					i = 0;
				}

				// Once we do, if we have any matches, remove the personalized content and bring in 2 unpinned.
				if  (personalizedContent != null)
				{
					LobbyGame game = LobbyGame.find(PersonalizedContentLobbyOptionDecorator1x2.gameKey);
					LobbyGame currentOptionGame = optionsForThisPage[i].game;

					if ( isSpecialGame(game) || // if it's a challenge lobby game we are trying to put into personalized content
						 (currentOptionGame != null && currentOptionGame.keyName == PersonalizedContentLobbyOptionDecorator1x2.gameKey) // if the option is already positioned on this page
					)
					{
						if (index >= unpinnedOptions.Count || index + 1 >= unpinnedOptions.Count)
						{
							Bugsnag.LeaveBreadcrumb(string.Format
							(
								"Personalized Content: Indexing error! Current index: {0}, Unpinned count: {1}, Replacing game: {2}"
								, index
								, unpinnedOptions.Count
								, optionsForThisPage[i].game.keyName
							));
							
							return optionsForThisPage;
						}

						int overWriteSpot = optionsForThisPage.IndexOf(personalizedContent);
						if (overWriteSpot >= 0)
						{
							optionsForThisPage[overWriteSpot] = unpinnedOptions[index];
							index++;
							optionsForThisPage.Add(unpinnedOptions[index]);
							contentOffset = 2;
							personalizedPage = page;
						}
						else
						{
							// We could throw an error here, but I'm not sure why we would.
							return optionsForThisPage;
						}
					}
				}
			}
		}

		return optionsForThisPage;
	}

	// A main lobby page panel has been created.
	private void onPageCreated(GameObject pagePanel, int page)
	{
		int spot = 0;   // A local spot on the current page.
		lobbyPageV3 = pagePanel.GetComponent<LobbyPageV3>();
		List<LobbyOption> optionsForThisPage = new List<LobbyOption>();
		List<int> pinnedSpotsOnPage = new List<int>();
		optionsForThisPage = organizeOptionsForPage(page);

		LobbyOption option;

		for (int i = 0; i < optionsForThisPage.Count; i++)
		{
			option = optionsForThisPage[i];
			
			Vector2 buttonPosition;

			float width = 0;
			float height = 0;
			
			int spotX = spot % OPTION_COLUMNS_PER_PAGE;
			
			// stat tracking lobby position
			int yPos = spot >= OPTION_COLUMNS_PER_PAGE ? 1 : 0; 
			int xPos = page * OPTION_COLUMNS_PER_PAGE + spotX + 1;

			// Make sure the occupied spots are skipped
			while (pinnedSpotsOnPage.Contains(spot))
			{
				spot++;
				xPos++;
				spotX = spot % TOTAL_OPTIONS_PER_PAGE;
			}
			if (option.isPinned)
			{
				// Just in case we ever pin a 1x1. On a similar note if we ever do
				// unpinned 1x2's we'll need similar logic below.

				if (option.pinned.shape != Pinned.Shape.NOT_SET)
				{
					for (int j = spot; j < spot + option.pinned.width; j++)
					{
						pinnedSpotsOnPage.Add(j); // add x
						if (option.pinned.height > 1) 
						{
							pinnedSpotsOnPage.Add(j + OPTION_COLUMNS_PER_PAGE); // add y pos
						}
					}
				}

				// Size rectangular shapes dynamically.
				width = getFrameWidth(option.pinned.width);
				height = getFrameHeight(option.pinned.height);
			}
			else
			{
				// Non-pinned options are 1x1, so that's hardcoded here.
				width = getFrameWidth(1);
				height = getFrameHeight(1);
			}
			
			Vector2? pos = getPositionForSpot(spot);
			if (pos == null)
			{
				//If we run out of spots, return before actually trying to create a lobby option object 
				return;
			}
			buttonPosition = pos.Value;

			option.lobbyPosition = (xPos * 10) + yPos; // Encoded as column*10 + row.

			if (option.type == LobbyOption.Type.ACTION)
			{
				option.panel = getGameObjectForAction(option, page);
			}
			else if (option.isRoyalRush)
			{
				StartCoroutine(TryLoadRoyalRush(option, spotX, page, pagePanel, width, height, buttonPosition));
			}
			else if (option.type == LobbyOption.Type.COMING_SOON)
			{
				// One of those empty "Coming Soon"
				option.panel = CommonGameObject.instantiate(optionButtonPrefabComingSoon) as GameObject;
			}
			else if (option.game.xp.isSkuGameUnlock)
			{
				// tbd look into making optionButtonPrefabLearnMore1X2 a generic overlay
				option.panel = CommonGameObject.instantiate(optionButtonPrefabLearnMore1X2) as GameObject;
			}
			else
			{
				//Grab a generic option from our cache if we have one ready or make a new one
				if (genericLobbyOptionsCache.Count == 0)
				{
					option.panel = CommonGameObject.instantiate(optionButtonPrefabGeneric) as GameObject;
				}
				else
				{
					option.panel = genericLobbyOptionsCache.Pop();
					option.panel.SetActive(true);
				}

				lobbyPageV3.lobbyOptions.Add(option);
			}

			if (option.panel != null)
			{
				handleOptionSetup(option, page, pagePanel, width, height, buttonPosition);
			}
			spot++;
		}
	}

	private Vector2? getPositionForSpot(int spot)
	{
		int maxCount = OPTION_ROWS_PER_PAGE * OPTION_COLUMNS_PER_PAGE;
		if (lobbyPageV3.spotsLocations.Count != maxCount)
		{
			Debug.LogError("Invalid config. Make sure the spot positions are assigned in the Main Lobby Prefab");
			return null;
		}

		if (spot >= maxCount)
		{
			Debug.LogError("The spot specified is not valid");
			return null;
		}

		return lobbyPageV3.spotsLocations[spot].localPosition;
	}

	private float getFrameWidth(int count)
	{
		return MAIN_BUTTON_SPOT_WIDTH * count + MAIN_BUTTON_HORIZONTAL_SPACING * (count - 1);
	}

	private float getFrameHeight(int count)
	{
		return MAIN_BUTTON_SPOT_HEIGHT * count + MAIN_BUTTON_VERTICAL_SPACING * (count - 1);
	}

	private void loadCustomPanelOption(string path, LobbyOption option, int page, GameObject pagePanel, float width, float height, Vector2 buttonPosition)
	{
		Dict args = Dict.create(
			D.DATA, option,
			D.INDEX, page,
			D.OBJECT, pagePanel,
			D.WIDTH, width,
			D.HEIGHT, height,
			D.OPTION, buttonPosition
		);
		AssetBundleManager.load(path, customPanelOptionLoadSuccess, customPanelOptionLoadFailed, args);
	}

	// Used by LobbyLoader to preload asset bundle.
	private void customPanelOptionLoadSuccess(string assetPath, Object obj, Dict data = null)
	{
		LobbyOption option = (LobbyOption)data.getWithDefault(D.DATA, null);
		int pageIndex = (int)data.getWithDefault(D.INDEX, -1);
		GameObject pagePanel = (GameObject)data.getWithDefault(D.OBJECT, null);
		float width = (float)data.getWithDefault(D.WIDTH, 0);
		float height = (float)data.getWithDefault(D.HEIGHT, 0);
		Vector2 buttonPosition = (Vector2)data.getWithDefault(D.OPTION, Vector2.zero);
		if (option != null && pageIndex >= 0 && pagePanel != null)
		{
			option.panel = CommonGameObject.instantiate(obj) as GameObject;
			handleOptionSetup(option, pageIndex, pagePanel, width, height, buttonPosition);
		}
	}
	
	private void customPanelOptionLoadFailed(string assetPath, Dict data = null)
	{
		Debug.LogError("Failed to download custom panel asset: " + assetPath);
	}

	public LobbyOption getFirstOption()
	{
		if (displayedOptions == null || displayedOptions.Count == 0)
		{
			return null;
		}
		return displayedOptions[0];
	}

	public LobbyOption getSpecificOption(string gameKey)
	{
		if (displayedOptions == null || displayedOptions.Count == 0)
		{
			return null;
		}

		for (int i = 0; i < displayedOptions.Count; i++)
		{
			if (displayedOptions[i] == null || displayedOptions[i].game == null)
			{
				continue;
			}
			
			if (displayedOptions[i].game.keyName == gameKey)
			{
				return displayedOptions[i];
			}
		}

		return null;
	}

	private void handleOptionSetup(LobbyOption option, int page, GameObject pagePanel, float width, float height, Vector2 buttonPosition)
	{
		if (option != null && option.panel != null && pagePanel != null)
		{
			option.button = option.panel.GetComponent<LobbyOptionButton>();

			if (option.button != null)
			{
				option.panel.transform.parent = pagePanel.transform;
				option.panel.transform.localScale = Vector3.one;
				option.panel.transform.localPosition = buttonPosition;
				CommonGameObject.setLayerRecursively(option.panel, pagePanel.layer);
				option.button.setup(option, page, width, height);
				displayedOptions.Add(option);
				// Load the image right away, don't bother re-looking this stuff up.
				RoutineRunner.instance.StartCoroutine(option.loadImages());
			}
		}
		else
		{
			if (option != null)
			{
				Debug.LogError("MainLobbyV3::handleOptionSetup - Failed to setup lobby option " + option.name);
			}
			else
			{
				Debug.LogError("MainLobbyV3::handleOptionSetup - Attempted to setup null lobby option");
			}

		}
	}

	IEnumerator TryLoadRoyalRush(LobbyOption option, int spotX, int page, GameObject pagePanel, float width, float height, Vector2 buttonPosition)
	{
		float loadTime = 0.0f;
		while (RoyalRushEvent.instance.lobbyOptionReference == null && loadTime < 10.0f)
		{
			loadTime += Time.unscaledDeltaTime;
			yield return null;
		}
			
		if (RoyalRushEvent.instance.lobbyOptionReference != null)
		{
			option.panel = CommonGameObject.instantiate(RoyalRushEvent.instance.lobbyOptionReference) as GameObject;
			LobbyOptionButtonRoyalRush buttonToManipulate = option.panel.GetComponent<LobbyOptionButtonRoyalRush>();
			buttonToManipulate.designateGame(option.game.keyName); // This won't be necesary eventually I think
																   // Setup in case we need to do the FTUE. This will be cleared when we do the forced scroll for the main lobby.
			if (LobbyOptionButtonRoyalRush.ftueButton == null && buttonToManipulate.rushInfo.currentState == RoyalRushInfo.STATE.AVAILABLE)
			{
				LobbyOptionButtonRoyalRush.pinnedLoc = spotX;
				LobbyOptionButtonRoyalRush.ftuePage = page;
				LobbyOptionButtonRoyalRush.ftueButton = buttonToManipulate;
			}

			handleOptionSetup(option, page, pagePanel, width, height, buttonPosition);
		}
		else
		{
			Bugsnag.LeaveBreadcrumb("Failed to load the royal rush lobby option");
		}
	}

	private GameObject getGameObjectForAction(LobbyOption option, int page)
	{
		if (option.isBannerAction)
		{
			return CommonGameObject.instantiate(optionButtonPrefabGeneric) as GameObject;
		}
		switch (option.action)
		{
			case "personalized_content":
				return CommonGameObject.instantiate(optionButtonPrefabGeneric) as GameObject;
			case "loz_lobby":
				if (LOZLobby.assetData.portalPrefab != null)
				{
					return CommonGameObject.instantiate(LOZLobby.assetData.portalPrefab) as GameObject;
				}
				break;

			case "vip_lobby":
				if (VIPLobbyHIRRevamp.optionPrefabPortal != null)
				{
					return CommonGameObject.instantiate(VIPLobbyHIRRevamp.optionPrefabPortal) as GameObject;
				}
				break;
							
			case "ppu_portal":
				if (PartnerPowerupCampaign.lobbyButton != null)
				{
					return CommonGameObject.instantiate(PartnerPowerupCampaign.lobbyButton) as GameObject;
				}
				break;
				
			case "ticket_tumbler":
				if (TicketTumblerFeature.instance.lobbyButton != null)
				{
					return CommonGameObject.instantiate(TicketTumblerFeature.instance.lobbyButton) as GameObject;
				}
				break;							

			case "sin_city_strip_lobby":
				if (SinCityLobby.assetData.portalPrefab != null)
				{
					return CommonGameObject.instantiate(SinCityLobby.assetData.portalPrefab) as GameObject;
				}
				break;

			case "slotventure":
				if (SlotventuresLobby.assetData.portalPrefab != null)
				{
					ChallengeLobbyOptionButtonSlotventure.pageIndex = page;
					return CommonGameObject.instantiate(SlotventuresLobby.assetData.mainLobbyOptionPrefab) as GameObject;
				}
				break;
			case "max_voltage_lobby":
				if (MaxVoltageLobbyHIR.optionPrefabPortal != null)
				{
					return CommonGameObject.instantiate(MaxVoltageLobbyHIR.optionPrefabPortal) as GameObject;
				}
				break;
			default:
				if (option.action.FastStartsWith(DoSomething.RECOMMENDED_GAME_PREFIX) ||
				    option.action.FastStartsWith(DoSomething.FAVORITE_GAME_PREFIX))
				{
					return CommonGameObject.instantiate(optionButtonPrefabGeneric) as GameObject;
				}
				break;
		}

		return CommonGameObject.instantiate(optionButtonPrefabAction) as GameObject;
	}

	public void transitionToLobby(GenericDelegate createLobbyMethod, string audioToPlay = "", string userFlowKey = "")
	{
		if (blackFade == null)
		{
			blackFade = new LobbyTransitionBlackFade(null, onTransitionComplete);
		}
		LobbyTransitioner.addTransition(blackFade, Dict.create(D.CALLBACK, createLobbyMethod, D.AUDIO_KEY, audioToPlay, D.KEY, userFlowKey));
		LobbyTransitioner.playTransition(blackFade);
	}

	public void onTransitionComplete(Dict args = null)
	{
		cleanupBeforeDestroy();
		Destroy(gameObject);
	}

	public override IEnumerator transitionToVIPLobby()
	{
		yield return StartCoroutine(base.transitionToVIPLobby());
		yield return RoutineRunner.instance.StartCoroutine(BlackFaderScript.instance.fadeTo(1.0f));
		yield return new WaitForSeconds(0.5f);
		
		LobbyLoader.instance.createVIPLobby();

		cleanupBeforeDestroy();
		Destroy(gameObject);
	}

	public override IEnumerator transitionToLOZLobby()
	{
		yield return RoutineRunner.instance.StartCoroutine(base.transitionToLOZLobby());
		yield return RoutineRunner.instance.StartCoroutine(BlackFaderScript.instance.fadeTo(1.0f));
		yield return new WaitForSeconds(0.5f);
		
		LobbyLoader.instance.createLOZLobby();

		cleanupBeforeDestroy();
		Destroy(gameObject);
	}

	public override IEnumerator transitionToMaxVoltageLobby()
	{
		yield return RoutineRunner.instance.StartCoroutine(base.transitionToMaxVoltageLobby());
		yield return RoutineRunner.instance.StartCoroutine(BlackFaderScript.instance.fadeTo(1.0f));
		yield return new WaitForSeconds(0.5f);
		
		LobbyLoader.instance.createMaxVoltageLobby();

		cleanupBeforeDestroy();
		Destroy(gameObject);
	}

	public override IEnumerator transitionToSlotventureLobby()
	{
		yield return StartCoroutine(base.transitionToSlotventureLobby());
		Loading.show(Loading.LoadingTransactionTarget.SLOTVENTURE_LOBBY);
		LobbyLoader.instance.createSlotventureLobby();

		cleanupBeforeDestroy();
		Destroy(gameObject);
	}

	// The main call to go into the Max Voltage Lobby from here.
	public override IEnumerator transitionToChallengeLobby(string campaignName)
	{
		yield return RoutineRunner.instance.StartCoroutine(base.transitionToChallengeLobby(campaignName));

		LobbyAssetData assetData = ChallengeLobby.findAssetDataForCampaign(campaignName);

		if (assetData != null)
		{		
			Audio.play(assetData.getAudioByKey(LobbyAssetData.TRANSITION));
		}

		yield return RoutineRunner.instance.StartCoroutine(BlackFaderScript.instance.fadeTo(1.0f));
		yield return new WaitForSeconds(0.5f);

		LobbyLoader.instance.createChallengeLobby(campaignName);

		cleanupBeforeDestroy();
		Destroy(gameObject);
	}

	public bool isSpecialGame(LobbyGame game)
	{
		return game != null && (game.isChallengeLobbyGame || game.isRoyalRush || game.isEOSControlled);
	}

	public bool isGameInView(LobbyGame game)
	{
		if (game != null)
		{
			List<LobbyOption> optionsOnPage = organizeOptionsForPage(getTrackedScrollPosition());

			if (getTrackedScrollPosition() + 1 < pageStartingIndexes.Count)
			{
				List<LobbyOption> optionsOnNextPage = organizeOptionsForPage(getTrackedScrollPosition() + 1);
				if (optionsOnNextPage.Count > 0)
				{
					optionsOnPage.AddRange(optionsOnNextPage);
				}
			}

			for (int i = 0; i < optionsOnPage.Count; ++i)
			{
				if (optionsOnPage[i].game != null && optionsOnPage[i].game == game)
				{
					return true;
				}
			}
		}

		return false;
	}

	public override void cleanupBeforeDestroy()
	{
		base.cleanupBeforeDestroy();
		eliteTransitionObject = null;
		eliteTransition = null;
	}

	// Defined here to avoid throwing exceptions in Glb::reinitializeGame() since it
	// attempts to call this method on all types that inherit from IResetGame.
	// By default, GetMethod() does not return static methods that types inherit from parents.
	// This could potentially be avoided by changing Glb to use BindingFlags.FlattenHierarchy 
	new public static void resetStaticClassData()
	{
		eliteTransitionObject = null;
		eliteTransition = null;
	}
}
 
